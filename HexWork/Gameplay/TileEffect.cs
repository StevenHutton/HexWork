using System;
using HexWork.Gameplay.Interfaces;
using HexWork.UI.Interfaces;

namespace HexWork.Gameplay
{
    public enum TileEffectType
    {
        None,
        Fire
    }

    public class TileEffect
    {
        public HexCoordinate Position;

        public Guid Guid = Guid.NewGuid();

        public TileEffectType Type = TileEffectType.Fire;

	    public async void TriggerEffect(IGameStateObject gameState, Character character)
	    {
		    gameState.ApplyDamage(character, 5);
		    gameState.ApplyStatus(character, new DotEffect());
	    }

        public TileEffect(HexCoordinate pos)
        {
            Position = pos;
        }
    }
}