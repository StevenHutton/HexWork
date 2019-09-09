using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace HexWork.UI
{
    public class UiEffect
    {
        public string Text;

        public Color Color;

        public Vector2 Position;

        public Vector2 Scale;

        public int FontSize;

        //how long to display this effect in seconds.
        public float Duration = 2.0f;

        //how long the effect has been displayed in seconds
        private float _timer = 0.0f;

        //bool indicating that this effect has finished.
        public bool IsComplete = false;

        //image to display
        public Texture2D Texture;

        #region Methods

        public UiEffect()
        {

        }

        public virtual void Update(GameTime gameTime)
        {
            //get elapsed time in seconds
            var dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

            //update and finish.
            _timer += dt;
            if (_timer >= Duration)
            {
                IsComplete = true;
            }
        }

        #endregion
    }
}
