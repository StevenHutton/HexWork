using System;
using System.Collections.Generic;
using System.Linq;
using HexWork.Gameplay.Actions;
using HexWork.Gameplay.Interfaces;
using HexWork.UI;

namespace HexWork.Gameplay.GameObject.Characters
{
    public static class CharacterFactory
    {
        #region Attributes

        private static HexAction _moveAction = new MoveAction("Move", TargetType.Move) { Range = 0, PotentialCost = 0 };
        
        private static PotentialGainAction _endTurnAction = new PotentialGainAction("Charge Up (Ends Turn)", TargetType.Free, null, null) { PotentialCost = 0 };

        private static HexAction _zombieGrab = new HexAction(name: "Zombie Grab",            
            combo: null,
            targetType: TargetType.Free) { Range = 1, Power = 2, Element = Element.Earth, };

        private static HexAction _zombieBite = new HexAction(name: "Zombie Bite",
            combo: new DamageComboAction() { Power = 2 },
            targetType: TargetType.Free) { Range = 1, Power = 2 };

        static TargetPattern _whirlWindTargetPattern = new TargetPattern(new HexCoordinate(1, 0, -1),
            new HexCoordinate(1, -1, 0),
            new HexCoordinate(0, -1, 1),
            new HexCoordinate(-1, 0, 1),
            new HexCoordinate(-1, 1, 0),
            new HexCoordinate(0, 1, -1));

        static TargetPattern _triangleTargetPattern = new TargetPattern(new HexCoordinate(1, -1, 0),
            new HexCoordinate(-1, 0, 1),
            new HexCoordinate(0, 1, -1));

        static TargetPattern _xAxisLinePattern = new TargetPattern(new HexCoordinate(0, 0, 0),
            new HexCoordinate(1, -1), new HexCoordinate(2, -2));

        static TargetPattern _rotatingLinePattern = new TargetPattern(new HexCoordinate(0, 0),
            new HexCoordinate(1, 0),
            new HexCoordinate(-1, 0));

        static TargetPattern _cornerPattern = new TargetPattern(new HexCoordinate(0, 0),
            new HexCoordinate(1, 0),
            new HexCoordinate(0, -1));

        #region TileEffects

        private static TileEffect _fireEffect;
        private static TileEffect _iceEffect;
        private static TileEffect _windEffect;
        private static TileEffect _electricityEffect;

        #endregion

        #endregion

        #region Create Characters

        public static Character CreateMajin()
        {
            var majinCharacter = new Character("Majin", 100, 100)
            {
                IsHero = true,
                MovementType = MovementType.MoveThroughHeroes,
                CharacterType = CharacterType.Majin
            };

            var burningBolt = new HexAction("Fire Bolt",
                TargetType.AxisAligned)
            {
                Range = 3,
                Element = Element.Fire,
                PushForce = 1,
                PushFromCaster = true
            };

            var fireWall = new HexAction("Fire Wall!",
                TargetType.AxisAlignedIgnoreLos,
                null,
                _rotatingLinePattern)
            {
                PotentialCost = 1,
                Range = 3,
                Element = Element.Fire,
            };

            var ringofFire = new HexAction("Ring of Fire!",
                TargetType.AxisAlignedIgnoreLos, null,
                _whirlWindTargetPattern)
            {
                PotentialCost = 2,
                Range = 3,
                Element = Element.Fire
            };

            var lightningBolt = new HexAction("Lightning Bolt", TargetType.FreeIgnoreUnits, new SpreadStatusCombo() { PushForce = 1, PushFromCaster = false})
            {
                Range = 3,
                Power = 2,
                PotentialCost = 0,
                Element = Element.Lightning,
            };

            //create majin hero
            majinCharacter.AddAction(_moveAction);
            majinCharacter.AddAction(burningBolt);
            //majinCharacter.AddAction(fireWall);
            //majinCharacter.AddAction(ringofFire);
            majinCharacter.AddAction(lightningBolt);
            majinCharacter.AddAction(_endTurnAction);

            return majinCharacter;
        }

