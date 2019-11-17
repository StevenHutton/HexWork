using System;
using System.Collections.Generic;
using HexWork.Gameplay;
using HexWork.Gameplay.Actions;
using HexWork.Gameplay.Interfaces;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameTestProject.Gameplay;

namespace HexWork.UI
{
    public class PreviewGameStateProxy : IGameStateObject, IDisposable
    {
        private IGameStateObject _gameState;

	    private SpriteBatch _spriteBatch;

	    private Texture2D _invalidTexture;
	    private Texture2D _blankTexture;
	    private Texture2D _damageTexture;
        private Texture2D _healingTexture;
        private Texture2D _arrowTexture;

	    private Texture2D _fireIconTexture;

		//screen dimensions
		private readonly int _screenWidth;
	    private readonly int _screenHeight;
	    private readonly Vector2 _screenCenter;
	    private readonly float _sqrt3 = (float)Math.Sqrt(3.0);

		//scaling of 
		private readonly float _hexScale;
	    private readonly Vector2 _hexScaleV;

	    //half-height of a hex-texture in pixels - todo should be set dynamically.
	    private readonly float _hexHalfSize = 128;
	    private readonly Vector2 _hexCenter;

        private HexWork _hexGame;

		#region Properties

		public IGameStateObject GameState
	    {
		    get => _gameState;
		    set => _gameState = value;
	    }

		#endregion

	    public void SpriteBatchBegin()
	    {
			_spriteBatch.Begin();
	    }
	    public void SpriteBatchEnd()
	    {
		    _spriteBatch.End();
		}

        public PreviewGameStateProxy(HexWork game)
        {
	        _screenHeight = game.ScreenHeight;
	        _screenWidth = game.ScreenWidth;
	        _screenCenter = new Vector2((float)_screenWidth / 2, (float)_screenHeight / 2);

	        _hexCenter = new Vector2(64, 64);
	        _hexScale = 0.4f * _screenWidth / 1920;
	        _hexScaleV = new Vector2(_hexScale);

			_spriteBatch = new SpriteBatch(game.GraphicsDevice);

	        _invalidTexture = game.Content.Load<Texture2D>("Cross");
	        _damageTexture = game.Content.Load<Texture2D>("Damage");
            _healingTexture = game.Content.Load<Texture2D>("Healing");
            _arrowTexture = game.Content.Load<Texture2D>("Arrow");

	        _fireIconTexture = game.Content.Load<Texture2D>("FireIcon");

			var data = new[] { Color.White, Color.White, Color.White, Color.White };
			_blankTexture = new Texture2D(game.GraphicsDevice, 2, 2);
	        _blankTexture.SetData(data);

            _hexGame = (HexWork) game;
        }

	    public List<HexCoordinate> GetValidDestinations(Character objectCharacter, int range)
	    {
		    return _gameState.GetValidDestinations(objectCharacter, range);
	    }

	    public bool IsValidDestination(Character objectCharacter, HexCoordinate targetPosition, int range)
        {
			var isValid = _gameState.IsValidDestination(objectCharacter, targetPosition, range);

	        if (!isValid)
	        {
		        var pos = GetHexScreenPosition(targetPosition);

				_spriteBatch.Draw(_invalidTexture, pos, null, Color.White, 0.0f, _hexCenter, _hexScaleV, SpriteEffects.None, 0.0f );
	        }
	        else
	        {
				FindShortestPath(objectCharacter.Position, targetPosition, objectCharacter.MovementType);
			}

	        return isValid;
        }

	    public bool IsValidTarget(Character objectCharacter, HexCoordinate targetPosition, int range, GetValidTargetsDelegate targetDelegate)
	    {
		    var isValid = _gameState.IsValidTarget(objectCharacter, targetPosition, range, targetDelegate);

		    if (!isValid)
		    {
			    var pos = GetHexScreenPosition(targetPosition);

			    _spriteBatch.Draw(_invalidTexture, pos, null, Color.White, 0.0f, _hexCenter, _hexScaleV, SpriteEffects.None, 0.0f);
		    }

		    return isValid;
	    }

