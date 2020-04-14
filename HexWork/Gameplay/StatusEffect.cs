using System;
using HexWork.Gameplay.Characters;

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

        void StartTurn(Character character, GameStateManager state);

        void Tick(Character character, GameStateManager state);

        void EndTurn(Character character, GameStateManager state);

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
	        if (string.IsNullOrWhiteSpace(Name))
		        Name = StatusEffectType.ToString();
        }

        public virtual StatusEffect Copy()
        {
            return new StatusEffect(this);
        }

        public virtual void StartTurn(Character character, GameStateManager state)
        {

        }

        public virtual void Tick(Character character, GameStateManager state)
        {

        }

        public virtual void EndTurn(Character character, GameStateManager state)
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
	        if (string.IsNullOrWhiteSpace(Name))
		        Name = StatusEffectType.ToString();
		}

        public override void StartTurn(Character character, GameStateManager state)
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
	        if (string.IsNullOrWhiteSpace(Name))
		        Name = StatusEffectType.ToString();
		}

        public override void StartTurn(Character character, GameStateManager state)
        {
            state.ApplyDamage(character, Damage, Name);
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
            if (string.IsNullOrWhiteSpace(Name))
                Name = StatusEffectType.ToString();
        }


        public override void StartTurn(Character character, GameStateManager state)
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
