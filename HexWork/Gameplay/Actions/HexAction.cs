using System.Collections.Generic;
using System.Threading.Tasks;
using HexWork.Gameplay.Interfaces;
using HexWork.UI;
using HexWork.UI.Interfaces;
using MonoGameTestProject.Gameplay;

namespace HexWork.Gameplay.Actions
{
    public class HexAction
    {
        #region Attributes

        protected readonly GetValidTargetsDelegate _getValidTargets;

        protected bool CanRotateTargetting = true;

        #endregion

        #region Properties

        public string Name { get; set; }

        public int Range { get; set; } = 2;

        public StatusEffect StatusEffect { get; }

        public bool IsDetonator => Combo != null;

        public ComboAction Combo { get; }

        public TargetPattern Pattern { get; set; }

        public int PotentialCost { get; set; } = 0;

        public bool IsExtender => PotentialCost > 0;

        public int Power { get; set; } = 15;

        public bool AllySafe { get; set; } = false;
        
        #endregion

        #region Methods

        public HexAction()
        { }

        public HexAction(string name, 
            GetValidTargetsDelegate targetDelegate,
            StatusEffect statusEffect = null, 
            ComboAction combo = null, TargetPattern targetPattern = null)
        {
            StatusEffect = statusEffect;
            Combo = combo;
            Name = name;
            Pattern = targetPattern ?? new TargetPattern(new HexCoordinate(0,0));
            _getValidTargets = targetDelegate;
        }

        /// <summary>
        /// Get a list of tiles that will be affected by this action if it targets the passed in position.
        /// </summary>
        public List<HexCoordinate> GetTargetTiles(HexCoordinate position)
        {
            return Pattern.GetPattern(position);
        }

        public void RotateTargeting(bool isAntiClockwise)
        {
            if (!CanRotateTargetting)
                return;

            if (isAntiClockwise)
                Pattern.RotateAntiClockwise();
            else
                Pattern.RotateClockwise();
        }

        public virtual async Task TriggerAsync(Character character, IInputProvider input, IGameStateObject gameState)
        {
			//get user input
	        var targetPosition = await input.GetTargetAsync(this);
	        if (targetPosition == null)
		        return;
			//check validity
	        if (!_getValidTargets.Invoke(character, character.RangeModifier + this.Range, gameState)
		        .Contains(targetPosition))
		        return;

            if (PotentialCost != 0)
                gameState.LosePotential(PotentialCost);

            gameState.NotifyAction(this, character);

			//loop through the affected tiles.
            var targetTiles = GetTargetTiles(targetPosition);
	        foreach (var targetTile in targetTiles)
	        {
		        var targetCharacter = gameState.GetCharacterAtCoordinate(targetTile);

		        //if no one is there, next tile
		        if (targetCharacter == null)
			        continue;

                if (AllySafe && targetCharacter.IsHero == character.IsHero)
                    continue;

                if (Combo != null)
                    await Combo.TriggerAsync(character, new DummyInputProvider(targetTile), gameState);

		        gameState.ApplyDamage(targetCharacter, Power);
		        gameState.ApplyStatus(targetCharacter, StatusEffect);
                gameState.CheckDied(targetCharacter);
            }
        }

        public virtual bool IsAvailable(Character character)
		{
			var gameState = GameStateManager.CurrentGameState;

			return character.IsActive && (this.PotentialCost <= gameState.Potential) && character.CanAttack;
		}

        /// <summary>
        /// Get a list of coordinates that are valid target locations for this action for the passed in character
        /// </summary>
        public virtual List<HexCoordinate> GetValidTargets(Character character, IGameStateObject gameState)
        {
            return _getValidTargets?.Invoke(character, character.RangeModifier + this.Range, gameState);
        }

        public virtual bool IsValidTarget(Character character, HexCoordinate targetCoordinate, IGameStateObject gameState)
        {
            return _getValidTargets != null && _getValidTargets.Invoke(character, character.RangeModifier + this.Range, gameState).Contains(targetCoordinate);
        }
        
        #endregion
    }
}