		public List<HexCoordinate> FindShortestPath(HexCoordinate startPosition, HexCoordinate destination, MovementType movementType = MovementType.NormalMove)
	    {
			var path = _gameState.FindShortestPath(startPosition, destination, movementType);
		    if (path == null)
			    return null;

		    foreach (var hex in path)
		    {
			    _spriteBatch.Draw(_blankTexture, GetHexScreenPosition(hex), null,
				    Color.White, 0.0f, new Vector2(1.0f, 1.0f),
				    6.0f, SpriteEffects.None, 0.0f);
		    }

			return new List<HexCoordinate>();
	    }

	    public List<HexCoordinate> GetAxisTilesInRange(Character objectCharacter, int range)
	    {
		    return _gameState.GetAxisTilesInRange(objectCharacter, range);
	    }

	    /// <summary>
		/// Intentionally does nothing
		/// </summary>
		/// <param name="character"></param>
		/// <param name="targetPosition"></param>
        public void MoveCharacterTo(Character character, HexCoordinate targetPosition, List<HexCoordinate> path = null)
        {
            ResolveTerrainEffects();
        }


        public void TeleportCharacterTo(Character character, HexCoordinate position)
        {

        }

        private void ResolveTerrainEffects()
        {

        }

        /// <summary>
        /// intentionally does nothing
        /// </summary>
        /// <param name="action"></param>
        public void NotifyAction(HexAction action, Character character)
        {

        }

        public Character GetCharacterAtCoordinate(HexCoordinate coordinate)
        {
            //throw new System.NotImplementedException();
	        return _gameState.GetCharacterAtCoordinate(coordinate);
        }

        public int ApplyDamage(Character character, int power, string message = null)
        {
	        var position = GetHexScreenPosition(character.Position);
            _spriteBatch.Draw(_damageTexture, position, null, Color.Red, 0.0f, new Vector2(128), _hexScaleV, SpriteEffects.None, 0.0f );
	        return power;
        }

        public void CheckDied(Character character)
        { }

        public void ApplyHealing(Character character, int power)
        {
            var position = GetHexScreenPosition(character.Position);

            _spriteBatch.Draw(_healingTexture, position, null, Color.Green, 0.0f, new Vector2(128), _hexScaleV,
                SpriteEffects.None, 0.0f);
        }

        public void ApplyStatus(Character character, StatusEffect effect)
        {
            if (effect == null) return;

            Texture2D statusTexture;
            switch (effect.StatusEffectType)
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

            var status = new UiStatusEffect(statusTexture, effect.Id);
	        var position = GetHexScreenPosition(character.Position);
	        _spriteBatch.Draw(status.Texture, 
		        position,
		        null,
		        Color.White,
		        0.0f,
		        status.Origin, 
		        new Vector2(0.3f), 
		        SpriteEffects.None, 
		        0.0f);
		}

        public void CreateTileEffect(HexCoordinate location)
        {
            var statusTexture = _hexGame.Content.Load<Texture2D>("FireIcon");
            var position = GetHexScreenPosition(location);

            var width = (float)statusTexture.Width;
            var height = (float)statusTexture.Height;

            var origin = new Vector2(width / 2, height - (width / 2));

            _spriteBatch.Draw(statusTexture,
                position,
                null,
                Color.White,
                0.0f,
                origin,
                new Vector2(0.3f), 
                SpriteEffects.None,
                0.0f);
        }

        public void ApplyCombo(Character targetCharacter, ComboAction combo)
        {
        }

