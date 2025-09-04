namespace GameShowCtrl
{
	partial class MnForm
	{
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing)
		{
			if (disposing && (components != null))
			{
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		#region Windows Form Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			components = new System.ComponentModel.Container();
			labelActiveConnections = new Label();
			labelRegisteredConnections = new Label();
			textBoxLog = new TextBox();
			PnlRapidFire = new TableLayoutPanel();
			tabControlMain = new TabControl();
			tPageTest = new TabPage();
			pnl_Test = new Panel();
			button9 = new Button();
			button6 = new Button();
			AudienceCountdownDuration = new NumericUpDown();
			btn_StopAudienceVoting = new Button();
			btn_StartAudienceVoting = new Button();
			votingUserListBox = new ListBox();
			tbx_YTVideoId = new TextBox();
			tBx_YTRedirectURI = new TextBox();
			tBx_YTClientID = new TextBox();
			txtMessageToSpecificClient = new TextBox();
			txtMessageToSend = new TextBox();
			btnNo = new Button();
			btn_YTAuth = new Button();
			tPageRapidFire = new TabPage();
			tableLayoutPanel1 = new TableLayoutPanel();
			tableLayoutPanel2 = new TableLayoutPanel();
			groupBox1 = new GroupBox();
			cBoxUseAudioControl = new CheckBox();
			cBoxUseLightControl = new CheckBox();
			cBoxDisableInput = new CheckBox();
			btn_ConnectLightDevice = new Button();
			flowLayoutPanel2 = new FlowLayoutPanel();
			rf_BtnLoadQuestions = new Button();
			btnShowLeaderboard = new Button();
			btnShowFinalResults = new Button();
			btnStoreResults = new Button();
			tableLayoutPanel3 = new TableLayoutPanel();
			panel2 = new Panel();
			btnSendQuestion = new Button();
			btnShowCorrect = new Button();
			btnIncorrectAnswer = new Button();
			rf_CBoxRapidFireMode = new ComboBox();
			btnCorrectAnswer = new Button();
			button4 = new Button();
			btnStartRapidFire = new Button();
			button5 = new Button();
			button2 = new Button();
			button3 = new Button();
			tableLayoutPanel5 = new TableLayoutPanel();
			dgvQuestions = new DataGridView();
			dgvContestants = new DataGridView();
			tableLayoutPanel4 = new TableLayoutPanel();
			groupBox2 = new GroupBox();
			richTextBox1 = new RichTextBox();
			flowLayoutPanel1 = new FlowLayoutPanel();
			btn_ytVotingOnOFF = new Button();
			button11 = new Button();
			button12 = new Button();
			flowLayoutPanelYutubeButtons = new FlowLayoutPanel();
			flowLayoutPanelLoads = new FlowLayoutPanel();
			BtnLoadFullQuestion = new Button();
			BtnLoadLowerQuestion = new Button();
			BtnLoadCountDown = new Button();
			BtnLoadLeaderBoard = new Button();
			button10 = new Button();
			btnClearGraphics = new Button();
			btnLoadBackGround = new Button();
			panel1 = new Panel();
			cmbCountdownMode = new ComboBox();
			lblPollingCountdown = new Label();
			lblCountdown = new Label();
			CountdownDuration = new NumericUpDown();
			listBoxClients = new ListBox();
			playerBindingSource = new BindingSource(components);
			countdownTimer = new System.Windows.Forms.Timer(components);
			PnlRapidFire.SuspendLayout();
			tabControlMain.SuspendLayout();
			tPageTest.SuspendLayout();
			pnl_Test.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)AudienceCountdownDuration).BeginInit();
			tPageRapidFire.SuspendLayout();
			tableLayoutPanel1.SuspendLayout();
			tableLayoutPanel2.SuspendLayout();
			groupBox1.SuspendLayout();
			flowLayoutPanel2.SuspendLayout();
			tableLayoutPanel3.SuspendLayout();
			panel2.SuspendLayout();
			tableLayoutPanel5.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)dgvQuestions).BeginInit();
			((System.ComponentModel.ISupportInitialize)dgvContestants).BeginInit();
			tableLayoutPanel4.SuspendLayout();
			groupBox2.SuspendLayout();
			flowLayoutPanel1.SuspendLayout();
			flowLayoutPanelLoads.SuspendLayout();
			panel1.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)CountdownDuration).BeginInit();
			((System.ComponentModel.ISupportInitialize)playerBindingSource).BeginInit();
			SuspendLayout();
			// 
			// labelActiveConnections
			// 
			labelActiveConnections.AutoSize = true;
			labelActiveConnections.Font = new Font("Microsoft Sans Serif", 12F, FontStyle.Regular, GraphicsUnit.Point, 0);
			labelActiveConnections.Location = new Point(1908, 15);
			labelActiveConnections.Name = "labelActiveConnections";
			labelActiveConnections.Size = new Size(64, 25);
			labelActiveConnections.TabIndex = 21;
			labelActiveConnections.Text = "label1";
			// 
			// labelRegisteredConnections
			// 
			labelRegisteredConnections.AutoSize = true;
			labelRegisteredConnections.Font = new Font("Microsoft Sans Serif", 12F, FontStyle.Regular, GraphicsUnit.Point, 0);
			labelRegisteredConnections.Location = new Point(1908, 78);
			labelRegisteredConnections.Name = "labelRegisteredConnections";
			labelRegisteredConnections.Size = new Size(64, 25);
			labelRegisteredConnections.TabIndex = 22;
			labelRegisteredConnections.Text = "label1";
			// 
			// textBoxLog
			// 
			textBoxLog.Dock = DockStyle.Fill;
			textBoxLog.Font = new Font("Microsoft Sans Serif", 12F, FontStyle.Regular, GraphicsUnit.Point, 0);
			textBoxLog.Location = new Point(3, 1077);
			textBoxLog.Margin = new Padding(3, 2, 3, 2);
			textBoxLog.Multiline = true;
			textBoxLog.Name = "textBoxLog";
			textBoxLog.ScrollBars = ScrollBars.Vertical;
			textBoxLog.Size = new Size(2449, 242);
			textBoxLog.TabIndex = 2;
			// 
			// PnlRapidFire
			// 
			PnlRapidFire.ColumnCount = 1;
			PnlRapidFire.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
			PnlRapidFire.Controls.Add(textBoxLog, 0, 3);
			PnlRapidFire.Controls.Add(tabControlMain, 0, 2);
			PnlRapidFire.Controls.Add(panel1, 0, 1);
			PnlRapidFire.Dock = DockStyle.Fill;
			PnlRapidFire.Enabled = false;
			PnlRapidFire.Location = new Point(0, 0);
			PnlRapidFire.Margin = new Padding(3, 2, 3, 2);
			PnlRapidFire.Name = "PnlRapidFire";
			PnlRapidFire.RowCount = 4;
			PnlRapidFire.RowStyles.Add(new RowStyle(SizeType.Percent, 7.158511F));
			PnlRapidFire.RowStyles.Add(new RowStyle(SizeType.Percent, 9.203799F));
			PnlRapidFire.RowStyles.Add(new RowStyle(SizeType.Percent, 65.1778946F));
			PnlRapidFire.RowStyles.Add(new RowStyle(SizeType.Percent, 18.5465565F));
			PnlRapidFire.RowStyles.Add(new RowStyle(SizeType.Absolute, 25F));
			PnlRapidFire.RowStyles.Add(new RowStyle(SizeType.Absolute, 25F));
			PnlRapidFire.RowStyles.Add(new RowStyle(SizeType.Absolute, 25F));
			PnlRapidFire.RowStyles.Add(new RowStyle(SizeType.Absolute, 25F));
			PnlRapidFire.RowStyles.Add(new RowStyle(SizeType.Absolute, 25F));
			PnlRapidFire.RowStyles.Add(new RowStyle(SizeType.Absolute, 25F));
			PnlRapidFire.Size = new Size(2455, 1321);
			PnlRapidFire.TabIndex = 40;
			// 
			// tabControlMain
			// 
			tabControlMain.Controls.Add(tPageTest);
			tabControlMain.Controls.Add(tPageRapidFire);
			tabControlMain.Dock = DockStyle.Fill;
			tabControlMain.Location = new Point(3, 217);
			tabControlMain.Margin = new Padding(3, 2, 3, 2);
			tabControlMain.Name = "tabControlMain";
			tabControlMain.SelectedIndex = 0;
			tabControlMain.Size = new Size(2449, 856);
			tabControlMain.TabIndex = 3;
			// 
			// tPageTest
			// 
			tPageTest.Controls.Add(pnl_Test);
			tPageTest.Location = new Point(4, 29);
			tPageTest.Margin = new Padding(3, 2, 3, 2);
			tPageTest.Name = "tPageTest";
			tPageTest.Padding = new Padding(3, 2, 3, 2);
			tPageTest.Size = new Size(2441, 823);
			tPageTest.TabIndex = 0;
			tPageTest.Text = "TEST";
			tPageTest.UseVisualStyleBackColor = true;
			// 
			// pnl_Test
			// 
			pnl_Test.Controls.Add(button9);
			pnl_Test.Controls.Add(button6);
			pnl_Test.Controls.Add(AudienceCountdownDuration);
			pnl_Test.Controls.Add(btn_StopAudienceVoting);
			pnl_Test.Controls.Add(btn_StartAudienceVoting);
			pnl_Test.Controls.Add(votingUserListBox);
			pnl_Test.Controls.Add(tbx_YTVideoId);
			pnl_Test.Controls.Add(tBx_YTRedirectURI);
			pnl_Test.Controls.Add(tBx_YTClientID);
			pnl_Test.Controls.Add(txtMessageToSpecificClient);
			pnl_Test.Controls.Add(txtMessageToSend);
			pnl_Test.Controls.Add(btnNo);
			pnl_Test.Controls.Add(btn_YTAuth);
			pnl_Test.Dock = DockStyle.Fill;
			pnl_Test.Location = new Point(3, 2);
			pnl_Test.Margin = new Padding(3, 2, 3, 2);
			pnl_Test.Name = "pnl_Test";
			pnl_Test.Size = new Size(2435, 819);
			pnl_Test.TabIndex = 47;
			// 
			// button9
			// 
			button9.Location = new Point(798, 107);
			button9.Name = "button9";
			button9.Size = new Size(135, 29);
			button9.TabIndex = 70;
			button9.Text = "button9";
			button9.UseVisualStyleBackColor = true;
			// 
			// button6
			// 
			button6.Location = new Point(291, 92);
			button6.Name = "button6";
			button6.Size = new Size(173, 68);
			button6.TabIndex = 69;
			button6.Text = "button6";
			button6.UseVisualStyleBackColor = true;
			button6.Click += button6_Click;
			// 
			// AudienceCountdownDuration
			// 
			AudienceCountdownDuration.Location = new Point(107, 270);
			AudienceCountdownDuration.Margin = new Padding(3, 2, 3, 2);
			AudienceCountdownDuration.Name = "AudienceCountdownDuration";
			AudienceCountdownDuration.Size = new Size(185, 27);
			AudienceCountdownDuration.TabIndex = 68;
			AudienceCountdownDuration.Value = new decimal(new int[] { 60, 0, 0, 0 });
			// 
			// btn_StopAudienceVoting
			// 
			btn_StopAudienceVoting.Location = new Point(867, 386);
			btn_StopAudienceVoting.Name = "btn_StopAudienceVoting";
			btn_StopAudienceVoting.Size = new Size(234, 94);
			btn_StopAudienceVoting.TabIndex = 67;
			btn_StopAudienceVoting.Text = "STOP Audience Voting";
			btn_StopAudienceVoting.UseVisualStyleBackColor = true;
			btn_StopAudienceVoting.Click += btn_StopAudienceVoting_Click;
			// 
			// btn_StartAudienceVoting
			// 
			btn_StartAudienceVoting.Location = new Point(867, 270);
			btn_StartAudienceVoting.Name = "btn_StartAudienceVoting";
			btn_StartAudienceVoting.Size = new Size(234, 94);
			btn_StartAudienceVoting.TabIndex = 66;
			btn_StartAudienceVoting.Text = "START Audience Voting";
			btn_StartAudienceVoting.UseVisualStyleBackColor = true;
			btn_StartAudienceVoting.Click += btn_StartAudienceVoting_Click;
			// 
			// votingUserListBox
			// 
			votingUserListBox.FormattingEnabled = true;
			votingUserListBox.Location = new Point(1260, 82);
			votingUserListBox.Name = "votingUserListBox";
			votingUserListBox.Size = new Size(392, 264);
			votingUserListBox.TabIndex = 65;
			// 
			// tbx_YTVideoId
			// 
			tbx_YTVideoId.Font = new Font("Segoe UI", 12F);
			tbx_YTVideoId.Location = new Point(838, 38);
			tbx_YTVideoId.Name = "tbx_YTVideoId";
			tbx_YTVideoId.Size = new Size(209, 34);
			tbx_YTVideoId.TabIndex = 64;
			// 
			// tBx_YTRedirectURI
			// 
			tBx_YTRedirectURI.Font = new Font("Segoe UI", 12F);
			tBx_YTRedirectURI.Location = new Point(517, 92);
			tBx_YTRedirectURI.Name = "tBx_YTRedirectURI";
			tBx_YTRedirectURI.Size = new Size(209, 34);
			tBx_YTRedirectURI.TabIndex = 63;
			// 
			// tBx_YTClientID
			// 
			tBx_YTClientID.Font = new Font("Segoe UI", 12F);
			tBx_YTClientID.Location = new Point(517, 38);
			tBx_YTClientID.Name = "tBx_YTClientID";
			tBx_YTClientID.Size = new Size(209, 34);
			tBx_YTClientID.TabIndex = 62;
			// 
			// txtMessageToSpecificClient
			// 
			txtMessageToSpecificClient.Font = new Font("Microsoft Sans Serif", 12F, FontStyle.Regular, GraphicsUnit.Point, 0);
			txtMessageToSpecificClient.Location = new Point(20, 562);
			txtMessageToSpecificClient.Margin = new Padding(3, 2, 3, 2);
			txtMessageToSpecificClient.Name = "txtMessageToSpecificClient";
			txtMessageToSpecificClient.Size = new Size(1171, 30);
			txtMessageToSpecificClient.TabIndex = 61;
			// 
			// txtMessageToSend
			// 
			txtMessageToSend.Font = new Font("Microsoft Sans Serif", 12F, FontStyle.Regular, GraphicsUnit.Point, 0);
			txtMessageToSend.Location = new Point(20, 485);
			txtMessageToSend.Margin = new Padding(3, 2, 3, 2);
			txtMessageToSend.Name = "txtMessageToSend";
			txtMessageToSend.Size = new Size(1171, 30);
			txtMessageToSend.TabIndex = 60;
			// 
			// btnNo
			// 
			btnNo.Font = new Font("Microsoft Sans Serif", 14F, FontStyle.Regular, GraphicsUnit.Point, 0);
			btnNo.Location = new Point(981, 190);
			btnNo.Margin = new Padding(3, 2, 3, 2);
			btnNo.Name = "btnNo";
			btnNo.Size = new Size(224, 62);
			btnNo.TabIndex = 56;
			btnNo.Text = "NO";
			btnNo.UseVisualStyleBackColor = true;
			btnNo.Visible = false;
			// 
			// btn_YTAuth
			// 
			btn_YTAuth.Font = new Font("Microsoft Sans Serif", 14F, FontStyle.Regular, GraphicsUnit.Point, 0);
			btn_YTAuth.Location = new Point(517, 220);
			btn_YTAuth.Margin = new Padding(3, 2, 3, 2);
			btn_YTAuth.Name = "btn_YTAuth";
			btn_YTAuth.Size = new Size(209, 108);
			btn_YTAuth.TabIndex = 55;
			btn_YTAuth.Text = "YouTube ავტორიზცია";
			btn_YTAuth.UseVisualStyleBackColor = true;
			btn_YTAuth.Click += btn_YTAuth_Click;
			// 
			// tPageRapidFire
			// 
			tPageRapidFire.Controls.Add(tableLayoutPanel1);
			tPageRapidFire.Location = new Point(4, 29);
			tPageRapidFire.Margin = new Padding(3, 2, 3, 2);
			tPageRapidFire.Name = "tPageRapidFire";
			tPageRapidFire.Padding = new Padding(3, 2, 3, 2);
			tPageRapidFire.Size = new Size(2441, 823);
			tPageRapidFire.TabIndex = 1;
			tPageRapidFire.Text = "RapidFire";
			tPageRapidFire.UseVisualStyleBackColor = true;
			tPageRapidFire.Click += tPageRapidFire_Click;
			// 
			// tableLayoutPanel1
			// 
			tableLayoutPanel1.ColumnCount = 3;
			tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20F));
			tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60F));
			tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20F));
			tableLayoutPanel1.Controls.Add(tableLayoutPanel2, 0, 0);
			tableLayoutPanel1.Controls.Add(tableLayoutPanel3, 1, 0);
			tableLayoutPanel1.Controls.Add(tableLayoutPanel4, 2, 0);
			tableLayoutPanel1.Dock = DockStyle.Fill;
			tableLayoutPanel1.Location = new Point(3, 2);
			tableLayoutPanel1.Name = "tableLayoutPanel1";
			tableLayoutPanel1.RowCount = 1;
			tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
			tableLayoutPanel1.Size = new Size(2435, 819);
			tableLayoutPanel1.TabIndex = 77;
			// 
			// tableLayoutPanel2
			// 
			tableLayoutPanel2.ColumnCount = 1;
			tableLayoutPanel2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
			tableLayoutPanel2.Controls.Add(groupBox1, 0, 6);
			tableLayoutPanel2.Controls.Add(btn_ConnectLightDevice, 0, 7);
			tableLayoutPanel2.Controls.Add(flowLayoutPanel2, 0, 5);
			tableLayoutPanel2.Dock = DockStyle.Fill;
			tableLayoutPanel2.Location = new Point(3, 3);
			tableLayoutPanel2.Name = "tableLayoutPanel2";
			tableLayoutPanel2.RowCount = 9;
			tableLayoutPanel2.RowStyles.Add(new RowStyle());
			tableLayoutPanel2.RowStyles.Add(new RowStyle());
			tableLayoutPanel2.RowStyles.Add(new RowStyle());
			tableLayoutPanel2.RowStyles.Add(new RowStyle());
			tableLayoutPanel2.RowStyles.Add(new RowStyle());
			tableLayoutPanel2.RowStyles.Add(new RowStyle(SizeType.Absolute, 280F));
			tableLayoutPanel2.RowStyles.Add(new RowStyle(SizeType.Absolute, 194F));
			tableLayoutPanel2.RowStyles.Add(new RowStyle(SizeType.Absolute, 200F));
			tableLayoutPanel2.RowStyles.Add(new RowStyle(SizeType.Absolute, 8F));
			tableLayoutPanel2.Size = new Size(481, 813);
			tableLayoutPanel2.TabIndex = 0;
			tableLayoutPanel2.Paint += tableLayoutPanel2_Paint;
			// 
			// groupBox1
			// 
			groupBox1.Controls.Add(cBoxUseAudioControl);
			groupBox1.Controls.Add(cBoxUseLightControl);
			groupBox1.Controls.Add(cBoxDisableInput);
			groupBox1.Dock = DockStyle.Top;
			groupBox1.Location = new Point(3, 283);
			groupBox1.Name = "groupBox1";
			groupBox1.Size = new Size(475, 174);
			groupBox1.TabIndex = 80;
			groupBox1.TabStop = false;
			// 
			// cBoxUseAudioControl
			// 
			cBoxUseAudioControl.AutoSize = true;
			cBoxUseAudioControl.Enabled = false;
			cBoxUseAudioControl.Location = new Point(6, 118);
			cBoxUseAudioControl.Name = "cBoxUseAudioControl";
			cBoxUseAudioControl.Size = new Size(142, 24);
			cBoxUseAudioControl.TabIndex = 75;
			cBoxUseAudioControl.Text = "აუდიო მართვა";
			cBoxUseAudioControl.UseVisualStyleBackColor = true;
			// 
			// cBoxUseLightControl
			// 
			cBoxUseLightControl.AutoSize = true;
			cBoxUseLightControl.Enabled = false;
			cBoxUseLightControl.Location = new Point(12, 75);
			cBoxUseLightControl.Name = "cBoxUseLightControl";
			cBoxUseLightControl.Size = new Size(169, 24);
			cBoxUseLightControl.TabIndex = 72;
			cBoxUseLightControl.Text = "განათების მართვა";
			cBoxUseLightControl.UseVisualStyleBackColor = true;
			// 
			// cBoxDisableInput
			// 
			cBoxDisableInput.AutoSize = true;
			cBoxDisableInput.Location = new Point(16, 38);
			cBoxDisableInput.Margin = new Padding(3, 4, 3, 4);
			cBoxDisableInput.Name = "cBoxDisableInput";
			cBoxDisableInput.Size = new Size(131, 24);
			cBoxDisableInput.TabIndex = 14;
			cBoxDisableInput.Text = "No Client Input";
			cBoxDisableInput.UseVisualStyleBackColor = true;
			cBoxDisableInput.Click += cBoxDisableInput_CheckedChanged;
			// 
			// btn_ConnectLightDevice
			// 
			btn_ConnectLightDevice.Location = new Point(3, 477);
			btn_ConnectLightDevice.Name = "btn_ConnectLightDevice";
			btn_ConnectLightDevice.Size = new Size(180, 60);
			btn_ConnectLightDevice.TabIndex = 79;
			btn_ConnectLightDevice.Text = "DMX ON";
			btn_ConnectLightDevice.UseVisualStyleBackColor = true;
			btn_ConnectLightDevice.Click += btn_ConnectLightDevice_Click_1;
			// 
			// flowLayoutPanel2
			// 
			flowLayoutPanel2.Controls.Add(rf_BtnLoadQuestions);
			flowLayoutPanel2.Controls.Add(btnShowLeaderboard);
			flowLayoutPanel2.Controls.Add(btnShowFinalResults);
			flowLayoutPanel2.Controls.Add(btnStoreResults);
			flowLayoutPanel2.Dock = DockStyle.Fill;
			flowLayoutPanel2.Location = new Point(3, 3);
			flowLayoutPanel2.Name = "flowLayoutPanel2";
			flowLayoutPanel2.Size = new Size(475, 274);
			flowLayoutPanel2.TabIndex = 81;
			// 
			// rf_BtnLoadQuestions
			// 
			rf_BtnLoadQuestions.Location = new Point(3, 2);
			rf_BtnLoadQuestions.Margin = new Padding(3, 2, 3, 2);
			rf_BtnLoadQuestions.Name = "rf_BtnLoadQuestions";
			rf_BtnLoadQuestions.Size = new Size(475, 75);
			rf_BtnLoadQuestions.TabIndex = 78;
			rf_BtnLoadQuestions.Text = "LOAD QUESTIONS";
			rf_BtnLoadQuestions.UseVisualStyleBackColor = true;
			rf_BtnLoadQuestions.Click += rf_BtnLoadQuestions_Click;
			// 
			// btnShowLeaderboard
			// 
			btnShowLeaderboard.Location = new Point(3, 82);
			btnShowLeaderboard.Name = "btnShowLeaderboard";
			btnShowLeaderboard.Size = new Size(475, 69);
			btnShowLeaderboard.TabIndex = 67;
			btnShowLeaderboard.Text = "Show Leaderboard";
			btnShowLeaderboard.UseVisualStyleBackColor = true;
			btnShowLeaderboard.Click += btnShowLeaderboard_Click_1;
			// 
			// btnShowFinalResults
			// 
			btnShowFinalResults.Location = new Point(3, 157);
			btnShowFinalResults.Name = "btnShowFinalResults";
			btnShowFinalResults.Size = new Size(475, 50);
			btnShowFinalResults.TabIndex = 70;
			btnShowFinalResults.Text = "Show Final Results";
			btnShowFinalResults.UseVisualStyleBackColor = true;
			btnShowFinalResults.Click += btnShowFinalResults_Click;
			// 
			// btnStoreResults
			// 
			btnStoreResults.Location = new Point(3, 213);
			btnStoreResults.Name = "btnStoreResults";
			btnStoreResults.Size = new Size(475, 50);
			btnStoreResults.TabIndex = 69;
			btnStoreResults.Text = "StoreResults";
			btnStoreResults.UseVisualStyleBackColor = true;
			btnStoreResults.Click += btnStoreResults_Click;
			// 
			// tableLayoutPanel3
			// 
			tableLayoutPanel3.ColumnCount = 1;
			tableLayoutPanel3.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
			tableLayoutPanel3.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 20F));
			tableLayoutPanel3.Controls.Add(panel2, 0, 1);
			tableLayoutPanel3.Controls.Add(tableLayoutPanel5, 0, 0);
			tableLayoutPanel3.Dock = DockStyle.Fill;
			tableLayoutPanel3.Location = new Point(490, 3);
			tableLayoutPanel3.Name = "tableLayoutPanel3";
			tableLayoutPanel3.RowCount = 3;
			tableLayoutPanel3.RowStyles.Add(new RowStyle(SizeType.Percent, 54.8585472F));
			tableLayoutPanel3.RowStyles.Add(new RowStyle(SizeType.Percent, 45.1414528F));
			tableLayoutPanel3.RowStyles.Add(new RowStyle(SizeType.Absolute, 20F));
			tableLayoutPanel3.Size = new Size(1455, 813);
			tableLayoutPanel3.TabIndex = 1;
			// 
			// panel2
			// 
			panel2.Controls.Add(btnSendQuestion);
			panel2.Controls.Add(btnShowCorrect);
			panel2.Controls.Add(btnIncorrectAnswer);
			panel2.Controls.Add(rf_CBoxRapidFireMode);
			panel2.Controls.Add(btnCorrectAnswer);
			panel2.Controls.Add(button4);
			panel2.Controls.Add(btnStartRapidFire);
			panel2.Controls.Add(button5);
			panel2.Controls.Add(button2);
			panel2.Controls.Add(button3);
			panel2.Dock = DockStyle.Top;
			panel2.Location = new Point(3, 438);
			panel2.Name = "panel2";
			panel2.Size = new Size(1449, 284);
			panel2.TabIndex = 4;
			// 
			// btnSendQuestion
			// 
			btnSendQuestion.Location = new Point(46, 119);
			btnSendQuestion.Name = "btnSendQuestion";
			btnSendQuestion.Size = new Size(210, 53);
			btnSendQuestion.TabIndex = 66;
			btnSendQuestion.Text = "Send Question";
			btnSendQuestion.UseVisualStyleBackColor = true;
			btnSendQuestion.Click += btnSendQuestion_Click;
			// 
			// btnShowCorrect
			// 
			btnShowCorrect.BackColor = Color.Tan;
			btnShowCorrect.Location = new Point(928, 118);
			btnShowCorrect.Name = "btnShowCorrect";
			btnShowCorrect.Size = new Size(166, 72);
			btnShowCorrect.TabIndex = 60;
			btnShowCorrect.Text = "Show Correct";
			btnShowCorrect.UseVisualStyleBackColor = false;
			btnShowCorrect.Click += btnShowCorrect_Click;
			// 
			// btnIncorrectAnswer
			// 
			btnIncorrectAnswer.BackColor = Color.Green;
			btnIncorrectAnswer.ForeColor = SystemColors.ControlText;
			btnIncorrectAnswer.Location = new Point(627, 18);
			btnIncorrectAnswer.Name = "btnIncorrectAnswer";
			btnIncorrectAnswer.Size = new Size(133, 81);
			btnIncorrectAnswer.TabIndex = 16;
			btnIncorrectAnswer.Text = "button2";
			btnIncorrectAnswer.UseVisualStyleBackColor = false;
			btnIncorrectAnswer.Visible = false;
			btnIncorrectAnswer.Click += btnIncorrectAnswer_Click;
			// 
			// rf_CBoxRapidFireMode
			// 
			rf_CBoxRapidFireMode.DropDownStyle = ComboBoxStyle.DropDownList;
			rf_CBoxRapidFireMode.FormattingEnabled = true;
			rf_CBoxRapidFireMode.Location = new Point(485, 208);
			rf_CBoxRapidFireMode.Margin = new Padding(3, 2, 3, 2);
			rf_CBoxRapidFireMode.Name = "rf_CBoxRapidFireMode";
			rf_CBoxRapidFireMode.Size = new Size(165, 28);
			rf_CBoxRapidFireMode.TabIndex = 1;
			// 
			// btnCorrectAnswer
			// 
			btnCorrectAnswer.BackColor = Color.Red;
			btnCorrectAnswer.ForeColor = SystemColors.ControlText;
			btnCorrectAnswer.Location = new Point(609, 134);
			btnCorrectAnswer.Name = "btnCorrectAnswer";
			btnCorrectAnswer.Size = new Size(122, 81);
			btnCorrectAnswer.TabIndex = 15;
			btnCorrectAnswer.Text = "CORRECT";
			btnCorrectAnswer.UseVisualStyleBackColor = false;
			btnCorrectAnswer.Visible = false;
			btnCorrectAnswer.Click += btnCorrectAnswer_Click;
			// 
			// button4
			// 
			button4.Location = new Point(385, 100);
			button4.Name = "button4";
			button4.Size = new Size(74, 72);
			button4.TabIndex = 64;
			button4.Text = "Stop";
			button4.UseVisualStyleBackColor = true;
			// 
			// btnStartRapidFire
			// 
			btnStartRapidFire.Location = new Point(46, 24);
			btnStartRapidFire.Name = "btnStartRapidFire";
			btnStartRapidFire.Size = new Size(210, 53);
			btnStartRapidFire.TabIndex = 14;
			btnStartRapidFire.Text = "StartRapidFire ";
			btnStartRapidFire.UseVisualStyleBackColor = true;
			btnStartRapidFire.Click += btnStartRapidFire_Click;
			// 
			// button5
			// 
			button5.Location = new Point(308, 181);
			button5.Name = "button5";
			button5.Size = new Size(74, 72);
			button5.TabIndex = 65;
			button5.Text = "Resume";
			button5.UseVisualStyleBackColor = true;
			button5.Click += button5_Click;
			// 
			// button2
			// 
			button2.Location = new Point(802, 36);
			button2.Name = "button2";
			button2.Size = new Size(74, 72);
			button2.TabIndex = 62;
			button2.Text = "Start";
			button2.UseVisualStyleBackColor = true;
			button2.Click += button2_Click;
			// 
			// button3
			// 
			button3.Location = new Point(802, 143);
			button3.Name = "button3";
			button3.Size = new Size(74, 72);
			button3.TabIndex = 63;
			button3.Text = "Pause";
			button3.UseVisualStyleBackColor = true;
			button3.Click += button3_Click;
			// 
			// tableLayoutPanel5
			// 
			tableLayoutPanel5.ColumnCount = 2;
			tableLayoutPanel5.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 58.3333321F));
			tableLayoutPanel5.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 41.6666679F));
			tableLayoutPanel5.Controls.Add(dgvQuestions, 0, 0);
			tableLayoutPanel5.Controls.Add(dgvContestants, 1, 0);
			tableLayoutPanel5.Dock = DockStyle.Fill;
			tableLayoutPanel5.Location = new Point(3, 3);
			tableLayoutPanel5.Name = "tableLayoutPanel5";
			tableLayoutPanel5.RowCount = 1;
			tableLayoutPanel5.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
			tableLayoutPanel5.Size = new Size(1449, 429);
			tableLayoutPanel5.TabIndex = 5;
			// 
			// dgvQuestions
			// 
			dgvQuestions.AllowUserToAddRows = false;
			dgvQuestions.AllowUserToDeleteRows = false;
			dgvQuestions.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
			dgvQuestions.Dock = DockStyle.Fill;
			dgvQuestions.Location = new Point(3, 2);
			dgvQuestions.Margin = new Padding(3, 2, 3, 2);
			dgvQuestions.Name = "dgvQuestions";
			dgvQuestions.RowHeadersWidth = 51;
			dgvQuestions.RowTemplate.Height = 24;
			dgvQuestions.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
			dgvQuestions.Size = new Size(839, 425);
			dgvQuestions.TabIndex = 3;
			// 
			// dgvContestants
			// 
			dgvContestants.AllowUserToAddRows = false;
			dgvContestants.AllowUserToDeleteRows = false;
			dgvContestants.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
			dgvContestants.Dock = DockStyle.Fill;
			dgvContestants.Location = new Point(848, 2);
			dgvContestants.Margin = new Padding(3, 2, 3, 2);
			dgvContestants.Name = "dgvContestants";
			dgvContestants.RowHeadersWidth = 51;
			dgvContestants.RowTemplate.Height = 24;
			dgvContestants.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
			dgvContestants.Size = new Size(598, 425);
			dgvContestants.TabIndex = 75;
			// 
			// tableLayoutPanel4
			// 
			tableLayoutPanel4.BackColor = Color.Gainsboro;
			tableLayoutPanel4.ColumnCount = 1;
			tableLayoutPanel4.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
			tableLayoutPanel4.Controls.Add(groupBox2, 0, 2);
			tableLayoutPanel4.Controls.Add(flowLayoutPanel1, 0, 0);
			tableLayoutPanel4.Controls.Add(flowLayoutPanelYutubeButtons, 0, 1);
			tableLayoutPanel4.Controls.Add(flowLayoutPanelLoads, 0, 3);
			tableLayoutPanel4.Dock = DockStyle.Fill;
			tableLayoutPanel4.Location = new Point(1951, 3);
			tableLayoutPanel4.Name = "tableLayoutPanel4";
			tableLayoutPanel4.RowCount = 5;
			tableLayoutPanel4.RowStyles.Add(new RowStyle());
			tableLayoutPanel4.RowStyles.Add(new RowStyle());
			tableLayoutPanel4.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
			tableLayoutPanel4.RowStyles.Add(new RowStyle());
			tableLayoutPanel4.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
			tableLayoutPanel4.Size = new Size(481, 813);
			tableLayoutPanel4.TabIndex = 2;
			// 
			// groupBox2
			// 
			groupBox2.Controls.Add(richTextBox1);
			groupBox2.Dock = DockStyle.Fill;
			groupBox2.Location = new Point(3, 291);
			groupBox2.Name = "groupBox2";
			groupBox2.Size = new Size(475, 110);
			groupBox2.TabIndex = 66;
			groupBox2.TabStop = false;
			groupBox2.Text = "ONLINE AUDIENCE";
			// 
			// richTextBox1
			// 
			richTextBox1.Location = new Point(64, 34);
			richTextBox1.Name = "richTextBox1";
			richTextBox1.Size = new Size(248, 100);
			richTextBox1.TabIndex = 0;
			richTextBox1.Text = "A:\nB:\nC:\nD:";
			// 
			// flowLayoutPanel1
			// 
			flowLayoutPanel1.Controls.Add(btn_ytVotingOnOFF);
			flowLayoutPanel1.Controls.Add(button11);
			flowLayoutPanel1.Controls.Add(button12);
			flowLayoutPanel1.Dock = DockStyle.Fill;
			flowLayoutPanel1.Location = new Point(3, 3);
			flowLayoutPanel1.Name = "flowLayoutPanel1";
			flowLayoutPanel1.Size = new Size(475, 276);
			flowLayoutPanel1.TabIndex = 81;
			// 
			// btn_ytVotingOnOFF
			// 
			btn_ytVotingOnOFF.BackColor = Color.LightCoral;
			btn_ytVotingOnOFF.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
			btn_ytVotingOnOFF.Location = new Point(3, 2);
			btn_ytVotingOnOFF.Margin = new Padding(3, 2, 3, 2);
			btn_ytVotingOnOFF.Name = "btn_ytVotingOnOFF";
			btn_ytVotingOnOFF.Size = new Size(196, 65);
			btn_ytVotingOnOFF.TabIndex = 7;
			btn_ytVotingOnOFF.Text = "YouTube VOTING";
			btn_ytVotingOnOFF.UseVisualStyleBackColor = false;
			btn_ytVotingOnOFF.Click += btn_ytVotingOnOFF_Click;
			// 
			// button11
			// 
			button11.Location = new Point(205, 2);
			button11.Margin = new Padding(3, 2, 3, 2);
			button11.Name = "button11";
			button11.Size = new Size(196, 65);
			button11.TabIndex = 58;
			button11.Text = "Get YT Vote Result";
			button11.UseVisualStyleBackColor = true;
			// 
			// button12
			// 
			button12.Location = new Point(3, 71);
			button12.Margin = new Padding(3, 2, 3, 2);
			button12.Name = "button12";
			button12.Size = new Size(398, 65);
			button12.TabIndex = 61;
			button12.Text = "Show Result";
			button12.UseVisualStyleBackColor = true;
			// 
			// flowLayoutPanelYutubeButtons
			// 
			flowLayoutPanelYutubeButtons.AutoSize = true;
			flowLayoutPanelYutubeButtons.BackColor = Color.LightCoral;
			flowLayoutPanelYutubeButtons.Dock = DockStyle.Top;
			flowLayoutPanelYutubeButtons.Location = new Point(3, 285);
			flowLayoutPanelYutubeButtons.Name = "flowLayoutPanelYutubeButtons";
			flowLayoutPanelYutubeButtons.Size = new Size(475, 0);
			flowLayoutPanelYutubeButtons.TabIndex = 79;
			// 
			// flowLayoutPanelLoads
			// 
			flowLayoutPanelLoads.Controls.Add(BtnLoadFullQuestion);
			flowLayoutPanelLoads.Controls.Add(BtnLoadLowerQuestion);
			flowLayoutPanelLoads.Controls.Add(BtnLoadCountDown);
			flowLayoutPanelLoads.Controls.Add(BtnLoadLeaderBoard);
			flowLayoutPanelLoads.Controls.Add(button10);
			flowLayoutPanelLoads.Controls.Add(btnClearGraphics);
			flowLayoutPanelLoads.Controls.Add(btnLoadBackGround);
			flowLayoutPanelLoads.Location = new Point(3, 407);
			flowLayoutPanelLoads.Name = "flowLayoutPanelLoads";
			flowLayoutPanelLoads.Size = new Size(475, 286);
			flowLayoutPanelLoads.TabIndex = 80;
			// 
			// BtnLoadFullQuestion
			// 
			BtnLoadFullQuestion.Location = new Point(3, 2);
			BtnLoadFullQuestion.Margin = new Padding(3, 2, 3, 2);
			BtnLoadFullQuestion.Name = "BtnLoadFullQuestion";
			BtnLoadFullQuestion.Size = new Size(196, 65);
			BtnLoadFullQuestion.TabIndex = 7;
			BtnLoadFullQuestion.Text = "Load Full Graphics";
			BtnLoadFullQuestion.UseVisualStyleBackColor = true;
			BtnLoadFullQuestion.Click += BtnLoadFullQuestion_Click;
			// 
			// BtnLoadLowerQuestion
			// 
			BtnLoadLowerQuestion.Location = new Point(205, 2);
			BtnLoadLowerQuestion.Margin = new Padding(3, 2, 3, 2);
			BtnLoadLowerQuestion.Name = "BtnLoadLowerQuestion";
			BtnLoadLowerQuestion.Size = new Size(196, 65);
			BtnLoadLowerQuestion.TabIndex = 58;
			BtnLoadLowerQuestion.Text = "Load Lower Graphics";
			BtnLoadLowerQuestion.UseVisualStyleBackColor = true;
			BtnLoadLowerQuestion.Click += BtnLoadLowerQuestion_Click;
			// 
			// BtnLoadCountDown
			// 
			BtnLoadCountDown.Location = new Point(3, 71);
			BtnLoadCountDown.Margin = new Padding(3, 2, 3, 2);
			BtnLoadCountDown.Name = "BtnLoadCountDown";
			BtnLoadCountDown.Size = new Size(196, 65);
			BtnLoadCountDown.TabIndex = 61;
			BtnLoadCountDown.Text = "Load CountDown";
			BtnLoadCountDown.UseVisualStyleBackColor = true;
			BtnLoadCountDown.Click += BtnLoadCountDown_Click;
			// 
			// BtnLoadLeaderBoard
			// 
			BtnLoadLeaderBoard.Location = new Point(205, 71);
			BtnLoadLeaderBoard.Margin = new Padding(3, 2, 3, 2);
			BtnLoadLeaderBoard.Name = "BtnLoadLeaderBoard";
			BtnLoadLeaderBoard.Size = new Size(196, 65);
			BtnLoadLeaderBoard.TabIndex = 66;
			BtnLoadLeaderBoard.Text = "Load Leaderboard";
			BtnLoadLeaderBoard.UseVisualStyleBackColor = true;
			BtnLoadLeaderBoard.Click += BtnLoadLeaderBoard_Click;
			// 
			// button10
			// 
			button10.Location = new Point(3, 140);
			button10.Margin = new Padding(3, 2, 3, 2);
			button10.Name = "button10";
			button10.Size = new Size(196, 68);
			button10.TabIndex = 76;
			button10.Text = "Load YT";
			button10.UseVisualStyleBackColor = true;
			button10.Click += button10_Click_1;
			// 
			// btnClearGraphics
			// 
			btnClearGraphics.Location = new Point(205, 141);
			btnClearGraphics.Name = "btnClearGraphics";
			btnClearGraphics.Size = new Size(214, 62);
			btnClearGraphics.TabIndex = 59;
			btnClearGraphics.Text = "Clear Graphics";
			btnClearGraphics.UseVisualStyleBackColor = true;
			btnClearGraphics.Click += btnClearGraphics_Click;
			// 
			// btnLoadBackGround
			// 
			btnLoadBackGround.Location = new Point(3, 213);
			btnLoadBackGround.Name = "btnLoadBackGround";
			btnLoadBackGround.Size = new Size(214, 62);
			btnLoadBackGround.TabIndex = 77;
			btnLoadBackGround.Text = "Load BackGround";
			btnLoadBackGround.UseVisualStyleBackColor = true;
			btnLoadBackGround.Click += btnLoadBackGround_Click;
			// 
			// panel1
			// 
			panel1.BackColor = SystemColors.ActiveBorder;
			panel1.Controls.Add(cmbCountdownMode);
			panel1.Controls.Add(lblPollingCountdown);
			panel1.Controls.Add(lblCountdown);
			panel1.Controls.Add(CountdownDuration);
			panel1.Controls.Add(listBoxClients);
			panel1.Dock = DockStyle.Fill;
			panel1.Location = new Point(3, 96);
			panel1.Margin = new Padding(3, 2, 3, 2);
			panel1.Name = "panel1";
			panel1.Size = new Size(2449, 117);
			panel1.TabIndex = 43;
			// 
			// cmbCountdownMode
			// 
			cmbCountdownMode.FormattingEnabled = true;
			cmbCountdownMode.Location = new Point(19, 63);
			cmbCountdownMode.Name = "cmbCountdownMode";
			cmbCountdownMode.Size = new Size(151, 28);
			cmbCountdownMode.TabIndex = 45;
			// 
			// lblPollingCountdown
			// 
			lblPollingCountdown.AutoSize = true;
			lblPollingCountdown.Font = new Font("Microsoft Sans Serif", 15F, FontStyle.Regular, GraphicsUnit.Point, 0);
			lblPollingCountdown.Location = new Point(233, 75);
			lblPollingCountdown.Name = "lblPollingCountdown";
			lblPollingCountdown.Size = new Size(81, 29);
			lblPollingCountdown.TabIndex = 39;
			lblPollingCountdown.Text = "label1";
			// 
			// lblCountdown
			// 
			lblCountdown.AutoSize = true;
			lblCountdown.Font = new Font("Microsoft Sans Serif", 25F, FontStyle.Regular, GraphicsUnit.Point, 0);
			lblCountdown.ForeColor = Color.Lime;
			lblCountdown.Location = new Point(233, 18);
			lblCountdown.Name = "lblCountdown";
			lblCountdown.Size = new Size(66, 48);
			lblCountdown.TabIndex = 37;
			lblCountdown.Text = "00";
			// 
			// CountdownDuration
			// 
			CountdownDuration.Location = new Point(19, 18);
			CountdownDuration.Margin = new Padding(3, 2, 3, 2);
			CountdownDuration.Name = "CountdownDuration";
			CountdownDuration.Size = new Size(185, 27);
			CountdownDuration.TabIndex = 42;
			CountdownDuration.Value = new decimal(new int[] { 60, 0, 0, 0 });
			// 
			// listBoxClients
			// 
			listBoxClients.Dock = DockStyle.Right;
			listBoxClients.FormattingEnabled = true;
			listBoxClients.Location = new Point(1798, 0);
			listBoxClients.Margin = new Padding(3, 2, 3, 2);
			listBoxClients.Name = "listBoxClients";
			listBoxClients.Size = new Size(651, 117);
			listBoxClients.TabIndex = 8;
			// 
			// countdownTimer
			// 
			countdownTimer.Tick += countdownTimer_Tick;
			// 
			// MnForm
			// 
			AutoScaleDimensions = new SizeF(8F, 20F);
			AutoScaleMode = AutoScaleMode.Font;
			ClientSize = new Size(2455, 1321);
			Controls.Add(PnlRapidFire);
			Controls.Add(labelRegisteredConnections);
			Controls.Add(labelActiveConnections);
			Margin = new Padding(3, 2, 3, 2);
			Name = "MnForm";
			Text = "Form1";
			WindowState = FormWindowState.Maximized;
			Load += MnForm_Load;
			PnlRapidFire.ResumeLayout(false);
			PnlRapidFire.PerformLayout();
			tabControlMain.ResumeLayout(false);
			tPageTest.ResumeLayout(false);
			pnl_Test.ResumeLayout(false);
			pnl_Test.PerformLayout();
			((System.ComponentModel.ISupportInitialize)AudienceCountdownDuration).EndInit();
			tPageRapidFire.ResumeLayout(false);
			tableLayoutPanel1.ResumeLayout(false);
			tableLayoutPanel2.ResumeLayout(false);
			groupBox1.ResumeLayout(false);
			groupBox1.PerformLayout();
			flowLayoutPanel2.ResumeLayout(false);
			tableLayoutPanel3.ResumeLayout(false);
			panel2.ResumeLayout(false);
			tableLayoutPanel5.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)dgvQuestions).EndInit();
			((System.ComponentModel.ISupportInitialize)dgvContestants).EndInit();
			tableLayoutPanel4.ResumeLayout(false);
			tableLayoutPanel4.PerformLayout();
			groupBox2.ResumeLayout(false);
			flowLayoutPanel1.ResumeLayout(false);
			flowLayoutPanelLoads.ResumeLayout(false);
			panel1.ResumeLayout(false);
			panel1.PerformLayout();
			((System.ComponentModel.ISupportInitialize)CountdownDuration).EndInit();
			((System.ComponentModel.ISupportInitialize)playerBindingSource).EndInit();
			ResumeLayout(false);
			PerformLayout();

		}

		#endregion
		private System.Windows.Forms.Label labelActiveConnections;
		private System.Windows.Forms.Label labelRegisteredConnections;
		private System.Windows.Forms.TextBox textBoxLog;
		private System.Windows.Forms.TableLayoutPanel PnlRapidFire;
		private System.Windows.Forms.TabControl tabControlMain;
		private System.Windows.Forms.TabPage tPageTest;
		private System.Windows.Forms.TabPage tPageRapidFire;
		private System.Windows.Forms.Label lblPollingCountdown;
		private System.Windows.Forms.Label lblCountdown;
		private System.Windows.Forms.NumericUpDown CountdownDuration;
		private System.Windows.Forms.Panel panel1;
		private System.Windows.Forms.ListBox listBoxClients;
		private System.Windows.Forms.ComboBox rf_CBoxRapidFireMode;
		private System.Windows.Forms.Panel pnl_Test;
		private System.Windows.Forms.Button btnLoadCG;
		private System.Windows.Forms.Button btnStartPolling;
		private System.Windows.Forms.Button btnDisconnectSelectedClient;
		private System.Windows.Forms.Button btnNo;
		private System.Windows.Forms.Button btn_YTAuth;
		public System.Windows.Forms.CheckBox chBoxClearScores;
		private System.Windows.Forms.Button btnSendToSelectedClient;
		private System.Windows.Forms.Button btnSendMessageToClients;
		private System.Windows.Forms.ComboBox cmbPollingMode;
		private System.Windows.Forms.TextBox txtMessageToSpecificClient;
		private System.Windows.Forms.TextBox txtMessageToSend;
		private Button btnStartCountdown;
		private System.Windows.Forms.Timer countdownTimer;
		private ComboBox cmbCountdownMode;
		private Button btnStartRapidFire;
		private Button btnIncorrectAnswer;
		private Button btnCorrectAnswer;
		private Button btnClearGraphics;
		private Button btnShowCorrect;
		private Button button4;
		private Button button3;
		private Button button2;
		private Button button5;
		private Button btnShowFinalResults;
		private Button btnStoreResults;
		private Button btnShowLeaderboard;
		public DataGridView dgvContestants;
		private BindingSource playerBindingSource;
		private TextBox tbx_YTVideoId;
		private TextBox tBx_YTRedirectURI;
		private TextBox tBx_YTClientID;
		private ListBox votingUserListBox;
		private Button btn_StopAudienceVoting;
		private Button btn_StartAudienceVoting;
		private NumericUpDown AudienceCountdownDuration;
		private Button button6;
		private Button button9;
		private TableLayoutPanel tableLayoutPanel1;
		private Button rf_BtnLoadQuestions;
		private TableLayoutPanel tableLayoutPanel2;
		private Button btn_ConnectLightDevice;
		private GroupBox groupBox1;
		private CheckBox cBoxUseAudioControl;
		private CheckBox cBoxUseLightControl;
		private CheckBox cBoxDisableInput;
		private TableLayoutPanel tableLayoutPanel3;
		public DataGridView dgvQuestions;
		private Panel panel2;
		private FlowLayoutPanel flowLayoutPanel2;
		private TableLayoutPanel tableLayoutPanel5;
		private TableLayoutPanel tableLayoutPanel4;
		private FlowLayoutPanel flowLayoutPanelLoads;
		private Button BtnLoadFullQuestion;
		private Button BtnLoadLowerQuestion;
		private Button BtnLoadCountDown;
		private Button BtnLoadLeaderBoard;
		private Button button10;
		private FlowLayoutPanel flowLayoutPanelYutubeButtons;
		private FlowLayoutPanel flowLayoutPanel1;
		private Button btn_ytVotingOnOFF;
		private Button button11;
		private Button button12;
		private GroupBox groupBox2;
		private RichTextBox richTextBox1;
		private Button btnSendQuestion;
		private Button btnLoadBackGround;
	}
}

