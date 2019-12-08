using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HexWork.Gameplay.Interfaces;
using HexWork.UI.Interfaces;
using MonoGameTestProject.Gameplay;

namespace HexWork.Gameplay.Actions
{
    public class SpreadStatusCombo : ComboAction
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

            var targetCharacter = gameState.GetCharacterAtCoordinate(targetPosition);
            if (targetCharacter == null)
                return;

            if (!targetCharacter.HasStatus)
                return;

            gameState.ApplyCombo(targetCharacter, this);

            var nearestNeighbor = gameState.GetNearestNeighbor(character.Position, targetCharacter.Position);

            var direction = targetCharacter.Position - nearestNeighbor;

            while (Pattern.Pattern.All(coord => coord != direction))
            {
                Pattern.RotateClockwise();
            }

            foreach (var targetTile in GetTargetTiles(targetPosition))
            {
                var newTargetCharacter = gameState.GetCharacterAtCoordinate(targetTile);

                //if no one is there, next tile
                if (newTargetCharacter == null)
                    continue;

                if (AllySafe && newTargetCharacter.IsHero == character.IsHero)
                    continue;

                gameState.ApplyStatus(newTargetCharacter, targetCharacter.StatusEffects.FirstOrDefault());
                gameState.ApplyDamage(newTargetCharacter, Power * character.Power);
            }

            gameState.ApplyDamage(targetCharacter, Power * character.Power);
        }
    }
}