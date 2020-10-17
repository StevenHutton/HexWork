﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
            GetValidTargetsDelegate targetDelegate,
            StatusEffect statusEffect = null,
            DamageComboAction combo = null, TargetPattern targetPattern = null) :
            base(name,
                targetDelegate,
                statusEffect,
                combo, targetPattern)
        {
            CanRotateTargetting = false;
        }

        public override async Task TriggerAsync(Character character, IInputProvider input, IGameStateObject gameState)
        {
            var targetPosition = await input.GetTargetAsync(this);
            if (targetPosition == null)
                return;

            if (!_getValidTargets.Invoke(character, character.RangeModifier + this.Range, gameState)
                .Contains(targetPosition))
                return;

            var target = gameState.GetEntityAtCoordinate(targetPosition);

            if(target == null)
                return;

            gameState.NotifyAction(this, character);

            //swap positions of character and target character.
            var characterPosition = character.Position; 
            
            gameState.TeleportEntityTo(character, targetPosition);
            gameState.TeleportEntityTo(target, characterPosition);

            if (Combo != null)
                await Combo.TriggerAsync(character, new DummyInputProvider(characterPosition), gameState);
            gameState.ApplyDamage(target, Power * character.Power);
            gameState.ApplyStatus(target, StatusEffect);


            character.HasActed = true;
        }
    }
}
