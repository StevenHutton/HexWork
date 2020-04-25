using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;

namespace HexWork.Gameplay
{
    
    public class HexGrid : Dictionary<HexCoordinate, Tile>
    {
        #region Attributes

        public static readonly HexCoordinate[] Directions = {
            new HexCoordinate(+1, -1, 0),
            new HexCoordinate(+1, 0, -1),
            new HexCoordinate(0, +1, -1),
            new HexCoordinate(-1, +1, 0),
            new HexCoordinate(-1, 0, +1),
            new HexCoordinate(0, -1, +1)
        };
        
        private int _cols, _rows;

        #endregion

        #region Properties
        
        #endregion

        #region Methods

        public HexGrid()
        {
            GenerateMap(5, 6);
        }

        #region Private Methods

        private void GenerateMap(int rows, int columns)
        {
            _cols = columns;
            _rows = rows;

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
                        tile.MovementCostModifier = 0;
                        break;
                    case TerrainType.Ground:
                        tile.Color =  Color.SaddleBrown;
                        tile.IsWalkable = true;
                        tile.MovementCostModifier = 0;
                        break;
                    case TerrainType.Lava:
                        tile.Color =  Color.Orange;
                        tile.IsWalkable = true;
                        tile.MovementCostModifier = 0.1f;
                        break;
                    case TerrainType.Ice:
                        tile.Color =  Color.LightBlue;
                        tile.IsWalkable = true;
                        tile.MovementCostModifier = 1;
                        break;
                    case TerrainType.ThinIce:
                        tile.Color =  Color.LightSteelBlue;
                        tile.IsWalkable = true;
                        tile.MovementCostModifier = 1;
                        break;
                    case TerrainType.Snow:
                        tile.Color =  Color.DarkGray;
                        tile.IsWalkable = true;
                        tile.MovementCostModifier = 1;
                        break;
                    case TerrainType.Sand:
                        tile.Color =  Color.SandyBrown;
                        tile.IsWalkable = true;
                        tile.MovementCostModifier = 1;
                        break;
                    case TerrainType.Pit:
                        tile.Color =  Color.DarkSlateGray;
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
                    tile.TerrainType = (TerrainType)(rand.Next(8) +1);
                }
            }

            //go through again and check the neighbors for terrain
            foreach (var entry in this)
            {
                var neighbors = GetNeighborTiles(entry.Key);

                foreach (Tile neighbor in neighbors)
                {
                    //if this tile is ground and it's neighbor isn't.
                    if (neighbor.TerrainType == TerrainType.Ground ||
                        entry.Value.TerrainType != TerrainType.Ground) continue;

                    //X% chance that we copy the neighbor's terrain type.
                    if (rand.Next(99) < 10)
                    {
                        entry.Value.TerrainType = neighbor.TerrainType;
                    }
                }
            }

            //go through again and check the neighbors for terrain
            foreach (var entry in this)
            {
                var neighbors = GetNeighborTiles(entry.Key);

                foreach (Tile neighbor in neighbors)
                {
                    //if this tile is ground and it's neighbor isn't.
                    if (neighbor.TerrainType == TerrainType.Ground ||
                        entry.Value.TerrainType != TerrainType.Ground) continue;

                    //X% chance that we copy the neighbor's terrain type.
                    if (rand.Next(99) < 10)
                    {
                        entry.Value.TerrainType = neighbor.TerrainType;
                    }
                }
            }
        }
        
        #endregion

        #region Public Methods
		
        public List<Tile> GetNeighborTiles(HexCoordinate position)
        {
            List<Tile> tiles = new List<Tile>();

            //loop through neighbours
            for (int i = 0; i < 6; i++)
            {
                HexCoordinate neighbourCoordinate = position + Directions[i];
                if (this.ContainsKey(neighbourCoordinate))
                {
                    tiles.Add(this[neighbourCoordinate]);
                }
            }

            return tiles;
        }
        
        public List<HexCoordinate> GetNeighborCoordinates(HexCoordinate position)
        {
            List<HexCoordinate> coordinates = new List<HexCoordinate>();

            //loop through neighbors
            for (int i = 0; i < 6; i++)
            {
                HexCoordinate neighbourCoordinate = position + Directions[i];
                if (this.ContainsKey(neighbourCoordinate))
                {
                    coordinates.Add(neighbourCoordinate);
                }
            }

            return coordinates;
        }

        public HexCoordinate GetRandomCoordinateInMap()
        {
            Random rand = new Random();

            var rowIndex = rand.Next(-_rows, _rows);
            var columnIndex = rand.Next(-_cols, _cols);

            var x = columnIndex - (rowIndex - (rowIndex & 1)) / 2;
            var z = rowIndex;
            var y = -(x + z);

            if (x + y + z != 0)
                throw new Exception(" Impossible co-ordinate");

            return new HexCoordinate(x, y, z);
        }

        public static int DistanceBetweenPoints(HexCoordinate a, HexCoordinate b)
        {
            return (Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y) + Math.Abs(a.Z - b.Z))/2;
        }

        #endregion

        #endregion
    }
}
