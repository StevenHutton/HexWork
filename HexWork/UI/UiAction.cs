﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;

namespace HexWork.UI
{
    public class UiAction
    {
        public Action ActionCompleteCallback;

        public UiCharacter Sprite;

        public Animation Animation;

        public TextEffect Effect;

        public void Start()
        {
            if (Sprite != null && Effect != null)
                Effect.Position = Sprite.Position;
        }

        public void Update(GameTime gameTime)
        {
            if (Sprite != null)
            {
                Animation?.Update(gameTime, Sprite);
            }

            Effect?.Update(gameTime);
        }

        public bool IsComplete()
        {
            if (Sprite == null && Animation == null && Effect == null)
                return true;

            if (Sprite != null && Animation != null)
            {
                return Animation.IsComplete;
            }

            if (Effect != null)
            {
                return Effect.IsComplete;
            }

            return false;
        }
    }
}