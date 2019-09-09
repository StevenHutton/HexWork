using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using HexWork.Gameplay;
using HexWork.Gameplay.Actions;
using HexWork.Gameplay.Interfaces;
using HexWork.Interfaces;
using HexWork.Screens;
using MonoGameTestProject.Gameplay;

namespace HexWork
{
	/// <summary>
	/// This is the main type for your game.
	/// </summary>
	public class HexWork : Game
	{
		#region Attributes

		GraphicsDeviceManager _graphics;
		SpriteBatch _spriteBatch;
		private GameState _gameState;

		public readonly int ScreenHeight = 900;
		public readonly int ScreenWidth = 1600;

		private IScreenManager _screenManager;
		private IInputManager _inputManager;

		private HexAction _moveAction;
		private HexAction _moveActionEx;

		#region Gameplay

		private const int MonsterCount = 8;

		#endregion

		private Vector2 _screenCenterVector;

		private readonly float _sqrt3 = (float) Math.Sqrt(3.0);

		#endregion

		#region Properties

		public GameState GameState => _gameState;

		#endregion

		#region Methods

		#region Initialisation Methods

		public HexWork()
		{
			_graphics = new GraphicsDeviceManager(this)
			{
				PreferredBackBufferWidth = ScreenWidth,
				PreferredBackBufferHeight = ScreenHeight
			};
			_screenCenterVector = new Vector2((float) ScreenWidth / 2, (float) ScreenHeight / 2);

			_gameState = new GameState();
			GameStateManager.SetGameState(_gameState);

			_screenManager = new ScreenManager(this);
			_inputManager = new InputManager(this);

			Components.Add(_screenManager);
			Components.Add(_inputManager);

			Content.RootDirectory = "Content";
		}

		/// <summary>
		/// Allows the game to perform any initialization it needs to before starting to run.
		/// This is where it can query for any required services and load any non-graphic
		/// related content.  Calling base.Initialize will enumerate through any components
		/// and initialize them as well.
		/// </summary>
		protected override void Initialize()
		{
			this.IsMouseVisible = true;
			base.Initialize();

			_moveAction = new MoveAction("Move", GetDestinationTargetTiles) {Range = 0};
			_moveActionEx = new MoveAction("Nimble Move! (1)", GetDestinationTargetTiles)
				{PotentialCost = 1, Range = 2};

			var whirlWindTargetPattern = new TargetPattern(new HexCoordinate(1, 0, -1),
				new HexCoordinate(1, -1, 0),
				new HexCoordinate(0, -1, 1),
				new HexCoordinate(-1, 0, 1),
				new HexCoordinate(-1, 1, 0),
				new HexCoordinate(0, 1, -1));

			InitialiseEnemies();
			InitialiseHeroes();

			_screenManager.AddScreen(new BattleScreen(_screenManager));
		}

		/// <summary>
		/// LoadContent will be called once per game and is the place to load
		/// all of your content.
		/// </summary>
		protected override void LoadContent()
		{
			// Create a new SpriteBatch, which can be used to draw textures.
			_spriteBatch = new SpriteBatch(GraphicsDevice);
		}

		#endregion

		#region Create Characters

		private void InitialiseEnemies()
		{
			var zombieGrab = new HexAction(name: "Zombie Grab",
				statusEffect: new ImmobalisedEffect()
				{
					StatusEffectType = StatusEffectType.Rooted
				},
				combo: null,
				targetDelegate: GetValidTargetTilesNoLos)
			{
				Range = 1,
				Power = 10
			};

			var zombieBite = new HexAction(name: "Zombie Bite",
				combo: new ComboAction() {Power = 15},
				targetDelegate: GetValidTargetTilesNoLos)
			{
				Range = 1,
				Power = 10
			};

			var zombieKing = new Character("Zom-boy King", 160, 140, 2, 1)
			{
				MonsterType = MonsterType.ZombieKing
			};
			zombieKing.AddAction(_moveAction);
			zombieKing.AddAction(zombieGrab);
			zombieKing.AddAction(zombieBite);
			_gameState.Characters.Add(zombieKing);

			for (var i = 0; i < 5; i++)
			{
				var zombie = new Character($"Zom-boy {i}", 60, 100, 1, 0);
				zombie.AddAction(_moveAction);
				zombie.AddAction(zombieGrab);
				zombie.AddAction(zombieBite);

				_gameState.Characters.Add(zombie);
			}
		}

