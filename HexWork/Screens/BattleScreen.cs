using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;
using HexWork.Gameplay;
using HexWork.Gameplay.Actions;
using HexWork.Gameplay.GameObject;
using HexWork.Gameplay.GameObject.Characters;
using HexWork.Gameplay.Interfaces;
using HexWork.GameplayEvents;
using HexWork.Interfaces;
using HexWork.Screens;
using HexWork.UI.Interfaces;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace HexWork.UI
{
    /// <summary>
    /// HUD class that handles user interaction
    /// </summary>
    public class BattleScreen : Screen, IInputProvider
    {
        #region Attributes

        #region Consts

        /// <summary>
        /// Used for conversions from HexSpace to Screen Space.
        /// </summary>
        private readonly float _sqrt3 = (float)Math.Sqrt(3.0);
        private const float ScreenEdgeMargin = 10;

        #endregion
        
        private readonly List<UiButton> _actionBarButtons = new List<UiButton>();        
        private Dictionary<Guid, UiGameObject> _gameObjectDictionary = new Dictionary<Guid, UiGameObject>();
        private readonly List<InitiativeTrackButton> _initiativeTrack = new List<InitiativeTrackButton>();
        private List<UiAction> _uiActions = new List<UiAction>();
	    private PreviewRulesProvider _RulesProviderProxy;

        public Character SelectedCharacter => BoardState.ActiveCharacter;
        private HexCoordinate _focusedTile = null;
        private HexCoordinate _cursorPosition;
        public event EventHandler<HexCoordinate> OnClickEvent;
        private HexAction SelectedHexAction = null;
        private RulesProvider RulesProvider;
        private BoardState BoardState;

        #region Rendering Attributes

        private readonly Color _cursorColor = Color.Red;
        private SpriteFont _buttonFont;
        private SpriteFont _uiFont;
        private SpriteFont _damageFont;
        private SpriteFont _effectFont;

        private Texture2D _buttonTexture;
        private Texture2D _blankTexture;
        
        private readonly Color _buttonEnabledColor = Color.LightGray;
        private readonly Color _buttonDisabledColor = Color.DarkGray;
        private readonly Color _buttonMouseDownColor = Color.SlateGray;
	    private readonly Color _buttonHoverColor = Color.LightSlateGray;

		private Texture2D _hexagonTexture;
        private Texture2D _hexagonOutlineTexture;

        //screen dimensions
        private readonly int _screenWidth;
        private readonly int _screenHeight;
        private readonly Vector2 _screenCenter;
        private Vector2 _cameraPosition = Vector2.Zero;
        private Matrix _cameraTransformMatrix = Matrix.Identity;

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

        #region Methods

        #region Initialisation Methods

        public BattleScreen(IScreenManager _screenManager, int difficulty)
            : base(_screenManager)
        {
            var game = (HexWork)_screenManager.Game;
            _hexGame = game;
            
            _screenHeight = game.ScreenHeight;
            _screenWidth = game.ScreenWidth;
            _screenCenter = new Vector2((float)_screenWidth / 2, (float)_screenHeight / 2);

            _hexCenter = new Vector2(_hexHalfSize, _hexHalfSize);
            _hexScreenScale = (float)_screenWidth / 1920;
            _hexScale = 0.4f * _screenWidth / 1920;
            _hexScaleV = new Vector2(_hexScale);

            RulesProvider = new RulesProvider();
            BoardState = new BoardState();
            BoardState.GenerateMap();

            RulesProvider.CharacterMoveEvent += OnCharacterMove;
            RulesProvider.CharacterTeleportEvent += OnCharacterTeleport;
            RulesProvider.EndTurnEvent += OnEndTurn;
            RulesProvider.SpawnEntityEvent += OnEntitySpawn;
            RulesProvider.RemoveEntityEvent += OnEntityDied;
            RulesProvider.TakeDamageEvent += OnTakeDamage;
            RulesProvider.ActionEvent += OnActionTrigger;
            RulesProvider.ComboEvent += OnComboTrigger;
            RulesProvider.StatusAppliedEvent += OnStatusEffectApplied;
            RulesProvider.StatusRemovedEvent += OnStatusEffectRemoved;
            RulesProvider.PotentialChangeEvent += OnPotentialChange;
            RulesProvider.MessageEvent += OnMessage;
            RulesProvider.GameOverEvent += OnGameOver;

            BoardState = RulesProvider.CreateCharacters(BoardState, difficulty);
        }

        public override void LoadContent(Game game)
        {
            _hexagonTexture = game.Content.Load<Texture2D>("hexagon");
            _hexagonOutlineTexture = game.Content.Load<Texture2D>("hexagonOutline");

            LoadBlankTextures(game);
            LoadEffects(game);

            //load the button font
            _buttonFont = game.Content.Load<SpriteFont>("Nunito");
            _uiFont = game.Content.Load<SpriteFont>("Arial");

            _damageFont = game.Content.Load<SpriteFont>("Nunito");
            _effectFont = game.Content.Load<SpriteFont>("MenuFont");

            _spriteBatch = new SpriteBatch(game.GraphicsDevice);
	        _RulesProviderProxy = new PreviewRulesProvider(_hexGame);
            
            BoardState = RulesProvider.StartGame(BoardState);
        }

        #region Private Load Content Methods

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
            UpdateInitiative(BoardState.Characters);
        }

        #endregion

        #region Update Methods

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            BoardState = RulesProvider.Update(BoardState);
            foreach (var character in _gameObjectDictionary.Values)
            {
                character.Update((float)gameTime.ElapsedGameTime.TotalSeconds);
            }
            
            var actionToPlay = _uiActions.FirstOrDefault();
            if (actionToPlay == null) return;

            actionToPlay.Update(gameTime);
            if (!actionToPlay.IsComplete()) return;

            actionToPlay.ActionCompleteCallback?.Invoke();

            _uiActions.Remove(actionToPlay);

            var newAction = _uiActions.FirstOrDefault();
            newAction?.Start();
        }

        public override void HandleInput()
        {
            //Update keyboard state and mouse state
            _previousMouseState = _mouseState;
            _mouseState = Mouse.GetState();
            _previousKeyboardState = _keyBoardState;
            _keyBoardState = Keyboard.GetState();
	        _actionBarButtons.ForEach(b => b.IsHover = false);

			UiButton button;
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
			else if((button = _actionBarButtons.FirstOrDefault(b => b.Rect.Contains(_mouseState.Position))) != null)
	        {
				button.IsHover = true;
	        }
            
            if (NewKeyRelease(Keys.Tab) || NewKeyRelease(Keys.R) || NewKeyRelease(Keys.Space))
            {
                SelectedHexAction?.RotateTargeting(_keyBoardState.IsKeyDown(Keys.LeftShift));
            }
            else if (_mouseState.ScrollWheelValue != _previousMouseState.ScrollWheelValue)
            {
                SelectedHexAction?.RotateTargeting(_mouseState.ScrollWheelValue < _previousMouseState.ScrollWheelValue);
            }

            if (_keyBoardState.IsKeyDown(Keys.W))
            {
                _cameraPosition.Y += 1.0f;
            }
            if (_keyBoardState.IsKeyDown(Keys.A))
            {
                _cameraPosition.X += 1.0f;
            }
            if (_keyBoardState.IsKeyDown(Keys.S))
            {
                _cameraPosition.Y -= 1.0f;
            }
            if (_keyBoardState.IsKeyDown(Keys.D))
            {
                _cameraPosition.X -= 1.0f;
            }

            _cameraTransformMatrix = Matrix.CreateTranslation(_cameraPosition.X, _cameraPosition.Y, 0.0f);

            //right click cancels UI state.
            if (_previousMouseState.RightButton == ButtonState.Pressed &&
                _mouseState.RightButton == ButtonState.Released)
            {
                _focusedTile = null;
                OnClickEvent?.Invoke(this, null);
                _actionBarButtons.ForEach(b => b.IsMouseDown = false);
            }

            var position = _mouseState.Position - _cameraPosition.ToPoint();

            var mouseOffsetX = position.X - (_screenWidth / 2);
            var mouseOffsetY = position.Y - (_screenHeight / 2);

            _cursorPosition = GetHexCoordinate(mouseOffsetX, mouseOffsetY);
        }

        #endregion

        #region Rendering Methods

        public override void Draw(GameTime _gameTime)
        {
            DrawMapUi();
            DrawEntities();
            DrawUiEffects();
            DrawHud();
            DrawActionBar();
            DrawInitiativeTrack();

            //Draw Preview
            if (_cursorPosition != null)
	        {
		        _RulesProviderProxy.rulesProvider = RulesProvider;

				_RulesProviderProxy.SpriteBatchBegin(_cameraTransformMatrix);

		        SelectedHexAction?.TriggerAsync(BoardState, SelectedCharacter.Id,
			        new DummyInputProvider(_cursorPosition), _RulesProviderProxy);
					
		        _RulesProviderProxy.SpriteBatchEnd();
			}
        }
        
        #region Private Rendering Methods

        private void DrawHud()
        {
            List<string> rightSideStrings = new List<string>();
            if (BoardState.ActiveCharacter != null)
            {
                rightSideStrings.Add("Active Character : " + BoardState.ActiveCharacter.Name);
            }
            rightSideStrings.Add($"Current Potential : {BoardState.Potential}");
            if (SelectedCharacter != null)
            {
                rightSideStrings.Add($"Selected Character : {SelectedCharacter.Name}");
                rightSideStrings.Add($"Power : {SelectedCharacter.Power}");
                rightSideStrings.Add($"Position : {SelectedCharacter.Position}");
                rightSideStrings.Add($"Health : {SelectedCharacter.Health}/{SelectedCharacter?.MaxHealth}");
            }
            
            DrawRightSideText(rightSideStrings);
            
            _spriteBatch.Begin();
            
            if (_cursorPosition != null)
	        {
		        _spriteBatch.DrawString(_uiFont, $"{_cursorPosition.X},{_cursorPosition.Y},{_cursorPosition.Z}",
			        new Vector2(ScreenEdgeMargin, ScreenEdgeMargin), Color.Black);
			}

			var startPositionX = _screenCenter.X - ((float)BoardState.MaxPotential / 2 * 65);
            var potentialPosY = 100 * _hexScreenScale;

            var screenPosition = new Vector2(startPositionX, potentialPosY);

            for (int i = 0; i < BoardState.MaxPotential; i++)
            {
                var color = BoardState.Potential <= i ? Color.LightPink : Color.Red;

                _spriteBatch.Draw(_blankTexture, screenPosition, null, color, 0.0f, Vector2.Zero, new Vector2(30.0f, 2.0f),
                    SpriteEffects.None, 0.0f);

                startPositionX += 65;
                screenPosition.X = startPositionX;
                screenPosition.Y = potentialPosY;
            }

            _spriteBatch.End();
        }

        private void DrawRightSideText(IEnumerable<string> textLines)
        {
            _spriteBatch.Begin();

            var posY = ScreenEdgeMargin;
            var posX = 0.0f;
            Vector2 stringSize;
            var titleSafeArea = _spriteBatch.GraphicsDevice.Viewport.TitleSafeArea;

            foreach (var line in textLines)
            {
                stringSize = _uiFont.MeasureString(line);
                posX = titleSafeArea.Right -
                       stringSize.X - ScreenEdgeMargin;
                _spriteBatch.DrawString(_uiFont, line, new Vector2(posX, posY), Color.Black);
                posY += stringSize.Y;
            }

            _spriteBatch.End();
        }

        private void DrawActionBar()
        {
            _spriteBatch.Begin();

            foreach (var button in _actionBarButtons)
            {
                _spriteBatch.Draw(_buttonTexture, button.Rect, button.Colour);
                _spriteBatch.DrawString(_buttonFont, button.Text, button.Position - button.TextSize / 2, button.TextColour);
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

                if (!_gameObjectDictionary.ContainsKey(id))
                    continue;

                var texture = _gameObjectDictionary[id].PortraitTexture;

                if (texture == null)
                    continue;

                if (id == SelectedCharacter?.Id)
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
            _spriteBatch.Begin(transformMatrix: _cameraTransformMatrix);
            foreach (var kvp in BoardState)
            {
                var coordinate = kvp.Key;
                var tile = kvp.Value;

                var renderPosition = GetHexScreenPosition(coordinate);

                _spriteBatch.Draw(_hexagonTexture, renderPosition, null, tile.Color, 0.0f,
                    _hexCenter, _hexScaleV, SpriteEffects.None, 0.0f);

                var terrainName = tile.TerrainType.ToString();
                if (tile.TerrainType == TerrainType.Ground) continue;

                _spriteBatch.DrawString(_uiFont, terrainName, renderPosition, Color.White, 0.0f, _uiFont.MeasureString(terrainName) / 2, Vector2.One, SpriteEffects.None, 0.0f);
            }
            _spriteBatch.End();
        }

        private void DrawHighlightedMap(Dictionary<HexCoordinate, int> highlightedTiles)
        {
            _spriteBatch.Begin(transformMatrix: _cameraTransformMatrix);

            foreach (var kvp in BoardState)
            {
                var coordinate = kvp.Key;
                var tile = kvp.Value;

                var renderPosition = GetHexScreenPosition(coordinate);

                var color = tile.Color;

                if (!highlightedTiles.Keys.Contains(coordinate))
                {
                    color = new Color(color.ToVector3() / 6);                    
                }

                _spriteBatch.Draw(_hexagonTexture, renderPosition, null, color, 0.0f,
                        _hexCenter, _hexScaleV, SpriteEffects.None, 0.0f);
            }

            _spriteBatch.End();
        }

        private void DrawMapUi()
        {
            _spriteBatch.Begin(transformMatrix: _cameraTransformMatrix);

            //if we've selected a tile highlight it in purple
            if (_focusedTile != null)
            {
                var highlightRenderPosition = GetHexScreenPosition(_focusedTile);

                var purple = new Color(new Vector3(0.5f, 0.0f, 0.5f));

                _spriteBatch.Draw(_hexagonOutlineTexture, highlightRenderPosition, null,
                    purple, 0.0f,
                    _hexCenter, _hexScaleV, SpriteEffects.None, 0.0f);
            }
            _spriteBatch.End();

            var highlightedCoords = GetHighlightedCoordinates();

            if (highlightedCoords != null)
            {
                DrawHighlightedMap(highlightedCoords);
            }
            else
            {
                DrawMap();
            }

            _spriteBatch.Begin(transformMatrix: _cameraTransformMatrix);
            
            //If the cursor is within the map area the highlight the appropriate tile.
	        if (_cursorPosition != null)
	        {
		        var highlightRenderPosition = GetHexScreenPosition(_cursorPosition);

		        _spriteBatch.Draw(_hexagonOutlineTexture, highlightRenderPosition, null, _cursorColor, 0.0f,
			        _hexCenter, _hexScaleV, SpriteEffects.None, 0.0f);
					
				var targetPattern = SelectedHexAction?.GetTargetTiles(_cursorPosition);
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

        private void DrawEntities()
        {
            //draw highlighted elements
            _spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend,
                null, null,
                null, _pixelOutlineEffect, transformMatrix: _cameraTransformMatrix);

            _outlineWidth.SetValue(5);
            _outlineColour.SetValue(Color.TransparentBlack.ToVector4());

            //draw characters
            foreach (var kvp in _gameObjectDictionary)
            {
                var sprite = kvp.Value;

				if (kvp.Key == SelectedCharacter?.Id)
                {
                    _outlineColour.SetValue(Color.Red.ToVector4());

                    _spriteBatch.Draw(sprite.Texture, sprite.Position, null, Color.White, 0.0f,
                        sprite.Origin, sprite.Scale, SpriteEffects.None, 0.0f);

                    _outlineColour.SetValue(Color.TransparentBlack.ToVector4());
                }
                else
                {
                    _spriteBatch.Draw(sprite.Texture, sprite.Position, null, Color.White, 0.0f,
                        sprite.Origin, sprite.Scale, SpriteEffects.None, 0.0f);
                }
            }

			_spriteBatch.End();

            _spriteBatch.Begin(transformMatrix: _cameraTransformMatrix);

            //draw health bars
            foreach (var character in _gameObjectDictionary.Values)
            {
                var position = character.Position + new Vector2(0.0f, 25.0f);

                _spriteBatch.Draw(_blankTexture, position, null, Color.Black, 0.0f,
                    new Vector2(1.0f, 1.0f), new Vector2(18.0f, 2.0f), SpriteEffects.None, 0.0f);
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

            _spriteBatch.Begin(transformMatrix: _cameraTransformMatrix);

            var highlightedCoords = GetHighlightedCoordinates();
            if (highlightedCoords != null)
                foreach (var kvp in highlightedCoords)
                {
                    var coordinate = kvp.Key;
                    var tile = kvp.Value;
                    var renderPosition = GetHexScreenPosition(coordinate);

                    _spriteBatch.DrawString(_damageFont, kvp.Value.ToString(), renderPosition, Color.White);
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
                effect.ColorToDraw, 0.0f, effect.Offset - _cameraPosition, effect.Scale, SpriteEffects.None, 0.0f);
            
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
        		
        #region Private Handle Input Methods

        /// <summary>
        /// Handle a click event in the ui
        /// </summary>
        /// <param name="clickPosition">The position of the click in screen space.</param>
        private void MouseUp(Point clickPosition)
        {
            //get mouse position in hex space
            var mouseOffsetX = clickPosition.X - (_screenWidth / 2);
            var mouseOffsetY = clickPosition.Y - (_screenHeight / 2);
            var cursorPosition = GetHexCoordinate(mouseOffsetX, mouseOffsetY);
            
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
        
        private void UpdateInitiative(IEnumerable<Character> initList)
        {
            _initiativeTrack.Clear();
            var buttonSize = (int)(70 * _hexScreenScale);
            var buttonMargin = (int) (10 * _hexScreenScale);

            //width of init rect * count + space between rects * count -1 
            var initListLength = initList.Count() * (buttonSize) + ((initList.Count() - 1) * buttonMargin);
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

        #region Private Action Bar Methods

        private void UpdateButtons()
        {
            _actionBarButtons.Clear();
            if (SelectedCharacter == null)
                return;

            var actions = SelectedCharacter.Actions;
            
            foreach (var action in actions)
            {
                var name = action.Name;
                if(action is PotentialGainAction)
                {
                    name = !BoardState.ActiveCharacterHasAttacked ? name : "End Turn";
                }

                AddButton(name,
                    async (input) =>
                    {
                        BoardState = await action.TriggerAsync(BoardState, SelectedCharacter.Id, input, RulesProvider);
                        var followUpAction = action.FollowUpAction;
                        while (followUpAction != null)
                        {
                            BoardState = await followUpAction.TriggerAsync(BoardState, SelectedCharacter.Id, input,
                                RulesProvider);
                            followUpAction = followUpAction.FollowUpAction;
                        }
                        UpdateButtons();
                    }, action.IsAvailable(SelectedCharacter, BoardState));
            }
            UpdateButtonPositions();
        }
        
        private void AddButton(string name, Action<IInputProvider> onClickCallback, bool isEnabled)
        {
            var button = new UiButton(name, _buttonFont, onClickCallback, isEnabled);
            
            button.Colour = button.IsEnabled ? //1st conditional
                    (button.IsMouseDown ? _buttonMouseDownColor
                        : (button.IsHover ? _buttonHoverColor : _buttonEnabledColor))
                    : _buttonDisabledColor; //1st conditional false

            button.TextColour = button.IsEnabled ? Color.Black : Color.Gray;

            _actionBarButtons.Add(button);
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
		private Dictionary<HexCoordinate, int> GetHighlightedCoordinates()
		{
            if (SelectedHexAction == null)
                return null;

            var selectedCharacter = BoardState.ActiveCharacter;

            return SelectedHexAction.GetValidTargets(BoardState, selectedCharacter, SelectedHexAction.TargetType);
		}

		private HexCoordinate GetHexCoordinate(float posX, float posY)
        {
            var x = (_sqrt3 / 3 * posX - 1.0f / 3 * posY) / (_hexHalfSize * _hexScale);
		    var z = 2.0f / 3 * posY / (_hexHalfSize * _hexScale);
		    var y = -(x + z);

		    var result = GetNearestCoord(x, y, z);
		    return BoardState.ContainsKey(result) ? result : null;
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
            UpdateButtons();
		}

        private void OnMessage(object sender, MessageEventArgs args)
        {
            var action = new UiAction();
            if (args?.Character != null)
            {
                var character = _gameObjectDictionary[args.Character.Id];
                action.Effect = new TextEffect(args.Message, _damageFont)
                {
                    Position = character.Position + new Vector2(10.0f, -25.0f)
                };
            }
            else
            {
                action.Effect = new TextEffect(args?.Message, _effectFont, TextAlignment.Center)
                {
                    Position = _screenCenter,
                    Scale = new Vector2(1.0f)
                };
            }

            _uiActions.Add(action);
        }

        private void OnCharacterMove(object sender, MoveEventArgs e)
        {
            var sprite = _gameObjectDictionary[e.CharacterId];
            
            //get the path that the sprite will take in screen space relative to it's starting position
            var action = new UiAction
	        {
		        Sprite = sprite,
				Animation = new MovementAnimation(GetHexScreenPosition(e.Destination))
	        };
            _uiActions.Add(action);
        }

        private void OnCharacterTeleport(object sender, MoveEventArgs e)
        {
            var sprite = _gameObjectDictionary[e.CharacterId];
            sprite.Position = GetHexScreenPosition(e.Destination);
        }
        
        private void OnEntitySpawn(object sender, EntityEventArgs e)
        {
            if (e.Entity is Character character)
            {
                var tex = _hexGame.Content.Load<Texture2D>($"{character.CharacterType}");
                var portraitTex = _hexGame.Content.Load<Texture2D>($"{character.CharacterType}Portrait");
                var sprite = new UiCharacter(tex, portraitTex, GetHexScreenPosition(character.Position), character.MaxHealth)
                {
                    Scale = new Vector2(_hexScale * 0.9f),
                    Origin = _hexCenter
                };

                _gameObjectDictionary.Add(e.Entity.Id, sprite);
                return;
            }

            Texture2D tex2 = _hexGame.Content.Load<Texture2D>(e.Entity.Name);

            var tileEffect = new UiTileEffect(e.Entity.Id, tex2, e.Entity.MaxHealth)
            {
                Position = GetHexScreenPosition(e.Entity.Position)
            };

            _gameObjectDictionary.Add(tileEffect.Id, tileEffect);
        }

        private void OnEntityDied(object sender, EntityEventArgs e)
        {
            var character = _gameObjectDictionary[e.Entity.Id];

            var action = new UiAction();
            
            action.ActionCompleteCallback = () =>
            {
                _gameObjectDictionary.Remove(e.Entity.Id);
            };
            _uiActions.Add(action);
        }

        private void OnTakeDamage(object sender, DamageTakenEventArgs args)
        {
            var character = _gameObjectDictionary[args.TargetCharacterId];

            var action = new UiAction();
            action.Sprite = character;

            //todo - the values here are set based on the character's position at the moment it is called not when it is SHOWN.
            action.Effect = new TextEffect(args.DamageTaken.ToString(), _damageFont)
            {
                PositionModifier = new Vector2(10.0f, -25.0f)
            };

            action.ActionCompleteCallback = () =>
            {
                if (_gameObjectDictionary.ContainsKey(args.TargetCharacterId))
                    _gameObjectDictionary[args.TargetCharacterId].TakeDamage(args.DamageTaken);
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
                Position = _screenCenter,
                Scale = new Vector2(1.0f)
            };

            _uiActions.Add(showActionNameUiAction);
        }

        private void OnComboTrigger(object sender, ComboEventArgs e)
        {
            var character = _gameObjectDictionary[e.TargetCharacterId];
            var action = new UiAction()
            {
                Sprite = character
            };

            action.Effect = new TextEffect(e.ComboEffect.Name, _damageFont)
            {
                Position = character.Position + new Vector2(10.0f, -25.0f),
                Scale = new Vector2(1.2f)
            };

            _uiActions.Add(action);
        }

        private void OnStatusEffectApplied(object sender, StatusEventArgs e)
        {
            var character = _gameObjectDictionary[e.TargetCharacterId];

            var action = new UiAction
            {
                Sprite = character
            };
            action.Effect = new TextEffect(e.StatusEffectType.ToString(), _damageFont)
            {
                Position = character.Position + new Vector2(20.0f, -30.0f)
            };

	        Texture2D statusTexture;
            switch (e.StatusEffectType)
            {
                case Element.Fire:
                    statusTexture = _hexGame.Content.Load<Texture2D>("FireIcon");
                    break;
                case Element.Ice:
                    statusTexture = _hexGame.Content.Load<Texture2D>("FrozenIcon");
                    break;
                case Element.Earth:
                    statusTexture = _hexGame.Content.Load<Texture2D>("StopIcon");
                    break;
                case Element.Lightning:
                    statusTexture = _hexGame.Content.Load<Texture2D>("LightningIcon");
                    break;
                case Element.Wind:
                    statusTexture = _hexGame.Content.Load<Texture2D>("Wind");
                    break;
                case Element.None:
                    statusTexture = _hexGame.Content.Load<Texture2D>("Cross");
                    break;
                default:
                    throw new ArgumentOutOfRangeException("element not declared");
            }
            var status = new UiStatusEffect(statusTexture, e.StatusEffectType);
			
			action.ActionCompleteCallback += () =>
            {
	            character.ApplyStatus(status);
            };

            _uiActions.Add(action);
        }

        private void OnStatusEffectRemoved(object sender, StatusEventArgs e)
        {
            var character = _gameObjectDictionary[e.TargetCharacterId];

            var action = new UiAction
            {
                Effect = new TextEffect("- " + e.StatusEffectType.ToString(), _damageFont)
                {
                    Position = character.Position + new Vector2(10.0f, -25.0f),
                    BaseColour = Color.White
                }
            };

            action.ActionCompleteCallback += () =>
            {
                character.Colour = Color.White;
	            character.RemoveStatus(e.StatusEffectType);
            };

            _uiActions.Add(action);
        }

        private void OnPotentialChange(object sender, PotentialEventArgs args)
        {
            var potentialChange = new UiAction();
            potentialChange.Effect = new TextEffect($"{args.PotentialChange} Potential", _effectFont, TextAlignment.Center)
            {
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

            gameOver.ActionCompleteCallback += () =>
            {
                Exit();
                screenManager.AddScreen(new RewardsScreen(screenManager));
                screenManager.RemoveScreen(this);
            };

            _uiActions.Add(gameOver);
        }

        #endregion
    }
}