using System;
using System.Threading.Tasks;
using HexWork.Gameplay.Interfaces;
using HexWork.UI.Interfaces;

namespace HexWork.Gameplay.Actions
{
    public class StatusCombo : DamageComboAction
    {
        public Element Element = Element.None;

        public StatusCombo()
        {
            Name = "Status Combo";
        }

        public override async Task<BoardState> TriggerAsync(BoardState state, Guid characterId, IInputProvider input, IRulesProvider gameState)
        {
            var newState = state.Copy();
            var character = newState.GetCharacterById(characterId);
            if (character == null)
                return state;

            var targetPosition = await input.GetTargetAsync(this);

            if (targetPosition == null)
                return state;

            var targetCharacter = BoardState.GetEntityAtCoordinate(newState, targetPosition);
            if (targetCharacter == null)
                return state;

            if (!targetCharacter.HasStatus)
                return state;

            int powerBonus = 0;
            newState = gameState.ApplyCombo(newState, targetCharacter.Id, this, out powerBonus);
            newState = gameState.ApplyStatus(newState, targetCharacter.Id, Element);
            newState = gameState.ApplyDamage(newState, targetCharacter.Id, (Power + powerBonus) * character.Power);
            return newState;
        }
    }
}
