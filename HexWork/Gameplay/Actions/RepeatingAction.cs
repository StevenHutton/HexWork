using System.Collections.Generic;
using System.Threading.Tasks;
using HexWork.Gameplay.Interfaces;
using HexWork.UI;
using HexWork.UI.Interfaces;
using MonoGameTestProject.Gameplay;

namespace HexWork.Gameplay.Actions
{
    public class RepeatingAction : HexAction
    {
        #region Attributes
        
        #endregion

        #region Properties

        public int NumberOfAttacks { get; set; } = 3;

        #endregion

        #region Methods

        public RepeatingAction()
        { }

        public RepeatingAction(string name,
            GetValidTargetsDelegate targetDelegate,
            StatusEffect statusEffect = null,
            ComboAction combo = null, TargetPattern targetPattern = null) :
            base(name,
                targetDelegate,
                statusEffect,
                combo, targetPattern)
        { }
        
        public override async Task TriggerAsync(Character character, IInputProvider input, IGameStateObject gameState)
        {
            var targetPosition = await input.GetTargetAsync(this);

            if (targetPosition == null)
                return;

            if (!gameState.IsValidTarget(character,
                targetPosition,
                character.RangeModifier + this.Range,
                _getValidTargets))
            {
                return;
            }
            var targetTiles = GetTargetTiles(targetPosition);

            gameState.NotifyAction(this, character);

            for (int i = NumberOfAttacks - 1; i >= 0; i--)
            {
                foreach (var targetTile in targetTiles)
                {
                    var targetCharacter = gameState.GetCharacterAtCoordinate(targetTile);

                    //if no one is there, next tile
                    if (targetCharacter == null)
                        continue;

                    if (AllySafe && targetCharacter.IsHero == character.IsHero)
                        continue;

                    gameState.ApplyDamage(targetCharacter, Power);
                    gameState.ApplyStatus(targetCharacter, StatusEffect);

                    if (Combo != null)
                        await Combo.TriggerAsync(character, new DummyInputProvider(targetPosition), gameState);

                    gameState.CheckDied(targetCharacter);
                }
            }

            if (PotentialCost != 0)
                gameState.LosePotential(PotentialCost);
        }

        #endregion
    }
}