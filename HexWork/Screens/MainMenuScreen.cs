using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using HexWork.Interfaces;

namespace HexWork.Screens
{
    class MainMenuScreen : MenuScreen
    {
        #region Attributes
        #endregion

        #region Properties
        #endregion

        #region Methods

        #region Initialisation
        public MainMenuScreen(IScreenManager _screenManager, PlayerIndex? _controllingPlayer)
            : base(_screenManager, _controllingPlayer)
        {
            MenuEntry pressStart = new MenuEntry("Start Game");
            MenuEntry exit = new MenuEntry("Exit");

            pressStart.selected += StartGame;
            exit.selected += Exit;

            menu.Add(pressStart);
            menu.Add(exit);

            position = GetPointOnScreen(0.5f, 0.5f);
        }
        #endregion

        #region Updating
        #endregion

        #region Drawing
        #endregion

        #region Private Methods

        private void StartGame(object sender, EventArgs args)
        {
            screenManager.AddScreen(new TeamSelectionScreen(screenManager));
            screenManager.RemoveScreen(this);
        }

        private void Exit(object sender, EventArgs args)
        {
            screenManager.Game.Exit();
        }

        #endregion

        #region Public Methods
        #endregion

        #endregion
    }
}
