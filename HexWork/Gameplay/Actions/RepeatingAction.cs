using System.Collections.Generic;
using System.Threading.Tasks;
using HexWork.Gameplay.GameObject.Characters;
using HexWork.Gameplay.Interfaces;
using HexWork.UI;
using HexWork.UI.Interfaces;

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
            TargetType targetType,
            StatusEffect statusEffect = null,
            DamageComboAction combo = null, TargetPattern targetPattern = null) :
            base(name,
                targetType,
                statusEffect,
                combo, targetPattern)
        { }
        
        public override async Task TriggerAsync(BoardState state, Character character, IInputProvider input, IRulesProvider gameState)
        {
            var targetPosition = await input.GetTargetAsync(this);

            if (targetPosition == null)
                return;

            //check validity
            if (!gameState.IsValidTarget(state, character, targetPosition, character.RangeModifier + Range, TargetType))
                return;

            var targetTiles = GetTargetTiles(targetPosition);

            for (int i = NumberOfAttacks - 1; i >= 0; i--)
            {
                foreach (var targetTile in targetTiles)
                {
                    var targetCharacter = BoardState.GetEntityAtCoordinate(state, targetTile);

                    //if no one is there, next tile
                    if (targetCharacter == null)
                        continue;

                    if (AllySafe && targetCharacter.IsHero == character.IsHero)
                        continue;

                    if (Combo != null)
                        await Combo.TriggerAsync(state, character, new DummyInputProvider(targetPosition), gameState);
                    gameState.ApplyDamage(state, targetCharacter, Power * character.Power);
                    gameState.ApplyStatus(state, targetCharacter, StatusEffect);
                }
            }

            if (PotentialCost != 0)
                gameState.LosePotential(state, PotentialCost);
            
            gameState.CompleteAction(character, this);
        }

        #endregion
    }
}