using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HexWork.Gameplay;
using HexWork.Gameplay.Actions;
using HexWork.UI.Interfaces;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGameTestProject.Gameplay;

namespace HexWork.UI
{
    /// <summary>
    /// HUD class that handles user interaction
    /// </summary>
    public class UserInterface : IInputProvider
    {
        #region Attributes

        #region Consts

        /// <summary>
        /// Used for conversions from HexSpace to Screen Space.
        /// </summary>
        private readonly float _sqrt3 = (float)Math.Sqrt(3.0);
        private const int ScreenEdgeMargin = 10;

        #endregion
        
        private readonly List<UiButton> _actionBarButtons = new List<UiButton>();
        private Dictionary<Guid, UiCharacter> _uiCharacterDictionary = new Dictionary<Guid, UiCharacter>();
        private readonly List<InitiativeTrackButton> _initiativeTrack = new List<InitiativeTrackButton>();
        private List<UiAction> _uiActions = new List<UiAction>();
	    private PreviewGameStateProxy _gameStateProxy;

        private Character _selectedCharacter;
        private HexCoordinate _focusedTile = null;
        private HexCoordinate _cursorPosition;

        public event EventHandler<HexCoordinate> OnClickEvent;

		#region Rendering Attributes

		private readonly Color _cursorColor = Color.Red;
        private SpriteFont _buttonFont;
        private SpriteFont _uiFont;
        private SpriteFont _damageFont;
        private SpriteFont _effectFont;

        private Texture2D _buttonTexture;
        private Texture2D _blankTexture;
        
        private Texture2D _monsterTexture;
        private Texture2D _monsterTexture_ZombieKing;
        private Texture2D _monsterPortraitTexture;
        private Texture2D _monsterPortraitTexture_ZombieKing;

        private readonly Color _buttonEnabledColor = Color.LightGray;
        private readonly Color _buttonDisabledColor = Color.DarkGray;
        private readonly Color _buttonMouseDownColor = Color.LightSlateGray;
        
        private Texture2D _hexagonTexture;
        private Texture2D _hexagonOutlineTexture;

        //screen dimensions
        private readonly int _screenWidth;
        private readonly int _screenHeight;
        private readonly Vector2 _screenCenter;

        private Effect _pixelOutlineEffect;
        private EffectParameter _outlineWidth;
        private EffectParameter _outlineColour;
        
        //scaling of 
        private readonly float _hexScale;
        private readonly Vector2 _hexScaleV;

        private readonly float _hexScreenScale;

        //half-height of a hex-texture in pixels - todo should be set dynamically.
        private readonly float _hexHalfSize = 128;
        private readonly Vector2 _hexCenter;
		
        //distance between buttons in pixels
        private readonly int _buttonMargin = 2;
        private SpriteBatch _spriteBatch;

        #endregion

        #region Input Handling Attributes

        private KeyboardState _keyBoardState;
        private KeyboardState _previousKeyboardState;

        private MouseState _mouseState;
        private MouseState _previousMouseState;
        
        private readonly HexWork _hexGame;
        
        #endregion

        #endregion

        #region Properties
        
        public HexAction SelectedHexAction { get; set; }
		
		#endregion

        #region Methods

        #region Initialisation Methods

        public UserInterface(HexWork game)
        {
            _hexGame = game;
            
            _screenHeight = game.ScreenHeight;
            _screenWidth = game.ScreenWidth;
            _screenCenter = new Vector2((float)_screenWidth / 2, (float)_screenHeight / 2);

            _hexCenter = new Vector2(_hexHalfSize, _hexHalfSize);
            _hexScreenScale = (float)_screenWidth / 1920;
            _hexScale = 0.4f * _screenWidth / 1920;
            _hexScaleV = new Vector2(_hexScale);

            var gameState = GameStateManager.CurrentGameState;
            
            gameState.CharacterMoveEvent += OnCharacterMove;
            gameState.EndTurnEvent += OnEndTurn;
            gameState.SpawnCharacterEvent += OnCharacterSpawn;
            gameState.CharacterDiedEvent += OnCharacterDied;
            gameState.TakeDamageEvent += OnTakeDamage;
            gameState.ActionEvent += OnActionTrigger;
            gameState.ComboEvent += OnComboTrigger;
            gameState.StatusAppliedEvent += OnStatusEffectApplied;
            gameState.StatusRemovedEvent += OnStatusEffectRemoved;
            gameState.PotentialChangeEvent += OnPotentialChange;
            gameState.MessageEvent += OnMessage;
            gameState.GameOverEvent += OnGameOver;
        }

