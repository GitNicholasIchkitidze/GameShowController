//using CasparCg.AmcpClient.Commands.Cg;
//using CasparCg.AmcpClient.Commands.Query.Common;
using CasparCg.AmcpClient.Commands.Query.Common;
using GameController.Shared.Enums;
using GameController.Shared.Models;
using GameController.Shared.Models.Connection;
using GameController.Shared.Models.YouTube;
using GameController.UI;
using GameController.UI.Model;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Numerics;
using System.Reflection;
using System.Reflection.Emit;
using System.Reflection.Metadata;
using System.Runtime;
using System.Text.Json;
using System.Threading.Channels;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Formats.Asn1.AsnWriter;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using Application = System.Windows.Forms.Application;
using MethodInvoker = System.Windows.Forms.MethodInvoker;


namespace GameShowCtrl
{
    public partial class MnForm : Form
    {






        /// <summary>
        /// /
        /// 
        /// </summary>


        private readonly HubConnection _hubConnection;
        private ArduinoTcpServer arduinoServer;
        //private bool _acceptingTcpAnswers = false; // ეს არის თქვენი ფლაგი
        private TcpListenModel _tcpListeningState = new TcpListenModel(); // შექმენით GameState ობიექტი

        private string ardPlayer1IP;
        private string ardPlayer2IP;

        private DateTime _countdownEndTime;
        private ConcurrentDictionary<string, string> _playerNamesToIds = new ConcurrentDictionary<string, string>();

        private readonly string _serverBaseUrl; // ახალი ცვლადი
        private readonly IConfiguration _config; // ახალი ცვლადი კონფიგურაციისთვის
        private readonly IConfiguration _configCG;

        //private readonly ILogger<MnForm> _logger;

        private string ClientId = "თქვენი_Client_ID"; // ჩაანაცვლეთ თქვენი Client ID-ით
        private string ClientSecret = "თქვენი_Client_ID"; // ჩაანაცვლეთ თქვენი Client ID-ით
        private string RedirectUri = "http://localhost:5001/auth"; // უნდა ემთხვეოდეს Google-ის კონფიგურაციას
        private HttpListener? _listener;

        private ConcurrentDictionary<string, string> _activeAudienceMembers = new ConcurrentDictionary<string, string>();

        private LastAction _lastAction = LastAction.None;

        public string? _currentPlayerId;

        //private static readonly Dictionary<CGTemplateEnums, (string serverIP, int channel, string templateName, int layer, int layerCg)> _cgSettingsMap = new();
        private static readonly Dictionary<CGTemplateEnums, templateSettingModel> _cgSettingsMap = new();
        public MnForm()
        {
            InitializeComponent();
            cmbCountDownMode.DataSource = Enum.GetValues(typeof(GameMode));

            _config = new ConfigurationBuilder()
            .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
            .AddJsonFile("appsettings.UI.json")
            .Build();


            ardPlayer1IP = _config.GetValue<string>("ServerSettingsForAVR:ardPlayer1IP");
            ardPlayer2IP = _config.GetValue<string>("ServerSettingsForAVR:ardPlayer2IP");

            var serverAppFolder = _config["ServerSettings:BaseAppFolder"] ?? "https://localhost:7172";
            _configCG = new ConfigurationBuilder()
            .SetBasePath(serverAppFolder)
            .AddJsonFile("appsettings.json")
            .Build();


            _serverBaseUrl = _config["ServerSettings:BaseUrl"] ?? "https://localhost:7172";

            if (!Uri.TryCreate(_serverBaseUrl, UriKind.Absolute, out var baseUri) ||
                    (baseUri.Scheme != Uri.UriSchemeHttp && baseUri.Scheme != Uri.UriSchemeHttps))
            {
                MessageBox.Show($"Invalid Server BaseUrl: '{_serverBaseUrl}'. Please check your configuration.", "Configuration Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                throw new UriFormatException($"Invalid Server BaseUrl: '{_serverBaseUrl}'");
            }


            var operatorName = _config["ServerSettings:Operator"] ?? "Operator";
            PnlRapidFire.Enabled = false;

            _hubConnection = new HubConnectionBuilder()

                        .WithUrl($"{_serverBaseUrl}gamehub?name={operatorName}")
                                .WithAutomaticReconnect()
                .Build();


            this.Shown += async (sender, e) =>
            {
                await SetupHubConnection();
            };


            //if (_hubConnection.State != HubConnectionState.Connected)
            //{
            //	// აჩვენეთ შეტყობინება მომხმარებელს და შეაჩერეთ მოქმედება
            //	MessageBox.Show($"კავშირი სერვერთან არ არის აქტიური. {_serverBaseUrl}");
            //	return;
            //}


            countdownTimer.Tick += new System.EventHandler(this.countdownTimer_Tick);

            ClientId = _config["YTVotingSettings:client_id"] ?? "";
            RedirectUri = _config["YTVotingSettings:redirect_uris"] ?? "";
            ClientSecret = _config["YTVotingSettings:client_secret"] ?? "";




            var _cgSettings = _configCG.GetSection("CG").Get<CasparCGSettings>();


            if (_cgSettings != null)
            {
                _cgSettingsMap[CGTemplateEnums.QuestionFull] = new templateSettingModel(
                    CGTemplateEnums.QuestionFull.ToString(),
                    _cgSettings.QuestionFull.TemplateName,
                    _cgSettings.QuestionFull.TemplateUrl,
                    _cgSettings.QuestionFull.Channel,
                    _cgSettings.QuestionFull.Layer,
                    _cgSettings.QuestionFull.LayerCg,
                    _cgSettings.QuestionFull.ServerIp ?? string.Empty
                );
                _cgSettingsMap[CGTemplateEnums.QuestionLower] = new templateSettingModel(
                    CGTemplateEnums.QuestionLower.ToString(),
                    _cgSettings.QuestionLower.TemplateName,
                    _cgSettings.QuestionLower.TemplateUrl,
                    _cgSettings.QuestionLower.Channel,
                    _cgSettings.QuestionLower.Layer,
                    _cgSettings.QuestionLower.LayerCg,
                    _cgSettings.QuestionLower.ServerIp ?? string.Empty
                );
                _cgSettingsMap[CGTemplateEnums.CountDown] = new templateSettingModel(
                    CGTemplateEnums.CountDown.ToString(),
                    _cgSettings.CountDown.TemplateName,
                    _cgSettings.CountDown.TemplateUrl,
                    _cgSettings.CountDown.Channel,
                    _cgSettings.CountDown.Layer,
                    _cgSettings.CountDown.LayerCg,
                    _cgSettings.CountDown.ServerIp ?? string.Empty
                );
                _cgSettingsMap[CGTemplateEnums.LeaderBoard] = new templateSettingModel(
                    CGTemplateEnums.LeaderBoard.ToString(),
                    _cgSettings.LeaderBoard.TemplateName,
                    _cgSettings.LeaderBoard.TemplateUrl,
                    _cgSettings.LeaderBoard.Channel,
                    _cgSettings.LeaderBoard.Layer,
                    _cgSettings.LeaderBoard.LayerCg,
                    _cgSettings.LeaderBoard.ServerIp ?? string.Empty
                );
                _cgSettingsMap[CGTemplateEnums.YTVote] = new templateSettingModel(
                    CGTemplateEnums.YTVote.ToString(),
                    _cgSettings.YTVote.TemplateName,
                    _cgSettings.YTVote.TemplateUrl,
                    _cgSettings.YTVote.Channel,
                    _cgSettings.YTVote.Layer,
                    _cgSettings.YTVote.LayerCg,
                    _cgSettings.YTVote.ServerIp ?? string.Empty
                );
                _cgSettingsMap[CGTemplateEnums.QuestionVideo] = new templateSettingModel(
                    CGTemplateEnums.QuestionVideo.ToString(),
                    _cgSettings.QuestionVideo.TemplateName,
                    _cgSettings.QuestionVideo.TemplateUrl,
                    _cgSettings.QuestionVideo.Channel,
                    _cgSettings.QuestionVideo.Layer,
                    _cgSettings.QuestionVideo.LayerCg,
                    _cgSettings.QuestionVideo.ServerIp ?? string.Empty
                );
                _cgSettingsMap[CGTemplateEnums.tPs1] = new templateSettingModel(
                    CGTemplateEnums.tPs1.ToString(),
                    _cgSettings.tPs1.TemplateName,
                    _cgSettings.tPs1.TemplateUrl,
                    _cgSettings.tPs1.Channel,
                    _cgSettings.tPs1.Layer,
                    _cgSettings.tPs1.LayerCg,
                    _cgSettings.tPs1.ServerIp ?? string.Empty
                );

            }







        }


        private void MnForm_Load(object sender, EventArgs e)
        {

            StarArduionServer();
            bSrc_TcpListeningState.DataSource = _tcpListeningState;


            dgvTemplateSettings_InitializeData(true);
        }


        #region dgvTemplateSettings
        private async void dgvTemplateSettings_InitializeData(bool AddButton = false)
        {
            // მონაცემების ჩატვირთვა
            var settingsList = _cgSettingsMap.Select(kvp => new templateSettingModel
            {
                TemplateType = kvp.Key.ToString(),
                TemplateName = kvp.Value.TemplateName,
                ServerIP = kvp.Value.ServerIP,
                Channel = kvp.Value.Channel,
                TemplateUrl = kvp.Value.TemplateUrl,
                Layer = kvp.Value.Layer,
                LayerCg = kvp.Value.LayerCg,
                IsRegistered = kvp.Value.IsRegistered
            }).ToList();



            if (dgvTemplateSettings.InvokeRequired)
            {
                dgvTemplateSettings.Invoke(new Action(() => dgvTemplateSettings.DataSource = settingsList));
            }
            else
            {
                dgvTemplateSettings.DataSource = settingsList;
            }

            // დაამატე ღილაკის სვეტი
            if (AddButton)
                dgvTemplateSettings_AddButtonColumn();
        }
        private void dgvTemplateSettings_AddButtonColumn()
        {
            // შექმენი ღილაკის სვეტი
            DataGridViewButtonColumn buttonColumn = new DataGridViewButtonColumn();
            buttonColumn.Name = "ActionColumn";
            buttonColumn.HeaderText = "Action";
            buttonColumn.Text = "Reload";
            buttonColumn.UseColumnTextForButtonValue = true;
            buttonColumn.Width = 80;

            if (dgvTemplateSettings.InvokeRequired)
            {
                dgvTemplateSettings.Invoke(new Action(() => dgvTemplateSettings.Columns.Add(buttonColumn)));
                dgvTemplateSettings.Invoke(new Action(() => dgvTemplateSettings.CellClick += dgvTemplateSettings_CellClick));
            }
            else
            {
                dgvTemplateSettings.Columns.Add(buttonColumn);
                dgvTemplateSettings.CellClick += dgvTemplateSettings_CellClick;
            }

            // დაამატე სვეტი DataGridView-ში
            //dgvTemplateSettings.Columns.Add(buttonColumn);

            // დაარეგისტრირე CellClick ივენთი
            //dgvTemplateSettings.CellClick += dgvTemplateSettings_CellClick;
        }


        private void dgvTemplateSettings_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            // გაიგე თუ ღილაკის სვეტზე დააწკაპუნეს
            if (e.RowIndex >= 0 && e.ColumnIndex == dgvTemplateSettings.Columns["ActionColumn"].Index)
            {
                // მიიღე მონაცემები ამ მწკრივიდან
                var rowData = (templateSettingModel)dgvTemplateSettings.Rows[e.RowIndex].DataBoundItem;

                // გამოიძახე ღილაკის ფუნქცია
                dgvTemplateSettings_OnPreviewButtonClick(rowData);
            }
        }

