//using CasparCg.AmcpClient.Commands.Cg;
//using CasparCg.AmcpClient.Commands.Query.Common;
using GameController.Shared.Enums;
using GameController.Shared.Models;
using GameController.Shared.Models.Connection;
using GameController.Shared.Models.YouTube;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection.Emit;
using System.Runtime;
using System.Threading.Channels;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Formats.Asn1.AsnWriter;
using Application = System.Windows.Forms.Application;


namespace GameShowCtrl
{
	public partial class MnForm : Form
	{






		/// <summary>
		/// /
		/// 
		/// </summary>


		private readonly HubConnection _hubConnection;
		private DateTime _countdownEndTime;
		private ConcurrentDictionary<string, string> _playerNamesToIds = new ConcurrentDictionary<string, string>();

		private readonly string _serverBaseUrl; // ახალი ცვლადი
		private readonly IConfiguration _config; // ახალი ცვლადი კონფიგურაციისთვის

		private readonly ILogger<MnForm> _logger;

		private string ClientId = "თქვენი_Client_ID"; // ჩაანაცვლეთ თქვენი Client ID-ით
		private string ClientSecret = "თქვენი_Client_ID"; // ჩაანაცვლეთ თქვენი Client ID-ით
		private string RedirectUri = "http://localhost:5001/auth"; // უნდა ემთხვეოდეს Google-ის კონფიგურაციას
		private HttpListener? _listener;

		private ConcurrentDictionary<string, string> _activeAudienceMembers = new ConcurrentDictionary<string, string>();

		private LastAction _lastAction = LastAction.None;
		public MnForm()
		{
			InitializeComponent();
			cmbCountdownMode.DataSource = Enum.GetValues(typeof(GameMode));

			_config = new ConfigurationBuilder()
			.SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
			.AddJsonFile("appsettings.UI.json")
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

		}

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