        public void LoadContent(Game game)
        {
            _monsterTexture = game.Content.Load<Texture2D>("ZombieSprite");
            _monsterTexture_ZombieKing = game.Content.Load<Texture2D>("ZombieKingSprite");
            _monsterPortraitTexture = game.Content.Load<Texture2D>("ZombiePortrait");
            _monsterPortraitTexture_ZombieKing = game.Content.Load<Texture2D>("ZombieKingPortrait");

            _hexagonTexture = game.Content.Load<Texture2D>("hexagon");
            _hexagonOutlineTexture = game.Content.Load<Texture2D>("hexagonOutline");

            LoadBlankTextures(game);
            LoadEffects(game);

            LoadCharacterDictionary(game);

            //load the button font
            _buttonFont = game.Content.Load<SpriteFont>("Nunito");
            _uiFont = game.Content.Load<SpriteFont>("Arial");

            _damageFont = game.Content.Load<SpriteFont>("Nunito-Bold");
            _effectFont = game.Content.Load<SpriteFont>("MenuFont");

            _spriteBatch = new SpriteBatch(game.GraphicsDevice);
	        _gameStateProxy = new PreviewGameStateProxy(_hexGame);
		}

        #region Private Load Content Methods

        private void LoadCharacterDictionary(Game game)
        {
            var gameState = GameStateManager.CurrentGameState;
            foreach (var character in gameState.Characters)
            {
                var tex = character.IsHero ? game.Content.Load<Texture2D>(character.Name) 
                    : character.MonsterType == MonsterType.Zombie ? _monsterTexture : _monsterTexture_ZombieKing;
                var portraitTex = character.IsHero ? game.Content.Load<Texture2D>($"{character.Name}Portrait")
                    : character.MonsterType == MonsterType.Zombie ? _monsterPortraitTexture
                    : _monsterPortraitTexture_ZombieKing;
                var sprite = new UiCharacter(tex, portraitTex, GetHexScreenPosition(character.Position), character.MaxHealth)
                {
                    Scale = new Vector2(_hexScale * 0.9f)
                };

                _uiCharacterDictionary.Add(character.Id, sprite);
            }
        }

        private void LoadBlankTextures(Game game)
        {
            var data = new[] { Color.White, Color.White, Color.White, Color.White };

            //set up the button with a blank texture for now.
            _buttonTexture = new Texture2D(game.GraphicsDevice, 2, 2);
            _buttonTexture.SetData(data);

            _blankTexture = new Texture2D(game.GraphicsDevice, 2, 2);
            _blankTexture.SetData(data);
        }

        private void LoadEffects(Game game)
        {
            _pixelOutlineEffect = game.Content.Load<Effect>("PixelOutlineSpriteEffect");
            _outlineWidth = _pixelOutlineEffect.Parameters["OutlineWidth"];
            _outlineColour = _pixelOutlineEffect.Parameters["OutlineColor"];
            _outlineColour.SetValue(Color.Red.ToVector4());
            
            _outlineWidth.SetValue(3);
        }

        #endregion

        public void Initialise()
        {
            var gameState = GameStateManager.CurrentGameState;
            UpdateInitiative(gameState.GetInitiativeList());
        }

        #endregion

        #region Update Methods

        public void Update(GameTime gameTime)
        {
            HandleInput(gameTime);

            foreach (var character in _uiCharacterDictionary.Values)
            {
                character.Update(gameTime);
            }
            
            var actionToPlay = _uiActions.FirstOrDefault();
            if (actionToPlay != null)
            {
                actionToPlay.Update(gameTime);
                if (actionToPlay.IsComplete())
                {
                    actionToPlay.ActionCompleteCallback?.Invoke();
                    _uiActions.Remove(actionToPlay);
                }
            }
        }

        public void HandleInput(GameTime gameTime)
        {
            //Update keyboard state and mouse state
            _previousMouseState = _mouseState;
            _mouseState = Mouse.GetState();
            _previousKeyboardState = _keyBoardState;
            _keyBoardState = Keyboard.GetState();
            
            if (_previousMouseState.LeftButton == ButtonState.Released
                && _mouseState.LeftButton == ButtonState.Pressed)
            {
                MouseDown(_mouseState.Position);
            }
            else if (_previousMouseState.LeftButton == ButtonState.Pressed
                && _mouseState.LeftButton == ButtonState.Released)
            {
                MouseUp(_mouseState.Position);
            }
            
            if (NewKeyRelease(Keys.Tab))
            {
                SelectedHexAction?.RotateTargeting(_keyBoardState.IsKeyDown(Keys.LeftShift));
            }

            //right click cancels UI state.
            if (_previousMouseState.RightButton == ButtonState.Pressed &&
                _mouseState.RightButton == ButtonState.Released)
            {
                _focusedTile = null;
                OnClickEvent?.Invoke(this, null);
                _actionBarButtons.ForEach(b => b.IsMouseDown = false);
            }

            var position = _mouseState.Position;

            var mouseOffsetX = position.X - (_screenWidth / 2);
            var mouseOffsetY = position.Y - (_screenHeight / 2);

            _cursorPosition = GetHexCoordinate(mouseOffsetX, mouseOffsetY);
        }

