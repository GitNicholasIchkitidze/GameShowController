using GameController.FBService.Models;
using GameController.Shared.Models.FaceBook;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using StackExchange.Redis;
using System.Text.Json;

namespace GameController.FBService.Pages.Admin
{
	public class VotesMonitorModel : PageModel
	{
		private readonly ILogger<VotesMonitorModel> _logger;
		private readonly IDatabase? _redis;
		private readonly ApplicationDbContext? _db; // если используешь EF Core
		private readonly string _voteStartFlag;
		private readonly IConfiguration _configuration;

		public VotesMonitorModel(ILogger<VotesMonitorModel> logger,
			IConfiguration configuration,
			IConnectionMultiplexer?	connectionMultiplexer = null,			
			ApplicationDbContext? db = null)
		{
			_logger = logger;
			_db = db;
			_redis = connectionMultiplexer?.GetDatabase();
			_voteStartFlag = configuration.GetValue<string>("voteStartFlag") ?? "";

		}

		[BindProperty(SupportsGet = true)]
		public DateTime? FromDate { get; set; }

		[BindProperty(SupportsGet = true)]
		public DateTime? ToDate { get; set; }

		public List<Vote> Votes { get; set; } = new();

		public Dictionary<string, int> Summary { get; set; } = new();

		public async Task<IActionResult> OnGetAsync()
		{
			await LoadVotesAsync();
			return Page();
		}

		public async Task<IActionResult> OnGetDataAsync(DateTime? fromDate, DateTime? toDate)
		{
			FromDate = fromDate;
			ToDate = toDate;
			await LoadVotesAsync();
			return new JsonResult(new
			{
				votes = Votes,
				summary = Summary
			});
		}

		private async Task LoadVotesAsync()
		{
			var from = FromDate?.Date ?? DateTime.UtcNow.Date;
			var to = (ToDate?.Date ?? DateTime.UtcNow.Date).AddDays(1).AddTicks(-1);

			if (_db != null)
			{
				Votes = await _db.FaceBookVotes
					.Where(v => v.Timestamp >= from && v.Timestamp <= to)
					.OrderByDescending(v => v.Timestamp)
					.Take(200)
					.ToListAsync();
			}
			else if (_redis != null)
			{
				var server = _redis.Multiplexer.GetServer(_redis.Multiplexer.GetEndPoints().First());
				var keys = server.Keys(pattern: "vote:*");

				foreach (var key in keys)
				{
					var json = await _redis.StringGetAsync(key);
					if (!json.HasValue) continue;

					var vote = JsonSerializer.Deserialize<Vote>(json!);
					if (vote != null && vote.Timestamp >= from && vote.Timestamp <= to)
						Votes.Add(vote);
				}

				Votes = Votes.OrderByDescending(v => v.Timestamp).Take(200).ToList();
			}

			Summary = Votes
				.GroupBy(v => v.Message.Trim().ToUpperInvariant())
				.ToDictionary(g => g.Key, g => g.Count());
		}

		public async Task<IActionResult> OnGetJsonVotesAsync(DateTime? from, DateTime? to)
		{
			from ??= DateTime.UtcNow.Date;
			to ??= DateTime.UtcNow.AddDays(1);

			var allVotes = await _db.FaceBookVotes
				.Where(v => v.Timestamp >= from && v.Timestamp <= to && v.Message != _voteStartFlag)
				.OrderByDescending(v => v.Timestamp)
				//.Take(200)
				.ToListAsync();

			var analytics = AnalyzeVotes(allVotes);

			return new JsonResult(new
			{
				// დავაბრუნოთ მხოლოდ საჭირო ველები ცხრილისთვის
				votes = allVotes.Select(v => new { userName = v.UserName, message = v.Message, candidatePhone= v.CandidatePhone, timestamp = v.Timestamp }),
				analytics = analytics // დავაბრუნოთ ანალიტიკა JS-ისთვის
			});
		}

		private object AnalyzeVotes(List<Vote> votes)
		{
			var totalVotes = votes.Count;
			var totalUniqueUsers = votes.Select(v => v.UserName).Distinct().Count();

			var groupedVotes = votes
				.GroupBy(v => v.Message?.Trim().ToUpperInvariant() + "\t" + v.CandidatePhone?.Trim().ToUpperInvariant())
				.Select(g => new
				{
					Option = g.Key,
					VoteCount = g.Count(), // 3) ხმის რაოდენობა
					UniqueUsers = g.Select(v => v.UserName).Distinct().Count(), // 4) უნიკალური მომხმარებელი თითოეულ ვარიანტზე

					// 5) ტოპ 3 მომხმარებელი თითოეულ ვარიანტზე
					TopUsers = g
						.GroupBy(v => v.UserName)
						.Select(u => new
						{
							UserName = u.Key,
							UserVoteCount = u.Count()
						})
						.OrderByDescending(u => u.UserVoteCount)
						.Take(3)
				})
				.OrderByDescending(a => a.VoteCount)
				.ToList();

			return new
			{
				TotalVotes = totalVotes, // 1) საერთო ხმების რაოდენობა
				TotalUniqueUsers = totalUniqueUsers, // 2) საერთო უნიკალური მომხმარებლის რაოდენობა
				Options = groupedVotes.Select(g => new
				{
					g.Option,
					g.VoteCount,
					Percentage = totalVotes > 0 ? (double)g.VoteCount / totalVotes * 100 : 0, // 3) პროცენტულობა
					g.UniqueUsers,
					g.TopUsers
				}).ToList()
			};
		}

		public async Task<IActionResult> OnGetJsonVotesAsync_(DateTime? from, DateTime? to)
		{
			from ??= DateTime.UtcNow.Date;
			to ??= DateTime.UtcNow.AddDays(1);

			var votes = await _db.FaceBookVotes
				.Where(v => v.Timestamp >= from && v.Timestamp <= to)
				.OrderByDescending(v => v.Timestamp)
				.Take(200)
				.Select(v => new {
					userName = v.UserName,
					message = v.Message,
					timestamp = v.Timestamp
				})
				.ToListAsync();

			return new JsonResult(votes);
		}
	}
}
