
using GameController.FBService.Extensions;
using GameController.FBService.Heplers;
using GameController.FBService.Models;
using GameController.FBService.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Client;
using StackExchange.Redis;
using System.Buffers.Text;
using static System.Reflection.Metadata.BlobBuilder;

namespace GameController.FBService.AntiBotServices
{
	public class RedisClickerDetectionService : IClickerDetectionService
	{
		private readonly IDatabase _db;
		private readonly IGlobalVarsKeeper _vars;
		private readonly ClickerOptions _defaults;
		private readonly ILogger<RedisClickerDetectionService> _logger;
        private readonly IAppMetrics _metrics;

        // Redis Vars (runtime override)
        private const string VarWindowSeconds = "fb_clicker_window_seconds";
		private const string VarSameSecondThreshold = "fb_clicker_same_second_threshold";
		private const string VarWarnScore = "fb_clicker_warn_score";
		private const string VarBlockScore = "fb_clicker_block_score";
        private const string VarAskConfirmationScore = "fb_clicker_asl_confirmation_score";
        private const string VarBlockEnabled = "fb_clicker_block_enabled";

		private const string VarCooldownHugToleranceSeconds = "fb_clicker_cooldown_hug_tolerance_seconds";
		private const string VarCooldownHugPoints = "fb_clicker_cooldown_hug_points";
		private const string VarCooldownHugHitsToFlag = "fb_clicker_cooldown_hug_hits_to_flag";

		private const string VarAfterCooldownWindowSeconds = "fb_clicker_after_cooldown_window_seconds";
		private const string VarAfterCooldownBurstThreshold = "fb_clicker_after_cooldown_burst_threshold";
		private const string VarAfterCooldownBurstPoints = "fb_clicker_after_cooldown_burst_points";

		private const string VarRhythmMinSamples = "fb_clicker_rhythm_min_samples";
		private const string VarRhythmBandMaxExtraSeconds = "fb_clicker_rhythm_band_max_extra_seconds";
		private const string VarRhythmMaxMadSeconds = "fb_clicker_rhythm_max_mad_seconds";
		private const string VarRhythmEmaAlpha = "fb_clicker_rhythm_ema_alpha";
		private const string VarRhythmPoints = "fb_clicker_rhythm_points";
		private const string VarRhythmHitsToFlag = "fb_clicker_rhythm_hits_to_flag";


		private readonly IRedisTtlProvider _ttl; // ახალი დამატებული

		public RedisClickerDetectionService(
			IConnectionMultiplexer redis,
			IGlobalVarsKeeper vars,
			IOptions<ClickerOptions> defaults,
			IRedisTtlProvider ttl,
            IAppMetrics metrics,

            ILogger<RedisClickerDetectionService> logger)
		{
			_db = redis.GetDatabase();
			_vars = vars;
			_defaults = defaults.Value;
			_logger = logger;
			_ttl = ttl;
			_metrics = metrics;

        }



