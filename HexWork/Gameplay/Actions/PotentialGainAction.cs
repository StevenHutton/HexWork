using System.Threading.Tasks;
using HexWork.Gameplay.GameObject.Characters;
using HexWork.Gameplay.Interfaces;
using HexWork.UI.Interfaces;
using MonoGameTestProject.Gameplay;

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

        /// <summary>
        /// immediately gains one potential and ends turn.
        /// </summary>
        public override async Task TriggerAsync(Character character, IInputProvider input, IGameStateObject gameState)
        {
            gameState.GainPotential();
            gameState.NextTurn(character);
        }
    }
}
