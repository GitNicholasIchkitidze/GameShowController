// Heplers/RedisKeys.cs
using GameController.FBService.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace GameController.FBService.Heplers
{
	/// <summary>
	/// Redis key registry (single source of truth).
	///
	/// IMPORTANT:
	/// - Cache.* keys are used with IDistributedCache -> InstanceName ("GameController:") will be auto-prefixed.
	/// - Native.* keys are used with IDatabase (StackExchange.Redis) -> we include Root prefix here ourselves.
	/// </summary>
	public static class RedisKeys
	{
        

        // Must match Program.cs: options.InstanceName = "GameController:"
        public const string Root = "GameController:";
        private static string Session = "L7:";


        public static class Vars
		{
			// RedisGlobalVarsKeeper currently concatenates prefix + key (no separator),
			// so we keep trailing ":" to avoid "Varsfb_listening_active"
			public const string Prefix = Root + "Vars:";

        }

		public static class FB
		{
			public static class Cache
			{
				// used by IDistributedCache (InstanceName auto adds GameController:)
				public static string UserName(string recipientId, string userId)
					=> $"FB:Session:{Session}:Recipient:{recipientId}:User:{userId}:Name";
			}



			public static class Native
			{
				// used by IDatabase directly (Root included)
				public static string IdempotencyMessage(string messageId)
					=> $"{Root}FB:Session:{Session}:Idem:Msg:{messageId}";

				public static string VoteLock(string recipientId, string userId)
					=> $"{Root}FB:Session:{Session}:VoteLock:R:{recipientId}:U:{userId}";

				public static string NeedForVoteLock(string recipientId, string userId)
					=> $"{Root}FB:Session:{Session}:NeedForVote:R:{recipientId}:U:{userId}";


                /// <summary>
                /// Short user-level processing lock key (anti-parallel).
                /// </summary>
                public static string BuildProcessLockKey(string recipientId, string senderId)
                    => $"FB:Session:{Session}:Recipient:{recipientId}:User:{senderId}:VoteProcessLock";

                /// <summary>
                /// Pending confirmation key (120 sec TTL).
                /// </summary>
                public static string BuildPendingConfirmKey(string recipientId, string senderId)
                    => $"FB:Session:{Session}:Recipient:{recipientId}:User:{senderId}:PendingConfirm";

                public static string GlobalRateLimit(string rawKeyFromConfig)
				{
					// If config already starts with Root, keep it
					if (!string.IsNullOrWhiteSpace(rawKeyFromConfig) &&
						rawKeyFromConfig.StartsWith(Root, System.StringComparison.OrdinalIgnoreCase))
						return rawKeyFromConfig;

					// Otherwise place it under Root namespace
					return $"{Root}{rawKeyFromConfig}";
				}
			}

			public static class Clicker
			{
				public static string UsersBySecond(string recipientId, long unixSecond)
					=> $"{Root}FB:Session:{Session}:Clicker:R:{recipientId}:Sec:{unixSecond}:Users";

				public static string UserRisk(string recipientId, string userId)
					=> $"{Root}FB:Session:{Session}:Clicker:R:{recipientId}:U:{userId}:Risk";

				public static string Suspects(string recipientId)
					=> $"{Root}FB:Session:{Session}:Clicker:R:{recipientId}:Suspects";

				public static string UserLastVoteUnix(string recipientId, string userId)
					=> $"{Root}FB:Session:{Session}:Clicker:R:{recipientId}:U:{userId}:LastVoteUnix";

				public static string UserNearCooldownCount(string recipientId, string userId)
					=> $"{Root}FB:Session:{Session}:Clicker:R:{recipientId}:U:{userId}:NearCooldownCount";

				public static string AfterCooldownUsersBucket(string recipientId, long bucketStartUnix, int windowSeconds)
					=> $"{Root}FB:Session:{Session}:Clicker:R:{recipientId}:AfterCooldown:Win:{windowSeconds}:T:{bucketStartUnix}:Users";

				public static string UserRhythmState(string recipientId, string userId)
					=> $"{Root}FB:Session:{Session}:Clicker:R:{recipientId}:U:{userId}:Rhythm";

			}
		}
	}
}
