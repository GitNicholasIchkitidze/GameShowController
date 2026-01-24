using Azure;
using GameController.FBService.Extensions;
using GameController.FBService.Models;
using GameController.FBService.Services; // ADDED
using GameController.Shared.Models.FaceBook;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using StackExchange.Redis;
using System.Diagnostics;
using System.Text.Json;

namespace GameController.FBService.Pages.Admin
{

	//[DetailedLog("JsonVotes")]
	public class VotesMonitorModel(ILogger<VotesMonitorModel> logger,
		IConfiguration configuration,
		IMessageQueueService queue,          // ADDED
		IAppMetrics metrics,                 // ADDED
		IConnectionMultiplexer? connectionMultiplexer = null,
		ApplicationDbContext? db = null) : PageModel
	{
		private readonly ILogger<VotesMonitorModel> _logger = logger;
		private readonly IDatabase? _redis = connectionMultiplexer?.GetDatabase();
		private readonly ApplicationDbContext? _db = db; // если используешь EF Core
		private readonly string _voteStartFlag = configuration.GetValue<string>("voteStartFlag") ?? "";
		private readonly IConfiguration _configuration;
		private readonly IMessageQueueService _queue = queue;
		private readonly IAppMetrics _metrics = metrics;

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
					//.Take(50)
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

				Votes = Votes.OrderByDescending(v => v.Timestamp).ToList();
			}

