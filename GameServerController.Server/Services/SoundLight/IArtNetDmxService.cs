using GameController.Shared.Enums;

namespace GameController.Server.Services.SoundLight
{
    public interface IArtNetDmxService : IAsyncDisposable
    {


        //bool IsRunning { get; }
        //Task StartAsync();

        Task SendDmxAsync(int universe, LightDMXEvent evt);
        // StopAsync();

    }
}
