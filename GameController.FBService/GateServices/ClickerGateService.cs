

using GameController.FBService.Extensions;
using GameController.FBService.Services;
using System.Text.Json;

namespace GameController.FBService.GateServices
{
    public class ClickerGateService : IClickerGateService
    {
        private readonly ILogger<ClickerGateService> _logger;
        private readonly ICacheService _cache;

        // ✅ NEW: tuneable knobs
        private readonly int _historyN;
        private readonly int _sameBandSec;
        private readonly int _sameCountToFlag;
        private readonly int _minMedianSec;
        private readonly int _maxMedianSec;

        private readonly TimeSpan _challengeTtl;
        private readonly TimeSpan _verifiedTtl;
        private readonly TimeSpan _historyTtl;

        public ClickerGateService(ILogger<ClickerGateService> logger, ICacheService cache)
        {
            _logger = logger;
            _cache = cache;

            // ✅ NEW: defaults (you can move these to config later)
            _historyN = 10;
            _sameBandSec = 1;          // +- 1 sec
            _sameCountToFlag = 8;      // 8 of 9 intervals close to median => suspicious
            _minMedianSec = 2;
            _maxMedianSec = 15;

            _challengeTtl = TimeSpan.FromMinutes(2);
            _verifiedTtl = TimeSpan.FromMinutes(60);
            _historyTtl = TimeSpan.FromHours(2);
        }

        // -----------------------------
        // Keys
        // -----------------------------
        private static string KHist(string psid) => $"abuse:hist:{psid}";
        private static string KPending(string psid) => $"abuse:cap:pending:{psid}";
        private static string KVerified(string psid) => $"abuse:cap:verified:{psid}";
        private static string KLastChalSent(string psid) => $"abuse:cap:lastsent:{psid}"; // anti-spam (optional)

        // Challenge payload format: CAP:<id>:<answer>
        private static bool IsCapPayload(string payload) => payload.StartsWith("CAP:", StringComparison.OrdinalIgnoreCase);

        public async Task<GateDecision> CheckVoteGateAsync(string psid, DateTime utcNow, string votePayload)
        {
            // ✅ NEW: If verified recently -> allow straight
            var verified = await _cache.GetAsync<string>(KVerified(psid));
            if (!string.IsNullOrEmpty(verified))
            {
                await AppendHistoryAsync(psid, utcNow);
                return GateDecision.Allow("verified");
            }

            // ✅ NEW: If challenge already pending -> block
            var pending = await _cache.GetAsync<string>(KPending(psid));
            if (!string.IsNullOrEmpty(pending))
            {
                await AppendHistoryAsync(psid, utcNow);

                // Optional: avoid spamming resend, but you can resend if you want
                return GateDecision.Block("challenge_pending");
            }

            // ✅ NEW: Update history + detect suspicious rhythm
            var (times, suspicious, debug) = await AppendHistoryAndDetectAsync(psid, utcNow);

            if (!suspicious)
            {
                return GateDecision.Allow("normal_rhythm");
            }

            // ✅ NEW: Create challenge
            var challenge = CreateMathChallenge();

            // Store pending answer in Redis (as one string)
            // value: "<chalId>|<correctAnswer>"
            await _cache.SetAsync(KPending(psid), $"{challenge.challengeId}|{challenge.correct}", _challengeTtl);

            // (optional) remember "last sent" to rate-limit resends
            await _cache.SetAsync(KLastChalSent(psid), utcNow.ToString("O"), TimeSpan.FromMinutes(1));

            _logger.LogWarningWithCaller($"[CAPTCHA] Suspicious rhythm detected for PSID={psid}. {debug}. ChallengeId={challenge.challengeId}");

            return GateDecision.Block(
                reason: "suspicious_rhythm",
                challenge: new ChallengeToSend
                {
                    Text = challenge.text,
                    Buttons = challenge.buttons
                }
            );
        }

