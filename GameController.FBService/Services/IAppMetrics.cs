namespace GameController.FBService.Services
{

	public interface IAppMetrics
	{
		long IngressCount { get; }
		long EnqueueOk { get; }
		long EnqueueDropped { get; }
		long Dequeued { get; }
		long ProcessedOk { get; }
		long ProcessedFailed { get; }
		long GarbageMessages { get; }
		long NotInTimeUserMessages { get; }
		long RecsSavedInDB { get; }
		long ErrorDBWhileSave { get; }
			long InFlight { get; }

			void IncInFlight();
			void DecInFlight();
			void IncIngress();
			void IncEnqueueOk();
			void IncEnqueueDropped();
			void IncDequeued();
			void IncProcessedOk();
			void IncProcessedFailed();
			void IncGarbageMessages();
			void IncNotInTimeUserMessages();
			void IncRecsSavedInDB();
			void IncErrorDBWhileSave();

		void IncSavedInDBByCandidate(string candidateName);

		// ✅ NEW: per-candidate per-flag counters
		// Example flags: COOLDOWN_HUGGING, AFTER_COOLDOWN_WINDOW, COOLDOWN_RHYTHM_BAND, MANY_USERS_SAME_SECOND, etc.
		void IncCandidateFlag(string candidateName, string flag);
		void IncCandidateFlags(string candidateName, IEnumerable<string>? flags);
        void IncSuspiciousByCandidate(string candidateName);
		void IncBlockedByCandidate(string candidateName);
        void IncAskedConfirmationByCandidate(string candidateName);


        /// <summary>
        /// Bucketize by RiskScore:
        /// Normal: 0-29, Suspicious: 30-59, VerySuspicious: 60-99, Blocked: >= blockScore.
        /// NOTE: "Blocked" is based on RiskScore threshold even if BlockEnabled=false.
        /// </summary>
        void IncRiskBandByCandidate(string candidate, int riskScore, int confirmationScore = 100, int blockScore = 160);

        /// <summary>
        /// Increment per-flag counters for a candidate.
        /// </summary>
        //void IncFlagsByCandidate(string candidate, IEnumerable<string>? flags);


        object Snapshot();
		object Reset();
	}

}
