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
		Bleeding
	}

    public interface IStatusEffect 
    {
		StatusEffectType StatusEffectType { get; set; }

        StatusEffect Copy();

        void StartTurn(BoardState state, Character character, RulesProvider ruleProvider);

        void Tick(Character character, RulesProvider state);

        void EndTurn(Character character, RulesProvider state);

        void Reset();
    }

    public class StatusEffect : IStatusEffect
    {
	    public StatusEffectType StatusEffectType { get; set; }

        public int LifeSpan = 1;

		public int Duration = 1;

        public string Name;

        public bool IsExpired => (Duration <= 0);
	    public Guid Id { get; set; } = Guid.NewGuid();

        //The tile effect that corresponds to this status effect (if it has one).
        public TileEffect TileEffect;

	    public StatusEffect()
        {
            Duration = LifeSpan;
        }

        public StatusEffect(StatusEffect effect)
        {
            this.LifeSpan = effect.LifeSpan;
            this.Duration = effect.Duration;
            this.Name = effect.Name;
	        this.StatusEffectType = effect.StatusEffectType;
            this.TileEffect = effect.TileEffect;
	        if (string.IsNullOrWhiteSpace(Name))
		        Name = StatusEffectType.ToString();
        }

        public virtual StatusEffect Copy()
        {
            return new StatusEffect(this);
        }

        public virtual void StartTurn(BoardState state, Character character, RulesProvider ruleProvider)
        {

        }

        public virtual void Tick(Character character, RulesProvider state)
        {

        }

        public virtual void EndTurn(Character character, RulesProvider state)
        {
            Duration -= 1;
        }

        public void Reset()
        {
            Duration = LifeSpan;
        }
    }

    public class ImmobalisedEffect : StatusEffect
    {
        public ImmobalisedEffect()
        {
            Name = "Immobalised";

	        this.StatusEffectType = StatusEffectType.Rooted;
		}

        public ImmobalisedEffect(ImmobalisedEffect effect)
        {
            this.Duration = effect.Duration;
            this.Name = effect.Name;

	        this.StatusEffectType = effect.StatusEffectType;
            this.TileEffect = effect.TileEffect;
            if (string.IsNullOrWhiteSpace(Name))
		        Name = StatusEffectType.ToString();
		}

        public override void StartTurn(BoardState state, Character character, RulesProvider ruleProvider)
        {
            character.CanMove = false;
        }

        public override StatusEffect Copy()
        {
            return new ImmobalisedEffect(this);
        }
    }

    public class DotEffect : StatusEffect
    {
        public int Damage = 15;

        public DotEffect()
        {
            LifeSpan = 3;
            Duration = 3;
	        Name = "Burning";
		}

        public DotEffect(DotEffect effect)
        {
            this.LifeSpan = effect.LifeSpan;
            this.Duration = effect.Duration;
            this.Name = effect.Name;
            this.Damage = effect.Damage;
	        this.StatusEffectType = effect.StatusEffectType;
            this.TileEffect = effect.TileEffect;
            if (string.IsNullOrWhiteSpace(Name))
		        Name = StatusEffectType.ToString();
		}

        public override void StartTurn(BoardState state, Character character, RulesProvider ruleProvider)
        {
            ruleProvider.ApplyDamage(state, character, Damage, Name);
        }

        public override StatusEffect Copy()
        {
            return new DotEffect(this);
        }
    }

    public class FreezeEffect : StatusEffect
    {
        public FreezeEffect()
        {
            Name = "Frozen";
            Duration = 1;
            StatusEffectType = StatusEffectType.Frozen;
        }

        public FreezeEffect(FreezeEffect effect)
        {
            this.Duration = effect.Duration;
            this.Name = effect.Name;
            this.StatusEffectType = effect.StatusEffectType;
            this.TileEffect = effect.TileEffect;
            if (string.IsNullOrWhiteSpace(Name))
                Name = StatusEffectType.ToString();
        }


        public override void StartTurn(BoardState state, Character character, RulesProvider ruleProvider)
        {
            character.HasActed = true;
            character.CanAttack = false;
            character.CanMove = false;
        }

        public override StatusEffect Copy()
        {
            return new FreezeEffect(this);
        }
    }
}
