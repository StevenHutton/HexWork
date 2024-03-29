﻿using System;
using System.Collections.Generic;
using System.Linq;
using HexWork.Gameplay.GameObject;
using HexWork.Gameplay.GameObject.Characters;
using HexWork.Gameplay.Interfaces;
using Microsoft.Xna.Framework;

namespace HexWork.Gameplay
{
    public class BoardState : Dictionary<HexCoordinate, Tile>
    {
        #region Attributes

        public Character ActiveCharacter;

        public Guid ActiveCharacterId;

        public bool ActiveCharacterHasAttacked;
        public bool ActiveCharacterHasMoved;

        //list of all characters in the current match ordered by initiative count
        public List<HexGameObject> Entities = new List<HexGameObject>();
        public int MaxPotential = 9;
        public int Potential = 3;

        public const int MapWidth = 12;
        public const int MapHeight = 10;

        public static readonly HexCoordinate[] Directions = {
            new HexCoordinate(+1, -1, 0),
            new HexCoordinate(+1, 0, -1),
            new HexCoordinate(0, +1, -1),
            new HexCoordinate(-1, +1, 0),
            new HexCoordinate(-1, 0, +1),
            new HexCoordinate(0, -1, +1)
        };

        //convenient to have
        private static readonly float Sqrt3 = (float)Math.Sqrt(3.0);

        private static readonly Random Random = new Random();

        #endregion

        #region Properties

        public IEnumerable<Character> Characters => Entities.OfType<Character>();
        public IEnumerable<TileEffect> TileEffects => Entities.OfType<TileEffect>();
        public IEnumerable<Character> Heroes => Characters.Where(character => character.IsHero);
        public IEnumerable<Character> Enemies => Characters.Where(character => !character.IsHero);

        #endregion

        public BoardState() { }

