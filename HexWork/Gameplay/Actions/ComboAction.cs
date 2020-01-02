using System.Threading.Tasks;
using HexWork.Gameplay.Interfaces;
using HexWork.UI.Interfaces;

namespace HexWork.Gameplay.Actions
{
    public class ComboAction : HexAction
    {
        public ComboAction()
        {
            Name = "Damage Combo";
            Power = 5;
        }
        
        public override async Task TriggerAsync(Character character, IInputProvider input, IGameStateObject gameState)
        {
            var targetPosition = await input.GetTargetAsync(this);

            if (targetPosition == null)
                return;

            var targetCharacter = gameState.GetCharacterAtCoordinate(targetPosition);
            if (targetCharacter == null)
                return;

            if (!targetCharacter.HasStatus)
                return;

            var powerBonus = gameState.ApplyCombo(targetCharacter, this);
            gameState.ApplyStatus(targetCharacter, this.StatusEffect);
            gameState.ApplyDamage(targetCharacter, (Power + powerBonus) * character.Power);
        }
    }
}