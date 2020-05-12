using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace HexWork.UI
{
    public class UiGameObject
    {
        public Guid Id; 
        public Rectangle SourceRectangle;
        public Texture2D Texture;
        public Vector2 Position;
        public Vector2 Scale = new Vector2(1f, 1f);
        public Vector2 Origin;
        public Color Colour = Color.White;
        public Texture2D PortraitTexture;
        public Animation Animation = null;

        //The target health for transitions expressed as a percentage
        public float TargetHealth = 100;

        //Speed at which the healthbar fills measured in percentage/second
        public float HealthBarSpeed = 100.0f;
        public float MaxHealth = 100.0f;

        /// <summary>
        /// Health expressed as a percentage.
        /// </summary>
        public float PercentageHealth => (Health / MaxHealth) * 100.0f;

        //
        public float HealthScale => PercentageHealth / 100.0f;

        public float Health = 0.0f;

        public List<UiStatusEffect> StatusEffects = new List<UiStatusEffect>();

        public UiGameObject(float maxHealth)
        {
            Health = maxHealth;
            MaxHealth = maxHealth;
            TargetHealth = maxHealth;
        }

        public virtual void Update(float dt)
        {

        }

        public void TakeDamage(int damage)
        {
            if (TargetHealth - damage > MaxHealth)
                return;

            TargetHealth -= damage;
        }
        
        public void ApplyStatus(UiStatusEffect uiSprite)
        {
            StatusEffects.Add(uiSprite);
        }

        public void RemoveStatus(Guid statusEffectId)
        {
            UiStatusEffect effect;

            if ((effect = StatusEffects.FirstOrDefault(data => data.Id == statusEffectId)) != null)
                StatusEffects.Remove(effect);
        }
    }
}