		public async Task<ClickerDecision> EvaluateAsync(string recipientId, string userId, string userName, DateTime utcNow)
		{

            _logger.LogWarningWithCaller($"Checking For CLICKERS");
            // 1) Load policy (defaults + Redis overrides)
            var policy = await LoadPolicyAsync();
			var flags = new List<string>();

			long unixSecond = new DateTimeOffset(utcNow).ToUnixTimeSeconds();
			var shouldAsk = false;
            var shouldBlock = false;

            // Keys
            string secUsersKey = RedisKeys.FB.Clicker.UsersBySecond(recipientId, unixSecond);
			string riskKey = RedisKeys.FB.Clicker.UserRisk(recipientId, userId);
			string suspectsKey = RedisKeys.FB.Clicker.Suspects(recipientId);

			/// 1.1) Name - based signals(low - cost).Never blocks alone.
            // დამატებულია 01 13 იანვარი, ედიტი 1
            bool nameLowQuality = await ApplyNameSignalsAsync(userName, policy, flags, riskKey);



            // 2) Phase C (v2/v3/v4) logic that needs last vote timestamp
            await EvaluateCooldownBasedSignalsAsync(recipientId, userId, unixSecond, policy, flags, riskKey, suspectsKey);

            // 2.1) Name + behavior combos (stronger)
            // დამატებულია 01 13 იანვარი, ედიტი 1
            if (nameLowQuality)
            {
                await ApplyNameCombosAsync(policy, flags, riskKey);
            }

            // 3) Same-second burst (points only with strong context)
            await TrackUsersInSecondAsync(secUsersKey, userId, policy);
			long usersThisSecond = await _db.SetLengthAsync(secUsersKey);
			if (usersThisSecond >= policy.SameSecondThreshold)
			{
				flags.Add("MANY_USERS_SAME_SECOND");

				bool hasStrongSignal = HasStrongContext(flags);

				if (hasStrongSignal)
				{
					int newScore = await AddRiskAsync(riskKey, points: 10);
					await TryMarkSuspectAsync(suspectsKey, userId, newScore, policy);

                    shouldAsk =
					policy.ConfirmEnabled &&
					newScore >= policy.ConfirmScore &&
					newScore < policy.BlockScore;

                    shouldBlock =
                    policy.BlockEnabled &&
                    newScore >= policy.BlockScore;

                    return BuildDecision(
						policy: policy,
						flags: flags,
						score: newScore,
						suspicious: true, // with strong signal we consider it suspicious
						shouldBlock: shouldBlock,
						shouldAskConfirmation: shouldAsk
                    );
				}

				flags.Add("SAME_SECOND_ONLY");
			}

			// 4) Return current risk score (cheap)
			int score = await GetRiskScoreAsync(riskKey);

			
			


			//if (flags.Count == 0 && score > 0)
			//{
            //    score = await ResetRiskAsync(riskKey);
            //}


            bool hasStrong = flags.Any(f =>
                f == "COOLDOWN_HUGGING" ||
                f == "REPEATED_COOLDOWN_HUGGING" ||
                f == "REPEATED_COOLDOWN_RHYTHM_BAND" ||
                f == "MANY_USERS_AFTER_COOLDOWN_WINDOW");

            if (!hasStrong && score > 0)
                score = await DecayRiskAsync(riskKey, 1);




            var isSuspicious = score >= policy.WarnScore;
            if (isSuspicious) flags.Add("RISK_SCORE_HIGH");

             shouldAsk =
                policy.ConfirmEnabled &&
                score >= policy.ConfirmScore &&
                score < policy.BlockScore;

             shouldBlock =
                policy.BlockEnabled &&
                score >= policy.BlockScore;



            return BuildDecision(
				policy: policy,
				flags: flags,
				score: score,
				suspicious: isSuspicious,
                shouldAskConfirmation: shouldAsk,

                shouldBlock: shouldBlock
            );
		}

        public async Task<Boolean> ApplyClikerMetrics(string clientName, ClickerDecision decision)
		{


            _metrics.IncRiskBandByCandidate(clientName, decision.RiskScore, 100, 160); // Blocked=RiskScore>=100
            _metrics.IncCandidateFlags(clientName, decision.Flags);



            if (decision?.IsSuspicious == true)
            {
                _metrics.IncSuspiciousByCandidate(clientName);
            }

			if (decision?.ShouldBlock == true)
			{
				_metrics.IncBlockedByCandidate(clientName);
			}

            if (decision?.ShouldAskConfirmation == true)
            {
                _metrics.IncAskedConfirmationByCandidate(clientName);
            }


            return true;
		}


