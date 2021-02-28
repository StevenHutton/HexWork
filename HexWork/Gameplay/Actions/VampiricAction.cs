using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using HexWork.Gameplay.GameObject.Characters;
using HexWork.Gameplay.Interfaces;
using HexWork.UI;
using HexWork.UI.Interfaces;

namespace HexWork.Gameplay.Actions
{
    public class VampiricAction : HexAction
    {
        public VampiricAction(string name,
            TargetType targetType,
            DamageComboAction combo = null, TargetPattern targetPattern = null) : base(name,
            targetType,
            combo, targetPattern)
        { }

        public override async Task<BoardState> TriggerAsync(BoardState state, Guid characterId, IInputProvider input, IRulesProvider gameState)
        {
            var newState = state.Copy();
            var character = newState.GetCharacterById(characterId);
            if (character == null)
                return state;

            //get user input
            var targetPosition = await input.GetTargetAsync(this);
            if (targetPosition == null)
                return state;

            var validTargets = GetValidTargets(newState, character, TargetType);
            if (!validTargets.ContainsKey(targetPosition))
                return state;

            var potentialCost = validTargets[targetPosition];
            if (newState.Potential < potentialCost)
                return state;
            newState = gameState.LosePotential(newState, potentialCost);

            int amountToHeal = 0;

            //loop through the affected tiles.
            var targetTiles = GetTargetTiles(targetPosition);
            foreach (var targetTile in targetTiles)
            {
                var targetCharacter = BoardState.GetEntityAtCoordinate(newState, targetTile);

                //if no one is there, next tile
                if (targetCharacter == null)
                    continue;

                if (AllySafe && targetCharacter.IsHero == character.IsHero)
                    continue;

                if (Combo != null)
                    newState = await Combo.TriggerAsync(newState, characterId, new DummyInputProvider(targetTile), gameState);

                newState = gameState.ApplyDamage(newState, targetCharacter.Id, Power * character.Power);
                newState = gameState.ApplyStatus(newState, targetCharacter.Id, Element);
                amountToHeal += Power * character.Power;
            }

            newState = gameState.ApplyHealing(newState, character.Id, amountToHeal);

            return gameState.CompleteAction(newState, character.Id, this);
        }
    }
}
