using System;
using System.Collections.Generic;
using System.Linq;
using HexWork.Gameplay.Actions;
using HexWork.Gameplay.Interfaces;
using HexWork.Gameplay.StatusEffects;
using HexWork.UI;

namespace HexWork.Gameplay.GameObject.Characters
{
    public static class CharacterFactory
    {
        #region Attributes

        private static HexAction _moveAction = new MoveAction("Move", TargetType.Move) { Range = 0, PotentialCost = 0 };
        
        private static PotentialGainAction _potentialGainAction = new PotentialGainAction("Charge Up (Ends Turn)", TargetType.Free, null, null, null) { PotentialCost = 0 };

        private static HexAction _zombieGrab = new HexAction(name: "Zombie Grab",
            statusEffect: new ImmobalisedEffect { StatusEffectType = StatusEffectType.Rooted },
            combo: null,
            targetType: TargetType.Free) { Range = 1, Power = 2 };

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

        #region Status Effects

        private static DotEffect _fireStatus;
        private static DotEffect _bleedingStatus;
        private static FreezeEffect _freezeEffect;
        private static ChargedEffect _electricStatusEffect;
        
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
                StatusEffect = _fireStatus,
                PushForce = 1,
                PushFromCaster = true,
                TileEffect = _fireEffect
            };

            var exBurningBoltAction = new HexAction("Fire Wall!",
                TargetType.AxisAlignedIgnoreLos,
                _fireStatus, null,
                _rotatingLinePattern)
            {
                PotentialCost = 1,
                Range = 3,
                TileEffect = _fireEffect
            };

            var ringofFire = new HexAction("Ring of Fire!",
                TargetType.AxisAlignedIgnoreLos,
                _fireStatus, null,
                _whirlWindTargetPattern)
            {
                PotentialCost = 2,
                Range = 3,
                TileEffect = _fireEffect
            };

            var lightningBolt = new HexAction("Lightning Bolt", TargetType.FreeIgnoreUnits, null, new SpreadStatusCombo() { PushForce = 1, PushFromCaster = false})
            {
                Range = 3,
                Power = 2,
                PotentialCost = 0,
                TileEffect = _electricityEffect,
                StatusEffect = _electricStatusEffect
            };

            //create majin hero            
            majinCharacter.AddAction(_moveAction);
            majinCharacter.AddAction(burningBolt);
            majinCharacter.AddAction(exBurningBoltAction);
            majinCharacter.AddAction(ringofFire);
            majinCharacter.AddAction(lightningBolt);
            majinCharacter.AddAction(_potentialGainAction);