        private async Task<ClickerOptions> LoadPolicyAsync()
		{
			// Defaults from appsettings
			var policy = new ClickerOptions
			{
				WindowSeconds = _defaults.WindowSeconds,
				SameSecondThreshold = _defaults.SameSecondThreshold,
				WarnScore = _defaults.WarnScore,
				BlockScore = _defaults.BlockScore,
				ConfirmScore = _defaults.ConfirmScore,
                BlockEnabled = _defaults.BlockEnabled,
				CooldownHugToleranceSeconds = _defaults.CooldownHugToleranceSeconds,
				CooldownHugPoints = _defaults.CooldownHugPoints,
				CooldownHugHitsToFlag = _defaults.CooldownHugHitsToFlag,
				AfterCooldownWindowSeconds = _defaults.AfterCooldownWindowSeconds,
				AfterCooldownBurstThreshold = _defaults.AfterCooldownBurstThreshold,
				AfterCooldownBurstPoints = _defaults.AfterCooldownBurstPoints,

				RhythmMinSamples = _defaults.RhythmMinSamples,
				RhythmBandMaxExtraSeconds = _defaults.RhythmBandMaxExtraSeconds,
				RhythmMaxMadSeconds = _defaults.RhythmMaxMadSeconds,
				RhythmEmaAlpha = _defaults.RhythmEmaAlpha,
				RhythmPoints = _defaults.RhythmPoints,
				RhythmHitsToFlag = _defaults.RhythmHitsToFlag,


				// Name-based defaults
				// დამატებულია 01 13 იანვარი, ედიტი 1
                NameDuplicateTokenPoints = _defaults.NameDuplicateTokenPoints,
                NameAllDigitsPoints = _defaults.NameAllDigitsPoints,
                NameAlnumShortPoints = _defaults.NameAlnumShortPoints,
                NameTooShortPoints = _defaults.NameTooShortPoints,
                NameLowAlphaRatioPoints = _defaults.NameLowAlphaRatioPoints,
                NameMinLength = _defaults.NameMinLength,
                NameAlnumShortMaxLength = _defaults.NameAlnumShortMaxLength,
                NameMinAlphaRatio = _defaults.NameMinAlphaRatio,
                NameComboCooldownHugPoints = _defaults.NameComboCooldownHugPoints,
                NameComboRhythmBandPoints = _defaults.NameComboRhythmBandPoints,
                NameComboAfterWindowBurstPoints = _defaults.NameComboAfterWindowBurstPoints

            };

			try
			{
				if (await _vars.ExistsAsync(VarWindowSeconds))
					policy.WindowSeconds = await _vars.GetValueAsync<int>(VarWindowSeconds);

				if (await _vars.ExistsAsync(VarSameSecondThreshold))
					policy.SameSecondThreshold = await _vars.GetValueAsync<int>(VarSameSecondThreshold);

				if (await _vars.ExistsAsync(VarWarnScore))
					policy.WarnScore = await _vars.GetValueAsync<int>(VarWarnScore);

				if (await _vars.ExistsAsync(VarBlockScore))
					policy.BlockScore = await _vars.GetValueAsync<int>(VarBlockScore);

                if (await _vars.ExistsAsync(VarAskConfirmationScore))
                    policy.ConfirmScore = await _vars.GetValueAsync<int>(VarAskConfirmationScore);

                if (await _vars.ExistsAsync(VarBlockEnabled))
					policy.BlockEnabled = await _vars.GetValueAsync<bool>(VarBlockEnabled);

				if (await _vars.ExistsAsync(VarAfterCooldownWindowSeconds))
					policy.AfterCooldownWindowSeconds = await _vars.GetValueAsync<int>(VarAfterCooldownWindowSeconds);

				if (await _vars.ExistsAsync(VarAfterCooldownBurstThreshold))
					policy.AfterCooldownBurstThreshold = await _vars.GetValueAsync<int>(VarAfterCooldownBurstThreshold);

				if (await _vars.ExistsAsync(VarAfterCooldownBurstPoints))
					policy.AfterCooldownBurstPoints = await _vars.GetValueAsync<int>(VarAfterCooldownBurstPoints);

				if (await _vars.ExistsAsync(VarCooldownHugToleranceSeconds))
					policy.CooldownHugToleranceSeconds = await _vars.GetValueAsync<int>(VarCooldownHugToleranceSeconds);

				if (await _vars.ExistsAsync(VarCooldownHugPoints))
					policy.CooldownHugPoints = await _vars.GetValueAsync<int>(VarCooldownHugPoints);

				if (await _vars.ExistsAsync(VarCooldownHugHitsToFlag))
					policy.CooldownHugHitsToFlag = await _vars.GetValueAsync<int>(VarCooldownHugHitsToFlag);

				if (await _vars.ExistsAsync(VarRhythmMinSamples))
					policy.RhythmMinSamples = await _vars.GetValueAsync<int>(VarRhythmMinSamples);

				if (await _vars.ExistsAsync(VarRhythmBandMaxExtraSeconds))
					policy.RhythmBandMaxExtraSeconds = await _vars.GetValueAsync<int>(VarRhythmBandMaxExtraSeconds);

				if (await _vars.ExistsAsync(VarRhythmMaxMadSeconds))
					policy.RhythmMaxMadSeconds = await _vars.GetValueAsync<int>(VarRhythmMaxMadSeconds);

				if (await _vars.ExistsAsync(VarRhythmEmaAlpha))
					policy.RhythmEmaAlpha = await _vars.GetValueAsync<double>(VarRhythmEmaAlpha);

				if (await _vars.ExistsAsync(VarRhythmPoints))
					policy.RhythmPoints = await _vars.GetValueAsync<int>(VarRhythmPoints);

				if (await _vars.ExistsAsync(VarRhythmHitsToFlag))
					policy.RhythmHitsToFlag = await _vars.GetValueAsync<int>(VarRhythmHitsToFlag);


			}
			catch (Exception ex)
			{
				_logger.LogWarning(ex, "Failed to load clicker policy overrides from Redis Vars. Using defaults.");
			}

			// Guardrails
			if (policy.WindowSeconds < 2) policy.WindowSeconds = 2;
			if (policy.WindowSeconds > 120) policy.WindowSeconds = 120;
			if (policy.SameSecondThreshold < 3) policy.SameSecondThreshold = 3;
			if (policy.SameSecondThreshold > 200) policy.SameSecondThreshold = 200;

			if (policy.CooldownHugToleranceSeconds < 0) policy.CooldownHugToleranceSeconds = 0;
			if (policy.CooldownHugToleranceSeconds > 10) policy.CooldownHugToleranceSeconds = 10;

			if (policy.CooldownHugPoints < 1) policy.CooldownHugPoints = 1;
			if (policy.CooldownHugPoints > 200) policy.CooldownHugPoints = 200;

			if (policy.CooldownHugHitsToFlag < 1) policy.CooldownHugHitsToFlag = 1;
			if (policy.CooldownHugHitsToFlag > 20) policy.CooldownHugHitsToFlag = 20;

			if (policy.AfterCooldownWindowSeconds < 2) policy.AfterCooldownWindowSeconds = 2;
			if (policy.AfterCooldownWindowSeconds > 10) policy.AfterCooldownWindowSeconds = 10;

			if (policy.AfterCooldownBurstThreshold < 3) policy.AfterCooldownBurstThreshold = 3;
			if (policy.AfterCooldownBurstThreshold > 200) policy.AfterCooldownBurstThreshold = 200;

			if (policy.AfterCooldownBurstPoints < 1) policy.AfterCooldownBurstPoints = 1;
			if (policy.AfterCooldownBurstPoints > 200) policy.AfterCooldownBurstPoints = 200;

			if (policy.RhythmMinSamples < 4) policy.RhythmMinSamples = 4;
			if (policy.RhythmMinSamples > 30) policy.RhythmMinSamples = 30;

			if (policy.RhythmBandMaxExtraSeconds < 15) policy.RhythmBandMaxExtraSeconds = 15;
			if (policy.RhythmBandMaxExtraSeconds > 300) policy.RhythmBandMaxExtraSeconds = 300;

			if (policy.RhythmMaxMadSeconds < 1) policy.RhythmMaxMadSeconds = 1;
			if (policy.RhythmMaxMadSeconds > 10) policy.RhythmMaxMadSeconds = 10;

			if (policy.RhythmEmaAlpha < 0.05) policy.RhythmEmaAlpha = 0.05;
			if (policy.RhythmEmaAlpha > 0.50) policy.RhythmEmaAlpha = 0.50;

			if (policy.RhythmPoints < 1) policy.RhythmPoints = 1;
			if (policy.RhythmPoints > 200) policy.RhythmPoints = 200;

			if (policy.RhythmHitsToFlag < 1) policy.RhythmHitsToFlag = 1;
			if (policy.RhythmHitsToFlag > 20) policy.RhythmHitsToFlag = 20;


            // Name guardrails
            // დამატებულია 01 13 იანვარი, ედიტი 1
            if (policy.NameMinLength < 1) policy.NameMinLength = 1;
            if (policy.NameMinLength > 10) policy.NameMinLength = 10;

            if (policy.NameAlnumShortMaxLength < 6) policy.NameAlnumShortMaxLength = 6;
            if (policy.NameAlnumShortMaxLength > 30) policy.NameAlnumShortMaxLength = 30;

            if (policy.NameMinAlphaRatio < 0.10) policy.NameMinAlphaRatio = 0.10;
            if (policy.NameMinAlphaRatio > 0.95) policy.NameMinAlphaRatio = 0.95;


            return policy;
		}



