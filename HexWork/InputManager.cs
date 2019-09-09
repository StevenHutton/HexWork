using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using HexWork.Interfaces;

namespace HexWork
{
    public class InputManager: DrawableGameComponent, IInputManager
    {
		#region Attributes

        const int MAXINPUTS = 4;

        int currentPlayers = 0;

        bool[] isActive;

        GamePadState[] currentPadStates;
        GamePadState[] lastPadStates;

        KeyboardState currentKeyoardState;
        KeyboardState lastKeyboardState;

		#endregion

		#region Properties

		#endregion

		#region Methods

		#region Initialisation

        public InputManager(Game game)
            : base(game)
        {
            Game.Services.AddService(typeof(IInputManager), this);
            
            isActive = new bool[MAXINPUTS];
            currentPadStates = new GamePadState[MAXINPUTS];
            lastPadStates = new GamePadState[MAXINPUTS];
            currentKeyoardState = new KeyboardState();
            lastKeyboardState = new KeyboardState();
        }

        public override void Initialize()
        {
            for(int i = 0; i < 4; i++)
            {
                currentPadStates[i] = new GamePadState();
                isActive[i] = false;
            }
            currentPlayers = 0;
            base.Initialize();
        }

		#endregion

		#region Updating

        public override void Update(GameTime gameTime)
        {
            for (int i = 0; i < MAXINPUTS ; i++)
            {
                //update all of the controllers
                lastPadStates[i] = currentPadStates[i];
                currentPadStates[i] = GamePad.GetState((PlayerIndex)i);

                //check if pad was connected this frame
                //if(isActive[i])
                //{
                //    if(currentPadStates[(int)players[i]].IsConnected && !lastPadStates[(int)players[i]].IsConnected)
                //    {
                //        throw new Exception("Active pad disconnected");
                //    }
                //}
            }
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
        public bool IsNewButtonPress(Buttons button, PlayerIndex? player, out PlayerIndex pIndex)
        {
            if (player.HasValue)
            {
                pIndex = player.Value;

                if (currentPadStates[(int)player.Value].IsButtonDown(button) &&
                (lastPadStates[(int)player.Value].IsButtonUp(button)))
                {
                    return true;
                }
                else
                    return false;
            }
            else
            {
                if ((IsNewButtonPress(button, PlayerIndex.One, out pIndex))
                || (IsNewButtonPress(button, PlayerIndex.Two, out pIndex))
                || (IsNewButtonPress(button, PlayerIndex.Three, out pIndex))
                || (IsNewButtonPress(button, PlayerIndex.Four, out pIndex)))
                {
                    return true;
                }
                return false;
            }
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

        public bool IsButtonDown(Buttons button, PlayerIndex? player, out PlayerIndex pIndex)
        {
            if (player.HasValue)
            {
                pIndex = player.Value;

                if (currentPadStates[(int)pIndex].IsButtonDown(button))
                {
                    return true;
                }
                else
                    return false;
            }
            else
            {
                if ((IsButtonDown(button, PlayerIndex.One, out pIndex))
                || (IsButtonDown(button, PlayerIndex.Two,  out pIndex))
                || (IsButtonDown(button, PlayerIndex.Three, out pIndex))
                || (IsButtonDown(button, PlayerIndex.Four, out pIndex)))
                {
                    return true;
                }
                return false;
            }
        }

        public int PlayerJoined(PlayerIndex index)
        {
            currentPlayers++;
            if (currentPlayers >= MAXINPUTS)
                currentPlayers = MAXINPUTS;
            isActive[(int)index] = true;
            return currentPlayers;
        }

        public int PlayerLeft(PlayerIndex index)
        {
            currentPlayers--;
            if (currentPlayers <= 0)
                currentPlayers = 0;
            isActive[(int)index] = false;
            return currentPlayers;
        }

        #endregion

        #endregion
    }
}