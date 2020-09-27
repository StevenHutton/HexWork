using System;
using System.Collections.Generic;
using System.Linq;
using HexWork.Gameplay.GameObject;
using HexWork.Gameplay.GameObject.Characters;
using Microsoft.Xna.Framework;

namespace HexWork.Gameplay
{
    public class BoardState : Dictionary<HexCoordinate, Tile>
    {
        #region Attributes

        //list of all characters in the current match ordered by initiative count
        public IEnumerable<Character> Characters => Entities.OfType<Character>();

        public IEnumerable<TileEffect> TileEffects => Entities.OfType<TileEffect>();

        public List<HexGameObject> Entities = new List<HexGameObject>();

        public int MaxPotential = 9;
        public int Potential = 0;

        #endregion

        #region Properties

        public Character ActiveCharacter;
        public IEnumerable<Character> Heroes => LivingCharacters.Where(character => character.IsHero);
        public IEnumerable<Character> LivingCharacters => Characters.Where(c => c.IsAlive);
        public IEnumerable<Character> Enemies => LivingCharacters.Where(character => !character.IsHero);

        #endregion

        public BoardState(int mapWidth, int mapHeight)
        {
            GenerateMap(mapWidth, mapHeight);
        }

        protected void GenerateMap(int columns, int rows)
        {
            for (var columnIndex = -columns; columnIndex <= columns; columnIndex++)
            {
                for (var rowIndex = -rows; rowIndex <= rows; rowIndex++)
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
                var neighbors = GameState.GetNeighbours(entry.Key);

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
                var neighbors = GameState.GetNeighbours(entry.Key);

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

        public Character GetCharacterAtInitiativeZero()
        {
            return LivingCharacters.OrderBy(a => a.TurnTimer).First();
        }
    }
}
