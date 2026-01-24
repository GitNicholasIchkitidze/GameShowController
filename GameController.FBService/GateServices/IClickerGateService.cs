namespace GameController.FBService.GateServices
{
    public interface IClickerGateService
    {
        /// <summary>
        /// Called for every incoming vote postback payload (NOT for CAP answers).
        /// Returns:
        /// - Allow = true => continue vote pipeline
        /// - Allow = false => vote must be blocked (challenge pending/sent)
        /// </summary>
        Task<GateDecision> CheckVoteGateAsync(string psid, DateTime utcNow, string votePayload);

        /// <summary>
        /// Called when CAP:* answer postback arrives.
        /// Returns true if solved successfully and user is verified.
        /// </summary>
        Task<bool> TrySolveChallengeAsync(string psid, DateTime utcNow, string capPayload);
    }

    public sealed class GateDecision
    {
        public bool AllowVote { get; init; }

        // if not null => you should send this challenge message to user
        public ChallengeToSend? Challenge { get; init; }

        // optional info for logging/metrics
        public string? Reason { get; init; }

        public static GateDecision Allow(string? reason = null) => new GateDecision { AllowVote = true, Reason = reason };
        public static GateDecision Block(string reason, ChallengeToSend? challenge = null) => new GateDecision { AllowVote = false, Reason = reason, Challenge = challenge };
    }

    public sealed class ChallengeToSend
    {
        public required string Text { get; init; }

        /// <summary>
        /// Title shown on button, and payload that will come back as postback.
        /// </summary>
        public required List<(string Title, string Payload)> Buttons { get; init; }
    }
}
