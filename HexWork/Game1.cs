using System;
using Microsoft.Xna.Framework;
using HexWork.Gameplay;
using HexWork.Interfaces;
using HexWork.Screens;
using HexWork.UI;
using MonoGameTestProject.Gameplay;

namespace HexWork
{
	/// <summary>
	/// This is the main type for your game.
	/// </summary>
	public class HexWork : Game
	{
		#region Attributes

		GraphicsDeviceManager _graphics;

		public readonly int ScreenHeight = 900;
		public readonly int ScreenWidth = 1600;

		private IScreenManager _screenManager;
		private IInputManager _inputManager;

        #region Gameplay

        private const int MonsterCount = 8;

		#endregion

		private Vector2 _screenCenterVector;

		private readonly float _sqrt3 = (float) Math.Sqrt(3.0);

		#endregion
		
		#region Methods

		#region Initialisation Methods

		public HexWork()
		{
			_graphics = new GraphicsDeviceManager(this)
			{
				PreferredBackBufferWidth = ScreenWidth,
				PreferredBackBufferHeight = ScreenHeight
			};
            _screenCenterVector = new Vector2((float) ScreenWidth / 2, (float) ScreenHeight / 2);

            _screenManager = new ScreenManager(this);
			_inputManager = new InputManager(this);

			Components.Add(_screenManager);
			Components.Add(_inputManager);

			Content.RootDirectory = "Content";
            
            GameStateManager.SetGameState(new GameState());
        }

		/// <summary>
		/// Allows the game to perform any initialization it needs to before starting to run.
		/// This is where it can query for any required services and load any non-graphic
		/// related content.  Calling base.Initialize will enumerate through any components
		/// and initialize them as well.
		/// </summary>
		protected override void Initialize()
		{
			this.IsMouseVisible = true;
			base.Initialize();

            _screenManager.AddScreen(new BattleSelectionScreen(_screenManager));
        }

		/// <summary>
		/// LoadContent will be called once per game and is the place to load
		/// all of your content.
		/// </summary>
		protected override void LoadContent()
		{
			
		}
        
        #endregion
		
		#endregion

		#region Main Loop

		/// <summary>
		/// Allows the game to run logic such as updating the world,
		/// checking for collisions, gathering input, and playing audio.
		/// </summary>
		/// <param name="gameTime">Provides a snapshot of timing values.</param>
		protected override void Update(GameTime gameTime)
		{
			base.Update(gameTime);
		}

		/// <summary>
		/// This is called when the game should draw itself.
		/// </summary>
		/// <param name="gameTime">Provides a snapshot of timing values.</param>
		protected override void Draw(GameTime gameTime)
		{
			GraphicsDevice.Clear(Color.CornflowerBlue);

			base.Draw(gameTime);
		}

		#endregion
	}
}