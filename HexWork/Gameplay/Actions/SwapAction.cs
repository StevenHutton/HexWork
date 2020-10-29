using System.Threading.Tasks;
using HexWork.Gameplay.GameObject.Characters;
using HexWork.Gameplay.Interfaces;
using HexWork.UI;
using HexWork.UI.Interfaces;

namespace HexWork.Gameplay.Actions
{
    public class SwapAction : HexAction
    {
        public SwapAction(string name,
            TargetType targetType,
            StatusEffect statusEffect = null,
            DamageComboAction combo = null, TargetPattern targetPattern = null) :
            base(name,
                targetType,
                statusEffect,
                combo, targetPattern)
        {
            CanRotateTargetting = false;
        }

        public override async Task TriggerAsync(BoardState state, Character character, IInputProvider input, IRulesProvider gameState)
        {
            var targetPosition = await input.GetTargetAsync(this);
            if (targetPosition == null)
                return;

            //check validity
            if (!gameState.IsValidTarget(state, character, targetPosition, character.RangeModifier + Range, TargetType))
                return;

            var target = BoardState.GetEntityAtCoordinate(state, targetPosition);

            if(target == null)
                return;

            //swap positions of character and target character.
            var characterPosition = character.Position; 
            
            gameState.TeleportEntityTo(state, character, targetPosition);
            gameState.TeleportEntityTo(state, target, characterPosition);

            if (Combo != null)
                await Combo.TriggerAsync(state, character, new DummyInputProvider(characterPosition), gameState);
            gameState.ApplyDamage(state, target, Power * character.Power);
            gameState.ApplyStatus(state, target, StatusEffect);

            gameState.CompleteAction(character, this);
        }
    }
}