		#region Helper methods (refactor)

		private async Task EvaluateCooldownBasedSignalsAsync(
			string recipientId,
			string userId,
			long unixSecond,
			ClickerOptions policy,
			List<string> flags,
			string riskKey,
			string suspectsKey)
		{
			try
			{
				int cooldownSeconds = (int)_ttl.VoteCooldown.TotalSeconds;
				int tol = policy.CooldownHugToleranceSeconds;

				// sanity guards
				if (cooldownSeconds < 30 || tol < 0)
					return;

				string lastTsKey = RedisKeys.FB.Clicker.UserLastVoteUnix(recipientId, userId);
				string nearCntKey = RedisKeys.FB.Clicker.UserNearCooldownCount(recipientId, userId);

				// previous vote ts
				var lastVal = await _db.StringGetAsync(lastTsKey);
				if (lastVal.HasValue && long.TryParse(lastVal.ToString(), out var lastUnix))
				{
					int delta = (int)(unixSecond - lastUnix);

					await EvaluateRhythmBandAsync(
						recipientId, userId,
						unixSecond, delta,
						cooldownSeconds, policy,
						flags, riskKey, suspectsKey);

					await EvaluateAfterCooldownWindowBurstAsync(
						recipientId, userId,
						unixSecond, delta,
						cooldownSeconds, policy,
						flags, riskKey, suspectsKey);

					await EvaluateCooldownHuggingAsync(
						recipientId, userId,
						delta, cooldownSeconds, tol,
						policy,
						flags, riskKey, suspectsKey,
						nearCntKey);
				}

				// update last vote timestamp always (24h TTL)
				await _db.StringSetAsync(lastTsKey, unixSecond.ToString(), TimeSpan.FromHours(24));
			}
			catch (Exception ex)
			{
				_logger.LogWarning(ex, "Cooldown-based evaluation failed for {RecipientId}/{UserId}", recipientId, userId);
			}
		}

