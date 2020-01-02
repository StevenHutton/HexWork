using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;

namespace HexWork.UI
{
    public class Animation
    {
        protected float _progress = 0.0f;

        protected float _speed = 8.0f;

        public bool IsComplete = false;

        public virtual void Update(GameTime gameTime, UiCharacter sprite)
        {
            //just end immediately
            sprite.Position = Vector2.Zero;
            IsComplete = true;
        }
    }

    public class MovementAnimation : Animation
    {
        private Vector2 _destination;
        private Vector2 _start = Vector2.Zero;

        public MovementAnimation(Vector2 destination)
        {
            _destination = destination;
        }

        public override void Update(GameTime gameTime, UiCharacter sprite)
        {
            var dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            if (_start == Vector2.Zero)
                _start = sprite.Position;
            
            _progress += dt * _speed;

            Vector2 pos;
            Vector2.Lerp(ref _start, ref _destination, _progress, out pos);
            sprite.Position = pos;

            if (_progress < 1.0f) return;
            
            sprite.Animation = null;
            IsComplete = true;
            sprite.Position = _destination;
        }
    }
}
