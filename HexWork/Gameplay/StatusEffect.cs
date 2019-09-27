using System;

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

        void StartTurn(Character character, GameState state);

        void Tick(Character character, GameState state);

        void EndTurn(Character character, GameState state);
    }

    public class StatusEffect : IStatusEffect
    {
	    public StatusEffectType StatusEffectType { get; set; }

		public int Duration { get; set; } = 1;

        public string Name { get; set; }

        public bool IsExpired => (Duration <= 0);
	    public Guid Id { get; set; } = Guid.NewGuid();

	    public StatusEffect()
        {

        }

        public StatusEffect(StatusEffect effect)
        {
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

        public virtual void StartTurn(Character character, GameState state)
        {

        }

        public virtual void Tick(Character character, GameState state)
        {

        }

        public virtual void EndTurn(Character character, GameState state)
        {
            Duration -= 1;
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

        public override void StartTurn(Character character, GameState state)
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
            Duration = 3;
	        Name = "Burning";
		}

        public DotEffect(DotEffect effect)
        {
            this.Duration = effect.Duration;
            this.Name = effect.Name;
            this.Damage = effect.Damage;
	        this.StatusEffectType = effect.StatusEffectType;
	        if (string.IsNullOrWhiteSpace(Name))
		        Name = StatusEffectType.ToString();
		}

        public override void StartTurn(Character character, GameState state)
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


        public override void StartTurn(Character character, GameState state)
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
