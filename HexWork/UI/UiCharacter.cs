using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace HexWork.UI
{
    public class UiCharacter : UiGameObject
    {
        #region Methods

        public UiCharacter(Texture2D tex, Texture2D pTex, Vector2 pos, float maxHealth)
        {
            Texture = tex;
            PortraitTexture = pTex;
            Position = pos;
            Health = maxHealth;
            MaxHealth = maxHealth;
            TargetHealth = maxHealth;
        }

        public override void Update(float dt)
        {
            Animation?.Update(dt, this);

            if (Health < TargetHealth)
            {
                Health += HealthBarSpeed * dt;
                if (Health >= TargetHealth)
                {
                    Health = TargetHealth;
                }
            }
            else if (Health > TargetHealth)
            {
                Health -= HealthBarSpeed * dt;
                if (Health <= TargetHealth)
                {
                    Health = TargetHealth;
                }
            }
        }
        
        #endregion
    }
}
