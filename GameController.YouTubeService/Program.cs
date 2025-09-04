using GameController.YouTubeService.Services;
using GameController.YouTubeService.Worker;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using YouTubeChatService.Services;

const string hubUrl = "http://localhost:5000/"; // Replace with your actual SignalR Hub URL
const string serviceName = "YTVoter"; // Service name for identification on the server

try
{

	

	var host = Host.CreateDefaultBuilder(args)
		.ConfigureServices((hostContext, services) =>
		{
			services.AddSingleton(provider => new HubConnectionBuilder()
				.WithUrl($"{hubUrl}gamehub?name={serviceName}")
				.WithAutomaticReconnect(new[] {
					TimeSpan.FromSeconds(0),
					TimeSpan.FromSeconds(2),
					TimeSpan.FromSeconds(5),
					TimeSpan.FromSeconds(10),
					TimeSpan.FromSeconds(15)
				})
				.Build());
			services.AddSingleton<ISignalRClient, SignalRClient>();
			services.AddHostedService<YouTubeChatWorker>();
			services.AddSingleton<IYouTubeService, YouTubeService>();
		})
		.ConfigureLogging((context, logging) =>
		{
			logging.AddConsole();
			logging.AddDebug();
		})
		.Build();

	await host.RunAsync();



}
catch (Exception ex)
{
	Console.WriteLine($"Application terminated unexpectedly: {ex.Message}");
	Console.WriteLine(ex.StackTrace);
}
