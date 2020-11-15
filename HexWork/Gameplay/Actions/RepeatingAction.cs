﻿using System;
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
            StatusEffect statusEffect = null,
            DamageComboAction combo = null, TargetPattern targetPattern = null) :
            base(name,
                targetType,
                statusEffect,
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

            //check validity
            if (!gameState.IsValidTarget(newState, character, targetPosition, character.RangeModifier + Range, TargetType))
                return state;

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
                    newState = gameState.ApplyStatus(newState, targetCharacter.Id, StatusEffect);
                }
            }

            newState = gameState.LosePotential(state, PotentialCost);
            
            return gameState.CompleteAction(newState, characterId, this);
        }

        #endregion
    }
}