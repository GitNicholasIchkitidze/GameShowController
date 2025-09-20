using GameController.Shared.Enums;
using GameController.Shared.Models;
using Microsoft.Extensions.Hosting;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace GameController.Server.Services
{
	public class GameService :IGameService
	{
		//s		private readonly IHostEnvironment _env;
		//s
		//s		public GameService(IHostEnvironment env)
		//s		{
		//s			_env = env;
		//s		}
		private readonly ConcurrentDictionary<string, Player> _connectedPlayers = new ConcurrentDictionary<string, Player>();

		public ConcurrentDictionary<string, Player> ConnectedPlayers => _connectedPlayers;

		public GameService()
		{
	
		}


		public void AddPoints(Player player, int point)
		{
			player.AddScore(point);
		}

		public Player AddNewPlayer(string connectionId, string ip, string name,string nickName, string clientType, bool isInPlay)
		{
			return new Player(connectionId, ip, name, nickName, clientType, isInPlay);
		}


		
	}
}