        #endregion

        #region Rendering Methods

        public void Draw()
        {
            DrawHud();
            DrawActionBar();
            DrawInitiativeTrack();
            DrawMapUi();
            DrawCharacters();
            DrawUiEffects();

			//Draw Preview
	        if (_cursorPosition != null)
	        {
		        _gameStateProxy.GameState = GameStateManager.CurrentGameState; ;

				_gameStateProxy.SpriteBatchBegin();

		        SelectedHexAction?.TriggerAsync(_selectedCharacter,
			        new DummyInputProvider(_cursorPosition), _gameStateProxy);
					
		        _gameStateProxy.SpriteBatchEnd();
			}
        }
        
        #region Private Rendering Methods

        private void DrawPath(IEnumerable<HexCoordinate> path)
        {
            foreach (var hex in path)
            {
                _spriteBatch.Draw(_blankTexture, GetHexScreenPosition(hex), null,
                    Color.White, 0.0f, new Vector2(1.0f, 1.0f),
                    6.0f, SpriteEffects.None, 0.0f);
            }
        }

        private void DrawHud()
        {
            _spriteBatch.Begin();

            if (_selectedCharacter != null)
            {
                _spriteBatch.DrawString(_uiFont, "Selected Character : " + _selectedCharacter.Name,
                    new Vector2(ScreenEdgeMargin, ScreenEdgeMargin), Color.Black);
            }

	        if (_cursorPosition != null)
	        {
		        _spriteBatch.DrawString(_uiFont, $"{_cursorPosition.X},{_cursorPosition.Y},{_cursorPosition.Z}",
			        new Vector2(ScreenEdgeMargin, ScreenEdgeMargin + 20), Color.Black);
			}


            var gameState = GameStateManager.CurrentGameState;

            if (gameState.ActiveCharacter != null)
            {
                var stringToRender = "Active Character : " + gameState.ActiveCharacter.Name;
                var posX = _spriteBatch.GraphicsDevice.Viewport.TitleSafeArea.Right -
                           _uiFont.MeasureString(stringToRender).X - ScreenEdgeMargin;
                _spriteBatch.DrawString(_uiFont, stringToRender, new Vector2(posX, ScreenEdgeMargin), Color.Black);
            }

	        var potentialString = $"Current Potential : {gameState.Potential}";
	        var posX2 = _spriteBatch.GraphicsDevice.Viewport.TitleSafeArea.Right -
	                   _uiFont.MeasureString(potentialString).X - ScreenEdgeMargin;
	        _spriteBatch.DrawString(_uiFont, potentialString, new Vector2(posX2, ScreenEdgeMargin+25), Color.Black);

			var tabHelpText = "PRESS [TAB] To rotate targeting \n" +
							  "Enemies with status effects are primed (blue)\n" +
                              "Primed enemies can be comboed\n" +
							  "Characters can move and attack once per turn.\n" +
							  "Some moves cost POTENTIAL. \n" +
                              "Potential is shown in red at the top of the screen.\n" +
							  "Defeat the Zom-Boy-King to win. \n" +
                              "If the Majin dies you lose.";
            _spriteBatch.DrawString(_uiFont, tabHelpText, new Vector2(ScreenEdgeMargin, ScreenEdgeMargin + 50), Color.Black);

			var startPositionX = _screenCenter.X - ((float)gameState.MaxPotential / 2 * 65);
            var potentialPosY = 100 * _hexScreenScale;

            var screenPosition = new Vector2(startPositionX, potentialPosY);

            for (int i = 0; i < gameState.MaxPotential; i++)
            {
                var color = gameState.Potential <= i ? Color.LightPink : Color.Red;

                _spriteBatch.Draw(_blankTexture, screenPosition, null, color, 0.0f, Vector2.Zero, new Vector2(30.0f, 2.0f),
                    SpriteEffects.None, 0.0f);

                startPositionX += 65;
                screenPosition.X = startPositionX;
                screenPosition.Y = potentialPosY;
            }

            _spriteBatch.End();
        }

        private void DrawActionBar()
        {
            _spriteBatch.Begin();

            foreach (var button in _actionBarButtons)
            {
                var buttonColor = button.IsEnabled ? //1st conditional
                    (button.IsMouseDown ? _buttonMouseDownColor : _buttonEnabledColor) //1st conditional true => second conditional
                    : _buttonDisabledColor; //1st conditional false

                var textColor = Color.White;

                _spriteBatch.Draw(_buttonTexture, button.Rect, buttonColor);
                _spriteBatch.DrawString(_buttonFont, button.Text, button.Position - button.TextSize / 2, textColor);
            }

            _spriteBatch.End();
        }

