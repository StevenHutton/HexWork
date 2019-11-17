using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace HexWork.UI
{
    public class UiTileEffect
    {
        public Guid Id;

        public Rectangle SourceRectangle;

        public Texture2D Texture;

        public Vector2 Position;

        public Vector2 Scale;

        public Vector2 Origin;

        private int _frameCount = 15;
        private int _currentFrame = 0;

        private int _firstFrameOffsetX = 16;
        private int _firstFrameOffsetY = 0;
        private int _frameStepX = 96;
        private int _frameStepY = 0;

        //in frames per second
        private float _frameRate = 10.0f;
        private float FrameLength => 1 / _frameRate;

        private float _timer = 0.0f;

        public UiTileEffect(Guid id, Texture2D tex)
        {
            Id = id;
            Texture = tex;
            SourceRectangle = new Rectangle(16, 0, 64, 64);
            Origin = new Vector2((float)Texture.Width / 2, (float)Texture.Height / 2);
        }

        public void Update(float dt)
        {
            _timer += dt;

            if (_timer >= FrameLength)
            {
                _timer -= FrameLength;

                _currentFrame++;
                if (_currentFrame >= _frameCount)
                    _currentFrame = 0;

                SourceRectangle.X = _firstFrameOffsetX + (_currentFrame * _frameStepX);
                SourceRectangle.Y = _firstFrameOffsetY + (_currentFrame * _frameStepY);
            }
        }
    }
}
