using System;
using HexWork.Gameplay.Characters;
using HexWork.Gameplay.Interfaces;

namespace HexWork.Gameplay
{
    public class TileEffect
    {
        public Guid Guid = Guid.NewGuid();
        public HexCoordinate Position;
        public int Damage = 5;
        public StatusEffect Effect;
        public float MovementModifier = 0.1f;
        public string Name = "TileEffect";

	    public virtual async void TriggerEffect(IGameStateObject gameState, Character character)
        {
            if (character == null)
                return;

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
            Damage = effectToCopy.Damage;
            Effect = effectToCopy.Effect;
            Name = effectToCopy.Name;
            MovementModifier = effectToCopy.MovementModifier;
        }
    }
}