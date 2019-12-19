/********************************************************************
	created:	2010/06/07
	created:	7:6:2010   14:47
	filename: 	C:\Users\Scallat\Documents\Visual Studio 2008\Projects\Illustrious Games Libraries\Illustrious Games Libraries\Components\Implimentations\ScreenManager\ScreenManager.cs
	file path:	C:\Users\Scallat\Documents\Visual Studio 2008\Projects\Illustrious Games Libraries\Illustrious Games Libraries\Components\Implimentations\ScreenManager
	file base:	ScreenManager
	file ext:	cs
	author:		Steven Hutton
	
	purpose:	A screen manager class that controls the menu system of the game

	notes:		
*********************************************************************/

using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using HexWork.Interfaces;

namespace HexWork
{
    public class ScreenManager : DrawableGameComponent, IScreenManager
    {
		#region Attributes
        
        /// <summary>
        /// A region containing pointers to the other services used by screen manager
        /// </summary>
        #region Services

        public IInputManager inputManager;

        #endregion

        //A list of all the screens that currently exist
        List<IScreen> screens = new List<IScreen>();

        /// <summary>
        /// A list that contains all of the screens to update. We populate this list with the contents
        /// of the screens list (above) at the beginning of each update function so that we can avoid the 
        /// issue of dealing with screens created during the update loop.
        /// </summary>
        List<IScreen> screensToUpdate = new List<IScreen>();

        //A sprite font to hold the font used by all of the games menu screens
        SpriteFont menuFont;
       
        //A sprite batch so that each game screen doesn't waste resources creating it's own instance.
        SpriteBatch spriteBatch;

        //Pretty self explanatory bool
        private bool isInitialised;
        
		#endregion

		#region Properties

        public SpriteFont MenuFont
        {
            get { return menuFont; }
            set { menuFont = value; }
        }

        public SpriteBatch SpriteBatch
        {
            get { return spriteBatch; }
        }

        public IInputManager InputManager
        {
            get { return inputManager; }
        }

		#endregion

		#region Methods

		#region Initialisation

        public ScreenManager(Game game)
            : base(game)
        {
            game.Services.AddService(typeof(IScreenManager), this);
        }

        public override void Initialize()
        {
            base.Initialize();
            isInitialised = true;
        }

        protected override void LoadContent()
        {
            //get a pointer to the input manager service
            //we do this in load content to ensure the game has had time to create the service
            inputManager = (IInputManager)Game.Services.GetService(typeof(IInputManager));

            //Create the sprite batch
            spriteBatch = new SpriteBatch(GraphicsDevice);

            //Load the menu font
            menuFont = Game.Content.Load<SpriteFont>("menufont");

            foreach(IScreen screen in screens)
            {
                screen.LoadContent(Game);
            }
            base.LoadContent();
        }

		#endregion

		#region Updating

        public override void Update(GameTime _gameTime)
        {
            base.Update(_gameTime);

            //Clear the screensToUpdate list
            screensToUpdate.Clear();

            /*Copy the screens list into screensToUpdate so that we don't have to worry about any screens created during
             this update loop*/
            foreach (IScreen screen in screens)
                screensToUpdate.Add(screen);

            bool activeScreenFound = false;
            bool screenCovered = false;

            //loop through all screens
            for (int i = screensToUpdate.Count - 1; i >= 0; i--)
            {
                screensToUpdate[i].Update(_gameTime);

                //check if this screen is active or becoming active
                if (screensToUpdate[i].State == ScreenState.Active ||
                    screensToUpdate[i].State == ScreenState.Activating)
                {
                    //if this is the first active screen we've found
                    if (!activeScreenFound)
                    {
                        //the make a note of that and let it handle input
                        activeScreenFound = true;

                        //Update the screen
                        screens[i].HandleInput();
                    }

                    if (screensToUpdate[i].FullScreen)
                    {
                        screenCovered = true;
                    }
                }
                else if (screenCovered && (screensToUpdate[i].State == ScreenState.Inactive ))
                {
                    //if this screen is inactive and is fully covered by another screen then we don't need it                    
                    screens.Remove(screens[i]);
                }
            }

	        if (!activeScreenFound)
	        {
		        screensToUpdate.Last().State = ScreenState.Activating;
	        }
        }
		#endregion

		#region Drawing

        public override void Draw(GameTime _gameTime)
        {
            foreach (IScreen screen in screens)
            {
                screen.Draw(_gameTime);
            }
        }

		#endregion

        #region Public Methods

        public void AddScreen(IScreen screen)
        {
            screen.State = ScreenState.Activating; 

            if (isInitialised)
                screen.LoadContent(Game);

            if (!screens.Contains(screen))
                screens.Add(screen);
        }

        public void RemoveScreen(IScreen screen)
        {
            if (isInitialised)
            {
                screen.Exit();
                screen.UnloadContent();
            }

            if (screens.Contains(screen))
                screens.Remove(screen);
        }
        #endregion

        #endregion
    }
}