        private void DrawInitiativeTrack()
        {
            _spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, null,
                null, null,
                _pixelOutlineEffect);

            _outlineWidth.SetValue(12);

            //draw the initiative track
            foreach (var initEntry in _initiativeTrack)
            {
                var id = initEntry.CharacterId;

                if (!_uiCharacterDictionary.ContainsKey(id))
                    continue;

                var texture = _uiCharacterDictionary[id].PortraitTexture;

                if (id == _selectedCharacter?.Id)
                {
                    _outlineColour.SetValue(Color.Red.ToVector4());

                    _spriteBatch.Draw(texture, initEntry.Rect, null, Color.White, 0.0f,
                        new Vector2(0, 0), SpriteEffects.None, 0.0f);
                
                    _outlineColour.SetValue(Color.TransparentBlack.ToVector4());
                    continue;
                }

                _spriteBatch.Draw(texture, initEntry.Rect, null, Color.White, 0.0f,
                    new Vector2(0, 0), SpriteEffects.None, 0.0f);
            }

            _spriteBatch.End();
        }

        private void DrawMap()
        {
            var gameState = GameStateManager.CurrentGameState;
            _spriteBatch.Begin();
            foreach (var kvp in gameState.Map)
            {
                var coordinate = kvp.Key;
                var tile = kvp.Value;

                var renderPosition = GetHexScreenPosition(coordinate);

                _spriteBatch.Draw(_hexagonTexture, renderPosition, null, tile.Color, 0.0f,
                    _hexCenter, _hexScaleV, SpriteEffects.None, 0.0f);

                var terrainName = tile.TerrainType.ToString();
                _spriteBatch.DrawString(_uiFont, terrainName, renderPosition, Color.White, 0.0f, _uiFont.MeasureString(terrainName) / 2, Vector2.One, SpriteEffects.None, 0.0f);
            }
            _spriteBatch.End();
        }

        private void DrawHighlightedMap(List<HexCoordinate> highlightedTiles)
        {
            var gameState = GameStateManager.CurrentGameState;
            _spriteBatch.Begin();

            foreach (var kvp in gameState.Map)
            {
                var coordinate = kvp.Key;
                var tile = kvp.Value;

                var renderPosition = GetHexScreenPosition(coordinate);

                var color = tile.Color;

                if (!highlightedTiles.Contains(coordinate))
                {
                    color = new Color(color.ToVector3() / 6);
                }

                _spriteBatch.Draw(_hexagonTexture, renderPosition, null, color, 0.0f,
                    _hexCenter, _hexScaleV, SpriteEffects.None, 0.0f);

                var movementCost = tile.MovementCost.ToString();
                _spriteBatch.DrawString(_uiFont, movementCost, renderPosition, Color.White, 0.0f, _uiFont.MeasureString(movementCost) / 2, Vector2.One, SpriteEffects.None, 0.0f);
            }
            
            _spriteBatch.End();
        }

        private void DrawMapUi()
        {
            _spriteBatch.Begin();

            //if we've selected a tile highlight it in purple
            if (_focusedTile != null)
            {
                var highlightRenderPosition = GetHexScreenPosition(_focusedTile);

                var purple = new Color(new Vector3(0.5f, 0.0f, 0.5f));

                _spriteBatch.Draw(_hexagonOutlineTexture, highlightRenderPosition, null,
                    purple, 0.0f,
                    _hexCenter, _hexScaleV, SpriteEffects.None, 0.0f);
            }

            var cursorPosition = _cursorPosition;
            var highlightedCoords = GetHighlightedCoordinates();

            _spriteBatch.End();

            if (highlightedCoords != null)
            {
                DrawHighlightedMap(highlightedCoords);
            }
            else
            {
                DrawMap();
            }

            _spriteBatch.Begin();

            //If the cursor is within the map area the highlight the appropriate tile.
	        if (cursorPosition != null)
	        {
		        var highlightRenderPosition = GetHexScreenPosition(cursorPosition);

		        _spriteBatch.Draw(_hexagonOutlineTexture, highlightRenderPosition, null, _cursorColor, 0.0f,
			        _hexCenter, _hexScaleV, SpriteEffects.None, 0.0f);
					
				var targetPattern = SelectedHexAction?.GetTargetTiles(cursorPosition);
                if (targetPattern != null)
                {
                    foreach (var hex in targetPattern)
                    {
                        var purple = new Color(new Vector3(0.5f, 0.0f, 0.5f));

                        _spriteBatch.Draw(_hexagonOutlineTexture, GetHexScreenPosition(hex), null,
                            purple, 0.0f,
                            _hexCenter, _hexScaleV, SpriteEffects.None, 0.0f);
                    }
                }
            }

            _spriteBatch.End();
        }

