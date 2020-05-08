using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HexWork.Gameplay.Characters;
using HexWork.Gameplay.Interfaces;
using HexWork.UI.Interfaces;
using MonoGameTestProject.Gameplay;

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
            Power = 2;
        }

        public override async Task TriggerAsync(Character character, IInputProvider input, IGameStateObject gameState)
        {
            var targetPosition = await input.GetTargetAsync(this);
            if (targetPosition == null)
                return;

            var nearestNeighbor = gameState.GetNearestNeighbor(character.Position, targetPosition);
            var direction = targetPosition - nearestNeighbor;

            while (Pattern.Pattern.All(coord => coord != direction))
            {
                Pattern.RotateClockwise();
            }

            var targetCharacter = gameState.GetCharacterAtCoordinate(targetPosition);
            var statusEffect = targetCharacter?.StatusEffects.FirstOrDefault();
            if (statusEffect != null)
            {
                var powerBonus = gameState.ApplyCombo(targetCharacter, this);

                foreach (var targetTile in GetTargetTiles(targetPosition))
                {
                    var newTargetCharacter = gameState.GetCharacterAtCoordinate(targetTile);

                    //if no one is there, next tile
                    if (newTargetCharacter == null)
                    {
                        if(statusEffect.TileEffect != null)
                            gameState.CreateTileEffect(targetTile, statusEffect.TileEffect);
                        
                        continue;
                    }

                    if (AllySafe && newTargetCharacter.IsHero == character.IsHero)
                        continue;

                    gameState.ApplyStatus(newTargetCharacter, statusEffect);
                    gameState.ApplyDamage(newTargetCharacter, Power * character.Power);
                    gameState.ApplyDamage(targetCharacter, (powerBonus + Power) * character.Power);
                }
            }

            var tileEffect = gameState.GetTileEffectAtCoordinate(targetPosition);
            if (tileEffect == null)
                return;
            
            foreach (var targetTile in GetTargetTiles(targetPosition))
            {
                var newTargetCharacter = gameState.GetCharacterAtCoordinate(targetTile);
                if (newTargetCharacter != null)
                {
                    if (AllySafe && newTargetCharacter.IsHero == character.IsHero)
                        continue;

                    tileEffect.TriggerEffect(gameState, newTargetCharacter);
                }
                else
                    gameState.CreateTileEffect(targetTile, tileEffect);
            }

            gameState.ResolveTileEffect(tileEffect);

        }
    }
}