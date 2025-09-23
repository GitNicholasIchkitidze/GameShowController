namespace GameController.UI
{
    partial class GraphicsLoader
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

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            btn_Load = new Button();
            numChannel = new NumericUpDown();
            numLayer = new NumericUpDown();
            ((System.ComponentModel.ISupportInitialize)numChannel).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numLayer).BeginInit();
            SuspendLayout();
            // 
            // btn_Load
            // 
            btn_Load.Location = new Point(14, 27);
            btn_Load.Name = "btn_Load";
            btn_Load.Size = new Size(176, 23);
            btn_Load.TabIndex = 0;
            btn_Load.Text = "button1";
            btn_Load.UseVisualStyleBackColor = true;
            btn_Load.Click += btn_Load_Click;
            // 
            // numChannel
            // 
            numChannel.Location = new Point(209, 27);
            numChannel.Name = "numChannel";
            numChannel.Size = new Size(57, 23);
            numChannel.TabIndex = 1;
            // 
            // numLayer
            // 
            numLayer.Location = new Point(290, 27);
            numLayer.Name = "numLayer";
            numLayer.Size = new Size(57, 23);
            numLayer.TabIndex = 2;
            // 
            // GraphicsLoader
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = SystemColors.ActiveCaption;
            Controls.Add(numLayer);
            Controls.Add(numChannel);
            Controls.Add(btn_Load);
            Name = "GraphicsLoader";
            Size = new Size(360, 76);
            ((System.ComponentModel.ISupportInitialize)numChannel).EndInit();
            ((System.ComponentModel.ISupportInitialize)numLayer).EndInit();
            ResumeLayout(false);
        }

        #endregion

        private Button btn_Load;
        private NumericUpDown numChannel;
        private NumericUpDown numLayer;
    }
}
