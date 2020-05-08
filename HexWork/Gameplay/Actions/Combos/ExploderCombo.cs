using System.Linq;
using System.Threading.Tasks;
using HexWork.Gameplay.Characters;
using HexWork.Gameplay.Interfaces;
using HexWork.UI.Interfaces;
using MonoGameTestProject.Gameplay;

namespace HexWork.Gameplay.Actions
{
    public class ExploderCombo : DamageComboAction
    {
        private TargetPattern _targetPattern;

        public ExploderCombo()
        {
            Name = "Exploder Combo";
            _targetPattern = new TargetPattern(new HexCoordinate(1, 0),
	            new HexCoordinate(-1, 0),
	            new HexCoordinate(0, 1),
	            new HexCoordinate(0, -1),
	            new HexCoordinate(-1, 1),
	            new HexCoordinate(1, -1));
			Power = 3;
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

            var powerBonus = gameState.ApplyCombo(targetCharacter, this);

            var nearestNeighbor = gameState.GetNearestNeighbor(character.Position, targetCharacter.Position);

            var direction = targetCharacter.Position - nearestNeighbor;

            while (_targetPattern.Pattern.All(coord => coord != direction))
            {
                _targetPattern.RotateClockwise();
            }

            foreach (var targetTile in _targetPattern.GetPattern(targetPosition))
            {
                var newTargetCharacter = gameState.GetCharacterAtCoordinate(targetTile);

                //if no one is there, next tile
                if (newTargetCharacter == null)
                    continue;

	            if (AllySafe && targetCharacter.IsHero == character.IsHero)
		            continue;

				gameState.ApplyDamage(newTargetCharacter, (Power+ powerBonus) * character.Power);
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