		private async Task EvaluateRhythmBandAsync(
			string recipientId,
			string userId,
			long unixSecond,
			int delta,
			int cooldownSeconds,
			ClickerOptions policy,
			List<string> flags,
			string riskKey,
			string suspectsKey)
		{
			// only >= cooldown (you already block earlier)
			if (delta < cooldownSeconds)
				return;

			try
			{
				string rhythmKey = RedisKeys.FB.Clicker.UserRhythmState(recipientId, userId);

				// Read state (ONE op)
				RedisValue[] state = await _db.HashGetAsync(rhythmKey, new RedisValue[] { "ema", "mad", "n", "hits" });

				double ema = (state[0].HasValue && double.TryParse(state[0].ToString(), out var emaParsed)) ? emaParsed : delta;
				double mad = (state[1].HasValue && double.TryParse(state[1].ToString(), out var madParsed)) ? madParsed : 9999.0;
				int n = (state[2].HasValue && int.TryParse(state[2].ToString(), out var nParsed)) ? nParsed : 0;
				int hits = (state[3].HasValue && int.TryParse(state[3].ToString(), out var hParsed)) ? hParsed : 0;

				double alpha = Clamp(policy.RhythmEmaAlpha, 0.05, 0.50);

				double prevEma = ema;
				ema = (n == 0) ? delta : (alpha * delta + (1.0 - alpha) * ema);

				double absErr = Math.Abs(delta - prevEma);
				mad = (n == 0) ? absErr : (alpha * absErr + (1.0 - alpha) * mad);

				n++;

				// Persist state + TTL
				await _db.HashSetAsync(rhythmKey, new HashEntry[]
				{
			new HashEntry("last", unixSecond),
			new HashEntry("ema", ema),
			new HashEntry("mad", mad),
			new HashEntry("n", n),
			new HashEntry("hits", hits)
				});
				await _db.KeyExpireAsync(rhythmKey, TimeSpan.FromHours(24));

				int extra = Clamp(policy.RhythmBandMaxExtraSeconds, 15, 300);
				int minSamples = Clamp(policy.RhythmMinSamples, 4, 30);
				int maxMad = Clamp(policy.RhythmMaxMadSeconds, 1, 10);

				bool inBand = ema >= cooldownSeconds && ema <= (cooldownSeconds + extra);
				bool stable = mad <= maxMad;

				if (n >= minSamples && inBand && stable)
				{
					flags.Add("COOLDOWN_RHYTHM_BAND");

					long newHits = await _db.HashIncrementAsync(rhythmKey, "hits", 1);

					if (newHits >= policy.RhythmHitsToFlag)
					{
						flags.Add("REPEATED_COOLDOWN_RHYTHM_BAND");

						int newScore = await AddRiskAsync(riskKey, policy.RhythmPoints);
						await TryMarkSuspectAsync(suspectsKey, userId, newScore, policy);
					}
				}
			}
			catch (Exception exRh)
			{
				_logger.LogWarning(exRh, "Rhythm-band evaluation failed for {RecipientId}/{UserId}", recipientId, userId);
			}
		}

