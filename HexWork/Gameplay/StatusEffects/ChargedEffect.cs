using HexWork.Gameplay.GameObject.Characters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HexWork.Gameplay.StatusEffects
{
    public class ChargedEffect : StatusEffect
    {
        public int Damage = 5;

        public ChargedEffect()
        {
            Name = "Electrified";
        }

        public ChargedEffect(ChargedEffect effect)
        {
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

            //todo - apply damage to all adjacent charged entities.
            foreach (var nb in BoardState.GetNeighbours(character.Position))
            {
                var ent = BoardState.GetEntityAtCoordinate(state, nb);
                if (ent == null) continue;

                if(ent.StatusEffects.Any(d => d.Name == "Electrified"))
                {
                    newState = ruleProvider.ApplyDamage(newState, ent.Id, Damage);
                    newState = ruleProvider.ApplyDamage(newState, characterId, Damage);
                }
            }

            newState = base.StartTurn(newState, characterId, ruleProvider);
            return newState;
        }

        public override StatusEffect Copy()
        {
            return new ChargedEffect(this);
        }
    }
}
