using System;
using System.Threading.Tasks;
using HexWork.Gameplay.GameObject.Characters;
using HexWork.Gameplay.Interfaces;
using HexWork.UI.Interfaces;

namespace HexWork.Gameplay.Actions
{
    public class PotentialGainAction : HexAction
    {
        public PotentialGainAction(string name,
            TargetType targetType,
            StatusEffect statusEffect = null,
            DamageComboAction combo = null, 
            TargetPattern targetPattern = null) :
        base(name,
            targetType,
            statusEffect,
            combo, targetPattern)
        { }

        public override bool IsAvailable(Character character, BoardState gameState)
        {
            return true;
        }

        /// <summary>
        /// immediately gains one potential and ends turn.
        /// </summary>
        public override async Task<BoardState> TriggerAsync(BoardState state, Guid characterId, IInputProvider input, IRulesProvider gameState)
        {
            var newState = state.Copy();
            var character = newState.GetEntityById(characterId) as Character;
            if (character == null)
                return state;

            if (character.CanAttack && character.IsHero)
                newState = gameState.GainPotential(newState);
            
            return gameState.NextTurn(newState, characterId);
        }
    }
}
