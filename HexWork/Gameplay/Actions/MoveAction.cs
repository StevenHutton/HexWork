using System.Collections.Generic;
using System.Linq;
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
            return character.CanMove;
        }

        public MoveAction(string name,
			GetValidTargetsDelegate targetDelegate= null,
			StatusEffect statusEffect = null,
			DamageComboAction combo = null, 
            TargetPattern targetPattern = null) :
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

            if (BoardState.IsValidDestination(state, character, targetPosition))
            {
                var path = BoardState.FindShortestPath(state, position, targetPosition, state.Potential, character.MovementType, character.MovementSpeed);
                gameState.LosePotential(state, BoardState.GetPathLengthToTile(state, character, targetPosition, path));
                gameState.MoveEntity(state, character, path);
            }
            
            if(TileEffect != null)
				gameState.CreateTileEffect(state, TileEffect, position);
        }		

        /// <summary>
        /// Get a list of coordinates that are valid target locations for this action for the passed in character
        /// </summary>
        public override List<HexCoordinate> GetValidTargets(BoardState state, Character character)
        {
            return BoardState.GetValidDestinations(state, character).Keys.ToList();
        }

        public override bool IsValidTarget(BoardState state, Character character, HexCoordinate targetCoordinate)
        {
            return GetValidTargets(state, character).Contains(targetCoordinate);
        }
	}
}