        private async void dgvTemplateSettings_OnPreviewButtonClick(templateSettingModel templateData)
        {
            // აქ დაამუშავე ღილაკის დაჭერის ლოგიკა
            //MessageBox.Show($"Preview clicked for: {templateData.TemplateType}\n" +
            //			   $"Template: {templateData.TemplateName}\n" +
            //			   $"Server: {templateData.ServerIP}");

            if (Enum.TryParse<CGTemplateEnums>(templateData.TemplateName, out var templateEnum))
            {

                var result = await _hubConnection.InvokeAsync<OperationResult>("CGLoadTemplate", templateEnum);
                AppendLog($"[WinForms UI] <- Hub {templateEnum} - ის ჩატვირთვა: {result.Message}");

            }


            // ან გამოიძახე სხვა მეთოდი
            // PreviewTemplate(templateData);
        }

        private async void dgvTemplateSettings_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0 && e.ColumnIndex >= 0)
            {
                var editedItem = (templateSettingModel)dgvTemplateSettings.Rows[e.RowIndex].DataBoundItem;

                // განაახლე _cgSettingsMap
                if (Enum.TryParse<CGTemplateEnums>(editedItem.TemplateType, out var templateEnum))
                {
                    _cgSettingsMap[templateEnum] = new templateSettingModel(
                        editedItem.TemplateType,
                        editedItem.TemplateName,
                        editedItem.TemplateUrl,
                        editedItem.Channel,
                        editedItem.Layer,
                        editedItem.LayerCg,
                        editedItem.ServerIP
                    );

                    await _hubConnection.InvokeAsync("UpdatecgSettingsMap", templateEnum, _cgSettingsMap);
                }
            }
        }

        #endregion

        private async Task SetupHubConnection()
        {
            try
            {
                await _hubConnection.StartAsync();
                this.Text = "Connected to Hub";

                PnlRapidFire.Enabled = true;


                await LoadInitialTemplates();
                SetHubConnections();
            }
            catch (HttpRequestException ex) when (ex.InnerException is SocketException)
            {
                this.Text = "Connection Refused";
                MessageBox.Show(
                    "Connection failed because the server at localhost:7127 actively refused it.\n\n" +
                    "Possible reasons:\n" +
                    "1. საკომუნიკაციო სერვერი არ მუშაობს.\n" +
                    "2. The server is configured on a different port.\n\n" +
                    "წადი ნახე მუშაობს თუ არა, ან ნიკას დაუძახე.",
                    "Connection Refused",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            // ვიჭერთ სხვა ზოგად HTTP შეცდომებს (მაგ. SSL სერტიფიკატის პრობლემა)
            // Catches other general HTTP errors (e.g., SSL certificate issues)
            catch (HttpRequestException ex)
            {
                this.Text = "Connection Error";
                MessageBox.Show($"A network error occurred while trying to connect.\n\nDetails: {ex.Message}", "Network Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            // ვიჭერთ Timeout შეცდომას
            // Catches a timeout error
            catch (TaskCanceledException ex)
            {
                this.Text = "Connection Timed Out";
                MessageBox.Show("The connection attempt timed out. The server might be slow to respond or the network connection is unstable.", "Connection Timeout", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            // ბოლოს, ვიჭერთ ყველა სხვა, მოულოდნელ შეცდომას
            // Finally, catch any other unexpected errors
            catch (Exception ex)
            {
                this.Text = "Connection Failed";
                MessageBox.Show($"An unexpected error occurred: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

        }


        private void StarArduionServer()
        {

            string ip = _config["ServerSettingsForAvr:ip"] ?? "localhost";
            int port = _config.GetValue<int>("ServerSettingsForAvr:port");

            // შექმენით სერვერის ინსტანცია და დაიწყეთ
            arduinoServer = new ArduinoTcpServer(
                // ლოგირების მეთოდი
                message => this.Invoke(new MethodInvoker(() => textBoxLog.AppendText(message))),
               // მონაცემთა დამუშავების მეთოდი
               (processMessage, clientIp) => this.Invoke(new MethodInvoker(() => ProcessArduinoData(processMessage, clientIp))),
            ip,
            port
            );
            arduinoServer.Start();
        }

        private void ProcessArduinoData(string jsonData, string clientIp)
        {

            if (!_tcpListeningState.AcceptingAnswers)
            {
                // არდუინოსგან მიღებული მონაცემები იგნორირებულია
                textBoxLog.AppendText("Ignoring Arduino data, not in an accepting state.\r\n");
                return;
            }


            try
            {
                JsonDocument doc = JsonDocument.Parse(jsonData);

                doc.RootElement.TryGetProperty("answer", out var msgtype);//.GetProperty("object")
                                                                          //string answer = doc.RootElement.GetProperty("object").GetString();
                string playerName = "ArduinoPlayer1";


                if (clientIp == ardPlayer1IP && msgtype.GetString() == "Answer 1")
                {
                    SetCurrentPlayer("Player1");
                }

                if (clientIp == ardPlayer2IP && msgtype.GetString() == "Answer 2")
                {
                    SetCurrentPlayer("Player2");
                }



                // გაუგზავნეთ პასუხი GameHub-ში
                // დარწმუნდით, რომ hubConnection ობიექტი ხელმისაწვდომია აქ.
                //if (hubConnection != null && hubConnection.State == HubConnectionState.Connected)
                //{
                //    hubConnection.InvokeAsync("ReceiveAnswer", playerName, answer);
                //    this.Invoke(new MethodInvoker(() =>
                //    {
                //        textBoxLog.AppendText($"Processed and sent answer to Hub: {answer}\r\n");
                //    }));
                //}

                _tcpListeningState.AcceptingAnswers = false;
                bSrc_TcpListeningState.ResetBindings(false);
            }
            catch (System.Text.Json.JsonException ex)
            {
                this.Invoke(new MethodInvoker(() =>
                {
                    textBoxLog.AppendText($"JSON parse error from Arduino: {ex.Message}\r\n");
                }));
            }
        }

        private async Task LoadInitialTemplates()
        {
            if (_hubConnection.State != HubConnectionState.Connected)
            {

                AppendLog($"[WinForms UI]\t{"კავშირი საკომუნიკაციო სერვერთან არ არის აქტიური, ვერ ხერხდება საწყისი ტემპლეტების ჩატვირთვა."}");
                return;
            }

            try
            {

                var autoLoad = _config.GetValue<bool>("ServerSettings:AutoLoadTemplates", false);
                if (autoLoad)
                {
                    AppendLog($"[WinForms UI]\tიწყება CG თემლეიტების ჩატვირთვა");
                    var useYt = _config["YTVotingSettings:UseYT"] ?? "NO";

                    var resultPs1 = await _hubConnection.InvokeAsync<OperationResult>("CGLoadTemplate", CGTemplateEnums.tPs1);
                    AppendLog($"[WinForms UI] <- Hub LowerThird PS1 - ის ჩატვირთვა: {resultPs1.Message}");
                    ////                    var resultQF = await _hubConnection.InvokeAsync<OperationResult>("CGLoadTemplate", CGTemplateEnums.QuestionFull);
                    ////                    AppendLog($"[WinForms UI] <- Hub QuestionFull - ის ჩატვირთვა: {resultQF.Message}");
                    ////                    var resultLB = await _hubConnection.InvokeAsync<OperationResult>("CGLoadTemplate", CGTemplateEnums.LeaderBoard);
                    ////                    AppendLog($"[WinForms UI] <- Hub LeaderBoard - ის ჩატვირთვა: {resultLB.Message}");
                    ////                    var resultQL = await _hubConnection.InvokeAsync<OperationResult>("CGLoadTemplate", CGTemplateEnums.QuestionLower);
                    ////                    AppendLog($"[WinForms UI] <- Hub QuestionLower - ის ჩატვირთვა: {resultQL.Message}");
                    ////                    var resultCD = await _hubConnection.InvokeAsync<OperationResult>("CGLoadTemplate", CGTemplateEnums.CountDown);
                    ////                    AppendLog($"[WinForms UI] <- Hub CountDown - ის ჩატვირთვა: {resultCD.Message}");

                    if (useYt.ToUpper() == "YES")
                    {
                        var resultYT = await _hubConnection.InvokeAsync<OperationResult>("CGLoadTemplate", CGTemplateEnums.YTVote);
                        AppendLog($"[WinForms UI] <- Hub YTVote - ის ჩატვირთვა: {resultYT.Message}");
                    }
                }
                else
                {
                    AppendLog($"[WinForms UI]\t AutoLoad Template is OFF");
                }

            }
            catch (Exception ex)
            {
                Console.Write($"{ex.Message}");
            }
        }
        private void SetHubConnections()
        {
            _hubConnection.On<string, string>("ReceiveMessage", (user, message) =>
            {
                AppendLog($"[WinForms UI] <- Hub\tReceiveMessage");
                // This will be used later to show messages from the server
                // For now, we can just log it or show a message box.
                MessageBox.Show($"{user}: {message}");
            });

            _hubConnection.On<string>("ReceiveRegistrationStatus", (status) =>
            {
                AppendLog($"[WinForms UI] <- Hub\tReceiveMessage");
                // This will be used later to show registration status
                MessageBox.Show($"Registration Status: {status}");
            });

            _hubConnection.On<List<Player>>("UpdatePlayerList", (players) =>
            {
                AppendLog($"[WinForms UI] <- Hub\tUpdatePlayerList");
                // UI-ის განახლება ხდება მთავარ Thread-ზე.
                // Invoke მეთოდი ამის უზრუნველსაყოფად გამოიყენება.
                if (this.InvokeRequired)
                {
                    this.Invoke(new Action(() => UpdatePlayerList(players)));
                }
                else
                {
                    UpdatePlayerList(players);
                }
            });

            _hubConnection.On<List<QuestionModel>>("ReceiveQuestionList", (questions) =>
            {
                AppendLog($"[WinForms UI] <- Hub\tReceiveQuestionList");
                //_questions = questions;
                PopulateQuestionGrid(questions);
            });

            _hubConnection.On<TemplateRegistrationModel>("CGTemplateStatusUpdate", (TemplateRegistrationModel) =>
            {
                var isreg = TemplateRegistrationModel.IsRegistered == true ? "Registered" : "";
                AppendLog($"[WinForms UI] <- Hub\tCGTemplateStatusUpdate {TemplateRegistrationModel.TemplateName} {isreg}");


                if (Enum.TryParse<CGTemplateEnums>(TemplateRegistrationModel.TemplateName, out var templateEnum))
                {
                    _cgSettingsMap[templateEnum].IsRegistered = TemplateRegistrationModel.IsRegistered.Value;
                    dgvTemplateSettings_InitializeData();
                }

            });




            _hubConnection.On<bool>("ReceiveMidiStatus", (isConnected) =>
            {
                AppendLog($"[WinForms UI] <- Hub\tReceiveMidiStatus");
                // განაახლეთ UI კონტროლი UI thread-ზე
                if (cBoxUseLightControl.InvokeRequired)
                {
                    cBoxUseLightControl.Invoke(new Action(() => cBoxUseLightControl.Enabled = isConnected));
                }
                else
                {
                    cBoxUseLightControl.Enabled = isConnected;
                }
            });

            _hubConnection.On<long>("ReceiveCountDown", async (endTimestamp) =>
            {
                AppendLog($"[WinForms UI] <- Hub\tReceiveCountDown");
                // Invoke-ის გამოყენება UI-ს მანიპულაციისთვის
                if (this.InvokeRequired)
                {
                    this.Invoke(new Action(() =>
                    {
                        _countdownEndTime = DateTimeOffset.FromUnixTimeMilliseconds(endTimestamp).LocalDateTime;
                        countdownTimer.Start();
                    }));
                }
                else
                {
                    _countdownEndTime = DateTimeOffset.FromUnixTimeMilliseconds(endTimestamp).LocalDateTime;
                    countdownTimer.Start();
                }
                await _hubConnection.InvokeAsync("CGWSCountDown", CGTemplateEnums.CountDown.ToString(), (int)CountDownDuration.Value, CountDownStopMode.Start.ToString(), endTimestamp);

            });

            _hubConnection.On<string>("StopCountDown", async (mode) =>
            {
                AppendLog($"[WinForms UI] <- Hub\tStopCountDown");
                if (this.InvokeRequired)
                {
                    this.Invoke(new Action(async () =>
                    {
                        countdownTimer.Stop();
                        if (mode == CountDownStopMode.Reset.ToString())
                        {
                            await _hubConnection.InvokeAsync("CGWSCountDown", CGTemplateEnums.CountDown.ToString(), (int)CountDownDuration.Value, CountDownStopMode.Reset, 0);
                            lblCountDown.Text = "";
                        }

                        else if (mode == CountDownStopMode.Pause.ToString())
                        {
                            await _hubConnection.InvokeAsync("CGWSCountDown", CGTemplateEnums.CountDown.ToString(), (int)CountDownDuration.Value, CountDownStopMode.Pause, 0);
                            lblCountDown.Text = lblCountDown.Text;
                        }
                        else
                        {
                            lblCountDown.Text = "დრო ამოოიწურა!";
                            await _hubConnection.InvokeAsync("CGWSCountDown", CGTemplateEnums.CountDown.ToString(), (int)CountDownDuration.Value, CountDownStopMode.Reset, 0);
                        }


                    }));
                }
                else
                {
                    countdownTimer.Stop();
                    if (mode == CountDownStopMode.Reset.ToString())
                        lblCountDown.Text = "0";
                    else if (mode == CountDownStopMode.Pause.ToString())
                        lblCountDown.Text = lblCountDown.Text;
                    else
                        lblCountDown.Text = "დრო ამოოიწურა!";

                }


            });

            _hubConnection.On<RoundEndAction>("RoundEnded", (action) =>
            {
                AppendLog($"[WinForms UI] <- Hub\tRoundEnded");
                if (this.InvokeRequired)
                {
                    this.Invoke(new Action(() =>
                    {
                        countdownTimer.Stop();
                        if (action == RoundEndAction.Reset)
                        {
                            lblCountDown.Text = "0s";
                        }
                        else if (action == RoundEndAction.Pause)
                        {
                            // The timer will simply be stopped, preserving its current value
                        }
                        MessageBox.Show($"Round Ended. Action: {action}", "Round Ended");
                    }));
                }
                else
                {
                    countdownTimer.Stop();
                    if (action == RoundEndAction.Reset)
                    {
                        lblCountDown.Text = "0s";
                    }
                    // No action needed for Pause, as the timer is already stopped.
                }
            });

            _hubConnection.On("PlayerAnsweredInRapidFire", () =>
            {
                AppendLog($"[WinForms UI] <- Hub\tPlayerAnsweredInRapidFire");
                if (this.InvokeRequired)
                {
                    this.Invoke(new Action(async () =>
                    {
                        var nextQuestion = GetNextQuestionFromGrid(); // Your existing method
                        if (nextQuestion != null)
                        {
                            await _hubConnection.InvokeAsync("SendRapidFireQuestionFromUI", nextQuestion, cBoxDisableInput.Checked);
                        }
                        else
                        {
                            MessageBox.Show("End of Rapid Fire questions.", "Rapid Fire Ended", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            //await _hubConnection.InvokeAsync("EndRound");
                        }
                    }));
                }
            });


            _hubConnection.On("OperatorConfirmedAnswer", () =>
            {
                AppendLog($"[WinForms UI] <- Hub\tOperatorConfirmedAnswer");
                if (this.InvokeRequired)
                {
                    this.Invoke(new Action(async () =>
                    {
                        // This is the core logic. Get the next question from the grid
                        // and send it to the server.
                        var nextQuestion = GetNextQuestionFromGrid();
                        if (nextQuestion != null)
                        {
                            if ((GameMode)cmbCountDownMode.SelectedItem == GameMode.RapidMode)
                                await _hubConnection.InvokeAsync("SendRapidFireQuestionFromUI", nextQuestion, cBoxDisableInput.Checked);
                            //if ((GameMode)cmbCountDownMode.SelectedItem == GameMode.Round1)
                            //	btnSendQuestion.PerformClick();

                        }
                        else
                        {
                            MessageBox.Show("End of Rapid Fire questions.", "Rapid Fire Ended", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            await _hubConnection.InvokeAsync("EndRound");
                        }
                    }));
                }
            });



            _hubConnection.On<string>("UpdateVotingModeStatus", (message) =>
            {
                AppendLog($"[WinForms UI] <- Hub\tUpdateVotingModeStatus\t{message}");
                // ამ მეთოდს გამოვიყენებთ ხმის მიცემის სტატუსის ჩვენებისთვის

            });

            _hubConnection.On<string, string>("UserKicked", (authorName, message) =>
            {
                AppendLog($"[WinForms UI] <- Hub\tUpdateVotingModeStatus\t{authorName}: {message}");
            });

            _hubConnection.On<List<AudienceMember>>("UpdateActiveAudienceList", (audienceList) =>
            {
                AppendLog($"[WinForms UI] <- Hub\tUpdateActiveAudienceList");
                // განვაახლოთ UI აქტიური მომხმარებლების სიით
                Invoke(() =>
                {
                    votingUserListBox.Items.Clear();
                    _activeAudienceMembers.Clear();
                    foreach (var member in audienceList)
                    {
                        var userItem = new ListViewItem(member.AuthorName);
                        userItem.Tag = member.AuthorChannelId; // ID-ის შენახვა
                        votingUserListBox.Items.Add(userItem);
                        _activeAudienceMembers.TryAdd(member.AuthorChannelId, member.AuthorName);
                    }
                });
            });

            _hubConnection.On<VoteResultsMessage>("VoteResultsMessage", (message) =>
            {
                string resultsString = JsonConvert.SerializeObject(message);
                AppendLog($"[WinForms UI] <- Hub\tVoteResultsMessage {resultsString}");



            });



        }
        //private void AppendChatBoxMessage(string message)
        //{
        //	// Invoke საჭიროა, რადგან SignalR მუშაობს სხვა ნაკადზე, ხოლო UI-ის განახლება უნდა მოხდეს მთავარ ნაკადზე.
        //	if (this.InvokeRequired)
        //	{
        //		this.Invoke(new Action<string>(AppendChatBoxMessage), message);
        //	}
        //	else
        //	{
        //		textBoxLog.Text = $"{message}{Environment.NewLine}{textBoxLog.Text}";
        //	}
        //}
        private QuestionModel? GetNextQuestionFromGrid()
        {
            if (dgvQuestions.SelectedRows.Count == 0)
            {
                // No row is selected. This is a problem, but we can't select the next one.
                // Returning null will trigger the end-of-round logic.
                return null;
            }

            var currentIndex = dgvQuestions.SelectedRows[0].Index;
            var nextIndex = currentIndex + 1;

            // Check if a next row exists in the grid
            if (nextIndex < dgvQuestions.Rows.Count)
            {
                // Clear the current selection and select the next row
                dgvQuestions.ClearSelection();
                dgvQuestions.Rows[nextIndex].Selected = true;

                // Ensure the newly selected row is visible
                dgvQuestions.FirstDisplayedScrollingRowIndex = nextIndex;

                // Retrieve the QuestionModel object from the DataBoundItem
                var nextQuestion = (QuestionModel)dgvQuestions.Rows[nextIndex].DataBoundItem;
                return nextQuestion;
            }
            else
            {
                // End of the question list
                return null;
            }
        }

        private void PopulateQuestionGrid(List<QuestionModel> questions)
        {
            // Check if the current thread is the UI thread
            if (this.InvokeRequired)
            {
                // If not, use Invoke to call this method on the UI thread
                this.Invoke(new Action(() => PopulateQuestionGrid(questions)));
            }
            else
            {
                // If it is, update the DataGridView
                dgvQuestions.DataSource = questions;
                //dgvQuestions.ReadOnly = true;
                //dgvQuestions.SelectionMode = DataGridViewSelectionMode.FullRowSelect;

                // გამორთეთ ავტომატური დალაგება თითოეული სვეტისთვის
                ///foreach (DataGridViewColumn column in dgvQuestions.Columns)
                ///{
                ///	column.SortMode = DataGridViewColumnSortMode.NotSortable;
                ///}
            }
        }

        private void UpdatePlayerList(List<Player> players)
        {
            listBoxClients.Items.Clear();

            dgvContestants.AutoGenerateColumns = true;

            dgvContestants.DataSource = players.Where(x => x.ClientType == "Contestant").ToList();
            dgvContestants.Columns["ConnectionID"].Visible = false;
            dgvContestants.Columns["Ip"].Visible = false;
            dgvContestants.Columns["ClientType"].Visible = false;
            dgvContestants.Columns["Name"].Visible = false;
            //dgvContestants.Columns["Ip"].Visible = false;
            _playerNamesToIds.Clear();


            foreach (var player in players)
            {
                listBoxClients.Items.Add($"{player.Ip} {player.Name} ({player.ClientType}) - Score: {player.Score}");

                if (player.ClientType == "Contestant")
                {
                    _playerNamesToIds.TryAdd(player.Name, player.ConnectionId);

                }

            }
        }



        public void AppendLog(string message)
        {
            string timeStamp = DateTime.Now.ToString("HH:mm:ss.fff");
            string logEntry = $"[{timeStamp}]\t{message}{Environment.NewLine}";
            // შეამოწმეთ, საჭიროა თუ არა Invoke, რადგან UI ელემენტებთან წვდომა მხოლოდ UI თრედიდან არის უსაფრთხო
            //if (textBoxLog.InvokeRequired)
            if (this.InvokeRequired)
            {
                this.Invoke(new MethodInvoker(() => AppendLog($"Invoked {message}")));
                // გამოიძახეთ მეთოდი UI თრედზე
                //textBoxLog.Invoke(new MethodInvoker(delegate {
                //	textBoxLog.AppendText($"{DateTime.Now}: {message} { Environment.NewLine}"
                //);
                //}));
            }
            else
            {
                // თუ უკვე UI თრედზე ვართ, პირდაპირ ჩაწერეთ
                //textBoxLog.AppendText($"{DateTime.Now}: {message} {Environment.NewLine}");
                textBoxLog.Text = logEntry + textBoxLog.Text;
            }
        }










        private async void btnSendToSelectedClient_Click(object sender, EventArgs e)
        {
            if (listBoxClients.SelectedItem == null)
            {
                MessageBox.Show("გთხოვთ, აირჩიოთ კლიენტი სიიდან.");
                return;
            }


            if (listBoxClients.SelectedIndex == -1)
            {
                MessageBox.Show("Please select a player to send a message to.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var selectedPlayer = listBoxClients.SelectedItem.ToString();

            var fetchIp = selectedPlayer?.Split(' ')[0] ?? string.Empty; // Extract the Connection ID safely

            var pls = await GetRegisteredPlayers();
            var selPlayerConnectionId = pls.FirstOrDefault(p => p.Ip == fetchIp)?.ConnectionId;
            if (!string.IsNullOrEmpty(listBoxClients.Text))
            {
                // Invoke the SignalR method on the server
                //await _hubConnection.InvokeAsync("SendMessageToClient", selPlayerConnectionId, txtMessageToSpecificClient.Text);
                //txtMessageToSpecificClient.Clear();


            }




        }

        // Change the return type from 'async List<Player>' to 'async Task<List<Player>>'
        private async Task<List<Player>> GetRegisteredPlayers()
        {
            try
            {
                // Call the GetRegisteredPlayers method on the server
                var players = await _hubConnection.InvokeAsync<List<Player>>("GetRegisteredPlayers");

                // Update the ListBox with the received data
                //UpdatePlayerList(players);
                return players;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to get player list: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return null;
            }
        }




        private async void rf_BtnLoadQuestions_Click(object sender, EventArgs e)
        {



            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*";
                openFileDialog.FilterIndex = 1;
                openFileDialog.RestoreDirectory = true;

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        // Read the content of the selected file
                        string fileContent = await System.IO.File.ReadAllTextAsync(openFileDialog.FileName);

                        // Invoke the server method and get the refreshed questions
                        var refreshedQuestions = await _hubConnection.InvokeAsync<List<QuestionModel>>("LoadQuestionsFromFile", fileContent);

                        // Populate the DataGridView with the new data
                        PopulateQuestionGrid(refreshedQuestions);

                        //MessageBox.Show("Questions have been loaded successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Failed to load questions from file: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }

        }






        private async void btnStartCountDown_Click(object sender, EventArgs e)
        {
            await _hubConnection.InvokeAsync("StartCountDown", 30);

        }

        private void countdownTimer_Tick(object? sender, EventArgs e)
        {

            var timeLeft = _countdownEndTime - DateTime.Now;



            if (timeLeft.TotalSeconds <= 0)

            {

                countdownTimer.Stop();

                lblCountDown.Text = "Time's Up!";

                return;

            }



            lblCountDown.Text = $"{(int)timeLeft.TotalSeconds}s";

        }

        private async void btnSendQuestion_Click(object sender, EventArgs e)
        {
            if (dgvQuestions.SelectedRows.Count > 0)
            {

                var selectedRow = dgvQuestions.SelectedRows[0];
                var question = (QuestionModel)selectedRow.DataBoundItem;
                bool disableInput = cBoxDisableInput.Checked; // Add a checkbox on the UI to control this



                int countdownDuration = (int)CountDownDuration.Value;


                var selectedMode = (GameMode)cmbCountDownMode.SelectedItem;

                var allPlayers = dgvContestants.DataSource as List<Player>;
                dgvContestants.MultiSelect = false;
                List<Player>? targetClients = new List<Player>();

                if (allPlayers != null)
                {
                    targetClients = allPlayers.Where(p => p.IsInPlay).ToList();
                }

                if (targetClients.Count == 0)
                {
                    targetClients = allPlayers;
                }

                if (selectedMode == GameMode.RapidMode && (targetClients == null || disableInput && (targetClients.Count != 1)))
                {
                    MessageBox.Show("Please select a One Player to Play with.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                else
                {
                    _currentPlayerId = targetClients.FirstOrDefault().ConnectionId;
                }

                await _hubConnection.InvokeAsync("UpdateScoresFromUIToMEM", targetClients);
                if (selectedMode == GameMode.Round1)
                {
                    _tcpListeningState.AcceptingAnswers = true;
                    bSrc_TcpListeningState.ResetBindings(false);
                }

                await _hubConnection.InvokeAsync("SendQuestion", question, countdownDuration, selectedMode, disableInput, targetClients);


                //btn_R1CorrectAnswer.Enabled = true;
                //btnIncorrectAnswer.Enabled = true;

                //if (selectedMode == GameMode.Round1 && cBoxDisableInput.Checked)
                //{
                //
                //	btn_R1PrepareNext.Visible = false;
                //}

            }
            else
            {

            }
        }

        private async void btnStartRapidFire_Click(object sender, EventArgs e)
        {
            //List<string>? targetClientIds = new List<string>();
            bool disableInput = cBoxDisableInput.Checked;

            var startQuestion = (QuestionModel)dgvQuestions.SelectedRows[0].DataBoundItem;
            if (startQuestion == null)
            {
                MessageBox.Show("Please select a question to start the Rapid Fire round.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }



            //foreach (var item in chBoxPlayers.CheckedItems)
            //{
            //	var key = item?.ToString();
            //	if (!string.IsNullOrEmpty(key) && _playerNamesToIds.TryGetValue(key, out var connectionId))
            //	{
            //		targetClientIds.Add(connectionId);
            //	}
            //}

            var allPlayers = dgvContestants.DataSource as List<Player>;
            List<Player>? targetClients = new List<Player>();

            if (allPlayers != null)
            {
                targetClients = allPlayers.Where(p => p.IsInPlay).ToList();
            }

            if (targetClients.Count == 0 && !disableInput)
            {
                targetClients = allPlayers;
            }

            if (disableInput && (targetClients == null || targetClients.Count != 1))
            {
                MessageBox.Show("Please select a One Player to Play with.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }


            if (disableInput && (targetClients == null || targetClients.Count != 1))
            {
                targetClients = null;
                MessageBox.Show("Please select a One Player to Play with.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }


            int rapidFireDuration = (int)CountDownDuration.Value; // 60-second countdown for the session


            await _hubConnection.InvokeAsync("UpdateScoresFromUIToMEM", targetClients);
            await _hubConnection.InvokeAsync("StartRapidFire", targetClients, rapidFireDuration, disableInput);


            await _hubConnection.InvokeAsync("SendRapidFireQuestionFromUI", startQuestion, disableInput);

        }

        private async void btnIncorrectAnswer_Click(object sender, EventArgs e)
        {



            await _hubConnection.InvokeAsync("OperatorConfirmAnswer", false, _currentPlayerId);
            _lastAction = LastAction.Incorrect;




            //btn_R1CorrectAnswer.Enabled = false;
            //btnIncorrectAnswer.Enabled = false;
            //
            //if ((GameMode)cmbCountDownMode.SelectedItem == GameMode.Round1 && cBoxDisableInput.Checked)
            //{
            //	btn_R1PrepareNext.Visible = true;
            //}


        }

        private async void btnCorrectAnswer_Click(object sender, EventArgs e)
        {

            //var selectedRow = dgvContestants.SelectedRows[0];
            //var player = (Player)selectedRow.DataBoundItem;



            await _hubConnection.InvokeAsync("OperatorConfirmAnswer", true, _currentPlayerId);


            _lastAction = LastAction.Correct;

            //btn_R1CorrectAnswer.Enabled = false;
            //btnIncorrectAnswer.Enabled = true;
            //if ((GameMode)cmbCountDownMode.SelectedItem == GameMode.Round1 && cBoxDisableInput.Checked)
            //{
            //	btn_R1PrepareNext.Visible = true;
            //}


        }

        private void cBoxDisableInput_CheckedChanged(object sender, EventArgs e)
        {
            return;

            if ((GameMode)cmbCountDownMode.SelectedItem == GameMode.Round1 && cBoxDisableInput.Checked)
            {
                tabPageRound1.Enabled = true;
                btn_R1CorrectAnswer.Visible = cBoxDisableInput.Checked;
                btn_R1InCorrectAnswer.Visible = cBoxDisableInput.Checked;
                btn_R1UuupsAnswer.Visible = cBoxDisableInput.Checked;
                btn_R1PrepareNext.Visible = true;
                btn_R1CorrectAnswer.Enabled = false;
                btn_R1InCorrectAnswer.Enabled = false;


                tabPageRound2.Enabled = false;
                tabPageRound3.Enabled = false;
                tabPageRapidFire.Enabled = false;

                btn_R1PrepareNext.Enabled = false;
            }
            else if ((GameMode)cmbCountDownMode.SelectedItem == GameMode.Round2 && !cBoxDisableInput.Checked)
            {
                tabPageRound2.Enabled = true;
                tabPageRound1.Enabled = false;
                tabPageRound3.Enabled = false;
                tabPageRapidFire.Enabled = false;

            }
            else if ((GameMode)cmbCountDownMode.SelectedItem == GameMode.Round3 && !cBoxDisableInput.Checked)
            {
                tabPageRound3.Enabled = true;
                tabPageRound1.Enabled = false;
                tabPageRound2.Enabled = false;
                tabPageRapidFire.Enabled = false;

            }
            else if ((GameMode)cmbCountDownMode.SelectedItem == GameMode.RapidMode && !cBoxDisableInput.Checked)
            {
                tabPageRapidFire.Enabled = true;
                tabPageRound1.Enabled = false;
                tabPageRound2.Enabled = false;
                tabPageRound3.Enabled = false;


            }


            btn_R1PrepareNext.Visible = false;

            //btnShowCorrect.Visible = cBoxDisableInput.Checked;
        }

        private async void btnShowLeaderBoard_Click(object sender, EventArgs e)
        {

            // Toggle the state on the server
            await _hubConnection.InvokeAsync("SetLeaderBoardActive", true);

            // Get the current player list from the server to send to CasparCG immediately
            //await _hubConnection.InvokeAsync("RequestScoreboardUpdate");
        }

        private async void btnHideLeaderBoard_Click(object sender, EventArgs e)
        {
            ///var configuration = new ConfigurationBuilder()
            ///	.SetBasePath(Directory.GetCurrentDirectory())
            ///	.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            ///	.Build();
            ///
            ///var _cgSettings = configuration.GetSection("CG").Get<CasparCGSettings>();
            // Toggle the state on the server
            await _hubConnection.InvokeAsync("SetLeaderBoardActive", false);

            // Send a command to CasparCG to clear the layer




        }

        private void button1_Click(object sender, EventArgs e)
        {

        }

        private async void btnTestFull_Click(object sender, EventArgs e)
        {
            var selectedRow = dgvQuestions.SelectedRows[0];
            var question = (QuestionModel)selectedRow.DataBoundItem;


            await _hubConnection.InvokeAsync("CGWSUpdateQuestionTemplateData", CGTemplateEnums.QuestionFull.ToString(), question);
        }


        private async void btnTestLower_Click(object sender, EventArgs e)
        {
            var selectedRow = dgvQuestions.SelectedRows[0];
            var question = (QuestionModel)selectedRow.DataBoundItem;

            // გამოიყენეთ ახალი Hub-ის მეთოდი, რომელიც იღებს ტემპლეიტის ტიპს და მონაცემებს.
            await _hubConnection.InvokeAsync("CGWSUpdateQuestionTemplateData", CGTemplateEnums.QuestionLower.ToString(), question);

        }

        private async void btnTestCountDown_Click(object sender, EventArgs e)
        {

            await _hubConnection.InvokeAsync("CGEnsureTemplateLoadedAsync", CGTemplateEnums.CountDown.ToString(), 2, 13);

            //await _hubConnection.InvokeAsync("CGLoadTemplate", CGTemplateEnums.CountDown);

            var endTimestamp = DateTimeOffset.UtcNow.AddSeconds(60).ToUnixTimeMilliseconds();
            var jsonData = JsonConvert.SerializeObject(new { endTimestamp = endTimestamp });



        }

        private async void btnTestLeaderBoard_Click(object sender, EventArgs e)
        {
            //await _hubConnection.InvokeAsync("CGLoadTemplate", CGTemplateEnums.LeaderBoard);
            await _hubConnection.InvokeAsync("CGStartCountDown", 60);

        }

        private async void rf_Btn_UpdateLeaderBoard_Click(object sender, EventArgs e)
        {

            await _hubConnection.InvokeAsync("CGLoadTemplate", CGTemplateEnums.QuestionFull);

            await Task.Delay(500);


        }

        private async void button1_Click_1(object sender, EventArgs e)
        {

        }

        private async void btnClearGraphics_Click(object sender, EventArgs e)
        {
            await _hubConnection.InvokeAsync("CGClearChannel", CGTemplateEnums.QuestionLower);
            await Task.Delay(50);
            await _hubConnection.InvokeAsync("CGClearChannel", CGTemplateEnums.QuestionFull);
            await Task.Delay(50);
            await _hubConnection.InvokeAsync("CGClearChannel", CGTemplateEnums.CountDown);
            await Task.Delay(50);
            await _hubConnection.InvokeAsync("CGClearChannel", CGTemplateEnums.LeaderBoard);
            await Task.Delay(50);
            await _hubConnection.InvokeAsync("CGClearChannel", CGTemplateEnums.YTVote);
            await Task.Delay(50);


        }

        private async void btnShowCorrect_Click(object sender, EventArgs e)
        {

            var selectedRow = dgvQuestions.SelectedRows[0];
            var question = (QuestionModel)selectedRow.DataBoundItem;
            var ind = question.Answers.FindIndex(i => i.Equals(question.CorrectAnswer));
            await _hubConnection.InvokeAsync("CGWSShowCorrectAnswer", CGTemplateEnums.QuestionLower.ToString(), ind);

        }

        private async void rf_Btn_LoadCountDown_Click(object sender, EventArgs e)
        {
            await _hubConnection.InvokeAsync("CGLoadTemplate", CGTemplateEnums.CountDown);
            await Task.Delay(500);
        }

        private async void button2_Click(object sender, EventArgs e)
        {
            var durationSeconds = (int)CountDownDuration.Value;
            var endTimestamp = DateTimeOffset.UtcNow.AddSeconds(durationSeconds).ToUnixTimeMilliseconds();
            await _hubConnection.InvokeAsync("CGWSCountDown", CGTemplateEnums.CountDown.ToString(), (int)CountDownDuration.Value, CountDownStopMode.Start, endTimestamp);

        }

        private async void button3_Click(object sender, EventArgs e)
        {
            var durationSeconds = (int)CountDownDuration.Value;
            var endTimestamp = DateTimeOffset.UtcNow.AddSeconds(durationSeconds).ToUnixTimeMilliseconds();
            await _hubConnection.InvokeAsync("CGWSCountDown", CGTemplateEnums.CountDown.ToString(), (int)CountDownDuration.Value, CountDownStopMode.Pause, endTimestamp);

        }

        private async void button5_Click(object sender, EventArgs e)
        {
            var durationSeconds = (int)CountDownDuration.Value;
            var endTimestamp = DateTimeOffset.UtcNow.AddSeconds(durationSeconds).ToUnixTimeMilliseconds();
            await _hubConnection.InvokeAsync("CGWSCountDown", CGTemplateEnums.CountDown.ToString(), (int)CountDownDuration.Value, CountDownStopMode.Resume, endTimestamp);
        }

        private async void button4_Click(object sender, EventArgs e)
        {
            var durationSeconds = (int)CountDownDuration.Value;
            var endTimestamp = DateTimeOffset.UtcNow.AddSeconds(durationSeconds).ToUnixTimeMilliseconds();
            await _hubConnection.InvokeAsync("CGWSCountDown", CGTemplateEnums.CountDown.ToString(), (int)CountDownDuration.Value, CountDownStopMode.Reset, endTimestamp);

        }

        private void tPageRapidFire_Click(object sender, EventArgs e)
        {

        }

        private async void button8_Click(object sender, EventArgs e)
        {
            await _hubConnection.InvokeAsync("CGLoadTemplate", CGTemplateEnums.LeaderBoard);
            await Task.Delay(500);
        }

        private async void btnShowLeaderBoard_Click_1(object sender, EventArgs e)
        {
            var button = sender as System.Windows.Forms.Button;
            if (button == null)
                return;

            if (button.Text == "Show LeaderBoard")
            {
                await _hubConnection.InvokeAsync("CGSWToggleLeaderBoard", true);
                button.Text = "Hide LeaderBoard";
            }
            else
            {
                button.Text = "Show LeaderBoard";
                await _hubConnection.InvokeAsync("CGSWToggleLeaderBoard", false);
            }
        }

        private async void btnHideLeaderBoard_Click_1(object sender, EventArgs e)
        {
            await _hubConnection.InvokeAsync("CGSWToggleLeaderBoard", false);

        }

        private async void btnStoreResults_Click(object sender, EventArgs e)
        {
            await _hubConnection.InvokeAsync("CGSWStoreFinalResults");
            MessageBox.Show("shedegebi shenaxulia!");
        }

        private async void btnShowFinalResults_Click(object sender, EventArgs e)
        {
            await _hubConnection.InvokeAsync("CGSWShowFinalResults");

        }

        private async void btn_ConnectLightDevice_Click(object sender, EventArgs e)
        {


        }

        private async void btn_DisconnectLightDevice_Click(object sender, EventArgs e)
        {
            await _hubConnection.InvokeAsync("DisconnectMidiDevice");

        }

        private async void cBoxUseLightControl_CheckedChanged(object sender, EventArgs e)
        {
            await _hubConnection.InvokeAsync("SetLightControlEnabled", cBoxUseLightControl.Checked);

        }

        private async void btn_YTAuth_Click(object sender, EventArgs e)
        {
            // კონფიგურაციის მონაცემების მიღება
            //ClientId = _config["YTVotingSettings:client_id"] ?? "";
            //RedirectUri = _config["YTVotingSettings:redirect_uris"] ?? "";
            //ClientSecret = _config["YTVotingSettings:client_secret"] ?? "";

            // ავტორიზაციის URL-ის შექმნა
            var authUrl = $"https://accounts.google.com/o/oauth2/auth?" +
                                $"client_id={ClientId}&" +
                                $"redirect_uri={RedirectUri}&" +
                                $"scope=https://www.googleapis.com/auth/youtube.force-ssl&" +
                                $"response_type=code&" +
                                $"access_type=offline&" +
                                $"prompt=consent"; // დაამატეთ ეს ხაზი

            HttpListener? listener = null;

            try
            {
                // 1. ვიწყებთ ლოკალურ ვებ სერვერს კოდის მისაღებად
                listener = new HttpListener();
                listener.Prefixes.Add(RedirectUri + "/");
                listener.Start();

                // 2. ვხსნით ბრაუზერს ავტორიზაციისთვის
                var psi = new ProcessStartInfo
                {
                    FileName = authUrl,
                    UseShellExecute = true
                };
                Process.Start(psi);

                // 3. ვიწყებთ ლოდინს, სანამ Google კოდს დააბრუნებს
                var context = await listener.GetContextAsync();
                var code = context.Request.QueryString["code"];

                // დაუყოვნებლივ ვაბრუნებთ პასუხს ბრაუზერს, სანამ HttpListener-ს გავთიშავთ
                string responseString = "<html><body><h1>Authorization DONE! შეგიძლიათ დახუროთ ეს გვერდი.</h1></body></html>";
                byte[] buffer = System.Text.Encoding.UTF8.GetBytes(responseString);
                context.Response.ContentType = "text/html; charset=UTF-8";

                context.Response.ContentLength64 = buffer.Length;
                context.Response.OutputStream.Write(buffer, 0, buffer.Length);
                context.Response.OutputStream.Close();

                // 4. ვცვლით კოდს ტოკენებზე
                var tokens = await ExchangeCodeForTokensAsync(code);
                string? accessToken = tokens["access_token"]?.ToString();
                string? refreshToken = tokens["refresh_token"]?.ToString();

                if (string.IsNullOrEmpty(accessToken))
                {
                    MessageBox.Show("ავტორიზაციის შეცდომა: Access Token ვერ იქნა მიღებული.");
                    return;
                }

                // 5. ვგზავნით ტოკენებს სერვერზე SignalR-ის გავლით
                await _hubConnection.InvokeAsync("SaveYTOAuthTokens", accessToken, refreshToken ?? "");

            }
            catch (Exception ex)
            {
                MessageBox.Show($"ავტორიზაციის შეცდომა: {ex.Message}");
            }
            finally
            {
                // დარწმუნდით, რომ listener არსებობს და მუშაობს მის გათიშვამდე
                if (listener?.IsListening == true)
                {
                    try { listener.Stop(); } catch { }
                    try { listener.Close(); } catch { }
                }
            }


        }
        private async Task<JObject> ExchangeCodeForTokensAsync(string code)
        {
            var tokenUrl = "https://www.googleapis.com/oauth2/v4/token";

            //ClientId = _config["YTVotingSettings:client_id"] ?? "";
            //RedirectUri = _config["YTVotingSettings:redirect_uris"] ?? "";
            //ClientSecret = _config["YTVotingSettings:client_secret"] ?? "";



            var tokenRequestContent = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("code", code),
                new KeyValuePair<string, string>("client_id", ClientId),
                new KeyValuePair<string, string>("client_secret", ClientSecret),
                new KeyValuePair<string, string>("redirect_uri", RedirectUri),
                new KeyValuePair<string, string>("grant_type", "authorization_code")
            });

            try
            {
                // ვქმნით HttpClient-ის ინსტანციას
                using var client = new HttpClient();

                // ვაგზავნით POST მოთხოვნას
                var response = await client.PostAsync(tokenUrl, tokenRequestContent);

                // ვამოწმებთ წარმატებულ პასუხს
                response.EnsureSuccessStatusCode();

                var responseContent = await response.Content.ReadAsStringAsync();
                return JObject.Parse(responseContent);
            }
            catch (Exception ex)
            {
                throw new Exception($"შეცდომა ტოკენების მიღებისას: {ex.Message}");
            }

        }
        private async void btn_StartAudienceVoting_Click(object sender, EventArgs e)
        {
            var message = new VoteRequestMessage()
            {
                IsVotingActive = true,
                Duration = TimeSpan.FromSeconds((int)AudienceCountDownDuration.Value)
            };

            await _hubConnection.InvokeAsync("StartVoting", message);
        }





        private async void btn_StopAudienceVoting_Click(object sender, EventArgs e)
        {

            var message = new VoteRequestMessage()
            {
                IsVotingActive = false,
                Duration = TimeSpan.FromSeconds((int)AudienceCountDownDuration.Value)
            };

            await _hubConnection.InvokeAsync("StopVoting", message);
        }



        private async void btn_ConnectLightDevice_Click_1(object sender, EventArgs e)
        {
            var button = sender as System.Windows.Forms.Button;
            if (button == null)
                return;
            if (button.Text == "DMX ON")
            {
                AppendLog($"[WinForms UI] MIDI ON");
                await _hubConnection.InvokeAsync("ConnectMidiDevice");
                button.Text = "DMX OFF";
            }
            else
            {
                AppendLog($"[WinForms UI] MIDI OFF");
                button.Text = "DMX ON";
                await _hubConnection.InvokeAsync("DisconnectMidiDevice");
            }


        }



        private void tableLayoutPanel2_Paint(object sender, PaintEventArgs e)
        {

        }



        private async void btn_ytVotingOnOFF_Click(object sender, EventArgs e)
        {
            var button = sender as System.Windows.Forms.Button;
            if (button == null)
                return;

            var message = new VoteRequestMessage();
            message.Duration = TimeSpan.FromSeconds((int)AudienceCountDownDuration.Value);
            if (button.BackColor == Color.LightCoral)
            {
                message.IsVotingActive = true;
                button.BackColor = Color.LightGreen;
            }
            else
            {
                message.IsVotingActive = false;
                button.BackColor = Color.LightCoral;
            }

            await _hubConnection.InvokeAsync("StartVoting", message);
        }



        private async void BtnLoadFullQuestion_Click(object? sender, EventArgs? e)
        {
            var result = await _hubConnection.InvokeAsync<OperationResult>("CGLoadTemplate", CGTemplateEnums.QuestionFull);
            AppendLog($"[WinForms UI] <- Hub QuestionFull - ის ჩატვირთვა: {result.Message}");
        }

        private async void BtnLoadLeaderBoard_Click(object sender, EventArgs e)
        {
            var result = await _hubConnection.InvokeAsync<OperationResult>("CGLoadTemplate", CGTemplateEnums.LeaderBoard);
            AppendLog($"[WinForms UI] <- Hub LeaderBoard - ის ჩატვირთვა: {result.Message}");
        }

        private async void BtnLoadLowerQuestion_Click(object sender, EventArgs e)
        {
            var result = await _hubConnection.InvokeAsync<OperationResult>("CGLoadTemplate", CGTemplateEnums.QuestionLower);
            AppendLog($"[WinForms UI] <- Hub QuestionLower - ის ჩატვირთვა: {result.Message}");

        }

        private async void BtnLoadCountDown_Click(object sender, EventArgs e)
        {
            var result = await _hubConnection.InvokeAsync<OperationResult>("CGLoadTemplate", CGTemplateEnums.CountDown);
            AppendLog($"[WinForms UI] <- Hub CountDown - ის ჩატვირთვა: {result.Message}");
        }

        private async void btnLoadBackGround_Click(object sender, EventArgs e)
        {

            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "mp4 files (*.mp4)|*.mp4|All files (*.*)|*.*";
                openFileDialog.FilterIndex = 1;
                openFileDialog.InitialDirectory = "d://Masala//";
                openFileDialog.RestoreDirectory = true;

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {

                    // Read the content of the selected file
                    string fileContent = await System.IO.File.ReadAllTextAsync(openFileDialog.FileName);

                    await _hubConnection.InvokeAsync("CGPlayClip", 2, 1, openFileDialog.FileName);

                    //await _hubConnection.InvokeAsync("CGLoadTemplate", CGTemplateEnums.YTVote);
                }
            }
        }

        private async void BtnLoadYutubeVote_Click(object sender, EventArgs e)
        {
            await _hubConnection.InvokeAsync("CGLoadTemplate", CGTemplateEnums.YTVote);
        }

        private async void btn_SendMidiNote_Click(object sender, EventArgs e)
        {



            int.TryParse(tBox_MidiNote.Text, out int Notenumber);
            int.TryParse(tBox_MidiVelocity.Text, out int Velocity);


            await _hubConnection.InvokeAsync("SendMIDInote", Notenumber, Velocity);
            AppendLog($"[WinForms UI] <- Hub MIDI Sent");
        }

        private async void btnUuupsAnswer_Click(object sender, EventArgs e)
        {
            await _hubConnection.InvokeAsync("Uuups", _lastAction);
        }

        private async void btnPrepareNext_Click(object sender, EventArgs e)
        {
            await _hubConnection.InvokeAsync("CGClearPlayClip", CGTemplateEnums.QuestionVideo);
            await _hubConnection.InvokeAsync("CGWSClearChannelLayer", CGTemplateEnums.QuestionFull);
            await _hubConnection.InvokeAsync("CGWSClearChannelLayer", CGTemplateEnums.QuestionLower);
        }

        private void tabGameModes_Selecting(object sender, TabControlCancelEventArgs e)
        {
            e.Cancel = false;
            var currGameMode = (GameMode)cmbCountDownMode.SelectedItem;
            if (e.TabPage == tabControl1.TabPages[0] && currGameMode == GameMode.Round1)
            {
                e.Cancel = true;
            }
            else if (e.TabPage == tabControl1.TabPages[1] && currGameMode == GameMode.Round2)
            {
                e.Cancel = true;
            }
            if (e.TabPage == tabControl1.TabPages[2] && currGameMode == GameMode.Round3)
            {
                e.Cancel = true;
            }
            else if (e.TabPage == tabControl1.TabPages[3] && currGameMode == GameMode.RapidMode)
            {
                e.Cancel = true;
            }



        }

        private void SetCurrentPlayer(string contestant)
        {
            if (dgvContestants.DataSource is not List<Player> allPlayers)
                return;

            var player = allPlayers.FirstOrDefault(p => p.Name == contestant);
            if (player == null)
                return;

            // Clear all selections first
            dgvContestants.ClearSelection();

            _currentPlayerId = player.ConnectionId;

            // Find and select the matching row
            foreach (DataGridViewRow row in dgvContestants.Rows)
            {
                if (row.Cells["ConnectionId"].Value?.ToString() == player.ConnectionId)
                {
                    row.Selected = true;
                    //dgvContestants.CurrentCell = row.Cells[3]; // optional – focus move
                    break;
                }
            }

        }

        private void button2_Click_1(object sender, EventArgs e)
        {
            SetCurrentPlayer("Player1");
        }


        private void button6_Click(object sender, EventArgs e)
        {
            SetCurrentPlayer("Player2");
        }

        private void dgvContestants_CellMouseEnter(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                // Example: Combine values from multiple cells for a detailed tooltip.
                string tooltipText = $"ID: {dgvContestants.Rows[e.RowIndex].Cells[0].Value}\n" +
                                     $"IP: {dgvContestants.Rows[e.RowIndex].Cells[1].Value}\n" +
                                     $"Name: {dgvContestants.Rows[e.RowIndex].Cells[3].Value}\n";

                // Display the tooltip for the entire row.
                toolTipDgvContestant.SetToolTip(dgvContestants, tooltipText);
            }
        }

        private void dgvContestants_CellMouseLeave(object sender, DataGridViewCellEventArgs e)
        {
            toolTipDgvContestant.Hide(dgvContestants);
        }

        private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

        }

        private void btn_TitleShowHide_Click(object sender, EventArgs e)
        {

            var curTitle = new TitleDataModel()
            {
                Status = "show",
                BreakingNews = "სპორტული ამბები",
                Headline = "საქართველო ევროპის ჩემპიონია",
                SecondLine = "ისტორიული გამარჯვება"
            };

            if (btn_TitleShowHide.Text == "Show Title")
            {
                curTitle.Status = "show";
                btn_TitleShowHide.Text = "Hide Title";
                btn_TitleShowHide.BackColor = Color.LightGreen;
                _ = _hubConnection.InvokeAsync("CGSWToggleTitle", curTitle, true);
            }
            else
            {
                curTitle.Status = "hide";
                btn_TitleShowHide.Text = "Show Title";
                btn_TitleShowHide.BackColor = Color.LightCoral;
                _ = _hubConnection.InvokeAsync("CGSWToggleTitle", curTitle, false);
            }
        }

        
        private void button3_Click_1(object sender, EventArgs e)
        {
            var curTitle = new TitleDataModel()
            {
                Status = "nextSecondLine",
                BreakingNews = "სპორტული ამბები",
                Headline = "საქართველო ევროპის ჩემპიონია",
                SecondLine = "ისტორიული გამარჯვება Second line 1"
            };


            if (curTitle.SecondLine == "ისტორიული გამარჯვება Second line 1")
                curTitle.SecondLine = "ისტორიული გამარჯვება Second line 2";
            else
                curTitle.SecondLine = "ისტორიული გამარჯვება Second line 1";



                _ = _hubConnection.InvokeAsync("CGSWToggleTitle", curTitle, true);

        }
    }
}
