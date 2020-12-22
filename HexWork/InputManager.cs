using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using HexWork.Interfaces;

namespace HexWork
{
    public class InputManager: DrawableGameComponent, IInputManager
    {
		#region Attributes

        GamePadState currentPadState;
        GamePadState lastPadState;

        KeyboardState currentKeyoardState;
        KeyboardState lastKeyboardState;

		#endregion

		#region Methods

		#region Initialisation

        public InputManager(Game game)
            : base(game)
        {
            Game.Services.AddService(typeof(IInputManager), this);
            
            currentPadState = new GamePadState();
            lastPadState = new GamePadState();
            currentKeyoardState = new KeyboardState();
            lastKeyboardState = new KeyboardState();
        }

        public override void Initialize()
        {
            base.Initialize();
        }

		#endregion

		#region Updating

        public override void Update(GameTime gameTime)
        {
            lastPadState = currentPadState;
            currentPadState = GamePad.GetState(0);
            lastKeyboardState = currentKeyoardState;
            currentKeyoardState = Keyboard.GetState();
        }
		#endregion

		#region Drawing

        public override void Draw(GameTime gameTime)
        {

        }
		#endregion

        #region Public Methods

        /// <summary>
        /// This method returns a bool if a player has pressed a specific button since the last update
        /// </summary>
        /// <param name="button">The specific button that we're going to be checking if the player has pressed</param>
        /// <param name="player">The number of the player from 1 to 4</param>
        /// <param name="pIndex">A return value indicating from which controller the input data was received.</param>
        /// <returns>Returns a bool. Returns true if the button is down this frame and was up in the last frame. Otherwise
        /// returns false.</returns>
        public bool IsNewButtonPress(Buttons button)
        {
            if (currentPadState.IsButtonDown(button)
                && lastPadState.IsButtonUp(button))
            {
                return true;
            }
            else
                return false;
        }

        /// <summary>
        /// This method returns a bool if a player has pressed a specific key since the last update
        /// </summary>
        /// <param name="key">The key on the keyboard to check</param>
        /// <returns>Returns a bool. Returns true if the key is down this frame and was up in the last frame. Otherwise
        /// returns false.</returns>
        public bool IsNewKeyPress(Keys key)
        {
            if (currentKeyoardState.IsKeyDown(key) &&
                lastKeyboardState.IsKeyUp(key))
            {
                return true;
            }
            else
                return false;
        }

        public bool IsButtonDown(Buttons button)
        {
            if (currentPadState.IsButtonDown(button))
            {
                return true;
            }
            else
                return false;
        }

        #endregion

        #endregion
    }
}