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
            TargetType targetType,
            StatusEffect statusEffect = null,
            DamageComboAction combo = null, TargetPattern targetPattern = null) :
            base(name,
                targetType,
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
            if (!gameState.IsValidTarget(state, character, targetPosition, character.RangeModifier + Range, TargetType))
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

            gameState.CompleteAction(character, this);
        }
    }
}