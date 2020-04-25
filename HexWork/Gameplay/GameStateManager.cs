using System;
using System.Collections.Generic;
using System.Linq;
using HexWork.Gameplay.Actions;
using HexWork.Gameplay.Characters;
using HexWork.Gameplay.Interfaces;
using HexWork.GameplayEvents;
using Microsoft.Xna.Framework;

namespace HexWork.Gameplay
{
    public class GameStateManager : IGameStateObject
    {
        #region Attributes

        public GameState CurrentGameState { get; set; }
        public List<TileEffect> TileEffects => CurrentGameState.TileEffects;

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

        #region Methods

        #region Initialisation 
        
        public GameStateManager()
        {
            CurrentGameState = new GameState();
        }

        public void CreateCharacters(int difficulty = 1)
        {
            var characterFactory = new CharacterFactory();
            CurrentGameState.Characters.AddRange(characterFactory.CreateHeroes());
            CurrentGameState.Characters.AddRange(characterFactory.CreateEnemies(difficulty));
        }

        #endregion

        #region Game Start
        
        public void StartGame()
        {
            var characters = CurrentGameState.Characters;

            //spawn enemies
            foreach (var character in CurrentGameState.Characters.Where(c => !c.IsHero))
            {
                var coordinate = CurrentGameState.GetRandomCoordinateInMap();

                //one unit per tile and only deploy to walkable spaces.
                while (characters.Select(cha => cha.Position).Contains(coordinate) || !CurrentGameState[coordinate].IsWalkable || !IsInEnemySpawnArea(coordinate))
                {
                    coordinate = CurrentGameState.GetRandomCoordinateInMap();
                }

                character.SpawnAt(coordinate);
                SpawnCharacterEvent?.Invoke(this,
                    new SpawnChracterEventArgs()
                    {
                        MonsterType = character.MonsterType,
                        Character = character
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
                                CurrentGameState.GetNeighborCoordinates(position).Any(IsHexPassable);
                }

                character.SpawnAt(position);
                SpawnCharacterEvent?.Invoke(this, new SpawnChracterEventArgs() { Character = character });
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
            if (activeCharacter.HasActed) return;

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
                GameOver();
            }
        }

        public void GameOver()
        {

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

        public void MoveCharacterTo(Character character, HexCoordinate position)
        {
            var path = FindShortestPath(character.Position, position, character.MovementType);

            foreach (var coordinate in
                path)
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
            var tileEffect = CurrentGameState.TileEffects.FirstOrDefault(data => data.Position == position);

            if (tileEffect == null)
                return;

            tileEffect.TriggerEffect(this, character);
            CurrentGameState.TileEffects.Remove(tileEffect);
            RemoveTileEffectEvent?.Invoke(this, new RemoveTileEffectEventArgs() { Id = tileEffect.Guid });
        }

        //when a character moves into a tile check to see if there're any terrain effects for moving into that tile.
        private void ResolveTerrainEffects(Character character, HexCoordinate destination)
        {
            //don't count terrain effects from a tile you're standing. We don't punish players for leaving lava.
            ResolveTerrainEnterEffect(character, CurrentGameState[destination]);
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

            if (!string.IsNullOrWhiteSpace(message))
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
            effectToApply.Reset();
            targetCharacter.StatusEffects.Add(effectToApply);

            StatusAppliedEvent?.Invoke(this, new StatusEventArgs(targetCharacter.Id, effectToApply));
        }

        /// <summary>
        /// Apply a combo effect to a target character if they're currently suffering a status effect.
        /// todo - Remove the current status effect from the character.
        /// </summary>
        /// <param name="targetCharacter"></param>
        /// <param name="combo"></param>
        public int ApplyCombo(Character targetCharacter, DamageComboAction combo)
        {
            if (!targetCharacter.HasStatus)
                return 0;

            ComboEvent?.Invoke(this, new ComboEventArgs(targetCharacter.Id, combo));

            //if the player scores a combo they gain potential. if their commander gets comboed they lose potential (uh-oh!)
            if (CurrentGameState.ActiveCharacter.IsHero)
                GainPotential(2);
            
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
                if (!CurrentGameState.ContainsKey(destinationPos))
                {
                    ApplyDamage(targetCharacter, distance * 15, "IMPACT");
                    distance = 0;
                }
                else if (IsHexPassable(destinationPos))
                {
                    MoveCharacterTo(targetCharacter, destinationPos);

                    var tile = CurrentGameState[destinationPos];

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
            if (!IsHexWalkable(location) || !IsTileEmpty(location))
                return;

            var tileEffect = new TileEffect(effect, location);
            CurrentGameState.TileEffects.Add(tileEffect);

            SpawnTileEffectEvent?.Invoke(this, new SpawnTileEffectEventArgs()
            {
                Id = tileEffect.Guid,
                Position = location,
                Type = effect.Type
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

        public Character GetCharacterAtCoordinate(HexCoordinate coordinate)
        {
            return CurrentGameState.LivingCharacters.FirstOrDefault(character => character.Position == coordinate);
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
            return !CurrentGameState.LivingCharacters.Any(character => character.Position == position);
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
            CurrentGameState.Characters = CurrentGameState.Characters.OrderBy(cha => cha.TurnTimer).ToList();
            Character characterToBeat = CurrentGameState.Characters?.First();

            var iterator = CurrentGameState.Characters.GetEnumerator();
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

            foreach (var direction in HexGrid.Directions)
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
            foreach (var direction in HexGrid.Directions)
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
            foreach (var direction in HexGrid.Directions)
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

        public List<HexCoordinate> GetValidDestinations(Character objectCharacter)
        {
            Dictionary<HexCoordinate, int> pathValues = new Dictionary<HexCoordinate, int> { { objectCharacter.Position, 0 } };

            GetWalkableNeighboursRecursive(pathValues, objectCharacter.Position, objectCharacter.MovementType, 0, 0);
            return pathValues.Keys.ToList();
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
            var reachable = GetValidDestinations(objectCharacter).Contains(targetPosition);

            var inRange = (this.CurrentGameState.Potential >= GetPathLengthToTile(objectCharacter, targetPosition));

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

            //data structure map such that key : a tile we've looked at one or more times, value : the previous tile in the shortest path to the key.
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
                foreach (var neighbor in CurrentGameState.GetNeighborCoordinates(current))
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
                    var pathLengthToNeighbor = pathValues[current] + (int)GetTileMovementCostModifier(neighbor);

                    //estimate the neighbor and add it to the list of estimates or update it if it's already in the list
                    if (!pathValues.ContainsKey(neighbor) || pathValues[neighbor] > pathLengthToNeighbor)
                    {
                        //deliberate truncation in cast.
                        pathValues[neighbor] = (int)pathLengthToNeighbor;

                        //heuristic for "distance to destination tile" is just absolute distance between current tile and the destination
                        float estimate = pathLengthToNeighbor + HexGrid.DistanceBetweenPoints(neighbor, destination);

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

            return CurrentGameState.TileEffects.Where(te => te.Position == coordinate).Sum(te => te.MovementModifier) +
                   CurrentGameState[coordinate].MovementCostModifier;
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

            var adjacentTiles = CurrentGameState.GetNeighborCoordinates(position);
            foreach (var coord in adjacentTiles)
            {
                if (!tilesInRange.Contains(coord))
                {
                    tilesInRange.Add(coord);
                }

                GetTilesInRangeRecursive(tilesInRange, coord, range, searchDepth + 1);
            }
        }

        private void GetWalkableNeighboursRecursive(Dictionary<HexCoordinate, int> neighbours, HexCoordinate position, MovementType movementType, int movementCost, int searchDepth)
        {
            var adjacentTiles = CurrentGameState.GetNeighborCoordinates(position);
            var tilesToSearch = new List<HexCoordinate>();

            foreach (var coord in adjacentTiles)
            {
                if (!IsTilePassable(movementType, coord)) continue;

                //get movement cost to next tile
                int movementCostModifier = (int)GetTileMovementCostModifier(coord);
                var movementCostToCoord = MovementSpeedNormal[searchDepth] + movementCostModifier;

                //if we don't have enough movement to reach this tile skip it.
                if (movementCostToCoord + movementCost > CurrentGameState.Potential) continue;

                if (!neighbours.ContainsKey(coord) || neighbours[coord] > searchDepth + movementCost)
                    tilesToSearch.Add(coord);

                if (!IsTileEmpty(coord))
                    continue;

                //if we've never looked at this tile or we found a shorter path to the tile add it to the list.
                if (!neighbours.ContainsKey(coord))
                {
                    neighbours.Add(coord, movementCostToCoord + movementCost);//then add it to the list.
                }
                else if (neighbours[coord] > movementCostToCoord + movementCost)
                {
                    neighbours[coord] = movementCostToCoord + movementCost;//or adjust the cost
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

                GetWalkableNeighboursRecursive(neighbours, coord, movementType,
                    movementCost + movementCostToCoord,
                    searchDepth + 1);
            }
        }

        public List<HexCoordinate> GetWalkableAdjacentTiles(HexCoordinate position, MovementType movementType)
        {
            var walkableNeighbours = new List<HexCoordinate>();

            var neighbours = CurrentGameState.GetNeighborCoordinates(position);

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

            var adjacentTiles = CurrentGameState.GetNeighborCoordinates(position);

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
            var neighbours = CurrentGameState.GetNeighborCoordinates(end);

            int distance = 100;
            HexCoordinate nearest = null;

            foreach (var neighbor in neighbours)
            {
                var delta = HexGrid.DistanceBetweenPoints(start, neighbor);
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

            var adjacentTiles = CurrentGameState.GetNeighborCoordinates(position);

            foreach (var coord in adjacentTiles)
            {
                //if the terrain blocks LOS then move on
                if (IsHexOpaque(coord)) continue;

                var distanceToPoint = HexGrid.DistanceBetweenPoints(startPosition, position);
                var distanceToNeighbour = HexGrid.DistanceBetweenPoints(startPosition, coord);

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

            switch (objectCharacter.MovementSpeed)
            {
                case MovementSpeed.Slow:
                    return path.Select((coord, index) => (int)GetTileAtCoordinate(coord).MovementCostModifier + GetMoveSpeedCost(MovementSpeed.Slow, index)).Sum();
                case MovementSpeed.Normal:
                    return path.Select((coord, index) => (int)GetTileAtCoordinate(coord).MovementCostModifier + GetMoveSpeedCost(MovementSpeed.Normal, index)).Sum();
                case MovementSpeed.Fast:
                    return path.Select((coord, index) => (int)GetTileAtCoordinate(coord).MovementCostModifier + GetMoveSpeedCost(MovementSpeed.Fast, index)).Sum();
                default:
                    throw new ArgumentOutOfRangeException();
            }
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

        #endregion

        #region Movement Attibutes

        public static readonly int[] MovementSpeedSlow = { 0, 1, 2, 3, 3, 3, 3 };
        public static readonly int[] MovementSpeedNormal = { 0, 1, 1, 2, 3, 3, 3 };
        public static readonly int[] MovementSpeedFast = { 0, 0, 1, 2, 2, 3, 3 };

        #endregion
    }
}