        public void GenerateMap()
        {
            for (var columnIndex = -MapWidth; columnIndex <= MapWidth; columnIndex++)
            {
                for (var rowIndex = -MapHeight; rowIndex <= MapHeight; rowIndex++)
                {
                    var x = columnIndex - (rowIndex - (rowIndex & 1)) / 2;
                    var z = rowIndex;
                    var y = -(x + z);

                    if (x + y + z != 0)
                        throw new Exception(" Impossible co-ordinate");

                    var coord = new HexCoordinate(x, y);

                    this.Add(coord, new Tile());
                }
            }

            RandomizeTerrain();

            foreach (var tile in Values)
            {
                switch (tile.TerrainType)
                {
                    case TerrainType.Water:
                        tile.Color = Color.CadetBlue;
                        tile.IsWalkable = true;
                        tile.MovementCost = 0;
                        break;
                    case TerrainType.Ground:
                        tile.Color = Color.SaddleBrown;
                        tile.IsWalkable = true;
                        tile.MovementCost = 0;
                        break;
                    case TerrainType.Lava:
                        tile.Color = Color.Orange;
                        tile.IsWalkable = true;
                        tile.MovementCost = 1.1f;
                        break;
                    case TerrainType.Ice:
                        tile.Color = Color.LightBlue;
                        tile.IsWalkable = true;
                        tile.MovementCost = 1;
                        break;
                    case TerrainType.ThinIce:
                        tile.Color = Color.LightSteelBlue;
                        tile.IsWalkable = true;
                        tile.MovementCost = 1;
                        break;
                    case TerrainType.Snow:
                        tile.Color = Color.DarkGray;
                        tile.IsWalkable = true;
                        tile.MovementCost = 2;
                        break;
                    case TerrainType.Sand:
                        tile.Color = Color.SandyBrown;
                        tile.IsWalkable = true;
                        tile.MovementCost = 1;
                        break;
                    case TerrainType.Pit:
                        tile.Color = Color.DarkSlateGray;
                        tile.IsWalkable = false;
                        break;
                    case TerrainType.Wall:
                        tile.Color = Color.Black;
                        tile.IsWalkable = false;
                        tile.BlocksLOS = true;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        private void RandomizeTerrain()
        {
            Random rand = new Random(DateTime.Now.Millisecond);

            //loop through the map once and seed with terrain
            foreach (var tile in Values)
            {
                if (rand.Next(10000) <= 1500 && tile.TerrainType == TerrainType.Ground) //make X% of tiles into a random non-ground terrain type
                {
                    tile.TerrainType = (TerrainType)(rand.Next(8) + 1);
                }
            }

            //go through again and check the neighbors for terrain
            foreach (var entry in this)
            {
                var neighbors = BoardState.GetNeighbours(entry.Key);

                foreach (var neighbor in neighbors)
                {
                    //if this tile is ground and it's neighbor isn't.
                    if (this[neighbor].TerrainType == TerrainType.Ground ||
                        entry.Value.TerrainType != TerrainType.Ground) continue;

                    //X% chance that we copy the neighbor's terrain type.
                    if (rand.Next(99) < 10)
                    {
                        entry.Value.TerrainType = this[neighbor].TerrainType;
                    }
                }
            }

            //go through again and check the neighbors for terrain
            foreach (var entry in this)
            {
                var neighbors = GetNeighbours(entry.Key);

                foreach (var neighbor in neighbors)
                {
                    //if this tile is ground and it's neighbor isn't.
                    if (this[neighbor].TerrainType == TerrainType.Ground ||
                        entry.Value.TerrainType != TerrainType.Ground) continue;

                    //X% chance that we copy the neighbor's terrain type.
                    if (rand.Next(99) < 10)
                    {
                        entry.Value.TerrainType = this[neighbor].TerrainType;
                    }
                }
            }
        }

        public BoardState Copy()
        {
            var bs = new BoardState();
            foreach(var kvp in this)
            {
                bs[kvp.Key] = kvp.Value;
            }
            bs.Potential = Potential;
            foreach(var ent in this.Entities)
            {
                bs.Entities.Add(ent.Copy());
            }
            if(this.ActiveCharacter != null)
                bs.ActiveCharacter = bs.GetCharacterById(this.ActiveCharacter.Id);
            
            bs.Potential = this.Potential;

            bs.ActiveCharacterHasAttacked = this.ActiveCharacterHasAttacked;
            bs.ActiveCharacterHasMoved = this.ActiveCharacterHasMoved;
            bs.ActiveCharacterId = this.ActiveCharacterId;

            return bs;
        }

        /// <summary>
        /// Returns null if no entity can be found.
        /// </summary>
        /// <returns>HexGameObject or null</returns>
        public HexGameObject GetEntityById(Guid id)
        {
            return Entities.FirstOrDefault(ent => ent.Id == id);
        }

        public Character GetCharacterById(Guid id)
        {
            return Characters.FirstOrDefault(ent => ent.Id == id);
        }

        #region Static Query Functions

        public static HexGameObject GetEntityAtCoordinate(BoardState state, HexCoordinate coordinate)
        {
            return state.Entities.FirstOrDefault(go => go.Position == coordinate);
        }

        /// <summary>
        /// returns null if no effect found.
        /// </summary>
        public static TileEffect GetTileEffectAtCoordinate(BoardState state, HexCoordinate coordinate)
        {
            return state.TileEffects.FirstOrDefault(data => data.Position == coordinate);
        }

        /// <summary>
        /// Checks to see if a specific tile is walkable and unoccupied.
        /// </summary>
        /// <returns>true/false</returns>
        public static bool IsWalkableAndEmpty(BoardState state, HexCoordinate coordinate)
        {
            return IsHexWalkable(state, coordinate)
                   && IsTileEmpty(state, coordinate);
        }

        public static bool IsHexWalkable(BoardState state, HexCoordinate co)
        {
            return state[co].IsWalkable;
        }

        public static bool IsHexOpaque(BoardState state, HexCoordinate co)
        {
            return state[co].BlocksLOS;
        }

        public static bool IsTileEmpty(BoardState state, HexCoordinate position)
        {
            return !state.Entities.Any(ent => ent.Position == position && ent.BlocksMovement);
        }

        public static Character GetCharacterAtInitiative(BoardState state, int initiative)
        {
            var initiativeList = GetInitiativeList(state);
            if (initiativeList.Count < initiative + 1)
                return null;

            return initiativeList[initiative];
        }

        public static List<Character> GetInitiativeList(BoardState state)
        {
            var initiativeList = new List<Character>();
            var turnTimerToBeat = int.MaxValue;
            var characters = state.Characters.OrderBy(cha => cha.TurnTimer).ToList();
            Character characterToBeat = state.Characters?.First();

            var iterator = characters.GetEnumerator();
            var endOfList = !iterator.MoveNext();

            while (!endOfList)
            {
                var character = iterator.Current;
                //if character is going to go before the time to beat, add them to the list.
                if (character.TurnTimer < turnTimerToBeat)
                {
                    initiativeList.Add(character);

                    //if they're going to go again before the time to beat, they're the new time to beat.
                    if (character.TurnTimer + character.TurnCooldown < turnTimerToBeat)
                    {
                        characterToBeat = character;
                        turnTimerToBeat = character.TurnTimer + character.TurnCooldown;
                    }

                    endOfList = !iterator.MoveNext();
                }
                else
                {
                    //if the character is slower than the time to beat the character to beat will go again and the time to beat will increase
                    initiativeList.Add(characterToBeat);
                    turnTimerToBeat = turnTimerToBeat + characterToBeat.TurnCooldown;
                }
            }
            iterator.Dispose();
            return initiativeList;
        }

        /// <summary>
        /// Get all the tiles within range of a target position along each of our three coordinate system axes.
        /// </summary>
        public static Dictionary<HexCoordinate, int> GetAxisTilesInRange(BoardState state, HexCoordinate position, int range, int basePotentialCost)
        {
            var targets = new Dictionary<HexCoordinate, int>();

            foreach (var direction in Directions)
            {
                for (var i = 0; i < range; i++)
                {
                    var hexToCheck = position + (direction * (i + 1));

                    if (!state.ContainsKey(hexToCheck))
                        break;

                    var cost = RulesProvider.GetRangeCost(i) + basePotentialCost;
                    if (cost <= state.Potential)
                        targets.Add(hexToCheck, cost);
                }
            }

            return targets;
        }

        public static HexCoordinate GetRandomDirection()
        {
            return Directions[Random.Next(6)];
        }

        /// <summary>
        /// Get all the visible tiles within range of a target position along each of our three coordinate system axes.
        /// </summary>
        public static Dictionary<HexCoordinate, int> GetVisibleAxisTilesInRange(BoardState state, HexCoordinate position, int range, int basePotentialCost)
        {
            var targets = new Dictionary<HexCoordinate, int>();
            foreach (var direction in Directions)
            {
                for (var i = 0; i < range; i++)
                {
                    var hexToCheck = position + (direction * (i + 1));

                    if (!state.ContainsKey(hexToCheck))
                        break;

                    if (IsHexOpaque(state, hexToCheck))
                        break;

                    var cost = RulesProvider.GetRangeCost(i) + basePotentialCost;
                    if (cost <= state.Potential)
                        targets.Add(hexToCheck, cost);

                    //if there's a unit in this tile then we can't see past them.
                    if (!IsTileEmpty(state, hexToCheck))
                        break;
                }
            }

            return targets;
        }

        public static Dictionary<HexCoordinate, int> GetVisibleAxisTilesInRangeIgnoreUnits(BoardState state, HexCoordinate position, int range, int basePotentialCost)
        {
            var targets = new Dictionary<HexCoordinate, int>();
            foreach (var direction in Directions)
            {
                for (var i = 0; i < range; i++)
                {
                    var hexToCheck = position + (direction * (i + 1));

                    if (!state.ContainsKey(hexToCheck))
                        break;

                    if (IsHexOpaque(state, hexToCheck))
                        break;

                    var cost = RulesProvider.GetRangeCost(i) + basePotentialCost;
                    if (cost <= state.Potential)
                        targets.Add(hexToCheck, cost);
                }
            }

            return targets;
        }

        /// <summary>
        /// Get all the visible tiles within range of a target position
        /// </summary>
        public static Dictionary<HexCoordinate, int> GetVisibleTilesInRange(BoardState state, HexCoordinate position, int range, int basePotentialCost = 0)
        {
            var targets = new Dictionary<HexCoordinate, int> { {position, 0} };

            GetVisibleTilesRecursive(state, targets, position, position, range, 0, false, basePotentialCost);

            return targets;
        }

        /// <summary>
        /// Get all the visible tiles within range of a target position
        /// </summary>
        public static Dictionary<HexCoordinate, int> GetVisibleTilesInRangeIgnoreUnits(BoardState state, HexCoordinate position, int range, int basePotentialCost = 0)
        {
            var targets = new Dictionary<HexCoordinate, int> { { position, 0} };

            GetVisibleTilesRecursive(state, targets, position, position, range, 0, true, basePotentialCost);

            return targets;
        }

        /// <summary>
        /// Get all the tiles within range of a target position
        /// </summary>
        public static Dictionary<HexCoordinate, int> GetTilesInRange(BoardState state, HexCoordinate position, int range, int basePotentialCost)
        {
            var result = new Dictionary<HexCoordinate, int> { { position, 0 } };

            GetTilesInRangeRecursive(state, result, position, position, range, 0, basePotentialCost);

            return result;
        }

        private static void GetTilesInRangeRecursive(BoardState state, 
            Dictionary<HexCoordinate, int> tilesInRange, 
            HexCoordinate position, 
            HexCoordinate startPosition, 
            int range, 
            int searchDepth,
            int basePotentialCost)
        {
            if (searchDepth >= range)
                return;

            var adjacentTiles = GetNeighbours(position);
            foreach (var coord in adjacentTiles)
            {
                var distanceToPoint = DistanceBetweenPoints(startPosition, position);
                var distanceToNeighbour = DistanceBetweenPoints(startPosition, coord);

                //only look at neighbouring tiles that're further away from the starting position than the tile we're currently at
                if (distanceToPoint >= distanceToNeighbour)
                    continue;

                if (!tilesInRange.ContainsKey(coord))
                {
                    var cost = RulesProvider.GetRangeCost(searchDepth) + basePotentialCost;
                    if(cost <= state.Potential)
                        tilesInRange.Add(coord, cost);
                }

                GetTilesInRangeRecursive(state, tilesInRange, coord, startPosition, range, searchDepth + 1, basePotentialCost);
            }
        }

        private static void GetVisibleTilesRecursive(BoardState state,
            Dictionary<HexCoordinate, int> results, HexCoordinate position,
            HexCoordinate startPosition, int maxSearchDepth, int searchDepth, bool ignoreUnits, int basePotentialCost)
        {
            if (searchDepth >= maxSearchDepth)
                return;

            var adjacentTiles = GetNeighbours(position);

            foreach (var coord in adjacentTiles)
            {
                //if the terrain blocks LOS then move on
                if (IsHexOpaque(state, coord)) continue;

                var distanceToPoint = DistanceBetweenPoints(startPosition, position);
                var distanceToNeighbour = DistanceBetweenPoints(startPosition, coord);

                //only look at neighbouring tiles that're further away from the starting position than the tile we're currently at
                if (distanceToPoint >= distanceToNeighbour)
                    continue;

                if (!results.ContainsKey(coord) && searchDepth + 1 <= maxSearchDepth)
                {
                    var cost = RulesProvider.GetRangeCost(searchDepth) + basePotentialCost;
                    if (cost <= state.Potential)
                        results.Add(coord, cost);
                }

                if (!IsTileEmpty(state, coord) && !ignoreUnits)
                    continue;

                GetVisibleTilesRecursive(state, results, coord, startPosition, maxSearchDepth, searchDepth + 1, ignoreUnits, basePotentialCost);
            }
        }

        /// <summary>
        /// Returns a boolean indicating if the selected tile is reachable from the start position in
        /// a number of steps =< range.
        /// </summary>
        public static bool IsValidDestination(BoardState state, Character objectCharacter, HexCoordinate targetPosition, int basePotentialCost)
        {
            var destinations = GetValidDestinations(state, objectCharacter.Position, objectCharacter, basePotentialCost);

            if (!destinations.Keys.Contains(targetPosition))
                return false;

            if (state.Potential < destinations[targetPosition])
                return false;

            return true;
        }

        public static Dictionary<HexCoordinate, int> GetValidDestinations(BoardState state, HexCoordinate position, Character oCharacter, int basePotentialCost)
        {
            MovementType mType = oCharacter.MovementType;
            MovementSpeed mSpeed = oCharacter.MovementSpeed;
            Dictionary<HexCoordinate, int> pathValues = new Dictionary<HexCoordinate, int> { { position, 0 } };

            GetWalkableNeighboursRecursive(state, pathValues, position, mType, mSpeed, 0, 0, state.Potential, basePotentialCost);
            return pathValues;
        }

        /// <summary></summary>
        /// <param name="output"></param>
        /// <param name="position"></param>
        /// <param name="movementType"></param>
        /// <param name="movementCost"></param>
        /// <param name="searchDepth"></param>
        private static void GetWalkableNeighboursRecursive(BoardState state,
            Dictionary<HexCoordinate, int> pathLengthsToTiles, HexCoordinate position, MovementType movementType, MovementSpeed movementSpeed,
            int movementCost, int searchDepth, int availableMovement, int basePotentialCost)
        {
            var adjacentTiles = BoardState.GetNeighbours(position);
            var tilesToSearch = new List<HexCoordinate>();

            //loop through all neighbour tiles.
            foreach (var coord in adjacentTiles)
            {
                if (!IsTilePassable(state, movementType, coord)) continue;

                //get movement cost to next tile
                int movementCostModifier = (int)GetTileTotalMovementCost(state, coord);
                var movementCostToCoord = RulesProvider.GetMoveSpeedCost(movementSpeed, searchDepth) + movementCostModifier;

                var totalMovementCostToCurrent = movementCostToCoord + movementCost + basePotentialCost;

                //if we don't have enough potential to reach this tile skip it.
                if (totalMovementCostToCurrent > availableMovement) continue;

                if (!pathLengthsToTiles.ContainsKey(coord) || pathLengthsToTiles[coord] > movementCostToCoord)
                    tilesToSearch.Add(coord);

                //valid destination check
                if (!IsTileEmpty(state, coord))
                    continue;

                //if we've never looked at this tile or we found a shorter path to the tile add it to the list.
                if (!pathLengthsToTiles.ContainsKey(coord))
                {
                    pathLengthsToTiles.Add(coord, totalMovementCostToCurrent);//then add it to the list.
                }
                else if (pathLengthsToTiles[coord] > totalMovementCostToCurrent)
                {
                    pathLengthsToTiles[coord] = totalMovementCostToCurrent;//or adjust the cost
                }
            }

            foreach (var coord in tilesToSearch)
            {
                TerrainType terrainType = state[coord].TerrainType;
                if (terrainType == TerrainType.Water
                    || terrainType == TerrainType.Lava)
                    continue;

                //get movement cost to next tile
                int movementCostModifier = (int)GetTileTotalMovementCost(state, coord);
                var movementCostToCoord = RulesProvider.GetMoveSpeedCost(movementSpeed, searchDepth) + movementCostModifier;

                GetWalkableNeighboursRecursive(state, pathLengthsToTiles, coord, movementType, movementSpeed,
                    movementCost + movementCostToCoord,
                    searchDepth + 1, availableMovement, basePotentialCost);
            }
        }       

        public static List<HexCoordinate> FindShortestPath(BoardState state, HexCoordinate startPosition, HexCoordinate destination, int availableMovement,
            MovementType movementType = MovementType.NormalMove, MovementSpeed speed = MovementSpeed.Normal)
        {
            if (startPosition == destination) return null;

            //this is the map of hex coordiantes to the shortest path length to that coordinate
            Dictionary<HexCoordinate, float> pathValues = new Dictionary<HexCoordinate, float>();

            Dictionary<HexCoordinate, List<HexCoordinate>> ancestorPathmap = new Dictionary<HexCoordinate, List<HexCoordinate>>();
            List<HexCoordinate> path = null;

            //get cost to move to this neighbour tile from the current search tile
            FindShortestPathRecursive(state, ancestorPathmap, pathValues, startPosition, destination, movementType, speed, 0, 0, availableMovement);

            if (ancestorPathmap.ContainsKey(destination))
            {
                path = ancestorPathmap[destination];
                path.Add(destination);
                path.RemoveAt(0);
            }

            return path;
        }

        private static void FindShortestPathRecursive(BoardState state,
            Dictionary<HexCoordinate, List<HexCoordinate>> ancestorPathmap,
            Dictionary<HexCoordinate, float> moveCostToSearchedTiles,
            HexCoordinate currentSearchCoord,
            HexCoordinate destination,
            MovementType movementType,
            MovementSpeed speed,
            int movementCostPrevious,
            int searchDepth,
            int availableMovement)
        {
            var adjacentTiles = BoardState.GetNeighbours(currentSearchCoord);
            var tilesToSearch = new List<HexCoordinate>();

            foreach (var coord in adjacentTiles)
            {
                if (!IsTilePassable(state, movementType, coord)) continue;

                List<HexCoordinate> shortestPathToCurrentSearchTile;

                //get cost to move to this neighbour tile from the current search tile
                float movementCostToTile = GetTileMovementCost(state, coord) + RulesProvider.GetMoveSpeedCost(speed, searchDepth);

                if (ancestorPathmap.ContainsKey(currentSearchCoord))
                {
                    //a list of ancestor tiles to walk to reach the current coord
                    shortestPathToCurrentSearchTile = new List<HexCoordinate>(ancestorPathmap[currentSearchCoord]);
                    shortestPathToCurrentSearchTile.Add(currentSearchCoord);

                    //if this is the first time we've visited this tile on this path then apply the tile effects (we only apply them once)
                    if (!shortestPathToCurrentSearchTile.Contains(coord))
                        movementCostToTile += GetMovementCostModifier(state, coord);
                }
                else
                    shortestPathToCurrentSearchTile = new List<HexCoordinate> { currentSearchCoord };

                var totalMovementCostToTile = (int)(movementCostToTile + movementCostPrevious);

                if (totalMovementCostToTile > availableMovement)
                    continue;

                //if we've never been to this tile or if we found a shorter path to this tile then add it to the list.
                if (!moveCostToSearchedTiles.ContainsKey(coord))
                {
                    moveCostToSearchedTiles.Add(coord, totalMovementCostToTile);//add it to the list.

                    if (!ancestorPathmap.ContainsKey(coord))
                        ancestorPathmap.Add(coord, shortestPathToCurrentSearchTile);
                    else
                        ancestorPathmap[coord] = shortestPathToCurrentSearchTile;

                    tilesToSearch.Add(coord);
                }
                else if (moveCostToSearchedTiles[coord] > totalMovementCostToTile)
                {
                    moveCostToSearchedTiles[coord] = totalMovementCostToTile;//or adjust the cost

                    if (!ancestorPathmap.ContainsKey(coord))
                        ancestorPathmap.Add(coord, shortestPathToCurrentSearchTile);
                    else
                        ancestorPathmap[coord] = shortestPathToCurrentSearchTile;

                    tilesToSearch.Add(coord);
                }
                else if (moveCostToSearchedTiles[coord] == totalMovementCostToTile && ancestorPathmap[coord].Count > shortestPathToCurrentSearchTile.Count)
                {
                    moveCostToSearchedTiles[coord] = totalMovementCostToTile;//or adjust the cost

                    if (!ancestorPathmap.ContainsKey(coord))
                        ancestorPathmap.Add(coord, shortestPathToCurrentSearchTile);
                    else
                        ancestorPathmap[coord] = shortestPathToCurrentSearchTile;

                    tilesToSearch.Add(coord);
                }

                //check here if we've found the destination and early return?  
                if (coord == destination)
                    return;
            }

            foreach (var searchCandidate in tilesToSearch)
            {
                TerrainType terrainType = state[searchCandidate].TerrainType;
                if (terrainType == TerrainType.Water
                    || terrainType == TerrainType.Lava)
                    continue;

                //get movement cost to next tile
                int movementCost = (int)GetTileTotalMovementCost(state, searchCandidate);
                var movementCostToCoord = RulesProvider.GetMoveSpeedCost(speed, searchDepth) + movementCost;

                FindShortestPathRecursive(state, ancestorPathmap,
                    moveCostToSearchedTiles,
                    searchCandidate,
                    destination,
                    movementType,
                    speed,
                    movementCostToCoord + movementCostPrevious,
                    searchDepth + 1,
                    availableMovement);
            }
        }

        public static Dictionary<HexCoordinate, int> GetWalkableAdjacentTiles(BoardState state, HexCoordinate position, int basePotentialCost)
        {
            var walkableNeighbours = new Dictionary<HexCoordinate, int>();

            var neighbours = GetNeighbours(position);

            foreach (var coordinate in neighbours)
            {
                if (!IsWalkableAndEmpty(state, coordinate)) continue;

                walkableNeighbours.Add(coordinate, basePotentialCost);
            }

            return walkableNeighbours;
        }

        public static Dictionary<HexCoordinate, int> GetWalkableAxisTiles(BoardState state, HexCoordinate position, MovementType movementType, MovementSpeed movementSpeed, int range, int basePotentialCost)
        {
            var targets = new Dictionary<HexCoordinate, int>();

            foreach (var direction in Directions)
            {
                int moveCost = basePotentialCost;
                for (var i = 0; i < range; i++)
                {
                    var hexToCheck = position + (direction * (i + 1));

                    //if we've wandered off the map, don't keep going, obviously
                    if (!state.ContainsKey(hexToCheck))
                        break;

                    //if we can't get through this tile, ok, give up
                    if (!IsTilePassable(state, movementType, hexToCheck)) break;

                    moveCost += (int)GetTileTotalMovementCost(state, hexToCheck) + RulesProvider.GetMoveSpeedCost(movementSpeed, i);

                    if (IsWalkableAndEmpty(state, hexToCheck))
                        targets.Add(hexToCheck, moveCost);
                }
            }

            return targets;
        }

        public static HexCoordinate GetNearestEmptyNeighbourRecursive(BoardState state, HexCoordinate position, int maxSearchDepth, int searchDepth = 0)
        {
            if (searchDepth >= maxSearchDepth)
                return null;

            var adjacentTiles = GetNeighbours(position);

            foreach (var coord in adjacentTiles)
            {
                if (IsWalkableAndEmpty(state, coord))
                    return coord;
            }

            foreach (var coord in adjacentTiles)
            {
                return GetNearestEmptyNeighbourRecursive(state, coord, maxSearchDepth, searchDepth + 1);
            }

            return null;
        }

        public static float GetTileTotalMovementCost(BoardState state, HexCoordinate coordinate)
        {
            return state[coordinate].MovementCost + GetMovementCostModifier(state, coordinate);
        }

        public static float GetTileMovementCost(BoardState state, HexCoordinate coord)
        {
            return state[coord].MovementCost;
        }

        public static float GetMovementCostModifier(BoardState state, HexCoordinate coord)
        {
            if (state.TileEffects.Any(te => te.Position == coord))
                return state.TileEffects.Where(te => te.Position == coord).Sum(te => te.MovementModifier);
            else return 0.0f;
        }

        public static int GetPathLengthToTile(BoardState state, Character objectCharacter, HexCoordinate destination, List<HexCoordinate> path)
        {
            if (path == null)
                path = FindShortestPath(state, objectCharacter.Position, destination, 200, objectCharacter.MovementType, objectCharacter.MovementSpeed);
            if (path == null) return 0;

            var pathLength = 0;
            switch (objectCharacter.MovementSpeed)
            {
                case MovementSpeed.Slow:
                    pathLength = path.Select((coord, index) => (int)GetTileTotalMovementCost(state, coord) + RulesProvider.GetMoveSpeedCost(MovementSpeed.Slow, index)).Sum();
                    break;
                case MovementSpeed.Normal:
                    pathLength = path.Select((coord, index) => (int)GetTileTotalMovementCost(state, coord) + RulesProvider.GetMoveSpeedCost(MovementSpeed.Normal, index)).Sum();
                    break;
                case MovementSpeed.Fast:
                    pathLength = path.Select((coord, index) => (int)GetTileTotalMovementCost(state, coord) + RulesProvider.GetMoveSpeedCost(MovementSpeed.Fast, index)).Sum();
                    break;
                default:
                    break;
            }
            //no negative path lengths
            return pathLength >= 0 ? pathLength : 0;
        }

        public static bool IsTilePassable(BoardState state, MovementType movementType, HexCoordinate coordinate)
        {
            //tile validation goes here.
            if (!IsHexWalkable(state, coordinate)) return false;

            var character = state.Characters.FirstOrDefault(c => c.Position == coordinate);
            if (character == null)
                return true;

            switch (movementType)
            {
                case MovementType.NormalMove:
                    return false;
                case MovementType.MoveThroughHeroes:
                    if (!character.IsHero)
                        return false;
                    break;
                case MovementType.MoveThroughMonsters:
                    if (character.IsHero)
                        return false;
                    break;
                case MovementType.MoveThroughUnits:
                    return true;
                case MovementType.Etheral:
                    break;
                case MovementType.Flying:
                    break;
                default:
                    return false;
            }

            return true;
        }


        #endregion

        #region Map Helpers

        public static bool IsHexInMap(HexCoordinate coord)
        {
            var coord2D = CubeToOddRight(coord);

            if (Math.Abs((int)coord2D.X) > MapWidth || Math.Abs((int)coord2D.Y) > MapHeight)
                return false;

            return true;
        }

        //not screenSpace
        private static Vector2 Get2DCoords(HexCoordinate coordinate)
        {
            var posX = (Sqrt3 * coordinate.X + (Sqrt3 / 2 * coordinate.Z));
            var posY = 1.5f * coordinate.Z;

            return new Vector2(posX, posY);
        }

        //convert from cube coordinates to 2D "odd-right" coordinate system
        //This is sometimes more intuitive to work with when using a rectangular map.
        public static Vector2 CubeToOddRight(HexCoordinate coord)
        {
            var col = coord.X + (coord.Z - (coord.Z & 1)) / 2;
            var row = coord.Z;
            return new Vector2(col, row);
        }

        public static int DistanceBetweenPoints(HexCoordinate a, HexCoordinate b)
        {
            return (Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y) + Math.Abs(a.Z - b.Z)) / 2;
        }

        public static List<HexCoordinate> GetNeighbours(HexCoordinate target)
        {
            var neighbours = new List<HexCoordinate>();
            for (int i = 0; i < 6; i++)
            {
                HexCoordinate neighbourCoordinate = target + Directions[i];

                var oddR = CubeToOddRight(neighbourCoordinate);

                //don't look at neighbours outside of the map
                if (Math.Abs((int)oddR.X) > MapWidth || Math.Abs((int)oddR.Y) > MapHeight)
                    continue;

                neighbours.Add(neighbourCoordinate);
            }

            return neighbours;
        }

        public static HexCoordinate GetNearestNeighbor(HexCoordinate start, HexCoordinate end)
        {
            var startCoords = Get2DCoords(start);
            int distance = 100;
            HexCoordinate nearest = null;

            for (int i = 0; i < 6; i++)
            {
                HexCoordinate neighbourCoordinate = end + Directions[i];

                var oddR = CubeToOddRight(neighbourCoordinate);

                //don't look at neighbours outside of the map
                if (Math.Abs((int)oddR.X) > MapWidth || Math.Abs((int)oddR.Y) > MapHeight)
                    continue;

                var delta = DistanceBetweenPoints(start, neighbourCoordinate);
                if (delta < distance) //if this is naively closer then we have a new nearest tile
                {
                    nearest = neighbourCoordinate;
                    distance = delta;
                }
                //if they're tied then convert to 2d coords and use that as the tiebeaker
                //I have no idea why this works but it ends up looking good intuitively
                else if (delta == distance && nearest != null)
                {
                    var neighborCoords = Get2DCoords(neighbourCoordinate);
                    var nearestCoords = Get2DCoords(nearest);

                    var nearestDelta = nearestCoords - startCoords;
                    var neighborDelta = neighborCoords - startCoords;

                    if (neighborDelta.Length() < nearestDelta.Length())
                    {
                        nearest = neighbourCoordinate;
                        distance = delta;
                    }
                }
            }

            return nearest;
        }

        public static HexCoordinate GetRandomCoordinateInMap()
        {
            var rowIndex = Random.Next(-MapHeight, MapHeight);
            var columnIndex = Random.Next(-MapWidth, MapWidth);

            var x = columnIndex - (rowIndex - (rowIndex & 1)) / 2;
            var z = rowIndex;
            var y = -(x + z);

            if (x + y + z != 0)
                throw new Exception(" Impossible co-ordinate");

            return new HexCoordinate(x, y, z);
        }

        public static HexCoordinate GetPushDirection(HexCoordinate pushOrigin, HexCoordinate targetPosition)
        {
            //determine direction of push
            var nearestNeighbor = GetNearestNeighbor(pushOrigin, targetPosition);
            var direction = targetPosition - nearestNeighbor;

            return direction;
        }

        #endregion
    }
}
