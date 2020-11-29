using HexWork.Gameplay.Interfaces;
using System.Linq;
using System.Threading.Tasks;

namespace HexWork.Gameplay.GameObject
{
    public class TileEffect : HexGameObject
    {
        public int Damage = 5;
        public StatusEffect Effect;
        public float MovementModifier = 0.1f;

	    public virtual async Task<BoardState> TriggerEffect(BoardState state, IRulesProvider gameState)
        {            
            //see if there's anything in my space
            var entity = state.Entities.FirstOrDefault(ent => ent != this && ent.Position == Position);
            if (entity == null)
                return state;

            var newState = state.Copy();

            newState = gameState.ApplyDamage(newState, entity.Id, Damage);
            
            if(Effect != null)
                newState = gameState.ApplyStatus(newState, entity.Id, Effect);

            return newState;
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
        
        //copy constructor - use this for creating a new tile effect with the exact same parameters
        public TileEffect(TileEffect effectToCopy)
        {
            Position = effectToCopy.Position;
            Damage = effectToCopy.Damage;
            Effect = effectToCopy.Effect;
            Name = effectToCopy.Name;
            Health = effectToCopy.Health;
            MaxHealth = effectToCopy.MaxHealth;
            MovementModifier = effectToCopy.MovementModifier;
            BlocksMovement = effectToCopy.BlocksMovement;
        }

        //use this to create a copy of the exact same tile effect (used for updating board state)
        public override HexGameObject Copy()
        {
            var te = new TileEffect(this);
            te.Id = Id;
            return te;
        }
    }
}