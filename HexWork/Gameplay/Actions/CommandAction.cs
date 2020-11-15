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
        public override async Task<BoardState> TriggerAsync(BoardState state, Guid characterId, IInputProvider input, IRulesProvider gameState)
        {
            var newState = state.Copy();
            var character = newState.GetEntityById(characterId) as Character;
            if (character == null)
                return state;

            var zombies = newState.Enemies.Where(c => !c.IsHero && c.CharacterType == CharacterType.Zombie && c.IsAlive).ToList();
            var rand = new Random(DateTime.Now.Millisecond);
            
            if (zombies.Count == 0) return state;
            var zombie = zombies[rand.Next(0, zombies.Count)];
            var zombie2 = zombies[rand.Next(0, zombies.Count)];
            zombie.StartTurn();
            newState = zombie.DoTurn(newState, gameState, zombie);
            zombie.EndTurn();
            zombie2.StartTurn();
            newState = zombie2.DoTurn(newState, gameState, zombie2);
            zombie2.EndTurn();

            character.HasActed = true;

            return gameState.CompleteAction(newState, characterId, this);
        }
    }
}
