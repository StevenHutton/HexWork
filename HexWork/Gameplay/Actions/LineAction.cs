using HexWork.Gameplay.Interfaces;
using HexWork.UI;
using HexWork.UI.Interfaces;
using System.Linq;
using System.Threading.Tasks;
using HexWork.Gameplay.GameObject.Characters;

namespace HexWork.Gameplay.Actions
{
    public class LineAction : HexAction
    {
        public LineAction(string name,
            GetValidTargetsDelegate targetDelegate,
            StatusEffect statusEffect = null,
            DamageComboAction combo = null, TargetPattern targetPattern = null) :
            base(name,
                targetDelegate,
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

            if (_getValidTargets?.Invoke(state, character.Position, character.RangeModifier + this.Range).Contains(targetPosition) ?? false == false)
                return;

            if (!RulesProvider.IsValidTarget(state, character,
                targetPosition,
                character.RangeModifier + this.Range,
                _getValidTargets))
            {
                return;
            }

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


            character.HasActed = true;
        }
    }
}