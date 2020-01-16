using System.Collections.Generic;
using System.Threading.Tasks;
using HexWork.Gameplay.Interfaces;
using HexWork.UI.Interfaces;
using MonoGameTestProject.Gameplay;

namespace HexWork.Gameplay.Actions
{
	public class MoveAction : HexAction
    {
        public bool IsFixedMovement = false;

        private int GetMovementRange(Character c)
        {
            return IsFixedMovement ? Range : c.Movement + Range;
        }

		public MoveAction(string name,
			GetValidTargetsDelegate targetDelegate,
			StatusEffect statusEffect = null,
			ComboAction combo = null, TargetPattern targetPattern = null) :
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

			if (gameState.IsValidDestination(character, targetPosition, GetMovementRange(character)))
				gameState.MoveCharacterTo(character, targetPosition);
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
			return _getValidTargets?.Invoke(character, GetMovementRange(character), gameState);
		}

		public override bool IsValidTarget(Character character, HexCoordinate targetCoordinate, IGameStateObject gameState)
		{
			return _getValidTargets != null && _getValidTargets.Invoke(character, GetMovementRange(character), gameState).Contains(targetCoordinate);
		}
	}
}