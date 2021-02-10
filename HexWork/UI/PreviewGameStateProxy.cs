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
    public class PreviewRulesProvider : IRulesProvider, IDisposable
    {
        public IRulesProvider rulesProvider;

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

	    public void SpriteBatchBegin()
	    {
			_spriteBatch.Begin();
	    }
	    public void SpriteBatchEnd()
	    {
		    _spriteBatch.End();
		}

        public PreviewRulesProvider(HexWork game)
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

        public BoardState CompleteAction(BoardState state, Guid characterId, HexAction action) { return state; }

        public BoardState AddEntity(BoardState state, HexGameObject entity) { return state; }

	    /// <summary>
		/// Intentionally does nothing
		/// </summary>
		/// <param name="character"></param>
		/// <param name="targetPosition"></param>
        public BoardState MoveEntity(BoardState state, Guid entityId, List<HexCoordinate> path)
        {
            foreach (var hex in path)
            {
                _spriteBatch.Draw(_blankTexture, GetHexScreenPosition(hex), null,
                    Color.White, 0.0f, new Vector2(1.0f, 1.0f),
                    6.0f, SpriteEffects.None, 0.0f);
            }
            var newState = state.Copy();

            foreach (var coordinate in path)
            {
                var entity = newState.Entities.First(ent => ent.Id == entityId);
                entity.MoveTo(coordinate);                

                newState = ResolveTileEffect(newState, coordinate);
                newState = ResolveTerrainEffect(newState, coordinate);
            }

            return newState;
        }

        public BoardState TeleportEntityTo(BoardState state, Guid entityId, HexCoordinate position)
        {
	        ResolveTileEffect(state, position);
			ResolveTerrainEffect(state, position); 
            return state;
        }

        public BoardState ResolveTileEffect(BoardState state, HexCoordinate position)
        {
            var tileEffect = state.TileEffects.FirstOrDefault(data => data.Position == position);

            if (tileEffect == null)
                return state;

            return tileEffect.TriggerEffect(state, this).Result;
        }

        public BoardState ResolveTerrainEffect(BoardState state, HexCoordinate position)
        {
            var newState = state.Copy();
            var character = newState.Characters.FirstOrDefault(ch => ch.Position == position);
            if (character == null)
                return state;

            var tile = state[position];
            switch (tile.TerrainType)
            {
                case TerrainType.Ground:
                    break;
                case TerrainType.Water:
                    break;
                case TerrainType.Lava:
                    ApplyStatus(state, character.Id, Element.Fire);
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
            return state;
        }

        public BoardState ApplyDamage(BoardState state, Guid entityId, int power)
        {
            if (power == 0)
                return state;
            var entity = state.GetEntityById(entityId);
            if (entity == null)
                return state;

	        var position = GetHexScreenPosition(entity.Position);
            _spriteBatch.Draw(_damageTexture, position, null, Color.Red, 0.0f, new Vector2(128), _hexScaleV, SpriteEffects.None, 0.0f );
	        return state;
        }

        public BoardState CheckDied(BoardState state, Guid entityId)
        { return state; }

        public BoardState ApplyHealing(BoardState state, Guid characterId, int power)
        {
            var character = state.GetCharacterById(characterId);

            var position = GetHexScreenPosition(character.Position);

            _spriteBatch.Draw(_healingTexture, position, null, Color.Green, 0.0f, new Vector2(128), _hexScaleV,
                SpriteEffects.None, 0.0f);
            return state;
        }

        public BoardState ApplyStatus(BoardState state, Guid entityId, Element effect)
        {
            if (effect == Element.None)
                return state;

            var entity = state.GetEntityById(entityId);

            Texture2D statusTexture;
            switch (effect)
            {
                case Element.Fire:
                    statusTexture = _fireIconTexture;
                    break;
                case Element.Ice:
                    statusTexture = _hexGame.Content.Load<Texture2D>("FrozenIcon");
                    break;
                case Element.Earth:
                    statusTexture = _hexGame.Content.Load<Texture2D>("StopIcon");
                    break;
                case Element.Wind:
                    statusTexture = _hexGame.Content.Load<Texture2D>("Wind");
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
            return state;
        }

        public BoardState CreateTileEffect(BoardState state, TileEffect effect, HexCoordinate location)
        {
            Texture2D statusTexture = _hexGame.Content.Load<Texture2D>(effect.Name);

            if (statusTexture == null)
                return state;

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

            return state;
        }

        public BoardState CreateTileEffect(BoardState state, Element effect, HexCoordinate location)
        {
            Texture2D statusTexture = _hexGame.Content.Load<Texture2D>(effect.ToString());

            if (statusTexture == null)
                return state;

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

            return state;
        }

        public BoardState ApplyCombo(BoardState state, Guid entityId, DamageComboAction combo, out int damage)
        {
            damage = 0;
            return state;
        }

        public BoardState ApplyPush(BoardState state, Guid entityId, HexCoordinate direction, int pushForce = 0)
        {
            var entity = state.GetEntityById(entityId);
            if (entity == null)
                return state;

	        var characterPos = entity.Position;
	        var destinationPos = characterPos + direction;

            while (pushForce > 0)
            {
                var characterScreenPos = GetHexScreenPosition(characterPos);
                var destinationScreenPos = GetHexScreenPosition(destinationPos);
                var pushVector = (destinationScreenPos - characterScreenPos);
                var rotation = (float)Math.Atan2(pushVector.Y, pushVector.X);
                var drawPosition = characterScreenPos + (pushVector * 0.8f);

                if (!BoardState.IsHexInMap(destinationPos))
                {
                    _spriteBatch.Draw(_damageTexture, characterScreenPos, null, Color.White, 0.0f, new Vector2(128),
                        _hexScaleV, SpriteEffects.None, 0.0f);
                    pushForce = 0;
                    break;
                }
                if (BoardState.IsWalkableAndEmpty(state, destinationPos))
                {
                    _spriteBatch.Draw(_arrowTexture, drawPosition, null, Color.White, rotation, new Vector2(64, 64), _hexScale, SpriteEffects.None, 0.0f);
                }
                else if (!BoardState.IsTileEmpty(state, destinationPos))
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
				
	            var tile = state[destinationPos];
	            if (tile.TerrainType != TerrainType.Ice && tile.TerrainType != TerrainType.ThinIce)
		            pushForce--;

				characterPos = destinationPos;
                destinationPos += direction;
            }
            return state;
        }

        public BoardState LosePotential(BoardState state, int potentialCost)
        {
            return state;
        }

        #region HelperMethods

        private Vector2 GetHexScreenPosition(HexCoordinate coordinate)
	    {
		    var posX = _hexHalfSize * (_sqrt3 * coordinate.X + (_sqrt3 / 2 * coordinate.Z)) * _hexScale;
		    var posY = _hexHalfSize * 1.5f * coordinate.Z * _hexScale;

		    return new Vector2(posX, posY) + _screenCenter;
	    }

        public BoardState RemoveTileEffect(BoardState state, Guid effectId)
        { return state; }

        #endregion

        public BoardState NextTurn(BoardState state, Guid activeCharacterId) { return state; }
        
        public BoardState GainPotential(BoardState state, int potentialGain = 1) { return state; }

        #region IDisposable

        public void Dispose()
        {
            _spriteBatch?.Dispose();
        }

        public void SpawnCharacter(Character character)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}