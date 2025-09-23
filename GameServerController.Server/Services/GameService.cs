using GameController.Shared.Models;
using System.Collections.Concurrent;

namespace GameController.Server.Services
{
    public class GameService : IGameService
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

        public Player AddNewPlayer(string connectionId, string ip, string name, string nickName, string clientType, bool isInPlay)
        {
            return new Player(connectionId, ip, name, nickName, clientType, isInPlay);
        }



    }
}