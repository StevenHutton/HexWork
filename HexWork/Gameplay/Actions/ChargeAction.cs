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

        public override async Task TriggerAsync(BoardState state, Character character, IInputProvider input, IRulesProvider gameState)
        {
            var targetPosition = await input.GetTargetAsync(this);

            if (targetPosition == null || character.Position == targetPosition)
                return;

            //check validity
            if (!IsValidTarget(state, character, targetPosition))
                return;

            if (PotentialCost != 0)
                gameState.LosePotential(state, PotentialCost);

            var position = character.Position;
            var direction = BoardState.GetPushDirection(position, targetPosition);

            List<HexCoordinate> path = new List<HexCoordinate>();

            while(position != targetPosition)
            {
                position += direction;
                path.Add(position);
            }            
            gameState.MoveEntity(state, character, path);
            
            var strikePosition = targetPosition + direction;

            ApplyToTile(state, strikePosition, gameState, character, direction);
        }

        /// <summary>
        /// Get a list of coordinates that are valid target locations for this action for the passed in character
        /// </summary>
        public override List<HexCoordinate> GetValidTargets(BoardState state, Character character)
        {
            return BoardState.GetVisibleAxisTilesInRange(state, character.Position, Range + character.RangeModifier).Where(d => BoardState.IsHexPassable(state, d)).ToList();
        }

        public override bool IsValidTarget(BoardState state, Character character, HexCoordinate targetCoordinate)
        {
            return GetValidTargets(state, character).Contains(targetCoordinate);
        }
    }
}