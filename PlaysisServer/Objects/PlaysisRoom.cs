using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlaysisServer.Objects
{
    internal class PlaysisRoom
    {
        public readonly int RoomID;
        public string Name;
        public PlayerObject? Owner;
        public List<PlayerObject> Players = new();

        public PlaysisRoom(int roomId, string name, PlayerObject owner)
        {
            RoomID = roomId;
            Name = name;
            Owner = owner;
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
