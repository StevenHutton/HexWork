using System;
using System.Collections.Generic;
using System.Linq;
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

        public const int ImpactDamage = 25;

        public const int CollisionDamage = 15;

        public const int MapWidth = 6;
        public const int MapHeight = 5;

        public BoardState BoardState { get; set; }

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
            BoardState = new BoardState(MapWidth, MapHeight);
        }

        public void CreateCharacters(int difficulty = 1)
        {
            BoardState.Entities.AddRange(CharacterFactory.CreateHeroes());
            BoardState.Entities.AddRange(CharacterFactory.CreateEnemies(difficulty));
        }

        #endregion

        #region Game Start
        
        public void StartGame()
        {
            var characters = BoardState.Characters;

            //spawn enemies
            foreach (var character in BoardState.Characters.Where(c => !c.IsHero))
            {
                var coordinate = GetRandomCoordinateInMap();

                //one unit per tile and only deploy to walkable spaces.
                while (characters.Select(cha => cha.Position).Contains(coordinate) || !BoardState[coordinate].IsWalkable || !IsInEnemySpawnArea(coordinate))
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
            foreach (var character in BoardState.Heroes)
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

            NextTurn(GetCharacterAtInitiative(0));            
        }

        #endregion

        #region Metegame

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

        #endregion

        #region Public Update Methods

        public void Update()
        {
            if (BoardState.ActiveCharacter == null)
                return;

            if (!BoardState.ActiveCharacter.IsAlive)
            {
                NextTurn(BoardState.ActiveCharacter);
                return;
            }
            
            if (BoardState.ActiveCharacter.IsHero)
                return;

            //if they can't act, end turn
            if (!BoardState.ActiveCharacter.CanAttack)
            {
                NextTurn(BoardState.ActiveCharacter);
                return;
            }

            BoardState.ActiveCharacter.DoTurn(this, BoardState.ActiveCharacter);
            BoardState.ActiveCharacter.CanAttack = false;                      
        }

        public void NextTurn(Character activeCharacter)
        {
            //if we have an active character then update all the initiative values
            if (activeCharacter != null)
            {
                ResolveTileEffects(activeCharacter, activeCharacter.Position);
                ResolveTerrainEffects(activeCharacter, activeCharacter.Position);
                activeCharacter.EndTurn();

                var deltaTime = activeCharacter.TurnTimer;
                foreach (var character in BoardState.Characters)
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
            BoardState.ActiveCharacter = GetCharacterAtInitiative(0);
            activeCharacter = BoardState.ActiveCharacter;

            if (activeCharacter.CharacterType == CharacterType.Majin)
                GainPotential(2);

            activeCharacter.StartTurn();

            //apply any status effects for the new active character that trigger at the start of thier turn.
            foreach (var statusEffect in activeCharacter.StatusEffects)
            {
                statusEffect.StartTurn(activeCharacter, this);
            }

            //apply any status effects for characters that apply whenever initiative moves.
            foreach (var character in BoardState.Characters)
            {
                foreach (var statusEffect in character.StatusEffects)
                {
                    statusEffect.Tick(character, this);
                }
            }

            EndTurnEvent?.Invoke(this, new EndTurnEventArgs(GetInitiativeList().ToList()));
        }

        #region Gamestate Transforms

        public static BoardState AddEntity(BoardState state, HexGameObject entity)
        {
            var newState = state.Copy();
            newState.Entities.Add(entity);
            return newState;
        }

        public void SpawnCharacter(Character character)
        {
            BoardState.Entities.Add(character);
            SpawnEntityEvent?.Invoke(this,
                new EntityEventArgs()
                {
                    Entity = character
                });
            TeleportEntityTo(character, character.Position);
        }

        public void MoveEntity(HexGameObject entity, List<HexCoordinate> path)
        {
            foreach (var coordinate in path)
            {
                entity.MoveTo(coordinate);
                CharacterMoveEvent?.Invoke(this, new MoveEventArgs
                {
                    CharacterId = entity.Id,
                    Destination = coordinate
                });

                ResolveTileEffects(entity, coordinate);
                ResolveTerrainEffects(entity, coordinate);
            }
        }

        public void TeleportEntityTo(HexGameObject gameObject, HexCoordinate position)
        {
            ResolveTileEffects(gameObject, position);
            ResolveTerrainEffects(gameObject, position);

            gameObject.MoveTo(position);
            CharacterTeleportEvent?.Invoke(this,
                new MoveEventArgs
                {
                    CharacterId = gameObject.Id,
                    Destination = position
                });
        }

        public void ResolveTileEffects(HexGameObject entity, HexCoordinate position)
        {
            var tileEffect = BoardState.TileEffects.FirstOrDefault(data => data.Position == position);

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
            if (!BoardState.Entities.Contains(effect)) return;

            BoardState.Entities.Remove(effect);
            RemoveEntityEvent?.Invoke(this, new EntityEventArgs() { Entity = effect });
        }

        //when a character moves into a tile check to see if there're any terrain effects for moving into that tile.
        private void ResolveTerrainEffects(HexGameObject entity, HexCoordinate destination)
        {
            //don't count terrain effects from a tile you're standing. We don't punish players for leaving lava.
            ResolveTerrainEnterEffect(entity, BoardState[destination]);
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
            if (entity is Character character)
            {
                character.CanAttack = false;
            }

            ActionEvent?.Invoke(this,
              new ActionEventArgs { Action = action });
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
            if (character.Health <= 0 && BoardState.Entities.Contains(character))
            {
                BoardState.Entities.Remove(character);
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
            if (BoardState.ActiveCharacter.IsHero)
                GainPotential(2);
            
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
            //don't push dead characters
            if (!BoardState.Entities.Contains(targetEntity))
                return;

            var targetCharacterPos = targetEntity.Position;
            var destinationPos = targetCharacterPos + direction;
            while (distance > 0)
            {
                if (!BoardState.ContainsKey(destinationPos))
                {
                    ApplyDamage(targetEntity, distance * 15, "IMPACT");
                    distance = 0;
                }
                else if (IsHexPassable(destinationPos))
                {
                    MoveEntity(targetEntity, new List<HexCoordinate> { destinationPos });

                    var tile = BoardState[destinationPos];

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
            if (BoardState.Potential + potential <= BoardState.MaxPotential)
            {
                BoardState.Potential += potential;
            }

            PotentialChangeEvent?.Invoke(this, new PotentialEventArgs(potential));
        }

        public void LosePotential(int potential = 1)
        {
            if (BoardState.Potential >= potential)
            {
                BoardState.Potential -= potential;
            }

            PotentialChangeEvent?.Invoke(this, new PotentialEventArgs(-potential));
        }

        public void CreateTileEffect(HexCoordinate location, TileEffect effect)
        {
            if(!IsHexInMap(location))
                return;

            //don't create a tile effect on unpassable tiles, occupied tiles or tiles that already have effects
            if (!IsHexWalkable(location) || !IsTileEmpty(location) || BoardState.TileEffects.Any(te => te.Position == location))
                return;

            var tileEffect = new TileEffect(effect);
            tileEffect.Position = location;
            BoardState.Entities.Add(tileEffect);

            SpawnEntityEvent?.Invoke(this, new EntityEventArgs
            {
                Entity = tileEffect
            });
        }

        #endregion


        #region Public Accessor Methods

        public HexGameObject GetEntityAtCoordinate(HexCoordinate coordinate)
        {
            return BoardState.Entities.FirstOrDefault(go => go.Position == coordinate);
        }

        public bool IsHexInMap(HexCoordinate coord)
        {
            return BoardState.ContainsKey(coord);
        }

        /// <summary>
        /// returns null if no effect found.
        /// </summary>
        public TileEffect GetTileEffectAtCoordinate(HexCoordinate coordinate)
        {
            return BoardState.TileEffects.FirstOrDefault(data => data.Position == coordinate);
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
            return !BoardState.Entities.Any(ent => ent.Position == position && ent.BlocksMovement);
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
            var characters = BoardState.Characters.OrderBy(cha => cha.TurnTimer).ToList();
            Character characterToBeat = BoardState.Characters?.First();

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
        public List<HexCoordinate> GetAxisTilesInRange(BoardState board, HexCoordinate position, int range)
        {
            var targets = new List<HexCoordinate>();

            foreach (var direction in Directions)
            {
                for (var i = 0; i < range; i++)
                {
                    var hexToCheck = position + (direction * (i + 1));

                    if (!board.ContainsKey(hexToCheck))
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
                    var hexToCheck = objectCharacter.Position + (direction * (i + 1));

                    if (!BoardState.ContainsKey(hexToCheck))
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
                    var hexToCheck = objectCharacter.Position + (direction * (i + 1));

                    if (!BoardState.ContainsKey(hexToCheck))
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

            GetVisibleTilesRecursive(targets, objectCharacter.Position, BoardState.ActiveCharacter.Position, range);

            return targets;
        }

        /// <summary>
        /// Get all the visible tiles within range of a target position
        /// </summary>
        public List<HexCoordinate> GetVisibleTilesInRangeIgnoreUnits(Character objectCharacter, int range)
        {
            var targets = new List<HexCoordinate>() { objectCharacter.Position };

            GetVisibleTilesRecursive(targets, objectCharacter.Position, BoardState.ActiveCharacter.Position, range, 0, true);

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

        public bool IsValidTarget(Character objectCharacter, HexCoordinate targetPosition, int range, GetValidTargetsDelegate targetDelegate)
        {
            return targetDelegate != null && targetDelegate.Invoke(objectCharacter, range, this).Contains(targetPosition);
        }
                
        #endregion

        #region Private Helper Methods

        private bool IsTilePassable(MovementType movementType, HexCoordinate coordinate)
        {
            //tile validation goes here.
            if (!IsHexWalkable(coordinate)) return false;

            var character = BoardState.LivingCharacters.FirstOrDefault(c => c.Position == coordinate);
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

        public float GetTileTotalMovementCost(HexCoordinate coordinate)
        {
            return BoardState[coordinate].MovementCost + GetMovementCostModifier(coordinate);
        }

        public float GetTileMovementCost(HexCoordinate coord)
        {
            return BoardState[coord].MovementCost;
        }

        public float GetMovementCostModifier(HexCoordinate coord)
        {
            if (BoardState.TileEffects.Any(te => te.Position == coord))
                return BoardState.TileEffects.Where(te => te.Position == coord).Sum(te => te.MovementModifier);
            else return 0.0f;

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
            return BoardState[co].IsWalkable;
        }

        private bool IsHexOpaque(HexCoordinate coordinate)
        {
            return BoardState[coordinate].BlocksLOS;
        }

        /// <summary>
        /// Returns a boolean indicating if the selected tile is reachable from the start position in
        /// a number of steps =< range.
        /// </summary>
        public bool IsValidDestination(Character objectCharacter, HexCoordinate targetPosition)
        {
            var destinations = GetValidDestinations(objectCharacter);

            if (!destinations.Keys.Contains(targetPosition))
                return false;

            if (BoardState.Potential < destinations[targetPosition])
                return false;

            return true;
        }

        private void GetTilesInRangeRecursive(List<HexCoordinate> tilesInRange, HexCoordinate position, int range, int searchDepth = 0)
        {
            if (searchDepth >= range)
                return;

            var adjacentTiles = GetNeighbours(position);
            foreach (var coord in adjacentTiles)
            {
                if (!tilesInRange.Contains(coord))
                {
                    tilesInRange.Add(coord);
                }

                GetTilesInRangeRecursive(tilesInRange, coord, range, searchDepth + 1);
            }
        }

        public Dictionary<HexCoordinate, int> GetValidDestinations(Character objectCharacter)
        {
            Dictionary<HexCoordinate, int> pathValues = new Dictionary<HexCoordinate, int> { { objectCharacter.Position, 0 } };

            GetWalkableNeighboursRecursive(pathValues, objectCharacter.Position, objectCharacter.MovementType, objectCharacter.MovementSpeed, 0, 0, BoardState.Potential);
            return pathValues;
        }

        /// <summary></summary>
        /// <param name="output"></param>
        /// <param name="position"></param>
        /// <param name="movementType"></param>
        /// <param name="movementCost"></param>
        /// <param name="searchDepth"></param>
        private void GetWalkableNeighboursRecursive(Dictionary<HexCoordinate, int> pathLengthsToTiles, HexCoordinate position, MovementType movementType, MovementSpeed movementSpeed,
            int movementCost, int searchDepth, int availableMovement = 5)
        {
            var adjacentTiles = GetNeighbours(position);
            var tilesToSearch = new List<HexCoordinate>();

            //loop through all neighbour tiles.
            foreach (var coord in adjacentTiles)
            {
                if (!IsTilePassable(movementType, coord)) continue;

                //get movement cost to next tile
                int movementCostModifier = (int)GetTileTotalMovementCost(coord);
                var movementCostToCoord = GetMoveSpeedCost(movementSpeed, searchDepth) + movementCostModifier;

                //if we don't have enough potential to reach this tile skip it.
                if (movementCostToCoord + movementCost > availableMovement) continue;

                if (!pathLengthsToTiles.ContainsKey(coord) || pathLengthsToTiles[coord] > movementCostToCoord)
                    tilesToSearch.Add(coord);

                //valid destination check
                if (!IsTileEmpty(coord))
                    continue;

                //if we've never looked at this tile or we found a shorter path to the tile add it to the list.
                if (!pathLengthsToTiles.ContainsKey(coord))
                {
                    pathLengthsToTiles.Add(coord, movementCostToCoord + movementCost);//then add it to the list.
                }
                else if (pathLengthsToTiles[coord] > movementCostToCoord + movementCost)
                {
                    pathLengthsToTiles[coord] = movementCostToCoord + movementCost;//or adjust the cost
                }
            }

            foreach (var coord in tilesToSearch)
            {
                TerrainType terrainType = BoardState[coord].TerrainType;
                if (terrainType == TerrainType.Water
                    || terrainType == TerrainType.Lava)
                    continue;

                //get movement cost to next tile
                int movementCostModifier = (int)GetTileTotalMovementCost(coord);
                var movementCostToCoord = GetMoveSpeedCost(movementSpeed, searchDepth) + movementCostModifier;

                GetWalkableNeighboursRecursive(pathLengthsToTiles, coord, movementType, movementSpeed,
                    movementCost + movementCostToCoord,
                    searchDepth + 1, availableMovement);
            }
        }

        public List<HexCoordinate> FindShortestPath(HexCoordinate startPosition, HexCoordinate destination, int availableMovement,
            MovementType movementType = MovementType.NormalMove, MovementSpeed speed = MovementSpeed.Normal)
        {
            if (startPosition == destination) return null;

            //this is the map of hex coordiantes to the shortest path length to that coordinate
            Dictionary<HexCoordinate, float> pathValues = new Dictionary<HexCoordinate, float>();

            Dictionary<HexCoordinate, List<HexCoordinate>> ancestorPathmap = new Dictionary<HexCoordinate, List<HexCoordinate>>();
            List<HexCoordinate> path = null;

            //get cost to move to this neighbour tile from the current search tile
            FindShortestPathRecursive(ancestorPathmap, pathValues, startPosition, destination, movementType, speed, 0, 0, availableMovement);

            if (ancestorPathmap.ContainsKey(destination))
            {
                path = ancestorPathmap[destination];
                path.Add(destination);
                path.RemoveAt(0);
            }

            return path;
        }

        private void FindShortestPathRecursive(Dictionary<HexCoordinate, List<HexCoordinate>> ancestorPathmap,
            Dictionary<HexCoordinate, float> moveCostToSearchedTiles,
            HexCoordinate currentSearchCoord,
            HexCoordinate destination,
            MovementType movementType,
            MovementSpeed speed,
            int movementCostPrevious,
            int searchDepth,
            int availableMovement)
        {
            var adjacentTiles = GetNeighbours(currentSearchCoord);
            var tilesToSearch = new List<HexCoordinate>();

            foreach (var coord in adjacentTiles)
            {
                if (!IsTilePassable(movementType, coord)) continue;

                List<HexCoordinate> shortestPathToCurrentSearchTile;

                //get cost to move to this neighbour tile from the current search tile
                float movementCostToTile = GetTileMovementCost(coord) + GetMoveSpeedCost(speed, searchDepth);

                if (ancestorPathmap.ContainsKey(currentSearchCoord))
                {
                    //a list of ancestor tiles to walk to reach the current coord
                    shortestPathToCurrentSearchTile = new List<HexCoordinate>(ancestorPathmap[currentSearchCoord]);
                    shortestPathToCurrentSearchTile.Add(currentSearchCoord);

                    //if this is the first time we've visited this tile on this path then apply the tile effects (we only apply them once)
                    if (!shortestPathToCurrentSearchTile.Contains(coord))
                        movementCostToTile += GetMovementCostModifier(coord);
                }
                else
                    shortestPathToCurrentSearchTile = new List<HexCoordinate> { currentSearchCoord };

                var totalMovementCostToTile = (int)(movementCostToTile + movementCostPrevious);

                if (totalMovementCostToTile > availableMovement)
                    continue;

                //if we've never been to this tile or if we found a shorter path to this tile then add it to the list.
                if (!moveCostToSearchedTiles.ContainsKey(coord))
                {
                    moveCostToSearchedTiles.Add(coord, totalMovementCostToTile);//add it to the list.
                    
                    if(!ancestorPathmap.ContainsKey(coord))
                        ancestorPathmap.Add(coord, shortestPathToCurrentSearchTile);
                    else
                        ancestorPathmap[coord] = shortestPathToCurrentSearchTile;

                    tilesToSearch.Add(coord);
                }
                else if (moveCostToSearchedTiles[coord] > totalMovementCostToTile)
                {
                    moveCostToSearchedTiles[coord] = totalMovementCostToTile;//or adjust the cost

                    if (!ancestorPathmap.ContainsKey(coord))
                        ancestorPathmap.Add(coord, shortestPathToCurrentSearchTile);
                    else
                        ancestorPathmap[coord] = shortestPathToCurrentSearchTile;

                    tilesToSearch.Add(coord);
                }
                else if(moveCostToSearchedTiles[coord] == totalMovementCostToTile && ancestorPathmap[coord].Count > shortestPathToCurrentSearchTile.Count)
                {
                    moveCostToSearchedTiles[coord] = totalMovementCostToTile;//or adjust the cost

                    if (!ancestorPathmap.ContainsKey(coord))
                        ancestorPathmap.Add(coord, shortestPathToCurrentSearchTile);
                    else
                        ancestorPathmap[coord] = shortestPathToCurrentSearchTile;

                    tilesToSearch.Add(coord);
                }

                //check here if we've found the destination and early return?  
                if (coord == destination)
                    return;
            }

            foreach (var searchCandidate in tilesToSearch)
            {
                TerrainType terrainType = BoardState[searchCandidate].TerrainType;
                if (terrainType == TerrainType.Water
                    || terrainType == TerrainType.Lava)
                    continue;

                //get movement cost to next tile
                int movementCost = (int)GetTileTotalMovementCost(searchCandidate);
                var movementCostToCoord = GetMoveSpeedCost(speed, searchDepth) + movementCost;

                FindShortestPathRecursive(ancestorPathmap, 
                    moveCostToSearchedTiles, 
                    searchCandidate, 
                    destination, 
                    movementType,
                    speed,
                    movementCostToCoord + movementCostPrevious, 
                    searchDepth + 1, 
                    availableMovement);
            }
        }

        public List<HexCoordinate> GetWalkableAdjacentTiles(HexCoordinate position, MovementType movementType)
        {
            var walkableNeighbours = new List<HexCoordinate>();

            var neighbours = GetNeighbours(position);

            foreach (var coordinate in neighbours)
            {
                if (!IsTileEmpty(coordinate)) continue;

                walkableNeighbours.Add(coordinate);
            }

            return walkableNeighbours;
        }

        private HexCoordinate GetNearestEmptyNeighbourRecursive(HexCoordinate position, int maxSearchDepth, int searchDepth = 0)
        {
            if (searchDepth >= maxSearchDepth)
                return null;

            var adjacentTiles = GetNeighbours(position);

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
            return BoardState.Characters.FirstOrDefault(cha => cha.Id == characterId);
        }

        public int GetPathLengthToTile(Character objectCharacter, HexCoordinate destination, List<HexCoordinate> path)
        {
            if(path == null)
                path = FindShortestPath(objectCharacter.Position, destination, 200, objectCharacter.MovementType, objectCharacter.MovementSpeed);
            if (path == null) return 0;

            var pathLength = 0;
            switch (objectCharacter.MovementSpeed)
            {
                case MovementSpeed.Slow:
                    pathLength= path.Select((coord, index) => (int)GetTileTotalMovementCost(coord) + GetMoveSpeedCost(MovementSpeed.Slow, index)).Sum();
                    break;
                case MovementSpeed.Normal:
                    pathLength= path.Select((coord, index) => (int)GetTileTotalMovementCost(coord) + GetMoveSpeedCost(MovementSpeed.Normal, index)).Sum();
                    break;
                case MovementSpeed.Fast:
                    pathLength= path.Select((coord, index) => (int)GetTileTotalMovementCost(coord) + GetMoveSpeedCost(MovementSpeed.Fast, index)).Sum();
                    break;
                default:
                    break;
            }
            //no negative path lengths
            return pathLength >= 0 ? pathLength : 0;
        }

        private static int GetMoveSpeedCost(MovementSpeed ms, int distance)
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

        public static readonly int[] MovementSpeedSlow = { 0, 1, 2, 2, 3, 3, 3 };
        public static readonly int[] MovementSpeedNormal = { 0, 1, 1, 2, 3, 3, 3 };
        public static readonly int[] MovementSpeedFast = { 0, 0, 1, 2, 2, 3, 3 };

        #endregion
    }
}