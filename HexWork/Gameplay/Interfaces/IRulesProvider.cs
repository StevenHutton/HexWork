using System.Collections.Generic;
using HexWork.Gameplay.Actions;
using HexWork.Gameplay.GameObject;
using HexWork.Gameplay.GameObject.Characters;

namespace HexWork.Gameplay.Interfaces
{
	public enum MovementType
	{
		NormalMove,
		MoveThroughHeroes,
		MoveThroughMonsters,
		MoveThroughUnits,
		Etheral,
		Flying
	}

    public enum TargetType
    {
        Free,
        FreeIgnoreUnits,
        FreeIgnoreLos,
        AxisAligned,
        AxisAlignedIgnoreUnits,
        AxisAlignedIgnoreLos,
        Move,
        FixedMove
    }

    public enum MovementSpeed
    {
		Slow,
		Normal,
		Fast
    }

	public interface IRulesProvider
    {
		BoardState BoardState { get; }

        #region Transforms

        BoardState MoveEntity(BoardState state, HexGameObject entity, List<HexCoordinate> path);

		BoardState TeleportEntityTo(BoardState state, HexGameObject entity, HexCoordinate position);

        int ApplyDamage(BoardState state, HexGameObject entity, int power, string message = null);

        void ApplyStatus(BoardState state, HexGameObject entity, StatusEffect effect);

        int ApplyCombo(BoardState state, HexGameObject entity, DamageComboAction combo);

        void ApplyPush(BoardState state, HexGameObject entity, HexCoordinate direction, int pushForce);

        void ApplyHealing(BoardState state, Character character, int healingAmount);

        void GainPotential(BoardState state, int potentialGain = 1);

        void LosePotential(BoardState state, int potentialCost);

		void NextTurn(BoardState state, Character activeCharacter);

        void CheckDied(BoardState state, HexGameObject entity);

        void ResolveTileEffect(BoardState state, TileEffect tileEffect, HexGameObject entity = null);

        BoardState CreateTileEffect(BoardState state, TileEffect effect, HexCoordinate location);

        void RemoveTileEffect(BoardState state, TileEffect effect);

        BoardState AddEntity(BoardState state, HexGameObject entity);

        void CompleteAction(Character ch, HexAction action);

        bool IsValidTarget(BoardState state, Character objectCharacter, HexCoordinate targetPosition, int range, TargetType targetType);

        #endregion
    }
}