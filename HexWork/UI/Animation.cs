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
        public List<Vector2> MovementPath;

        public override void Update(GameTime gameTime, UiCharacter sprite)
        {
            var dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

            if (MovementPath != null)
            {
                if (MovementPath.Count < 2)
                {
                    IsComplete = true;
                    return;
                }

                _progress += dt * _speed;

                Vector2 pos;
                Vector2 start = MovementPath[0];
                Vector2 end = MovementPath[1];
                Vector2.Lerp(ref start, ref end, _progress, out pos);
                sprite.Position = pos;

                if (_progress < 1.0f) return;

                _progress -= _progress;
                MovementPath.RemoveAt(0);

                if (MovementPath.Count >= 2) return;

                sprite.Animation = null;
                IsComplete = true;
            }
        }
    }
}
