using System;

namespace HexWork.Gameplay.StatusEffects
{
    public class ImmobalisedEffect : StatusEffect
    {
        public ImmobalisedEffect()
        {
            Name = "Immobalised";

            this.StatusEffectType = StatusEffectType.Rooted;
        }

        public ImmobalisedEffect(ImmobalisedEffect effect)
        {
            this.Name = effect.Name;
            this.Id = effect.Id;
            this.StatusEffectType = effect.StatusEffectType;
            this.TileEffect = effect.TileEffect;
            if (string.IsNullOrWhiteSpace(Name))
                Name = StatusEffectType.ToString();
        }

        public override BoardState StartTurn(BoardState state, Guid characterId, RulesProvider ruleProvider)
        {
            var newState = state.Copy();

            var character = newState.GetCharacterById(characterId);
            if (character == null)
                return state;

            newState.ActiveCharacterHasMoved = true;

            character.StatusEffects.RemoveAll(d => d.Id == this.Id);

            return newState;
        }

        public override StatusEffect Clone()
        {
            var de = new ImmobalisedEffect(this);
            de.Id = Guid.NewGuid();
            return de;
        }

        public override StatusEffect Copy()
        {
            return new ImmobalisedEffect(this);
        }
    }
}