		private async Task LoadInitialTemplates()
		{
			if (_hubConnection.State != HubConnectionState.Connected)
			{

				AppendLog($"[WinForms UI]\t{"კავშირი სერვერთან არ არის აქტიური, ვერ ხერხდება საწყისი ტემპლეტების ჩატვირთვა."}");
				return;
			}

			try
			{
				AppendLog($"[WinForms UI]\tიწყება CG თემლეიტების ჩატვირთვა");



				var resultQF = await _hubConnection.InvokeAsync<OperationResult>("CGLoadTemplate", CGTemplateEnums.QuestionFull);
				AppendLog($"[WinForms UI] <- Hub QuestionFull - ის ჩატვირთვა: {resultQF.Message}");
				var resultLB = await _hubConnection.InvokeAsync<OperationResult>("CGLoadTemplate", CGTemplateEnums.LeaderBoard);
				AppendLog($"[WinForms UI] <- Hub LeaderBoard - ის ჩატვირთვა: {resultLB.Message}");
				var resultQL = await _hubConnection.InvokeAsync<OperationResult>("CGLoadTemplate", CGTemplateEnums.QuestionLower);
				AppendLog($"[WinForms UI] <- Hub QuestionLower - ის ჩატვირთვა: {resultQL.Message}");
				var resultCD = await _hubConnection.InvokeAsync<OperationResult>("CGLoadTemplate", CGTemplateEnums.Countdown);
				AppendLog($"[WinForms UI] <- Hub Countdown - ის ჩატვირთვა: {resultCD.Message}");
				var resultYT = await _hubConnection.InvokeAsync<OperationResult>("CGLoadTemplate", CGTemplateEnums.YTVote);
				AppendLog($"[WinForms UI] <- Hub YTVote - ის ჩატვირთვა: {resultYT.Message}");
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
				AppendLog($"[WinForms UI] <- Hub\tReceiveRegistrationStatus");
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

			_hubConnection.On<long>("ReceiveCountdown", async (endTimestamp) =>
			{
				AppendLog($"[WinForms UI] <- Hub\tReceiveCountdown");
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
				await _hubConnection.InvokeAsync("CGWSCountdown", CGTemplateEnums.Countdown.ToString(), (int)CountdownDuration.Value, CountdownStopMode.Start.ToString(), endTimestamp);

			});

			_hubConnection.On<string>("StopCountdown", async (mode) =>
			{
				AppendLog($"[WinForms UI] <- Hub\tStopCountdown");
				if (this.InvokeRequired)
				{
					this.Invoke(new Action(async () =>
					{
						countdownTimer.Stop();
						if (mode == CountdownStopMode.Reset.ToString())
						{
							await _hubConnection.InvokeAsync("CGWSCountdown", CGTemplateEnums.Countdown.ToString(), (int)CountdownDuration.Value, CountdownStopMode.Reset, 0);
							lblCountdown.Text = "";
						}

						else if (mode == CountdownStopMode.Pause.ToString())
						{
							await _hubConnection.InvokeAsync("CGWSCountdown", CGTemplateEnums.Countdown.ToString(), (int)CountdownDuration.Value, CountdownStopMode.Pause, 0);
							lblCountdown.Text = lblCountdown.Text;
						}
						else
						{
							lblCountdown.Text = "დრო ამოოიწურა!";
							await _hubConnection.InvokeAsync("CGWSCountdown", CGTemplateEnums.Countdown.ToString(), (int)CountdownDuration.Value, CountdownStopMode.Reset, 0);
						}


					}));
				}
				else
				{
					countdownTimer.Stop();
					if (mode == CountdownStopMode.Reset.ToString())
						lblCountdown.Text = "0";
					else if (mode == CountdownStopMode.Pause.ToString())
						lblCountdown.Text = lblCountdown.Text;
					else
						lblCountdown.Text = "დრო ამოოიწურა!";

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
							lblCountdown.Text = "0s";
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
						lblCountdown.Text = "0s";
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
							if ((GameMode)cmbCountdownMode.SelectedItem == GameMode.RapidMode)
								await _hubConnection.InvokeAsync("SendRapidFireQuestionFromUI", nextQuestion, cBoxDisableInput.Checked);
							//if ((GameMode)cmbCountdownMode.SelectedItem == GameMode.Round1)
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
		private void AppendChatBoxMessage(string message)
		{
			// Invoke საჭიროა, რადგან SignalR მუშაობს სხვა ნაკადზე, ხოლო UI-ის განახლება უნდა მოხდეს მთავარ ნაკადზე.
			if (this.InvokeRequired)
			{
				this.Invoke(new Action<string>(AppendChatBoxMessage), message);
			}
			else
			{
				textBoxLog.Text = $"{message}{Environment.NewLine}{textBoxLog.Text}";
			}
		}
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
			dgvContestants.Columns["ClientType"].Visible = false;
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



		private async void MnForm_Load(object sender, EventArgs e)
		{



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

						MessageBox.Show("Questions have been loaded successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
					}
					catch (Exception ex)
					{
						MessageBox.Show($"Failed to load questions from file: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
					}
				}
			}

		}






		private async void btnStartCountdown_Click(object sender, EventArgs e)
		{
			await _hubConnection.InvokeAsync("StartCountdown", 30);

		}

		private void countdownTimer_Tick(object? sender, EventArgs e)
		{

			var timeLeft = _countdownEndTime - DateTime.Now;



			if (timeLeft.TotalSeconds <= 0)

			{

				countdownTimer.Stop();

				lblCountdown.Text = "Time's Up!";

				return;

			}



			lblCountdown.Text = $"{(int)timeLeft.TotalSeconds}s";

		}

		private async void btnSendQuestion_Click(object sender, EventArgs e)
		{
			if (dgvQuestions.SelectedRows.Count > 0)
			{

				var selectedRow = dgvQuestions.SelectedRows[0];
				var question = (QuestionModel)selectedRow.DataBoundItem;
				bool disableInput = cBoxDisableInput.Checked; // Add a checkbox on the UI to control this



				int countdownDuration = (int)CountdownDuration.Value;


				var selectedMode = (GameMode)cmbCountdownMode.SelectedItem;

				var allPlayers = dgvContestants.DataSource as List<Player>;
				List<Player>? targetClients = new List<Player>();

				if (allPlayers != null)
				{
					targetClients = allPlayers.Where(p => p.IsInPlay).ToList();
				}

				if (targetClients.Count == 0)
				{
					targetClients = allPlayers;
				}

				if (targetClients == null || disableInput && (targetClients.Count != 1))
				{
					MessageBox.Show("Please select a One Player to Play with.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
					return;
				}

				await _hubConnection.InvokeAsync("UpdateScoresFromUIToMEM", targetClients);
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


			int rapidFireDuration = (int)CountdownDuration.Value; // 60-second countdown for the session


			await _hubConnection.InvokeAsync("UpdateScoresFromUIToMEM", targetClients);
			await _hubConnection.InvokeAsync("StartRapidFire", targetClients, rapidFireDuration, disableInput);


			await _hubConnection.InvokeAsync("SendRapidFireQuestionFromUI", startQuestion, disableInput);

		}

		private async void btnIncorrectAnswer_Click(object sender, EventArgs e)
		{



			await _hubConnection.InvokeAsync("OperatorConfirmAnswer", false);
			_lastAction = LastAction.Incorrect;
			//btn_R1CorrectAnswer.Enabled = false;
			//btnIncorrectAnswer.Enabled = false;
			//
			//if ((GameMode)cmbCountdownMode.SelectedItem == GameMode.Round1 && cBoxDisableInput.Checked)
			//{
			//	btn_R1PrepareNext.Visible = true;
			//}


		}

		private async void btnCorrectAnswer_Click(object sender, EventArgs e)
		{


			await _hubConnection.InvokeAsync("OperatorConfirmAnswer", true);


			_lastAction = LastAction.Correct;

			//btn_R1CorrectAnswer.Enabled = false;
			//btnIncorrectAnswer.Enabled = true;
			//if ((GameMode)cmbCountdownMode.SelectedItem == GameMode.Round1 && cBoxDisableInput.Checked)
			//{
			//	btn_R1PrepareNext.Visible = true;
			//}


		}

		private void cBoxDisableInput_CheckedChanged(object sender, EventArgs e)
		{
			return;

			if ((GameMode)cmbCountdownMode.SelectedItem == GameMode.Round1 && cBoxDisableInput.Checked)
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
			else if ((GameMode)cmbCountdownMode.SelectedItem == GameMode.Round2 && !cBoxDisableInput.Checked)
			{
				tabPageRound2.Enabled = true;
				tabPageRound1.Enabled = false;
				tabPageRound3.Enabled = false;
				tabPageRapidFire.Enabled = false;

			}
			else if ((GameMode)cmbCountdownMode.SelectedItem == GameMode.Round3 && !cBoxDisableInput.Checked)
			{
				tabPageRound3.Enabled = true;
				tabPageRound1.Enabled = false;
				tabPageRound2.Enabled = false;
				tabPageRapidFire.Enabled = false;

			}
			else if ((GameMode)cmbCountdownMode.SelectedItem == GameMode.RapidMode && !cBoxDisableInput.Checked)
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

		private async void btnTestCountdown_Click(object sender, EventArgs e)
		{

			await _hubConnection.InvokeAsync("CGEnsureTemplateLoadedAsync", CGTemplateEnums.Countdown.ToString(), 2, 13);

			//await _hubConnection.InvokeAsync("CGLoadTemplate", CGTemplateEnums.Countdown);

			var endTimestamp = DateTimeOffset.UtcNow.AddSeconds(60).ToUnixTimeMilliseconds();
			var jsonData = JsonConvert.SerializeObject(new { endTimestamp = endTimestamp });



		}

		private async void btnTestLeaderBoard_Click(object sender, EventArgs e)
		{
			//await _hubConnection.InvokeAsync("CGLoadTemplate", CGTemplateEnums.LeaderBoard);
			await _hubConnection.InvokeAsync("CGStartCountdown", 60);

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
			await _hubConnection.InvokeAsync("CGClearChannel", CGTemplateEnums.Countdown);
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
			await _hubConnection.InvokeAsync("CGLoadTemplate", CGTemplateEnums.Countdown);
			await Task.Delay(500);
		}

		private async void button2_Click(object sender, EventArgs e)
		{
			var durationSeconds = (int)CountdownDuration.Value;
			var endTimestamp = DateTimeOffset.UtcNow.AddSeconds(durationSeconds).ToUnixTimeMilliseconds();
			await _hubConnection.InvokeAsync("CGWSCountdown", CGTemplateEnums.Countdown.ToString(), (int)CountdownDuration.Value, CountdownStopMode.Start, endTimestamp);

		}

		private async void button3_Click(object sender, EventArgs e)
		{
			var durationSeconds = (int)CountdownDuration.Value;
			var endTimestamp = DateTimeOffset.UtcNow.AddSeconds(durationSeconds).ToUnixTimeMilliseconds();
			await _hubConnection.InvokeAsync("CGWSCountdown", CGTemplateEnums.Countdown.ToString(), (int)CountdownDuration.Value, CountdownStopMode.Pause, endTimestamp);

		}

		private async void button5_Click(object sender, EventArgs e)
		{
			var durationSeconds = (int)CountdownDuration.Value;
			var endTimestamp = DateTimeOffset.UtcNow.AddSeconds(durationSeconds).ToUnixTimeMilliseconds();
			await _hubConnection.InvokeAsync("CGWSCountdown", CGTemplateEnums.Countdown.ToString(), (int)CountdownDuration.Value, CountdownStopMode.Resume, endTimestamp);
		}

		private async void button4_Click(object sender, EventArgs e)
		{
			var durationSeconds = (int)CountdownDuration.Value;
			var endTimestamp = DateTimeOffset.UtcNow.AddSeconds(durationSeconds).ToUnixTimeMilliseconds();
			await _hubConnection.InvokeAsync("CGWSCountdown", CGTemplateEnums.Countdown.ToString(), (int)CountdownDuration.Value, CountdownStopMode.Reset, endTimestamp);

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
			var button = sender as Button;
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
				Duration = TimeSpan.FromSeconds((int)AudienceCountdownDuration.Value)
			};

			await _hubConnection.InvokeAsync("StartVoting", message);
		}

		private async void btn_StartAudienceVoting_Click_(object sender, EventArgs e)
		{
			textBoxLog.Text = $"btn_StartAudienceVoting_Click{Environment.NewLine}{textBoxLog.Text}";




			var duration = (int)AudienceCountdownDuration.Value;
			using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(8));



			if (_hubConnection.State != HubConnectionState.Connected)
			{
				textBoxLog.Text = $"შეცდომა: SignalR კავშირი არ არის აქტიური. სცადეთ დაკავშირება.{Environment.NewLine}{textBoxLog.Text}";
				return;
			}
			else
			{
				textBoxLog.Text = $"SignalR კავშირი აქტიურია.{Environment.NewLine}{textBoxLog.Text}";
			}

			await _hubConnection.InvokeAsync("StartYTDataCollectingAsync");

			try
			{
				await _hubConnection.InvokeAsync("StartYTVoting");//, duration,cts.Token);			
				textBoxLog.Text = $"Audience Voting Started{Environment.NewLine}{textBoxLog.Text}";
			}
			catch (OperationCanceledException ocex)
			{
				textBoxLog.Text = $"StartVotingMode timed out {ocex.Message} {Environment.NewLine}{textBoxLog.Text}";
			}
			catch (Exception ex)
			{
				textBoxLog.Text = $"{ex.Message}{Environment.NewLine}{textBoxLog.Text}";
			}
		}



		private async void btn_StopAudienceVoting_Click(object sender, EventArgs e)
		{

			var message = new VoteRequestMessage()
			{
				IsVotingActive = false,
				Duration = TimeSpan.FromSeconds((int)AudienceCountdownDuration.Value)
			};

			await _hubConnection.InvokeAsync("StopVoting", message);
		}



		private async void btn_ConnectLightDevice_Click_1(object sender, EventArgs e)
		{
			var button = sender as Button;
			if (button == null)
				return;
			if (button.Text == "DMX ON")
			{
				await _hubConnection.InvokeAsync("ConnectMidiDevice");
				button.Text = "DMX OFF";
			}
			else
			{
				button.Text = "DMX ON";
				await _hubConnection.InvokeAsync("DisconnectMidiDevice");
			}


		}



		private void tableLayoutPanel2_Paint(object sender, PaintEventArgs e)
		{

		}



		private async void btn_ytVotingOnOFF_Click(object sender, EventArgs e)
		{
			var button = sender as Button;
			if (button == null)
				return;

			var message = new VoteRequestMessage();
			message.Duration = TimeSpan.FromSeconds((int)AudienceCountdownDuration.Value);
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



		private async void BtnLoadFullQuestion_Click(object sender, EventArgs e)
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
			var result = await _hubConnection.InvokeAsync<OperationResult>("CGLoadTemplate", CGTemplateEnums.Countdown);
			AppendLog($"[WinForms UI] <- Hub Countdown - ის ჩატვირთვა: {result.Message}");
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
			var currGameMode = (GameMode)cmbCountdownMode.SelectedItem;
			if (e.TabPage == tabControl1.TabPages[0] && currGameMode == GameMode.Round1)
			{
				e.Cancel = true;
			} else if (e.TabPage == tabControl1.TabPages[1] && currGameMode == GameMode.Round2)
			{
				e.Cancel = true;
			} if (e.TabPage == tabControl1.TabPages[2] && currGameMode == GameMode.Round3)
			{
				e.Cancel = true;			
			} else if (e.TabPage == tabControl1.TabPages[3] && currGameMode == GameMode.RapidMode)
			{
				e.Cancel = true;
			}



}
	}
}
