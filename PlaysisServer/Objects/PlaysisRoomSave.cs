using System.Numerics;

namespace PlaysisServer.Objects
{
    public class PlaysisRoomSave
    {
        public int RoomID { get; set; }
        public string Name { get; set; }
        public string? Description { get; set; }

        public string? OwnerUID { get; set; }

        public List<PlayerSave> Players { get; set; } = new();
        public List<ModelSave> Models { get; set; } = new();
    }

    public class PlayerSave
    {
        public string UID { get; set; }
    }

    public class ModelSave
    {
        public string ParentUID { get; set; }

        public string? Name { get; set; }
        public string? FetchURL { get; set; }
        public string ModelHash { get; set; }

        public Vector3 Location { get; set; }
        public Vector3 Scale { get; set; }
        public Vector3 Rotation { get; set; }

        public float FootprintX { get; set; }
        public float FootprintZ { get; set; }
        public float OffsetX { get; set; }
        public float OffsetZ { get; set; }
    }
}

