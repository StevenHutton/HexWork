using System;
using System.Threading.Tasks;
using HexWork.Gameplay.GameObject.Characters;
using HexWork.Gameplay.Interfaces;
using HexWork.UI;
using HexWork.UI.Interfaces;

namespace HexWork.Gameplay.Actions
{
    public class SwapAction : HexAction
    {
        public SwapAction(string name,
            TargetType targetType,
            StatusEffect statusEffect = null,
            DamageComboAction combo = null, TargetPattern targetPattern = null) :
            base(name,
                targetType,
                statusEffect,
                combo, targetPattern)
        {
            CanRotateTargetting = false;
        }

        public override async Task<BoardState> TriggerAsync(BoardState state, Guid characterId, IInputProvider input, IRulesProvider gameState)
        {
            var newState = state.Copy();
            var character = newState.GetEntityById(characterId) as Character;
            if (character == null)
                return state;

            var targetPosition = await input.GetTargetAsync(this);
            if (targetPosition == null)
                return state;

            //check validity
            var validTargets = BoardState.GetValidTargets(newState, character, this, TargetType);
            if (!validTargets.ContainsKey(targetPosition))
                return state;

            var potentialCost = validTargets[targetPosition];
            if (newState.Potential < potentialCost)
                return state;

            var target = BoardState.GetEntityAtCoordinate(newState, targetPosition);

            if(target == null)
                return state;

            //swap positions of character and target character.
            var characterPosition = character.Position;

            newState = gameState.TeleportEntityTo(newState, characterId, targetPosition);
            newState = gameState.TeleportEntityTo(newState, target.Id, characterPosition);

            if (Combo != null)
                newState = await Combo.TriggerAsync(newState, characterId, new DummyInputProvider(characterPosition), gameState);
            newState = gameState.ApplyDamage(newState, target.Id, Power * character.Power);
            newState = gameState.ApplyStatus(newState, target.Id, StatusEffect);

            newState = gameState.CompleteAction(newState, characterId, this);

            return newState;
        }
    }
}
