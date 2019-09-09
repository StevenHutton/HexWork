﻿using HexWork.Gameplay.Interfaces;
using HexWork.UI;
using HexWork.UI.Interfaces;
using MonoGameTestProject.Gameplay;
using System.Linq;
using System.Threading.Tasks;

namespace HexWork.Gameplay.Actions
{
    public class LineAction : HexAction
    {
        public LineAction(string name,
            GetValidTargetsDelegate targetDelegate,
            StatusEffect statusEffect = null,
            ComboAction combo = null, TargetPattern targetPattern = null) :
            base(name,
                targetDelegate,
                statusEffect,
                combo, targetPattern)
        { }

        public override async Task TriggerAsync(Character character, IInputProvider input, IGameStateObject gameState)
        {
            var targetPosition = await input.GetTargetAsync(this);

            if (targetPosition == null)
                return;

            if (!gameState.IsValidTarget(character,
                targetPosition,
                character.RangeModifier + this.Range,
                _getValidTargets))
            {
                return;
            }

            var nearestNeighbor = gameState.GetNearestNeighbor(character.Position, targetPosition);

            var direction = targetPosition - nearestNeighbor;

            while (Pattern.Pattern.All(coord => coord != direction))
            {
                Pattern.RotateClockwise();
            }

            var targetTiles = GetTargetTiles(targetPosition);

            gameState.NotifyAction(this, character);

            foreach (var targetTile in targetTiles)
            {
                var targetCharacter = gameState.GetCharacterAtCoordinate(targetTile);

                //if no one is there, next tile
                if (targetCharacter == null)
                    continue;

                if (AllySafe && targetCharacter.IsHero == character.IsHero)
                    continue;

                gameState.ApplyDamage(targetCharacter, Power);
                gameState.ApplyStatus(targetCharacter, StatusEffect);

                if (Combo != null)
                    await Combo.TriggerAsync(character, new DummyInputProvider(targetPosition), gameState);
            }

            if (PotentialCost != 0)
                gameState.LosePotential(PotentialCost);
        }
    }
}