using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameController.Shared.Models
{
	public class MidiSettingsModels
	{
		public string? DeviceName { get; set; }
		public int CorrectAnswerNote { get; set; }
		public int CorrectAnswerOctave { get; set; }
		public int CorrectAnswerVelocity { get; set; }
		public int CountdownNote { get; set; }
		public int CountdownOctave { get; set; }
		public int CountdownVelocity { get; set; }
	}
}
