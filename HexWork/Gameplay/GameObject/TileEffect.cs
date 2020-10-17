using HexWork.Gameplay.Interfaces;

namespace HexWork.Gameplay.GameObject
{
    public class TileEffect : HexGameObject
    {
        public int Damage = 5;
        public StatusEffect Effect;
        public float MovementModifier = 0.1f;

	    public virtual async void TriggerEffect(IGameStateObject gameState, HexGameObject entity)
        {
            if (entity == null)
                return;

            if(Damage > 0)
                gameState.ApplyDamage(entity, Damage);
            
            if(Effect != null)
                gameState.ApplyStatus(entity, Effect);
	    }

        public TileEffect() : base()
        {
            Name = "TileEffect";
        }

        public TileEffect(string name, int maxHealth) : base(name, maxHealth) { }

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
            Health = effectToCopy.Health;
            MaxHealth = effectToCopy.MaxHealth;
            MovementModifier = effectToCopy.MovementModifier;
            BlocksMovement = effectToCopy.BlocksMovement;
        }
    }
}