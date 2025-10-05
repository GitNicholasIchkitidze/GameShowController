using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameController.Shared.Models
{
    public class TitleDataModel
    {

        public string Status { get; set; } = "LIVE";
        public string BreakingNews  { get; set; }
        public string Headline { get; set; }
        public string SecondLine { get; set; }
        public List<string> SecondLines { get; set; }
    }
}
