using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameController.Shared.Enums
{
	public enum CountdownMode
	{
		// Countdown stops only when time runs out
		TimedOnly,

		// Countdown stops when time runs out OR when all active players have answered
		AllPlayersAnswered,

		// Countdown stops when time runs out OR when the first player answers
		FirstAnswer,
		Round1,
		Round2,	
		Round3
	}

	public enum GameMode
	{
		// Countdown stops only when time runs out
		TimedOnly,
		RapidMode,

		// Countdown stops when time runs out OR when all active players have answered
		AllPlayersAnswered,

		// Countdown stops when time runs out OR when the first player answers
		FirstAnswer,
		Round1,
		Round2,
		Round3,
		None
	}

	public enum CountdownStopMode
	{
		// Countdown stops only when time runs out
		Start,
		Pause,

		// Countdown stops when time runs out OR when all active players have answered
		Reset,

		// Countdown stops when time runs out OR when the first player answers
		TimeUp,
		Resume
	}
}
