using GameController.Shared.Models;
using System.Collections.Concurrent;

namespace GameController.Server.Services
{
	public interface IGameService
	{
		ConcurrentDictionary<string, Player> ConnectedPlayers { get; }


		void AddPoints(Player player, int point);
		Player AddNewPlayer(string connectionId, string ip, string name, string clientType, bool isInPlay);


	}
}
