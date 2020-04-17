﻿using System;
using HexWork.Gameplay;
using HexWork.Interfaces;
using Microsoft.Xna.Framework;
using MonoGameTestProject.Gameplay;

namespace HexWork.Screens
{
	public class LevelUpScreen : MenuScreen
	{
		private HexWork _hexGame;

		public LevelUpScreen(IScreenManager _screenManager, Guid characterId, PlayerIndex? _controllingPlayer = null) : base(_screenManager, _controllingPlayer)
		{ 
			_hexGame = (HexWork)_screenManager.Game;
            fullscreen = false;
            
			var gainPower = new MenuEntry("Gain Power");
			var gainHealth = new MenuEntry("Gain Health");

			//gainPower.selected += (sender, args) =>
			//	{
			//		GameStateManager.GameState.CharacterGainPower(characterId);
			//		this.Exit();
			//	};
			//gainHealth.selected += (sender, args) =>
			//	{
			//		GameStateManager.GameState.CharacterGainHealth(characterId);
			//		this.Exit();
			//	};

			menu.Add(gainPower);
			menu.Add(gainHealth);

			position = GetPointOnScreen(0.5f, 0.5f);
		}
		
		public override void Draw(GameTime gameTime)
		{
			

			base.Draw(gameTime);
		}
	}
}