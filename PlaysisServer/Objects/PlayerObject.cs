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
        public string ConnectSecret;

        public PlayerObject()
        {

        }


    }
}
