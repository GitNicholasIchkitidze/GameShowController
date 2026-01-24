using System.Collections.Concurrent;
using System.Threading;

namespace GameController.FBService.Services
{
	// ADDED (2025-12): Thread-safe counters (Interlocked).
	public class AppMetrics : IAppMetrics
	{
		private long _ingress;
		private long _enqueueOk;
		private long _enqueueDropped;
		private long _dequeued;
		private long _processedOk;
		private long _processedFailed;
		private long _garbageMessages;
		private long _notInTimeUserMessages;
		private long _recsSavedInDB;
		private long _errorDBWhileSave;
        private long _inFlight;

		public long IngressCount => Interlocked.Read(ref _ingress);
		public long EnqueueOk => Interlocked.Read(ref _enqueueOk);
		public long EnqueueDropped => Interlocked.Read(ref _enqueueDropped);
		public long Dequeued => Interlocked.Read(ref _dequeued);
		public long ProcessedOk => Interlocked.Read(ref _processedOk);
		public long ProcessedFailed => Interlocked.Read(ref _processedFailed);
		public long GarbageMessages => Interlocked.Read(ref _garbageMessages);
		public long NotInTimeUserMessages => Interlocked.Read(ref _notInTimeUserMessages);
		public long RecsSavedInDB => Interlocked.Read(ref _recsSavedInDB);
        public long ErrorDBWhileSave => Interlocked.Read(ref _errorDBWhileSave);
        

        public void IncIngress() => Interlocked.Increment(ref _ingress);
		public void IncEnqueueOk() => Interlocked.Increment(ref _enqueueOk);
		public void IncEnqueueDropped() => Interlocked.Increment(ref _enqueueDropped);
		public void IncDequeued() => Interlocked.Increment(ref _dequeued);
		public void IncProcessedOk() => Interlocked.Increment(ref _processedOk);
		public void IncProcessedFailed() => Interlocked.Increment(ref _processedFailed);
		public void IncGarbageMessages() => Interlocked.Increment(ref _garbageMessages);
		public void IncNotInTimeUserMessages() => Interlocked.Increment(ref _notInTimeUserMessages);
		public void IncRecsSavedInDB() => Interlocked.Increment(ref _recsSavedInDB);
        public void IncErrorDBWhileSave() => Interlocked.Increment(ref _errorDBWhileSave);

        public long InFlight => Interlocked.Read(ref _inFlight);



		private sealed class LongCounter
		{
			public long Value;
		}

		private readonly ConcurrentDictionary<string, LongCounter> _savedInDbByCandidate =
			new(StringComparer.OrdinalIgnoreCase);

		// Stores counters per "candidate||flag"
		private readonly ConcurrentDictionary<string, LongCounter> _candidateFlagCounters =
			new(StringComparer.OrdinalIgnoreCase);


		// -----------------------------------------
		// RiskScore bands (per candidate) // ახალი დამატებული
		// Normal: 0-29
		// Suspicious: 30-59
		// VerySuspicious: 60-99
		// Blocked: >= blockScore (default 100)
		// IMPORTANT: "Blocked" here is based on RiskScore threshold even if BlockEnabled=false.
		// -----------------------------------------
		private readonly ConcurrentDictionary<string, LongCounter> _bandNormalByCandidate =
            new(StringComparer.OrdinalIgnoreCase); // ახალი დამატებული

        private readonly ConcurrentDictionary<string, LongCounter> _bandSuspiciousByCandidate =
            new(StringComparer.OrdinalIgnoreCase); // ახალი დამატებული

        private readonly ConcurrentDictionary<string, LongCounter> _bandVerySuspiciousByCandidate =
            new(StringComparer.OrdinalIgnoreCase); // ახალი დამატებული

        private readonly ConcurrentDictionary<string, LongCounter> _bandBlockedByCandidate =
            new(StringComparer.OrdinalIgnoreCase); // ახალი დამატებული

        private readonly ConcurrentDictionary<string, LongCounter> _bandAskedConfirmationByCandidate =
            new(StringComparer.OrdinalIgnoreCase); // ახალი დამატებული


        public void IncInFlight() => Interlocked.Increment(ref _inFlight);

		public void DecInFlight()
		{
			var v = Interlocked.Decrement(ref _inFlight);
			if (v < 0) Interlocked.Exchange(ref _inFlight, 0); // safety
		}


