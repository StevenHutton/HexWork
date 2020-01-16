using System;
using System.Collections.Generic;
using System.Linq;
using HexWork.Gameplay.Actions;
using HexWork.Gameplay.Interfaces;
using HexWork.UI;
using Microsoft.Xna.Framework;
using MonoGameTestProject.Gameplay;

namespace HexWork.Gameplay
{
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
		public Guid CharacterId;
        public HexCoordinate Destination;
    }

    public class InteractionRequestEventArgs : EventArgs
    {
        public Guid TargetCharacterId;

        public HexCoordinate TargetPosition;
    }

    public class SpawnTileEffectEventArgs : EventArgs
    {
        public Guid Id;

        public TileEffectType Type = TileEffectType.Fire;

        public HexCoordinate Position;
    }

    public class RemoveTileEffectEventArgs : EventArgs
    {
        public Guid Id;
    }

    public class SpawnChracterEventArgs
    {
        public Character Character;
        
        public MonsterType MonsterType;
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

        public List<TileEffect> TileEffects { get; set; } = new List<TileEffect>();

        /// <summary>
        ///The character who currently has initiative
        /// </summary>
        public Character ActiveCharacter;

        private int _difficulty = 1;

        #region Gameplay Actions

        private HexAction _moveAction;
        private HexAction _moveActionEx;

        private TileEffect _fireEffect = new TileEffect()
        {

        };

        private HexAction _zombieGrab;

        private HexAction _zombieBite;

        private PotentialGainAction _potentialGainAction;

        TargetPattern _whirlWindTargetPattern = new TargetPattern(new HexCoordinate(1, 0, -1),
            new HexCoordinate(1, -1, 0),
            new HexCoordinate(0, -1, 1),
            new HexCoordinate(-1, 0, 1),
            new HexCoordinate(-1, 1, 0),
            new HexCoordinate(0, 1, -1));

        TargetPattern _xAxisLinePattern = new TargetPattern(new HexCoordinate(0, 0, 0),
            new HexCoordinate(1, -1), new HexCoordinate(2, -2));

        TargetPattern _rotatingLinePattern = new TargetPattern(new HexCoordinate(0, 0),
            new HexCoordinate(1, 0),
            new HexCoordinate(-1, 0));

        TargetPattern _cornerPattern = new TargetPattern(new HexCoordinate(0, 0),
            new HexCoordinate(1, 0),
            new HexCoordinate(0, -1));

        #endregion

        #endregion

        #region Events

        public event EventHandler<MoveEventArgs> CharacterMoveEvent;

        public event EventHandler<MoveEventArgs> CharacterTeleportEvent;

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

        public event EventHandler<SpawnTileEffectEventArgs> SpawnTileEffectEvent;

        public event EventHandler<RemoveTileEffectEventArgs> RemoveTileEffectEvent;

        #endregion

        #region Properties

        public IEnumerable<Character> Heroes => LivingCharacters.Where(character => character.IsHero);

        public HexGrid Map => _map;
        
        public List<Character> Characters
        {
            get => _characters;
            set => _characters = value;
        }

        public IEnumerable<Character> LivingCharacters
        {
            get => _characters.Where(c => c.IsAlive);
        }

        public IEnumerable<Character> Enemies
        {
            get => _characters.Where(character => !character.IsHero && character.IsAlive);
        }
        
        public Character Commander { get; set; }
        
        public int TeamSize { get; set; } = 5;

        public int MaxPotential { get; set; } = 0;

        public int Potential { get; set; } = 0;

        #endregion

        #region Methods

        #region Initialisation

        public GameState()
        {
            _moveAction = new MoveAction("Move", TargetingHelper.GetDestinationTargetTiles) { Range = 0 };
            _moveActionEx = new MoveAction("Nimble Move! (1)", TargetingHelper.GetDestinationTargetTiles)
                { PotentialCost = 1, Range = 2 };

            _zombieGrab = new HexAction(name: "Zombie Grab",
                statusEffect: new ImmobalisedEffect()
                {
                    StatusEffectType = StatusEffectType.Rooted
                },
                combo: null,
                targetDelegate: TargetingHelper.GetValidTargetTilesNoLos)
            {
                Range = 1,
                Power = 2
            };

            _zombieBite = new HexAction(name: "Zombie Bite",
                combo: new ComboAction() { Power = 2 },
                targetDelegate: TargetingHelper.GetValidTargetTilesNoLos)
            {
                Range = 1,
                Power = 2
            };

            _potentialGainAction = new PotentialGainAction("Wind", null, null, null, null);

            InitialiseHeroes();
        }

        public void StartGame(int difficulty = 1)
        {
            _difficulty = difficulty; 
            InitialiseEnemies();

            foreach (var character in _characters.Where(c => !c.IsHero))
            {
                var coordinate = _map.GetRandomCoordinateInMap();

                //one unit per tile and only deploy to walkable spaces.
                while (Characters.Select(cha => cha.Position).Contains(coordinate) || !_map[coordinate].IsWalkable || !IsInEnemySpawnArea(coordinate))
                {
                    coordinate = _map.GetRandomCoordinateInMap();
                }

                character.SpawnAt(coordinate);
                SpawnCharacterEvent?.Invoke(this, 
                    new SpawnChracterEventArgs()
                    {
                        MonsterType = character.MonsterType,
                        Character = character
                    });
            }

            var spawnPoint = new HexCoordinate(-2, -2, 4);

            foreach (var character in Heroes)
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

                character.SpawnAt(position);
                SpawnCharacterEvent?.Invoke(this, new SpawnChracterEventArgs() { Character = character });
            }

            MaxPotential = Commander.Command;
            Potential = 0;

            NextTurn();
        }

        #region Create Characters

        private void InitialiseEnemies()
        {
            Characters.RemoveAll(cha => !cha.IsHero);

            for (int i = 0; i < _difficulty; i++)
            {
                var zombieKing = new Character($"Zom-boy King {i}", 160, 140, 2, 1)
                {
                    MonsterType = MonsterType.ZombieKing
                };
                zombieKing.AddAction(_moveAction);
                zombieKing.AddAction(_zombieGrab);
                zombieKing.AddAction(_zombieBite);
                Characters.Add(zombieKing);
            }

            for (var i = 0; i < 5; i++)
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
        }

        private void CreateMajin()
        {
            var burningBolt = new HexAction("Fire Bolt",
                TargetingHelper.GetValidAxisTargetTilesLos,
                new DotEffect())
            {
                Range = 3
            };

			var exBurningBoltAction = new HexAction("Fire Wall! (1)",
                TargetingHelper.GetValidAxisTargetTilesLosIgnoreUnits,
                new DotEffect(), null,
                _rotatingLinePattern)
            {
                PotentialCost = 1,
                Range = 3,
                TileEffect = TileEffectType.Fire
            };

	        var ringofFire = new HexAction("Ring of Fire! (2)",
		        TargetingHelper.GetValidAxisTargetTilesLosIgnoreUnits,
		        new DotEffect(), null,
		        _whirlWindTargetPattern)
	        {
		        PotentialCost = 2,
		        Range = 3,
                TileEffect = TileEffectType.Fire
            };

			var lightningBolt = new HexAction("Lightning Bolt (1)", TargetingHelper.GetValidAxisTargetTilesLosIgnoreUnits, null, new SpreadStatusCombo())
            {
                Range = 3,
                Power = 3,
                PotentialCost = 1
            };

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
	        majinCharacter.AddAction(ringofFire);
			majinCharacter.AddAction(lightningBolt);
            majinCharacter.AddAction(_potentialGainAction);

            Characters.Add(majinCharacter);
            Commander = majinCharacter;
        }

        private void CreateGunner()
        {
            var shotgunBlast = new LineAction("Shotgun Blast! (1)",
                TargetingHelper.GetValidAxisTargetTilesLos,
                null, new ComboAction(),
                _cornerPattern)
            {
                PotentialCost = 1,
                Range = 2,
                TileEffect = TileEffectType.Wind
            };

            var shovingSnipeAction = new PushAction(name: "Shoving Snipe",
                targetDelegate: TargetingHelper.GetValidAxisTargetTilesLos,
                combo: null)
            {
                Power = 1,
                Range = 5,
                PushForce = 1
            };

            var detonatingSnipeActionEx = new HexAction("Perfect Snipe! (1)",
                TargetingHelper.GetValidAxisTargetTilesLos,
                null,
                new ComboAction{ Power = 7 })
            {
                PotentialCost = 1,
                Power = 1,
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
            gunnerCharacter.AddAction(shotgunBlast);
            gunnerCharacter.AddAction(_potentialGainAction);
            Characters.Add(gunnerCharacter);
        }

        private void CreateNinja()
        {
            var shurikenHailAction = new HexAction("Shuriken",
                TargetingHelper.GetValidTargetTilesLos,
                new DotEffect
                {
                    Name = "Bleeding",
                    Damage = 5,
                    Duration = 1,
                    StatusEffectType = StatusEffectType.Bleeding
                })
            {
                Range = 2,
                FollowUpAction = new MoveAction("Shift", TargetingHelper.GetDestinationTargetTiles) { Range = 1, IsFixedMovement = true }
            };

            var shurikenPattern = new TargetPattern(new HexCoordinate(-1, 1), new HexCoordinate(0, -1),
                new HexCoordinate(1, 0));
            var shurikenHailActionEx = new HexAction("Shuriken Hail! (1)",
                TargetingHelper.GetValidTargetTilesLosIgnoreUnits,
                new DotEffect
                {
                    Name = "Bleeding",
                    Damage = 5,
                    Duration = 2,
                    StatusEffectType = StatusEffectType.Bleeding
                },
                null, shurikenPattern)
            {
                PotentialCost = 1,
                Range = 3
            };

            var swapAction = new SwapAction("Swap Positions (1)", TargetingHelper.GetValidTargetTilesLos)
            {
                Power = 3,
                AllySafe = false,
                PotentialCost = 1,
                Range = 2
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
            ninjaCharacter.AddAction(swapAction);
            ninjaCharacter.AddAction(_potentialGainAction);

            Characters.Add(ninjaCharacter);
        }

        private void CreateIronSoul()
        {
            var pushingFist = new PushAction("Pushing Fist", TargetingHelper.GetValidTargetTilesLos)
            {
                Range = 1,
                Power = 3,
                PushForce = 2
            };

            var overwhelmingStrike = new PushAction("Overwhelming Strike! (1)", TargetingHelper.GetValidTargetTilesLos,
                null, new StatusCombo()
                {
                    Effect = new ImmobalisedEffect()
                    {
                        StatusEffectType = StatusEffectType.Rooted
                    }
                })
            {
                Range = 1,
                Power = 4,
                PushForce = 3,
                PotentialCost = 1
            };

            var vampiricStrike = new VampiricAction("Vampiric Strike", TargetingHelper.GetValidAxisTargetTilesLos)
            {
                Range = 1,
            };

            var exDetonatingSlash =
            new HexAction("Massive Detonation! (1)", TargetingHelper.GetValidTargetTilesLos, null,
                new ExploderCombo
                {
                    Power = 5,
                    Pattern = _whirlWindTargetPattern,
                    AllySafe = false
                })
            {
                Range = 1,
                PotentialCost = 1,
                Power = 5
            };

            //create Iron Soul hero
            var ironSoulCharacter = new Character("Iron Soul", 200, 120, 2, 3)
            {
                IsHero = true,
                MovementType = MovementType.MoveThroughHeroes
            };
            ironSoulCharacter.AddAction(_moveAction);
            ironSoulCharacter.AddAction(_moveActionEx);
            ironSoulCharacter.AddAction(vampiricStrike);
            ironSoulCharacter.AddAction(pushingFist);
            ironSoulCharacter.AddAction(overwhelmingStrike);
            ironSoulCharacter.AddAction(exDetonatingSlash);
            ironSoulCharacter.AddAction(_potentialGainAction);
            Characters.Add(ironSoulCharacter);
        }

        private void CreateBarbarian()
        {
            var spreadStatusCombo = new SpreadStatusCombo { AllySafe = true, Power = 3};
            var detonatingSlash =
              new HexAction("Detonating Strike! (1)", TargetingHelper.GetValidTargetTilesLos, null, spreadStatusCombo)
                  {
                      Range = 1,
                      PotentialCost = 1
                  };

			var earthQuakeStrike = new LineAction("Earthquake Strike",
				TargetingHelper.GetValidAxisTargetTilesLos,
				new ImmobalisedEffect(),
				null,
				_xAxisLinePattern)
            {
                Range = 1
            };

            var whirlwindAttack = new HexAction("Spin Attack", TargetingHelper.GetValidTargetTilesLos, null, new ComboAction(),
                _whirlWindTargetPattern)
            {
                Power = 3,
                PotentialCost = 1,
                Range = 0
            };

            //create Barbarian hero
            var barbarianCharacter = new Character("Barbarian", 150, 100, 3, 2)
            {
                IsHero = true,
                MovementType = MovementType.MoveThroughHeroes
            };
            barbarianCharacter.AddAction(_moveAction);
            barbarianCharacter.AddAction(_moveActionEx);
            barbarianCharacter.AddAction(earthQuakeStrike);
            barbarianCharacter.AddAction(whirlwindAttack);
            barbarianCharacter.AddAction(detonatingSlash);
            barbarianCharacter.AddAction(_potentialGainAction);
            Characters.Add(barbarianCharacter);
        }

        #endregion

        #endregion

        #region Public Update Methods

        public void Update(GameTime gameTime)
        {
            if (!ActiveCharacter.IsAlive)
            {
                NextTurn();
                return;
            }

            if (ActiveCharacter.IsHero)
                return;

            //if they can move take their actions and end turn
            if (!ActiveCharacter.HasActed)
            {
                switch (ActiveCharacter.MonsterType)
                {
                    case MonsterType.Zombie:
                        ZombieTurn(ActiveCharacter);
                        break;
                    case MonsterType.ZombieKing:
                        ZombieKingTurn(ActiveCharacter);
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
            if (ActiveCharacter != null)
            {

                ResolveTileEffects(ActiveCharacter, ActiveCharacter.Position);
                ResolveTerrainEffects(ActiveCharacter, ActiveCharacter.Position);
                ActiveCharacter.EndTurn();

                var deltaTime = ActiveCharacter.TurnTimer;
                foreach (var character in _characters)
                {
                    character.TurnTimer -= deltaTime;
                }

                ActiveCharacter.TurnTimer += ActiveCharacter.TurnCooldown;

                //also apply any status effects for the active character that trigger at the end of the turn.
                foreach (var statusEffect in ActiveCharacter.StatusEffects.ToList())
                {
                    statusEffect.EndTurn(ActiveCharacter, this);

                    //if the effect is expired remove it
                    if (statusEffect.IsExpired && ActiveCharacter.StatusEffects.Contains(statusEffect))
                    {
                        ActiveCharacter.StatusEffects.Remove(statusEffect);

                        StatusRemovedEvent?.Invoke(this, new StatusEventArgs(ActiveCharacter.Id, statusEffect));
                    }
                }
            }

            ActiveCharacter = GetCharacterAtInitiative(0);
            ActiveCharacter.StartTurn();

            //apply any status effects for the new active character that trigger at the start of thier turn.
            foreach (var statusEffect in ActiveCharacter.StatusEffects)
            {
                statusEffect.StartTurn(ActiveCharacter, this);
            }

            //apply any status effects for characters that apply whenever initiative moves.
            foreach (var character in _characters)
            {
                foreach (var statusEffect in character.StatusEffects)
                {
                    statusEffect.Tick(character, this);
                }
            }
            
            EndTurnEvent?.Invoke(this, new EndTurnEventArgs(GetInitiativeList().ToList()));

            if(ActiveCharacter == Commander)
                GainPotential();

            if (Enemies.All(c => c.MonsterType != MonsterType.ZombieKing))
            {
                SendMessage("Enemy Leader(s) Defeated");
                GameOverEvent?.Invoke(this, new MessageEventArgs("You Win!"));
                GameOver();
            }

            if (!Commander.IsAlive)
            {
                SendMessage("Commander Defeated - Game Over");
                GameOverEvent?.Invoke(this, new MessageEventArgs("You Lose."));
                GameOver();
            }
        }

        #region Gamestate Transforms

        private void GameOver()
        {
            _characters.RemoveAll(ch => !ch.IsHero);
        }

        public void CharacterGainPower(Guid characterId)
        {
            var character = FindCharacterById(characterId);

            if (character == null)
                return;

            character.Power += 1;
        }

        public void CharacterGainHealth(Guid characterId)
        {
            var character = FindCharacterById(characterId);

            if (character == null)
                return;

            character.MaxHealth += 10;
            character.Health += 10;

            if (character.Health >= character.MaxHealth)
                character.Health = character.MaxHealth;
        }

        public void MoveCharacterTo(Character character, HexCoordinate position)
        {
            var path = FindShortestPath(character.Position, position, character.MovementType);
            
            foreach (var coordinate in path)
            {
                CharacterMoveEvent?.Invoke(this, new MoveEventArgs
                {
                    CharacterId = character.Id,
                    Destination = coordinate
                });

                character.MoveTo(coordinate);
                ResolveTileEffects(character, coordinate);
                ResolveTerrainEffects(character, coordinate);
            }
        }

        public void TeleportCharacterTo(Character character, HexCoordinate position)
        {
            CharacterTeleportEvent?.Invoke(this,
                new MoveEventArgs
                {
                    CharacterId = character.Id,
                    Destination = position
                });

	        ResolveTileEffects(character, position);
			ResolveTerrainEffects(character, position);
            character.MoveTo(position);
        }

	    private void ResolveTileEffects(Character character, HexCoordinate position)
	    {
            var tileEffect = TileEffects.FirstOrDefault(data => data.Position == position);

			if(tileEffect == null)
				return;

			tileEffect.TriggerEffect(this, character);
			TileEffects.Remove(tileEffect);
			RemoveTileEffectEvent?.Invoke(this, new RemoveTileEffectEventArgs(){Id = tileEffect.Guid});
	    }

        //when a character moves into a tile check to see if there're any terrain effects for moving into that tile.
        private void ResolveTerrainEffects(Character character, HexCoordinate destination)
        {
            //don't count terrain effects from a tile you're standing. We don't punish players for leaving lava.
            ResolveTerrainEnterEffect(character, Map[destination]);
        }

        private void ResolveTerrainEnterEffect(Character character, Tile tile)
        {
            switch (tile.TerrainType)
            {
                case TerrainType.Ground:
                    break;
                case TerrainType.Water:
                    break;
                case TerrainType.Lava:
                    ApplyStatus(character,
                        new DotEffect
                        {
                            Damage = 5,
                            Duration = 3,
                            Name = "Burning",
                            StatusEffectType = StatusEffectType.Burning
                        });
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

            CheckDied(characterToDamage);

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

        public void CheckDied(Character character)
        {
            //check to see if they died.
            if (character.Health <= 0 && character.IsAlive)
            {
                character.IsAlive = false;
                CharacterDiedEvent?.Invoke(this, new InteractionRequestEventArgs() { TargetCharacterId = character.Id });
            }
        }

        public void ApplyStatus(Character targetCharacter, StatusEffect effect)
        {
            //todo - apply status effects based on status damage
            //for now we just always apply any relevant status effects
            if (effect == null) return;

            if (!targetCharacter.IsAlive)
                return;

            var effectToApply = effect.Copy();
            targetCharacter.StatusEffects.Add(effectToApply);

            StatusAppliedEvent?.Invoke(this, new StatusEventArgs(targetCharacter.Id, effectToApply));
        }

        /// <summary>
        /// Apply a combo effect to a target character if they're currently suffering a status effect.
        /// todo - Remove the current status effect from the character.
        /// </summary>
        /// <param name="targetCharacter"></param>
        /// <param name="combo"></param>
        public int ApplyCombo(Character targetCharacter, ComboAction combo)
        {
            if (!targetCharacter.HasStatus)
                return 0;

            ComboEvent?.Invoke(this, new ComboEventArgs(targetCharacter.Id, combo));

            //if the player scores a combo they gain potential. if their commander gets comboed they lose potential (uh-oh!)
            if (ActiveCharacter.IsHero)
                GainPotential();

            if (targetCharacter == Commander)
            {
                SendMessage("Commander Comboed\nPotential Down");
                LosePotential(1);
            }

            var status = targetCharacter.StatusEffects.First();

            var count = targetCharacter.StatusEffects.Count(e => e.StatusEffectType == status.StatusEffectType);

            foreach (var statusEffect in targetCharacter.StatusEffects.Where(s =>
                s.StatusEffectType == status.StatusEffectType).ToList())
            {
                targetCharacter.StatusEffects.Remove(statusEffect);

                StatusRemovedEvent?.Invoke(this, new StatusEventArgs(targetCharacter.Id, statusEffect));
            }

            return count;
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

                    var tile = Map[destinationPos];

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

        public void CreateTileEffect(HexCoordinate location, TileEffectType effectType)
        {
            var tileEffect = new TileEffect(location);
            TileEffects.Add(tileEffect);

            SpawnTileEffectEvent?.Invoke(this, new SpawnTileEffectEventArgs()
            {
                Id = tileEffect.Guid,
                Position = location,
                Type = effectType
            });
        }

        #endregion

        #endregion

        #endregion

        #region Public Accessor Methods

        public Tile GetTileAtCoordinate(HexCoordinate coordinate)
	    {
		    return _map[coordinate];
	    }

        public Character GetCharacter(Guid characterId)
        {
            return _characters.FirstOrDefault(ch => ch.Id == characterId);
        }

        public Character GetCharacterAtCoordinate(HexCoordinate coordinate)
        {
            return LivingCharacters.FirstOrDefault(character => character.Position == coordinate);
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
			return !LivingCharacters.Any(character => character.Position == position);
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
                    var hexToCheck = ActiveCharacter.Position + (direction * (i + 1));

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
                    var hexToCheck = ActiveCharacter.Position + (direction * (i + 1));

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
                    var hexToCheck = ActiveCharacter.Position + (direction * (i + 1));

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

            GetVisibleTilesRecursive(targets, objectCharacter.Position, ActiveCharacter.Position, range);

            return targets;
        }

        /// <summary>
        /// Get all the visible tiles within range of a target position
        /// </summary>
        public List<HexCoordinate> GetVisibleTilesInRangeIgnoreUnits(Character objectCharacter, int range)
        {
            var targets = new List<HexCoordinate>() { objectCharacter.Position };

            GetVisibleTilesRecursive(targets, objectCharacter.Position, ActiveCharacter.Position, range, 0, true);

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
        /// If no path can be found returns null.
        ///
        /// A* is fuckin' rad.
        ///
        /// Rad as shit.
        ///
        /// Lots of comments on this - DON'T DELETE THEM!
        ///
        /// DO UPDATE THEM!
        /// </summary>
        public List<HexCoordinate> FindShortestPath(HexCoordinate start, HexCoordinate destination, MovementType movementType = MovementType.NormalMove)
        {
            if (!_map.ContainsKey(start) || !_map.ContainsKey(destination))
                return null;

            //data structure map such that key : a tile we've looked at one or more times, value : the previous tile in the shortest path to the hex in the key.
            var ancestorMap = new Dictionary<HexCoordinate, HexCoordinate> { { start, null } };

            //a data structure that holds the shortest distance found to each tile that we've searched
            var pathValues = new Dictionary<HexCoordinate, int> { { start, 0 } };

            //data structure holding the estimated path length from the start to the destination for each tile we've searched.
            var tileEstimates = new Dictionary<HexCoordinate, float> { { start, 0 } };

            HexCoordinate current = start;

            while (tileEstimates.Any())
            {
                //get the current best estimated tile
                //this is the tile we *think* probably leads to the shortest path to the destination
                current = tileEstimates.OrderBy(data => data.Value).First().Key;
                tileEstimates.Remove(current);

                if (current == destination)
                    break;

                //look at all of the best estimated tile's neighbors
                foreach (var neighbor in _map.GetNeighborCoordinates(current))
                {
                    //tile validation goes here.
                    if (!IsTilePassable(movementType, neighbor))
                        continue;

                    //check if the tile is water or lava.
                    if ((_map[neighbor].TerrainType == TerrainType.Water
                        || _map[neighbor].TerrainType == TerrainType.Lava)
                        && neighbor != destination)
                        continue;

	                //nodes are always one space away - hexgrid!
                    //BUT hexes have different movement costs to move through!
                    //the path from the start to the tile we're looking at now is the path the
                    var pathLengthToNeighbor = pathValues[current] + (int)GetTileMovementCost(neighbor);

                    //estimate the neighbor and add it to the list of estimates or update it if it's already in the list
                    if (!pathValues.ContainsKey(neighbor) || pathValues[neighbor] > pathLengthToNeighbor)
                    {
                        //deliberate truncation in cast.
                        pathValues[neighbor] = (int)pathLengthToNeighbor;

                        //heuristic for "distance to destination tile" is just absolute distance between current tile and the destination
                        float estimate = pathLengthToNeighbor + _map.DistanceBetweenPoints(neighbor, destination);

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
            path.RemoveAt(0);
            return path;
        }

        #endregion

        #region Private Helper Methods

        private bool IsTilePassable(MovementType movementType, HexCoordinate coordinate)
	    {
		    //tile validation goes here.
		    if (!IsHexWalkable(coordinate)) return false;

            var character = LivingCharacters.FirstOrDefault(c => c.Position == coordinate);
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
                    return true;
			    case MovementType.Etheral:
				    break;
			    case MovementType.Flying:
				    break;
			    default:
				    return false;
			}

		    return true;
	    }

        private float GetTileMovementCost(HexCoordinate coordinate)
        {
            if (!TileEffects.Any(te => te.Position == coordinate))
                return _map[coordinate].MovementCost;

            return TileEffects.Where(te => te.Position == coordinate).Sum(te => te.MovementModifier) +
                   _map[coordinate].MovementCost;
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
		    return _map[co].IsWalkable;
	    }

        private bool IsHexOpaque(HexCoordinate coordinate)
        {
            return _map[coordinate].BlocksLOS;
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

                int movementCost = (int)GetTileMovementCost(coord);
                TerrainType terrainType = _map[coord].TerrainType;

                //if this tile is not already in the list
                if (!neighbours.Contains(coord)
                    && searchDepth + movementCost <= maxSearchDepth //and we have enough movement to walk to this tile
                    && IsTileEmpty(coord)) //and the tile is empty
                {
                    neighbours.Add(coord);//then add it to the list.
                }

                if (terrainType == TerrainType.Water
                    || terrainType == TerrainType.Lava)
                    continue;

                GetWalkableNeighboursRecursive(neighbours, coord, movementType,
                    maxSearchDepth,
                    searchDepth + movementCost);//deliberate truncation in cast.
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

        //Get nearest adjacent tile to a target destination.
        private HexCoordinate GetNearestTileAdjacentToDestination(HexCoordinate start, HexCoordinate end)
        {
            var neighbours = _map.GetNeighborCoordinates(end);

            int distance = 1000;
            HexCoordinate nearest = null;

            foreach (var neighbor in neighbours)
            {
                if(!IsHexPassable(neighbor))
                    continue;

                var delta = _map.DistanceBetweenPoints(start, neighbor);
                if (delta < distance)
                {
                    nearest = neighbor;
                    distance = delta;
                }
            }

            return nearest;
        }

	    private Character FindCharacterById(Guid characterId)
	    {
		    return _characters.FirstOrDefault(cha => cha.Id == characterId);
	    }

        #region Private Update Methods

        private void ZombieTurn(Character character)
        {
            var position = character.Position;
            int characterMovement = character.Movement;
            List<HexCoordinate> shortestPath = null;
            List<HexCoordinate> pathToWalk = new List<HexCoordinate>();
            int shortestPathLength = int.MaxValue;
            Character closestHero = null;

            //find the closest hero
            foreach (var hero in Heroes)
            {
                var nearestNeighbour = GetNearestTileAdjacentToDestination(position, hero.Position);

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
                //if we can hit the hero, hit them now and end turn. - don't move.
                if (action.IsValidTarget(character, closestHero.Position, this) &&
                    action.IsDetonator == closestHero.HasStatus
                    && action.IsDetonator == closestHero.HasStatus && action.IsAvailable(character))
                {
                    NotifyAction(action, character);
                    ApplyDamage(closestHero, action.Power * character.Power);
                    ApplyStatus(closestHero, action.StatusEffect);

                    action.Combo?.TriggerAsync(character, new DummyInputProvider(closestHero.Position), this);

                    return;
                }
            }

            //if we couldn't reach the closest hero move towards them.
            //if we found a path to a hero
            if (!character.CanMove) return;

            //get all the tiles to which the zombie COULD move
            var tilesInRange = GetValidDestinations(character, character.Movement);

            float shortestDistance = 100;
            HexCoordinate destination = null;

            //look at all the possible destinations and get the one which is closest to a hero
            foreach (var tile in tilesInRange)
            {
                var distanceToHeroes = Heroes.Select(data => _map.DistanceBetweenPoints(tile, data.Position));

                var distance = (float)distanceToHeroes.Sum() / (float)Heroes.Count();
                if (distance < shortestDistance)
                {
                    shortestDistance = distance;
                    destination = tile;
                }
            }
            if (destination != null)
                MoveCharacterTo(character, destination);

            foreach (var action in character.Actions.Where(action =>
                action.IsValidTarget(character, closestHero.Position, this)
                && action.IsDetonator == closestHero.HasStatus && action.IsAvailable(character)))
            {
                NotifyAction(action, character);
                ApplyDamage(closestHero, action.Power * character.Power);
                ApplyStatus(closestHero, action.StatusEffect);

                action.Combo?.TriggerAsync(character, new DummyInputProvider(closestHero.Position), this);

                return;
            }
        }

        private void ZombieKingTurn(Character character)
        {
            var position = character.Position;
            int shortestPathLength = int.MaxValue;
            Character closestHero = null;

            //find the closest hero
            foreach (var hero in Heroes)
            {
                var nearestNeighbour = GetNearestTileAdjacentToDestination(position, hero.Position);

                if (nearestNeighbour == null)
                    continue;

                var path = FindShortestPath(position, nearestNeighbour);

                if (path == null) continue;

                if (path.Count >= shortestPathLength) continue;

                shortestPathLength = path.Count;
                closestHero = hero;
            }

            if (closestHero != null && character.CanMove)
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
                    if (destination != null)
                        MoveCharacterTo(character, destination);
                }
            }

            var zombies = LivingCharacters.Where(c => !c.IsHero && c.MonsterType == MonsterType.Zombie && c.IsAlive).ToList();

            var rand = new Random(DateTime.Now.Millisecond);

            if (rand.Next(0, 10) >= zombies.Count)
            {
                //spawn zombie
                var zombie = CreateZombie();

                MessageEvent?.Invoke(this, new MessageEventArgs("Zombie Summon"));

                foreach (var tile in _map.GetNeighborCoordinates(character.Position))
                {
                    //one unit per tile and only deploy to walkable spaces.
                    if (IsHexPassable(tile))
                    {
                        zombie.SpawnAt(tile);
                        Characters.Add(zombie);
                        MoveCharacterTo(zombie, position);
                        SpawnCharacterEvent?.Invoke(this, new SpawnChracterEventArgs
                        {
                            MonsterType = MonsterType.Zombie,
                            Character = zombie
                        });
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
        
        #endregion

        #endregion


	}
}
