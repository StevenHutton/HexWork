using HexWork.Gameplay.Interfaces;
using HexWork.UI.Interfaces;
using System.Threading.Tasks;
using HexWork.Gameplay.GameObject.Characters;

namespace HexWork.Gameplay.Actions
{
    public class LineAction : HexAction
    {
        public LineAction(string name,
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

            var nearestNeighbor = BoardState.GetNearestNeighbor(character.Position, targetPosition);

            var direction = targetPosition - nearestNeighbor;

	        Pattern.RotatePatternTo(direction);

            var targetTiles = GetTargetTiles(targetPosition);

            foreach (var targetTile in targetTiles)
            {
                ApplyToTile(state, targetTile, gameState, character);
            }

            if (PotentialCost != 0)
                gameState.LosePotential(state, PotentialCost);

            gameState.CompleteAction(character, this);
        }
    }
}