		// -----------------------------
		// ✅ NEW: per-candidate totals
		// -----------------------------
		public void IncSavedInDBByCandidate(string candidateName)
		{
			candidateName = NormalizeCandidate(candidateName);
			var c = _savedInDbByCandidate.GetOrAdd(candidateName, _ => new LongCounter());
			Interlocked.Increment(ref c.Value);
		}

		// -----------------------------
		// ✅ NEW: per-candidate per-flag
		// -----------------------------
		public void IncCandidateFlag(string candidateName, string flag)
		{
			candidateName = NormalizeCandidate(candidateName);
			flag = NormalizeFlag(flag);

			var key = ComposeCandidateFlagKey(candidateName, flag);
			var c = _candidateFlagCounters.GetOrAdd(key, _ => new LongCounter());
			Interlocked.Increment(ref c.Value);
		}

        public void IncCandidateFlags(string candidateName, IEnumerable<string>? flags)
        {
            if (flags == null) return;
            foreach (var f in flags)
                IncCandidateFlag(candidateName, f);
        }




        // Optional convenience:
        // These just map to IncCandidateFlag with reserved flag names.
        public void IncSuspiciousByCandidate(string candidateName)
			=> IncCandidateFlag(candidateName, "SUSPICIOUS");

		public void IncBlockedByCandidate(string candidateName)
			=> IncCandidateFlag(candidateName, "BLOCKED");

        public void IncAskedConfirmationByCandidate(string candidateName)
            => IncCandidateFlag(candidateName, "ASKEDCONFIRMATION");

        // -----------------------------------------
        // RiskScore band increment (NEW) // ახალი დამატებული
        // -----------------------------------------
        public void IncRiskBandByCandidate(string candidateName, int riskScore, int confirmationScore = 100, int blockScore = 160)
        {
            candidateName = NormalizeCandidate(candidateName);
            if (blockScore < 1) blockScore = 160;

            // Blocked band is based on RiskScore threshold (even if BlockEnabled=false)
            if (riskScore >= blockScore)
            {
                IncBand(_bandBlockedByCandidate, candidateName);
                return;
            }

            if (riskScore >= confirmationScore)
            {
                IncBand(_bandAskedConfirmationByCandidate, candidateName);
                return;
            }


            if (riskScore >= 60)
            {
                IncBand(_bandVerySuspiciousByCandidate, candidateName);
                return;
            }

            if (riskScore >= 30)
            {
                IncBand(_bandSuspiciousByCandidate, candidateName);
                return;
            }

            IncBand(_bandNormalByCandidate, candidateName);
        }

        // Optional helper: increment many flags at once // ახალი დამატებული


        // Small helper to increment per-candidate band dict // ახალი დამატებული
        private static void IncBand(ConcurrentDictionary<string, LongCounter> dict, string candidateName)
        {
            var c = dict.GetOrAdd(candidateName, _ => new LongCounter());
            Interlocked.Increment(ref c.Value);
        }


        private static string NormalizeCandidate(string candidateName)
			=> string.IsNullOrWhiteSpace(candidateName) ? "UNKNOWN" : candidateName.Trim();

		private static string NormalizeFlag(string flag)
			=> string.IsNullOrWhiteSpace(flag) ? "UNKNOWN_FLAG" : flag.Trim().ToUpperInvariant();

		private static string ComposeCandidateFlagKey(string candidate, string flag)
			=> $"{candidate}||{flag}";

		private static void SplitCandidateFlagKey(string key, out string candidate, out string flag)
		{
			var idx = key.IndexOf("||");
			if (idx < 0)
			{
				candidate = key;
				flag = "UNKNOWN_FLAG";
				return;
			}

			candidate = key.Substring(0, idx);
			flag = key.Substring(idx + 2);
		}



		public object Snapshot()
		{

			var savedByCandidate = new Dictionary<string, long>(_savedInDbByCandidate.Count, StringComparer.OrdinalIgnoreCase);
			foreach (var kv in _savedInDbByCandidate)
				savedByCandidate[kv.Key] = Interlocked.Read(ref kv.Value.Value);

