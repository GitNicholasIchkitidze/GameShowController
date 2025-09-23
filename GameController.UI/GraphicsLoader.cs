using GameController.Shared.Enums;
using GameController.Shared.Models;
using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GameController.UI
{
    public partial class GraphicsLoader : UserControl
    {
        public GraphicsLoader()
        {
            InitializeComponent();
        }


        public Button Btn_Load
        {
            get { return this.btn_Load; }
        }

        public NumericUpDown Channel
        {
            get { return this.numChannel; }
        }

        public NumericUpDown Layer
        {
            get { return this.numLayer; }
        }


        private void button1_Click(object sender, EventArgs e)
        {

        }

        private async void btn_Load_Click(object sender, EventArgs e)
        {
            if ((sender as Button).Text == "Load Full Question Template")
            {
                //var result = await _hubConnection.InvokeAsync<OperationResult>("CGLoadTemplate", CGTemplateEnums.QuestionFull);
            //    AppendLog($"[WinForms UI] <- Hub QuestionFull - ის ჩატვირთვა: {result.Message}");
            }
            else
            {
            //    (sender as Button).Text = "Load";
            }
        }
    }
}
