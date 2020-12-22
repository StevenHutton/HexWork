/********************************************************************
	created:	2010/06/07
	created:	7:6:2010   14:40
	filename: 	C:\Users\Scallat\Documents\Visual Studio 2008\Projects\Illustrious Games Libraries\Illustrious Games Libraries\Components\Implimentations\ScreenManager\Screen.cs
	file path:	C:\Users\Scallat\Documents\Visual Studio 2008\Projects\Illustrious Games Libraries\Illustrious Games Libraries\Components\Implimentations\ScreenManager
	file base:	Screen
	file ext:	cs
	author:		Steven Hutton
	
	purpose:	A screen object

	notes:		This is a generic screen object it works but doesn't do very much.
 *              Inherit from this class to make new screens for your game.
*********************************************************************/

using System;
using Microsoft.Xna.Framework;
using HexWork.Interfaces;

namespace HexWork.Screens
{
    public class Screen : IScreen
    {
		#region Attributes
        protected ScreenState state = ScreenState.Inactive;
        protected IScreenManager screenManager;
        protected bool fullscreen = false;

        protected Vector2 screenSize;
        protected Rectangle safeArea;

        protected TimeSpan _activeTransitionTime = new TimeSpan(0, 0, 0, 0, 500);
        protected TimeSpan _deactiveTransitionTime = new TimeSpan(0, 0, 0, 0, 500);
        protected TimeSpan transitioningTime = TimeSpan.Zero;

        protected Color screenColour = Color.White;

        #endregion

        #region Properties

        public int DrawOrder { get; }
        public bool Visible { get; }
        public event EventHandler<EventArgs> DrawOrderChanged;
        public event EventHandler<EventArgs> VisibleChanged;

        public ScreenState State
        {
            get { return state; }
            set 
            { 
                state = value;
                transitioningTime = TimeSpan.Zero;
            }
        }

        public bool FullScreen
        {
            get { return fullscreen; }            
        }

		#endregion

		#region Methods

        #region Initialisation

        public Screen(IScreenManager _screenManager)
        {
            this.screenManager = _screenManager;
            screenSize = new Vector2(screenManager.GraphicsDevice.Viewport.Width,
            screenManager.GraphicsDevice.Viewport.Height);
            safeArea = screenManager.GraphicsDevice.Viewport.TitleSafeArea;
        }

        public virtual void LoadContent(Game game)
        {
        
        }

        public virtual void UnloadContent()
        {

        }

        #endregion

		#region Updating

        public virtual void Update(GameTime gameTime)
        {
            switch(state)
            {
                case ScreenState.Inactive:
                {
                    break;
                }
                case ScreenState.Activating:
                {
                    transitioningTime += gameTime.ElapsedGameTime;
                    if (transitioningTime > _activeTransitionTime)
                    {
                        State = ScreenState.Active;
                    }
                    break;
                }
                case ScreenState.Active:
                {
                    break;
                }
                case ScreenState.Deactivating:
                {
                    transitioningTime += gameTime.ElapsedGameTime;
                    if (transitioningTime > _deactiveTransitionTime)
                    {
                        State = ScreenState.Inactive;
                    }
                    break;
                }
                case ScreenState.Exiting:
                {
                    screenManager.RemoveScreen(this);
                    break;
                }
            }
        }

        public bool Enabled { get; }
        public int UpdateOrder { get; }
        public event EventHandler<EventArgs> EnabledChanged;
        public event EventHandler<EventArgs> UpdateOrderChanged;

        public virtual void HandleInput()
        { }

        #endregion

        #region Drawing

        public virtual void Draw(GameTime gameTime)
        {
            switch (state)
            {
                case ScreenState.Inactive:
                    {
                        screenColour.A = 0;
                        break;
                    }
                case ScreenState.Activating:
                    {
                        float transitionProgress = (float)(transitioningTime.TotalMilliseconds / _activeTransitionTime.TotalMilliseconds);
                        screenColour.A = (byte)(transitionProgress * 255);
                        break;
                    }
                case ScreenState.Active:
                    {
                        screenColour.A = 255;
                        break;
                    }
                case ScreenState.Deactivating:
                    {
                        float transitionProgress = (float)(transitioningTime.TotalMilliseconds / _deactiveTransitionTime.TotalMilliseconds);
                        screenColour.A = (byte)(255 - (transitionProgress * 255));
                        break;
                    }
                case ScreenState.Exiting:
                    {
                        screenColour.A = 0;
                        break;
                    }
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// This method takes a vector with values between 0.0 and 1.0 and returns
        /// a point in the tile safe area of the screen.
        /// </summary>
        /// <param name="_point"></param>
        /// <returns></returns>
        public Vector2 GetPointOnScreen(Vector2 _point)
        {
            //clamp the vector to the required range
            if(_point.X > 1.0f)
                _point.X = 1.0f;
            if(_point.X < 0.0f)
                _point.X = 0.0f;
            if(_point.Y > 1.0f)
                _point.Y = 1.0f;
            if(_point.Y < 0.0f)
                _point.Y = 0.0f;

            float x = (float)safeArea.Width;
            float y = (float)safeArea.Height;
            x *= _point.X;
            y *= _point.Y;

            return new Vector2(safeArea.Location.X + x, safeArea.Location.Y + y);  
        }

        /// <summary>
        /// This method takes two floats with values between 0.0f and 1.0 and 
        /// returns a point in the title safe area of the screen
        /// </summary>
        /// <param name="_x"></param>
        /// <param name="_y"></param>
        /// <returns></returns>
        public Vector2 GetPointOnScreen(float _x, float _y)
        {
            //clamp the vector to the required range
            if(_x > 1.0f)
                _x = 1.0f;
            if(_x < 0.0f)
                _x = 0.0f;
            if(_y > 1.0f)
                _y = 1.0f;
            if(_y < 0.0f)
                _y = 0.0f;

            _x *= (float)safeArea.Width;
            _y *= (float)safeArea.Height;

            return new Vector2(_x + safeArea.Location.X, _y + safeArea.Location.Y);
        }

        public virtual void Exit()
        {
            state = ScreenState.Exiting;
        }

        #endregion

        #region Public Methods

        #endregion

        #endregion
    }
}