using HexWork.Gameplay.GameObject.Characters;
using System;

namespace HexWork.Gameplay.StatusEffects
{
    public class DotEffect : StatusEffect
    {
        public int Damage = 15;

        public DotEffect()
        {
            Name = "Burning";
        }

        public DotEffect(DotEffect effect)
        {
            this.Id = effect.Id;
            this.Name = effect.Name;
            this.Damage = effect.Damage;
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
            newState = ruleProvider.ApplyDamage(newState, character.Id, Damage);
            newState = base.StartTurn(newState, characterId, ruleProvider);
            return newState;
        }

        public override StatusEffect Clone()
        {
            var de  = new DotEffect(this);
            de.Id = Guid.NewGuid();
            return de;
        }

        public override StatusEffect Copy()
        {
            return new DotEffect(this);
        }
    }
}
