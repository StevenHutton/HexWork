using System;
using HexWork.UI.Interfaces;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace HexWork.UI
{
    public delegate bool IsEnabledDelegate();
    
    public class UiButton
    {
        private string _text = "";

	    private const int MinWidth = 180;

        public Rectangle Rect = new Rectangle();

        private IsEnabledDelegate _isEnabled = null;

        private Action<IInputProvider> _onClick;

        #region Properties

        public bool IsHover { get; set; } = false;

        public bool IsMouseDown { get; set; } = false;

        public string Text
        {
            get => _text;
        }

        public SpriteFont Font { get; set; }

        public Vector2 TextSize => Font.MeasureString(Text);

        //Position of the button in screen space
        //(indicates the position of the center of the button)
        public Vector2 Position => new Vector2(Rect.X + Rect.Width/2, Rect.Y + Rect.Height/2);

        public bool IsEnabled => _isEnabled?.Invoke() ?? true;

        public Vector2 Padding { get; set; } = new Vector2(5.0f, 5.0f);

        #endregion
        
        public UiButton(string text, SpriteFont font, Action<IInputProvider> onClickAction, Func<bool> isEnabledHandler)
        {
            Font = font;
            SetText(text);
            _onClick += onClickAction;
            if (isEnabledHandler != null)
                _isEnabled = new IsEnabledDelegate(isEnabledHandler);
        }

        public void Click(IInputProvider source)
        {
            _onClick?.Invoke(source);
            IsMouseDown = false;
        }

        #region Setter Methods

        public void SetText(string text)
        {
            _text = text;

            var size = Font.MeasureString(Text);
            
            var width = size.X + (Padding.X * 2);
	        if (width <= MinWidth)
		        width = MinWidth;
            var height = size.Y + (Padding.Y * 2);

            Rect = new Rectangle(0, 0, (int)width, (int)height);
        }

        public void SetPosition(Vector2 position)
        {
            Rect.X = (int)position.X;
            Rect.Y = (int)position.Y;
        }

        #endregion
    }
}
