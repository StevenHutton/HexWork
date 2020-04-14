using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HexWork.Gameplay.Characters;
using HexWork.Gameplay.Interfaces;

namespace HexWork.Gameplay
{
    public static class TargetingHelper
    {
        #region Targetting Methods

        public static List<HexCoordinate> GetValidTargetTilesLos(Character objectCharacter, int range,
            IGameStateObject gameState)
        {
            return gameState.GetVisibleTilesInRange(objectCharacter, range);
        }

        public static List<HexCoordinate> GetValidTargetTilesNoLos(Character objectCharacter, int range,
            IGameStateObject gameState)
        {
            return gameState.GetTilesInRange(objectCharacter, range);
        }

        public static List<HexCoordinate> GetValidTargetTilesLosIgnoreUnits(Character objectCharacter, int range,
            IGameStateObject gameState)
        {
            return gameState.GetVisibleTilesInRangeIgnoreUnits(objectCharacter, range);
        }

        public static List<HexCoordinate> GetValidAxisTargetTilesLos(Character objectCharacter, int range,
            IGameStateObject gameState)
        {
            return gameState.GetVisibleAxisTilesInRange(objectCharacter, range);
        }

        public static List<HexCoordinate> GetValidAxisTargetTilesLosIgnoreUnits(Character objectCharacter, int range,
            IGameStateObject gameState)
        {
            return gameState.GetVisibleAxisTilesInRangeIgnoreUnits(objectCharacter, range);
        }

        public static List<HexCoordinate> GetValidAxisTargetTilesNoLos(Character objectCharacter, int range,
            IGameStateObject gameState)
        {
            return gameState.GetAxisTilesInRange(objectCharacter, range);
        }

        #endregion
    }
}
