
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


			var app = builder.Build();

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