        private void DrawCharacters()
        {
            var gameState = GameStateManager.CurrentGameState;
            //draw highlighted elements
            _spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend,
                null, null,
                null, _pixelOutlineEffect);

            _outlineWidth.SetValue(5);
            _outlineColour.SetValue(Color.TransparentBlack.ToVector4());

            //draw characters
            foreach (var kvp in _uiCharacterDictionary)
            {
                var sprite = kvp.Value;
	            var character = gameState.GetCharacter(kvp.Key);
	            var color = Color.White;
				if(character != null)
					color = character.IsPrimed ? Color.Blue: Color.White;

				if (kvp.Key == _selectedCharacter?.Id)
                {
                    _outlineColour.SetValue(Color.Red.ToVector4());

                    _spriteBatch.Draw(sprite.Texture, sprite.Position, null, color, 0.0f,
                        _hexCenter, sprite.Scale, SpriteEffects.None, 0.0f);

                    _outlineColour.SetValue(Color.TransparentBlack.ToVector4());
                }
                else
                {
                    _spriteBatch.Draw(sprite.Texture, sprite.Position, null, color, 0.0f,
                        _hexCenter, sprite.Scale, SpriteEffects.None, 0.0f);
                }
            }

			_spriteBatch.End();

            _spriteBatch.Begin();

            //draw health bars
            foreach (var character in _uiCharacterDictionary.Values)
            {
                var position = character.Position + new Vector2(0.0f, 25.0f);

                _spriteBatch.Draw(_blankTexture, position, null, Color.Green, 0.0f,
                    new Vector2(1.0f, 1.0f), new Vector2(character.HealthScale * 18.0f, 2.0f), SpriteEffects.None, 0.0f);

	            if (character.StatusEffects == null)
		            continue;

                var offset = new Vector2(14.0f, -70.0f);

	            foreach (var statusGroup in character.StatusEffects.GroupBy(se => se.Texture))
                {
                    offset.Y += 25.0f;
                    offset.X = 10.0f;
                    foreach (var status in statusGroup)
                    {
                        offset.X += 12.0f;
                        _spriteBatch.Draw(status.Texture, character.Position + offset,
			            null,
			            Color.White,
			            0.0f,
			            status.Origin, status.Scale, SpriteEffects.None, 0.0f);
                    }
				}
			}

