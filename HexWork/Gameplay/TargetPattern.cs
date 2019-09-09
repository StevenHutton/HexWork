using System.Collections.Generic;
using System.Linq;
using HexWork.Gameplay;

namespace MonoGameTestProject.Gameplay
{
    public class TargetPattern
    {
        private readonly List<HexCoordinate> _pattern;

        public List<HexCoordinate> Pattern => _pattern;
        
        public TargetPattern(TargetPattern pattern)
        {
            //make a new list with the same content by calling "ToList".
            _pattern = pattern.Pattern.ToList();
        }

        public TargetPattern(params HexCoordinate[] coords)
        {
            _pattern = coords.Length == 0 ? new List<HexCoordinate> {new HexCoordinate(0, 0, 0)} : coords.ToList();
        }

        public void RotateClockwise()
        {
            foreach (var coord in _pattern)
            {
                coord.SetValues(-coord.Z, -coord.X, -coord.Y);
            }
        }

        public void RotateAntiClockwise()
        {
            foreach (var coord in _pattern)
            {
                coord.SetValues(-coord.Y, -coord.Z, -coord.X);
            }
        }

        /// <summary>
        /// Get the pattern translated to the target coordinates in hex space.
        /// </summary>
        /// <param name="coordinate"></param>
        /// <returns></returns>
        public List<HexCoordinate> GetPattern(HexCoordinate coordinate)
        {
            return _pattern.Select(coord => coord + coordinate).ToList();
        }
    }
}
