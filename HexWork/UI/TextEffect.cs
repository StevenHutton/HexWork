using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace HexWork.UI
{
    public enum TextAlignment
    {
        Left,
        Right,
        Center
    };

    public class TextEffect
    {
        #region Attributes

        private string _text;

        private TextAlignment _alignment = TextAlignment.Center;

        public Color ColorToDraw = Color.Red;

        private Vector2 _position;

        private Vector2 _scale = Vector2.One;

        private Vector2 _offset;

        private SpriteFont _font;

        //how long to display this effect in seconds.
        public float Duration = 0.5f;

        //how long the effect has been displayed in seconds
        private float _timer = 0.0f;

        //bool indicating that this effect has finished.
        public bool IsComplete = false;

        public Color BaseColour = Color.Red;

        #endregion

        #region Properties

        public string Text => _text;

        public Vector2 Position
        {
            get => _position;
            set => _position = value;
        }

        public Vector2 Scale
        {
            get => _scale;
            set => _scale = value;
        }

        public SpriteFont Font => _font;

        public Vector2 Offset => _offset;

        #endregion

        #region Methods
        
        public TextEffect(string text, SpriteFont font, TextAlignment alignment = TextAlignment.Center)
        {
            _font = font;
            _alignment = alignment;
            SetText(text);
        }

        public virtual void Update(GameTime gameTime)
        {
            //get elapsed time in seconds
            var dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

            //update and finish.
            _timer += dt;

            _position.Y -= 0.5f;

            var x = Duration * 5 / 6;

            if (_timer <= x)
            {
                ColorToDraw = BaseColour;
            }
            else
                ColorToDraw = Color.Lerp(BaseColour, Color.TransparentBlack, MathHelper.Min(1.0f, (_timer - x) / (Duration - x)));
            
            if (_timer >= Duration)
            {
                IsComplete = true;
            }
        }

        public void SetText(string text)
        {
            _text = text;

            Vector2 stringDimensions = _font.MeasureString(_text);

            switch (_alignment)
            {
                case TextAlignment.Left:
                    _offset = new Vector2(0.0f, stringDimensions.Y) / 2;
                    break;
                case TextAlignment.Right:
                    _offset = new Vector2(stringDimensions.X, stringDimensions.Y / 2);
                    break;
                case TextAlignment.Center:
                    _offset = (stringDimensions) / 2;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        #endregion
    }
}
