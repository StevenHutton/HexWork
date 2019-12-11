using Microsoft.Xna.Framework;
using HexWork.Gameplay;
using HexWork.Interfaces;
using HexWork.UI;
using MonoGameTestProject.Gameplay;

namespace HexWork.Screens
{
    public class BattleScreen : Screen
    {
        #region Attributes
        
        private readonly UserInterface _ui;
        private readonly GameState _gameState;

		#endregion
        
		#region Methods
        
        public BattleScreen(IScreenManager _screenManager)
            : base(_screenManager)
        {
            var game = (HexWork)_screenManager.Game;
            _gameState = GameStateManager.CurrentGameState;
            _ui = new UserInterface(game);
        }

        public void Initialize()
        {
            _ui.Initialise();
        }

        public override void LoadContent(Game game)
        {
            _ui.LoadContent(game);
            _gameState.StartGame();
            Initialize();
        }

        public override void Update(GameTime gameTime)
        {
            _gameState.Update(gameTime);
            _ui.Update(gameTime);
        }
		
        public override void Draw(GameTime gameTime)
        {
            _ui.Draw();
        }
        
        #endregion
    }
}
