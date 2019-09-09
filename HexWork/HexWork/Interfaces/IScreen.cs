using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace HexWork.Interfaces
{
    public enum ScreenState
    {
        Active,
        Inactive,
        Activating,
        Deactivating,
        Exiting,
    }

    public interface IScreen: IDrawable, IUpdateable
    {
        #region Properties

        ScreenState State { get; set; }

        bool FullScreen { get; }

        PlayerIndex? ControllingPlayer { get; set; }

        #endregion

        #region Methods

        void LoadContent(Game game);

        void UnloadContent();

        void HandleInput();
        
        Vector2 GetPointOnScreen(Vector2 _point);

        Vector2 GetPointOnScreen(float x, float y);

        void Exit();

        #endregion
    }
}
