using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HexWork.Gameplay.GameObject.Characters;
using HexWork.Gameplay.Interfaces;
using HexWork.UI;
using HexWork.UI.Interfaces;

namespace HexWork.Gameplay.Actions
{
    public class RepeatingAction : HexAction
    {
        #region Attributes
        
        #endregion

        #region Properties

        public int NumberOfAttacks { get; set; } = 3;

        #endregion

        #region Methods

        public RepeatingAction()
        { }

        public RepeatingAction(string name,
            TargetType targetType,
            DamageComboAction combo = null, TargetPattern targetPattern = null) :
            base(name,
                targetType,
                combo, targetPattern)
        { }
        
        public override async Task<BoardState> TriggerAsync(BoardState state, Guid characterId, IInputProvider input, IRulesProvider gameState)
        {
            var newState = state.Copy();
            var character = newState.GetCharacterById(characterId);
            if (character == null)
                return state;

            var targetPosition = await input.GetTargetAsync(this);
            if (targetPosition == null)
                return state;

            var validTargets = BoardState.GetValidTargets(newState, character, this, TargetType);
            if (!validTargets.ContainsKey(targetPosition))
                return state;

            var potentialCost = validTargets[targetPosition];
            if (newState.Potential < potentialCost)
                return state;

            newState = gameState.LosePotential(newState, potentialCost);

            var targetTiles = GetTargetTiles(targetPosition);

            for (int i = NumberOfAttacks - 1; i >= 0; i--)
            {
                foreach (var targetTile in targetTiles)
                {
                    var targetCharacter = BoardState.GetEntityAtCoordinate(state, targetTile);

                    //if no one is there, next tile
                    if (targetCharacter == null)
                        continue;

                    if (AllySafe && targetCharacter.IsHero == character.IsHero)
                        continue;

                    if (Combo != null)
                        newState = await Combo.TriggerAsync(newState, characterId, new DummyInputProvider(targetPosition), gameState);
                    newState = gameState.ApplyDamage(newState, targetCharacter.Id, Power * character.Power);
                    newState = gameState.ApplyStatus(newState, targetCharacter.Id, Element);
                }
            }
                        
            return gameState.CompleteAction(newState, characterId, this);
        }

        #endregion
    }
}