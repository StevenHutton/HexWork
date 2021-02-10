using System;
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

        public TargetType TargetType = TargetType.Free;

        protected bool CanRotateTargetting = true;

        public TargetPattern Pattern;
        public int PotentialCost = 1;

        public Element Element = Element.None;
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
            return !gameState.ActiveCharacterHasAttacked && gameState.Potential >= PotentialCost;
        }
        
        #endregion

        #region Methods

        public HexAction()
        { }
        
        public HexAction(string name, 
            TargetType targetType,
            HexAction combo = null, TargetPattern targetPattern = null)
        {
            Combo = combo;
            Name = name;
            Pattern = targetPattern ?? new TargetPattern(new HexCoordinate(0,0));
            TargetType = targetType;
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

        public virtual async Task<BoardState> TriggerAsync(BoardState state, Guid characterId, IInputProvider input, IRulesProvider gameState)
        {
            var newState = state.Copy();
            var character = newState.GetEntityById(characterId) as Character;
            if (character == null)
                return state;

            //get user input
            var targetPosition = await input.GetTargetAsync(this);
	        if (targetPosition == null)
		        return state;

            //check validity
            var validTargets = BoardState.GetValidTargets(newState, character, this, TargetType);
            if (!validTargets.ContainsKey(targetPosition))
                return state;

            var potentialCost = validTargets[targetPosition];
            if (newState.Potential < potentialCost)
                return state;

            newState = gameState.LosePotential(newState, potentialCost);

			//loop through the affected tiles.
            var targetTiles = GetTargetTiles(targetPosition);
	        foreach (var targetTile in targetTiles)
            {
                var direction = PushFromCaster ?
                    BoardState.GetPushDirection(character.Position, targetTile) :
                    BoardState.GetPushDirection(targetPosition, targetTile);

                newState = await ApplyToTile(newState, targetTile, gameState, characterId, direction);
            }

            return gameState.CompleteAction(newState, character.Id, this);
        }

        public virtual async Task<BoardState> ApplyToTile(BoardState state, HexCoordinate targetTile, IRulesProvider gameState, Guid characterId, HexCoordinate direction = null)
        {
            var newState = state.Copy();
            var character = newState.GetEntityById(characterId) as Character;
            if (character == null)
                return state;

            var targetEntity = BoardState.GetEntityAtCoordinate(newState, targetTile);

            //if there's an enemy
            if (targetEntity != null)
            {
                if (direction != null)
                    newState = gameState.ApplyPush(newState, targetEntity.Id, direction, PushForce);

                if (Combo != null)
                    newState = await Combo.TriggerAsync(newState, characterId, new DummyInputProvider(targetTile), gameState);

                //only apply damage and status effects to legal targets
                if (!AllySafe || targetEntity.IsHero != character.IsHero)
                {
                    newState = gameState.ApplyDamage(newState, targetEntity.Id, Power * character.Power);
                    newState = gameState.ApplyStatus(newState, targetEntity.Id, Element);
                }                
            }
            else //if no one is there
            {
                newState = gameState.CreateTileEffect(newState, Element, targetTile);
            }
            return newState;
        }
        
        #endregion
    }
}