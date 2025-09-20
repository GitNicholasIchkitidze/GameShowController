using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameController.Shared.Models
{
	public class Player
	{
		public string ConnectionId { get; }
		public  string Ip { get; }
		public string Name { get; }
		public string NickName { get; }
		public string ClientType { get; }
		public int Score { get; set; }
		public bool IsInPlay { get; set; } = false;
		public Player(string connectionId, string ip, string name, string nickName, string clientType, bool isInPlay)
		{
			ConnectionId = connectionId;
			Ip = ip;
			Name = name;
			NickName = nickName;
			ClientType = clientType;
			Score = 0;
			IsInPlay = IsInPlay;
		}

		public void AddScore(int points)
		{
			Score += points;
		}
	}

	public class PlayerScore
	{
		public required string PlayerId { get; set; }
		public required string PlayerName { get; set; }
		public int Score { get; set; }
		public DateTime Timestamp { get; set; }
	}
}
