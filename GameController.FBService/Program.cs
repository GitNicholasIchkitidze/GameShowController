
using GameController.FBService.Services;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Client;

namespace GameController.FBService
{
	public class Program
	{
		public static void Main(string[] args)
		{
			var builder = WebApplication.CreateBuilder(args);

			// Add services to the container.

			builder.Services.AddControllers();
			// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
			builder.Services.AddEndpointsApiExplorer();
			builder.Services.AddSwaggerGen();
			builder.Services.AddLogging();
			builder.Services.AddHttpClient();
			builder.Services.AddMemoryCache();

			


			builder.Services.AddDbContext<ApplicationDbContext>(options =>
				options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));


			builder.Services.AddScoped<IRateLimitingService, RateLimitingService>();

			builder.Services.AddSingleton<IMessageQueueService, MessageQueueService>(); // Singleton is often appropriate for queue clients
			builder.Services.AddScoped<IWebhookProcessorService, WebhookProcessorService>();
			builder.Services.AddHostedService<QueueWorkerService>();


			builder.Services.AddStackExchangeRedisCache(options =>
			{
				options.Configuration = builder.Configuration.GetConnectionString("RedisConnection");
				options.InstanceName = "GameController:"; // Prefix for keys
			});

			//builder.Services.AddSingleton<StackExchange.Redis.IConnectionMultiplexer>(sp =>
			//	StackExchange.Redis.ConnectionMultiplexer.Connect(
			//		builder.Configuration.GetConnectionString("RedisConnection")
			//	)
			//);

			//StackExchange.Redis: Connection Multiplexer-ის რეგისტრაცია
			builder.Services.AddSingleton<StackExchange.Redis.IConnectionMultiplexer>(sp =>
			{
				var configString = builder.Configuration.GetConnectionString("RedisConnection") ?? "localhost:6379,ssl=False,abortConnect=False";
				var configuration = StackExchange.Redis.ConfigurationOptions.Parse(configString);

				// CRITICAL: AbortOnConnectFail = false საშუალებას გვაძლევს, ხელით შევამოწმოთ კავშირი გაშვებისას.
				configuration.AbortOnConnectFail = false;

				return StackExchange.Redis.ConnectionMultiplexer.Connect(configuration);
			});


			// 7. NEW: Register a specialized service for handling Redis locks and caching
			builder.Services.AddSingleton<ICacheService, RedisCacheService>();
			builder.Services.AddSingleton<IDempotencyService, DempotencyService>();









			// SignalR-ის HubConnectionBuilder-ის კონფიგურაცია და რეგისტრაცია Singleton-ად
			builder.Services.AddSingleton(sp =>
			{
				var hubUrl = sp.GetRequiredService<IConfiguration>().GetSection("ServerSettings:BaseUrl").Value;
				var serviceName = sp.GetRequiredService<IConfiguration>().GetSection("ServerSettings:ServiceName").Value;

				return new HubConnectionBuilder()
					.WithUrl($"{hubUrl}gamehub?name={serviceName}")
					.WithAutomaticReconnect(new[] {
							TimeSpan.FromSeconds(0),
							TimeSpan.FromSeconds(2),
							TimeSpan.FromSeconds(5),
							TimeSpan.FromSeconds(10),
							TimeSpan.FromSeconds(15)
					})
					.Build();
			});

			builder.Services.AddSingleton<ISignalRClient, SignalRClient>();
			builder.Services.AddRazorPages();


			var app = builder.Build();
			app.MapRazorPages();


			// ------------------------------------
			// 🛑 NEW: Redis Connection Health Check
			// ------------------------------------
			try
			{
				// 1. Dependency-ების მიღება Service Provider-იდან
				var multiplexer = app.Services.GetRequiredService<StackExchange.Redis.IConnectionMultiplexer>();
				var logger = app.Services.GetRequiredService<ILogger<Program>>();

				// 2. კავშირის სტატუსის იძულებითი შემოწმება
				if (!multiplexer.IsConnected)
				{
					// ვცდილობთ ნებისმიერ endpoint-თან დაკავშირებას
					var endpoint = multiplexer.GetEndPoints().FirstOrDefault();
					if (endpoint == null || !multiplexer.GetServer(endpoint).IsConnected)
					{
						throw new Exception("Redis connection is not available. Please check your Redis server and 'RedisConnection' string.");
					}
				}

				logger.LogInformation("✅ Redis connection established successfully.");
			}
			catch (Exception ex)
			{
				// ფატალური შეცდომის დალოგვა და აპლიკაციის გათიშვა
				app.Logger.LogCritical(ex, "❌ FATAL: Application startup failed due to missing or unhealthy Redis connection. FaceBook Voting will not work.");

				// აპლიკაციის გათიშვა
				//Environment.Exit(1);
			}


			// Configure the HTTP request pipeline.
			if (app.Environment.IsDevelopment())
			{
				app.UseSwagger();
				app.UseSwaggerUI();
			}

			app.UseHttpsRedirection();

			app.UseAuthorization();
			app.UseStaticFiles();


			app.MapControllers();

			var clientService = app.Services.GetRequiredService<ISignalRClient>();
			clientService.ConnectWithRetryAsync();

			app.Run();
		}
	}
}
