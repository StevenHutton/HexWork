using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Channels;
using HexWork.Gameplay.Actions;
using HexWork.Gameplay.Interfaces;
using HexWork.UI;
using Microsoft.Xna.Framework;
using MonoGameTestProject.Gameplay;

namespace HexWork.Gameplay
{
    public enum GamePhase
    {
        Playing,
        EnemyTurn,
        GameOver
    }


	#region Event Args

	public class MessageEventArgs : EventArgs
    {
        public MessageEventArgs() { }

        public MessageEventArgs(string message)
        {
            Message = message;
        }

        public MessageEventArgs(string message, Character targetCharacter)
        {
            Message = message;
            Character = targetCharacter;
        }

        public string Message;
        public Character Character;
    }

	public class MoveEventArgs : EventArgs
	{
		public Guid ActiveCharacterId;
		public List<HexCoordinate> Path;
	}

    public class InteractionRequestEventArgs : EventArgs
    {
        public Guid ActiveCharacterId;

        public Guid TargetCharacterId;

        public HexCoordinate TargetPosition;
        

    }

    public class SpawnChracterEventArgs
    {
        public Guid CharacterId;

        public HexCoordinate TargetPosition;

        public MonsterType MonsterType;

        public int MaxHealth;
    }

    public class EndTurnEventArgs : EventArgs
    {
        public List<Character> InitativeOrder { get; set; }

        public EndTurnEventArgs(List<Character> initList)
        {
            InitativeOrder = initList;
        }
    }

    public class DamageTakenEventArgs : EventArgs
    {
        public Guid TargetCharacterId;

        public int DamageTaken;
    }

    public class ActionEventArgs : EventArgs
    {
        public HexAction Action;
    }

    public class StatusEventArgs : EventArgs
    {
        public Guid TargetCharacterId;

        public StatusEffect StatusEffect;

        public StatusEventArgs(Guid targetId, StatusEffect effect)
        {
            TargetCharacterId = targetId;
            StatusEffect = effect;
        }
    }

    public class ComboEventArgs : EventArgs
    {
        public Guid TargetCharacterId;

        public ComboAction ComboEffect;

        public ComboEventArgs(Guid targetId, ComboAction effect)
        {
            TargetCharacterId = targetId;
            ComboEffect = effect;
        }
    }

    public class PotentialEventArgs : EventArgs
    {
        public int PotentialChange;

        public PotentialEventArgs(int potentialChange)
        {
            PotentialChange = potentialChange;
        }
    }

    #endregion

    public class GameState : IGameStateObject
    {
        #region Attributes

        private readonly HexGrid _map = new HexGrid();

        private List<Character> _characters = new List<Character>();

        /// <summary>
        /// The character who currently has initiative
        /// </summary>
        private Character _activeCharacter = null;

        #region Gameplay Actions

        private HexAction _moveAction;
        private HexAction _moveActionEx;

        private HexAction _zombieGrab;

        private HexAction _zombieBite;

        #endregion 

        #endregion

        #region Events

        public event EventHandler<MoveEventArgs> CharacterMoveEvent;
        
        public event EventHandler<SpawnChracterEventArgs> SpawnCharacterEvent;

        public event EventHandler<DamageTakenEventArgs> TakeDamageEvent;

        public event EventHandler<InteractionRequestEventArgs> CharacterDiedEvent; 
        
        public event EventHandler<EndTurnEventArgs> EndTurnEvent;

        public event EventHandler<ActionEventArgs> ActionEvent;

        public event EventHandler<ComboEventArgs> ComboEvent;

        public event EventHandler<StatusEventArgs> StatusAppliedEvent;

        public event EventHandler<StatusEventArgs> StatusRemovedEvent;

        public event EventHandler<PotentialEventArgs> PotentialChangeEvent;

        public event EventHandler<MessageEventArgs> MessageEvent;

        public event EventHandler<MessageEventArgs> GameOverEvent;

        #endregion

        #region Properties

        public Dictionary<HexCoordinate, Tile> Map
        {
            get => _map.Map;
        }

        public List<Character> Characters
        {
            get => _characters;
            set => _characters = value;
        }

        public IEnumerable<Character> Heroes
        {
            get => _characters.Where(character => character.IsHero && character.IsAlive);
        }

        public IEnumerable<Character> Enemies
        {
            get => _characters.Where(character => !character.IsHero && character.IsAlive);
        }

        /// <summary>
        ///The character who currently has initiative
        /// </summary>
        public Character ActiveCharacter
        {
            get => _activeCharacter;
        }

        /// <summary>
        /// 
        /// </summary>
        public Character Commander { get; set; }

        public GamePhase Phase { get; set; }

        public int TeamSize { get; set; } = 5;

        public int MaxPotential { get; set; } = 0;

        public int Potential { get; set; } = 0;

        #endregion

        #region Methods

        #region Initialisation

        public GameState()
        {
            _moveAction = new MoveAction("Move", TargettingHelper.GetDestinationTargetTiles) { Range = 0 };
            _moveActionEx = new MoveAction("Nimble Move! (1)", TargettingHelper.GetDestinationTargetTiles)
                { PotentialCost = 1, Range = 2 };

            _zombieGrab = new HexAction(name: "Zombie Grab",
                statusEffect: new ImmobalisedEffect()
                {
                    StatusEffectType = StatusEffectType.Rooted
                },
                combo: null,
                targetDelegate: TargettingHelper.GetValidTargetTilesNoLos)
            {
                Range = 1,
                Power = 10
            };

            _zombieBite = new HexAction(name: "Zombie Bite",
                combo: new ComboAction() { Power = 15 },
                targetDelegate: TargettingHelper.GetValidTargetTilesNoLos)
            {
                Range = 1,
                Power = 10
            };

            InitialiseEnemies();
            InitialiseHeroes();
        }
        