            return majinCharacter;
        }

        public static Character CreateGunner()
        {
            var shotgunBlast = new LineAction("Shotgun Blast!",
                TargetType.Free,
                null, new DamageComboAction(),
                _cornerPattern)
            {
                PotentialCost = 1,
                Power = 3,
                Range = 2,
                TileEffect = _windEffect
            };

            var shovingSnipeAction = new HexAction(name: "Shoving Snipe",
                targetType: TargetType.AxisAligned,
                combo: null)
            {
                Power = 2,
                Range = 5,
                PushForce = 2
            };

            var detonatingSnipeActionEx = new HexAction("Perfect Snipe!",
                TargetType.AxisAligned,
                null,
                new DamageComboAction { Power = 5 })
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
            gunnerCharacter.AddAction(detonatingSnipeActionEx);
            gunnerCharacter.AddAction(shotgunBlast);
            gunnerCharacter.AddAction(_potentialGainAction);
            return gunnerCharacter;
        }

        public static Character CreateNinja()
        {
            var shurikenHailAction = new HexAction("Shuriken",
                TargetType.Free,
                _bleedingStatus)
            {
                Range = 2,
                FollowUpAction = new FixedMoveAction("Shift", TargetType.FixedMove) { Range = 1, TileEffect = _windEffect }
            };

            var shurikenPattern = new TargetPattern(new HexCoordinate(-1, 1), new HexCoordinate(0, -1),
                new HexCoordinate(1, 0));
            var shurikenHailActionEx = new HexAction("Shuriken Hail!",
                TargetType.Free,
                _bleedingStatus,
                null, shurikenPattern)
            {
                PotentialCost = 2,
                Range = 3
            };

            var swapAction = new SwapAction("Swap Positions", TargetType.Free)
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
            ninjaCharacter.AddAction(shurikenHailAction);
            ninjaCharacter.AddAction(shurikenHailActionEx);
            ninjaCharacter.AddAction(swapAction);
            ninjaCharacter.AddAction(_potentialGainAction);

            return ninjaCharacter;
        }

        public static Character CreateIronSoul()
        {
            var pushingFist = new HexAction("Heavy Blow", TargetType.Free, null, new StatusCombo()
            {
                Power = 2,
                Effect = new ImmobalisedEffect()
                {
                    StatusEffectType = StatusEffectType.Rooted
                }
            })
            {
                Range = 1,
                Power = 2,
                PushForce = 2
            };

            var charge = new ChargeAction("Charge", TargetType.AxisAlignedIgnoreUnits) { Range = 2, Power = 2, PotentialCost = 1, PushForce = 1 };

            var stomp = new HexAction("Stomp", TargetType.Free, new ImmobalisedEffect(), null,
                _whirlWindTargetPattern)
            {
                Power = 1,
                PotentialCost = 1,
                Range = 0
            };

            var exDetonatingSlash =
            new HexAction("Massive Detonation!", TargetType.Free, null,
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
            ironSoulCharacter.AddAction(pushingFist);
            ironSoulCharacter.AddAction(charge);
            ironSoulCharacter.AddAction(stomp);
            ironSoulCharacter.AddAction(exDetonatingSlash);
            ironSoulCharacter.AddAction(_potentialGainAction);
            
            return ironSoulCharacter;
        }

        public static Character CreateBarbarian()
        {
            var spreadStatusCombo = new SpreadStatusCombo { AllySafe = true, Power = 1 };
            var detonatingSlash =
              new HexAction("Detonating Strike!", TargetType.Free, null, spreadStatusCombo)
              {
                  Range = 1,
                  PotentialCost = 1
              };

            var earthQuakeStrike = new LineAction("Earthquake Strike",
                TargetType.Free,
                new ImmobalisedEffect(),
                null,
                _xAxisLinePattern)
            {
                Range = 1,
                Power = 2
            };

            var whirlwindAttack = new HexAction("Spin Attack", TargetType.Free, null, new DamageComboAction(),
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
                Power = 12,
                CharacterType = CharacterType.Barbarian
            };

            barbarianCharacter.AddAction(_moveAction);
            barbarianCharacter.AddAction(earthQuakeStrike);
            barbarianCharacter.AddAction(whirlwindAttack);
            barbarianCharacter.AddAction(detonatingSlash);
            barbarianCharacter.AddAction(_potentialGainAction);
            
            return barbarianCharacter;
        }

        public static IEnumerable<Character> CreateHeroes()
        {
            CreateActions();

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

        public static void CreateActions()
        {
            _fireEffect = new TileEffect
            {
                Damage = 15,
                Name = "Fire",
                Health = 5,
                MaxHealth = 5,
                BlocksMovement = false,
            };

            _iceEffect = new TileEffect
            {
                Damage = 0,
                Name = "Ice",
                MovementModifier = 100,
                Health = 50,
                MaxHealth = 50,
                BlocksMovement = false,
            };

            _electricityEffect = new TileEffect
            {
                Damage = 0,
                Name = "Electricity",
                MovementModifier = 0,
                Health = 5,
                MaxHealth = 5,
                Potential = 1,
                BlocksMovement = false,
            };

            _windEffect = new TileEffect
            {
                Damage = 0,
                Name = "Wind",
                MovementModifier = -1,
                Health = 5,
                MaxHealth = 5,
                BlocksMovement = false,
            };

            _fireStatus = new DotEffect { Name = "Fire", TileEffect = _fireEffect, Damage = 10};

            _bleedingStatus = new DotEffect
            {
                Name = "Bleeding",
                Damage = 5,
                StatusEffectType = StatusEffectType.Bleeding
            };

            _freezeEffect = new FreezeEffect
            {
                Name = "Freeze",
                StatusEffectType = StatusEffectType.Frozen
            };

            _electricStatusEffect = new ChargedEffect
            {
                Name = "Electified",
                StatusEffectType = StatusEffectType.Electrified,

            };

            _fireEffect.Effect = _fireStatus;
            _fireStatus.TileEffect = _fireEffect;
            _iceEffect.Effect = _freezeEffect;
            _electricityEffect.Effect = _electricStatusEffect;
            _electricStatusEffect.TileEffect = _electricityEffect;
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

        public static BoardState ZombieTurn(BoardState state, IRulesProvider gameState, Character character)
        {
            var newState = state.Copy();

            var position = character.Position;
            int shortestPathLength = int.MaxValue;
            Character closestHero = null;
            var heroes = newState.Heroes;

            //find the closest hero
            foreach (var hero in heroes)
            {
                var nearestNeighbour = GetNearestPassableTileAdjacentToDestination(newState, position, hero.Position, gameState);
                if (nearestNeighbour == null)
                    continue;

                var path = BoardState.FindShortestPath(newState, position, nearestNeighbour, 200);
                if (path == null) continue;
                if (path.Count >= shortestPathLength) continue;
                shortestPathLength = path.Count;
                closestHero = hero;
            }
            if (closestHero == null)
                return newState;

            //loop through available actions
            foreach (var action in character.Actions)
            {
                //if we can hit the hero, hit them now and end turn. - don't move.
                if (gameState.IsValidTarget(newState, character, closestHero.Position, action.Range, action.TargetType)
                    && action.IsDetonator == closestHero.HasStatus
                    && action.IsDetonator == closestHero.HasStatus)
                {
                    newState = gameState.ApplyDamage(newState, closestHero.Id, action.Power * character.Power);
                    newState = gameState.ApplyStatus(newState, closestHero.Id, action.StatusEffect);
                    newState = action.Combo?.TriggerAsync(newState, character.Id, new DummyInputProvider(closestHero.Position), gameState).Result;
                    return newState;
                }
            }

            //if we couldn't reach the closest hero move towards them.
            if (!character.CanMove) return newState;

            //get all the tiles to which the zombie COULD move
            var tilesInRange = BoardState.GetWalkableAdjacentTiles(newState, character.Position, character.MovementType);
            float shortestDistance = 100;
            HexCoordinate destination = null;
            //look at all the possible destinations and get the one which is closest to a hero
            foreach (var tile in tilesInRange)
            {
                var distanceToHeroes = heroes.Select(data => BoardState.DistanceBetweenPoints(tile, data.Position));
                var distance = (float)distanceToHeroes.Sum() / (float)heroes.Count();
                if (distance < shortestDistance)
                {
                    shortestDistance = distance;
                    destination = tile;
                }
            }
            if (destination != null)
                newState = gameState.MoveEntity(newState, character.Id, new List<HexCoordinate> { destination });
            foreach (var action in character.Actions.Where(action =>
                gameState.IsValidTarget(newState, character, closestHero.Position, action.Range, action.TargetType)
                && action.IsDetonator == closestHero.HasStatus))
            {
                newState = gameState.ApplyDamage(newState, closestHero.Id, action.Power * character.Power);
                newState = gameState.ApplyStatus(newState, closestHero.Id, action.StatusEffect);
                newState = action.Combo?.TriggerAsync(newState, character.Id, new DummyInputProvider(closestHero.Position), gameState).Result;
                return newState;
            }

            return newState;
        }

        private static BoardState ZombieKingTurn(BoardState state, IRulesProvider gameState, Character character)
        {
            var newState = state.Copy();

            var position = character.Position;
            int shortestPathLength = int.MaxValue;
            Character closestHero = null;
            var heroes = newState.Heroes;

            //find the closest hero
            foreach (var hero in heroes)
            {
                var nearestNeighbour = GetNearestPassableTileAdjacentToDestination(newState, position, hero.Position, gameState);
                if (nearestNeighbour == null)
                    continue;
                var path = BoardState.FindShortestPath(newState, position, nearestNeighbour, 200);
                
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
                    var tilesInRange = BoardState.GetWalkableAdjacentTiles(newState, character.Position, character.MovementType);

                    float greatestDistance = 0;
                    HexCoordinate destination = null;
                    //look at all the possible destinations and get the one which is the furthest average distance away from heroes
                    foreach (var tile in tilesInRange)
                    {
                        var distanceToHeroes = heroes.Select(data => BoardState.DistanceBetweenPoints(tile, data.Position));
                        var distance = (float)distanceToHeroes.Sum() / (float)heroes.Count();
                        if (distance > greatestDistance)
                        {
                            greatestDistance = distance;
                            destination = tile;
                        }
                    }
                    if (destination != null)
                        newState = gameState.MoveEntity(newState, character.Id, new List<HexCoordinate> { destination });
                }
            }
            var zombies = newState.Enemies.Where(c => !c.IsHero && c.CharacterType == CharacterType.Zombie && c.IsAlive).ToList();
            var rand = new Random(DateTime.Now.Millisecond);
            
            if (rand.Next(0, 10) >= zombies.Count)
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

        #region Turn Helper

        private static HexCoordinate GetNearestPassableTileAdjacentToDestination(BoardState state, HexCoordinate start, HexCoordinate end, IRulesProvider gameState)
        {
            var neighbours = BoardState.GetNeighbours(end);

            //if the start tile is adjacent then there isn't a nearer tile.
            if (neighbours.Contains(start))
                return start;

            int distance = 1000;
            HexCoordinate nearest = null;
            HexCoordinate result = null;
            bool found = false;
            while (!found)
            {
                foreach (var neighbor in neighbours)
                {
                    var delta = BoardState.DistanceBetweenPoints(start, neighbor);
                    if (delta <= distance)
                    {
                        nearest = neighbor;
                        distance = delta;

                        if (BoardState.IsHexPassable(state, neighbor))
                        { 
                            found = true;
                            result = neighbor;
                        }
                    }
                }

                neighbours = BoardState.GetNeighbours(nearest);
                if (neighbours.Contains(end))
                    neighbours.Remove(end);

                if (neighbours.Contains(start))
                    neighbours.Remove(start);
            }

            return result;
        }

        #endregion

        #endregion
    }
}