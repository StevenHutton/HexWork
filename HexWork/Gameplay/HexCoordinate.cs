using System;

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
            return Equals((HexCoordinate)obj);
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
            return new HexCoordinate(a.X * b, a.Y * b, a.Z * b);
        }

        #endregion

        public override string ToString()
        {
            return $"{X},{Y},{Z}";
        }
    }
}
