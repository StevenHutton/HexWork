using HexWork.Gameplay.GameObject.Characters;
using HexWork.Gameplay.Interfaces;
using HexWork.UI.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HexWork.Gameplay.Actions
{
    public class DashAction : HexAction
    {
        public MovementType MovementType = MovementType.MoveThroughUnits;

        public DashAction(string name,
            TargetType targetType,
            DamageComboAction combo = null, TargetPattern targetPattern = null) :
            base(name,
                targetType,
                combo, targetPattern)
        {
        }

        public override async Task<BoardState> TriggerAsync(BoardState state, Guid characterId, IInputProvider input, IRulesProvider rules)
        {
            var newState = state.Copy();
            var character = newState.GetEntityById(characterId) as Character;
            if (character == null)
                return state;

            var targetPosition = await input.GetTargetAsync(this);

            if (targetPosition == null || character.Position == targetPosition)
                return state;

            //check validity
            var validTargets = GetValidTargets(newState, character, TargetType);
            if (!validTargets.ContainsKey(targetPosition))
                return state;

            var potentialCost = validTargets[targetPosition];
            if (newState.Potential < potentialCost)
                return state;

            newState = rules.LosePotential(newState, potentialCost);

            var position = character.Position;
            var direction = BoardState.GetPushDirection(position, targetPosition);
            
            while (position != targetPosition)
            {
                var newPosition = position + direction;
                newState = rules.MoveEntity(newState, characterId, new List<HexCoordinate>() { newPosition });
                newState = await ApplyToTile(newState, position, rules, characterId, direction);
                position = newPosition;
            }
            newState = rules.MoveEntity(newState, characterId, new List<HexCoordinate>() { targetPosition });
            newState = rules.CompleteAction(newState, characterId, this);
            return newState;
        }

        public override Dictionary<HexCoordinate, int> GetValidTargets(BoardState state,
            Character objectCharacter, TargetType targetType)
        {
            var position = objectCharacter.Position;

            switch (targetType)
            {
                case TargetType.Free:
                    return BoardState.GetVisibleTilesInRange(state, position, objectCharacter.RangeModifier + Range, PotentialCost);
                case TargetType.FreeIgnoreUnits:
                    return BoardState.GetVisibleTilesInRangeIgnoreUnits(state, position, objectCharacter.RangeModifier + Range, PotentialCost);
                case TargetType.FreeIgnoreLos:
                    return BoardState.GetTilesInRange(state, position, objectCharacter.RangeModifier + Range, PotentialCost);
                case TargetType.AxisAligned:
                    return BoardState.GetVisibleAxisTilesInRange(state, position, objectCharacter.RangeModifier + Range, PotentialCost);
                case TargetType.AxisAlignedIgnoreUnits:
                    return BoardState.GetVisibleAxisTilesInRangeIgnoreUnits(state, position, objectCharacter.RangeModifier + Range, PotentialCost);
                case TargetType.AxisAlignedIgnoreLos:
                    return BoardState.GetAxisTilesInRange(state, position, objectCharacter.RangeModifier + Range, PotentialCost);
                case TargetType.Move:
                    return BoardState.GetValidDestinations(state, position, objectCharacter, PotentialCost);
                case TargetType.FixedMove:
                    return BoardState.GetWalkableAdjacentTiles(state, position, PotentialCost);
                case TargetType.AxisAlignedFixedMove:
                    return BoardState.GetWalkableAxisTiles(state, position, MovementType, objectCharacter.MovementSpeed, objectCharacter.RangeModifier + Range, PotentialCost);
                default:
                    return null;
            }
        }
    }
}
