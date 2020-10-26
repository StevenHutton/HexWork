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

            var position = character.Position;

            //check validity
            if (!gameState.IsValidTarget(state, character, targetPosition, character.RangeModifier + Range, TargetType))
                return;

            gameState.MoveEntity(state, character, new List<HexCoordinate>{ targetPosition });

            if (TileEffect != null)
                gameState.CreateTileEffect(state, TileEffect, position);

            character.CanMove = false;
        }        
	}
}
