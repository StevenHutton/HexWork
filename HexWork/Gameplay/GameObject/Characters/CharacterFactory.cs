using System;
using System.Collections.Generic;
using System.Linq;
using HexWork.Gameplay.Actions;
using HexWork.Gameplay.Interfaces;
using HexWork.UI;
using MonoGameTestProject.Gameplay;

namespace HexWork.Gameplay.GameObject.Characters
{
    public static class CharacterFactory
    {
        #region Attributes

        private static HexAction _moveAction = new MoveAction("Move") { Range = 0 };
        
        private static PotentialGainAction _potentialGainAction = new PotentialGainAction("Wind", null, null, null, null);

        private static HexAction _zombieGrab = new HexAction(name: "Zombie Grab",
            statusEffect: new ImmobalisedEffect { StatusEffectType = StatusEffectType.Rooted },
            combo: null,
            targetDelegate: TargetingHelper.GetValidTargetTilesNoLos) { Range = 1, Power = 2 };

        private static HexAction _zombieBite = new HexAction(name: "Zombie Bite",
            combo: new DamageComboAction() { Power = 2 },
            targetDelegate: TargetingHelper.GetValidTargetTilesNoLos) { Range = 1, Power = 2 };

        static TargetPattern _whirlWindTargetPattern = new TargetPattern(new HexCoordinate(1, 0, -1),
            new HexCoordinate(1, -1, 0),
            new HexCoordinate(0, -1, 1),
            new HexCoordinate(-1, 0, 1),
            new HexCoordinate(-1, 1, 0),
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

        #endregion

        #region Status Effects

        private static DotEffect _fireStatus;
        private static DotEffect _bleedingStatus;
        private static FreezeEffect _freezeEffect;
        
        #endregion

        #endregion

        #region Create Characters

        public static Character CreateMajin()
        {
            var majinCharacter = new Character("Majin", 100, 100, 5)
            {
                IsHero = true,
                MovementType = MovementType.MoveThroughHeroes
            };

            var burningBolt = new HexAction("Fire Bolt",
                TargetingHelper.GetValidAxisTargetTilesLos)
            {
                Range = 3,
                StatusEffect = _fireStatus,
                PushForce = 1,
                PushFromCaster = true
            };

            var exBurningBoltAction = new HexAction("Fire Wall! (1)",
                TargetingHelper.GetValidAxisTargetTilesLosIgnoreUnits,
                _fireStatus, null,
                _rotatingLinePattern)
            {
                PotentialCost = 1,
                Range = 3,
                TileEffect = _fireEffect
            };

            var ringofFire = new HexAction("Ring of Fire! (2)",
                TargetingHelper.GetValidAxisTargetTilesLosIgnoreUnits,
                _fireStatus, null,
                _whirlWindTargetPattern)
            {
                PotentialCost = 2,
                Range = 3,
                TileEffect = _fireEffect
            };

            var lightningBolt = new HexAction("Lightning Bolt (1)", TargetingHelper.GetValidAxisTargetTilesLosIgnoreUnits, null, new SpreadStatusCombo() { PushForce = 1, PushFromCaster = false})
            {
                Range = 3,
                Power = 2,
                PotentialCost = 1
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
            var shotgunBlast = new LineAction("Shotgun Blast! (1)",
                TargetingHelper.GetValidAxisTargetTilesLos,
                null, new DamageComboAction(),
                _cornerPattern)
            {
                PotentialCost = 1,
                Power = 3,
                Range = 2,
                TileEffect = _windEffect
            };

            var shovingSnipeAction = new HexAction(name: "Shoving Snipe",
                targetDelegate: TargetingHelper.GetValidAxisTargetTilesLos,
                combo: null)
            {
                Power = 2,
                Range = 5,
                PushForce = 2,
                TileEffect = _iceEffect
            };

            var detonatingSnipeActionEx = new HexAction("Perfect Snipe! (1)",
                TargetingHelper.GetValidAxisTargetTilesLos,
                null,
                new DamageComboAction { Power = 4 })
            {
                PotentialCost = 1,
                Power = 1,
                Range = 5
            };

            //create gunner hero
            var gunnerCharacter = new Character("Gunner", 60, 100, 4)
            {
                IsHero = true,
                MovementType = MovementType.MoveThroughHeroes
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
                TargetingHelper.GetValidTargetTilesLos,
                _bleedingStatus)
            {
                Range = 2,
                FollowUpAction = new FixedMoveAction("Shift") { Range = 1, TileEffect = _windEffect }
            };

            var shurikenPattern = new TargetPattern(new HexCoordinate(-1, 1), new HexCoordinate(0, -1),
                new HexCoordinate(1, 0));
            var shurikenHailActionEx = new HexAction("Shuriken Hail!",
                TargetingHelper.GetValidTargetTilesLosIgnoreUnits,
                _bleedingStatus,
                null, shurikenPattern)
            {
                PotentialCost = 2,
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
            var ninjaCharacter = new Character("Ninja", 80, 80, 4)
            {
                IsHero = true,
                MovementType = MovementType.MoveThroughHeroes
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
            var pushingFist = new HexAction("Pushing Fist", TargetingHelper.GetValidTargetTilesLos)
            {
                Range = 1,
                Power = 3,
                PushForce = 2
            };

            var overwhelmingStrike = new HexAction("Overwhelming Strike! (1)", TargetingHelper.GetValidTargetTilesLos,
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
            var ironSoulCharacter = new Character("Iron Soul", 200, 120, 3)
            {
                IsHero = true,
                MovementType = MovementType.MoveThroughHeroes,
                Power = 15
            };
            ironSoulCharacter.AddAction(_moveAction);
            ironSoulCharacter.AddAction(vampiricStrike);
            ironSoulCharacter.AddAction(pushingFist);
            ironSoulCharacter.AddAction(overwhelmingStrike);
            ironSoulCharacter.AddAction(exDetonatingSlash);
            ironSoulCharacter.AddAction(_potentialGainAction);
            
            return ironSoulCharacter;
        }

        public static Character CreateBarbarian()
        {
            var spreadStatusCombo = new SpreadStatusCombo { AllySafe = true, Power = 3 };
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

            var whirlwindAttack = new HexAction("Spin Attack", TargetingHelper.GetValidTargetTilesLos, null, new DamageComboAction(),
                _whirlWindTargetPattern)
            {
                Power = 1,
                PotentialCost = 1,
                Range = 0,
                PushForce = 1,
                PushFromCaster = true
            };

            //create Barbarian hero
            var barbarianCharacter = new Character("Barbarian", 150, 100, 2)
            {
                IsHero = true,
                MovementType = MovementType.MoveThroughHeroes,
                Power = 15
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

            for (var i = 0; i < (difficulty*2)+1; i++)
            {
                var zombie = CreateZombie(i);
                characters.Add(zombie);
            }

            return characters;
        }

        public static void CreateActions()
        {
            _fireEffect = new TileEffect
            {
                Damage = 15,
                Effect = _fireStatus,
                Name = "Fire",
                Health = 5,
                MaxHealth = 5,
                BlocksMovement = false,
            };

            _iceEffect = new TileEffect
            {
                Damage = 0,
                Effect = null,
                Name = "Ice",
                MovementModifier = 100,
                Health = 50,
                MaxHealth = 50,
                BlocksMovement = true,
            };

            _windEffect = new TileEffect
            {
                Damage = 0,
                Effect = null,
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
                Duration = 1,
                StatusEffectType = StatusEffectType.Bleeding
            };

            _freezeEffect = new FreezeEffect
            {
                Name = "Freeze",
                Duration = 1,
                StatusEffectType = StatusEffectType.Frozen
            };

            _fireEffect.Effect = _fireStatus;
            _fireStatus.TileEffect = _fireEffect;
            _iceEffect.Effect = _freezeEffect;
        }

        private static Character CreateZombieKing()
        {
            var zombieKing = new Character($"Zom-boy King", 160, 120, 1)
            {
                MonsterType = MonsterType.ZombieKing
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
            var zombie = new Character($"Zom-boy {i}", 60, 100, 0)
            {
                Power = 5,
            };
            zombie.AddAction(new FixedMoveAction("Shamble"){Range = 1});
            zombie.AddAction(_zombieGrab);
            zombie.AddAction(_zombieBite);
            zombie.DoTurn = ZombieTurn;
            return zombie;
        }

        #endregion

        #region TurnFunctions

        public static void ZombieTurn(IGameStateObject gameState, Character character)
        {
            var position = character.Position;
            int shortestPathLength = int.MaxValue;
            Character closestHero = null;
            var heroes = gameState.CurrentGameState.Heroes;

            //find the closest hero
            foreach (var hero in heroes)
            {
                var nearestNeighbour = GetNearestPassableTileAdjacentToDestination(position, hero.Position, gameState);
                if (nearestNeighbour == null)
                    continue;
                var path = gameState.FindShortestPath(position, nearestNeighbour);
                if (path == null) continue;
                if (path.Count >= shortestPathLength) continue;
                shortestPathLength = path.Count;
                closestHero = hero;
            }
            if (closestHero == null)
                return;

            //loop through available actions
            foreach (var action in character.Actions)
            {
                //if we can hit the hero, hit them now and end turn. - don't move.
                if (action.IsValidTarget(character, closestHero.Position, gameState) &&
                    action.IsDetonator == closestHero.HasStatus
                    && action.IsDetonator == closestHero.HasStatus)
                {
                    gameState.NotifyAction(action, character);
                    gameState.ApplyDamage(closestHero, action.Power * character.Power);
                    gameState.ApplyStatus(closestHero, action.StatusEffect);
                    action.Combo?.TriggerAsync(character, new DummyInputProvider(closestHero.Position), gameState);
                    return;
                }
            }
            //if we couldn't reach the closest hero move towards them.
            //if we found a path to a hero
            if (!character.CanMove) return;

            //get all the tiles to which the zombie COULD move
            var tilesInRange = character.Actions.FirstOrDefault(data => data.Name == "Shamble")?.GetValidTargets(character, gameState) ?? new List<HexCoordinate>();

            float shortestDistance = 100;
            HexCoordinate destination = null;
            //look at all the possible destinations and get the one which is closest to a hero
            foreach (var tile in tilesInRange)
            {
                var distanceToHeroes = heroes.Select(data => GameState.DistanceBetweenPoints(tile, data.Position));
                var distance = (float)distanceToHeroes.Sum() / (float)heroes.Count();
                if (distance < shortestDistance)
                {
                    shortestDistance = distance;
                    destination = tile;
                }
            }
            if (destination != null)
                gameState.MoveEntityTo(character, destination);
            foreach (var action in character.Actions.Where(action =>
                action.IsValidTarget(character, closestHero.Position, gameState)
                && action.IsDetonator == closestHero.HasStatus))
            {
                gameState.NotifyAction(action, character);
                gameState.ApplyDamage(closestHero, action.Power * character.Power);
                gameState.ApplyStatus(closestHero, action.StatusEffect);
                action.Combo?.TriggerAsync(character, new DummyInputProvider(closestHero.Position), gameState);
                return;
            }
        }

        private static void ZombieKingTurn(IGameStateObject gameState, Character character)
        {
            var position = character.Position;
            int shortestPathLength = int.MaxValue;
            Character closestHero = null;
            var heroes = gameState.CurrentGameState.Heroes;

            //find the closest hero
            foreach (var hero in heroes)
            {
                var nearestNeighbour = GetNearestPassableTileAdjacentToDestination(position, hero.Position, gameState);
                if (nearestNeighbour == null)
                    continue;
                var path = gameState.FindShortestPath(position, nearestNeighbour);
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
                    var tilesInRange = character.Actions.FirstOrDefault(data => data.Name == "Shamble")?.GetValidTargets(character, gameState) ?? new List<HexCoordinate>();

                    float greatestDistance = 0;
                    HexCoordinate destination = null;
                    //look at all the possible destinations and get the one which is the furthest average distance away from heroes
                    foreach (var tile in tilesInRange)
                    {
                        var distanceToHeroes = heroes.Select(data => GameState.DistanceBetweenPoints(tile, data.Position));
                        var distance = (float)distanceToHeroes.Sum() / (float)heroes.Count();
                        if (distance > greatestDistance)
                        {
                            greatestDistance = distance;
                            destination = tile;
                        }
                    }
                    if (destination != null)
                        gameState.MoveEntityTo(character, destination);
                }
            }
            var zombies = gameState.CurrentGameState.Enemies.Where(c => !c.IsHero && c.MonsterType == MonsterType.Zombie && c.IsAlive).ToList();
            var rand = new Random(DateTime.Now.Millisecond);
            
            if (rand.Next(0, 10) >= zombies.Count)
            {
                character.Actions.FirstOrDefault(data => data.Name == "Summon Zombie")?
                    .TriggerAsync(character, null, gameState);
            }
            else
            {
                character.Actions.FirstOrDefault(data => data.Name == "Zombie Rush")
                    ?.TriggerAsync(character, null, gameState);
            }
        }

        #region Turn Helper

        private static HexCoordinate GetNearestPassableTileAdjacentToDestination(HexCoordinate start, HexCoordinate end, IGameStateObject gameState)
        {
            var neighbours = GameState.GetNeighbours(end);

            //if the start tile is adjacent then there isn't a nearer tile.
            if (neighbours.Contains(start))
                return start;

            int distance = 1000;
            HexCoordinate nearest = null;
            bool found = false;
            while (!found)
            {
                foreach (var neighbor in neighbours)
                {
                    var delta = GameState.DistanceBetweenPoints(start, neighbor);
                    if (delta <= distance)
                    {
                        nearest = neighbor;
                        distance = delta;

                        if (gameState.IsHexPassable(neighbor))
                            found = true;
                    }
                }

                neighbours = GameState.GetNeighbours(nearest);
                if (neighbours.Contains(end))
                    neighbours.Remove(end);

                if (neighbours.Contains(start))
                    neighbours.Remove(start);
            }

            return nearest;
        }

        #endregion

        #endregion
    }
}