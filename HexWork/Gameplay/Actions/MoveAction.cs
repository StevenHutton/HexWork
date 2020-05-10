using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HexWork.Gameplay.Characters;
using HexWork.Gameplay.GameObject.Characters;
using HexWork.Gameplay.Interfaces;
using HexWork.UI.Interfaces;
using MonoGameTestProject.Gameplay;

namespace HexWork.Gameplay.Actions
{
	public class MoveAction : HexAction
    {
        public override bool IsAvailable(Character character)
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
                gameState.LosePotential(gameState.GetPathLengthToTile(character, targetPosition));
                gameState.MoveEntityTo(character, targetPosition);
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
            return gameState.GetValidDestinations(character).Keys.Contains(targetCoordinate);
        }
	}
}