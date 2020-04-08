using System.Threading.Tasks;
using HexWork.Gameplay.Interfaces;
using HexWork.UI;
using HexWork.UI.Interfaces;
using MonoGameTestProject.Gameplay;

namespace HexWork.Gameplay.Actions
{
    public class PushAction : HexAction
    {
        public int PushForce { get; set; } = 1;

        public PushAction(string name,
            GetValidTargetsDelegate targetDelegate,
            StatusEffect statusEffect = null,
            DamageComboAction combo = null, TargetPattern targetPattern = null) : 
            base(name,
            targetDelegate,
            statusEffect,
            combo, targetPattern)
        {

        }

	    public override async Task TriggerAsync(Character character, IInputProvider input, IGameStateObject gameState)
	    {
			//get target input and check validity.
		    var targetPosition = await input.GetTargetAsync(this);
			if (targetPosition == null)
			    return;
		    if (!gameState.IsValidTarget(character, targetPosition, character.RangeModifier + this.Range, _getValidTargets))
				return;

			//Get target character and check validity
		    var targetCharacter = gameState.GetCharacterAtCoordinate(targetPosition);
		    if (targetCharacter == null)
			    return;
			
		    gameState.NotifyAction(this, character);

			//determine direction of push
		    var nearestNeighbor = gameState.GetNearestNeighbor(character.Position, targetCharacter.Position);
		    var direction = targetCharacter.Position - nearestNeighbor;
            
            if(Combo != null)
                await Combo.TriggerAsync(character, new DummyInputProvider(targetCharacter.Position), gameState);
			gameState.ApplyDamage(targetCharacter, Power * character.Power);
		    gameState.ApplyStatus(targetCharacter, StatusEffect);
		    gameState.ApplyPush(targetCharacter, direction, PushForce);

		    if (PotentialCost != 0)
			    gameState.LosePotential(PotentialCost);
		}
	}
}