        public void StartGame()
        {
            foreach (var character in _characters.Where(c => !c.IsHero))
            {
                var coordinate = _map.GetRandomCoordinateInMap();

                //one unit per tile and only deploy to walkable spaces.
                while (Characters.Select(cha => cha.Position).Contains(coordinate) || !_map.Map[coordinate].IsWalkable || !IsInEnemySpawnArea(coordinate))
                {
                    coordinate = _map.GetRandomCoordinateInMap();
                }

                character.SpawnAt(coordinate);
                SpawnCharacterEvent?.Invoke(this, new SpawnChracterEventArgs() { CharacterId = character.Id, TargetPosition = coordinate });
            }

            var spawnPoint = new HexCoordinate(-2, -2, 4);

            while (Characters.Any(hero => hero.IsHero && !hero.IsAlive))
            {
                var position = spawnPoint;

                var validTile = IsHexPassable(position);

                while (!validTile)
                {
                    position = GetNearestEmptyNeighbourRecursive(position, 9);

                    //tiles are only valid so long as they're walkable and have at least one passable neighbor.
                    validTile = IsHexPassable(position) &&
                                _map.GetNeighborCoordinates(position).Any(IsHexPassable);
                }
                SpawnHero(position);
            }

            MaxPotential = Commander.Potential;
            Potential = 0;

            NextTurn();
        }

        #region Create Characters

        private void InitialiseEnemies()
        {
            var zombieKing = new Character("Zom-boy King", 160, 140, 2, 1)
            {
                MonsterType = MonsterType.ZombieKing
            };
            zombieKing.AddAction(_moveAction);
            zombieKing.AddAction(_zombieGrab);
            zombieKing.AddAction(_zombieBite);
            Characters.Add(zombieKing);

            for (var i = 0; i < 4; i++)
            {
                var zombie = new Character($"Zom-boy {i}", 60, 100, 1, 0);
                zombie.AddAction(_moveAction);
                zombie.AddAction(_zombieGrab);
                zombie.AddAction(_zombieBite);

                Characters.Add(zombie);
            }
        }

        public Character CreateZombie()
        {
            var zombie = new Character($"Zom-boy Summon", 60, 100, 1, 0);
            zombie.AddAction(_moveAction);
            zombie.AddAction(_zombieGrab);
            zombie.AddAction(_zombieBite);

            return zombie;
        }

        private void InitialiseHeroes()
        {
            CreateMajin();

            CreateGunner();

            CreateNinja();

            CreateIronSoul();

            CreateBarbarian();

            Characters = Characters.OrderByDescending(c => c.TurnTimer).ToList();
        }

        private void CreateMajin()
        {
            var burningBolt = new HexAction("Fire Bolt",
                TargettingHelper.GetValidAxisTargetTilesLos,
                new DotEffect())
            {
                Range = 3
            };

            var linePattern = new TargetPattern(new HexCoordinate(0, 0),
                new HexCoordinate(1, 0),
                new HexCoordinate(-1, 0));

            var exBurningBoltAction = new HexAction("Fire Wall! (2)",
                TargettingHelper.GetValidAxisTargetTilesLosIgnoreUnits,
                new DotEffect(), null,
                linePattern)
            {
                PotentialCost = 2,
                Range = 3
            };

            var lightningBolt = new HexAction("Lightning Bolt (1)", TargettingHelper.GetValidAxisTargetTilesLosIgnoreUnits, null, new SpreadStatusCombo())
            {
                Range = 3,
                Power = 15,
                PotentialCost = 1
            };

            var whirlWindTargetPattern = new TargetPattern(new HexCoordinate(1, 0, -1),
                new HexCoordinate(1, -1, 0),
                new HexCoordinate(0, -1, 1),
                new HexCoordinate(-1, 0, 1),
                new HexCoordinate(-1, 1, 0),
                new HexCoordinate(0, 1, -1));

            //create majin hero
            var majinCharacter = new Character("Majin", 100, 100, 3, 5)
            {
                IsHero = true,
                MovementType = MovementType.MoveThroughHeroes
            };
            majinCharacter.AddAction(_moveAction);
            majinCharacter.AddAction(_moveActionEx);
            majinCharacter.AddAction(burningBolt);
            majinCharacter.AddAction(exBurningBoltAction);
            majinCharacter.AddAction(lightningBolt);

            Characters.Add(majinCharacter);
            Commander = majinCharacter;
        }

        private void CreateGunner()
        {
            var shovingSnipeAction = new PushAction(name: "Shoving Snipe",
                targetDelegate: TargettingHelper.GetValidAxisTargetTilesLos,
                combo: null)
            {
                Power = 5,
                Range = 5,
                PushForce = 1
            };

            var detonatingSnipeActionEx = new HexAction("Perfect Snipe! (1)",
                TargettingHelper.GetValidAxisTargetTilesLos,
                null,
                new ComboAction() { Power = 55 })
            {
                PotentialCost = 1,
                Power = 5,
                Range = 5
            };

            //create gunner hero
            var gunnerCharacter = new Character("Gunner", 60, 100, 3, 4)
            {
                IsHero = true,
                MovementType = MovementType.MoveThroughHeroes
            };

            gunnerCharacter.AddAction(_moveAction);
            gunnerCharacter.AddAction(_moveActionEx);

            gunnerCharacter.AddAction(shovingSnipeAction);
            gunnerCharacter.AddAction(detonatingSnipeActionEx);
            Characters.Add(gunnerCharacter);
        }

