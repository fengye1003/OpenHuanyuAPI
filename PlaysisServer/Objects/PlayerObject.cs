using LiteNetLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace PlaysisServer.Objects
{
    internal class PlayerObject
    {
        public Vector3 Location;
        public Vector3 Scale;
        public Vector3 Rotation;
        public readonly int UID;
        public string Name;
        public string? ConnectSecret;
        public NetPeer? UserNetPeer;


        private PlaysisRoom? _room;

        public PlaysisRoom Room
        {
            get
            {
                return _room!;
            }
            set
            {
                if (_room != null)
                {
                    _room.ExitRoom(this);
                }

                _room = value;

                if (_room != null)
                {
                    _room.JoinRoom(this);
                }
            }
        }

        public PlayerObject(int uid, string name, string? secret)
        {
            UID = uid;
            Name = name;
            ConnectSecret = secret;
        }


    }
}
