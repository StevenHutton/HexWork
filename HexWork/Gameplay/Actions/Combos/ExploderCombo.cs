using System.Linq;
using System.Threading.Tasks;
using HexWork.Gameplay.GameObject.Characters;
using HexWork.Gameplay.Interfaces;
using HexWork.UI.Interfaces;

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

        public override async Task TriggerAsync(BoardState state, Character character, IInputProvider input, IRulesProvider gameState)
        {
            var targetPosition = await input.GetTargetAsync(this);

            if (targetPosition == null)
                return;

            var targetCharacter = BoardState.GetEntityAtCoordinate(state, targetPosition);
            if (targetCharacter == null)
                return;

            if (!targetCharacter.HasStatus)
                return;

            var powerBonus = gameState.ApplyCombo(state, targetCharacter, this);

            var nearestNeighbor = BoardState.GetNearestNeighbor(character.Position, targetCharacter.Position);

            var direction = targetCharacter.Position - nearestNeighbor;

            while (_targetPattern.Pattern.All(coord => coord != direction))
            {
                _targetPattern.RotateClockwise();
            }

            foreach (var targetTile in _targetPattern.GetPattern(targetPosition))
            {
                var newTargetCharacter = BoardState.GetEntityAtCoordinate(state, targetTile);

                //if no one is there, next tile
                if (newTargetCharacter == null)
                    continue;

	            if (AllySafe && targetCharacter.IsHero == character.IsHero)
		            continue;

				gameState.ApplyDamage(state, newTargetCharacter, (Power + powerBonus) * character.Power);
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
        }
    }
}