        public static Character CreateGunner()
        {
            var shotgunBlast = new LineAction("Shotgun Blast!",
                TargetType.Free,
                new DamageComboAction(),
                _cornerPattern)
            {
                PotentialCost = 1,
                Power = 3,
                Range = 2,
                PushForce = 1,
                PushFromCaster = true,
                Element = Element.Wind
            };

            var shovingSnipeAction = new HexAction(name: "Shoving Snipe",
                targetType: TargetType.AxisAligned,
                combo: new DamageComboAction { Power = 3 })
            {
                Power = 2,
                Range = 5,
                PushForce = 1,
                Element = Element.Wind
            };

            var detonatingSnipeActionEx = new HexAction("Perfect Snipe!",
                TargetType.AxisAligned,
                new DamageComboAction { Power = 3 })
            {
                PotentialCost = 2,
                Power = 1,
                Range = 5
            };

            //create gunner hero
            var gunnerCharacter = new Character("Gunner", 60, 100)
            {
                IsHero = true,
                MovementType = MovementType.MoveThroughHeroes,
                CharacterType = CharacterType.Gunner
            };

            gunnerCharacter.AddAction(_moveAction);
            gunnerCharacter.AddAction(shovingSnipeAction);
            //gunnerCharacter.AddAction(detonatingSnipeActionEx);
            gunnerCharacter.AddAction(shotgunBlast);
            gunnerCharacter.AddAction(_endTurnAction);
            return gunnerCharacter;
        }

        public static Character CreateNinja()
        {
            var shurikenPattern = new TargetPattern(new HexCoordinate(-1, 1), new HexCoordinate(0, -1),
                new HexCoordinate(1, 0));
            
            var shurikenHailAction = new HexAction("Shuriken Hail!",
                TargetType.Free, 
                null, shurikenPattern)
            {
                PotentialCost = 1,
                Range = 3
            };

            var ninpoDash = new DashAction("Ninpo Dash", TargetType.AxisAlignedFixedMove, null, null)
            {
                Element = Element.Wind,
                Range = 3
            };

            var swapAction = new SwapAction("Swap Positions", TargetType.Free, new DamageComboAction())
            {
                Power = 3,
                AllySafe = false,
                PotentialCost = 2,
                Range = 5
            };

            //create ninja hero
            var ninjaCharacter = new Character("Ninja", 80, 80)
            {
                IsHero = true,
                MovementType = MovementType.MoveThroughHeroes,
                CharacterType = CharacterType.Ninja
            };
            ninjaCharacter.MovementSpeed = MovementSpeed.Fast;
            ninjaCharacter.AddAction(_moveAction);
            ninjaCharacter.AddAction(ninpoDash);
            //ninjaCharacter.AddAction(shurikenHailAction);
            ninjaCharacter.AddAction(swapAction);
            ninjaCharacter.AddAction(_endTurnAction);

            return ninjaCharacter;
        }

        public static Character CreateIronSoul()
        {
            var pushingFist = new HexAction("Heavy Blow", TargetType.Free, new StatusCombo()
            {
                Power = 2,
                Element = Element.Earth,
            })
            {
                Range = 1,
                Power = 2,
                PushForce = 3
            };

            var charge = new ChargeAction("Charge", TargetType.AxisAlignedFixedMove) { Range = 2, Power = 2, PotentialCost = 0, PushForce = 2 };

            var stomp = new HexAction("Stomp", TargetType.Free, null,
                _whirlWindTargetPattern)
            {
                Power = 1,
                PotentialCost = 0,
                Range = 0,
                Element = Element.Earth
            };

            var exDetonatingSlash =
            new HexAction("Massive Detonation!", TargetType.Free, 
                new ExploderCombo
                {
                    Power = 2,
                    Pattern = _whirlWindTargetPattern,
                    AllySafe = false
                })
            {
                Range = 1,
                PotentialCost = 1,
                Power = 3
            };

            //create Iron Soul hero
            var ironSoulCharacter = new Character("Iron Soul", 200, 120)
            {
                IsHero = true,
                MovementType = MovementType.MoveThroughHeroes,
                Power = 12,
                CharacterType = CharacterType.IronSoul
            };
            ironSoulCharacter.AddAction(_moveAction);
            //ironSoulCharacter.AddAction(pushingFist);
            ironSoulCharacter.AddAction(charge);
            //ironSoulCharacter.AddAction(stomp);
            ironSoulCharacter.AddAction(exDetonatingSlash);
            ironSoulCharacter.AddAction(_endTurnAction);
            
            return ironSoulCharacter;
        }

