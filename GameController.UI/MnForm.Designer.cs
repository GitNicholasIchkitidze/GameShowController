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
			Button btn_R1CorrectAnswer;
			labelActiveConnections = new Label();
			labelRegisteredConnections = new Label();
			textBoxLog = new TextBox();
			PnlRapidFire = new TableLayoutPanel();
			tabControlMain = new TabControl();
			tPageTest = new TabPage();
			pnl_Test = new Panel();
			votingUserListBox = new ListBox();
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
			btnShowLeaderBoard = new Button();
			btnShowFinalResults = new Button();
			btnStoreResults = new Button();
			tableLayoutPanel3 = new TableLayoutPanel();
			panel2 = new Panel();
			tabGameModes = new TabControl();
			tabPageRound1 = new TabPage();
			btn_R1InCorrectAnswer = new Button();
			btn_R1PrepareNext = new Button();
			btn_R1UuupsAnswer = new Button();
			btn_SendQuestion = new Button();
			tabPageRound2 = new TabPage();
			tabPageRound3 = new TabPage();
			tabPageRapidFire = new TabPage();
			btnStartRapidFire = new Button();
			tabPage7 = new TabPage();
			btnShowCorrect = new Button();
			tableLayoutPanel5 = new TableLayoutPanel();
			dgvQuestions = new DataGridView();
			dgvContestants = new DataGridView();
			tableLayoutPanel4 = new TableLayoutPanel();
			flowLayoutPanelYutubeButtons = new FlowLayoutPanel();
			tabControl1 = new TabControl();
			tabPage1 = new TabPage();
			flowLayoutPanelLoads = new FlowLayoutPanel();
			BtnLoadFullQuestion = new Button();
			BtnLoadLowerQuestion = new Button();
			BtnLoadCountDown = new Button();
			BtnLoadLeaderBoard = new Button();
			BtnLoadYutubeVote = new Button();
			btnClearGraphics = new Button();
			btnLoadBackGround = new Button();
			tabPage2 = new TabPage();
			tbx_YTVideoId = new TextBox();
			AudienceCountdownDuration = new NumericUpDown();
			groupBox2 = new GroupBox();
			richTextBox1 = new RichTextBox();
			flowLayoutPanel1 = new FlowLayoutPanel();
			btn_ytVotingOnOFF = new Button();
			button11 = new Button();
			button12 = new Button();
			tabPage3 = new TabPage();
			tabPage4 = new TabPage();
			btn_SendMidiNote = new Button();
			tBox_MidiVelocity = new TextBox();
			tBox_MidiNote = new TextBox();
			button1 = new Button();
			panel1 = new Panel();
			cmbCountdownMode = new ComboBox();
			lblCountdown = new Label();
			CountdownDuration = new NumericUpDown();
			listBoxClients = new ListBox();
			playerBindingSource = new BindingSource(components);
			countdownTimer = new System.Windows.Forms.Timer(components);
			btn_R1CorrectAnswer = new Button();
			PnlRapidFire.SuspendLayout();
			tabControlMain.SuspendLayout();
			tPageTest.SuspendLayout();
			pnl_Test.SuspendLayout();
			tPageRapidFire.SuspendLayout();
			tableLayoutPanel1.SuspendLayout();
			tableLayoutPanel2.SuspendLayout();
			groupBox1.SuspendLayout();
			flowLayoutPanel2.SuspendLayout();
			tableLayoutPanel3.SuspendLayout();
			panel2.SuspendLayout();
			tabGameModes.SuspendLayout();
			tabPageRound1.SuspendLayout();
			tabPageRapidFire.SuspendLayout();
			tabPage7.SuspendLayout();
			tableLayoutPanel5.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)dgvQuestions).BeginInit();
			((System.ComponentModel.ISupportInitialize)dgvContestants).BeginInit();
			tableLayoutPanel4.SuspendLayout();
			tabControl1.SuspendLayout();
			tabPage1.SuspendLayout();
			flowLayoutPanelLoads.SuspendLayout();
			tabPage2.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)AudienceCountdownDuration).BeginInit();
			groupBox2.SuspendLayout();
			flowLayoutPanel1.SuspendLayout();
			tabPage4.SuspendLayout();
			panel1.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)CountdownDuration).BeginInit();
			((System.ComponentModel.ISupportInitialize)playerBindingSource).BeginInit();
			SuspendLayout();
			// 
			// btn_R1CorrectAnswer
			// 
			btn_R1CorrectAnswer.BackColor = Color.Green;
			btn_R1CorrectAnswer.Font = new Font("Segoe UI", 26F);
			btn_R1CorrectAnswer.ForeColor = SystemColors.ControlText;
			btn_R1CorrectAnswer.Location = new Point(253, 16);
			btn_R1CorrectAnswer.Margin = new Padding(3, 2, 3, 2);
			btn_R1CorrectAnswer.Name = "btn_R1CorrectAnswer";
			btn_R1CorrectAnswer.Size = new Size(318, 61);
			btn_R1CorrectAnswer.TabIndex = 68;
			btn_R1CorrectAnswer.Text = "YES";
			btn_R1CorrectAnswer.UseVisualStyleBackColor = false;
			btn_R1CorrectAnswer.Click += btnCorrectAnswer_Click;
			// 
			// labelActiveConnections
			// 
			labelActiveConnections.AutoSize = true;
			labelActiveConnections.Font = new Font("Microsoft Sans Serif", 12F, FontStyle.Regular, GraphicsUnit.Point, 0);
			labelActiveConnections.Location = new Point(1670, 11);
			labelActiveConnections.Name = "labelActiveConnections";
			labelActiveConnections.Size = new Size(51, 20);
			labelActiveConnections.TabIndex = 21;
			labelActiveConnections.Text = "label1";
			// 
			// labelRegisteredConnections
			// 
			labelRegisteredConnections.AutoSize = true;
			labelRegisteredConnections.Font = new Font("Microsoft Sans Serif", 12F, FontStyle.Regular, GraphicsUnit.Point, 0);
			labelRegisteredConnections.Location = new Point(1670, 58);
			labelRegisteredConnections.Name = "labelRegisteredConnections";
			labelRegisteredConnections.Size = new Size(51, 20);
			labelRegisteredConnections.TabIndex = 22;
			labelRegisteredConnections.Text = "label1";
			// 
			// textBoxLog
			// 
			textBoxLog.Dock = DockStyle.Fill;
			textBoxLog.Font = new Font("Microsoft Sans Serif", 12F, FontStyle.Regular, GraphicsUnit.Point, 0);
			textBoxLog.Location = new Point(3, 918);
			textBoxLog.Margin = new Padding(3, 2, 3, 2);
			textBoxLog.Multiline = true;
			textBoxLog.Name = "textBoxLog";
			textBoxLog.ScrollBars = ScrollBars.Vertical;
			textBoxLog.Size = new Size(2142, 206);
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
			PnlRapidFire.RowStyles.Add(new RowStyle(SizeType.Absolute, 19F));
			PnlRapidFire.RowStyles.Add(new RowStyle(SizeType.Absolute, 19F));
			PnlRapidFire.RowStyles.Add(new RowStyle(SizeType.Absolute, 19F));
			PnlRapidFire.RowStyles.Add(new RowStyle(SizeType.Absolute, 19F));
			PnlRapidFire.RowStyles.Add(new RowStyle(SizeType.Absolute, 19F));
			PnlRapidFire.RowStyles.Add(new RowStyle(SizeType.Absolute, 19F));
			PnlRapidFire.Size = new Size(2148, 1126);
			PnlRapidFire.TabIndex = 40;
			// 
			// tabControlMain
			// 
			tabControlMain.Controls.Add(tPageTest);
			tabControlMain.Controls.Add(tPageRapidFire);
			tabControlMain.Dock = DockStyle.Fill;
			tabControlMain.Location = new Point(3, 185);
			tabControlMain.Margin = new Padding(3, 2, 3, 2);
			tabControlMain.Name = "tabControlMain";
			tabControlMain.SelectedIndex = 0;
			tabControlMain.Size = new Size(2142, 729);
			tabControlMain.TabIndex = 3;
			// 
			// tPageTest
			// 
			tPageTest.Controls.Add(pnl_Test);
			tPageTest.Location = new Point(4, 24);
			tPageTest.Margin = new Padding(3, 2, 3, 2);
			tPageTest.Name = "tPageTest";
			tPageTest.Padding = new Padding(3, 2, 3, 2);
			tPageTest.Size = new Size(2134, 701);
			tPageTest.TabIndex = 0;
			tPageTest.Text = "TEST";
			tPageTest.UseVisualStyleBackColor = true;
			// 
			// pnl_Test
			// 
			pnl_Test.Controls.Add(votingUserListBox);
			pnl_Test.Controls.Add(btn_YTAuth);
			pnl_Test.Dock = DockStyle.Fill;
			pnl_Test.Location = new Point(3, 2);
			pnl_Test.Margin = new Padding(3, 2, 3, 2);
			pnl_Test.Name = "pnl_Test";
			pnl_Test.Size = new Size(2128, 697);
			pnl_Test.TabIndex = 47;
			// 
			// votingUserListBox
			// 
			votingUserListBox.FormattingEnabled = true;
			votingUserListBox.ItemHeight = 15;
			votingUserListBox.Location = new Point(1102, 62);
			votingUserListBox.Margin = new Padding(3, 2, 3, 2);
			votingUserListBox.Name = "votingUserListBox";
			votingUserListBox.Size = new Size(344, 199);
			votingUserListBox.TabIndex = 65;
			// 
			// btn_YTAuth
			// 
			btn_YTAuth.Font = new Font("Microsoft Sans Serif", 14F, FontStyle.Regular, GraphicsUnit.Point, 0);
			btn_YTAuth.Location = new Point(452, 165);
			btn_YTAuth.Margin = new Padding(3, 2, 3, 2);
			btn_YTAuth.Name = "btn_YTAuth";
			btn_YTAuth.Size = new Size(183, 81);
			btn_YTAuth.TabIndex = 55;
			btn_YTAuth.Text = "YouTube ავტორიზცია";
			btn_YTAuth.UseVisualStyleBackColor = true;
			btn_YTAuth.Click += btn_YTAuth_Click;
			// 
			// tPageRapidFire
			// 
			tPageRapidFire.Controls.Add(tableLayoutPanel1);
			tPageRapidFire.Location = new Point(4, 24);
			tPageRapidFire.Margin = new Padding(3, 2, 3, 2);
			tPageRapidFire.Name = "tPageRapidFire";
			tPageRapidFire.Padding = new Padding(3, 2, 3, 2);
			tPageRapidFire.Size = new Size(2134, 701);
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
			tableLayoutPanel1.Margin = new Padding(3, 2, 3, 2);
			tableLayoutPanel1.Name = "tableLayoutPanel1";
			tableLayoutPanel1.RowCount = 1;
			tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
			tableLayoutPanel1.Size = new Size(2128, 697);
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
			tableLayoutPanel2.Location = new Point(3, 2);
			tableLayoutPanel2.Margin = new Padding(3, 2, 3, 2);
			tableLayoutPanel2.Name = "tableLayoutPanel2";
			tableLayoutPanel2.RowCount = 9;
			tableLayoutPanel2.RowStyles.Add(new RowStyle());
			tableLayoutPanel2.RowStyles.Add(new RowStyle());
			tableLayoutPanel2.RowStyles.Add(new RowStyle());
			tableLayoutPanel2.RowStyles.Add(new RowStyle());
			tableLayoutPanel2.RowStyles.Add(new RowStyle());
			tableLayoutPanel2.RowStyles.Add(new RowStyle(SizeType.Absolute, 210F));
			tableLayoutPanel2.RowStyles.Add(new RowStyle(SizeType.Absolute, 146F));
			tableLayoutPanel2.RowStyles.Add(new RowStyle(SizeType.Absolute, 150F));
			tableLayoutPanel2.RowStyles.Add(new RowStyle(SizeType.Absolute, 6F));
			tableLayoutPanel2.Size = new Size(419, 693);
			tableLayoutPanel2.TabIndex = 0;
			tableLayoutPanel2.Paint += tableLayoutPanel2_Paint;
			// 
			// groupBox1
			// 
			groupBox1.Controls.Add(cBoxUseAudioControl);
			groupBox1.Controls.Add(cBoxUseLightControl);
			groupBox1.Controls.Add(cBoxDisableInput);
			groupBox1.Dock = DockStyle.Top;
			groupBox1.Location = new Point(3, 212);
			groupBox1.Margin = new Padding(3, 2, 3, 2);
			groupBox1.Name = "groupBox1";
			groupBox1.Padding = new Padding(3, 2, 3, 2);
			groupBox1.Size = new Size(413, 130);
			groupBox1.TabIndex = 80;
			groupBox1.TabStop = false;
			// 
			// cBoxUseAudioControl
			// 
			cBoxUseAudioControl.AutoSize = true;
			cBoxUseAudioControl.Enabled = false;
			cBoxUseAudioControl.Font = new Font("Segoe UI", 12F);
			cBoxUseAudioControl.Location = new Point(10, 86);
			cBoxUseAudioControl.Margin = new Padding(3, 2, 3, 2);
			cBoxUseAudioControl.Name = "cBoxUseAudioControl";
			cBoxUseAudioControl.Size = new Size(150, 25);
			cBoxUseAudioControl.TabIndex = 75;
			cBoxUseAudioControl.Text = "აუდიო მართვა";
			cBoxUseAudioControl.UseVisualStyleBackColor = true;
			// 
			// cBoxUseLightControl
			// 
			cBoxUseLightControl.AutoSize = true;
			cBoxUseLightControl.Enabled = false;
			cBoxUseLightControl.Font = new Font("Segoe UI", 12F);
			cBoxUseLightControl.Location = new Point(10, 56);
			cBoxUseLightControl.Margin = new Padding(3, 2, 3, 2);
			cBoxUseLightControl.Name = "cBoxUseLightControl";
			cBoxUseLightControl.Size = new Size(178, 25);
			cBoxUseLightControl.TabIndex = 72;
			cBoxUseLightControl.Text = "განათების მართვა";
			cBoxUseLightControl.UseVisualStyleBackColor = true;
			// 
			// cBoxDisableInput
			// 
			cBoxDisableInput.AutoSize = true;
			cBoxDisableInput.Font = new Font("Segoe UI", 12F);
			cBoxDisableInput.Location = new Point(10, 26);
			cBoxDisableInput.Name = "cBoxDisableInput";
			cBoxDisableInput.Size = new Size(134, 25);
			cBoxDisableInput.TabIndex = 14;
			cBoxDisableInput.Text = "No Client Input";
			cBoxDisableInput.UseVisualStyleBackColor = true;
			cBoxDisableInput.Click += cBoxDisableInput_CheckedChanged;
			// 
			// btn_ConnectLightDevice
			// 
			btn_ConnectLightDevice.Location = new Point(3, 358);
			btn_ConnectLightDevice.Margin = new Padding(3, 2, 3, 2);
			btn_ConnectLightDevice.Name = "btn_ConnectLightDevice";
			btn_ConnectLightDevice.Size = new Size(158, 45);
			btn_ConnectLightDevice.TabIndex = 79;
			btn_ConnectLightDevice.Text = "DMX ON";
			btn_ConnectLightDevice.UseVisualStyleBackColor = true;
			btn_ConnectLightDevice.Click += btn_ConnectLightDevice_Click_1;
			// 
			// flowLayoutPanel2
			// 
			flowLayoutPanel2.Controls.Add(rf_BtnLoadQuestions);
			flowLayoutPanel2.Controls.Add(btnShowLeaderBoard);
			flowLayoutPanel2.Controls.Add(btnShowFinalResults);
			flowLayoutPanel2.Controls.Add(btnStoreResults);
			flowLayoutPanel2.Dock = DockStyle.Fill;
			flowLayoutPanel2.Location = new Point(3, 2);
			flowLayoutPanel2.Margin = new Padding(3, 2, 3, 2);
			flowLayoutPanel2.Name = "flowLayoutPanel2";
			flowLayoutPanel2.Size = new Size(413, 206);
			flowLayoutPanel2.TabIndex = 81;
			// 
			// rf_BtnLoadQuestions
			// 
			rf_BtnLoadQuestions.Font = new Font("Segoe UI", 15F);
			rf_BtnLoadQuestions.Location = new Point(3, 2);
			rf_BtnLoadQuestions.Margin = new Padding(3, 2, 3, 2);
			rf_BtnLoadQuestions.Name = "rf_BtnLoadQuestions";
			rf_BtnLoadQuestions.Size = new Size(416, 56);
			rf_BtnLoadQuestions.TabIndex = 78;
			rf_BtnLoadQuestions.Text = "LOAD QUESTIONS";
			rf_BtnLoadQuestions.UseVisualStyleBackColor = true;
			rf_BtnLoadQuestions.Click += rf_BtnLoadQuestions_Click;
			// 
			// btnShowLeaderBoard
			// 
			btnShowLeaderBoard.Font = new Font("Segoe UI", 15F);
			btnShowLeaderBoard.Location = new Point(3, 62);
			btnShowLeaderBoard.Margin = new Padding(3, 2, 3, 2);
			btnShowLeaderBoard.Name = "btnShowLeaderBoard";
			btnShowLeaderBoard.Size = new Size(416, 52);
			btnShowLeaderBoard.TabIndex = 67;
			btnShowLeaderBoard.Text = "Show LeaderBoard";
			btnShowLeaderBoard.UseVisualStyleBackColor = true;
			btnShowLeaderBoard.Click += btnShowLeaderBoard_Click_1;
			// 
			// btnShowFinalResults
			// 
			btnShowFinalResults.Font = new Font("Segoe UI", 15F);
			btnShowFinalResults.Location = new Point(3, 118);
			btnShowFinalResults.Margin = new Padding(3, 2, 3, 2);
			btnShowFinalResults.Name = "btnShowFinalResults";
			btnShowFinalResults.Size = new Size(416, 38);
			btnShowFinalResults.TabIndex = 70;
			btnShowFinalResults.Text = "Show Final Results";
			btnShowFinalResults.UseVisualStyleBackColor = true;
			btnShowFinalResults.Click += btnShowFinalResults_Click;
			// 
			// btnStoreResults
			// 
			btnStoreResults.Font = new Font("Segoe UI", 15F);
			btnStoreResults.Location = new Point(3, 160);
			btnStoreResults.Margin = new Padding(3, 2, 3, 2);
			btnStoreResults.Name = "btnStoreResults";
			btnStoreResults.Size = new Size(416, 38);
			btnStoreResults.TabIndex = 69;
			btnStoreResults.Text = "StoreResults";
			btnStoreResults.UseVisualStyleBackColor = true;
			btnStoreResults.Click += btnStoreResults_Click;
			// 
			// tableLayoutPanel3
			// 
			tableLayoutPanel3.ColumnCount = 1;
			tableLayoutPanel3.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
			tableLayoutPanel3.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 18F));
			tableLayoutPanel3.Controls.Add(panel2, 0, 1);
			tableLayoutPanel3.Controls.Add(tableLayoutPanel5, 0, 0);
			tableLayoutPanel3.Dock = DockStyle.Fill;
			tableLayoutPanel3.Location = new Point(428, 2);
			tableLayoutPanel3.Margin = new Padding(3, 2, 3, 2);
			tableLayoutPanel3.Name = "tableLayoutPanel3";
			tableLayoutPanel3.RowCount = 3;
			tableLayoutPanel3.RowStyles.Add(new RowStyle(SizeType.Percent, 54.8585472F));
			tableLayoutPanel3.RowStyles.Add(new RowStyle(SizeType.Percent, 45.1414528F));
			tableLayoutPanel3.RowStyles.Add(new RowStyle(SizeType.Absolute, 15F));
			tableLayoutPanel3.Size = new Size(1270, 693);
			tableLayoutPanel3.TabIndex = 1;
			// 
			// panel2
			// 
			panel2.Controls.Add(tabGameModes);
			panel2.Dock = DockStyle.Top;
			panel2.Location = new Point(3, 373);
			panel2.Margin = new Padding(3, 2, 3, 2);
			panel2.Name = "panel2";
			panel2.Size = new Size(1264, 248);
			panel2.TabIndex = 4;
			// 
			// tabGameModes
			// 
			tabGameModes.Controls.Add(tabPageRound1);
			tabGameModes.Controls.Add(tabPageRound2);
			tabGameModes.Controls.Add(tabPageRound3);
			tabGameModes.Controls.Add(tabPageRapidFire);
			tabGameModes.Controls.Add(tabPage7);
			tabGameModes.Dock = DockStyle.Fill;
			tabGameModes.Location = new Point(0, 0);
			tabGameModes.Name = "tabGameModes";
			tabGameModes.SelectedIndex = 0;
			tabGameModes.Size = new Size(1264, 248);
			tabGameModes.TabIndex = 69;
			tabGameModes.Selecting += tabGameModes_Selecting;
			// 
			// tabPageRound1
			// 
			tabPageRound1.Controls.Add(btn_R1InCorrectAnswer);
			tabPageRound1.Controls.Add(btn_R1PrepareNext);
			tabPageRound1.Controls.Add(btn_R1UuupsAnswer);
			tabPageRound1.Controls.Add(btn_R1CorrectAnswer);
			tabPageRound1.Controls.Add(btn_SendQuestion);
			tabPageRound1.ForeColor = SystemColors.ControlText;
			tabPageRound1.Location = new Point(4, 24);
			tabPageRound1.Name = "tabPageRound1";
			tabPageRound1.Padding = new Padding(3);
			tabPageRound1.Size = new Size(1256, 220);
			tabPageRound1.TabIndex = 0;
			tabPageRound1.Text = "Round1";
			tabPageRound1.UseVisualStyleBackColor = true;
			// 
			// btn_R1InCorrectAnswer
			// 
			btn_R1InCorrectAnswer.BackColor = Color.OrangeRed;
			btn_R1InCorrectAnswer.Font = new Font("Segoe UI", 26F);
			btn_R1InCorrectAnswer.ForeColor = SystemColors.ControlText;
			btn_R1InCorrectAnswer.Location = new Point(577, 16);
			btn_R1InCorrectAnswer.Margin = new Padding(3, 2, 3, 2);
			btn_R1InCorrectAnswer.Name = "btn_R1InCorrectAnswer";
			btn_R1InCorrectAnswer.Size = new Size(318, 61);
			btn_R1InCorrectAnswer.TabIndex = 72;
			btn_R1InCorrectAnswer.Text = "Wrong";
			btn_R1InCorrectAnswer.UseVisualStyleBackColor = false;
			btn_R1InCorrectAnswer.Click += btnIncorrectAnswer_Click;
			// 
			// btn_R1PrepareNext
			// 
			btn_R1PrepareNext.BackColor = Color.DarkSeaGreen;
			btn_R1PrepareNext.Font = new Font("Segoe UI", 26F);
			btn_R1PrepareNext.ForeColor = SystemColors.ControlText;
			btn_R1PrepareNext.Location = new Point(253, 144);
			btn_R1PrepareNext.Margin = new Padding(3, 2, 3, 2);
			btn_R1PrepareNext.Name = "btn_R1PrepareNext";
			btn_R1PrepareNext.Size = new Size(642, 61);
			btn_R1PrepareNext.TabIndex = 71;
			btn_R1PrepareNext.Text = "Prepare Next";
			btn_R1PrepareNext.UseVisualStyleBackColor = false;
			btn_R1PrepareNext.Click += btnPrepareNext_Click;
			// 
			// btn_R1UuupsAnswer
			// 
			btn_R1UuupsAnswer.BackColor = Color.SandyBrown;
			btn_R1UuupsAnswer.Font = new Font("Segoe UI", 25F);
			btn_R1UuupsAnswer.ForeColor = SystemColors.ControlText;
			btn_R1UuupsAnswer.Location = new Point(253, 81);
			btn_R1UuupsAnswer.Margin = new Padding(3, 2, 3, 2);
			btn_R1UuupsAnswer.Name = "btn_R1UuupsAnswer";
			btn_R1UuupsAnswer.Size = new Size(642, 59);
			btn_R1UuupsAnswer.TabIndex = 70;
			btn_R1UuupsAnswer.Text = "UPS";
			btn_R1UuupsAnswer.UseVisualStyleBackColor = false;
			btn_R1UuupsAnswer.Click += btnUuupsAnswer_Click;
			// 
			// btn_SendQuestion
			// 
			btn_SendQuestion.Location = new Point(16, 16);
			btn_SendQuestion.Margin = new Padding(3, 2, 3, 2);
			btn_SendQuestion.Name = "btn_SendQuestion";
			btn_SendQuestion.Size = new Size(184, 189);
			btn_SendQuestion.TabIndex = 67;
			btn_SendQuestion.Text = "Send Question";
			btn_SendQuestion.UseVisualStyleBackColor = true;
			btn_SendQuestion.Click += btnSendQuestion_Click;
			// 
			// tabPageRound2
			// 
			tabPageRound2.Location = new Point(4, 24);
			tabPageRound2.Name = "tabPageRound2";
			tabPageRound2.Padding = new Padding(3);
			tabPageRound2.Size = new Size(1256, 220);
			tabPageRound2.TabIndex = 1;
			tabPageRound2.Text = "Round2";
			tabPageRound2.UseVisualStyleBackColor = true;
			// 
			// tabPageRound3
			// 
			tabPageRound3.Location = new Point(4, 24);
			tabPageRound3.Name = "tabPageRound3";
			tabPageRound3.Padding = new Padding(3);
			tabPageRound3.Size = new Size(1256, 220);
			tabPageRound3.TabIndex = 2;
			tabPageRound3.Text = "Round3";
			tabPageRound3.UseVisualStyleBackColor = true;
			// 
			// tabPageRapidFire
			// 
			tabPageRapidFire.Controls.Add(btnStartRapidFire);
			tabPageRapidFire.Location = new Point(4, 24);
			tabPageRapidFire.Name = "tabPageRapidFire";
			tabPageRapidFire.Padding = new Padding(3);
			tabPageRapidFire.Size = new Size(1256, 220);
			tabPageRapidFire.TabIndex = 3;
			tabPageRapidFire.Text = "RapidFire";
			tabPageRapidFire.UseVisualStyleBackColor = true;
			// 
			// btnStartRapidFire
			// 
			btnStartRapidFire.Location = new Point(94, 53);
			btnStartRapidFire.Margin = new Padding(3, 2, 3, 2);
			btnStartRapidFire.Name = "btnStartRapidFire";
			btnStartRapidFire.Size = new Size(184, 40);
			btnStartRapidFire.TabIndex = 15;
			btnStartRapidFire.Text = "StartRapidFire ";
			btnStartRapidFire.UseVisualStyleBackColor = true;
			btnStartRapidFire.Click += btnStartRapidFire_Click;
			// 
			// tabPage7
			// 
			tabPage7.Controls.Add(btnShowCorrect);
			tabPage7.Location = new Point(4, 24);
			tabPage7.Name = "tabPage7";
			tabPage7.Padding = new Padding(3);
			tabPage7.Size = new Size(1256, 220);
			tabPage7.TabIndex = 4;
			tabPage7.Text = "Common";
			tabPage7.UseVisualStyleBackColor = true;
			// 
			// btnShowCorrect
			// 
			btnShowCorrect.BackColor = Color.Tan;
			btnShowCorrect.Location = new Point(243, 83);
			btnShowCorrect.Margin = new Padding(3, 2, 3, 2);
			btnShowCorrect.Name = "btnShowCorrect";
			btnShowCorrect.Size = new Size(145, 54);
			btnShowCorrect.TabIndex = 61;
			btnShowCorrect.Text = "Show Correct";
			btnShowCorrect.UseVisualStyleBackColor = false;
			// 
			// tableLayoutPanel5
			// 
			tableLayoutPanel5.AutoSize = true;
			tableLayoutPanel5.AutoSizeMode = AutoSizeMode.GrowAndShrink;
			tableLayoutPanel5.ColumnCount = 2;
			tableLayoutPanel5.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 69.14557F));
			tableLayoutPanel5.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30.8544312F));
			tableLayoutPanel5.Controls.Add(dgvQuestions, 0, 0);
			tableLayoutPanel5.Controls.Add(dgvContestants, 1, 0);
			tableLayoutPanel5.Dock = DockStyle.Fill;
			tableLayoutPanel5.Location = new Point(3, 2);
			tableLayoutPanel5.Margin = new Padding(3, 2, 3, 2);
			tableLayoutPanel5.Name = "tableLayoutPanel5";
			tableLayoutPanel5.RowCount = 1;
			tableLayoutPanel5.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
			tableLayoutPanel5.Size = new Size(1264, 367);
			tableLayoutPanel5.TabIndex = 5;
			// 
			// dgvQuestions
			// 
			dgvQuestions.AllowUserToAddRows = false;
			dgvQuestions.AllowUserToDeleteRows = false;
			dgvQuestions.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCellsExceptHeaders;
			dgvQuestions.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
			dgvQuestions.Dock = DockStyle.Fill;
			dgvQuestions.Location = new Point(3, 2);
			dgvQuestions.Margin = new Padding(3, 2, 3, 2);
			dgvQuestions.Name = "dgvQuestions";
			dgvQuestions.RowHeadersWidth = 51;
			dgvQuestions.RowTemplate.Height = 24;
			dgvQuestions.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
			dgvQuestions.Size = new Size(868, 363);
			dgvQuestions.TabIndex = 3;
			// 
			// dgvContestants
			// 
			dgvContestants.AllowUserToAddRows = false;
			dgvContestants.AllowUserToDeleteRows = false;
			dgvContestants.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
			dgvContestants.Dock = DockStyle.Fill;
			dgvContestants.Location = new Point(877, 2);
			dgvContestants.Margin = new Padding(3, 2, 3, 2);
			dgvContestants.Name = "dgvContestants";
			dgvContestants.RowHeadersWidth = 51;
			dgvContestants.RowTemplate.Height = 24;
			dgvContestants.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
			dgvContestants.Size = new Size(384, 363);
			dgvContestants.TabIndex = 75;
			// 
			// tableLayoutPanel4
			// 
			tableLayoutPanel4.BackColor = Color.Gainsboro;
			tableLayoutPanel4.ColumnCount = 1;
			tableLayoutPanel4.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
			tableLayoutPanel4.Controls.Add(flowLayoutPanelYutubeButtons, 0, 1);
			tableLayoutPanel4.Controls.Add(tabControl1, 0, 4);
			tableLayoutPanel4.Dock = DockStyle.Fill;
			tableLayoutPanel4.Location = new Point(1704, 2);
			tableLayoutPanel4.Margin = new Padding(3, 2, 3, 2);
			tableLayoutPanel4.Name = "tableLayoutPanel4";
			tableLayoutPanel4.RowCount = 6;
			tableLayoutPanel4.RowStyles.Add(new RowStyle());
			tableLayoutPanel4.RowStyles.Add(new RowStyle());
			tableLayoutPanel4.RowStyles.Add(new RowStyle(SizeType.Percent, 3.053435F));
			tableLayoutPanel4.RowStyles.Add(new RowStyle());
			tableLayoutPanel4.RowStyles.Add(new RowStyle(SizeType.Percent, 96.94656F));
			tableLayoutPanel4.RowStyles.Add(new RowStyle(SizeType.Absolute, 15F));
			tableLayoutPanel4.Size = new Size(421, 693);
			tableLayoutPanel4.TabIndex = 2;
			// 
			// flowLayoutPanelYutubeButtons
			// 
			flowLayoutPanelYutubeButtons.AutoSize = true;
			flowLayoutPanelYutubeButtons.BackColor = Color.LightCoral;
			flowLayoutPanelYutubeButtons.Dock = DockStyle.Top;
			flowLayoutPanelYutubeButtons.Location = new Point(3, 2);
			flowLayoutPanelYutubeButtons.Margin = new Padding(3, 2, 3, 2);
			flowLayoutPanelYutubeButtons.Name = "flowLayoutPanelYutubeButtons";
			flowLayoutPanelYutubeButtons.Size = new Size(415, 0);
			flowLayoutPanelYutubeButtons.TabIndex = 79;
			// 
			// tabControl1
			// 
			tabControl1.Controls.Add(tabPage1);
			tabControl1.Controls.Add(tabPage2);
			tabControl1.Controls.Add(tabPage3);
			tabControl1.Controls.Add(tabPage4);
			tabControl1.Dock = DockStyle.Fill;
			tabControl1.Location = new Point(3, 26);
			tabControl1.Margin = new Padding(3, 2, 3, 2);
			tabControl1.Name = "tabControl1";
			tabControl1.SelectedIndex = 0;
			tabControl1.Size = new Size(415, 649);
			tabControl1.TabIndex = 82;
			// 
			// tabPage1
			// 
			tabPage1.Controls.Add(flowLayoutPanelLoads);
			tabPage1.Location = new Point(4, 24);
			tabPage1.Margin = new Padding(3, 2, 3, 2);
			tabPage1.Name = "tabPage1";
			tabPage1.Padding = new Padding(3, 2, 3, 2);
			tabPage1.Size = new Size(407, 621);
			tabPage1.TabIndex = 0;
			tabPage1.Text = "Graphics";
			tabPage1.UseVisualStyleBackColor = true;
			// 
			// flowLayoutPanelLoads
			// 
			flowLayoutPanelLoads.Controls.Add(BtnLoadFullQuestion);
			flowLayoutPanelLoads.Controls.Add(BtnLoadLowerQuestion);
			flowLayoutPanelLoads.Controls.Add(BtnLoadCountDown);
			flowLayoutPanelLoads.Controls.Add(BtnLoadLeaderBoard);
			flowLayoutPanelLoads.Controls.Add(BtnLoadYutubeVote);
			flowLayoutPanelLoads.Controls.Add(btnClearGraphics);
			flowLayoutPanelLoads.Controls.Add(btnLoadBackGround);
			flowLayoutPanelLoads.Location = new Point(8, 17);
			flowLayoutPanelLoads.Margin = new Padding(3, 2, 3, 2);
			flowLayoutPanelLoads.Name = "flowLayoutPanelLoads";
			flowLayoutPanelLoads.Size = new Size(416, 214);
			flowLayoutPanelLoads.TabIndex = 81;
			// 
			// BtnLoadFullQuestion
			// 
			BtnLoadFullQuestion.Location = new Point(3, 2);
			BtnLoadFullQuestion.Margin = new Padding(3, 2, 3, 2);
			BtnLoadFullQuestion.Name = "BtnLoadFullQuestion";
			BtnLoadFullQuestion.Size = new Size(172, 49);
			BtnLoadFullQuestion.TabIndex = 7;
			BtnLoadFullQuestion.Text = "Load Full Graphics";
			BtnLoadFullQuestion.UseVisualStyleBackColor = true;
			BtnLoadFullQuestion.Click += BtnLoadFullQuestion_Click;
			// 
			// BtnLoadLowerQuestion
			// 
			BtnLoadLowerQuestion.Location = new Point(181, 2);
			BtnLoadLowerQuestion.Margin = new Padding(3, 2, 3, 2);
			BtnLoadLowerQuestion.Name = "BtnLoadLowerQuestion";
			BtnLoadLowerQuestion.Size = new Size(172, 49);
			BtnLoadLowerQuestion.TabIndex = 58;
			BtnLoadLowerQuestion.Text = "Load Lower Graphics";
			BtnLoadLowerQuestion.UseVisualStyleBackColor = true;
			BtnLoadLowerQuestion.Click += BtnLoadLowerQuestion_Click;
			// 
			// BtnLoadCountDown
			// 
			BtnLoadCountDown.Location = new Point(3, 55);
			BtnLoadCountDown.Margin = new Padding(3, 2, 3, 2);
			BtnLoadCountDown.Name = "BtnLoadCountDown";
			BtnLoadCountDown.Size = new Size(172, 49);
			BtnLoadCountDown.TabIndex = 61;
			BtnLoadCountDown.Text = "Load CountDown";
			BtnLoadCountDown.UseVisualStyleBackColor = true;
			BtnLoadCountDown.Click += BtnLoadCountDown_Click;
			// 
			// BtnLoadLeaderBoard
			// 
			BtnLoadLeaderBoard.Location = new Point(181, 55);
			BtnLoadLeaderBoard.Margin = new Padding(3, 2, 3, 2);
			BtnLoadLeaderBoard.Name = "BtnLoadLeaderBoard";
			BtnLoadLeaderBoard.Size = new Size(172, 49);
			BtnLoadLeaderBoard.TabIndex = 66;
			BtnLoadLeaderBoard.Text = "Load LeaderBoard";
			BtnLoadLeaderBoard.UseVisualStyleBackColor = true;
			BtnLoadLeaderBoard.Click += BtnLoadLeaderBoard_Click;
			// 
			// BtnLoadYutubeVote
			// 
			BtnLoadYutubeVote.Location = new Point(3, 108);
			BtnLoadYutubeVote.Margin = new Padding(3, 2, 3, 2);
			BtnLoadYutubeVote.Name = "BtnLoadYutubeVote";
			BtnLoadYutubeVote.Size = new Size(172, 51);
			BtnLoadYutubeVote.TabIndex = 76;
			BtnLoadYutubeVote.Text = "Load YT";
			BtnLoadYutubeVote.UseVisualStyleBackColor = true;
			BtnLoadYutubeVote.Click += BtnLoadYutubeVote_Click;
			// 
			// btnClearGraphics
			// 
			btnClearGraphics.Location = new Point(181, 108);
			btnClearGraphics.Margin = new Padding(3, 2, 3, 2);
			btnClearGraphics.Name = "btnClearGraphics";
			btnClearGraphics.Size = new Size(187, 46);
			btnClearGraphics.TabIndex = 59;
			btnClearGraphics.Text = "Clear Graphics";
			btnClearGraphics.UseVisualStyleBackColor = true;
			btnClearGraphics.Click += btnClearGraphics_Click;
			// 
			// btnLoadBackGround
			// 
			btnLoadBackGround.Location = new Point(3, 163);
			btnLoadBackGround.Margin = new Padding(3, 2, 3, 2);
			btnLoadBackGround.Name = "btnLoadBackGround";
			btnLoadBackGround.Size = new Size(187, 46);
			btnLoadBackGround.TabIndex = 77;
			btnLoadBackGround.Text = "Load BackGround";
			btnLoadBackGround.UseVisualStyleBackColor = true;
			btnLoadBackGround.Click += btnLoadBackGround_Click;
			// 
			// tabPage2
			// 
			tabPage2.Controls.Add(tbx_YTVideoId);
			tabPage2.Controls.Add(AudienceCountdownDuration);
			tabPage2.Controls.Add(groupBox2);
			tabPage2.Controls.Add(flowLayoutPanel1);
			tabPage2.Location = new Point(4, 24);
			tabPage2.Margin = new Padding(3, 2, 3, 2);
			tabPage2.Name = "tabPage2";
			tabPage2.Padding = new Padding(3, 2, 3, 2);
			tabPage2.Size = new Size(407, 621);
			tabPage2.TabIndex = 1;
			tabPage2.Text = "YouTUBE";
			tabPage2.UseVisualStyleBackColor = true;
			// 
			// tbx_YTVideoId
			// 
			tbx_YTVideoId.Font = new Font("Segoe UI", 12F);
			tbx_YTVideoId.Location = new Point(126, 221);
			tbx_YTVideoId.Margin = new Padding(3, 2, 3, 2);
			tbx_YTVideoId.Name = "tbx_YTVideoId";
			tbx_YTVideoId.Size = new Size(183, 29);
			tbx_YTVideoId.TabIndex = 86;
			// 
			// AudienceCountdownDuration
			// 
			AudienceCountdownDuration.Font = new Font("Segoe UI", 25F);
			AudienceCountdownDuration.Location = new Point(222, 158);
			AudienceCountdownDuration.Margin = new Padding(3, 2, 3, 2);
			AudienceCountdownDuration.Name = "AudienceCountdownDuration";
			AudienceCountdownDuration.Size = new Size(87, 52);
			AudienceCountdownDuration.TabIndex = 85;
			AudienceCountdownDuration.Value = new decimal(new int[] { 60, 0, 0, 0 });
			// 
			// groupBox2
			// 
			groupBox2.Controls.Add(richTextBox1);
			groupBox2.Dock = DockStyle.Bottom;
			groupBox2.Location = new Point(3, 361);
			groupBox2.Margin = new Padding(3, 2, 3, 2);
			groupBox2.Name = "groupBox2";
			groupBox2.Padding = new Padding(3, 2, 3, 2);
			groupBox2.Size = new Size(401, 258);
			groupBox2.TabIndex = 83;
			groupBox2.TabStop = false;
			groupBox2.Text = "ONLINE AUDIENCE";
			// 
			// richTextBox1
			// 
			richTextBox1.Location = new Point(62, 44);
			richTextBox1.Margin = new Padding(3, 2, 3, 2);
			richTextBox1.Name = "richTextBox1";
			richTextBox1.Size = new Size(218, 76);
			richTextBox1.TabIndex = 0;
			richTextBox1.Text = "A:\nB:\nC:\nD:";
			// 
			// flowLayoutPanel1
			// 
			flowLayoutPanel1.Controls.Add(btn_ytVotingOnOFF);
			flowLayoutPanel1.Controls.Add(button11);
			flowLayoutPanel1.Controls.Add(button12);
			flowLayoutPanel1.Dock = DockStyle.Top;
			flowLayoutPanel1.Location = new Point(3, 2);
			flowLayoutPanel1.Margin = new Padding(3, 2, 3, 2);
			flowLayoutPanel1.Name = "flowLayoutPanel1";
			flowLayoutPanel1.Size = new Size(401, 139);
			flowLayoutPanel1.TabIndex = 82;
			// 
			// btn_ytVotingOnOFF
			// 
			btn_ytVotingOnOFF.BackColor = Color.LightCoral;
			btn_ytVotingOnOFF.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
			btn_ytVotingOnOFF.Location = new Point(3, 2);
			btn_ytVotingOnOFF.Margin = new Padding(3, 2, 3, 2);
			btn_ytVotingOnOFF.Name = "btn_ytVotingOnOFF";
			btn_ytVotingOnOFF.Size = new Size(172, 49);
			btn_ytVotingOnOFF.TabIndex = 7;
			btn_ytVotingOnOFF.Text = "YouTube VOTING";
			btn_ytVotingOnOFF.UseVisualStyleBackColor = false;
			// 
			// button11
			// 
			button11.Location = new Point(181, 2);
			button11.Margin = new Padding(3, 2, 3, 2);
			button11.Name = "button11";
			button11.Size = new Size(172, 49);
			button11.TabIndex = 58;
			button11.Text = "Get YT Vote Result";
			button11.UseVisualStyleBackColor = true;
			// 
			// button12
			// 
			button12.Location = new Point(3, 55);
			button12.Margin = new Padding(3, 2, 3, 2);
			button12.Name = "button12";
			button12.Size = new Size(348, 49);
			button12.TabIndex = 61;
			button12.Text = "Show Result";
			button12.UseVisualStyleBackColor = true;
			// 
			// tabPage3
			// 
			tabPage3.Location = new Point(4, 24);
			tabPage3.Margin = new Padding(3, 2, 3, 2);
			tabPage3.Name = "tabPage3";
			tabPage3.Padding = new Padding(3, 2, 3, 2);
			tabPage3.Size = new Size(407, 621);
			tabPage3.TabIndex = 2;
			tabPage3.Text = "FaceBOOK";
			tabPage3.UseVisualStyleBackColor = true;
			// 
			// tabPage4
			// 
			tabPage4.Controls.Add(btn_SendMidiNote);
			tabPage4.Controls.Add(tBox_MidiVelocity);
			tabPage4.Controls.Add(tBox_MidiNote);
			tabPage4.Controls.Add(button1);
			tabPage4.Location = new Point(4, 24);
			tabPage4.Margin = new Padding(3, 2, 3, 2);
			tabPage4.Name = "tabPage4";
			tabPage4.Padding = new Padding(3, 2, 3, 2);
			tabPage4.Size = new Size(407, 621);
			tabPage4.TabIndex = 3;
			tabPage4.Text = "DMX";
			tabPage4.UseVisualStyleBackColor = true;
			// 
			// btn_SendMidiNote
			// 
			btn_SendMidiNote.Location = new Point(147, 142);
			btn_SendMidiNote.Name = "btn_SendMidiNote";
			btn_SendMidiNote.Size = new Size(73, 61);
			btn_SendMidiNote.TabIndex = 83;
			btn_SendMidiNote.Text = "SEND";
			btn_SendMidiNote.UseVisualStyleBackColor = true;
			btn_SendMidiNote.Click += btn_SendMidiNote_Click;
			// 
			// tBox_MidiVelocity
			// 
			tBox_MidiVelocity.Location = new Point(22, 183);
			tBox_MidiVelocity.Name = "tBox_MidiVelocity";
			tBox_MidiVelocity.Size = new Size(94, 23);
			tBox_MidiVelocity.TabIndex = 82;
			// 
			// tBox_MidiNote
			// 
			tBox_MidiNote.Location = new Point(22, 142);
			tBox_MidiNote.Name = "tBox_MidiNote";
			tBox_MidiNote.Size = new Size(94, 23);
			tBox_MidiNote.TabIndex = 81;
			// 
			// button1
			// 
			button1.Location = new Point(21, 38);
			button1.Margin = new Padding(3, 2, 3, 2);
			button1.Name = "button1";
			button1.Size = new Size(200, 45);
			button1.TabIndex = 80;
			button1.Text = "DMX";
			button1.UseVisualStyleBackColor = true;
			// 
			// panel1
			// 
			panel1.BackColor = SystemColors.ActiveBorder;
			panel1.Controls.Add(cmbCountdownMode);
			panel1.Controls.Add(lblCountdown);
			panel1.Controls.Add(CountdownDuration);
			panel1.Controls.Add(listBoxClients);
			panel1.Dock = DockStyle.Fill;
			panel1.Location = new Point(3, 82);
			panel1.Margin = new Padding(3, 2, 3, 2);
			panel1.Name = "panel1";
			panel1.Size = new Size(2142, 99);
			panel1.TabIndex = 43;
			// 
			// cmbCountdownMode
			// 
			cmbCountdownMode.Font = new Font("Segoe UI", 25F);
			cmbCountdownMode.FormattingEnabled = true;
			cmbCountdownMode.Location = new Point(23, 17);
			cmbCountdownMode.Margin = new Padding(3, 2, 3, 2);
			cmbCountdownMode.Name = "cmbCountdownMode";
			cmbCountdownMode.Size = new Size(349, 53);
			cmbCountdownMode.TabIndex = 45;
			// 
			// lblCountdown
			// 
			lblCountdown.AutoSize = true;
			lblCountdown.Font = new Font("Microsoft Sans Serif", 50F, FontStyle.Regular, GraphicsUnit.Point, 0);
			lblCountdown.ForeColor = Color.Lime;
			lblCountdown.Location = new Point(1266, 5);
			lblCountdown.Name = "lblCountdown";
			lblCountdown.Size = new Size(106, 76);
			lblCountdown.TabIndex = 37;
			lblCountdown.Text = "00";
			// 
			// CountdownDuration
			// 
			CountdownDuration.Font = new Font("Segoe UI", 25F);
			CountdownDuration.Location = new Point(1019, 18);
			CountdownDuration.Margin = new Padding(3, 2, 3, 2);
			CountdownDuration.Name = "CountdownDuration";
			CountdownDuration.Size = new Size(87, 52);
			CountdownDuration.TabIndex = 42;
			CountdownDuration.Value = new decimal(new int[] { 60, 0, 0, 0 });
			// 
			// listBoxClients
			// 
			listBoxClients.Dock = DockStyle.Right;
			listBoxClients.FormattingEnabled = true;
			listBoxClients.ItemHeight = 15;
			listBoxClients.Location = new Point(1572, 0);
			listBoxClients.Margin = new Padding(3, 2, 3, 2);
			listBoxClients.Name = "listBoxClients";
			listBoxClients.Size = new Size(570, 99);
			listBoxClients.TabIndex = 8;
			// 
			// countdownTimer
			// 
			countdownTimer.Tick += countdownTimer_Tick;
			// 
			// MnForm
			// 
			AutoScaleDimensions = new SizeF(7F, 15F);
			AutoScaleMode = AutoScaleMode.Font;
			ClientSize = new Size(2148, 1126);
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
			tPageRapidFire.ResumeLayout(false);
			tableLayoutPanel1.ResumeLayout(false);
			tableLayoutPanel2.ResumeLayout(false);
			groupBox1.ResumeLayout(false);
			groupBox1.PerformLayout();
			flowLayoutPanel2.ResumeLayout(false);
			tableLayoutPanel3.ResumeLayout(false);
			tableLayoutPanel3.PerformLayout();
			panel2.ResumeLayout(false);
			tabGameModes.ResumeLayout(false);
			tabPageRound1.ResumeLayout(false);
			tabPageRapidFire.ResumeLayout(false);
			tabPage7.ResumeLayout(false);
			tableLayoutPanel5.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)dgvQuestions).EndInit();
			((System.ComponentModel.ISupportInitialize)dgvContestants).EndInit();
			tableLayoutPanel4.ResumeLayout(false);
			tableLayoutPanel4.PerformLayout();
			tabControl1.ResumeLayout(false);
			tabPage1.ResumeLayout(false);
			flowLayoutPanelLoads.ResumeLayout(false);
			tabPage2.ResumeLayout(false);
			tabPage2.PerformLayout();
			((System.ComponentModel.ISupportInitialize)AudienceCountdownDuration).EndInit();
			groupBox2.ResumeLayout(false);
			flowLayoutPanel1.ResumeLayout(false);
			tabPage4.ResumeLayout(false);
			tabPage4.PerformLayout();
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
		private System.Windows.Forms.Label lblCountdown;
		private System.Windows.Forms.NumericUpDown CountdownDuration;
		private System.Windows.Forms.Panel panel1;
		private System.Windows.Forms.ListBox listBoxClients;
		private System.Windows.Forms.ComboBox rf_CBoxRapidFireMode;
		private System.Windows.Forms.Panel pnl_Test;
		private System.Windows.Forms.Button btnLoadCG;
		private System.Windows.Forms.Button btnStartPolling;
		private System.Windows.Forms.Button btnDisconnectSelectedClient;
		private System.Windows.Forms.Button btn_YTAuth;
		public System.Windows.Forms.CheckBox chBoxClearScores;
		private System.Windows.Forms.Button btnSendToSelectedClient;
		private System.Windows.Forms.Button btnSendMessageToClients;
		private System.Windows.Forms.ComboBox cmbPollingMode;
		private Button btnStartCountdown;
		private System.Windows.Forms.Timer countdownTimer;
		private ComboBox cmbCountdownMode;
		private Button button4;
		private Button button3;
		private Button btn_R1InCorrectAnswer;
		private Button button5;
		private Button btnShowFinalResults;
		private Button btnStoreResults;
		private Button btnShowLeaderBoard;
		public DataGridView dgvContestants;
		private BindingSource playerBindingSource;
		private ListBox votingUserListBox;
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
		private FlowLayoutPanel flowLayoutPanelYutubeButtons;
		private TabControl tabControl1;
		private TabPage tabPage1;
		private FlowLayoutPanel flowLayoutPanelLoads;
		private Button BtnLoadFullQuestion;
		private Button BtnLoadLowerQuestion;
		private Button BtnLoadCountDown;
		private Button BtnLoadLeaderBoard;
		private Button BtnLoadYutubeVote;
		private Button btnClearGraphics;
		private Button btnLoadBackGround;
		private TabPage tabPage2;
		private GroupBox groupBox2;
		private RichTextBox richTextBox1;
		private FlowLayoutPanel flowLayoutPanel1;
		private Button btn_ytVotingOnOFF;
		private Button button11;
		private Button button12;
		private TabPage tabPage3;
		private TabPage tabPage4;
		private Button button1;
		private Button btn_SendMidiNote;
		private TextBox tBox_MidiVelocity;
		private TextBox tBox_MidiNote;
		private Button btnUuupsAnswer;
		private NumericUpDown AudienceCountdownDuration;
		private TextBox tbx_YTVideoId;
		private TabControl tabGameModes;
		private TabPage tabPageRound1;
		private TabPage tabPageRound2;
		private TabPage tabPageRound3;
		private TabPage tabPageRapidFire;
		private Button btnStartRapidFire;
		private Button btnIncorrectAnswer;
		private Button btn_R1CorrectAnswer;
		private Button btn_SendQuestion;
		private Button btn_R1PrepareNext;
		private Button btn_R1UuupsAnswer;
		private TabPage tabPage7;
		private Button btnShowCorrect;
	}
}

