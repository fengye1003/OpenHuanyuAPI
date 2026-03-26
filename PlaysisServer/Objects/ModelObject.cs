using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace PlaysisServer.Objects
{
    internal class ModelObject
    {
        public readonly PlayerObject ParentPlayer;
        public readonly string Name;
        public readonly string FetchURL;
        public Vector3 Location;
        public Vector3 Scale;
        public Vector3 Rotation;
        public readonly string ModelHash;

        public ModelObject(PlayerObject parent, string name, string url, Vector3 location, Vector3 scale, Vector3 rotation, string hash)
        {
            ParentPlayer = parent;
            Name = name;
            FetchURL = url;
            Location = location;
            Scale = scale;
            Rotation = rotation;
            ModelHash = hash;
        }

        
    }
}
