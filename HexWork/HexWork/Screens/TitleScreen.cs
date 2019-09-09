using Microsoft.Xna.Framework;
using HexWork.Interfaces;

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
        public TitleScreen(IScreenManager _screenManager, PlayerIndex? _controllingPlayer = null)
            : base(_screenManager, _controllingPlayer)
        {
            MenuEntry pressStart = new MenuEntry("Press Start");
            pressStart.selected += StartMenuSelected;
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
        void StartMenuSelected(object sender, PlayerIndexEventArgs args)
        {
            screenManager.InputManager.PlayerJoined(args.PlayerIndex);
            controllingPlayer = args.PlayerIndex;
            screenManager.AddScreen(new MainMenuScreen(screenManager, controllingPlayer.Value));
            this.state = ScreenState.Deactivating;
        }
        #endregion

		#region Public Methods
		#endregion

		#endregion
    }
}
