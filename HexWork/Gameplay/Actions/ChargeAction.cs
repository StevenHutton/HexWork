using HexWork.Gameplay.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;
using HexWork.Gameplay.GameObject.Characters;
using HexWork.UI.Interfaces;
using System.Linq;

namespace HexWork.Gameplay.Actions
{
    public class ChargeAction : HexAction
    {
        public ChargeAction(string name,
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

            //check validity
            if (!IsValidTarget(character, targetPosition, gameState))
                return;

            gameState.NotifyAction(this, character);

            if (PotentialCost != 0)
                gameState.LosePotential(PotentialCost);

            var position = character.Position;
            var direction = GameState.GetPushDirection(position, targetPosition);

            List<HexCoordinate> path = new List<HexCoordinate>();

            while(position != targetPosition)
            {
                position += direction;
                path.Add(position);
            }            
            gameState.MoveEntity(character, path);
            
            var strikePosition = targetPosition + direction;

            ApplyToTile(strikePosition, gameState, character, direction);
        }

        /// <summary>
        /// Get a list of coordinates that are valid target locations for this action for the passed in character
        /// </summary>
        public override List<HexCoordinate> GetValidTargets(Character character, IGameStateObject gameState)
        {
            return gameState.GetVisibleAxisTilesInRange(character, Range + character.RangeModifier).Where(d => gameState.IsHexPassable(d)).ToList();
        }

        public override bool IsValidTarget(Character character, HexCoordinate targetCoordinate, IGameStateObject gameState)
        {
            return GetValidTargets(character, gameState).Contains(targetCoordinate);
        }
    }
}