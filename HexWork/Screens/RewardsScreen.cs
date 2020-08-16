using System;
using System.Collections.Generic;
using System.Linq;
using HexWork.Gameplay;
using HexWork.Interfaces;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace HexWork.Screens
{
    class CharacterButton
    {
        public Guid CharacterId;

        public Rectangle Rect;

        public Texture2D Texture;

        public Color Color = new Color(new Vector3(0.20f, 0.20f, 0.20f));

        public bool HasReward = false;

        public CharacterButton(Texture2D tex)
        {
            Texture = tex;
            Rect = new Rectangle(0,0, tex.Width, tex.Height);
        }
    }

    public class RewardsScreen : Screen
    {
        private HexWork _hexGame;
        private BoardState _gameState;

        private List<CharacterButton> _characterPortraits = new List<CharacterButton>();

        private Texture2D _starTexture;

        private SpriteBatch _spriteBatch;

        private int rewardCount;

        private Random _rand;

        #region Input Attributes

        private MouseState _mouseState;
        private MouseState _previousMouseState;

        #endregion 

        public RewardsScreen(IScreenManager _screenManager, int numRewards = 3) : base(_screenManager)
        {
            _hexGame = (HexWork)_screenManager.Game;
            //_gameState = 

            _rand = new Random(DateTime.Now.Millisecond);
            rewardCount = numRewards;
        }

        public RewardsScreen(IScreenManager _screenManager, PlayerIndex? _controllingPlayer, int numRewards = 1) : base(_screenManager, _controllingPlayer)
        {
            _hexGame = (HexWork)_screenManager.Game;
            rewardCount = numRewards;
            _rand = new Random(DateTime.Now.Millisecond);
        }

        public override void LoadContent(Game game)
        {
            _spriteBatch = new SpriteBatch(game.GraphicsDevice);
            _starTexture = game.Content.Load<Texture2D>("star");

            foreach (var hero in _gameState.Heroes)
            {
                var charButton = new CharacterButton(game.Content.Load<Texture2D>($"{hero.Name}Portrait"));
                charButton.CharacterId = hero.Id;
                _characterPortraits.Add(charButton);
            }

            while (rewardCount > 0)
            {
                var rewardHavers = _characterPortraits.Where(cp => !cp.HasReward).ToList();
                var index = _rand.Next(0, rewardHavers.Count());

                rewardHavers.ElementAt(index).HasReward = true;
                rewardHavers.ElementAt(index).Color = Color.White;

                rewardCount--;
            }

            //set portrait positions
            var portraitMargin = 10;
            var portraitWidth = _characterPortraits.Max(data => data.Rect.Width) + portraitMargin * 2;
            var portraitHeight = (float)safeArea.Height / 3;
            var numPortraits = _characterPortraits.Count;

            var posX = safeArea.Center.X - ((float)numPortraits / 2 * portraitWidth);
            
            foreach (var button in _characterPortraits)
            {
                button.Rect.X = (int)posX;
                button.Rect.Y = (int)portraitHeight;
                posX += portraitWidth;
            }

            base.LoadContent(game);
        }

        public override void Draw(GameTime gameTime)
        {
            _spriteBatch.Begin();

            foreach (var button in _characterPortraits)
            {
                _spriteBatch.Draw(button.Texture, button.Rect, button.Color);

                if (button.HasReward)
                {
                    var position = new Vector2(button.Rect.X + button.Rect.Width/2 - _starTexture.Width/2, button.Rect.Y - 10 - _starTexture.Height);
                    _spriteBatch.Draw(_starTexture, position);
                }
            }

            _spriteBatch.End();
            
            base.Draw(gameTime);
        }

        public override void HandleInput()
        {
            if (!_characterPortraits.Any(p => p.HasReward))
            {
                screenManager.AddScreen(new BattleSelectionScreen(screenManager));
                Exit();
            }

            _previousMouseState = _mouseState;
            _mouseState = Mouse.GetState();

            if (_mouseState.LeftButton != ButtonState.Released ||
                _previousMouseState.LeftButton != ButtonState.Pressed) return;

            foreach (var button in _characterPortraits.Where(b => b.HasReward))
            {
                if (!button.Rect.Contains(_mouseState.Position)) continue;

                this.screenManager.AddScreen(new LevelUpScreen(this.screenManager, button.CharacterId));
                button.HasReward = false;
                button.Color = new Color(new Vector3(0.20f, 0.20f, 0.20f));
            }
        }

    }
}
