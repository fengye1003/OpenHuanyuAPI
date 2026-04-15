using System.Numerics;

namespace PlaysisServer.Objects
{
    public class PlaysisRoomSave
    {
        public int RoomID { get; set; }
        public string Name { get; set; }
        public string? Description { get; set; }

        public int? OwnerUID { get; set; }

        public List<PlayerSave> Players { get; set; } = new();
        public List<ModelSave> Models { get; set; } = new();
    }

    public class PlayerSave
    {
        public int UID { get; set; }
    }

    public class ModelSave
    {
        public int ParentUID { get; set; }

        public string? Name { get; set; }
        public string? FetchURL { get; set; }
        public string ModelHash { get; set; }

        public Vector3 Location { get; set; }
        public Vector3 Scale { get; set; }
        public Vector3 Rotation { get; set; }
    }
}