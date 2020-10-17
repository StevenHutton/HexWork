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

		public override async Task TriggerAsync(Character character, IInputProvider input, IGameStateObject gameState)
		{
			var targetPosition = await input.GetTargetAsync(this);

			if (targetPosition == null || character.Position == targetPosition)
				return;

            var position = character.Position;

            if (gameState.IsValidDestination(character, targetPosition))
            {
                var path = gameState.FindShortestPath(position, targetPosition, gameState.CurrentGameState.Potential, character.MovementType, character.MovementSpeed);
                gameState.LosePotential(gameState.GetPathLengthToTile(character, targetPosition, path));
                gameState.MoveEntity(character, path);
            }
            
            if(TileEffect != null)
				gameState.CreateTileEffect(position, TileEffect);
        }		

        /// <summary>
        /// Get a list of coordinates that are valid target locations for this action for the passed in character
        /// </summary>
        public override List<HexCoordinate> GetValidTargets(Character character, IGameStateObject gameState)
        {
            return gameState.GetValidDestinations(character).Keys.ToList();
        }

        public override bool IsValidTarget(Character character, HexCoordinate targetCoordinate, IGameStateObject gameState)
        {
            return GetValidTargets(character, gameState).Contains(targetCoordinate);
        }
	}
}