		private void InitialiseHeroes()
		{
			CreateMajin();

			CreateGunner();

			CreateNinja();

			CreateIronSoul();

			CreateBarbarian();

			_gameState.Characters = _gameState.Characters.OrderByDescending(c => c.TurnTimer).ToList();
		}

		private void CreateMajin()
		{
			var burningBolt = new HexAction("Fire Bolt",
				GetValidAxisTargetTilesLosIgnoreUnits,
				new DotEffect())
			{
				Range = 3
			};

			var linePattern = new TargetPattern(new HexCoordinate(0, 0),
				new HexCoordinate(1, 0),
				new HexCoordinate(-1, 0));

			var exBurningBoltAction = new HexAction("Fire Wall! (2)",
				GetValidAxisTargetTilesLosIgnoreUnits,
				new DotEffect(), null,
				linePattern)
			{
				PotentialCost = 2,
				Range = 3
			};

			var coldSnap = new HexAction("Cold Snap", GetValidAxisTargetTilesNoLos, new FreezeEffect())
			{
				Power = 20
			};

			var whirlWindTargetPattern = new TargetPattern(new HexCoordinate(0, 0, 0),
				new HexCoordinate(1, 0, -1),
				new HexCoordinate(1, -1, 0),
				new HexCoordinate(0, -1, 1),
				new HexCoordinate(-1, 0, 1),
				new HexCoordinate(-1, 1, 0),
				new HexCoordinate(0, 1, -1));

			var coldSnapEx = new HexAction("Absolute Zero (3)", GetValidTargetTilesNoLos, new FreezeEffect(), null,
				whirlWindTargetPattern)
			{
				Power = 30,
				PotentialCost = 3,
				Range = 0
			};

			//create majin hero
			var majinCharacter = new Character("Majin", 100, 100, 3, 5)
			{
				IsHero = true,
				MovementType =  MovementType.MoveThroughHeroes
			};
			majinCharacter.AddAction(_moveAction);
			majinCharacter.AddAction(_moveActionEx);
			majinCharacter.AddAction(burningBolt);
			majinCharacter.AddAction(exBurningBoltAction);
			majinCharacter.AddAction(coldSnap);
			majinCharacter.AddAction(coldSnapEx);

			_gameState.Characters.Add(majinCharacter);
			_gameState.Commander = majinCharacter;
		}

		private void CreateGunner()
		{
			var crackShotAction = new HexAction(name: "Crack Shot",
				statusEffect: new StatusEffect()
					{Name = "Marked", Duration = 0, StatusEffectType = StatusEffectType.Marked},
				targetDelegate: GetValidAxisTargetTilesNoLos)
			{
				Range = 3,
				Power = 10
			};

			var shovingSnipeAction = new PushAction(name: "Shoving Snipe",
				targetDelegate: GetValidAxisTargetTilesLos,
				combo: null)
			{
				Power = 5,
				Range = 5,
				PushForce = 1
			};

			var detonatingSnipeActionEx = new HexAction("Perfect Snipe! (1)",
				GetValidAxisTargetTilesLosIgnoreUnits,
				null,
				new ComboAction() {Power = 55})
			{
				PotentialCost = 1,
				Power = 5,
				Range = 5
			};

			var shovingSnipeActionEx = new PushAction("Shoving Snipe! (1)",
				GetValidTargetTilesLosIgnoreUnits,
				null, null)
			{
				PotentialCost = 1,
				Power = 5,
				Range = 5,
				PushForce = 2
			};

			//create gunner hero
			var gunnerCharacter = new Character("Gunner", 60, 100, 3, 4)
			{
				IsHero = true,
				MovementType = MovementType.MoveThroughHeroes
			};

			gunnerCharacter.AddAction(_moveAction);
			gunnerCharacter.AddAction(_moveActionEx);

			gunnerCharacter.AddAction(crackShotAction);
			gunnerCharacter.AddAction(shovingSnipeAction);

			gunnerCharacter.AddAction(detonatingSnipeActionEx);
			gunnerCharacter.AddAction(shovingSnipeActionEx);
			_gameState.Characters.Add(gunnerCharacter);
		}

