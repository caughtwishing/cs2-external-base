using System.Numerics;

namespace invertable_xandsex
{
    public class Entity
    {
        public Vector3 position { get; set; }
        public Vector3 viewOffset { get; set; }
        public Vector2 position2D { get; set; }
        public Vector2 viewPosition2D { get; set; }
        public int team { get; set; }
        public int health { get; set; }
        public string entityName { get; set; }
        public string entityWeapon { get; set; }
        public bool spotted { get; set; }
    }
}