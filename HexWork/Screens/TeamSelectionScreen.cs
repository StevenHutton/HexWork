using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using HexWork.Interfaces;

namespace HexWork.Screens
{
    public class TeamSelectionScreen : Screen
    {
	    private HexWork _hexGame;

        public TeamSelectionScreen(IScreenManager _screenManager) : base(_screenManager)
        {
			_hexGame = (HexWork)_screenManager.Game;
        }

        public TeamSelectionScreen(IScreenManager _screenManager, PlayerIndex? _controllingPlayer) : base(_screenManager, _controllingPlayer)
        {

        }

        public override void Draw(GameTime gameTime)
        {
			

            base.Draw(gameTime);
        }
    }
}