using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HexWork.Gameplay.Actions;
using HexWork.Gameplay.GameObject;
using HexWork.Gameplay.GameObject.Characters;
using HexWork.Gameplay.Interfaces;
using HexWork.GameplayEvents;
using Microsoft.Xna.Framework;

namespace HexWork.Gameplay
{
    public class GameState : IGameStateObject
    {
        #region Attributes

        public const int ImpactDamage = 50;

        public const int CollisionDamage = 30;

        public const int MapWidth = 6;
        public const int MapHeight = 5;

        public BoardState CurrentGameState { get; set; }
        public IEnumerable<TileEffect> TileEffects => CurrentGameState.TileEffects;

        public static readonly HexCoordinate[] Directions = {
            new HexCoordinate(+1, -1, 0),
            new HexCoordinate(+1, 0, -1),
            new HexCoordinate(0, +1, -1),
            new HexCoordinate(-1, +1, 0),
            new HexCoordinate(-1, 0, +1),
            new HexCoordinate(0, -1, +1)
        };

        #endregion

        #region Events

        public event EventHandler<MoveEventArgs> CharacterMoveEvent;

        public event EventHandler<MoveEventArgs> CharacterTeleportEvent;

        public event EventHandler<EntityEventArgs> SpawnEntityEvent;

        public event EventHandler<EntityEventArgs> RemoveEntityEvent;
        
        public event EventHandler<DamageTakenEventArgs> TakeDamageEvent;
        
        public event EventHandler<EndTurnEventArgs> EndTurnEvent;

        public event EventHandler<ActionEventArgs> ActionEvent;

        public event EventHandler<ComboEventArgs> ComboEvent;

        public event EventHandler<StatusEventArgs> StatusAppliedEvent;

        public event EventHandler<StatusEventArgs> StatusRemovedEvent;

        public event EventHandler<PotentialEventArgs> PotentialChangeEvent;

        public event EventHandler<MessageEventArgs> MessageEvent;

        public event EventHandler<MessageEventArgs> GameOverEvent;

        #endregion

        #region Methods

        #region Initialisation 
        
        public GameState()
        {
            CurrentGameState = new BoardState(MapWidth, MapHeight);
        }

        public void CreateCharacters(int difficulty = 1)
        {
            CurrentGameState.Entities.AddRange(CharacterFactory.CreateHeroes());
            CurrentGameState.Entities.AddRange(CharacterFactory.CreateEnemies(difficulty));
        }

        #endregion

        #region Game Start
        
        public void StartGame()
        {
            var characters = CurrentGameState.Characters;

            //spawn enemies
            foreach (var character in CurrentGameState.Characters.Where(c => !c.IsHero))
            {
                var coordinate = GetRandomCoordinateInMap();

                //one unit per tile and only deploy to walkable spaces.
                while (characters.Select(cha => cha.Position).Contains(coordinate) || !CurrentGameState[coordinate].IsWalkable || !IsInEnemySpawnArea(coordinate))
                {
                    coordinate = GetRandomCoordinateInMap();
                }

                character.SpawnAt(coordinate);
                SpawnEntityEvent?.Invoke(this,
                    new EntityEventArgs()
                    {
                        Entity = character
                    });
            }

            //spawn heroes
            var spawnPoint = new HexCoordinate(-2, -2, 4);
            foreach (var character in CurrentGameState.Heroes)
            {
                var position = spawnPoint;
                var validTile = IsHexPassable(position);

                while (!validTile)
                {
                    position = GetNearestEmptyNeighbourRecursive(position, 9);
                    //tiles are only valid so long as they're walkable and have at least one passable neighbor.
                    validTile = IsHexPassable(position) &&
                                GetNeighbours(position).Any(IsHexPassable);
                }

                character.SpawnAt(position);
                SpawnEntityEvent?.Invoke(this,
                    new EntityEventArgs()
                    {
                        Entity = character
                    });
            }

            NextTurn();
        }
        
        #endregion

        #region Public Update Methods
        
