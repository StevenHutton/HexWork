using HexWork.Gameplay.GameObject.Characters;
using System;

namespace HexWork.Gameplay.StatusEffects
{
    public class FreezeEffect : StatusEffect
    {
        public FreezeEffect()
        {
            Name = "Frozen";
            StatusEffectType = StatusEffectType.Frozen;
        }

        public FreezeEffect(FreezeEffect effect)
        {
            this.Name = effect.Name;
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
            newState.ActiveCharacterHasAttacked = true;
            newState = base.StartTurn(newState, characterId, ruleProvider);
            return newState;
        }

        public override StatusEffect Copy()
        {
            return new FreezeEffect(this);
        }
    }
}
