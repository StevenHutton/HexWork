using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace HexWork.Interfaces
{
    public interface IInputManager: IGameComponent, IUpdateable, IDrawable
    {
        bool IsNewButtonPress(Buttons button);
        bool IsButtonDown(Buttons button);
        bool IsNewKeyPress(Keys key);
    }
}
