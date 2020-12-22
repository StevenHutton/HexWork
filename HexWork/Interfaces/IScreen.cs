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
        ScreenState State { get; set; }
        bool FullScreen { get; }
        void LoadContent(Game game);
        void UnloadContent();
        void HandleInput();        
        Vector2 GetPointOnScreen(Vector2 _point);
        Vector2 GetPointOnScreen(float x, float y);
        void Exit();
    }
}
