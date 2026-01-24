// Heplers/RedisTtlProvider.cs
using GameController.FBService.Models;
using Microsoft.Extensions.Options;

namespace GameController.FBService.Heplers
{
	public interface IRedisTtlProvider
	{
		TimeSpan VoteCooldown { get; }
		TimeSpan NeedForVote { get; }
		TimeSpan Idempotency { get; }
	}

	public class RedisTtlProvider : IRedisTtlProvider
	{
		private readonly VotingOptions _voting;

		public RedisTtlProvider(IOptions<VotingOptions> voting)
		{
			_voting = voting.Value;
		}

		public TimeSpan VoteCooldown => TimeSpan.FromMinutes(_voting.CooldownMinutes);
		public TimeSpan NeedForVote => TimeSpan.FromMinutes(_voting.NeedForVoteMinutes);
		public TimeSpan Idempotency => TimeSpan.FromHours(24);
	}
}
