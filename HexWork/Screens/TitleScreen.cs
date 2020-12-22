using Microsoft.Xna.Framework;
using HexWork.Interfaces;
using System;

namespace HexWork.Screens
{
    class TitleScreen : MenuScreen
    {
		#region Attributes
		#endregion

		#region Properties
		#endregion

		#region Methods

		#region Initialisation
        public TitleScreen(IScreenManager _screenManager)
            : base(_screenManager)
        {
            MenuEntry pressStart = new MenuEntry("Press Start");
            pressStart.Selected += StartMenuSelected;
            menu.Add(pressStart);
            position = GetPointOnScreen(0.5f,0.5f);
        }
		#endregion

		#region Updating

        public override void HandleInput()
        {
            base.HandleInput();
        }

		#endregion

		#region Drawing
		#endregion

		#region Private Methods

        void StartMenuSelected(object sender, EventArgs args)
        {
            screenManager.AddScreen(new MainMenuScreen(screenManager));
            this.state = ScreenState.Deactivating;
        }

        #endregion

		#region Public Methods
		#endregion

		#endregion
    }
}
