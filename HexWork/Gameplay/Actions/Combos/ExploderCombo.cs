using System.Linq;
using System.Threading.Tasks;
using HexWork.Gameplay.GameObject.Characters;
using HexWork.Gameplay.Interfaces;
using HexWork.UI.Interfaces;
using System;

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

        public override async Task<BoardState> TriggerAsync(BoardState state, Guid characterId, IInputProvider input, IRulesProvider rulesProvider)
        {
            var newState = state.Copy();
            var character = newState.GetCharacterById(characterId);
            if (character == null)
                return state;

            var targetPosition = await input.GetTargetAsync(this);

            if (targetPosition == null)
                return state;

            var targetCharacter = BoardState.GetEntityAtCoordinate(newState, targetPosition);
            if (targetCharacter == null)
                return state;

            if (!targetCharacter.HasStatus)
                return state;

            int powerBonus;

            newState = rulesProvider.ApplyCombo(newState, targetCharacter.Id, this, out powerBonus);

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

                newState = rulesProvider.ApplyDamage(newState, newTargetCharacter.Id, (Power + powerBonus) * character.Power);
            }

            var tileEffect = BoardState.GetTileEffectAtCoordinate(newState, targetPosition);
            if (tileEffect == null)
                return newState;

            foreach (var targetTile in GetTargetTiles(targetPosition))
            {
                var newTargetCharacter = BoardState.GetEntityAtCoordinate(newState, targetTile);
                if (newTargetCharacter != null)
                {
                    if (AllySafe && newTargetCharacter.IsHero == character.IsHero)
                        continue;

                    newState = await tileEffect.TriggerEffect(newState, rulesProvider);
                }
                else
                    newState = rulesProvider.CreateTileEffect(newState, Element, targetTile);
            }

            newState = rulesProvider.ResolveTileEffect(newState, tileEffect.Position);
            return newState;
        }
    }
}