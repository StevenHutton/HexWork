using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HexWork.Gameplay;
using HexWork.Interfaces;
using Microsoft.Xna.Framework;
using MonoGameTestProject.Gameplay;

namespace HexWork.Screens
{
    

    public class BattleSelectionScreen : Screen
    {
        private HexWork _hexGame;
        private GameState _gameState;

        public BattleSelectionScreen(IScreenManager _screenManager) : base(_screenManager)
        {
            _hexGame = (HexWork)_screenManager.Game;
            _gameState = GameStateManager.CurrentGameState;
        }

        public BattleSelectionScreen(IScreenManager _screenManager, PlayerIndex? _controllingPlayer) : base(_screenManager, _controllingPlayer)
        {
            _hexGame = (HexWork)_screenManager.Game;
        }

        public override void Draw(GameTime gameTime)
        {
            

            base.Draw(gameTime);
        }
    }
}
