using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
            ComboAction combo = null, TargetPattern targetPattern = null) :
            base(name,
                targetDelegate,
                statusEffect,
                combo, targetPattern)
        { }

        /// <summary>
        /// immediate gains one potential and ends turn.
        /// </summary>
        public override async Task TriggerAsync(Character character, IInputProvider input, IGameStateObject gameState)
        {
            gameState.GainPotential();

            gameState.NextTurn();
        }

        public override bool IsAvailable(Character character)
        {
            var gameState = GameStateManager.CurrentGameState;

            return character.IsActive && character.CanMove && character.CanAttack;
        }
    }
}
