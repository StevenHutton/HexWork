using System;
using HexWork.Gameplay.Interfaces;
using HexWork.UI.Interfaces;

namespace HexWork.Gameplay
{
    public enum TileEffectType
    {
        None,
        Fire,
        Wind
    }

    public class TileEffect
    {
        public HexCoordinate Position;

        public Guid Guid = Guid.NewGuid();

        public TileEffectType Type = TileEffectType.Fire;

	    public virtual async void TriggerEffect(IGameStateObject gameState, Character character)
	    {
		    gameState.ApplyDamage(character, 5);
		    gameState.ApplyStatus(character, new DotEffect());
	    }

        public TileEffect(HexCoordinate pos)
        {
            Position = pos;
        }
    }

    public class WindEffect : TileEffect
    {
        public override async void TriggerEffect(IGameStateObject gameState, Character character)
        { }

        public WindEffect(HexCoordinate pos) : base(pos) { }
    }
}