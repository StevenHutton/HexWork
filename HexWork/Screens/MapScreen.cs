using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HexWork.Interfaces;
using Microsoft.Xna.Framework;

namespace HexWork.Screens
{
	public class MapScreen : Screen
	{
		private HexWork _hexGame;

		public MapScreen(IScreenManager _screenManager) : base(_screenManager)
		{
			_hexGame = (HexWork)_screenManager.Game;
		}

		public MapScreen(IScreenManager _screenManager, PlayerIndex? _controllingPlayer) : base(_screenManager, _controllingPlayer)
		{
			_hexGame = (HexWork)_screenManager.Game;
		}

		public override void Draw(GameTime gameTime)
		{


			base.Draw(gameTime);
		}
	}
}
