using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace HexWork.UI
{
    public class UiCharacter
    {
        #region Attributes

        //The target health for transitions expressed as a percentage
        private float _targetHealth;

        //Speed at which the health bar fills measured in percentage/second
        private float _healthBarSpeed = 100.0f;

        #endregion

        #region Properties
		public Texture2D Texture { get; set; }
        public Texture2D PortraitTexture { get; set; }
        public Color Colour { get; set; } = Color.White;

        public Vector2 Position { get; set; }

        public Vector2 Scale { get; set; } = new Vector2(1f, 1f);

        public Animation Animation { get; set; } = null;

        public float MaxHealth { get; set; }

        public float Health { get; set; }

		public List<UiStatusEffect> StatusEffects { get; } = new List<UiStatusEffect>();

        /// <summary>
        /// Health expressed as a percentage.
        /// </summary>
        public float PercentageHealth => (Health / MaxHealth) * 100.0f;

        //
        public float HealthScale => PercentageHealth / 100.0f;

        #endregion

        #region Methods

        public UiCharacter(Texture2D tex, Texture2D pTex, Vector2 pos, float maxHealth)
        {
            Texture = tex;
            PortraitTexture = pTex;
            Position = pos;
            Health = maxHealth;
            MaxHealth = maxHealth;
            _targetHealth = maxHealth;
        }

        public void Update(GameTime gameTime)
        {
            float dt = (float) gameTime.ElapsedGameTime.TotalSeconds;
            Animation?.Update(gameTime, this);

            if (Health < _targetHealth)
            {
                Health += _healthBarSpeed * dt;
                if (Health >= _targetHealth)
                {
                    Health = _targetHealth;
                }
            }
            else if (Health > _targetHealth)
            {
                Health -= _healthBarSpeed * dt;
                if (Health <= _targetHealth)
                {
                    Health = _targetHealth;
                }
            }
        }

        public void TakeDamage(int damage)
        {
            if (_targetHealth - damage > MaxHealth)
                return;

            _targetHealth -= damage;
        }

        #endregion

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