        public void Update()
        {
            var activeCharacter = CurrentGameState.ActiveCharacter;

            if (!activeCharacter.IsAlive)
            {
                NextTurn();
                return;
            }

            if (activeCharacter.IsHero)
                return;

            //if they can move take their actions and end turn
            if (activeCharacter.HasActed)
            {
                NextTurn();
                return;
            }

            activeCharacter.DoTurn(this, activeCharacter);
            activeCharacter.HasActed = true;
        }

        public void NextTurn()
        {
            var activeCharacter = CurrentGameState.ActiveCharacter;

            //if we have an active character then update all the initiative values
            if (activeCharacter != null)
            {
                ResolveTileEffects(activeCharacter, activeCharacter.Position);
                ResolveTerrainEffects(activeCharacter, activeCharacter.Position);
                activeCharacter.EndTurn();

                var deltaTime = activeCharacter.TurnTimer;
                foreach (var character in CurrentGameState.Characters)
                {
                    character.TurnTimer -= deltaTime;
                }

                activeCharacter.TurnTimer += activeCharacter.TurnCooldown;

                //also apply any status effects for the active character that trigger at the end of the turn.
                foreach (var statusEffect in activeCharacter.StatusEffects.ToList())
                {
                    statusEffect.EndTurn(activeCharacter, this);

                    //if the effect is expired remove it
                    if (statusEffect.IsExpired && activeCharacter.StatusEffects.Contains(statusEffect))
                    {
                        activeCharacter.StatusEffects.Remove(statusEffect);

                        StatusRemovedEvent?.Invoke(this, new StatusEventArgs(activeCharacter.Id, statusEffect));
                    }
                }
            }

            //get the new active character
            activeCharacter = GetCharacterAtInitiative(0);
            activeCharacter.StartTurn();

            if (activeCharacter.IsHero)
                GainPotential(1);

            //apply any status effects for the new active character that trigger at the start of thier turn.
            foreach (var statusEffect in activeCharacter.StatusEffects)
            {
                statusEffect.StartTurn(activeCharacter, this);
            }

            //apply any status effects for characters that apply whenever initiative moves.
            foreach (var character in CurrentGameState.Characters)
            {
                foreach (var statusEffect in character.StatusEffects)
                {
                    statusEffect.Tick(character, this);
                }
            }

            EndTurnEvent?.Invoke(this, new EndTurnEventArgs(GetInitiativeList().ToList()));
            
            if (CurrentGameState.Enemies.All(c => c.MonsterType != MonsterType.ZombieKing))
            {
                SendMessage("Enemy Leader(s) Defeated");
                GameOverEvent?.Invoke(this, new MessageEventArgs("You Win!"));               
            }
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


        #region Gamestate Transforms

        public void SpawnCharacter(Character character)
        {
            CurrentGameState.Entities.Add(character);
            SpawnEntityEvent?.Invoke(this,
                new EntityEventArgs()
                {
                    Entity = character
                });
            TeleportEntityTo(character, character.Position);
        }

        public void MoveEntityTo(HexGameObject entity, HexCoordinate position)
        {
            List<HexCoordinate> path;
            if (entity is Character character)
                path = FindShortestPath(entity.Position, position, character.MovementType);
            else
                path = FindShortestPath(entity.Position, position);

            foreach (var coordinate in path)
            {
                CharacterMoveEvent?.Invoke(this, new MoveEventArgs
                {
                    CharacterId = entity.Id,
                    Destination = coordinate
                });

                entity.MoveTo(coordinate);
                ResolveTileEffects(entity, coordinate);
                ResolveTerrainEffects(entity, coordinate);
            }
        }

        public void TeleportEntityTo(HexGameObject gameObject, HexCoordinate position)
        {
            CharacterTeleportEvent?.Invoke(this,
                new MoveEventArgs
                {
                    CharacterId = gameObject.Id,
                    Destination = position
                });

            ResolveTileEffects(gameObject, position);
            ResolveTerrainEffects(gameObject, position);
            gameObject.MoveTo(position);
        }

        public void ResolveTileEffects(HexGameObject entity, HexCoordinate position)
        {
            var tileEffect = CurrentGameState.TileEffects.FirstOrDefault(data => data.Position == position);

            ResolveTileEffect(tileEffect, entity);
        }

        public void ResolveTileEffect(TileEffect tileEffect, HexGameObject entity = null)
        {
            if (tileEffect == null)
                return;

            if (!(entity is Character))
                return;

            tileEffect.TriggerEffect(this, (Character)entity);
            RemoveTileEffect(tileEffect);
        }

        public void RemoveTileEffect(TileEffect effect)
        {
            if (!CurrentGameState.Entities.Contains(effect)) return;

            CurrentGameState.Entities.Remove(effect);
            RemoveEntityEvent?.Invoke(this, new EntityEventArgs() { Entity = effect });
        }

        //when a character moves into a tile check to see if there're any terrain effects for moving into that tile.
        private void ResolveTerrainEffects(HexGameObject entity, HexCoordinate destination)
        {
            //don't count terrain effects from a tile you're standing. We don't punish players for leaving lava.
            ResolveTerrainEnterEffect(entity, CurrentGameState[destination]);
        }

        private void ResolveTerrainEnterEffect(HexGameObject entity, Tile tile)
        {
            if (!(entity is Character))
                return;

            var character = (Character) entity;

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

        public void NotifyAction(HexAction action, HexGameObject entity)
        {
            ActionEvent?.Invoke(this,
                new ActionEventArgs { Action = action });

            if(entity is Character character)
                character.CanAttack = false;
        }

        public int ApplyDamage(HexGameObject entity, int damage, string message = null)
        {
            entity.Health -= damage;

            if (!string.IsNullOrWhiteSpace(message))
                SendMessage(message, entity);

            TakeDamageEvent?.Invoke(this,
                new DamageTakenEventArgs { DamageTaken = damage, TargetCharacterId = entity.Id });

            CheckDied(entity);

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

        public void CheckDied(HexGameObject character)
        {
            //check to see if they died.
            if (character.Health <= 0)
            {
                CurrentGameState.Entities.Remove(character);
                RemoveEntityEvent?.Invoke(this, new EntityEventArgs() { Entity = character });
            }
        }

        public void ApplyStatus(HexGameObject entity, StatusEffect effect)
        {
            //todo - apply status effects based on status damage
            //for now we just always apply any relevant status effects
            if (effect == null) return;

            var effectToApply = effect.Copy();
            effectToApply.Reset();
            entity.StatusEffects.Add(effectToApply);

            StatusAppliedEvent?.Invoke(this, new StatusEventArgs(entity.Id, effectToApply));
        }

        /// <summary>
        /// Apply a combo effect to a target character if they're currently suffering a status effect.
        /// todo - Remove the current status effect from the character.
        /// </summary>
        /// <param name="targetCharacter"></param>
        /// <param name="combo"></param>
        public int ApplyCombo(HexGameObject targetEntity, DamageComboAction combo)
        {
            if (!targetEntity.HasStatus)
                return 0;

            ComboEvent?.Invoke(this, new ComboEventArgs(targetEntity.Id, combo));

            //if the player scores a combo they gain potential.
            if (CurrentGameState.ActiveCharacter.IsHero)
                GainPotential(1);
            
            var status = targetEntity.StatusEffects.First();

            var count = targetEntity.StatusEffects.Count(e => e.StatusEffectType == status.StatusEffectType);

            foreach (var statusEffect in targetEntity.StatusEffects.Where(s =>
                s.StatusEffectType == status.StatusEffectType).ToList())
            {
                targetEntity.StatusEffects.Remove(statusEffect);

                StatusRemovedEvent?.Invoke(this, new StatusEventArgs(targetEntity.Id, statusEffect));
            }

            return count;
        }

        public void ApplyPush(HexGameObject targetEntity, HexCoordinate direction, int distance = 0)
        {
            var targetCharacterPos = targetEntity.Position;
            var destinationPos = targetCharacterPos + direction;
            while (distance > 0)
            {
                if (!CurrentGameState.ContainsKey(destinationPos))
                {
                    ApplyDamage(targetEntity, distance * 15, "IMPACT");
                    distance = 0;
                }
                else if (IsHexPassable(destinationPos))
                {
                    MoveEntityTo(targetEntity, destinationPos);

                    var tile = CurrentGameState[destinationPos];

                    if (tile.TerrainType != TerrainType.Ice && tile.TerrainType != TerrainType.ThinIce)
                        distance--;

                    destinationPos = destinationPos + direction;
                }
                else if (!IsTileEmpty(destinationPos))
                {
                    var objectCharacter = GetEntityAtCoordinate(destinationPos);
                    ApplyDamage(targetEntity, distance * CollisionDamage, "Collision");
                    ApplyDamage(objectCharacter, distance * CollisionDamage, "Collision");
                    distance = 0;
                }
                else
                {
                    ApplyDamage(targetEntity, distance * ImpactDamage, "IMPACT");
                    distance = 0;
                }
            }
        }

        public void GainPotential(int potential = 1)
        {
            if (CurrentGameState.Potential + potential <= CurrentGameState.MaxPotential)
            {
                CurrentGameState.Potential += potential;
            }

            PotentialChangeEvent?.Invoke(this, new PotentialEventArgs(potential));
        }

        public void LosePotential(int potential = 1)
        {
            if (CurrentGameState.Potential >= potential)
            {
                CurrentGameState.Potential -= potential;
            }

            PotentialChangeEvent?.Invoke(this, new PotentialEventArgs(-potential));
        }

        public void CreateTileEffect(HexCoordinate location, TileEffect effect)
        {
            //don't create a tile effect on unpassable tiles, occupied tiles or tiles that already have effects
            if (!IsHexWalkable(location) || !IsTileEmpty(location) || CurrentGameState.TileEffects.Any(te => te.Position == location))
                return;

            var tileEffect = new TileEffect(effect, location);
            CurrentGameState.Entities.Add(tileEffect);

            SpawnEntityEvent?.Invoke(this, new EntityEventArgs
            {
                Entity = tileEffect
            });
        }

        #endregion
        
        #region Public Accessor Methods

        public Tile GetTileAtCoordinate(HexCoordinate coordinate)
        {
            return CurrentGameState[coordinate];
        }

        public Character GetCharacter(Guid characterId)
        {
            return CurrentGameState.Characters.FirstOrDefault(ch => ch.Id == characterId);
        }

        public HexGameObject GetEntityAtCoordinate(HexCoordinate coordinate)
        {
            return CurrentGameState.Entities.FirstOrDefault(go => go.Position == coordinate);
        }

        public bool IsHexInMap(HexCoordinate coord)
        {
            return CurrentGameState.ContainsKey(coord);
        }

        /// <summary>
        /// returns null if no effect found.
        /// </summary>
        public TileEffect GetTileEffectAtCoordinate(HexCoordinate coordinate)
        {
            return CurrentGameState.TileEffects.FirstOrDefault(data => data.Position == coordinate);
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
            return !CurrentGameState.Entities.Any(character => character.Position == position && character.BlocksMovement);
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
            var characters = CurrentGameState.Characters.OrderBy(cha => cha.TurnTimer).ToList();
            Character characterToBeat = CurrentGameState.Characters?.First();

            var iterator = characters.GetEnumerator();
            var endOfList = !iterator.MoveNext();

            while (!endOfList)
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
            iterator.Dispose();
            return initiativeList;
        }

        /// <summary>
        /// Get all the tiles within range of a target position along each of our three coordinate system axes.
        /// </summary>
        public List<HexCoordinate> GetAxisTilesInRange(Character objectCharacter, int range)
        {
            var targets = new List<HexCoordinate>();

            foreach (var direction in Directions)
            {
                for (var i = 0; i < range; i++)
                {
                    var hexToCheck = CurrentGameState.ActiveCharacter.Position + (direction * (i + 1));

                    if (!CurrentGameState.ContainsKey(hexToCheck))
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
            foreach (var direction in Directions)
            {
                for (var i = 0; i < range; i++)
                {
                    var hexToCheck = CurrentGameState.ActiveCharacter.Position + (direction * (i + 1));

                    if (!CurrentGameState.ContainsKey(hexToCheck))
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
            foreach (var direction in Directions)
            {
                for (var i = 0; i < range; i++)
                {
                    var hexToCheck = CurrentGameState.ActiveCharacter.Position + (direction * (i + 1));

                    if (!CurrentGameState.ContainsKey(hexToCheck))
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

            GetVisibleTilesRecursive(targets, objectCharacter.Position, CurrentGameState.ActiveCharacter.Position, range);

            return targets;
        }

        /// <summary>
        /// Get all the visible tiles within range of a target position
        /// </summary>
        public List<HexCoordinate> GetVisibleTilesInRangeIgnoreUnits(Character objectCharacter, int range)
        {
            var targets = new List<HexCoordinate>() { objectCharacter.Position };

            GetVisibleTilesRecursive(targets, objectCharacter.Position, CurrentGameState.ActiveCharacter.Position, range, 0, true);

            return targets;
        }

        /// <summary>
        /// Get all the tiles within range of a target position
        /// </summary>
        public List<HexCoordinate> GetTilesInRange(Character objectCharacter, int range)
        {
            var result = new List<HexCoordinate>() { objectCharacter.Position };

            GetTilesInRangeRecursive(result, objectCharacter.Position, range);

            return result;
        }

        public Dictionary<HexCoordinate, int> GetValidDestinations(Character objectCharacter)
        {
            Dictionary<HexCoordinate, int> pathValues = new Dictionary<HexCoordinate, int> { { objectCharacter.Position, 0 } };

            GetWalkableNeighboursRecursive(pathValues, objectCharacter.Position, objectCharacter.MovementType, 0, 0);
            return pathValues;
        }

        public bool IsValidTarget(Character objectCharacter, HexCoordinate targetPosition, int range, GetValidTargetsDelegate targetDelegate)
        {
            return targetDelegate != null && targetDelegate.Invoke(objectCharacter, range, this).Contains(targetPosition);
        }

        /// <summary>
        /// Returns a boolean indicating if the selected tile is reachable from the start position in
        /// a number of steps =< range.
        /// </summary>
        public bool IsValidDestination(Character objectCharacter, HexCoordinate targetPosition)
        {
            var destinations = GetValidDestinations(objectCharacter);
            var reachable = destinations.Keys.Contains(targetPosition);
            var inRange = (this.CurrentGameState.Potential >= destinations[targetPosition]);

            return (reachable && inRange);
        }

        /// <summary>
        /// Get the shortest traverseable path between two points on the map.
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
            if (!CurrentGameState.ContainsKey(start) || !CurrentGameState.ContainsKey(destination))
                return null;

            //data structure map such that key : a tile we've looked at one or more times, value : the previous tile in the shortest path to the key-tile
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
                foreach (var neighbor in GameState.GetNeighbours(current))
                {
                    //tile validation goes here.
                    if (!IsTilePassable(movementType, neighbor))
                        continue;

                    //check if the tile is water or lava. - these tiles have a special rule, they stop your movement.
                    if ((CurrentGameState[neighbor].TerrainType == TerrainType.Water
                        || CurrentGameState[neighbor].TerrainType == TerrainType.Lava)
                        && neighbor != destination)
                        continue;

                    //nodes are always one space away - hexgrid!
                    //BUT hexes have different movement costs to move through!
                    //the path from the start to the tile we're looking at now is the path the
                    var pathLengthToNeighbor = pathValues[current] + GetTileMovementCostModifier(neighbor) + 1;

                    //estimate the neighbor and add it to the list of estimates or update it if it's already in the list
                    if (!pathValues.ContainsKey(neighbor) || pathValues[neighbor] > pathLengthToNeighbor)
                    {
                        pathValues[neighbor] = (int)pathLengthToNeighbor;

                        //heuristic for "distance to destination tile" is just absolute distance between current tile and the destination
                        float estimate = pathLengthToNeighbor + GameState.DistanceBetweenPoints(neighbor, destination);

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

            var character = CurrentGameState.LivingCharacters.FirstOrDefault(c => c.Position == coordinate);
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

        public float GetTileMovementCostModifier(HexCoordinate coordinate)
        {
            if (!CurrentGameState.TileEffects.Any(te => te.Position == coordinate))
                return CurrentGameState[coordinate].MovementCostModifier;

            float modifier = CurrentGameState.TileEffects.Where(te => te.Position == coordinate).Sum(te => te.MovementModifier) +
                             CurrentGameState[coordinate].MovementCostModifier;
            return modifier;
        }

        private void SendMessage(string message)
        {
            MessageEvent?.Invoke(this, new MessageEventArgs(message));
        }

        private void SendMessage(string message, HexGameObject targetCharacter)
        {
            MessageEvent?.Invoke(this, new MessageEventArgs(message, targetCharacter));
        }

        private bool IsInEnemySpawnArea(HexCoordinate coord)
        {
            return coord.Z <= 0 && coord.Z >= -3 && coord.X >= -4 && coord.X <= 4;
        }

        private bool IsHexWalkable(HexCoordinate co)
        {
            return CurrentGameState[co].IsWalkable;
        }

        private bool IsHexOpaque(HexCoordinate coordinate)
        {
            return CurrentGameState[coordinate].BlocksLOS;
        }

        private bool BlocksLineOfSight(HexCoordinate coordinate)
        {
            return IsHexOpaque(coordinate) || !IsTileEmpty(coordinate);
        }

        private void GetTilesInRangeRecursive(List<HexCoordinate> tilesInRange, HexCoordinate position, int range, int searchDepth = 0)
        {
            if (searchDepth >= range)
                return;

            var adjacentTiles = GameState.GetNeighbours(position);
            foreach (var coord in adjacentTiles)
            {
                if (!tilesInRange.Contains(coord))
                {
                    tilesInRange.Add(coord);
                }

                GetTilesInRangeRecursive(tilesInRange, coord, range, searchDepth + 1);
            }
        }

        private void GetWalkableNeighboursRecursive(Dictionary<HexCoordinate, int> output, HexCoordinate position, MovementType movementType, int movementCost, int searchDepth)
        {
            var adjacentTiles = GameState.GetNeighbours(position);
            var tilesToSearch = new List<HexCoordinate>();

            foreach (var coord in adjacentTiles)
            {
                if (!IsTilePassable(movementType, coord)) continue;

                //get movement cost to next tile
                int movementCostModifier = (int)GetTileMovementCostModifier(coord);
                var movementCostToCoord = MovementSpeedNormal[searchDepth] + movementCostModifier;

                //if we don't have enough potential to reach this tile skip it.
                if (movementCostToCoord + movementCost > CurrentGameState.Potential) continue;

                if (!output.ContainsKey(coord) || output[coord] > movementCostToCoord)
                    tilesToSearch.Add(coord);

                if (!IsTileEmpty(coord))
                    continue;

                //if we've never looked at this tile or we found a shorter path to the tile add it to the list.
                if (!output.ContainsKey(coord))
                {
                    output.Add(coord, movementCostToCoord + movementCost);//then add it to the list.
                }
                else if (output[coord] > movementCostToCoord + movementCost)
                {
                    output[coord] = movementCostToCoord + movementCost;//or adjust the cost
                }
            }

            foreach (var coord in tilesToSearch)
            {
                TerrainType terrainType = CurrentGameState[coord].TerrainType;
                if (terrainType == TerrainType.Water
                    || terrainType == TerrainType.Lava)
                    continue;

                //get movement cost to next tile
                int movementCostModifier = (int)GetTileMovementCostModifier(coord);
                var movementCostToCoord = MovementSpeedNormal[searchDepth] + movementCostModifier;

                GetWalkableNeighboursRecursive(output, coord, movementType,
                    movementCost + movementCostToCoord,
                    searchDepth + 1);
            }
        }

        public List<HexCoordinate> GetWalkableAdjacentTiles(HexCoordinate position, MovementType movementType)
        {
            var walkableNeighbours = new List<HexCoordinate>();

            var neighbours = GameState.GetNeighbours(position);

            foreach (var coordinate in neighbours)
            {
                if (!IsTilePassable(movementType, coordinate)) continue;

                walkableNeighbours.Add(coordinate);
            }

            return walkableNeighbours;
        }

        private HexCoordinate GetNearestEmptyNeighbourRecursive(HexCoordinate position, int maxSearchDepth, int searchDepth = 0)
        {
            if (searchDepth >= maxSearchDepth)
                return null;

            var adjacentTiles = GameState.GetNeighbours(position);

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

        private readonly float _sqrt3 = (float)Math.Sqrt(3.0);

        private static readonly float Sqrt3 = (float)Math.Sqrt(3.0);

        //not screenSpace
        private static Vector2 Get2DCoords(HexCoordinate coordinate)
        {
            var posX = (Sqrt3 * coordinate.X + (Sqrt3 / 2 * coordinate.Z));
            var posY = 1.5f * coordinate.Z;

            return new Vector2(posX, posY);
        }

        private void GetVisibleTilesRecursive(List<HexCoordinate> neighbours, HexCoordinate position, HexCoordinate startPosition, int maxSearchDepth, int searchDepth = 0, bool ignoreUnits = false)
        {
            if (searchDepth >= maxSearchDepth)
                return;

            var adjacentTiles = GetNeighbours(position);

            foreach (var coord in adjacentTiles)
            {
                //if the terrain blocks LOS then move on
                if (IsHexOpaque(coord)) continue;

                var distanceToPoint = DistanceBetweenPoints(startPosition, position);
                var distanceToNeighbour = DistanceBetweenPoints(startPosition, coord);

                //only look at neighbouring tiles that're further away from the starting position than the tile we're currently at
                if (distanceToPoint >= distanceToNeighbour)
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
        
        private Character FindCharacterById(Guid characterId)
        {
            return CurrentGameState.Characters.FirstOrDefault(cha => cha.Id == characterId);
        }

        public int GetPathLengthToTile(Character objectCharacter, HexCoordinate destination)
        {
            var path = FindShortestPath(objectCharacter.Position, destination, objectCharacter.MovementType);
            var pathLength = 0;
            switch (objectCharacter.MovementSpeed)
            {
                case MovementSpeed.Slow:
                    pathLength= path.Select((coord, index) => (int)GetTileMovementCostModifier(coord) + GetMoveSpeedCost(MovementSpeed.Slow, index)).Sum();
                    break;
                case MovementSpeed.Normal:
                    pathLength= path.Select((coord, index) => (int)GetTileMovementCostModifier(coord) + GetMoveSpeedCost(MovementSpeed.Normal, index)).Sum();
                    break;
                case MovementSpeed.Fast:
                    pathLength= path.Select((coord, index) => (int)GetTileMovementCostModifier(coord) + GetMoveSpeedCost(MovementSpeed.Fast, index)).Sum();
                    break;
                default:
                    break;
            }
            //no negative path lengths
            return pathLength >= 0 ? pathLength : 0;
        }

        private int GetMoveSpeedCost(MovementSpeed ms, int distance)
        {
            if (distance >= 7)
                return 4;

            switch (ms)
            {
                case MovementSpeed.Slow:
                    return MovementSpeedSlow[distance];
                case MovementSpeed.Normal:
                    return MovementSpeedNormal[distance];
                case MovementSpeed.Fast:
                    return MovementSpeedFast[distance];
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        #endregion

        #endregion

        #region Gameplay Helper Function

        //Get all the neighbours of a tile that're inside the map.
        public static IEnumerable<HexCoordinate> GetNeighbourCoordinates(HexCoordinate position)
        {
            List<HexCoordinate> coordinates = new List<HexCoordinate>();

            //loop through neighbors
            for (int i = 0; i < 6; i++)
            {
                HexCoordinate neighbourCoordinate = position + Directions[i];
                
                var position2D = Get2DCoords(neighbourCoordinate);

                if( Math.Abs((int)position2D.X) <= MapWidth || Math.Abs((int)position2D.Y) <= MapHeight)
                {
                    coordinates.Add(neighbourCoordinate);
                }            
            }

            return coordinates;
        }

        //convert from cube coordinates to 2D "odd-right" coordinate system
        //This is sometimes more intuitive to work with when using a rectangular map.
        public static Vector2 CubeToOddRight(HexCoordinate coord) 
        {
            var col = coord.X + (coord.Z - (coord.Z & 1)) / 2;
            var row = coord.Z;
            return new Vector2(col, row);
        }

        public static List<HexCoordinate> GetNeighbours(HexCoordinate target)
        {
            var neighbours = new List<HexCoordinate>();
            for (int i = 0; i < 6; i++)
            {
                HexCoordinate neighbourCoordinate = target + Directions[i];

                var oddR = CubeToOddRight(neighbourCoordinate);

                //don't look at neighbours outside of the map
                if (Math.Abs((int)oddR.X) > MapWidth || Math.Abs((int)oddR.Y) > MapHeight)
                    continue;

                neighbours.Add(neighbourCoordinate);
            }

            return neighbours;
        }

        public static HexCoordinate GetNearestNeighbor(HexCoordinate start, HexCoordinate end)
        {
            var startCoords = Get2DCoords(start);
            int distance = 100;
            HexCoordinate nearest = null;

            for (int i = 0; i < 6; i++)
            {
                HexCoordinate neighbourCoordinate = end + Directions[i];

                var oddR = CubeToOddRight(neighbourCoordinate);

                //don't look at neighbours outside of the map
                if (Math.Abs((int)oddR.X) > MapWidth || Math.Abs((int)oddR.Y) > MapHeight)
                    continue;

                var delta = DistanceBetweenPoints(start, neighbourCoordinate);
                if (delta < distance) //if this is naively closer then we have a new nearest tile
                {
                    nearest = neighbourCoordinate;
                    distance = delta;
                }
                //if they're tied then convert to 2d coords and use that as the tiebeaker
                //I have no idea why this works but it ends up looking good intuitively
                else if (delta == distance && nearest != null)
                {
                    var neighborCoords = Get2DCoords(neighbourCoordinate);
                    var nearestCoords = Get2DCoords(nearest);

                    var nearestDelta = nearestCoords - startCoords;
                    var neighborDelta = neighborCoords - startCoords;

                    if (neighborDelta.Length() < nearestDelta.Length())
                    {
                        nearest = neighbourCoordinate;
                        distance = delta;
                    }
                }
            }

            return nearest;
        }


        public static HexCoordinate GetPushDirection(HexCoordinate pushOrigin, HexCoordinate targetPosition)
        {            
            //determine direction of push
            var nearestNeighbor = GetNearestNeighbor(pushOrigin, targetPosition);
            var direction = targetPosition - nearestNeighbor;

            return direction;
        }

        public HexCoordinate GetRandomCoordinateInMap()
        {
            Random rand = new Random();

            var rowIndex = rand.Next(-MapHeight, MapHeight);
            var columnIndex = rand.Next(-MapWidth, MapWidth);

            var x = columnIndex - (rowIndex - (rowIndex & 1)) / 2;
            var z = rowIndex;
            var y = -(x + z);

            if (x + y + z != 0)
                throw new Exception(" Impossible co-ordinate");

            return new HexCoordinate(x, y, z);
        }

        public static int DistanceBetweenPoints(HexCoordinate a, HexCoordinate b)
        {
            return (Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y) + Math.Abs(a.Z - b.Z)) / 2;
        }

        #endregion

        #endregion

        #region Movement Attibutes

        public static readonly int[] MovementSpeedSlow = { 0, 1, 2, 3, 3, 3, 3 };
        public static readonly int[] MovementSpeedNormal = { 0, 1, 1, 2, 3, 3, 3 };
        public static readonly int[] MovementSpeedFast = { 0, 0, 1, 2, 2, 3, 3 };

        #endregion
    }
}