		private void CreateNinja()
		{
			var shurikenPattern = new TargetPattern(new HexCoordinate(-1, 1), new HexCoordinate(0, -1),
				new HexCoordinate(1, 0));

			var shurikenHailAction = new HexAction("Shuriken",
				GetValidTargetTilesLos,
				new DotEffect()
				{
					Name = "Bleeding",
					Damage = 5,
					Duration = 3,
					StatusEffectType = StatusEffectType.Bleeding
				})
			{
				Range = 3
			};

			var shurikenHailActionEx = new HexAction("Shuriken Hail! (1)",
				GetValidTargetTilesLosIgnoreUnits,
				new DotEffect()
				{
					Name = "Bleeding",
					Damage = 5,
					Duration = 3,
					StatusEffectType = StatusEffectType.Bleeding
				},
				null, shurikenPattern)
			{
				PotentialCost = 1,
				Range = 3
			};

			//create ninja hero
			var ninjaCharacter = new Character("Ninja", 80, 80, 3, 4)
			{
				IsHero = true,
				MovementType = MovementType.MoveThroughHeroes
			};

			ninjaCharacter.AddAction(_moveAction);
			ninjaCharacter.AddAction(_moveActionEx);

			ninjaCharacter.AddAction(shurikenHailAction);
			ninjaCharacter.AddAction(shurikenHailActionEx);
            
			_gameState.Characters.Add(ninjaCharacter);
		}

		private void CreateIronSoul()
		{
			var whirlWindTargetPattern = new TargetPattern(new HexCoordinate(1, 0, -1),
				new HexCoordinate(1, -1, 0),
				new HexCoordinate(0, -1, 1),
				new HexCoordinate(-1, 0, 1),
				new HexCoordinate(-1, 1, 0),
				new HexCoordinate(0, 1, -1));

			var pushingFist = new PushAction("Pushing Fist", GetValidTargetTilesLos,
				null, new ComboAction())
			{
				Range = 1,
				Power = 10,
				PushForce = 1
			};

			var overwhelmingStrike = new PushAction("Overwhelming Strike", GetValidTargetTilesLos,
				null, new ComboAction())
			{
				Range = 1,
				Power = 10,
				PushForce = 3,
				PotentialCost = 0
			};

			var detonatingSlash =
				new HexAction("Detonating Strike", GetValidTargetTilesLos, null, new SpreadStatusCombo());
			detonatingSlash.Range = 1;

			var exDetonatingSlash =
				new HexAction("Massive Detonation! (1)", GetValidTargetTilesLos, null, new ExploderCombo()
				{
					Power = 25,
					Pattern = whirlWindTargetPattern
				})
				{
					Range = 1,
					PotentialCost = 1,
					Power = 25
				};

			//create Iron Soul hero
			var ironSoulCharacter = new Character("Iron Soul", 200, 120, 2, 3) 
			{
				IsHero = true,
				MovementType = MovementType.MoveThroughHeroes
			};
			ironSoulCharacter.AddAction(_moveAction);
			ironSoulCharacter.AddAction(_moveActionEx);
			ironSoulCharacter.AddAction(detonatingSlash);
			ironSoulCharacter.AddAction(exDetonatingSlash);
			ironSoulCharacter.AddAction(pushingFist);
			ironSoulCharacter.AddAction(overwhelmingStrike);
			_gameState.Characters.Add(ironSoulCharacter);
		}

