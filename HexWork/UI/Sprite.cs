using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace HexWork.UI
{
    public class Sprite
    {
        public Texture2D Texture;

        public Vector2 Offset;

        public Vector2 Position;

        public Color Color = Color.White;

        public float Rotation = 0.0f;

        public Vector2 Scale = Vector2.One;

        public int Width => Texture.Width;

        public int Height => Texture.Height;

        public Sprite(Texture2D tex)
        {
            Texture = tex;
            Offset = new Vector2((float)Width/2, (float)Height /2);
        }

        /// <summary>
        /// Will shit out if spriteBatch.Begin hasn't been called.
        /// </summary>
        public void Draw(SpriteBatch spriteBatch)
        {
            spriteBatch.Draw(Texture, Position, null, Color, Rotation, Offset, Scale, SpriteEffects.None, 0.0f);
        }
    }
}
