using System;
using HexWork.Gameplay.GameObject;
using HexWork.Gameplay.GameObject.Characters;

namespace HexWork.Gameplay
{
	public enum StatusEffectType
	{
		Burning,
		Frozen,
		Rooted,
		Bleeding,
        Electrified
    }

    public interface IStatusEffect 
    {
		StatusEffectType StatusEffectType { get; set; }

        StatusEffect Copy();

        StatusEffect Clone();

        BoardState StartTurn(BoardState state, Guid characterId, RulesProvider ruleProvider);
    }

    public class StatusEffect : IStatusEffect
    {
	    public StatusEffectType StatusEffectType { get; set; }

        public string Name;

	    public Guid Id { get; set; } = Guid.NewGuid();

        //The tile effect that corresponds to this status effect (if it has one).
        public TileEffect TileEffect;

	    public StatusEffect()
        {
        }

        public StatusEffect(StatusEffect effect)
        {
            this.Id = effect.Id;
            this.Name = effect.Name;
	        this.StatusEffectType = effect.StatusEffectType;
            this.TileEffect = effect.TileEffect;
	        if (string.IsNullOrWhiteSpace(Name))
		        Name = StatusEffectType.ToString();
        }

        //creates an exact copy of this instance WITH IT'S OWN ID
        public virtual StatusEffect Clone()
        {
            return new StatusEffect()
            {
                Name = this.Name,
                StatusEffectType = this.StatusEffectType,
                TileEffect = this.TileEffect
            };
        }

        //creates an exact copy of this instance
        public virtual StatusEffect Copy()
        {
            return new StatusEffect()
            {
                Id = this.Id,
                Name = this.Name,
                StatusEffectType = this.StatusEffectType,
                TileEffect = this.TileEffect
            };
        }

        public virtual BoardState StartTurn(BoardState state, Guid characterId, RulesProvider ruleProvider)
        {
            return state;
        }
    }
}
