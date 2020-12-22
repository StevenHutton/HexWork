using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HexWork.Gameplay.GameObject.Characters;
using HexWork.Gameplay.Interfaces;
using HexWork.UI.Interfaces;
using System;
using HexWork.Gameplay.GameObject;

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

        public override async Task<BoardState> TriggerAsync(BoardState state, Guid characterId, IInputProvider input, IRulesProvider gameState)
        {
            var newState = state.Copy();
            var character = newState.GetCharacterById(characterId);
            if (character == null)
                return state;

            var targetPosition = await input.GetTargetAsync(this);
            if (targetPosition == null)
                return state;

            var nearestNeighbor = BoardState.GetNearestNeighbor(character.Position, targetPosition);
            var direction = targetPosition - nearestNeighbor;

            while (Pattern.Pattern.All(coord => coord != direction))
            {
                Pattern.RotateClockwise();
            }

            var targetCharacter = BoardState.GetEntityAtCoordinate(newState, targetPosition);
            var statusEffect = targetCharacter?.StatusEffects.FirstOrDefault();
            if (statusEffect != null)
            {
                //the more status effects we detonate the more damage we add
                int powerBonus = 0;
                newState = gameState.ApplyCombo(newState, targetCharacter.Id, this, out powerBonus);

                foreach (var targetTile in GetTargetTiles(targetPosition))
                {
                    var newTargetCharacter = BoardState.GetEntityAtCoordinate(state, targetTile);

                    //if no one is there, next tile
                    if (newTargetCharacter == null)
                    {
                        if (statusEffect.TileEffect != null)
                            newState = gameState.CreateTileEffect(newState, statusEffect.TileEffect, targetTile);
                        continue;
                    }

                    if (AllySafe && newTargetCharacter.IsHero == character.IsHero)
                        continue;
                    newState = gameState.ApplyStatus(newState, newTargetCharacter.Id, statusEffect);
                    newState = gameState.ApplyDamage(newState, newTargetCharacter.Id, Power * character.Power);

                    var dir = PushFromCaster ?
                        BoardState.GetPushDirection(character.Position, targetTile) :
                        BoardState.GetPushDirection(targetPosition, targetTile);
                    newState = gameState.ApplyPush(newState, newTargetCharacter.Id, dir, PushForce);
                }
                newState = gameState.ApplyDamage(newState, targetCharacter.Id, (powerBonus + Power) * character.Power);
                return newState;
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

                    newState = gameState.ApplyCombo(newState, targetCharacter.Id, this, out _);
                    newState = gameState.ApplyStatus(newState, newTargetCharacter.Id, tileEffect.Effect);
                    newState = gameState.ApplyDamage(newState, newTargetCharacter.Id, Power * character.Power);
                    
                    var dir = PushFromCaster ?
                        BoardState.GetPushDirection(character.Position, targetTile) :
                        BoardState.GetPushDirection(targetPosition, targetTile);

                    newState = gameState.ApplyPush(newState, newTargetCharacter.Id, dir, PushForce);
                }
                else
                    newState = gameState.CreateTileEffect(newState, tileEffect, targetTile);
            }

            newState = gameState.ApplyDamage(newState, targetCharacter.Id, Power * character.Power);
            return newState;
        }
    }
}