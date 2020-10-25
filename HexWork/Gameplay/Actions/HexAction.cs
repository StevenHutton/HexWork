using System.Collections.Generic;
using System.Threading.Tasks;
using HexWork.Gameplay.GameObject;
using HexWork.Gameplay.GameObject.Characters;
using HexWork.Gameplay.Interfaces;
using HexWork.UI;
using HexWork.UI.Interfaces;

namespace HexWork.Gameplay.Actions
{
    public class HexAction
    {
        #region Attributes

        protected readonly GetValidTargetsDelegate _getValidTargets;

        protected bool CanRotateTargetting = true;

        public TargetPattern Pattern;
        public int PotentialCost = 1;

        public StatusEffect StatusEffect = null;
        public TileEffect TileEffect = null;
        public HexAction FollowUpAction = null;
        public HexAction Combo = null;

        public int PushForce = 0;
        public bool PushFromCaster = true;

        #endregion

        #region Properties

        public string Name { get; set; }

        public int Range { get; set; } = 2;
        
        public bool IsDetonator => Combo != null;
        
        public int Power = 1;

        public bool AllySafe = false;

        public virtual bool IsAvailable(Character character, BoardState gameState)
        {
            return character.CanAttack && gameState.Potential >= PotentialCost;
        }
        
        #endregion

        #region Methods

        public HexAction()
        { }
        
        public HexAction(string name, 
            GetValidTargetsDelegate targetDelegate,
            StatusEffect statusEffect = null,
            HexAction combo = null, TargetPattern targetPattern = null)
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

        public virtual async Task TriggerAsync(BoardState state, Character character, IInputProvider input, IRulesProvider gameState)
        {
			//get user input
	        var targetPosition = await input.GetTargetAsync(this);
	        if (targetPosition == null)
		        return;
			//check validity
	        if (!IsValidTarget(state, character, targetPosition))
		        return;

            if (PotentialCost != 0)
                gameState.LosePotential(state, PotentialCost);

			//loop through the affected tiles.
            var targetTiles = GetTargetTiles(targetPosition);
	        foreach (var targetTile in targetTiles)
            {
                var direction = PushFromCaster ?
                    BoardState.GetPushDirection(character.Position, targetTile) :
                    BoardState.GetPushDirection(targetPosition, targetTile);

                ApplyToTile(state, targetTile, gameState, character, direction);
            }
        }

        public virtual async void ApplyToTile(BoardState state, HexCoordinate targetTile, IRulesProvider gameState, Character character, HexCoordinate direction = null)
        {
            var targetCharacter = BoardState.GetEntityAtCoordinate(state, targetTile);
                                    
            if (Combo != null)
                await Combo.TriggerAsync(state, character, new DummyInputProvider(targetTile), gameState);

            //if no one is there, next tile
            if (targetCharacter != null)
            {
                //only apply damage and status effects to legal targets
                if (!AllySafe || targetCharacter.IsHero != character.IsHero)
                { 
                    gameState.ApplyDamage(state, targetCharacter, Power * character.Power);
                    gameState.ApplyStatus(state, targetCharacter, StatusEffect);
                }

                //everyone gets pushed
                if (direction != null)
                    gameState.ApplyPush(state, targetCharacter, direction, PushForce);
            }

            if (TileEffect != null)
                gameState.CreateTileEffect(state, TileEffect, targetTile);
        }

        /// <summary>
        /// Get a list of coordinates that are valid target locations for this action for the passed in character
        /// </summary>
        public virtual List<HexCoordinate> GetValidTargets(BoardState state, Character character)
        {
            return _getValidTargets?.Invoke(state, character.Position, character.RangeModifier + this.Range);
        }

        public virtual bool IsValidTarget(BoardState state, Character character, HexCoordinate targetCoordinate)
        {
            return _getValidTargets != null && _getValidTargets.Invoke(state, character.Position, character.RangeModifier + this.Range).Contains(targetCoordinate);
        }
        
        #endregion
    }
}