			// Convert "candidate||flag" -> nested dict { candidate: { flag: count } }
			var candidateFlags = new Dictionary<string, Dictionary<string, long>>(StringComparer.OrdinalIgnoreCase);
			foreach (var kv in _candidateFlagCounters)
			{
				var count = Interlocked.Read(ref kv.Value.Value);
				if (count <= 0) continue;

				SplitCandidateFlagKey(kv.Key, out var candidate, out var flag);

				if (!candidateFlags.TryGetValue(candidate, out var flagsDict))
				{
					flagsDict = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
					candidateFlags[candidate] = flagsDict;
				}

				flagsDict[flag] = count;
			}


            // RiskScore bands snapshot (nested per candidate) // ახალი დამატებული
            var candidateBands = new Dictionary<string, Dictionary<string, long>>(StringComparer.OrdinalIgnoreCase); // ახალი დამატებული
            AddBandSnapshot(candidateBands, _bandNormalByCandidate, "NORMAL"); // ახალი დამატებული
            AddBandSnapshot(candidateBands, _bandSuspiciousByCandidate, "SUSPICIOUS_30_59"); // ახალი დამატებული
            AddBandSnapshot(candidateBands, _bandVerySuspiciousByCandidate, "VERY_SUSPICIOUS_60_99"); // ახალი დამატებული
            AddBandSnapshot(candidateBands, _bandBlockedByCandidate, "BLOCKED_160_PLUS"); // ახალი დამატებული
            AddBandSnapshot(candidateBands, _bandAskedConfirmationByCandidate, "ASKEDCONFIRMATION_100_PLUS"); // ახალი დამატებული

            return new
			{
				IngressCount,
				EnqueueOk,
				EnqueueDropped,
				Dequeued,
				ProcessedOk,
				ProcessedFailed,
				GarbageMessages,
				NotInTimeUserMessages,
				RecsSavedInDB,
                ErrorDBWhileSave,

                InFlight,
				// ✅ NEW
				SavedInDBByCandidate = savedByCandidate,

				// ✅ NEW: per-candidate per-flag metrics
				// Example:
				// { "CandidateA": { "SUSPICIOUS": 12, "COOLDOWN_HUGGING": 5, "COOLDOWN_RHYTHM_BAND": 4 } }
				CandidateFlagMetrics = candidateFlags,

                // ✅ NEW: per-candidate RiskScore band metrics // ახალი დამატებული
                // Example:
                // { "CandidateA": { "NORMAL": 100, "SUSPICIOUS_30_59": 20, "VERY_SUSPICIOUS_60_99": 10, "BLOCKED_100_PLUS": 5 } }
                CandidateRiskBands = candidateBands // ახალი დამატებული
            };
		}

        private static void AddBandSnapshot(
                    Dictionary<string, Dictionary<string, long>> target,
                    ConcurrentDictionary<string, LongCounter> source,
                    string bandName)
        {
            foreach (var kv in source)
            {
                var count = Interlocked.Read(ref kv.Value.Value);
                if (count <= 0) continue;

                if (!target.TryGetValue(kv.Key, out var bands))
                {
                    bands = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
                    target[kv.Key] = bands;
                }

                bands[bandName] = count;
            }
        }

        public object Reset()
		{

			Interlocked.Exchange(ref _ingress, 0);
			Interlocked.Exchange(ref _enqueueOk, 0);
			Interlocked.Exchange(ref _enqueueDropped, 0);
			Interlocked.Exchange(ref _dequeued, 0);
			Interlocked.Exchange(ref _processedOk, 0);
			Interlocked.Exchange(ref _processedFailed, 0);
			Interlocked.Exchange(ref _garbageMessages, 0);
			Interlocked.Exchange(ref _notInTimeUserMessages, 0);
			Interlocked.Exchange(ref _recsSavedInDB, 0);
            Interlocked.Exchange(ref _errorDBWhileSave, 0);
            


            _savedInDbByCandidate.Clear();
			_candidateFlagCounters.Clear();

            _bandNormalByCandidate.Clear(); // ახალი დამატებული
            _bandSuspiciousByCandidate.Clear(); // ახალი დამატებული
            _bandVerySuspiciousByCandidate.Clear(); // ახალი დამატებული
            _bandBlockedByCandidate.Clear(); // ახალი დამატებული
			_bandAskedConfirmationByCandidate.Clear(); // ახალი დამატებული

            return Snapshot();
		}


	}
}
