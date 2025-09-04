using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameController.Shared.Enums
{
	public enum RoundEndAction
	{
		// Countdown is stopped and the timer is reset to zero.
		Reset,

		// Countdown is stopped and the timer holds its current value.
		Pause
	}
}