        public async Task<bool> TrySolveChallengeAsync(string psid, DateTime utcNow, string capPayload)
        {
            if (string.IsNullOrWhiteSpace(capPayload) || !IsCapPayload(capPayload))
                return false;

            // CAP:<id>:<answer>
            var parts = capPayload.Split(':', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 3) return false;

            var chalId = parts[1].Trim();
            var answer = parts[2].Trim();

            var pending = await _cache.GetAsync<string>(KPending(psid));
            if (string.IsNullOrEmpty(pending)) return false;

            var p = pending.Split('|', StringSplitOptions.RemoveEmptyEntries);
            if (p.Length < 2) return false;

            var expectedId = p[0];
            var expectedAnswer = p[1];

            if (!string.Equals(chalId, expectedId, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarningWithCaller($"[CAPTCHA] Wrong challengeId. PSID={psid}, got={chalId}, expected={expectedId}");
                return false;
            }

            if (!string.Equals(answer, expectedAnswer, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarningWithCaller($"[CAPTCHA] Wrong answer. PSID={psid}, got={answer}, expected={expectedAnswer}");
                return false;
            }

            // ✅ NEW: Mark verified
            await _cache.SetAsync(KVerified(psid), "1", _verifiedTtl);

            // ✅ NEW: Remove pending (simple way: overwrite with short TTL)
            await _cache.SetAsync(KPending(psid), "", TimeSpan.FromSeconds(1));

            _logger.LogInformationWithCaller($"[CAPTCHA] Solved. PSID={psid}, VerifiedTTL={_verifiedTtl.TotalMinutes}m");
            return true;
        }

        // -----------------------------
        // History + detection
        // -----------------------------
        private async Task AppendHistoryAsync(string psid, DateTime utcNow)
        {
            // lightweight: just store times, no detection
            var times = await ReadHistoryAsync(psid);
            times.Add(utcNow);
            Trim(times, _historyN);
            await WriteHistoryAsync(psid, times);
        }

        private async Task<(List<DateTime> times, bool suspicious, string debug)> AppendHistoryAndDetectAsync(string psid, DateTime utcNow)
        {
            var times = await ReadHistoryAsync(psid);
            times.Add(utcNow);
            Trim(times, _historyN);

            await WriteHistoryAsync(psid, times);

            if (times.Count < 6) // need enough points
                return (times, false, $"hist={times.Count}");

            // Compute intervals in seconds
            var intervals = new List<double>();
            for (int i = 1; i < times.Count; i++)
            {
                var dt = (times[i] - times[i - 1]).TotalSeconds;
                if (dt < 0.3) continue;
                if (dt > 120) continue;
                intervals.Add(dt);
            }

            if (intervals.Count < 5)
                return (times, false, $"intervals={intervals.Count}");

            var median = Median(intervals);

            // Only suspicious if median looks like “clicker rhythm”
            if (median < _minMedianSec || median > _maxMedianSec)
                return (times, false, $"median={median:0.00}s out of band");

            var same = intervals.Count(x => Math.Abs(x - median) <= _sameBandSec);

            var suspicious = same >= _sameCountToFlag;
            var debug = $"hist={times.Count}, intervals={intervals.Count}, median={median:0.00}s, same={same}/{intervals.Count}";
            return (times, suspicious, debug);
        }

        private async Task<List<DateTime>> ReadHistoryAsync(string psid)
        {
            var json = await _cache.GetAsync<string>(KHist(psid));
            if (string.IsNullOrWhiteSpace(json))
                return new List<DateTime>();

            try
            {
                var arr = JsonSerializer.Deserialize<List<DateTime>>(json);
                return arr ?? new List<DateTime>();
            }
            catch
            {
                return new List<DateTime>();
            }
        }

        private async Task WriteHistoryAsync(string psid, List<DateTime> times)
        {
            var json = JsonSerializer.Serialize(times);
            await _cache.SetAsync(KHist(psid), json, _historyTtl);
        }

        private static void Trim(List<DateTime> times, int max)
        {
            if (times.Count <= max) return;
            // keep last 'max'
            var skip = times.Count - max;
            times.RemoveRange(0, skip);
        }

        private static double Median(List<double> values)
        {
            var v = values.OrderBy(x => x).ToList();
            var n = v.Count;
            if (n == 0) return 0;
            if (n % 2 == 1) return v[n / 2];
            return (v[n / 2 - 1] + v[n / 2]) / 2.0;
        }

        private (string challengeId, string correct, string text, List<(string Title, string Payload)> buttons) CreateMathChallenge()
        {
            // ✅ NEW: simple deterministic question (can randomize later)
            // Question: 7+2 -> correct 9
            var chalId = Guid.NewGuid().ToString("N")[..4]; // short id
            var correct = "9";

            var text = "დასადასტურებლად: რამდენია 7+2 ?";

            // 3 buttons: 2,9,5
            var buttons = new List<(string Title, string Payload)>
            {
                ("2", $"CAP:{chalId}:2"),
                ("9", $"CAP:{chalId}:9"),
                ("5", $"CAP:{chalId}:5"),
            };

            return (chalId, correct, text, buttons);
        }
    }
}
