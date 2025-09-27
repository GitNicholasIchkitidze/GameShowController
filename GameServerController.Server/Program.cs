using GameController.Server.Hubs;
using GameController.Server.Services;
using GameController.Server.Services.SoundLight;
using GameController.Server.VotingManagers;
using GameController.Server.VotingServices;
using GameController.Shared.Models;
using Microsoft.AspNetCore.SignalR;
// ახალი ჩამატებული
using Microsoft.Extensions.FileProviders;
using System.Text;

var builder = WebApplication.CreateBuilder(args);



try
{

    Console.OutputEncoding = Encoding.UTF8;
    Console.InputEncoding = Encoding.UTF8;
}
catch (Exception ex)
{

    Console.WriteLine($"Encoding setup error: {ex.Message}");

}

// Add services to the container.
builder.Services.AddSignalR();

builder.Services.AddHttpClient<IYouTubeChatService, YouTubeChatService>();


builder.Services.AddSingleton<IMidiLightingService, MidiLightingService>();

//builder.Services.AddSingleton<MidiLightingService>();
//builder.Services.AddSingleton<IMidiLightingService, MidiLightingService>(x => x.GetRequiredService<MidiLightingService>());
//builder.Services.AddHostedService(x => x.GetRequiredService<MidiLightingService>());

builder.Services.AddSingleton<IGameService, GameService>();
builder.Services.AddSingleton<IQuestionService, QuestionService>();


builder.Services.AddSingleton<IYTOAuthTokenService, YTOAuthTokenService>();
builder.Services.AddSingleton<IYTAudienceVoteManager, YTAudienceVoteManager>();
builder.Services.AddSingleton<IYouTubeDataCollectorService, YouTubeDataCollectorService>();

//builder.Services.AddHostedService<YouTubeDataCollectorService>();











// Configure logging
builder.Services.AddLogging(options => options.AddConsole());
builder.Logging.ClearProviders(); // Clear default providers
builder.Logging.AddConsole(); // Add console logging
builder.Logging.AddDebug(); // Optional: Add debug output logging

builder.Services.AddSingleton<CasparCGWsService>();


builder.Services.AddSingleton<ICasparService>(provider =>
{
    // host და port შეგიძლია appsettings.json-დან ამოიღო
    var host = builder.Configuration["CG:ServerIp"] ?? "127.0.0.1";
    var port = int.Parse(builder.Configuration["CG:ServerPort"] ?? "5250");
    try
    {

        var logger = provider.GetRequiredService<ILogger<CasparService>>();

        var caspar = new CasparService(logger, host, port);
        return caspar is null ? throw new ArgumentException("CasparCG server IP and port must be configured.") : (ICasparService)caspar;
    }
    catch (Exception)
    {

        throw new ArgumentException("CasparCG server Error.");
    }
});


builder.Services.Configure<MidiSettingsModels>(
    builder.Configuration.GetSection("MidiSettings"));

var midiDeviceName = builder.Configuration.GetSection("MidiSettings:DeviceName").Value;
Console.WriteLine($"MIDI Device Name from Config: {midiDeviceName}");




var UseDmx = builder.Configuration.GetValue<bool>("Lighting:UseDMX", false);
if (UseDmx)
{
    builder.Services.AddSingleton<IArtNetDmxService>(sp =>
    {
        // IP მისამართის აღება appsettings.json-დან
        var dmxIp = builder.Configuration["Lighting:DMXIpAddress"];
        var dmxPort = builder.Configuration.GetValue<int>("Lighting:UsePort", 6454);
        var logger = sp.GetRequiredService<ILogger<ArtNetDmxService>>();
        return new ArtNetDmxService(logger, dmxIp, dmxPort);
    });
}




builder.Services.AddSignalR(options =>
{
    options.AddFilter<LogHubFilter>();
    //options.
});


builder.Services.AddSignalR()
    .AddJsonProtocol(options => {
        options.PayloadSerializerOptions.PropertyNamingPolicy = null;
    });


// Register the filter itself
builder.Services.AddTransient<LogHubFilter>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    _ = app.UseDeveloperExceptionPage();
}

app.UseDefaultFiles();
app.UseStaticFiles();
//app.Urls.Add("http://0.0.0.0:7172");



var templatesPath = Path.Combine(builder.Environment.ContentRootPath, "CasparCG", "Templates");
if (Directory.Exists(templatesPath))
{
    _ = app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new PhysicalFileProvider(templatesPath),
        RequestPath = "/templates" // The URL path to access the templates
    });
}


// Enable WebSockets
app.UseWebSockets();

// Set up the WebSocket endpoint for CasparCG

app.Use(async (context, next) =>
{
    Console.WriteLine($"{Environment.NewLine}{DateTime.Now}  Incoming request path: {context.Request.Path}");

    if (context.Request.Path == "/ws-casparcg")
    {
        if (context.WebSockets.IsWebSocketRequest)
        {
            var wsService = context.RequestServices.GetRequiredService<CasparCGWsService>();
            var webSocket = await context.WebSockets.AcceptWebSocketAsync();

            // Add the new connection to our service
            wsService.AddConnection(webSocket);

            // Wait until the connection is closed
            await Task.Delay(-1);
        }
        else
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
        }
    }
    else
    {
        await next();
    }
});



// Map SignalR Hub
app.MapHub<GameHub>("/gamehub");

app.Run();