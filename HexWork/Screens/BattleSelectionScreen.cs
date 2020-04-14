using HexWork.Gameplay;
using HexWork.Interfaces;
using HexWork.UI;
using Microsoft.Xna.Framework;
using MonoGameTestProject.Gameplay;

namespace HexWork.Screens
{
    enum RoomType
    {
        Battle,
        Event,
    }

    public class BattleSelectionScreen : MenuScreen
    {
        private HexWork _hexGame;

        public BattleSelectionScreen(IScreenManager _screenManager) : base(_screenManager, null)
        {
            _hexGame = (HexWork)_screenManager.Game;

            menu.Add(new MenuEntry("Easy", EasyMission));
            menu.Add(new MenuEntry("Medium", MediumMission));
            menu.Add(new MenuEntry("Hard", HardMission));
        }

        private void EasyMission(object sender, PlayerIndexEventArgs args)
        {
            screenManager.AddScreen(new BattleScreen(screenManager, 1));
            Exit();
        }
        private void MediumMission(object sender, PlayerIndexEventArgs args)
        {
            screenManager.AddScreen(new BattleScreen(screenManager, 2));
            Exit();
        }
        private void HardMission(object sender, PlayerIndexEventArgs args)
        {
            screenManager.AddScreen(new BattleScreen(screenManager, 3));
            Exit();
        }

        public override void Draw(GameTime gameTime)
        {
            

            base.Draw(gameTime);
        }
    }
}
