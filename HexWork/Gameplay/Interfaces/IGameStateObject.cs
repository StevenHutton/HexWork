using System.Collections.Generic;
using HexWork.Gameplay.Actions;
using HexWork.Gameplay.GameObject;
using HexWork.Gameplay.GameObject.Characters;

namespace HexWork.Gameplay.Interfaces
{
	public delegate List<HexCoordinate> GetValidTargetsDelegate(Character objectCharacter, int range, IGameStateObject gameState);

	public enum MovementType
	{
		NormalMove,
		MoveThroughHeroes,
		MoveThroughMonsters,
		MoveThroughUnits,
		Etheral,
		Flying
	}

    public enum MovementSpeed
    {
		Slow,
		Normal,
		Fast
    }

	public interface IGameStateObject
    {
		BoardState CurrentGameState { get; }

		IEnumerable<TileEffect> TileEffects { get; }

		void NotifyAction(HexAction action, HexGameObject entity);

        void MoveEntityTo(HexGameObject entity, HexCoordinate targetPosition);

        void TeleportEntityTo(HexGameObject entity, HexCoordinate position);

        HexGameObject GetEntityAtCoordinate(HexCoordinate coordinate);

        int ApplyDamage(HexGameObject entity, int power, string message = null);

        void ApplyStatus(HexGameObject entity, StatusEffect effect);

        int ApplyCombo(HexGameObject entity, DamageComboAction combo);

        void ApplyPush(HexGameObject entity, HexCoordinate direction, int pushForce);

        void ApplyHealing(Character character, int healingAmount);

        void CreateTileEffect(HexCoordinate position, TileEffect tileEffect);

        void GainPotential(int potentialGain = 1);

        void LosePotential(int potentialCost);

        void NextTurn();

        void CheckDied(HexGameObject entity);

        void ResolveTileEffect(TileEffect tileEffect, HexGameObject entity = null);

        void RemoveTileEffect(TileEffect effect);

		/// <summary>
		/// Get all the visible tiles within range of a target position along each of our three coordinate system axes.
		/// </summary>
		List<HexCoordinate> GetVisibleAxisTilesInRange(Character objectCharacter, int range);

	    List<HexCoordinate> GetVisibleAxisTilesInRangeIgnoreUnits(Character objectCharacter, int range);

	    /// <summary>
	    /// Get all the visible tiles within range of a target position
	    /// </summary>
	    List<HexCoordinate> GetVisibleTilesInRange(Character objectCharacter, int range);

	    /// <summary>
	    /// Get all the visible tiles within range of a target position
	    /// </summary>
	    List<HexCoordinate> GetVisibleTilesInRangeIgnoreUnits(Character objectCharacter, int range);

	    /// <summary>
	    /// Get all the tiles within range of a target position
	    /// </summary>
	    List<HexCoordinate> GetTilesInRange(Character objectCharacter, int range);

	    Dictionary<HexCoordinate, int> GetValidDestinations(Character objectCharacter);

	    /// <summary>
	    /// Returns a boolean indicating if the selected tile is reachable from the start position in
	    /// a number of steps =< range.
	    /// </summary>
	    bool IsValidDestination(Character objectCharacter, HexCoordinate targetPosition);

	    bool IsValidTarget(Character objectCharacter, HexCoordinate targetPosition, int range, GetValidTargetsDelegate targetDelegate);

	    /// <summary>
	    /// Get the shortest traversable path between two points on the map.
	    /// If no path can be found returns null
	    /// 
	    /// A* is fuckin' rad.
	    /// 
	    /// Rad as shit.
	    /// </summary>
	    List<HexCoordinate> FindShortestPath(HexCoordinate start, HexCoordinate destination, MovementType movementType = MovementType.NormalMove);
        
	    List<HexCoordinate> GetAxisTilesInRange(Character objectCharacter, int range);
        
        bool IsHexPassable(HexCoordinate coordinate);

        bool IsTileEmpty(HexCoordinate coordinate);

	    Tile GetTileAtCoordinate(HexCoordinate coordinate);
        
        bool IsHexInMap(HexCoordinate destinationPos);
        TileEffect GetTileEffectAtCoordinate(HexCoordinate targetPosition);

        int GetPathLengthToTile(Character character, HexCoordinate destination);

        List<HexCoordinate> GetWalkableAdjacentTiles(HexCoordinate position, MovementType movementType);

        void SpawnCharacter(Character character);

    }
}