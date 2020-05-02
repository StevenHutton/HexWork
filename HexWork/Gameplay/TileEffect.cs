using System;
using HexWork.Gameplay.Characters;
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
        public Guid Guid = Guid.NewGuid();
        public HexCoordinate Position;
        public TileEffectType Type = TileEffectType.Fire;
        public int Damage = 5;
        public StatusEffect Effect;
        public float MovementModifier = 0.1f;

	    public virtual async void TriggerEffect(IGameStateObject gameState, Character character)
	    {
            if(Damage > 0)
                gameState.ApplyDamage(character, Damage);
            
            if(Effect != null)
                gameState.ApplyStatus(character, Effect);
	    }
        public TileEffect() { }
        
        public TileEffect(HexCoordinate pos)
        {
            Position = pos;
        }

        public TileEffect(TileEffect effectToCopy, HexCoordinate pos)
        {
            Position = pos;
            Type = effectToCopy.Type;
            Damage = effectToCopy.Damage;
            Effect = effectToCopy.Effect;
            MovementModifier = effectToCopy.MovementModifier;
        }
    }
}