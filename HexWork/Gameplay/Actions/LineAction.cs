using HexWork.Gameplay.Interfaces;
using HexWork.UI.Interfaces;
using System.Threading.Tasks;
using HexWork.Gameplay.GameObject.Characters;
using System;

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

        public override async Task<BoardState> TriggerAsync(BoardState state, Guid characterId, IInputProvider input, IRulesProvider gameState)
        {
            var newState = state.Copy();
            var character = newState.GetEntityById(characterId) as Character;
            if (character == null)
                return state;

            var targetPosition = await input.GetTargetAsync(this);

            if (targetPosition == null)
                return state;

            //check validity
            if (!gameState.IsValidTarget(newState, character, targetPosition, character.RangeModifier + Range, TargetType))
                return state;

            var nearestNeighbor = BoardState.GetNearestNeighbor(character.Position, targetPosition);

            var direction = targetPosition - nearestNeighbor;

	        Pattern.RotatePatternTo(direction);

            var targetTiles = GetTargetTiles(targetPosition);

            foreach (var targetTile in targetTiles)
            {
                newState = await ApplyToTile(newState, targetTile, gameState, characterId);
            }

            newState = gameState.LosePotential(newState, PotentialCost);

            return gameState.CompleteAction(newState, characterId, this);
        }
    }
}