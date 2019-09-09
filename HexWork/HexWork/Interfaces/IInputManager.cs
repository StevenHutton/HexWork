using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace HexWork.Interfaces
{
    public interface IInputManager: IGameComponent, IUpdateable, IDrawable
    {
        bool IsNewButtonPress(Buttons button, PlayerIndex? player, out PlayerIndex pIndex);
        bool IsButtonDown(Buttons button, PlayerIndex? player, out PlayerIndex pIndex);
        bool IsNewKeyPress(Keys key);
        int PlayerJoined(PlayerIndex _pIndex);
        int PlayerLeft(PlayerIndex _pIndex);
    }
}
