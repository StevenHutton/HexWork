using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;

namespace HexWork.Interfaces
{
    public interface IScreenManager: IGameComponent, IUpdateable, IDrawable
    {
        #region Properties

        //A sprite font held by the screen manager for all the menu screens
        SpriteFont MenuFont
        {
            get;
        }

        //The screen manager creates it's own sprite batch to share with the screens
        SpriteBatch SpriteBatch
        {
            get;
        }

        Game Game
        {
            get;
        }

        IInputManager InputManager
        {
            get;
        }

        GraphicsDevice GraphicsDevice
        {
            get;
        }

        #endregion

        #region Methods

        void AddScreen(IScreen screen);
        void RemoveScreen(IScreen screen);
        #endregion
    }
}
