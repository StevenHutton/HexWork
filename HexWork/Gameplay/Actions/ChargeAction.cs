using HexWork.Gameplay.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;
using HexWork.Gameplay.GameObject.Characters;
using HexWork.UI.Interfaces;
using System;
using System.Linq;

namespace HexWork.Gameplay.Actions
{
    public class ChargeAction : HexAction
    {
        public ChargeAction(string name,
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
            var validTargets = BoardState.GetValidTargets(newState, character, this, TargetType);
            if (!validTargets.ContainsKey(targetPosition))
                return state;

            var potentialCost = validTargets[targetPosition];
            if (newState.Potential < potentialCost)
                return state;

            newState = rules.LosePotential(newState, potentialCost);

            var position = character.Position;
            var direction = BoardState.GetPushDirection(position, targetPosition);

            List<HexCoordinate> path = new List<HexCoordinate>();

            while(position != targetPosition)
            {
                position += direction;
                path.Add(position);
            }
            newState = rules.MoveEntity(newState, characterId, path);
            
            var strikePosition = targetPosition + direction;

            newState = await ApplyToTile(newState, strikePosition, rules, characterId, direction);

            newState = rules.CompleteAction(newState, characterId, this);
            return newState;
        }
    }
}