		private async Task EvaluateAfterCooldownWindowBurstAsync(
			string recipientId,
			string userId,
			long unixSecond,
			int delta,
			int cooldownSeconds,
			ClickerOptions policy,
			List<string> flags,
			string riskKey,
			string suspectsKey)
		{
			int afterW = Clamp(policy.AfterCooldownWindowSeconds, 2, 10);

			if (delta < cooldownSeconds || delta > cooldownSeconds + afterW)
				return;

			flags.Add("AFTER_COOLDOWN_WINDOW");

			long bucketStart = unixSecond - (unixSecond % afterW);
			string afterBucketKey = RedisKeys.FB.Clicker.AfterCooldownUsersBucket(recipientId, bucketStart, afterW);

			await _db.SetAddAsync(afterBucketKey, userId);
			await _db.KeyExpireAsync(afterBucketKey, TimeSpan.FromSeconds(afterW + 2));

			long usersInAfterWindow = await _db.SetLengthAsync(afterBucketKey);
			if (usersInAfterWindow >= policy.AfterCooldownBurstThreshold)
			{
				flags.Add("MANY_USERS_AFTER_COOLDOWN_WINDOW");

				int newScore = await AddRiskAsync(riskKey, policy.AfterCooldownBurstPoints);
				await TryMarkSuspectAsync(suspectsKey, userId, newScore, policy);
			}
		}

		private async Task EvaluateCooldownHuggingAsync(
			string recipientId,
			string userId,
			int delta,
			int cooldownSeconds,
			int toleranceSeconds,
			ClickerOptions policy,
			List<string> flags,
			string riskKey,
			string suspectsKey,
			string nearCntKey)
		{
			if (Math.Abs(delta - cooldownSeconds) > toleranceSeconds)
				return;

			flags.Add("COOLDOWN_HUGGING");

			int newScore = await AddRiskAsync(riskKey, policy.CooldownHugPoints);

			int hitsCd = (int)await _db.StringIncrementAsync(nearCntKey, 1);
			await _db.KeyExpireAsync(nearCntKey, TimeSpan.FromHours(24));

			if (hitsCd >= policy.CooldownHugHitsToFlag)
			{
				flags.Add("REPEATED_COOLDOWN_HUGGING");
				await _db.SetAddAsync(suspectsKey, userId);
				await _db.KeyExpireAsync(suspectsKey, TimeSpan.FromHours(24));
			}

			await TryMarkSuspectAsync(suspectsKey, userId, newScore, policy);
		}

