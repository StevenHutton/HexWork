﻿using System.Collections.Generic;
using System.Threading.Tasks;
using HexWork.Gameplay.Interfaces;
using HexWork.UI.Interfaces;
using MonoGameTestProject.Gameplay;

namespace HexWork.Gameplay.Actions
{
    public class FixedMoveAction : HexAction
    {
        public FixedMoveAction(string name,
            GetValidTargetsDelegate targetDelegate = null,
            StatusEffect statusEffect = null,
            DamageComboAction combo = null, TargetPattern targetPattern = null) :
            base(name,
                targetDelegate,
                statusEffect,
                combo, targetPattern)
        {

        }

        public override async Task TriggerAsync(Character character, IInputProvider input, IGameStateObject gameState)
        {
            var targetPosition = await input.GetTargetAsync(this);

            if (targetPosition == null || character.Position == targetPosition)
                return;

            var position = character.Position;

            if (gameState.IsValidDestination(character, targetPosition))
                gameState.MoveCharacterTo(character, targetPosition);

            if (TileEffect != null)
                gameState.CreateTileEffect(position, TileEffect);
        }

        public override bool IsAvailable(Character character)
        {
            var gameState = GameStateManager.CurrentGameState;

            return character.IsActive && (PotentialCost <= gameState.Potential) && character.CanMove;
        }

        /// <summary>
        /// Get a list of coordinates that are valid target locations for this action for the passed in character
        /// </summary>
        public override List<HexCoordinate> GetValidTargets(Character character, IGameStateObject gameState)
        {
            return gameState.GetValidDestinations(character);
        }

        public override bool IsValidTarget(Character character, HexCoordinate targetCoordinate, IGameStateObject gameState)
        {
            return gameState.GetValidDestinations(character).Contains(targetCoordinate);
        }
	}
}
