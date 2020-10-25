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
            GetValidTargetsDelegate targetDelegate = null,
            StatusEffect statusEffect = null,
            DamageComboAction combo = null, TargetPattern targetPattern = null) :
            base(name,
                targetDelegate,
                statusEffect,
                combo, targetPattern)
        {

        }

        public override async Task TriggerAsync(BoardState state, Character character, IInputProvider input, IRulesProvider gameState)
        {
            var targetPosition = await input.GetTargetAsync(this);

            if (targetPosition == null || character.Position == targetPosition)
                return;

            var position = character.Position;

            if (IsValidTarget(state, character, targetPosition))
                gameState.MoveEntity(state, character, new List<HexCoordinate>{ targetPosition });

            if (TileEffect != null)
                gameState.CreateTileEffect(state, TileEffect, position);

            character.CanMove = false;
        }
        
        /// <summary>
        /// Get a list of coordinates that are valid target locations for this action for the passed in character
        /// </summary>
        public override List<HexCoordinate> GetValidTargets(BoardState state, Character character)
        {
            return BoardState.GetWalkableAdjacentTiles(state, character.Position, character.MovementType);
        }

        public override bool IsValidTarget(BoardState state, Character character, HexCoordinate targetCoordinate)
        {
            return BoardState.GetWalkableAdjacentTiles(state, character.Position, character.MovementType).Contains(targetCoordinate);
        }
	}
}
