using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameController.Shared.Enums
{
	public enum CountDownMode
	{
		// CountDown stops only when time runs out
		TimedOnly,

		// CountDown stops when time runs out OR when all active players have answered
		AllPlayersAnswered,

		// CountDown stops when time runs out OR when the first player answers
		FirstAnswer,
		Round1,
		Round2,	
		Round3
	}

	public enum GameMode
	{
        // CountDown stops only when time runs out
        Round1,
        Round2,
        Round3,

        TimedOnly,
		RapidMode ,

		// CountDown stops when time runs out OR when all active players have answered
		AllPlayersAnswered,

		// CountDown stops when time runs out OR when the first player answers
		FirstAnswer,
		None
	}

	public enum CountDownStopMode
	{
		// CountDown stops only when time runs out
		Start,
		Pause,

		// CountDown stops when time runs out OR when all active players have answered
		Reset,

		// CountDown stops when time runs out OR when the first player answers
		TimeUp,
		Resume
	}
}
