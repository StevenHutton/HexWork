﻿using System;
using System.Linq;
using System.Threading.Tasks;
using HexWork.Gameplay.GameObject.Characters;
using HexWork.Gameplay.Interfaces;
using HexWork.UI.Interfaces;

namespace HexWork.Gameplay.Actions
{
    public class CommandAction : HexAction
    {
        public override async Task TriggerAsync(BoardState state, Character character, IInputProvider input, IRulesProvider gameState)
        {
            var zombies = gameState.BoardState.Enemies.Where(c => !c.IsHero && c.CharacterType == CharacterType.Zombie && c.IsAlive).ToList();
            var rand = new Random(DateTime.Now.Millisecond);
            
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

            gameState.CompleteAction(character, this);
        }
    }
}
