using System;
using HexWork.Interfaces;
using HexWork.UI;

namespace HexWork.Screens
{
    public class MainMenuScreen : MenuScreen
    {
        public MainMenuScreen(IScreenManager _screenManager)
            : base(_screenManager)
        {
            MenuEntry pressStart = new MenuEntry("Start Game");
            MenuEntry exit = new MenuEntry("Exit");

            pressStart.Selected += StartGame;
            exit.Selected += Exit;

            menu.Add(pressStart);
            menu.Add(exit);

            position = GetPointOnScreen(0.5f, 0.5f);
        }

        private void StartGame(object sender, EventArgs args)
        {
            screenManager.AddScreen(new BattleScreen(screenManager, 2));
            screenManager.RemoveScreen(this);
        }

        private void Exit(object sender, EventArgs args)
        {
            screenManager.Game.Exit();
        }
    }
}
