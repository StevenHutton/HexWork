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

        public int Damage = 5;

        public StatusEffect Effect = new DotEffect();

        public float MovementModifier = 0.1f;

	    public virtual async void TriggerEffect(IGameStateObject gameState, Character character)
	    {
		    gameState.ApplyDamage(character, Damage);
		    gameState.ApplyStatus(character, Effect);
	    }
        public TileEffect() { }
        
        public TileEffect(HexCoordinate pos)
        {
            Position = pos;
        }
    }
}