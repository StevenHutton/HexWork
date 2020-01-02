using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace HexWork
{
    public class MenuEntry
    {
		#region Attributes

        string name;
        
        public event EventHandler<PlayerIndexEventArgs> selected;

        //Menu entries fade between their selected and deselected appearances
        //this float tracks the amount of fade
        float fade = 0.0f;

		#endregion

		#region Properties
        public string Name
        {
            get { return name; }
            set { name = value; }
        }

        public float Fade
        {
            get { return fade; }
            set { fade = value; }
        }
		#endregion

		#region Methods

		#region Initialisation

        public MenuEntry(string text)
        {
            name = text;
        }

        public MenuEntry(string text, EventHandler<PlayerIndexEventArgs> onSelected)
        {
            name = text;
            selected += onSelected;
        }

		#endregion

		#region Updating

        public void Update(GameTime gameTime, bool isSelected)
        {
            float fadeSpeed = (float)gameTime.ElapsedGameTime.TotalSeconds;

            if (isSelected)
                fade = Math.Min(fade + fadeSpeed, 1);
            else
                fade = Math.Max(fade - fadeSpeed, 0);
        }

		#endregion

        #region Public Methods

        protected internal virtual void OnSelect(PlayerIndex pIndex)
        {
            selected?.Invoke(this, new PlayerIndexEventArgs(pIndex));
        }

        #endregion

        #endregion
    }
}