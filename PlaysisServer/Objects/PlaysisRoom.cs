using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlaysisServer.Objects
{
    public class PlaysisRoom
    {
        public readonly int RoomID;
        public string Name;
        public string? Description;
        public PlayerObject? Owner;
        public List<PlayerObject> Players = new();
        public Dictionary<string, ModelObject> Models = new();
        public List<AnnotationObject> Annotations = new();
        public Dictionary<string, ScoreObject> Scores = new();
        public long CreatedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        public int Capacity = 0;
        public int NextAnnotationId = 1;

        public PlaysisRoom(int roomId, string name, PlayerObject? owner, string? description = null)
        {
            RoomID = roomId;
            Name = name;
            Owner = owner;
            Description = description;
        }

        public void JoinRoom(PlayerObject player)
        {
            Players.Add(player);
        }

        public void ExitRoom(PlayerObject player)
        {
            if (Players.Contains<PlayerObject>(player))
            {
                Players.Remove(player);
            }
        }
    }
}





