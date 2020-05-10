using System.Threading.Tasks;
using HexWork.Gameplay.Characters;
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

        public override async Task TriggerAsync(Character character, IInputProvider input, IGameStateObject gameState)
        {
            var targetPosition = await input.GetTargetAsync(this);

            if (targetPosition == null)
                return;

            var targetCharacter = gameState.GetEntityAtCoordinate(targetPosition);
            if (targetCharacter == null)
                return;

            if (!targetCharacter.HasStatus)
                return;

            var powerBonus = gameState.ApplyCombo(targetCharacter, this);
            gameState.ApplyStatus(targetCharacter, Effect);
            gameState.ApplyDamage(targetCharacter, (Power + powerBonus) * character.Power);
        }
    }
}