        public static Character CreateBarbarian()
        {
            var spreadStatusCombo = new SpreadStatusCombo
            { 
                AllySafe = true,
                Power = 1, 
                Pattern = new TargetPattern(new HexCoordinate(1, -1),
                    new HexCoordinate(2, -2), new HexCoordinate(3,-3))
            };
            var shockwavePunch =
              new HexAction("Shockwave Punch", TargetType.Free, spreadStatusCombo)
              {
                  Range = 1,
                  PotentialCost = 1
              };

            var earthQuakeStrike = new LineAction("Earthquake Strike",
                TargetType.Free,
                null,
                _xAxisLinePattern)
            {
                Range = 1,
                Power = 2,
                Element = Element.Earth
            };

            var whirlwindAttack = new HexAction("Spin Attack", TargetType.Free, new DamageComboAction(),
                _whirlWindTargetPattern)
            {
                Power = 2,
                PotentialCost = 1,
                Range = 0,
                PushForce = 1,
                PushFromCaster = true
            };

            //create Barbarian hero
            var barbarianCharacter = new Character("Barbarian", 150, 100)
            {
                IsHero = true,
                MovementType = MovementType.MoveThroughHeroes,
                MovementSpeed = MovementSpeed.Fast,
                Power = 12,
                CharacterType = CharacterType.Barbarian
            };

            barbarianCharacter.AddAction(_moveAction);
            barbarianCharacter.AddAction(earthQuakeStrike);
            //barbarianCharacter.AddAction(whirlwindAttack);
            barbarianCharacter.AddAction(shockwavePunch);
            barbarianCharacter.AddAction(_endTurnAction);
            
            return barbarianCharacter;
        }

        public static IEnumerable<Character> CreateHeroes()
        {
            return new List<Character>
            {
                CreateMajin(),
                CreateGunner(),
                CreateNinja(),
                CreateIronSoul(),
                CreateBarbarian()
            };
        }

        public static IEnumerable<Character> CreateEnemies(int difficulty = 1)
        {
            var characters = new List<Character>();

            for (int i = 0; i < difficulty; i++)
            {
                var zombieKing = CreateZombieKing();
                characters.Add(zombieKing);
            }

            for (var i = 0; i < (difficulty*2)+2; i++)
            {
                var zombie = CreateZombie(0);
                characters.Add(zombie);
            }

            return characters;
        }

        private static Character CreateZombieKing()
        {
            var zombieKing = new Character($"Zom-boy King", 160, 120)
            {
                CharacterType = CharacterType.ZombieKing
            };
            zombieKing.AddAction(_moveAction);
            zombieKing.AddAction(_zombieGrab);
            zombieKing.AddAction(_zombieBite);
            zombieKing.AddAction(new SpawnAction(){Name = "Summon Zombie"});
            zombieKing.AddAction(new CommandAction(){Name = "Zombie Rush"});
            zombieKing.DoTurn = ZombieKingTurn;
            return zombieKing;
        }

        public static Character CreateZombie(int i = 0)
        {
            var zombie = new Character($"Zom-boy {i}", 60, 100)
            {
                Power = 5,
            };
            zombie.AddAction(new FixedMoveAction("Shamble", TargetType.FixedMove) {Range = 1});
            zombie.AddAction(_zombieGrab);
            zombie.AddAction(_zombieBite);
            zombie.DoTurn = ZombieTurn;
            return zombie;
        }

