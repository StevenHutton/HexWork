using System;
using System.Linq;
using System.Threading.Tasks;
using HexWork.Gameplay.GameObject.Characters;
using HexWork.Gameplay.Interfaces;
using HexWork.UI.Interfaces;

namespace HexWork.Gameplay.Actions
{
    public class CommandAction : HexAction
    {
        public override async Task TriggerAsync(Character character, IInputProvider input, IGameStateObject gameState)
        {
            var zombies = gameState.CurrentGameState.Enemies.Where(c => !c.IsHero && c.CharacterType == CharacterType.Zombie && c.IsAlive).ToList();
            var rand = new Random(DateTime.Now.Millisecond);

            gameState.NotifyAction(this, character);
            
            if (zombies.Count == 0) return;
            var zombie = zombies[rand.Next(0, zombies.Count)];
            var zombie2 = zombies[rand.Next(0, zombies.Count)];
            zombie.StartTurn();
            zombie.DoTurn(gameState, zombie);
            zombie.EndTurn();
            zombie2.StartTurn();
            zombie2.DoTurn(gameState, zombie2);
            zombie2.EndTurn();

            character.HasActed = true;
        }
    }
}
