using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HexWork.Gameplay.GameObject.Characters;
using HexWork.Gameplay.Interfaces;
using HexWork.UI.Interfaces;

namespace HexWork.Gameplay.Actions
{
    public class FixedMoveAction : HexAction
    {
        public FixedMoveAction(string name,
            TargetType targetType,
            DamageComboAction combo = null, TargetPattern targetPattern = null) :
            base(name,
                targetType,
                combo, targetPattern)
        {

        }

        public override async Task<BoardState> TriggerAsync(BoardState state, Guid characterId, IInputProvider input, IRulesProvider gameState)
        {
            var newState = state.Copy();
            var character = newState.GetEntityById(characterId) as Character;
            if (character == null)
                return state;

            var targetPosition = await input.GetTargetAsync(this);

            if (targetPosition == null || character.Position == targetPosition)
                return state;

            var position = character.Position;

            //check validity
            var validTargets = GetValidTargets(newState, character, TargetType);
            if (!validTargets.ContainsKey(targetPosition))
                return state;

            var potentialCost = validTargets[targetPosition];
            if (newState.Potential < potentialCost)
                return state;

            newState = gameState.MoveEntity(newState, characterId, new List<HexCoordinate>{ targetPosition });            
            newState = gameState.CreateTileEffect(newState, Element, position);

            return newState;
        }        
	}
}
