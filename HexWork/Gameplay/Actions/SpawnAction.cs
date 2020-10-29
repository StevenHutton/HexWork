using System.Linq;
using System.Threading.Tasks;
using HexWork.Gameplay.GameObject.Characters;
using HexWork.Gameplay.Interfaces;
using HexWork.UI.Interfaces;

namespace HexWork.Gameplay.Actions
{
    public class SpawnAction : HexAction
    {
        public override async Task TriggerAsync(BoardState state, Character character, IInputProvider input, IRulesProvider gameState)
        {
            //spawn zombie
            var zombie = CharacterFactory.CreateZombie();

            var tile = BoardState.GetNeighbours(character.Position)
                .FirstOrDefault(d => BoardState.IsHexPassable(gameState.BoardState, d));

            if (tile != null)
            {
                zombie.SpawnAt(tile);
                gameState.AddEntity(gameState.BoardState, zombie);
            }

            gameState.CompleteAction(character, this);
        }
    }
}