		private async Task TrackUsersInSecondAsync(string secUsersKey, string userId, ClickerOptions policy)
		{
			await _db.SetAddAsync(secUsersKey, userId);
			await _db.KeyExpireAsync(secUsersKey, TimeSpan.FromSeconds(policy.WindowSeconds + 2));
		}

		private static bool HasStrongContext(List<string> flags)
		{
			// Strong signatures only
			return flags.Contains("AFTER_COOLDOWN_WINDOW")
				|| flags.Contains("MANY_USERS_AFTER_COOLDOWN_WINDOW")
				|| flags.Contains("COOLDOWN_HUGGING")
				|| flags.Contains("REPEATED_COOLDOWN_HUGGING")
				|| flags.Contains("COOLDOWN_RHYTHM_BAND")
				|| flags.Contains("REPEATED_COOLDOWN_RHYTHM_BAND");
		}

		private async Task<int> AddRiskAsync(string riskKey, int points)
		{
			int newScore = (int)await _db.StringIncrementAsync(riskKey, points);
			await _db.KeyExpireAsync(riskKey, TimeSpan.FromHours(24));
			return newScore;
		}

        private async Task<int> ResetRiskAsync(string riskKey)
        {
            await _db.StringSetAsync(riskKey, 0);
            await _db.KeyExpireAsync(riskKey, TimeSpan.FromHours(24));
            return 0;
        }

        private const string DecayRiskLua = @"
local v = redis.call('GET', KEYS[1])
if not v then return 0 end

local n = tonumber(v)
if not n then n = 0 end

local step = tonumber(ARGV[1])
if not step then step = 1 end

n = n - step
if n < 0 then n = 0 end

redis.call('SET', KEYS[1], tostring(n))
redis.call('EXPIRE', KEYS[1], tonumber(ARGV[2]))
return n
";

        private async Task<int> DecayRiskAsync(string riskKey, int step)
        {
            if (step <= 0) step = 1;

            var ttlSeconds = (int)TimeSpan.FromHours(24).TotalSeconds;

            var res = await _db.ScriptEvaluateAsync(
                DecayRiskLua,                       // ✅ string
                new RedisKey[] { riskKey },
                new RedisValue[] { step, ttlSeconds });

            return (int)res;
        }



        private async Task<int> GetRiskScoreAsync(string riskKey)
		{
			var scoreVal = await _db.StringGetAsync(riskKey);
			if (scoreVal.HasValue && int.TryParse(scoreVal.ToString(), out var parsed))
				return parsed;
			return 0;
		}

		private async Task TryMarkSuspectAsync(string suspectsKey, string userId, int score, ClickerOptions policy)
		{
			if (score < policy.WarnScore)
				return;

			await _db.SetAddAsync(suspectsKey, userId);
			await _db.KeyExpireAsync(suspectsKey, TimeSpan.FromHours(24));
		}

		private static ClickerDecision BuildDecision(ClickerOptions policy, List<string> flags, int score, bool suspicious, bool shouldBlock, bool shouldAskConfirmation)
		{
			return new ClickerDecision
			{
				IsSuspicious = suspicious,
				RiskScore = score,
				Flags = flags.ToArray(),
				ShouldBlock = shouldBlock,
				ShouldAskConfirmation = shouldAskConfirmation
            };
		}

		private static int Clamp(int value, int min, int max)
		{
			if (value < min) return min;
			if (value > max) return max;
			return value;
		}

		private static double Clamp(double value, double min, double max)
		{
			if (value < min) return min;
			if (value > max) return max;
			return value;
		}


        // --------------------------------------------------------------------
        // Name-based helpers
        // დამატებულია 01 13 იანვარი, ედიტი 1
        // --------------------------------------------------------------------

        private async Task<bool> ApplyNameSignalsAsync(string userName, ClickerOptions policy, List<string> flags, string riskKey)
        {
            if (string.IsNullOrWhiteSpace(userName))
                return false;

            var (nameFlags, points, isLowQuality) = EvaluateNameSignals(userName, policy);

            foreach (var f in nameFlags)
                flags.Add(f);

            if (points > 0)
                await AddRiskAsync(riskKey, points);

            return isLowQuality;
        }

