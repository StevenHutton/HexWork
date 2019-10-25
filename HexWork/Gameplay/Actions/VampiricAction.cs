﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using HexWork.Gameplay.Interfaces;
using HexWork.UI;
using HexWork.UI.Interfaces;
using MonoGameTestProject.Gameplay;

namespace HexWork.Gameplay.Actions
{
    public class VampiricAction : HexAction
    {
        public VampiricAction(string name,
            GetValidTargetsDelegate targetDelegate,
            StatusEffect statusEffect = null,
            ComboAction combo = null, TargetPattern targetPattern = null) : base(name,
            targetDelegate,
            statusEffect,
            combo, targetPattern)
        { }

        public override async Task TriggerAsync(Character character, IInputProvider input, IGameStateObject gameState)
        {
            //get user input
            var targetPosition = await input.GetTargetAsync(this);
            if (targetPosition == null)
                return;
            //check validity
            if (!_getValidTargets.Invoke(character, character.RangeModifier + this.Range, gameState)
                .Contains(targetPosition))
                return;

            gameState.NotifyAction(this, character);

            int amountToHeal = 0;

            //loop through the affected tiles.
            var targetTiles = GetTargetTiles(targetPosition);
            foreach (var targetTile in targetTiles)
            {
                var targetCharacter = gameState.GetCharacterAtCoordinate(targetTile);

                //if no one is there, next tile
                if (targetCharacter == null)
                    continue;

                if (AllySafe && targetCharacter.IsHero == character.IsHero)
                    continue;

                amountToHeal += gameState.ApplyDamage(targetCharacter, Power);

                if (Combo != null)
                    await Combo.TriggerAsync(character, new DummyInputProvider(targetTile), gameState);

                gameState.ApplyStatus(targetCharacter, StatusEffect);

                gameState.CheckDied(targetCharacter);
            }

            gameState.ApplyHealing(character, amountToHeal);

            if (PotentialCost != 0)
                gameState.LosePotential(PotentialCost);
        }
    }
}