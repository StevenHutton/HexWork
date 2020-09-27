using System;
using System.Collections.Generic;
using System.Linq;
using HexWork.Gameplay;
using HexWork.Gameplay.Actions;
using HexWork.Gameplay.GameObject;
using HexWork.Gameplay.GameObject.Characters;
using HexWork.Gameplay.Interfaces;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace HexWork.UI
{
    public class PreviewGameStateProxy : IGameStateObject, IDisposable
    {
        public IGameStateObject gameState;

	    private SpriteBatch _spriteBatch;

        private SpriteFont _font;
		
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
        private readonly Vector2 _iconOrigin;

        private HexWork _hexGame;

		#region Properties

        public BoardState CurrentGameState  => gameState.CurrentGameState;

        public IEnumerable<TileEffect> TileEffects => CurrentGameState.TileEffects;

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
            _iconOrigin = new Vector2(_fireIconTexture.Width/2, _fireIconTexture.Height/2);
            
            _font = game.Content.Load<SpriteFont>("Nunito-Bold");

            var data = new[] { Color.White, Color.White, Color.White, Color.White };
			_blankTexture = new Texture2D(game.GraphicsDevice, 2, 2);
	        _blankTexture.SetData(data);

            _hexGame = (HexWork)game;
        }

	    public Dictionary<HexCoordinate, int> GetValidDestinations(Character objectCharacter)
	    {
		    var destinations =  gameState.GetValidDestinations(objectCharacter);

            foreach (var coord in destinations)
            {
                var postion = coord.Key;
                var movementCost = coord.Value.ToString();
                
                _spriteBatch.DrawString(_font, movementCost, GetHexScreenPosition(postion), Color.White, 0.0f, _font.MeasureString(movementCost) / 2, Vector2.One, SpriteEffects.None, 0.0f);
            }
            
            return destinations;
        }

	    public bool IsValidDestination(Character objectCharacter, HexCoordinate targetPosition)
        {
			var isValid = GetValidDestinations(objectCharacter).Keys.Contains(targetPosition);

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
		    var isValid = gameState.IsValidTarget(objectCharacter, targetPosition, range, targetDelegate);

		    if (!isValid)
		    {
			    var pos = GetHexScreenPosition(targetPosition);

			    _spriteBatch.Draw(_invalidTexture, pos, null, Color.White, 0.0f, _hexCenter, _hexScaleV, SpriteEffects.None, 0.0f);
		    }

		    return isValid;
	    }

		public List<HexCoordinate> FindShortestPath(HexCoordinate startPosition, HexCoordinate destination, MovementType movementType = MovementType.NormalMove, MovementSpeed speed = MovementSpeed.Normal)
	    {
			var path = gameState.FindShortestPath(startPosition, destination, movementType, speed);
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
		    return gameState.GetAxisTilesInRange(objectCharacter, range);
	    }

	    /// <summary>
		/// Intentionally does nothing
		/// </summary>
		/// <param name="character"></param>
		/// <param name="targetPosition"></param>
        public void MoveEntityTo(HexGameObject entity, HexCoordinate targetPosition)
        {
            var path = gameState.FindShortestPath(entity.Position, targetPosition);
	        ResolveTileEffects(entity, path);
            ResolveTerrainEffects(entity, path);
        }

        public void TeleportEntityTo(HexGameObject entity, HexCoordinate position)
        {
	        ResolveTileEffects(entity, new List<HexCoordinate> { position});
			ResolveTerrainEffects(entity, new List<HexCoordinate>{position});
		}

	    private void ResolveTileEffects(HexGameObject entity, List<HexCoordinate> path)
	    {
		    //don't count terrain effects from a tile you're standing. We don't punish players for e.g. leaving lava.
		    if (path.First() == entity.Position)
		    {
			    path.Remove(entity.Position);
		    }

		    foreach (var tile in path)
		    {
			    var tileEffect = TileEffects.FirstOrDefault(data => data.Position == tile);

			    if (tileEffect == null)
				    continue;

                ResolveTileEffect(tileEffect, entity);
            }
		}

        public void ResolveTileEffect(TileEffect effect, HexGameObject entity = null)
        {
            effect.TriggerEffect(this, entity);
        }

        private void ResolveTerrainEffects(HexGameObject entity, List<HexCoordinate> path)
        {
            if (path == null || path.Count == 0)
                return;

	        //don't count terrain effects from a tile you're standing. We don't punish players for e.g. leaving lava.
	        if (path.First() == entity.Position)
	        {
		        path.Remove(entity.Position);
	        }

			foreach (var position in path)
            {
                ResolveTerrainEffect(entity, position);
            }
        }

        private void ResolveTerrainEffect(HexGameObject entity, HexCoordinate position)
        {
            var tile = gameState.GetTileAtCoordinate(position);
            switch (tile.TerrainType)
            {
                case TerrainType.Ground:
                    break;
                case TerrainType.Water:
                    break;
                case TerrainType.Lava:
                    ApplyStatus(entity, new StatusEffect{StatusEffectType = StatusEffectType.Burning});
                    break;
                case TerrainType.Ice:
                    break;
                case TerrainType.ThinIce:
                    break;
                case TerrainType.Snow:
                    break;
                case TerrainType.Sand:
                    break;
                case TerrainType.Pit:
                    break;
                case TerrainType.Wall:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        /// <summary>
        /// intentionally does nothing
        /// </summary>
        /// <param name="action"></param>
        public void NotifyAction(HexAction action, HexGameObject entity)
        {

        }

        public HexGameObject GetEntityAtCoordinate(HexCoordinate coordinate)
        {
	        return gameState.GetEntityAtCoordinate(coordinate);
        }

        public int ApplyDamage(HexGameObject entity, int power, string message = null)
        {
	        var position = GetHexScreenPosition(entity.Position);
            _spriteBatch.Draw(_damageTexture, position, null, Color.Red, 0.0f, new Vector2(128), _hexScaleV, SpriteEffects.None, 0.0f );
	        return power;
        }

        public void CheckDied(HexGameObject entity)
        { }

        public void ApplyHealing(Character character, int power)
        {
            var position = GetHexScreenPosition(character.Position);

            _spriteBatch.Draw(_healingTexture, position, null, Color.Green, 0.0f, new Vector2(128), _hexScaleV,
                SpriteEffects.None, 0.0f);
        }

        public void ApplyStatus(HexGameObject entity, StatusEffect effect)
        {
            if (effect == null) return;

            Texture2D statusTexture;
            switch (effect.StatusEffectType)
            {
                case StatusEffectType.Burning:
                    statusTexture = _fireIconTexture;
                    break;
                case StatusEffectType.Frozen:
                    statusTexture = _hexGame.Content.Load<Texture2D>("FrozenIcon");
                    break;
                case StatusEffectType.Rooted:
                    statusTexture = _hexGame.Content.Load<Texture2D>("StopIcon");
                    break;
                case StatusEffectType.Bleeding:
                    statusTexture = _hexGame.Content.Load<Texture2D>("Blood");
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

	        var position = GetHexScreenPosition(entity.Position);
	        _spriteBatch.Draw(statusTexture, 
		        position,
		        null,
		        Color.White,
		        0.0f,
		        _iconOrigin, 
		        new Vector2(0.3f), 
		        SpriteEffects.None, 
		        0.0f);
		}

        public void CreateTileEffect(HexCoordinate location, TileEffect effect)
        {
            Texture2D statusTexture = _hexGame.Content.Load<Texture2D>(effect.Name);

            if (statusTexture == null)
                return;

            var position = GetHexScreenPosition(location);

            var width = (float)statusTexture.Width;
            var height = (float)statusTexture.Height;
            var scaleFactor = 256.0f / height * 0.35f;

            var origin = new Vector2(width / 2, height - (width / 2));
            _spriteBatch.Draw(statusTexture,
                position,
                null,
                Color.White,
                0.0f,
                origin,
                new Vector2(scaleFactor), 
                SpriteEffects.None,
                0.0f);
        }

        public int ApplyCombo(HexGameObject entity, DamageComboAction combo)
        {
            return 0;
        }

        public void ApplyPush(HexGameObject entity, HexCoordinate direction, int pushForce = 0)
        {
	        var characterPos = entity.Position;
	        var destinationPos = characterPos + direction;

            while (pushForce > 0)
            {
                var characterScreenPos = GetHexScreenPosition(characterPos);
                var destinationScreenPos = GetHexScreenPosition(destinationPos);
                var pushVector = (destinationScreenPos - characterScreenPos);
                var rotation = (float)Math.Atan2(pushVector.Y, pushVector.X);
                var drawPosition = characterScreenPos + (pushVector * 0.8f);

                if (!IsHexInMap(destinationPos))
                {
                    _spriteBatch.Draw(_damageTexture, characterScreenPos, null, Color.White, 0.0f, new Vector2(128),
                        _hexScaleV, SpriteEffects.None, 0.0f);
                    pushForce = 0;
                    break;
                }
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
		    return gameState.GetVisibleAxisTilesInRange(objectCharacter, range);
	    }

	    public List<HexCoordinate> GetVisibleAxisTilesInRangeIgnoreUnits(Character objectCharacter, int range)
		{
			return gameState.GetVisibleAxisTilesInRangeIgnoreUnits(objectCharacter, range);
		}

		public List<HexCoordinate> GetVisibleTilesInRange(Character objectCharacter, int range)
		{
			return gameState.GetVisibleTilesInRange(objectCharacter, range);

		}

		public List<HexCoordinate> GetVisibleTilesInRangeIgnoreUnits(Character objectCharacter, int range)
		{
			return gameState.GetVisibleTilesInRangeIgnoreUnits(objectCharacter, range);
		}

		public List<HexCoordinate> GetTilesInRange(Character objectCharacter, int range)
		{
			return gameState.GetTilesInRange(objectCharacter, range);
		}

        public bool IsHexPassable(HexCoordinate coordinate)
        {
            return gameState.IsHexPassable(coordinate);
        }

        public bool IsTileEmpty(HexCoordinate coordinate)
        {
            return gameState.IsTileEmpty(coordinate);
        }

        public bool IsHexInMap(HexCoordinate coordinate)
        {
            return gameState.IsHexInMap(coordinate);
        }

        public TileEffect GetTileEffectAtCoordinate(HexCoordinate targetPosition)
        {
            return gameState.GetTileEffectAtCoordinate(targetPosition);
        }

        public Tile GetTileAtCoordinate(HexCoordinate coordinate)
	    {
		    return gameState.GetTileAtCoordinate(coordinate);
	    }

        #region HelperMethods

        private Vector2 GetHexScreenPosition(HexCoordinate coordinate)
	    {
		    var posX = _hexHalfSize * (_sqrt3 * coordinate.X + (_sqrt3 / 2 * coordinate.Z)) * _hexScale;
		    var posY = _hexHalfSize * 1.5f * coordinate.Z * _hexScale;

		    return new Vector2(posX, posY) + _screenCenter;
	    }

        public int GetPathLengthToTile(Character objectCharacter, HexCoordinate destination)
        {
            return gameState.GetPathLengthToTile(objectCharacter, destination);
        }

        public void RemoveTileEffect(TileEffect effect)
        { }

        #endregion

        public void NextTurn(Character activeCharacter) { }

        public void GainPotential(int potentialGain = 1) { }

        #region IDisposable

        public void Dispose()
        {
            _spriteBatch?.Dispose();
        }

        public List<HexCoordinate> GetWalkableAdjacentTiles(HexCoordinate position, MovementType movementType)
        {
            return gameState.GetWalkableAdjacentTiles(position, movementType);
        }

        public void SpawnCharacter(Character character)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}