		private void CreateBarbarian()
		{
			var burningStrike = new HexAction("Burning Strike", GetValidTargetTilesLos,
				new DotEffect())
			{
				Power = 20,
				Range = 1
			};

			var targetPattern =
				new TargetPattern(new HexCoordinate(0, 0), new HexCoordinate(1, 0), new HexCoordinate(2, 0));

			var burningStrikeEx = new LineAction("Burning Wave", GetValidTargetTilesLos,
				new DotEffect(), null, targetPattern)
			{
				Power = 15,
				Range = 1,
				PotentialCost = 1
			};

			var whirlWindTargetPattern = new TargetPattern(new HexCoordinate(1, 0, -1),
				new HexCoordinate(1, -1, 0),
				new HexCoordinate(0, -1, 1),
				new HexCoordinate(-1, 0, 1),
				new HexCoordinate(-1, 1, 0),
				new HexCoordinate(0, 1, -1));

			var whirlwindAttack = new HexAction("Spin Attack", GetValidTargetTilesLos, null, new ComboAction(),
				whirlWindTargetPattern)
			{
				Power = 15,
				PotentialCost = 0,
				Range = 0
			};

			var whirlwindAttackEx = new RepeatingAction("Whirlwind Attack (2)", GetValidTargetTilesLos, null, null,
				whirlWindTargetPattern)
			{
				Power = 10,
				PotentialCost = 2,
				Range = 0,
				AllySafe = true
			};


			//create Barbarian hero
			var barbarianCharacter = new Character("Barbarian", 150, 100, 3, 2)
			{
				IsHero = true,
				MovementType = MovementType.MoveThroughHeroes
			};
			barbarianCharacter.AddAction(_moveAction);
			barbarianCharacter.AddAction(_moveActionEx);
			barbarianCharacter.AddAction(burningStrike);
			barbarianCharacter.AddAction(burningStrikeEx);
			barbarianCharacter.AddAction(whirlwindAttack);
			barbarianCharacter.AddAction(whirlwindAttackEx);
			_gameState.Characters.Add(barbarianCharacter);
		}

		#endregion

		#endregion

		#region Main Loop

		/// <summary>
		/// Allows the game to run logic such as updating the world,
		/// checking for collisions, gathering input, and playing audio.
		/// </summary>
		/// <param name="gameTime">Provides a snapshot of timing values.</param>
		protected override void Update(GameTime gameTime)
		{
			base.Update(gameTime);
		}

		/// <summary>
		/// This is called when the game should draw itself.
		/// </summary>
		/// <param name="gameTime">Provides a snapshot of timing values.</param>
		protected override void Draw(GameTime gameTime)
		{
			GraphicsDevice.Clear(Color.CornflowerBlue);

			base.Draw(gameTime);
		}

		#endregion

		#region Targetting Methods

		public List<HexCoordinate> GetValidTargetTilesLos(Character objectCharacter, int range,
			IGameStateObject gameState)
		{
			return gameState.GetVisibleTilesInRange(objectCharacter, range);
		}

		public List<HexCoordinate> GetValidTargetTilesNoLos(Character objectCharacter, int range,
			IGameStateObject gameState)
		{
			return gameState.GetTilesInRange(objectCharacter, range);
		}

		public List<HexCoordinate> GetValidTargetTilesLosIgnoreUnits(Character objectCharacter, int range,
			IGameStateObject gameState)
		{
			return gameState.GetVisibleTilesInRangeIgnoreUnits(objectCharacter, range);
		}

		public List<HexCoordinate> GetValidAxisTargetTilesLos(Character objectCharacter, int range,
			IGameStateObject gameState)
		{
			return gameState.GetVisibleAxisTilesInRange(objectCharacter, range);
		}

		public List<HexCoordinate> GetValidAxisTargetTilesLosIgnoreUnits(Character objectCharacter, int range,
			IGameStateObject gameState)
		{
			return gameState.GetVisibleAxisTilesInRangeIgnoreUnits(objectCharacter, range);
		}

		public List<HexCoordinate> GetValidAxisTargetTilesNoLos(Character objectCharacter, int range,
			IGameStateObject gameState)
		{
			return gameState.GetAxisTilesInRange(objectCharacter, range);
		}

		public List<HexCoordinate> GetDestinationTargetTiles(Character objectCharacter, int range,
			IGameStateObject gameState)
		{
			return gameState.GetValidDestinations(objectCharacter, range);
		}

		#endregion
	}
}