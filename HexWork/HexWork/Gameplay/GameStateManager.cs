using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;
using HexWork.Gameplay;

namespace MonoGameTestProject.Gameplay
{
    public static class GameStateManager
    {
        #region Attributes

        private static GameState _gameState;

        #endregion

        #region Properties

        public static GameState CurrentGameState
        {
            get => _gameState;
        }

        #endregion
        
        #region Methods

        public static void SetGameState(GameState gameState)
        {
            _gameState = gameState;
        }
		
		#endregion
	}
}