			Summary = Votes
				.GroupBy(v => v.Message.Trim().ToUpperInvariant())
				.ToDictionary(g => g.Key, g => g.Count());
		}

		
		public async Task<IActionResult> OnGetJsonVotesAsync(DateTime? from, DateTime? to, int page = 1, int pageSize = 25)
		{
			//Console.WriteLine($"{DateTime.Now} + OnGetJsonVotesAsync - Line: {new System.Diagnostics.StackTrace(true).GetFrame(0).GetFileLineNumber()}");

			

			from ??= DateTime.UtcNow.Date;
			to ??= DateTime.UtcNow.AddDays(1);

			// ADDED (2025-12): sanitize paging inputs
			if (page < 1) page = 1;
			if (pageSize < 5) pageSize = 5;
			if (pageSize > 200) pageSize = 200;



			// ADDED: base query
			var baseQuery = _db.FaceBookVotes
				.AsNoTracking()
				.Where(v => v.Timestamp >= from && v.Timestamp <= to && v.Message != _voteStartFlag).OrderByDescending(v => v.Timestamp);
			// ADDED: total count for pager
			var totalCount = await baseQuery.CountAsync();
			var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
			if (totalPages < 1) totalPages = 1;

			if (page > totalPages) page = totalPages;

			// ADDED: fetch only the requested page for Last Voters table
			var pageVotes = await baseQuery
				.OrderByDescending(v => v.Timestamp)
				.Skip((page - 1) * pageSize)
				.Take(pageSize)
				.Select(v => new
				{
					userName = v.UserId + "_" + v.UserName,
					message = v.Message,
					candidatePhone = v.CandidatePhone,
					timestamp = v.Timestamp
				})
				.ToListAsync();

			var allVotesForAnalytics = await baseQuery
				.OrderByDescending(v => v.Timestamp)
				.ToListAsync();

			//var analytics = AnalyzeVotes(allVotes);
			var analytics = AnalyzeVotesWithYesAndNo(allVotesForAnalytics);






			//votes = allVotes.Take(100).Select(v => new { userName = v.UserId + "_" + v.UserName, message = v.Message, candidatePhone = v.CandidatePhone, timestamp = v.Timestamp }),
			return new JsonResult(new
			{
				votes = pageVotes,
				pageVotes,
				analytics,
				pagination = new
				{
					page,
					pageSize,
					totalCount,
					totalPages,
					hasPrev = page > 1,
					hasNext = page < totalPages
				}

			});
		}

		// ADDED (2025-12): UI metrics endpoint for the same page
		public IActionResult OnGetJsonMetrics(bool reset = false)
		{
			if (reset)
			{
				_metrics.Reset();
				_queue.ConsumePeakDepth(); 
			}

			return new JsonResult(new
			{
				serverTime = DateTime.UtcNow,
				queue = new
				{
					capacity = _queue.Capacity,
					currentDepth = _queue.CurrentDepth,
					peakDepthSinceLastPoll = _queue.ConsumePeakDepth(),
					droppedCount = _queue.DroppedCount
				},
				counters = _metrics.Snapshot()
			});
		}

		private object AnalyzeVotes(List<Vote> votes)
		{
			var totalVotes = votes.Count;
			var totalUniqueUsers = votes.Select(v => v.UserId + "_" + v.UserName).Distinct().Count();

			var groupedVotes =  votes
				.GroupBy(v => v.Message?.Trim().ToUpperInvariant() + "\t" + v.CandidatePhone?.Trim().ToUpperInvariant())
				.Select(g => new
				{
					Option = g.Key,
					VoteCount = g.Count(), // 3) ხმის რაოდენობა
					UniqueUsers = g.Select(v =>  v.UserId + "_" + v.UserName).Distinct().Count(), // 4) უნიკალური მომხმარებელი თითოეულ ვარიანტზე

					// 5) ტოპ 3 მომხმარებელი თითოეულ ვარიანტზე
					TopUsers = g
						.GroupBy(v => v.UserId + "_" + v.UserName)
						.Select(u => new
						{
							UserName = FormatReadableIdentifier(u.Key),
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


        private object AnalyzeVotesWithYesAndNo_(List<Vote> votes)
        {
            var totalVotes = votes.Count;

            var totalUniqueUsers = votes
                .Select(v => v.UserId + "_" + v.UserName)
                .Distinct()
                .Count();

            var groupedVotes = votes
                .GroupBy(v =>
                    (v.Message?.Split(':')[0].Trim().ToUpperInvariant() ?? "") +
                    "\t" +
                    (v.CandidatePhone?.Trim().ToUpperInvariant() ?? "")
                )
                .Select(g =>
                {
                    var yesCount = g.Count(v => ExtractChoice(v.Message) == "YES");
                    var noCount = g.Count(v => ExtractChoice(v.Message) == "NO");

                    return new
                    {
                        Option = g.Key,
                        VoteCount = g.Count(),                  // როგორც ადრე
                        VoteCountYes = yesCount,               // ➕ ახალი
                        VoteCountNo = noCount,                 // ➕ ახალი
                        UniqueUsers = g
                            .Select(v => v.UserId + "_" + v.UserName)
                            .Distinct()
                            .Count(),
                        TopUsers = g
                            .GroupBy(v => v.UserId + "_" + v.UserName)
                            .Select(u => new
                            {
                                UserName = FormatReadableIdentifier(u.Key),
                                UserVoteCount = u.Count()
                            })
                            .OrderByDescending(u => u.UserVoteCount)
                            .Take(3)
                    };
                })
                .OrderByDescending(a => a.VoteCount)
                .ToList();

            return new
            {
                TotalVotes = totalVotes,
                TotalUniqueUsers = totalUniqueUsers,
                Options = groupedVotes.Select(g => new
                {
                    g.Option,
                    g.VoteCount,
                    g.VoteCountYes, // ➕
                    g.VoteCountNo,  // ➕
                    Percentage = totalVotes > 0
                        ? (double)g.VoteCount / totalVotes * 100
                        : 0,
                    g.UniqueUsers,
                    g.TopUsers
                }).ToList()
            };
        }


        private object AnalyzeVotesWithYesAndNo(List<Vote> votes)
        {
            const int PENALTY = 0;

            var totalUniqueUsers = votes
                .Select(v => v.UserId + "_" + v.UserName)
                .Distinct()
                .Count();

            var groupedVotes = votes
                .GroupBy(v =>
                    (v.Message?.Split(':')[0].Trim().ToUpperInvariant() ?? "") +
                    "\t" +
                    (v.CandidatePhone?.Trim().ToUpperInvariant() ?? "")
                )
                .Select(g =>
                {
                    var yesCount = g.Count(v => ExtractChoice(v.Message) == "YES");
                    var noCount = g.Count(v => ExtractChoice(v.Message) == "NO");
                    var total = g.Count();

                    // 🔴 სანქციის პირობა
                    bool applyPenalty = g.Any(v => v.CandidatePhone == "903300302");

                    int penalizedTotal = applyPenalty
                        ? Math.Max(0, total - PENALTY)
                        : total;

                    int penalizedYes = applyPenalty
                        ? Math.Max(0, yesCount - PENALTY)
                        : yesCount;

                    int penalizedNo = applyPenalty
                        ? Math.Max(0, noCount - PENALTY)
                        : noCount;

                    return new
                    {
                        Option = g.Key,

                        VoteCount = penalizedTotal,
                        VoteCountYes = penalizedYes,
                        VoteCountNo = penalizedNo,

                        UniqueUsers = g
                            .Select(v => v.UserId + "_" + v.UserName)
                            .Distinct()
                            .Count(),

                        TopUsers = g
                            .GroupBy(v => v.UserId + "_" + v.UserName)
                            .Select(u => new
                            {
                                UserName = FormatReadableIdentifier(u.Key),
                                UserVoteCount = applyPenalty
                                    ? Math.Max(0, u.Count() - PENALTY)
                                    : u.Count()
                            })
                            .OrderByDescending(u => u.UserVoteCount)
                            .Take(3)
                    };
                })
                .OrderByDescending(a => a.VoteCount)
                .ToList();

            var penalizedTotalVotes = groupedVotes.Sum(g => g.VoteCount);

            return new
            {
                TotalVotes = penalizedTotalVotes,
                TotalUniqueUsers = totalUniqueUsers,

                Options = groupedVotes.Select(g => new
                {
                    g.Option,
                    g.VoteCount,
                    g.VoteCountYes,
                    g.VoteCountNo,

                    Percentage = penalizedTotalVotes > 0
                        ? (double)g.VoteCount / penalizedTotalVotes * 100
                        : 0,

                    g.UniqueUsers,
                    g.TopUsers
                }).ToList()
            };
        }




        private static string ExtractChoice(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
                return null;

            var parts = message.Split(':', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 2)
                return null;

            return parts[1].Trim().ToUpperInvariant(); // YES / NO
        }

        private static (string Candidate, string Choice) ParseVote(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
                return (null, null);

            var parts = message.Split(':', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 2)
                return (null, null);

            return (
                parts[0].Trim(),
                parts[1].Trim().ToUpperInvariant() // YES / NO
            );
        }

        public string FormatReadableIdentifier(string fullIdentifier)
        {
            if (string.IsNullOrEmpty(fullIdentifier))
            {
                return fullIdentifier;
            }

            string numberPart;
            string namePart = null;

            // ვპოულობთ პირველ ქვედა ტირეს, რომ სწორად დავყოთ
            // იმ შემთხვევაშიც, თუ სახელიც შეიცავს ქვედა ტირეს.
            int underscoreIndex = fullIdentifier.IndexOf('_');

            if (underscoreIndex != -1)
            {
                // დავყავით ორ ნაწილად: რიცხვი და სახელი
                numberPart = fullIdentifier.Substring(0, underscoreIndex);
                namePart = fullIdentifier.Substring(underscoreIndex + 1); // ვიღებთ ყველაფერს _-ის შემდეგ
            }
            else
            {
                // თუ ქვედა ტირე არ არის, მთლიან სტრიქონს ვთვლით რიცხვად
                numberPart = fullIdentifier;
            }

            string truncatedNumber;
            int firstChars = 2; // რამდენი სიმბოლო დავტოვოთ თავში
            int lastChars = 4;  // რამდენი სიმბოლო დავტოვოთ ბოლოში

            // ვამოწმებთ, რომ რიცხვი საკმარისად გრძელია შესამოკლებლად
            // (2 + 4 = 6). თუ 6-ზე ნაკლებია, შემოკლებას აზრი არ აქვს.
            if (numberPart.Length > firstChars + lastChars)
            {
                string first = numberPart.Substring(0, firstChars);
                string last = numberPart.Substring(numberPart.Length - lastChars);
                truncatedNumber = $"{first}....{last}";
            }
            else
            {
                // რიცხვი ძალიან მოკლეა, ვტოვებთ როგორც არის
                truncatedNumber = numberPart;
            }

            // ვაბრუნებთ შედეგს. თუ სახელი არ გვქონდა, დაბრუნდება მხოლოდ შემოკლებული რიცხვი.
            return (namePart != null)
                ? $"{truncatedNumber}_{namePart}"
                : truncatedNumber;
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
