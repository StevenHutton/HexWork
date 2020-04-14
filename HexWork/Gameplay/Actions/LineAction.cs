using HexWork.Gameplay.Interfaces;
using HexWork.UI;
using HexWork.UI.Interfaces;
using MonoGameTestProject.Gameplay;
using System.Linq;
using System.Threading.Tasks;
using HexWork.Gameplay.Characters;

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

        public override async Task TriggerAsync(Character character, IInputProvider input, IGameStateObject gameState)
        {
            var targetPosition = await input.GetTargetAsync(this);

            if (targetPosition == null)
                return;

            if (!gameState.IsValidTarget(character,
                targetPosition,
                character.RangeModifier + this.Range,
                _getValidTargets))
            {
                return;
            }

            var nearestNeighbor = gameState.GetNearestNeighbor(character.Position, targetPosition);

            var direction = targetPosition - nearestNeighbor;

	        Pattern.RotatePatternTo(direction);

            var targetTiles = GetTargetTiles(targetPosition);

            gameState.NotifyAction(this, character);

            foreach (var targetTile in targetTiles)
            {
                ApplyToTile(targetTile, gameState, character);
            }

            if (PotentialCost != 0)
                gameState.LosePotential(PotentialCost);
        }
    }
}