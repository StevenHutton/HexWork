using System;
using Microsoft.Xna.Framework;

namespace HexWork.Gameplay
{
    public enum TerrainType
    {
        Ground,
        Water,
        Lava,
        Ice,
        ThinIce,
        Snow,
        Sand,
        Pit,
        Wall
    }


    public class Tile
    {
        public TerrainType TerrainType = TerrainType.Ground;

        private static Random rand = new Random();

        public Color Color { get; set; }

        public bool IsWalkable { get; set; }

        // ReSharper disable once InconsistentNaming
        public bool BlocksLOS { get; set; } = false;

        public float MovementCost { get; set; }

        public Tile()
        {
        }
        
    }
}
