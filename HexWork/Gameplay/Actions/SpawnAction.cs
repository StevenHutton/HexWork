using System;
using System.Linq;
using System.Threading.Tasks;
using HexWork.Gameplay.GameObject.Characters;
using HexWork.Gameplay.Interfaces;
using HexWork.UI.Interfaces;

namespace HexWork.Gameplay.Actions
{
    public class SpawnAction : HexAction
    {
        public override async Task<BoardState> TriggerAsync(BoardState state, Guid characterId, IInputProvider input, IRulesProvider gameState)
        {
            var newState = state.Copy();
            var character = newState.GetCharacterById(characterId);
            if (character == null)
                return state;

            //spawn zombie
            var zombie = CharacterFactory.CreateZombie();

            var tile = BoardState.GetNeighbours(character.Position)
                .FirstOrDefault(d => BoardState.IsHexPassable(newState, d));

            if (tile != null)
            {
                zombie.SpawnAt(tile);
                newState = gameState.AddEntity(newState, zombie);
            }

            return gameState.CompleteAction(newState, characterId, this);
        }
    }
}