        public void ApplyPush(Character character, HexCoordinate direction, int pushForce = 0)
        {
	        var characterPos = character.Position;
	        var destinationPos = characterPos + direction;

            while (pushForce > 0)
            {
                var characterScreenPos = GetHexScreenPosition(characterPos);
                var destinationScreenPos = GetHexScreenPosition(destinationPos);
                var pushVector = (destinationScreenPos - characterScreenPos);
                var rotation = (float)Math.Atan2(pushVector.Y, pushVector.X);
                var drawPosition = characterScreenPos + (pushVector * 0.8f);

                if (IsHexPassable(destinationPos))
                {
                    _spriteBatch.Draw(_arrowTexture, drawPosition, null, Color.White, rotation, new Vector2(64, 64), _hexScale, SpriteEffects.None, 0.0f);
                }
                else if (!IsTileEmpty(destinationPos))
                {
                    _spriteBatch.Draw(_damageTexture, characterScreenPos, null, Color.White, 0.0f, new Vector2(128), _hexScaleV, SpriteEffects.None, 0.0f);
                    _spriteBatch.Draw(_damageTexture, destinationScreenPos, null, Color.White, 0.0f, new Vector2(128),
                        _hexScaleV, SpriteEffects.None, 0.0f);
                    break;
                }
                else
                {
                    _spriteBatch.Draw(_damageTexture, characterScreenPos, null, Color.White, 0.0f, new Vector2(128),
                        _hexScaleV, SpriteEffects.None, 0.0f);
                    break;
                }
				
	            var tile = GetTileAtCoordinate(destinationPos);
	            if (tile.TerrainType != TerrainType.Ice && tile.TerrainType != TerrainType.ThinIce)
		            pushForce--;

				characterPos = destinationPos;
                destinationPos += direction;
            }
        }

        public void LosePotential(int potentialCost)
        {
            //throw new System.NotImplementedException();
        }

	    public List<HexCoordinate> GetVisibleAxisTilesInRange(Character objectCharacter, int range)
	    {
		    return _gameState.GetVisibleAxisTilesInRange(objectCharacter, range);
	    }

	    public List<HexCoordinate> GetVisibleAxisTilesInRangeIgnoreUnits(Character objectCharacter, int range)
		{
			return _gameState.GetVisibleAxisTilesInRangeIgnoreUnits(objectCharacter, range);
		}

		public List<HexCoordinate> GetVisibleTilesInRange(Character objectCharacter, int range)
		{
			return _gameState.GetVisibleTilesInRange(objectCharacter, range);

		}

		public List<HexCoordinate> GetVisibleTilesInRangeIgnoreUnits(Character objectCharacter, int range)
		{
			return _gameState.GetVisibleTilesInRangeIgnoreUnits(objectCharacter, range);
		}

		public List<HexCoordinate> GetTilesInRange(Character objectCharacter, int range)
		{
			return _gameState.GetTilesInRange(objectCharacter, range);
		}

	    public HexCoordinate GetNearestNeighbor(HexCoordinate start, HexCoordinate end)
	    {
		    return _gameState.GetNearestNeighbor(start, end);
	    }

        public bool IsHexPassable(HexCoordinate coordinate)
        {
            return _gameState.IsHexPassable(coordinate);
        }

        public bool IsTileEmpty(HexCoordinate coordinate)
        {
            return _gameState.IsTileEmpty(coordinate);
        }

	    public Tile GetTileAtCoordinate(HexCoordinate coordinate)
	    {
		    return _gameState.GetTileAtCoordinate(coordinate);
	    }

        #region HelperMethods

        private Vector2 GetHexScreenPosition(HexCoordinate coordinate)
	    {
		    var posX = _hexHalfSize * (_sqrt3 * coordinate.X + (_sqrt3 / 2 * coordinate.Z)) * _hexScale;
		    var posY = _hexHalfSize * 1.5f * coordinate.Z * _hexScale;

		    return new Vector2(posX, posY) + _screenCenter;
	    }

        #endregion

        public void NextTurn(){}

        public void GainPotential(int potentialGain = 1) { }

        #region IDisposable

        public void Dispose()
        {
            _spriteBatch?.Dispose();
        }

        #endregion
    }
}