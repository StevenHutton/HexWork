using System;
using System.Collections.Generic;
using HexWork.Gameplay.Actions;
using HexWork.Gameplay.GameObject;

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

    public enum Element
    {
        Fire,
        Ice,
        Lightning,
        Earth,
        Wind,
        None
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
        FixedMove,
        AxisAlignedFixedMove
    }

    public enum MovementSpeed
    {
		Slow,
		Normal,
		Fast
    }

	public interface IRulesProvider
    {
        BoardState MoveEntity(BoardState state, Guid entityId, List<HexCoordinate> path);

		BoardState TeleportEntityTo(BoardState state, Guid entityId, HexCoordinate position);

        BoardState ApplyDamage(BoardState state, Guid entityId, int power);

        BoardState ApplyStatus(BoardState state, Guid entityId, Element effectType);

        BoardState ApplyCombo(BoardState state, Guid entityId, DamageComboAction combo, out int comboPower);

        BoardState ApplyPush(BoardState state, Guid entityId, HexCoordinate direction, int pushForce);

        BoardState ApplyHealing(BoardState state, Guid entityId, int healingAmount);

        BoardState GainPotential(BoardState state, int potentialGain = 1);

        BoardState LosePotential(BoardState state, int potentialCost);

        BoardState NextTurn(BoardState state, Guid entityId);

        BoardState CheckDied(BoardState state, Guid entityId);

        BoardState ResolveTileEffect(BoardState state, HexCoordinate location);

        BoardState ResolveTerrainEffect(BoardState state, HexCoordinate location);

        BoardState CreateTileEffect(BoardState state, Element effectType, HexCoordinate location);
        
        BoardState RemoveTileEffect(BoardState state, Guid entityId);

        BoardState AddEntity(BoardState state, HexGameObject entity);

        BoardState CompleteAction(BoardState state, Guid characterId, HexAction action);
    }
}