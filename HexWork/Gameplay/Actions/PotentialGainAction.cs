using System.Threading.Tasks;
using HexWork.Gameplay.GameObject.Characters;
using HexWork.Gameplay.Interfaces;
using HexWork.UI.Interfaces;

namespace HexWork.Gameplay.Actions
{
    public class PotentialGainAction : HexAction
    {
        public PotentialGainAction(string name,
            GetValidTargetsDelegate targetDelegate,
            StatusEffect statusEffect = null,
            DamageComboAction combo = null, 
            TargetPattern targetPattern = null) :
        base(name,
            targetDelegate,
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
        public override async Task TriggerAsync(BoardState state, Character character, IInputProvider input, IRulesProvider gameState)
        {
            if(character.CanAttack && character.IsHero)
                gameState.GainPotential(state);
            
            gameState.NextTurn(state, character);
        }
    }
}