            _spriteBatch.End();
        }

        private void DrawUiEffects()
        {
            var action = _uiActions.FirstOrDefault();
            if (action?.Effect == null) return;
            
            //draw highlighted elements
            _spriteBatch.Begin();

            var effect = action.Effect;
            _spriteBatch.DrawString(effect.Font, effect.Text, effect.Position, 
                effect.ColorToDraw, 0.0f, effect.Offset, effect.Scale, SpriteEffects.None, 0.0f);
            
            _outlineColour.SetValue(Color.TransparentBlack.ToVector4());

            _spriteBatch.End();
        }

        #endregion

        #endregion

        #region Public Methods

        public async Task<HexCoordinate> GetTargetAsync(HexAction action)
        {
            var tcs = new TaskCompletionSource<HexCoordinate>();

            //local function, triggers task completion when called.
            void EventHandler(object s, HexCoordinate e) => tcs.TrySetResult(e);

            HexCoordinate result = null;
            try
            {
                OnClickEvent += EventHandler;
                SelectedHexAction = action;
                result = await tcs.Task;
            }
            finally
            {
                OnClickEvent -= EventHandler;
                SelectedHexAction = null;
            }

            return result;
        }

        #endregion

        #region Private Methods
		
        #region Private Handle Input Methods

        /// <summary>
        /// Handle a click event in the ui
        /// </summary>
        /// <param name="clickPosition">The position of the click in screen space.</param>
        private void MouseUp(Point clickPosition)
        {
            var gameState = GameStateManager.CurrentGameState;
            //get mouse position in hex space
            var mouseOffsetX = clickPosition.X - (_screenWidth / 2);
            var mouseOffsetY = clickPosition.Y - (_screenHeight / 2);
            var cursorPosition = GetHexCoordinate(mouseOffsetX, mouseOffsetY);
            
            //if we clicked on the map
            if (_focusedTile != null 
                && _focusedTile == cursorPosition 
                && OnClickEvent == null)
            {
                var ch = gameState.GetCharacterAtCoordinate(cursorPosition);
                SelectCharacter(ch);
            }

            //If the UI was busy then handle the click.
            OnClickEvent?.Invoke(this, cursorPosition);

            _focusedTile = null;

            //check to see if we clicked a button
            var button =
                _actionBarButtons.FirstOrDefault(b => b.IsEnabled && b.IsMouseDown && b.Rect.Contains(clickPosition));
            _actionBarButtons.ForEach(b => b.IsMouseDown = false);

            if (button != null)
            {
                button.Click(this);
                return;
            }

            //check to see if we clicked a portrait
            var initButton = _initiativeTrack.FirstOrDefault(b => b.Rect.Contains(clickPosition));
            if (initButton != null)
            {
                SelectCharacter(gameState.GetCharacter(initButton.CharacterId));
            }
        }

        private void MouseDown(Point position)
        {
            var button = _actionBarButtons.FirstOrDefault(b => b.Rect.Contains(position));
            if (button != null)
            {
                button.IsMouseDown = true;
                return;
            }

            //if we didn't find a button click
            //get mouse position in hex space
            var mouseOffsetX = position.X - (_screenWidth / 2);
            var mouseOffsetY = position.Y - (_screenHeight / 2);

            var cursorPosition = GetHexCoordinate(mouseOffsetX, mouseOffsetY);
            
            //if the cursor is over a tile set that tile as focused.
            _focusedTile = cursorPosition;
        }
        
        /// <summary>
        /// Was this key pressed this frame
        /// </summary>
        private bool NewKeyPress(Keys key)
        {
            return _previousKeyboardState.IsKeyUp(key) && _keyBoardState.IsKeyDown(key);
        }

        /// <summary>
        /// Was this key release this frame
        /// </summary>
        private bool NewKeyRelease(Keys key)
        {
            return _previousKeyboardState.IsKeyDown(key) && _keyBoardState.IsKeyUp(key);
        }

        #endregion
        
        private void UpdateInitiative(List<Character> initList)
        {
            _initiativeTrack.Clear();
            var buttonSize = (int)(70 * _hexScreenScale);

            var buttonMargin = (int) (10 * _hexScreenScale);

            //width of init rect * count + space between rects * count -1 
            var initListLength = initList.Count * (buttonSize) + ((initList.Count - 1) * buttonMargin);
            var posX = _screenCenter.X - ((float)initListLength / 2);
            
            foreach (var character in initList)
            {
                _initiativeTrack.Add(new InitiativeTrackButton
                {
                    CharacterId = character.Id,
                    Rect = new Rectangle((int)posX, (int)(10 * _hexScreenScale), buttonSize, buttonSize)
                });
                posX += buttonSize + buttonMargin;
            }
        }

        private void SelectCharacter(Character character)
        {
            _selectedCharacter = character;
            UpdateButtons();
        }

        #region Private Action Bar Methods

        private void UpdateButtons()
        {
            _actionBarButtons.Clear();
            if (_selectedCharacter == null)
                return;

            var actions = _selectedCharacter.Actions;
            
            foreach (var action in actions)
            {
                AddButton(action.Name,
                    async (IInputProvider input) =>
                    {
                        await action.TriggerAsync(_selectedCharacter, input, GameStateManager.CurrentGameState);
                    },
                    () => action.IsAvailable(_selectedCharacter));
            }

            AddButton("End Turn", OnEndTurn, IsActiveCharacterSelected);
            UpdateButtonPositions();
        }
        
        private void AddButton(string name, Action<IInputProvider> onClickCallback, Func<bool> isEnabled = null)
        {
            var button = new UiButton(name, _buttonFont, onClickCallback, isEnabled);

            _actionBarButtons.Add(button);
            UpdateButtonPositions();
        }

        private void UpdateButtonPositions()
        {
            var actionBarLength = _actionBarButtons.Sum(button => button.Rect.Width);
            actionBarLength += _buttonMargin * (_actionBarButtons.Count - 1);

            var startPositionX = _screenCenter.X - (float)actionBarLength / 2;

            foreach (var button in _actionBarButtons)
            {
                button.SetPosition(new Vector2(startPositionX, _screenHeight - 60));
                startPositionX += button.Rect.Width + _buttonMargin;
            }
        }

		#endregion

		#endregion

		#region HelperFunctions

		/// <summary>
		/// Return a list of highlighted co-ordinates. If none exist returns null.
		/// </summary>
		/// <returns></returns>
		private List<HexCoordinate> GetHighlightedCoordinates()
		{
            var gameState = GameStateManager.CurrentGameState;
            return SelectedHexAction?.GetValidTargets(_selectedCharacter, gameState);
		}

		private bool IsActiveCharacterSelected()
        {
            var gameState = GameStateManager.CurrentGameState;
            return _selectedCharacter != null
                   && _selectedCharacter == gameState.ActiveCharacter;
        }

		private HexCoordinate GetHexCoordinate(float posX, float posY)
        {
            var gameState = GameStateManager.CurrentGameState;
            var x = (_sqrt3 / 3 * posX - 1.0f / 3 * posY) / (_hexHalfSize * _hexScale);
		    var z = 2.0f / 3 * posY / (_hexHalfSize * _hexScale);
		    var y = -(x + z);

		    var result = GetNearestCoord(x, y, z);
		    return gameState.Map.ContainsKey(result) ? result : null;
	    }

	    private HexCoordinate GetHexCoordinate(Vector2 position)
	    {
		    return GetHexCoordinate(position.X, position.Y);
	    }

	    private HexCoordinate GetNearestCoord(float x, float y, float z)
	    {
		    var rx = (int)Math.Round(x);
		    var ry = (int)Math.Round(y);
		    var rz = (int)Math.Round(z);

		    var deltaX = Math.Abs(rx - x);
		    var deltaY = Math.Abs(ry - y);
		    var deltaZ = Math.Abs(rz - z);

		    if (deltaX > deltaY && deltaX > deltaZ)
			    rx = -(ry + rz);
		    else if (deltaY > deltaZ)
			    ry = -(rx + rz);

		    return new HexCoordinate((int)rx, (int)ry);
	    }

		private Vector2 GetHexScreenPosition(HexCoordinate coordinate)
        {
            var posX = _hexHalfSize * (_sqrt3 * coordinate.X + (_sqrt3 / 2 * coordinate.Z)) * _hexScale;
            var posY = _hexHalfSize * 1.5f * coordinate.Z * _hexScale;

            return new Vector2(posX, posY) + _screenCenter;
        }

		#endregion

		#region Event Handlers
		
		private void EndTurn(EndTurnEventArgs e)
		{
			UpdateInitiative(e.InitativeOrder);
			SelectCharacter(e.InitativeOrder.FirstOrDefault());
		}

		private void OnEndTurn(IInputProvider input)
        {
            var gameState = GameStateManager.CurrentGameState;
            gameState.NextTurn();
        }

        private void OnMessage(object sender, MessageEventArgs args)
        {
            var action = new UiAction();

            if (args?.Character != null)
            {
                var character = _uiCharacterDictionary[args.Character.Id];
                action.Effect = new TextEffect(args.Message, _damageFont)
                {
                    Duration = 1.0f,
                    Position = character.Position + new Vector2(10.0f, -25.0f)
                };
            }
            else
            {
                action.Effect = new TextEffect(args.Message, _effectFont, TextAlignment.Center)
                {
                    Duration = 1.0f,
                    Position = _screenCenter,
                    Scale = new Vector2(1.0f)
                };
            }

            _uiActions.Add(action);
        }

        private void OnCharacterMove(object sender, MoveEventArgs e)
        {
            var sprite = _uiCharacterDictionary[e.ActiveCharacterId];
			
            //get the path that the sprite will take in screen space relative to it's starting position
            var movementPath = e.Path.Select(GetHexScreenPosition);
	        var action = new UiAction
	        {
		        Sprite = sprite,
				Animation = new MovementAnimation()
		        {
			        MovementPath = movementPath.ToList()
		        }
	        };
            _uiActions.Add(action);
        }

        private void OnCharacterSpawn(object sender, SpawnChracterEventArgs e)
        {
            if (_uiCharacterDictionary.ContainsKey(e.CharacterId))
            {
                var sprite = _uiCharacterDictionary[e.CharacterId];
                sprite.Position = GetHexScreenPosition(e.TargetPosition);
                return;
            }
			
            var tex = e.MonsterType == MonsterType.Zombie ? _monsterTexture : _monsterTexture_ZombieKing;
            var portraitTex = e.MonsterType == MonsterType.Zombie ? _monsterPortraitTexture
                : _monsterPortraitTexture_ZombieKing;

            var sprite2 = new UiCharacter(tex, portraitTex, GetHexScreenPosition(e.TargetPosition), e.MaxHealth)
            {
                Scale = new Vector2(_hexScale * 0.9f)
            };
            _uiCharacterDictionary.Add(e.CharacterId, sprite2);
        }

        private void OnCharacterDied(object sender, InteractionRequestEventArgs e)
        {
            var character = _uiCharacterDictionary[e.TargetCharacterId];

            var action = new UiAction();
            action.Effect = new TextEffect("DEAD", _damageFont, TextAlignment.Center)
            {
                Duration = 1.0f,
                Position = character.Position + new Vector2(10.0f, -25.0f)
            };

            action.ActionCompleteCallback = () =>
            {
                _uiCharacterDictionary.Remove(e.TargetCharacterId);
            };
            _uiActions.Add(action);
        }

        private void OnTakeDamage(object sender, DamageTakenEventArgs args)
        {
            var character = _uiCharacterDictionary[args.TargetCharacterId];

            var action = new UiAction();

            //todo - the values here are set based on the character's position at the moment it is called not when it is SHOWN.
            action.Effect = new TextEffect(args.DamageTaken.ToString(), _damageFont)
            {
                Duration = 1.0f,
                Position = character.Position + new Vector2(10.0f, -25.0f)
            };

            action.ActionCompleteCallback += () =>
            {
                if (_uiCharacterDictionary.ContainsKey(args.TargetCharacterId))
                    _uiCharacterDictionary[args.TargetCharacterId].TakeDamage(args.DamageTaken);
            };

            _uiActions.Add(action);
        }

        private void OnEndTurn(object sender, EndTurnEventArgs e)
        {
            var action = new UiAction();

            action.ActionCompleteCallback += () => { EndTurn(e); };

            _uiActions.Add(action);
        }

        private void OnActionTrigger(object sender, ActionEventArgs e)
        {
            var showActionNameUiAction = new UiAction();
            showActionNameUiAction.Effect = new TextEffect(e.Action.Name, _effectFont, TextAlignment.Center)
            {
                Duration = 1.0f,
                Position = _screenCenter,
                Scale = new Vector2(1.0f)
            };

            _uiActions.Add(showActionNameUiAction);
        }

        private void OnComboTrigger(object sender, ComboEventArgs e)
        {
            var character = _uiCharacterDictionary[e.TargetCharacterId];
            var action = new UiAction();

            action.Effect = new TextEffect(e.ComboEffect.Name, _damageFont)
            {
                Duration = 1.0f,
                Position = character.Position + new Vector2(10.0f, -25.0f),
                Scale = new Vector2(1.2f)
            };

            _uiActions.Add(action);
        }

        private void OnStatusEffectApplied(object sender, StatusEventArgs e)
        {
            var character = _uiCharacterDictionary[e.TargetCharacterId];
	        var statusEffect = e.StatusEffect;

            var action = new UiAction();
            action.Effect = new TextEffect(statusEffect.Name, _damageFont)
            {
                Duration = 1.0f,
                Position = character.Position + new Vector2(20.0f, -30.0f)
            };

	        Texture2D statusTexture;
	        switch (statusEffect.StatusEffectType)
	        {
		        case StatusEffectType.Burning:
			        statusTexture = _hexGame.Content.Load<Texture2D>("FireIcon");
					break;
		        case StatusEffectType.Frozen:
			        statusTexture = _hexGame.Content.Load<Texture2D>("FrozenIcon");
					break;
		        case StatusEffectType.Rooted:
			        statusTexture = _hexGame.Content.Load<Texture2D>("StopIcon");
					break;
		        case StatusEffectType.Bleeding:
			        statusTexture = _hexGame.Content.Load<Texture2D>("BloodIcon");
					break;
		        default:
			        throw new ArgumentOutOfRangeException();
	        }
			var status = new UiStatusEffect(statusTexture, statusEffect.Id);
			
			action.ActionCompleteCallback += () =>
            {
	            character.ApplyStatus(status);
            };

            _uiActions.Add(action);
        }

        private void OnStatusEffectRemoved(object sender, StatusEventArgs e)
        {
            var character = _uiCharacterDictionary[e.TargetCharacterId];

            var action = new UiAction
            {
                Effect = new TextEffect("- " + e.StatusEffect.Name, _damageFont)
                {
                    Duration = 1.0f,
                    Position = character.Position + new Vector2(10.0f, -25.0f),
                    BaseColour = Color.White
                }
            };

            action.ActionCompleteCallback += () =>
            {
                character.Colour = Color.White;
	            character.RemoveStatus(e.StatusEffect.Id);
            };

            _uiActions.Add(action);
        }

        private void OnPotentialChange(object sender, PotentialEventArgs args)
        {
            var potentialChange = new UiAction();
            potentialChange.Effect = new TextEffect($"{args.PotentialChange} Potential", _effectFont, TextAlignment.Center)
            {
                Duration = 1.0f,
                Position = _screenCenter,
                Scale = new Vector2(1.0f)
            };

            _uiActions.Add(potentialChange);
        }

        private void OnGameOver(object sender, MessageEventArgs args)
        {
            var gameOver = new UiAction();
            gameOver.Effect = new TextEffect($"Game Over! \n {args.Message}", _effectFont, TextAlignment.Center)
            {
                Duration = 5.0f,
                Position = _screenCenter,
                Scale = new Vector2(1.2f)
            };

            gameOver.ActionCompleteCallback += () => { _hexGame.Exit(); };

            _uiActions.Add(gameOver);
        }
        
        #endregion

        #endregion
    }
}