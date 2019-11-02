using System;

namespace HexWork.Gameplay
{
    public enum TerrainEffectType
    {
        Fire
    }

    public class TerrainEffect
    {
        public HexCoordinate Position;

        public Guid Guid = Guid.NewGuid();

        public TerrainEffectType Type = TerrainEffectType.Fire;
    }
}
