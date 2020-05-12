using System.Linq;
using System.Threading.Tasks;
using HexWork.Gameplay.GameObject.Characters;
using HexWork.Gameplay.Interfaces;
using HexWork.UI.Interfaces;

namespace HexWork.Gameplay.Actions
{
    public class SpawnAction : HexAction
    {
        public override async Task TriggerAsync(Character character, IInputProvider input, IGameStateObject gameState)
        {
            //spawn zombie
            var zombie = CharacterFactory.CreateZombie();

            var tile = gameState.CurrentGameState.GetNeighborCoordinates(character.Position)
                .FirstOrDefault(gameState.IsHexPassable);

            if (tile != null)
            {
                gameState.NotifyAction(this, character);
                zombie.SpawnAt(tile);
                gameState.SpawnCharacter(zombie);
            }

            character.HasActed = true;
        }
    }
}