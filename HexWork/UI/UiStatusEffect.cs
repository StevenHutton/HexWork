using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace HexWork.UI
{
	public class UiStatusEffect
	{
		public Texture2D Texture { get; set; }
        
		public Vector2 Scale { get; set; } = new Vector2(0.175f);

		public Vector2 Origin { get; set; } = new Vector2(0.0f);

		public Color Color { get; set; } = Color.Red;

		public Guid Id { get; set; }

		public UiStatusEffect()
		{

		}

		public UiStatusEffect(Texture2D texture, Guid id)
		{
			Texture = texture;

			Id = id;

			var width = (float)Texture.Width;
			var height = (float)Texture.Height;

			Origin = new Vector2(width/2, height-(width/2));
		}
	}
}