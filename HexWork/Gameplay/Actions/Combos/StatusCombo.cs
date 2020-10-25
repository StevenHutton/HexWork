using System.Threading.Tasks;
using HexWork.Gameplay.GameObject.Characters;
using HexWork.Gameplay.Interfaces;
using HexWork.UI.Interfaces;

namespace HexWork.Gameplay.Actions
{
    public class StatusCombo : DamageComboAction
    {
        public StatusEffect Effect = null;

        public StatusCombo()
        {
            Name = "Status Combo";
        }

        public override async Task TriggerAsync(BoardState state, Character character, IInputProvider input, IRulesProvider gameState)
        {
            var targetPosition = await input.GetTargetAsync(this);

            if (targetPosition == null)
                return;

            var targetCharacter = BoardState.GetEntityAtCoordinate(state, targetPosition);
            if (targetCharacter == null)
                return;

            if (!targetCharacter.HasStatus)
                return;

            var powerBonus = gameState.ApplyCombo(state, targetCharacter, this);
            gameState.ApplyStatus(state, targetCharacter, Effect);
            gameState.ApplyDamage(state, targetCharacter, (Power + powerBonus) * character.Power);
        }
    }
}
