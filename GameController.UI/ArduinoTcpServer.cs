using Microsoft.Extensions.Configuration;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace GameController.UI
{
    public class ArduinoTcpServer
    {
        private readonly TcpListener _tcpListener;
        private readonly Action<string> _logMessage;
        private readonly Action<string, string> _processArduinoMessage;

        private CancellationTokenSource _cancellationTokenSource;

        //private readonly IConfiguration _config; // ახალი ცვლადი კონფიგურაციისთვის
        //
        public ArduinoTcpServer(Action<string> logMessage, Action<string, string> processArduinoMessage, string ip, int port)
        {
            _logMessage = logMessage;
            _processArduinoMessage = processArduinoMessage;



            IPAddress localAddr = IPAddress.Parse(ip);
            _tcpListener = new TcpListener(localAddr, port);
        }

        public void Start()
        {
            try
            {
                _tcpListener.Start();
                _logMessage($"TCP Server started on {_tcpListener.LocalEndpoint}\r\n");

                _cancellationTokenSource = new CancellationTokenSource();
                Task.Run(() => ListenForClients(_cancellationTokenSource.Token));

                //Task.Run(() => ListenForClients());
            }
            catch (Exception ex)
            {
                _logMessage($"Failed to start TCP server: {ex.Message}\r\n");
            }
        }

        public void Stop()
        {
            _cancellationTokenSource?.Cancel();
            _tcpListener.Stop();
            _logMessage("TCP Server stopped.\r\n");
        }

        private async void ListenForClients(CancellationToken cancellationToken)
        {
            _logMessage($"I am in ListenForClients");
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    TcpClient client = await _tcpListener.AcceptTcpClientAsync(cancellationToken);
                    _logMessage("Arduino connected.\r\n");
                    Task.Run(() => ProcessClientRequest(client));
                }
                catch (OperationCanceledException)
                {
                    // Listener შეჩერდა Cancel-ის ბრძანებით
                    _logMessage("TCP Listener was cancelled.\r\n");
                    break;
                }
                catch (ObjectDisposedException)
                {
                    // Listener was stopped.
                    _logMessage("TCP Listener ObjectDisposed.\r\n");

                    break;
                }
                catch (Exception ex)
                {
                    _logMessage($"Error accepting client: {ex.Message}\r\n");
                }
            }
        }

        private void ProcessClientRequest(TcpClient client)
        {
            _logMessage($"I am in ProcessClientRequest");

            try
            {

                var remoteIpEndPoint = client.Client.RemoteEndPoint as IPEndPoint;
                string clientIp = remoteIpEndPoint?.Address.ToString();

                NetworkStream stream = client.GetStream();
                StringBuilder receivedDataBuilder = new StringBuilder();
                byte[] buffer = new byte[256];
                int bytesRead;

                // წაიკითხეთ მონაცემები სანამ კავშირი ღიაა
                // შეგიძლიათ გამოიყენოთ ტაიმაუტი, რათა არ მოხდეს გაუთავებელი ლოდინი
                client.ReceiveTimeout = 2000; // 2 წამი

                while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    receivedDataBuilder.Append(Encoding.ASCII.GetString(buffer, 0, bytesRead));

                    // შეამოწმეთ, არის თუ არა კავშირი დახურული
                    if (stream.DataAvailable == false && bytesRead < buffer.Length)
                    {
                        break; // კავშირი დახურულია და ყველა მონაცემი წაკითხულია
                    }
                }

                string requestData = receivedDataBuilder.ToString();
                _logMessage($"Received from Arduino: ({clientIp}) {requestData}\r\n");

                if (!string.IsNullOrEmpty(requestData))
                {
                    _processArduinoMessage(requestData, clientIp);
                }
            }
            catch (IOException ex)
            {
                // ჩაწერა ლოგში თუ კავშირი მოულოდნელად დაიხურა
                _logMessage($"Client read error: {ex.Message}\r\n");
            }
            catch (Exception ex)
            {
                // სხვა შეცდომები
                _logMessage($"Error processing client request: {ex.Message}\r\n");
            }
            finally
            {
                client.Close();
            }
        }
    }
}


// ArduinoTcpServer.cs