        private async Task ApplyNameCombosAsync(ClickerOptions policy, List<string> flags, string riskKey)
        {
            // Name + Hugging
            if (flags.Contains("COOLDOWN_HUGGING"))
            {
                flags.Add("NAME_PLUS_COOLDOWN_HUGGING");
                if (policy.NameComboCooldownHugPoints > 0)
                    await AddRiskAsync(riskKey, policy.NameComboCooldownHugPoints);
            }

            // Name + Rhythm Band (either band or repeated band)
            if (flags.Contains("COOLDOWN_RHYTHM_BAND") || flags.Contains("REPEATED_COOLDOWN_RHYTHM_BAND"))
            {
                flags.Add("NAME_PLUS_RHYTHM_BAND");
                if (policy.NameComboRhythmBandPoints > 0)
                    await AddRiskAsync(riskKey, policy.NameComboRhythmBandPoints);
            }

            // Name + After window burst (strongest after-window signal)
            if (flags.Contains("MANY_USERS_AFTER_COOLDOWN_WINDOW"))
            {
                flags.Add("NAME_PLUS_AFTER_WINDOW_BURST");
                if (policy.NameComboAfterWindowBurstPoints > 0)
                    await AddRiskAsync(riskKey, policy.NameComboAfterWindowBurstPoints);
            }
        }

        private static (List<string> flags, int points, bool lowQuality) EvaluateNameSignals(string userName, ClickerOptions policy)
        {
            var flags = new List<string>();
            int points = 0;

            string s = CollapseSpaces(userName.Trim());
            if (s.Length == 0)
                return (flags, 0, false);

            int signals = 0;

            // TOO_SHORT
            if (s.Length < policy.NameMinLength)
            {
                flags.Add("NAME_TOO_SHORT");
                points += policy.NameTooShortPoints;
                signals++;
            }

            // ALL_DIGITS (ignore spaces)
            string compact = s.Replace(" ", "");
            if (compact.Length > 0 && compact.All(char.IsDigit))
            {
                flags.Add("NAME_ALL_DIGITS");
                points += policy.NameAllDigitsPoints;
                signals++;
            }

            // DUPLICATE_TOKEN: two tokens same (case-insensitive)
            var tokens = s.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (tokens.Length >= 2)
            {
                if (string.Equals(tokens[0], tokens[1], StringComparison.OrdinalIgnoreCase))
                {
                    flags.Add("NAME_DUPLICATE_TOKEN");
                    points += policy.NameDuplicateTokenPoints;
                    signals++;
                }
            }

            // ALNUM_SHORT: has both letters and digits, small length
            bool hasLetter = compact.Any(char.IsLetter);
            bool hasDigit = compact.Any(char.IsDigit);
            if (hasLetter && hasDigit && compact.Length <= policy.NameAlnumShortMaxLength)
            {
                flags.Add("NAME_ALNUM_SHORT");
                points += policy.NameAlnumShortPoints;
                signals++;
            }

            // LOW_ALPHA_RATIO: letters/(letters+digits) < threshold
            int letters = compact.Count(char.IsLetter);
            int digits = compact.Count(char.IsDigit);
            int denom = letters + digits;
            if (denom > 0)
            {
                double ratio = (double)letters / denom;
                if (ratio < policy.NameMinAlphaRatio)
                {
                    flags.Add("NAME_LOW_ALPHA_RATIO");
                    points += policy.NameLowAlphaRatioPoints;
                    signals++;
                }
            }

            bool lowQuality = signals > 0;
            if (lowQuality)
                flags.Add("NAME_LOW_QUALITY");

            return (flags, points, lowQuality);
        }

        private static string CollapseSpaces(string input)
        {
            // simple collapse without regex (fast)
            var sb = new System.Text.StringBuilder(input.Length);
            bool prevSpace = false;

            foreach (char c in input)
            {
                if (char.IsWhiteSpace(c))
                {
                    if (!prevSpace)
                    {
                        sb.Append(' ');
                        prevSpace = true;
                    }
                }
                else
                {
                    sb.Append(c);
                    prevSpace = false;
                }
            }

            return sb.ToString().Trim();
        }
        
		// --------------------------------------------------------------------

        #endregion

    }
}
