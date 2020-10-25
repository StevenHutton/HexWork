using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HexWork.Gameplay.GameObject.Characters;
using HexWork.Gameplay.Interfaces;
using HexWork.UI.Interfaces;

namespace HexWork.Gameplay.Actions
{
    public class SpreadStatusCombo : DamageComboAction
    {
        public SpreadStatusCombo()
        {
            Name = "Chain Combo";
            Pattern = new TargetPattern(new HexCoordinate(1,0), 
                new HexCoordinate(-1, 0),
                new HexCoordinate(0, 1),
                new HexCoordinate(0, -1),
                new HexCoordinate(-1, 1),
                new HexCoordinate(1, -1));
            Power = 1;
        }

        public override async Task TriggerAsync(BoardState state, Character character, IInputProvider input, IRulesProvider gameState)
        {
            var targetPosition = await input.GetTargetAsync(this);
            if (targetPosition == null)
                return;

            var nearestNeighbor = BoardState.GetNearestNeighbor(character.Position, targetPosition);
            var direction = targetPosition - nearestNeighbor;

            while (Pattern.Pattern.All(coord => coord != direction))
            {
                Pattern.RotateClockwise();
            }

            var targetCharacter = BoardState.GetEntityAtCoordinate(state, targetPosition);
            var statusEffect = targetCharacter?.StatusEffects.FirstOrDefault();
            if (statusEffect != null)
            {
                //the more status effects we detonate the more damage we add
                var powerBonus = gameState.ApplyCombo(state, targetCharacter, this);

                foreach (var targetTile in GetTargetTiles(targetPosition))
                {
                    var newTargetCharacter = BoardState.GetEntityAtCoordinate(state, targetTile);

                    //if no one is there, next tile
                    if (newTargetCharacter == null)
                    {
                        if (statusEffect.TileEffect != null)
                            gameState.CreateTileEffect(state, statusEffect.TileEffect, targetTile);

                        continue;
                    }

                    if (AllySafe && newTargetCharacter.IsHero == character.IsHero)
                        continue;

                    gameState.ApplyStatus(state, newTargetCharacter, statusEffect);
                    gameState.ApplyDamage(state, newTargetCharacter, Power * character.Power);

                    var dir = PushFromCaster ?
                        BoardState.GetPushDirection(character.Position, targetTile) :
                        BoardState.GetPushDirection(targetPosition, targetTile);
                    gameState.ApplyPush(state, newTargetCharacter, dir, PushForce);
                }
                gameState.ApplyDamage(state, targetCharacter, (powerBonus + Power) * character.Power);
            }

            var tileEffect = BoardState.GetTileEffectAtCoordinate(state, targetPosition);
            if (tileEffect == null)
                return;
            
            foreach (var targetTile in GetTargetTiles(targetPosition))
            {
                var newTargetCharacter = BoardState.GetEntityAtCoordinate(state, targetTile);
                if (newTargetCharacter != null)
                {
                    if (AllySafe && newTargetCharacter.IsHero == character.IsHero)
                        continue;

                    tileEffect.TriggerEffect(state, gameState, newTargetCharacter);
                }
                else
                    gameState.CreateTileEffect(state, tileEffect, targetTile);
            }

            gameState.ResolveTileEffect(state, tileEffect);
            gameState.RemoveTileEffect(state, tileEffect);
        }
    }
}