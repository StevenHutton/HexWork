using HexWork.Gameplay.GameObject.Characters;
using HexWork.Gameplay.Interfaces;
using HexWork.UI.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HexWork.Gameplay.Actions
{
    public class DashAction : HexAction
    {
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
            var validTargets = BoardState.GetValidTargets(newState, character, this, TargetType);
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
    }
}
