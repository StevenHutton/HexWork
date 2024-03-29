﻿using System;
using System.Threading.Tasks;
using HexWork.Gameplay.GameObject.Characters;
using HexWork.Gameplay.Interfaces;
using HexWork.UI.Interfaces;

namespace HexWork.Gameplay.Actions
{
	public class MoveAction : HexAction
    {
        public override bool IsAvailable(Character character, BoardState gameState)
        {
            return !gameState.ActiveCharacterHasMoved;
        }

        public MoveAction(string name,
			TargetType targetType,
			DamageComboAction combo = null, 
            TargetPattern targetPattern = null) :
			base(name,
				targetType,
				combo, targetPattern)
		{ }

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

            if (BoardState.IsValidDestination(newState, character, targetPosition, PotentialCost))
            {
                var path = BoardState.FindShortestPath(newState, position, targetPosition, newState.Potential, character.MovementType, character.MovementSpeed);
                newState = gameState.LosePotential(newState, BoardState.GetPathLengthToTile(newState, character, targetPosition, path));
                newState = gameState.MoveEntity(newState, characterId, path);
            }

            newState.ActiveCharacterHasMoved = true;
            return gameState.CreateTileEffect(newState, Element, position);
        }
	}
}