        #endregion

        #region TurnFunctions

        public static BoardState ZombieAttack(BoardState state, IRulesProvider ruleProvider, Character character, out bool attacked)
        {
            attacked = false;
            var newState = state.Copy();
            var position = character.Position;
            Character closestHero = null;
            var heroes = newState.Heroes;

            //look for an adjacent hero
            foreach (var hero in heroes)
            {
                if (BoardState.DistanceBetweenPoints(hero.Position, position) == 1)
                {
                    closestHero = hero;
                    break;
                }
            }

            //if we found an adjacent hero, try to use any available actions
            if (closestHero != null)
            {               
                foreach (var action in character.Actions)
                {
                    //if we can hit the hero, hit them now and end turn. - don't move.
                    if (BoardState.IsValidTarget(newState, character, closestHero.Position, action, action.TargetType)
                        && action.IsDetonator == closestHero.HasStatus)
                    {
                        newState = ruleProvider.ApplyDamage(newState, closestHero.Id, action.Power * character.Power);
                        newState = ruleProvider.ApplyStatus(newState, closestHero.Id, action.Element);
                        if(action.Combo != null)
                            newState = action.Combo.TriggerAsync(newState, character.Id, new DummyInputProvider(closestHero.Position), ruleProvider).Result;
                        attacked = true;
                        break;
                    }
                }                
            }

            return newState;
        }

        public static BoardState ZombieTurn(BoardState state, IRulesProvider gameState, Character character)
        {
            var newState = state.Copy();
            var position = character.Position;

            bool attacked;
            newState = ZombieAttack(newState, gameState, character, out attacked);
            if (attacked)
                return newState;


            //if we can't move at this point, just give up and end turn
            if (newState.ActiveCharacterHasMoved) return newState;
            List<HexCoordinate> postentialDestinations = new List<HexCoordinate>();
            int shortestPathLength = int.MaxValue;
            List<HexCoordinate> shortestPath = null;

            //get the list of all possible destination tiles.
            foreach (var hero in newState.Heroes)
            {
                var walkableNeighbours = BoardState.GetWalkableAdjacentTiles(newState, hero.Position, 0);
                postentialDestinations.AddRange(walkableNeighbours.Keys.ToList());
            }

            foreach (var potentialDestination in postentialDestinations)
            {
                var path = BoardState.FindShortestPath(newState, position, potentialDestination, 50, MovementType.NormalMove);
                if (path == null) continue;

                if( path.Count() <= shortestPathLength)
                {
                    shortestPath = path;
                    shortestPathLength = path.Count();
                }
            }

            //if we can't find a path, give up
            if (shortestPath == null)
                return newState;

            HexCoordinate destination = shortestPath[0];

            newState = gameState.MoveEntity(newState, character.Id, new List<HexCoordinate> { destination });

            newState = ZombieAttack(newState, gameState, character, attacked: out _);

            return newState;
        }

        private static BoardState ZombieKingTurn(BoardState state, IRulesProvider gameState, Character character)
        {
            var newState = state.Copy();

            var position = character.Position;
            var heroes = newState.Heroes;

            var zombies = newState.Enemies.Where(c => !c.IsHero && c.CharacterType == CharacterType.Zombie).ToList();
            var rand = new Random(DateTime.Now.Millisecond);
            
            if (rand.Next(0, 10) >= zombies.Count())
            {
                newState = character.Actions.FirstOrDefault(data => data.Name == "Summon Zombie")?
                    .TriggerAsync(state, character.Id, null, gameState).Result;
            }
            else
            {
                newState = character.Actions.FirstOrDefault(data => data.Name == "Zombie Rush")
                    ?.TriggerAsync(state, character.Id, null, gameState).Result;
            }

            return newState;
        }

        #endregion
    }
}