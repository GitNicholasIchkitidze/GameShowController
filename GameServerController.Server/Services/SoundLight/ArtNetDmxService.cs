
using ArtNetSharp;
using ArtNetSharp.Communication;
using GameController.Shared.Enums;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;


namespace GameController.Server.Services.SoundLight
{
    public class ArtNetDmxService : IArtNetDmxService
    {
        private readonly UdpClient _udp;
        private readonly IPEndPoint _remote;
        private readonly SemaphoreSlim _lock = new SemaphoreSlim(1, 1);
        private readonly ILogger<ArtNetDmxService> _logger;
        private bool _disposed;

        public ArtNetDmxService(ILogger<ArtNetDmxService> logger, string targetIp, int port = 6454)
        {
            _logger = logger;

            if (string.IsNullOrWhiteSpace(targetIp))
                targetIp = "255.255.255.255"; // Default broadcast

            if (!IPAddress.TryParse(targetIp, out var ip))
                throw new ArgumentException("Invalid IP address", nameof(targetIp));

            _remote = new IPEndPoint(ip, port);
            _udp = new UdpClient();

            if (ip.Equals(IPAddress.Broadcast))
                _udp.EnableBroadcast = true;

            _udp.Connect(_remote);

            _logger.LogInformation($" DMX Controller Loaded '{targetIp}' {port}");

            _disposed = false;


        }



        public async Task SendDmxAsync(int universe, LightDMXEvent evt)
        {
            await _lock.WaitAsync();
            try
            {
                if (_disposed)
                    throw new ObjectDisposedException(nameof(ArtNetDmxService));

                var dmx = new byte[512];

                if (evt == LightDMXEvent.everythingOff)
                {
                    // ყველა არხი = 0
                    Array.Clear(dmx, 0, dmx.Length);
                }
                else
                {
                    // ჩართავს შესაბამის არხს (enum value = channel number)
                    int channel = (int)evt;
                    if (channel < 1 || channel > 512)
                        throw new ArgumentOutOfRangeException(nameof(evt), "Channel must be between 1 and 512");

                    dmx[channel - 1] = 255; // channel-1 რადგან index 0-based
                }

                ushort length = (ushort)dmx.Length;
                var packet = BuildArtDmxPacket(universe, dmx, length);

                await _udp.SendAsync(packet, packet.Length);

                _logger.LogInformation($" DMX Sent for {evt.ToString()} {universe}, {dmx} Packet: '{packet}' {packet}");
            }
            finally
            {
                _lock.Release();
            }
        }



        private static byte[] BuildArtDmxPacket(int universe, byte[] data, ushort length)
        {
            var packet = new byte[18 + length];
            int idx = 0;

            // "Art-Net\0"
            var id = Encoding.ASCII.GetBytes("Art-Net\0");
            Array.Copy(id, 0, packet, idx, id.Length);
            idx += id.Length;

            // OpCode = 0x5000 (OpDmx) (little endian)
            packet[idx++] = 0x00;
            packet[idx++] = 0x50;

            // Protocol version (14 = 0x000E)
            packet[idx++] = 0x00;
            packet[idx++] = 0x0E;

            // Sequence
            packet[idx++] = 0x00;

            // Physical
            packet[idx++] = 0x00;

            // SubUni (low 8 bits)
            packet[idx++] = (byte)(universe & 0xFF);

            // Net (high 7 bits)
            packet[idx++] = (byte)((universe >> 8) & 0x7F);

            // Length hi/lo
            packet[idx++] = (byte)((length >> 8) & 0xFF);
            packet[idx++] = (byte)(length & 0xFF);

            // Payload
            if (length > 0)
                Array.Copy(data, 0, packet, idx, length);

            return packet;
        }


        public async ValueTask DisposeAsync()
        {
            await _lock.WaitAsync();
            try
            {
                if (_disposed) return;
                _disposed = true;
                _udp?.Dispose();
            }
            finally
            {
                _lock.Release();
                _lock.Dispose();
            }
        }


    }
}
