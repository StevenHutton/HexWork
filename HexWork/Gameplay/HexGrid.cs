using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace HexWork.Gameplay
{
    public class HexCoordinate : IEquatable<HexCoordinate>
    {
        #region Properties

        public int X { get; set; }

        public int Y { get; set; }

        public int Z { get; set; }

	    public int VectorLength => (int)Math.Sqrt((X * X) + (Y * Y) + (Z * Z));

        #endregion

        public HexCoordinate(int x, int y)
        {
            X = x;
            Y = y;
            Z = -(X + Y);
        }

        public HexCoordinate(int x, int y, int z)
        {
            X = x;
            Y = y;
            Z = z;

            if (x + y + z != 0)
            {
                throw new Exception("Impossible tile coordinate");
            }
        }

        public void SetValues(int x, int y, int z)
        {
            X = x;
            Y = y;
            Z = z;

            if (x + y + z != 0)
            {
                throw new Exception("Impossible tile coordinate");
            }
        }

        #region IComparable

        public bool Equals(HexCoordinate other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return X == other.X && Y == other.Y;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((HexCoordinate) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = 17;
                hashCode = hashCode * 23 + X.GetHashCode();
                hashCode = hashCode * 29 + Y.GetHashCode();
                return hashCode;
            }
        }

        #endregion

        #region Operator Overloads

        public static HexCoordinate operator +(HexCoordinate a, HexCoordinate b)
        {
            return new HexCoordinate(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
        }

        public static HexCoordinate operator -(HexCoordinate a, HexCoordinate b)
        {
            return new HexCoordinate(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
        }

        public static bool operator ==(HexCoordinate a, HexCoordinate b)
        {
            return a?.Equals(b) ?? ReferenceEquals(null, b);
        }

        public static bool operator !=(HexCoordinate a, HexCoordinate b)
        {
            return !(a == b);
        }

        public static HexCoordinate operator *(HexCoordinate a, int b)
        {
            return new HexCoordinate(a.X * b, a.Y * b, a.Z *b);
        }

        #endregion
    }
    
    public class HexGrid
    {
        #region Attributesa

        public static readonly HexCoordinate[] Directions = {
            new HexCoordinate(+1, -1, 0),
            new HexCoordinate(+1, 0, -1),
            new HexCoordinate(0, +1, -1),
            new HexCoordinate(-1, +1, 0),
            new HexCoordinate(-1, 0, +1),
            new HexCoordinate(0, -1, +1)
        };
        
        public Dictionary<HexCoordinate, Tile> Map = new Dictionary<HexCoordinate, Tile>();

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

                    Map.Add(coord, new Tile());
                }
            }
            
            RandomizeTerrain();

            foreach (var tile in Map.Values)
            {
                switch (tile.TerrainType)
                {
                    case TerrainType.Water:
                        tile.Color = Color.CadetBlue;
                        tile.IsWalkable = true;
                        tile.MovementCost = 1;
                        break;
                    case TerrainType.Ground:
                        tile.Color =  Color.SaddleBrown;
                        tile.IsWalkable = true;
                        tile.MovementCost = 1;
                        break;
                    case TerrainType.Lava:
                        tile.Color =  Color.Orange;
                        tile.IsWalkable = true;
                        tile.MovementCost = 1;
                        break;
                    case TerrainType.Ice:
                        tile.Color =  Color.LightBlue;
                        tile.IsWalkable = true;
                        tile.MovementCost = 2;
                        break;
                    case TerrainType.ThinIce:
                        tile.Color =  Color.LightSteelBlue;
                        tile.IsWalkable = true;
                        tile.MovementCost = 2;
                        break;
                    case TerrainType.Snow:
                        tile.Color =  Color.DarkGray;
                        tile.IsWalkable = true;
                        tile.MovementCost = 2;
                        break;
                    case TerrainType.Sand:
                        tile.Color =  Color.SandyBrown;
                        tile.IsWalkable = true;
                        tile.MovementCost = 2;
                        break;
                    case TerrainType.Pit:
                        tile.Color =  Color.DarkSlateGray;
                        tile.IsWalkable = false;
                        tile.MovementCost = 0;
                        break;
                    case TerrainType.Wall:
                        tile.Color = Color.Black;
                        tile.IsWalkable = false;
                        tile.MovementCost = 0;
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
            foreach (var tile in Map.Values)
            {
                if (rand.Next(10000) <= 1500 && tile.TerrainType == TerrainType.Ground) //make X% of tiles into a random non-ground terrain type
                {
                    tile.TerrainType = (TerrainType)(rand.Next(8) +1);
                }
            }

            //go through again and check the neighbors for terrain
            foreach (var entry in Map)
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
            foreach (var entry in Map)
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
                if (Map.ContainsKey(neighbourCoordinate))
                {
                    tiles.Add(Map[neighbourCoordinate]);
                }
            }

            return tiles;
        }
        
        public List<HexCoordinate> GetNeighborCoordinates(HexCoordinate position)
        {
            List<HexCoordinate> coordinates = new List<HexCoordinate>();

            //loop through neighbours
            for (int i = 0; i < 6; i++)
            {
                HexCoordinate neighbourCoordinate = position + Directions[i];
                if (Map.ContainsKey(neighbourCoordinate))
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

        public int DistanceBetweenPoints(HexCoordinate a, HexCoordinate b)
        {
            return (Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y) + Math.Abs(a.Z - b.Z))/2;
        }

        #endregion

        #endregion
    }
}
