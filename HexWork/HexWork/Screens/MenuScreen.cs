using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using HexWork.Interfaces;

namespace HexWork.Screens
{
    public class MenuScreen : Screen
    {
        #region Attributes

        protected List<MenuEntry> menu = new List<MenuEntry>();
        protected SpriteFont theFont;
        protected Vector2 position = new Vector2(100, 100);
        protected int selected = 0;

        #endregion

		#region Properties
		#endregion

		#region Methods

		#region Initialisation

        public MenuScreen(IScreenManager _screenManager, PlayerIndex? _controllingPlayer)
            : base(_screenManager, _controllingPlayer)
        {
            fullscreen = false;
        }

        public override void LoadContent(Game game)
        {
            base.LoadContent(game);
            float temp = 0.0f;
            theFont = screenManager.Game.Content.Load<SpriteFont>("MenuFont");
            for (int i = 0; i < menu.Count; i++)
            {
                float width = theFont.MeasureString(menu[i].Name).X;
                if (width > temp)
                    temp = width;
            }
            position.X -= temp / 2;
        }

		#endregion

		#region Updating

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
        }

        public override void HandleInput()
        {
            base.HandleInput();

            //get a pointer to the input manager
            IInputManager inputs = screenManager.InputManager;

            //check for menu up
            if(IsMenuUp(inputs))
            {
                selected--;
                if (selected < 0)
                    selected = menu.Count - 1;
            }

            //check for menu down
            if (IsMenuDown(inputs))
            {
                selected++;
                if (selected >= menu.Count)
                    selected = 0;
            }

            PlayerIndex pIndex;

            //check for select
            if(IsSelect(inputs, out pIndex))
            {
                menu[selected].OnSelect(pIndex);
            }
        }

        private bool IsMenuUp(IInputManager inputs)
        {
            PlayerIndex pIndex;

            if (inputs.IsNewButtonPress(Buttons.DPadUp, controllingPlayer, out pIndex) ||
                inputs.IsNewButtonPress(Buttons.LeftThumbstickUp, controllingPlayer, out pIndex) ||
                inputs.IsNewKeyPress(Keys.Up))
            {
                return true;
            }
            else 
                return false;
        }

        private bool IsMenuDown(IInputManager inputs)
        {
            PlayerIndex pIndex;

            if (inputs.IsNewButtonPress(Buttons.DPadDown, controllingPlayer, out pIndex) ||
                inputs.IsNewButtonPress(Buttons.LeftThumbstickDown, controllingPlayer, out pIndex) ||
                inputs.IsNewKeyPress(Keys.Down))
            {
                return true;
            }
            else
                return false;
        }

        private bool IsSelect(IInputManager inputs, out PlayerIndex pIndex)
        {
            if (inputs.IsNewButtonPress(Buttons.A, controllingPlayer, out pIndex)
                || inputs.IsNewButtonPress(Buttons.Start, controllingPlayer, out pIndex)
                || inputs.IsNewKeyPress(Keys.Enter))
            {
                return true;
            }
            else 
                return false;
        }

		#endregion

		#region Drawing

        public override void Draw(GameTime gameTime)
        {
            base.Draw(gameTime);
            // Get drawing objects from screen manager
            SpriteBatch spriteBatch = screenManager.SpriteBatch;
            //SpriteFont font = screenManager.MenuFont;
            
            //set position to draw from
            Vector2 drawAt = position;
            spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.NonPremultiplied);
            for (int i = 0; i < menu.Count ; i++)
            {
                if (i == selected)
                {
                    spriteBatch.DrawString(theFont, menu[i].Name, drawAt, new Color((byte)255, (byte)0, (byte)0, (byte)screenColour.A));
                    //move down
                    drawAt.Y += theFont.LineSpacing;
                }
                else
                {
                    spriteBatch.DrawString(theFont, menu[i].Name, drawAt, new Color((byte)255, (byte)255, (byte)255, (byte)screenColour.A));
                    //move down
                    drawAt.Y += theFont.LineSpacing;
                }
            }
            spriteBatch.End();
        }

		#endregion

		#endregion
    }
}