        private void CreateNinja()
        {
            var shurikenPattern = new TargetPattern(new HexCoordinate(-1, 1), new HexCoordinate(0, -1),
                new HexCoordinate(1, 0));

            var shurikenHailAction = new HexAction("Shuriken",
                TargettingHelper.GetValidTargetTilesLos,
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
                TargettingHelper.GetValidTargetTilesLosIgnoreUnits,
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

            Characters.Add(ninjaCharacter);
        }

        private void CreateIronSoul()
        {
            var pushingFist = new PushAction("Pushing Fist", TargettingHelper.GetValidTargetTilesLos)
            {
                Range = 1,
                Power = 10,
                PushForce = 2
            };

            var overwhelmingStrike = new PushAction("Overwhelming Strike! (1)", TargettingHelper.GetValidTargetTilesLos,
                null, new StatusCombo()
                {
                    Effect = new ImmobalisedEffect()
                    {
                        StatusEffectType = StatusEffectType.Rooted
                    }
                })
            {
                Range = 1,
                Power = 10,
                PushForce = 3,
                PotentialCost = 1
            };

            var vampiricStrike = new VampiricAction("Vampiric Strike", TargettingHelper.GetValidAxisTargetTilesLos)
            {
                Range = 1,
            };

            //create Iron Soul hero
            var ironSoulCharacter = new Character("Iron Soul", 200, 120, 2, 3)
            {
                IsHero = true,
                MovementType = MovementType.MoveThroughHeroes
            };
            ironSoulCharacter.AddAction(_moveAction);
            ironSoulCharacter.AddAction(_moveActionEx);
            ironSoulCharacter.AddAction(pushingFist);
            ironSoulCharacter.AddAction(overwhelmingStrike);
            ironSoulCharacter.AddAction(vampiricStrike);

            //ironSoulCharacter.AddAction(detonatingSlash);
            //ironSoulCharacter.AddAction(exDetonatingSlash);
            Characters.Add(ironSoulCharacter);
        }

        private void CreateBarbarian()
        {
            //var targetPattern =
            //    new TargetPattern(new HexCoordinate(0, 0), new HexCoordinate(1, 0), new HexCoordinate(2, 0));
            //var burningStrikeEx = new LineAction("Burning Wave", GetValidTargetTilesLos,
            //	new DotEffect(), null, targetPattern)
            //{
            //	Power = 15,
            //	Range = 1,
            //	PotentialCost = 1
            //};

            //var burningStrike = new HexAction("Burning Strike", GetValidTargetTilesLos,
            //	new DotEffect())
            //{
            //	Power = 20,
            //	Range = 1
            //};

            var whirlWindTargetPattern = new TargetPattern(new HexCoordinate(1, 0, -1),
                new HexCoordinate(1, -1, 0),
                new HexCoordinate(0, -1, 1),
                new HexCoordinate(-1, 0, 1),
                new HexCoordinate(-1, 1, 0),
                new HexCoordinate(0, 1, -1));

            var detonatingSlash =
              new HexAction("Detonating Strike! (1)", TargettingHelper.GetValidTargetTilesLos, null, new SpreadStatusCombo() { AllySafe = false }) { Range = 1, PotentialCost = 1 };

            var exDetonatingSlash =
            new HexAction("Massive Detonation! (1)", TargettingHelper.GetValidTargetTilesLos, null,
              new ExploderCombo
              {
                  Power = 25,
                  Pattern = whirlWindTargetPattern
              })
            {
                Range = 1,
                PotentialCost = 1,
                Power = 25
            };


            var whirlwindAttack = new HexAction("Spin Attack", TargettingHelper.GetValidTargetTilesLos, null, new ComboAction(),
                whirlWindTargetPattern)
            {
                Power = 15,
                PotentialCost = 0,
                Range = 0
            };

            var whirlwindAttackEx = new RepeatingAction("Whirlwind Attack (2)", TargettingHelper.GetValidTargetTilesLos, null, null,
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
            //barbarianCharacter.AddAction(burningStrike);
            //barbarianCharacter.AddAction(burningStrikeEx);
            barbarianCharacter.AddAction(whirlwindAttack);
            barbarianCharacter.AddAction(detonatingSlash);
            barbarianCharacter.AddAction(exDetonatingSlash);
            //barbarianCharacter.AddAction(whirlwindAttackEx);
            Characters.Add(barbarianCharacter);
        }

        #endregion

        #endregion

        public void Update(GameTime gameTime)
        {
            if (Phase != GamePhase.EnemyTurn) return;

            if (_activeCharacter.IsHero)
            {
                Phase = GamePhase.Playing;
                return;
            }

            if (!_activeCharacter.IsAlive)
            {
                NextTurn();
                return;
            }

            //if they can move take their actions and end turn
            if (!_activeCharacter.HasActed)
            {
                switch (_activeCharacter.MonsterType)
                {
                    case MonsterType.Zombie:
                        ZombieTurn(_activeCharacter);
                        break;
                    case MonsterType.ZombieKing:
                        ZombieKingTurn(_activeCharacter);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                NextTurn();
            }
        }

        public void NextTurn()
        {
            //if we have an active character then update all the initiative values
            if (_activeCharacter != null)
            {
                _activeCharacter.EndTurn();
                var deltaTime = _activeCharacter.TurnTimer;
                foreach (var character in _characters)
                {
                    character.TurnTimer -= deltaTime;
                }

                _activeCharacter.TurnTimer += _activeCharacter.TurnCooldown;

                //also apply any status effects for the active character that trigger at the end of the turn.
                foreach (var statusEffect in _activeCharacter.StatusEffects.ToList())
                {
                    statusEffect.EndTurn(_activeCharacter, this);

                    //if the effect is expired remove it 
                    if (statusEffect.IsExpired && _activeCharacter.StatusEffects.Contains(statusEffect))
                    {
                        _activeCharacter.StatusEffects.Remove(statusEffect);

                        StatusRemovedEvent?.Invoke(this, new StatusEventArgs(_activeCharacter.Id, statusEffect));
                    }
                }

                //if the character no longer has any status, no more combos.
                if (!_activeCharacter.StatusEffects.Any())
                    _activeCharacter.IsPrimed = false;
            }

            _activeCharacter = GetCharacterAtInitiative(0);
            _activeCharacter.StartTurn();

            //apply any status effects for the new active character that trigger at the start of thier turn.
            foreach (var statusEffect in _activeCharacter.StatusEffects)
            {
                statusEffect.StartTurn(_activeCharacter, this);
            }
        
            //apply any status effects for characters that apply whenever initiative moves.
            foreach (var character in _characters)
            {
                foreach (var statusEffect in character.StatusEffects)
                {
                    statusEffect.Tick(character, this);
                }
            }

            if (!_activeCharacter.IsHero)
                Phase = GamePhase.EnemyTurn;
            else
            {
                Phase = GamePhase.Playing;
            }

            if (!Heroes.Any() || !Enemies.Any())
                Phase = GamePhase.GameOver;

            EndTurnEvent?.Invoke(this, new EndTurnEventArgs(GetInitiativeList().ToList()));

            if(_activeCharacter == Commander)
                GainPotential();

            var zk = _characters.FirstOrDefault(c => !c.IsHero && c.MonsterType == MonsterType.ZombieKing);

            if (zk == null || !zk.IsAlive)
            {
                SendMessage("Enemy Leader Defaated");
                GameOverEvent?.Invoke(this, new MessageEventArgs("You Win!"));
            }

            if (!Commander.IsAlive)
            {

                SendMessage("Commander Defaated");
                GameOverEvent?.Invoke(this, new MessageEventArgs("You Lose."));
            }

        }
        
        public void MoveCharacterTo(Character character, HexCoordinate position)
        {
            CharacterMoveEvent?.Invoke(this, 
                new MoveEventArgs
				{
                    ActiveCharacterId = character.Id,
					Path = FindShortestPath(character.Position, position, character.MovementType)
                });

            character.MoveTo(position);
        }

        public void NotifyAction(HexAction action, Character character)
        {
            ActionEvent?.Invoke(this,
                new ActionEventArgs { Action = action });

	        character.CanAttack = false;
        }
        
        public int ApplyDamage(Character characterToDamage, int damage, string message = null)
        {
            characterToDamage.Health -= damage;

            if(!string.IsNullOrWhiteSpace(message))
                SendMessage(message, characterToDamage);

            TakeDamageEvent?.Invoke(this,
                new DamageTakenEventArgs { DamageTaken = damage, TargetCharacterId = characterToDamage.Id });
            
            //check to see if they died.
            if (characterToDamage.Health <= 0)
            {
                characterToDamage.IsAlive = false;
                CharacterDiedEvent?.Invoke(this, new InteractionRequestEventArgs() { TargetCharacterId = characterToDamage.Id });
            }

            return damage;
        }

        public void ApplyHealing(Character character, int healing)
        {
            character.Health += healing;
            if (character.Health >= character.MaxHealth)
                character.Health = character.MaxHealth;

            TakeDamageEvent?.Invoke(this, new DamageTakenEventArgs()
            {
                DamageTaken = -healing,
                TargetCharacterId = character.Id
            });
        }

        #region Private Update Methods

        private void ZombieTurn(Character character)
        {
            var position = character.Position;
            List<HexCoordinate> shortestPath = null;
            int shortestPathLength = int.MaxValue;
            Character closestHero = null;

            //find the closest hero
            foreach (var hero in Heroes)
            {
                var nearestNeighbour = GetNearestWalkableNeighbor(position, hero.Position);

                if (nearestNeighbour == null)
                    continue;

                var path = FindShortestPath(position, nearestNeighbour);

                if (path == null) continue;

                if (path.Count >= shortestPathLength) continue;

                shortestPathLength = path.Count;
                shortestPath = path;
                closestHero = hero;
            }

            if (closestHero == null)
                return;

            //loop through available actions
            foreach (var action in character.Actions.Where(a => a.IsAvailable(character)))
            {
                if (action.IsValidTarget(character, closestHero.Position, this) &&
                    action.IsDetonator == closestHero.IsPrimed)
                {
                    //if we can hit the hero, hit them now and end turn.
                    NotifyAction(action, character);
                    ApplyDamage(closestHero, action.Power);
                    ApplyStatus(closestHero, action.StatusEffect);
	                if (action.Combo != null)
	                {
		                ApplyCombo(closestHero, action.Combo);
		                action.Combo.TriggerAsync(character, new DummyInputProvider(closestHero.Position), this);
	                }

	                return;
                }
            }

            //if we couldn't reach the closest hero move towards them.
            //if we found a path to a hero 
            if (shortestPath == null) return;

            while (shortestPath.Count > character.Movement + 1)
            {
                shortestPath.RemoveAt(character.Movement + 1);
            }

            MoveCharacterTo(character, shortestPath.Last());

            foreach (var action in character.Actions.Where(action =>
                action.IsValidTarget(character, closestHero.Position, this)
                && action.IsDetonator == closestHero.IsPrimed && action.IsAvailable(character)))
            {
                NotifyAction(action, character);
                ApplyDamage(closestHero, action.Power);
                ApplyStatus(closestHero, action.StatusEffect);
	            if (action.Combo != null)
	            {
		            ApplyCombo(closestHero, action.Combo);
		            action.Combo.TriggerAsync(character, new DummyInputProvider(closestHero.Position), this);
	            }

	            return;
            }
        }

        private void ZombieKingTurn(Character character)
        {
            var position = character.Position;
            List<HexCoordinate> shortestPath = null;
            int shortestPathLength = int.MaxValue;
            Character closestHero = null;

            //find the closest hero
            foreach (var hero in Heroes)
            {
                var nearestNeighbour = GetNearestWalkableNeighbor(position, hero.Position);

                if (nearestNeighbour == null)
                    continue;

                var path = FindShortestPath(position, nearestNeighbour);

                if (path == null) continue;

                if (path.Count >= shortestPathLength) continue;

                shortestPathLength = path.Count;
                shortestPath = path;
                closestHero = hero;
            }

            if (closestHero != null)
            {
                //if the closest hero is close then move away.
                if (shortestPathLength <= 3)
                {
                    //get all the tiles to which the zombie COULD move
                    var tilesInRange = GetValidDestinations(character, character.Movement);

                    float greatestDistance = 0;
                    HexCoordinate destination = null;

                    //look at all the possible destinations and get the one which is the furthest average distance away from heroes
                    foreach (var tile in tilesInRange)
                    {
                        var distanceToHeroes = Heroes.Select(data => _map.DistanceBetweenPoints(tile, data.Position));

                        var distance = (float)distanceToHeroes.Sum() / (float)Heroes.Count();
                        if (distance > greatestDistance)
                        {
                            greatestDistance = distance;
                            destination = tile;
                        }
                    }
                    if(destination != null)
                        MoveCharacterTo(character, destination);
                }
            }
	        var zombies = _characters.Where(c => !c.IsHero && c.MonsterType == MonsterType.Zombie && c.IsAlive).ToList();

			var rand = new Random(DateTime.Now.Millisecond);

            if (rand.Next(0, 8) >= zombies.Count)
			{
                //spawn zombie
                var zombie = CreateZombie();

                MessageEvent?.Invoke(this, new MessageEventArgs("Zombie Summon"));
                
                foreach (var tile in _map.GetNeighborCoordinates(character.Position))
                {
                    //one unit per tile and only deploy to walkable spaces.
                    if(IsHexPassable(tile))
                    {
                        zombie.SpawnAt(tile);
                        _characters.Add(zombie);
                        SpawnCharacterEvent?.Invoke(this, new SpawnChracterEventArgs(){ CharacterId = zombie.Id, TargetPosition = tile, MaxHealth = zombie.MaxHealth, MonsterType = MonsterType.Zombie});
                        break;
                    }
                }
            }
            else
            {
                
                if (zombies.Count == 0) return;

                var zombie = zombies[rand.Next(0, zombies.Count)];
                var zombie2 = zombies[rand.Next(0, zombies.Count)];

                MessageEvent?.Invoke(this, new MessageEventArgs("Zombie Rush!"));

                zombie.StartTurn();
                ZombieTurn(zombie);
                zombie.EndTurn();
                zombie2.StartTurn();
                ZombieTurn(zombie2);
                zombie2.EndTurn();
            }
        }

        public void ApplyStatus(Character targetCharacter, StatusEffect effect)
        {
            //todo - apply status effects based on status damage
            //for now we just always apply any relevant status effects
            if (effect == null) return;

            var effectToApply = effect.Copy();
            targetCharacter.StatusEffects.Add(effectToApply);
            targetCharacter.IsPrimed = true;

            StatusAppliedEvent?.Invoke(this, new StatusEventArgs(targetCharacter.Id, effectToApply));
        }

        public void ApplyCombo(Character targetCharacter, ComboAction combo)
        {
            ComboEvent?.Invoke(this, new ComboEventArgs(targetCharacter.Id, combo));
            targetCharacter.IsPrimed = false;

            //if the player scores a combo they gain potential. if their commander gets comboed they lose potential (uh-oh!)
            if (_activeCharacter.IsHero)
                GainPotential();

            if (targetCharacter == Commander)
            {
                SendMessage("Commander Comboed\nPotential Down");
                LosePotential(1);
            }
        }

        public void ApplyPush(Character targetCharacter, HexCoordinate direction, int distance = 0)
        {
            SendMessage("PUSH");

            var targetCharacterPos = targetCharacter.Position;
            var destinationPos = targetCharacterPos + direction;
            while (distance > 0)
            {
                if (IsHexPassable(destinationPos))
                {
                    MoveCharacterTo(targetCharacter, destinationPos);

                    var tile = _map.Map[destinationPos];

                    if (tile.TerrainType != TerrainType.Ice && tile.TerrainType != TerrainType.ThinIce)
                        distance--;

                    destinationPos = destinationPos + direction;
                }
                else if (!IsTileEmpty(destinationPos))
                {
                    var objectCharacter = GetCharacterAtCoordinate(destinationPos);
                    ApplyDamage(targetCharacter, distance * 10, "IMPACT");
                    ApplyDamage(objectCharacter, distance * 10, "IMPACT");
                    distance = 0;
                }
                else
                {
                    ApplyDamage(targetCharacter, distance * 15, "IMPACT");
                    distance = 0;
                }
            }
        }

        private void SpawnHero(HexCoordinate position)
        {
            if (_characters.Any(character => character.Position == position))
                return;

            var hero = _characters.FirstOrDefault(chracter => chracter.IsHero && !chracter.IsAlive);
            if (hero == null) return;

            if (!IsHexPassable(position))
                return;

            hero.SpawnAt(position);

            SpawnCharacterEvent?.Invoke(this, new SpawnChracterEventArgs() { CharacterId = hero.Id, TargetPosition = position });
        }

        public void GainPotential(int potential = 1)
        {
            if (Potential + potential <= MaxPotential)
            {
                Potential += potential;
            }

            PotentialChangeEvent?.Invoke(this, new PotentialEventArgs(potential));
        }

        public void LosePotential(int potential)
        {
            if (Potential >= potential)
            {
                Potential -= potential;
            }

            PotentialChangeEvent?.Invoke(this, new PotentialEventArgs(-potential));
        }

        #endregion

        #region Public Accessor Methods

	    public Tile GetTileAtCoordinate(HexCoordinate coordinate)
	    {
		    return _map.Map[coordinate];
	    }

        public Character GetCharacter(Guid characterId)
        {
            return _characters.FirstOrDefault(ch => ch.Id == characterId);
        }

        public Character GetCharacterAtCoordinate(HexCoordinate coordinate)
        {
            return Characters.FirstOrDefault(character => character.Position == coordinate && character.IsAlive);
        }

        /// <summary>
        /// Checks to see if a specific tile is walkable and unoccupied.
        /// </summary>
        /// <returns>true/false</returns>
        public bool IsHexPassable(HexCoordinate coordinate)
        {
            return IsHexWalkable(coordinate)
                   && IsTileEmpty(coordinate);
        }

        public bool IsTileEmpty(HexCoordinate position)
        {
			return !_characters.Any(character => character.Position == position && character.IsAlive);
        }
		
        public Character GetCharacterAtInitiative(int initiative)
        {
            var initiativeList = GetInitiativeList();
            if (initiativeList.Count < initiative + 1)
                return null;

            return initiativeList[initiative];
        }

        public List<Character> GetInitiativeList()
        {
            var initiativeList = new List<Character>();
            var turnTimerToBeat = int.MaxValue;
            var orderedList = _characters.Where(cha => cha.IsAlive).OrderBy(cha => cha.TurnTimer);
            Character characterToBeat = orderedList?.First();

            var iterator = orderedList.GetEnumerator();
            var endOfList = !iterator.MoveNext();

            while(!endOfList)
            {
                var character = iterator.Current;
                //if character is going to go before the time to beat, add them to the list.
                if (character.TurnTimer < turnTimerToBeat)
                {
                    initiativeList.Add(character);

                    //if they're going to go again before the time to beat, they're the new time to beat.
                    if (character.TurnTimer + character.TurnCooldown < turnTimerToBeat)
                    {
                        characterToBeat = character;
                        turnTimerToBeat = character.TurnTimer + character.TurnCooldown;
                    }

                    endOfList = !iterator.MoveNext();
                }
                else
                {
                    //if the character is slower than the time to beat the character to beat will go again and the time to beat will increase
                    initiativeList.Add(characterToBeat);
                    turnTimerToBeat = turnTimerToBeat + characterToBeat.TurnCooldown;
                }
            }

            return initiativeList;
        }
		
        /// <summary>
        /// Get all the tiles within range of a target position along each of our three coordinate system axes.
        /// </summary>
        public List<HexCoordinate> GetAxisTilesInRange(Character objectCharacter, int range)
        {
            var targets = new List<HexCoordinate>();

            foreach (var direction in HexGrid.Directions)
            {
                for (var i = 0; i < range; i++)
                {
                    var hexToCheck = _activeCharacter.Position + (direction * (i + 1));

                    if (!Map.ContainsKey(hexToCheck))
                        break;
                    
                    targets.Add(hexToCheck);

                    //if there's a unit in this tile then we can't see past them.
                    if (!IsTileEmpty(hexToCheck))
                        break;
                }
            }

            return targets;
        }

        /// <summary>
        /// Get all the visible tiles within range of a target position along each of our three coordinate system axes.
        /// </summary>
        public List<HexCoordinate> GetVisibleAxisTilesInRange(Character objectCharacter, int range)
        {
            var targets = new List<HexCoordinate>();
            foreach (var direction in HexGrid.Directions)
            {
                for (var i = 0; i < range; i++)
                {
                    var hexToCheck = _activeCharacter.Position + (direction * (i + 1));

                    if (!Map.ContainsKey(hexToCheck))
                        break;

                    if (IsHexOpaque(hexToCheck))
                        break;

                    targets.Add(hexToCheck);

                    //if there's a unit in this tile then we can't see past them.
                    if (!IsTileEmpty(hexToCheck))
                        break;
                }
            }

            return targets;
        }

        public List<HexCoordinate> GetVisibleAxisTilesInRangeIgnoreUnits(Character objectCharacter, int range)
        {
            var targets = new List<HexCoordinate>();
            foreach (var direction in HexGrid.Directions)
            {
                for (var i = 0; i < range; i++)
                {
                    var hexToCheck = _activeCharacter.Position + (direction * (i + 1));

                    if (!Map.ContainsKey(hexToCheck))
                        break;

                    if (IsHexOpaque(hexToCheck))
                        break;

                    targets.Add(hexToCheck);
                }
            }

            return targets;
        }

        /// <summary>
        /// Get all the visible tiles within range of a target position
        /// </summary>
        public List<HexCoordinate> GetVisibleTilesInRange(Character objectCharacter, int range)
        {
            var targets = new List<HexCoordinate>() { objectCharacter.Position };

            GetVisibleTilesRecursive(targets, objectCharacter.Position, _activeCharacter.Position, range);

            return targets;
        }

        /// <summary>
        /// Get all the visible tiles within range of a target position
        /// </summary>
        public List<HexCoordinate> GetVisibleTilesInRangeIgnoreUnits(Character objectCharacter, int range)
        {
            var targets = new List<HexCoordinate>() { objectCharacter.Position };

            GetVisibleTilesRecursive(targets, objectCharacter.Position, _activeCharacter.Position, range, 0, true);

            return targets;
        }

        /// <summary>
        /// Get all the tiles within range of a target position
        /// </summary>
        public List<HexCoordinate> GetTilesInRange(Character objectCharacter, int range)
        {
            var result = new List<HexCoordinate>(){ objectCharacter.Position };

            GetTilesInRangeRecursive(result, objectCharacter.Position, range);

            return result;
        }

        public List<HexCoordinate> GetValidDestinations(Character objectCharacter, int range)
        {
            List<HexCoordinate> visitedHexes = new List<HexCoordinate>();
            GetWalkableNeighboursRecursive(visitedHexes, objectCharacter.Position, objectCharacter.MovementType, range);
            return visitedHexes;
        }
		
	    public bool IsValidTarget(Character objectCharacter, HexCoordinate targetPosition, int range, GetValidTargetsDelegate targetDelegate)
	    {
		    return targetDelegate != null && targetDelegate.Invoke(objectCharacter, range, this).Contains(targetPosition);
	    }

		/// <summary>
		/// Returns a boolean indicating if the selected tile is reachable from the start position in
		/// a number of steps =< range.
		/// </summary>
		public bool IsValidDestination(Character objectCharacter, HexCoordinate targetPosition, int range)
        {
            return GetValidDestinations(objectCharacter, range).Contains(targetPosition);
        }

        /// <summary>
        /// Get the shortest traversable path between two points on the map.
        /// If no path can be found returns null
        /// 
        /// A* is fuckin' rad.
        /// 
        /// Rad as shit.
        /// </summary>
        public List<HexCoordinate> FindShortestPath(HexCoordinate start, HexCoordinate destination, MovementType movementType = MovementType.NormalMove)
        {
            if (!_map.Map.ContainsKey(start) || !_map.Map.ContainsKey(destination))
                return null;

            //data structure map such that key : a tile we've looked at one or more times, value : the previous tile in the shortest path to the hex in the key.
            var ancestorMap = new Dictionary<HexCoordinate, HexCoordinate> { { start, null } };

            //a data structure that holds the shortest distance found to each tile that we've searched
            var pathValues = new Dictionary<HexCoordinate, int> { { start, 0 } };

            //data structure holding the estimated path length from the start to the destination for each tile we've searched.
            var tileEstimates = new Dictionary<HexCoordinate, int> { { start, 0 } };

            HexCoordinate current = start;

            while (tileEstimates.Any())
            {
                //get the current best estimated tile
                //this is the tile we *think* probably leads to the shortest path to the destination
                current = tileEstimates.OrderBy(data => data.Value).First().Key;
                tileEstimates.Remove(current);

                if (current == destination)
                    break;
                
                //look at all of the best estimated tile's neighbours
                foreach (var neighbor in _map.GetNeighborCoordinates(current))
                {
                    //tile validation goes here.
                    if (!IsTilePassable(movementType, neighbor))
                        continue;

                    //check if the tile is water.
                    if (_map.Map[neighbor].TerrainType == TerrainType.Water && neighbor != destination)
                        continue;

                    //nodes are always one space away - hexgrid!
                    //BUT hexes have different movement costs to move through!
                    //the path from the start to the tile we're looking at now is the path the 
                    var pathLengthToNeighbor = pathValues[current] + _map.Map[neighbor].MovementCost;
                    
                    //estimate the neighbour and add it to the list of estimates or update it if it's already in the list
                    if (!pathValues.ContainsKey(neighbor) || pathValues[neighbor] > pathLengthToNeighbor)
                    {
                        pathValues[neighbor] = pathLengthToNeighbor;

                        //heuristic for "distance to destination tile" is just absolute distance between current tile and the destination
                        var estimate = pathLengthToNeighbor + _map.DistanceBetweenPoints(neighbor, destination);
                        ancestorMap[neighbor] = current;

                        tileEstimates[neighbor] = estimate;
                    }
                }
            }

            //if we looked at every reachable tile and didn't find the destination in there then no path exists.
            if (current != destination)
                return null;

            //go backwards up the ancestor path to the start.
            var path = new List<HexCoordinate>();
            path.Add(current);
            var ancestor = ancestorMap[current];
            while (ancestor != null)
            {
                path.Add(ancestor);
                ancestor = ancestorMap[ancestor];
            }

            path.Reverse();
            return path;
        }

        #endregion

        #region Private Helper Methods

	    private bool IsTilePassable(MovementType movementType, HexCoordinate coordinate)
	    {
		    //tile validation goes here.
		    if (!IsHexWalkable(coordinate)) return false;

		    var character = _characters.FirstOrDefault(c => c.Position == coordinate);
		    if (character == null)
			    return true;
				
		    switch (movementType)
		    {
			    case MovementType.NormalMove:
				    return false;
			    case MovementType.MoveThroughHeroes:
				    if (!character.IsHero)
						return false;
					break;
			    case MovementType.MoveThroughMonsters:
				    if (character.IsHero)
						return false;
					break;
			    case MovementType.MoveThroughUnits:
				    break;
			    case MovementType.Etheral:
				    break;
			    case MovementType.Flying:
				    break;
			    default:
				    return false;
			}

		    return true;
	    }

        private void SendMessage(string message)
        {
            MessageEvent?.Invoke(this, new MessageEventArgs(message));
        }

        private void SendMessage(string message, Character targetCharacter)
        {
            MessageEvent?.Invoke(this, new MessageEventArgs(message, targetCharacter));
        }

        private bool IsInEnemySpawnArea(HexCoordinate coord)
        {
            return coord.Z <= 0 && coord.Z >= -3 && coord.X >= -4 && coord.X <= 4;
        }

	    private bool IsHexWalkable(HexCoordinate co)
        { 
		    return _map.Map[co].IsWalkable;
	    }

        private bool IsHexOpaque(HexCoordinate coordinate)
        {
            return _map.Map[coordinate].BlocksLOS;
        }

        private bool BlocksLineOfSight(HexCoordinate coordinate)
        {
            return IsHexOpaque(coordinate) || !IsTileEmpty(coordinate);
        }

        private void GetTilesInRangeRecursive(List<HexCoordinate> tilesInRange, HexCoordinate position, int range, int searchDepth = 0)
        {
            if (searchDepth >= range)
                return;

            var adjacentTiles = _map.GetNeighborCoordinates(position);
            foreach (var coord in adjacentTiles)
            {
                if (!tilesInRange.Contains(coord))
                {
                    tilesInRange.Add(coord);
                }

                GetTilesInRangeRecursive(tilesInRange, coord, range, searchDepth + 1);
            }
        }

        private void GetWalkableNeighboursRecursive(List<HexCoordinate> neighbours, HexCoordinate position, MovementType movementType, int maxSearchDepth, int searchDepth = 0)
        {
            if (searchDepth >= maxSearchDepth)
                return;

            var adjacentTiles = _map.GetNeighborCoordinates(position);

            foreach (var coord in adjacentTiles)
            {
	            if (!IsTilePassable(movementType, coord)) continue;

                //if this tile is not already in the list
                if (!neighbours.Contains(coord) 
                    && searchDepth + _map.Map[coord].MovementCost <= maxSearchDepth //and we have enough movement to walk to this tile
                    && IsTileEmpty(coord)) //and the tile is empty
                {
                    neighbours.Add(coord);//then add it to the list.
                }

                if (_map.Map[coord].TerrainType == TerrainType.Water)
                    continue;

                GetWalkableNeighboursRecursive(neighbours, coord, movementType, maxSearchDepth, searchDepth + _map.Map[coord].MovementCost);
            }
        }

        private HexCoordinate GetNearestEmptyNeighbourRecursive( HexCoordinate position, int maxSearchDepth, int searchDepth = 0)
        {
            if (searchDepth >= maxSearchDepth)
                return null;

            var adjacentTiles = _map.GetNeighborCoordinates(position);

            foreach (var coord in adjacentTiles)
            {
                if (IsHexPassable(coord))
                    return coord;
            }

            foreach (var coord in adjacentTiles)
            {
                return GetNearestEmptyNeighbourRecursive(coord, maxSearchDepth, searchDepth + 1);
            }

            return null;
        }

		public HexCoordinate GetNearestNeighbor(HexCoordinate start, HexCoordinate end)
		{
			var neighbours = _map.GetNeighborCoordinates(end);

			int distance = 100;
			HexCoordinate nearest = null;

			foreach (var neighbor in neighbours)
			{
				var delta = _map.DistanceBetweenPoints(start, neighbor);
				if (delta < distance)
				{
					nearest = neighbor;
					distance = delta;
				}
				else if (delta == distance && nearest != null)
				{
					var startCoords = Get2DCoords(start);
					var neighborCoords = Get2DCoords(neighbor);
					var nearestCoords = Get2DCoords(nearest);

					var nearestDelta = nearestCoords - startCoords;
					var neighborDelta = neighborCoords - startCoords;

					if (neighborDelta.Length() < nearestDelta.Length())
					{
						nearest = neighbor;
						distance = delta;
					}
				}
			}

			return nearest;
		}

	    private readonly float _sqrt3 = (float)Math.Sqrt(3.0);
		
		//not screenSpace
		private Vector2 Get2DCoords(HexCoordinate coordinate)
	    {
		    var posX = (_sqrt3 * coordinate.X + (_sqrt3 / 2 * coordinate.Z));
		    var posY = 1.5f * coordinate.Z;

		    return new Vector2(posX, posY);
		}

		private void GetVisibleTilesRecursive(List<HexCoordinate> neighbours, HexCoordinate position, HexCoordinate startPosition, int maxSearchDepth, int searchDepth = 0, bool ignoreUnits = false)
        {
            if (searchDepth >= maxSearchDepth)
                return;

            var adjacentTiles = _map.GetNeighborCoordinates(position);

            foreach (var coord in adjacentTiles)
            {
                //if the terrain blocks LOS then move on
                if (IsHexOpaque(coord)) continue;

                var distanceToPoint = _map.DistanceBetweenPoints(startPosition, position);
                var distanceToNeighbour = _map.DistanceBetweenPoints(startPosition, coord);

                //only look at neighbouring tiles that're further away from the starting position than the tile we're currently at
                if(distanceToPoint >= distanceToNeighbour)
                    continue;
                
                if (!neighbours.Contains(coord) && searchDepth + 1 <= maxSearchDepth)
                {
                    neighbours.Add(coord);
                }

                if (!IsTileEmpty(coord) && !ignoreUnits)
                    continue;

                GetVisibleTilesRecursive(neighbours, coord, startPosition, maxSearchDepth, searchDepth + 1);
            }
        }

        //get the nearest walkable neightbour to a target tile from a destination
        private HexCoordinate GetNearestWalkableNeighbor(HexCoordinate start, HexCoordinate end)
        {
            var neighbours = _map.GetNeighborCoordinates(end);

            int distance = 100;
            HexCoordinate nearest = null;

            foreach (var neighbour in neighbours)
            {
                if(!IsHexPassable(neighbour))
                    continue;

                var delta = _map.DistanceBetweenPoints(start, neighbour);
                if (delta < distance)
                {
                    nearest = neighbour;
                    distance = delta;
                }
            }

            return nearest;
        }
        
        #endregion

        #endregion
    }
}
