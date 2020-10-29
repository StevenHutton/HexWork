using System;
using System.Collections.Generic;
using System.Linq;
using HexWork.Gameplay.Actions;
using HexWork.Gameplay.GameObject;
using HexWork.Gameplay.GameObject.Characters;
using HexWork.Gameplay.Interfaces;
using HexWork.GameplayEvents;

namespace HexWork.Gameplay
{
    public class RulesProvider : IRulesProvider
    {
        #region Attributes

        public const int ImpactDamage = 25;

        public const int CollisionDamage = 15;

        public BoardState BoardState { get; set; }

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
        
        public RulesProvider()
        {
            BoardState = new BoardState();
            BoardState.GenerateMap();
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
                var coordinate = BoardState.GetRandomCoordinateInMap();

                //one unit per tile and only deploy to walkable spaces.
                while (characters.Select(cha => cha.Position).Contains(coordinate) || !BoardState[coordinate].IsWalkable || !IsInEnemySpawnArea(coordinate))
                {
                    coordinate = BoardState.GetRandomCoordinateInMap();
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
                var validTile = BoardState.IsHexPassable(BoardState, position);

                while (!validTile)
                {
                    position = BoardState.GetNearestEmptyNeighbourRecursive(BoardState, position, 9);
                    //tiles are only valid so long as they're walkable and have at least one passable neighbor.
                    validTile = BoardState.IsHexPassable(BoardState, position) &&
                                BoardState.GetNeighbours(position).Any(d => BoardState.IsHexPassable(BoardState, d));
                }

                character.SpawnAt(position);
                SpawnEntityEvent?.Invoke(this,
                    new EntityEventArgs()
                    {
                        Entity = character
                    });
            }

            NextTurn(BoardState, BoardState.GetCharacterAtInitiative(BoardState, 0));            
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
                NextTurn(BoardState, BoardState.ActiveCharacter);
                return;
            }
            
            if (BoardState.ActiveCharacter.IsHero)
                return;

            //if they can't act, end turn
            if (!BoardState.ActiveCharacter.CanAttack)
            {
                NextTurn(BoardState, BoardState.ActiveCharacter);
                return;
            }

            BoardState.ActiveCharacter.DoTurn(this, BoardState.ActiveCharacter);
            BoardState.ActiveCharacter.CanAttack = false;                      
        }

        public void NextTurn(BoardState state, Character activeCharacter)
        {
            //if we have an active character then update all the initiative values
            if (activeCharacter != null)
            {
                ResolveTileEffects(state, activeCharacter, activeCharacter.Position);
                ResolveTerrainEffects(state, activeCharacter, activeCharacter.Position);
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
            BoardState.ActiveCharacter = BoardState.GetCharacterAtInitiative(BoardState, 0);
            activeCharacter = BoardState.ActiveCharacter;

            if (activeCharacter.CharacterType == CharacterType.Majin)
                GainPotential(state, 2);

            activeCharacter.StartTurn();

            //apply any status effects for the new active character that trigger at the start of thier turn.
            foreach (var statusEffect in activeCharacter.StatusEffects)
            {
                statusEffect.StartTurn(BoardState, activeCharacter, this);
            }

            //apply any status effects for characters that apply whenever initiative moves.
            foreach (var character in BoardState.Characters)
            {
                foreach (var statusEffect in character.StatusEffects)
                {
                    statusEffect.Tick(character, this);
                }
            }

            EndTurnEvent?.Invoke(this, new EndTurnEventArgs(BoardState.GetInitiativeList(BoardState).ToList()));
        }

        #region Gamestate Transforms

        public BoardState AddEntity(BoardState state, HexGameObject entity)
        {
            var newState = state.Copy();
            newState.Entities.Add(entity);
            
            SpawnEntityEvent?.Invoke(this,
                new EntityEventArgs()
                {
                    Entity = entity
                });

            TeleportEntityTo(state, entity, entity.Position);

            return state;
        }

        public BoardState MoveEntity(BoardState state, HexGameObject entity, List<HexCoordinate> path)
        {
            var newState = state.Copy();

            foreach (var coordinate in path)
            {
                entity.MoveTo(coordinate);
                CharacterMoveEvent?.Invoke(this, new MoveEventArgs
                {
                    CharacterId = entity.Id,
                    Destination = coordinate
                });

                ResolveTileEffects(newState, entity, coordinate);
                ResolveTerrainEffects(newState, entity, coordinate);
            }

            return state;
        }

        public BoardState TeleportEntityTo(BoardState state, HexGameObject gameObject, HexCoordinate position)
        {
            ResolveTileEffects(state, gameObject, position);
            ResolveTerrainEffects(state, gameObject, position);

            gameObject.MoveTo(position);
            CharacterTeleportEvent?.Invoke(this,
                new MoveEventArgs
                {
                    CharacterId = gameObject.Id,
                    Destination = position
                });

            return state;
        }

        public void ResolveTileEffects(BoardState state, HexGameObject entity, HexCoordinate position)
        {
            var tileEffect = BoardState.TileEffects.FirstOrDefault(data => data.Position == position);

            ResolveTileEffect(state, tileEffect, entity);
        }

        public void ResolveTileEffect(BoardState state, TileEffect tileEffect, HexGameObject entity = null)
        {
            if (tileEffect == null)
                return;

            if (!(entity is Character))
                return;

            tileEffect.TriggerEffect(state, this, (Character)entity);
            RemoveTileEffect(state, tileEffect);
        }

        public void RemoveTileEffect(BoardState state, TileEffect effect)
        {
            if (!BoardState.Entities.Contains(effect)) return;

            BoardState.Entities.Remove(effect);
            RemoveEntityEvent?.Invoke(this, new EntityEventArgs() { Entity = effect });
        }

        //when a character moves into a tile check to see if there're any terrain effects for moving into that tile.
        private void ResolveTerrainEffects(BoardState state, HexGameObject entity, HexCoordinate destination)
        {
            //don't count terrain effects from a tile you're standing. We don't punish players for leaving lava.
            ResolveTerrainEnterEffect(state, entity, BoardState[destination]);
        }

        private void ResolveTerrainEnterEffect(BoardState state, HexGameObject entity, Tile tile)
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
                    ApplyStatus(state, character,
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

        public int ApplyDamage(BoardState state, HexGameObject entity, int damage, string message = null)
        {
            entity.Health -= damage;

            if (!string.IsNullOrWhiteSpace(message))
                SendMessage(message, entity);

            TakeDamageEvent?.Invoke(this,
                new DamageTakenEventArgs { DamageTaken = damage, TargetCharacterId = entity.Id });

            CheckDied(state, entity);

            return damage;
        }

        public void ApplyHealing(BoardState state, Character character, int healing)
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

        public void CheckDied(BoardState state, HexGameObject character)
        {
            //check to see if they died.
            if (character.Health <= 0 && BoardState.Entities.Contains(character))
            {
                BoardState.Entities.Remove(character);
                RemoveEntityEvent?.Invoke(this, new EntityEventArgs() { Entity = character });
            }
        }

        public void ApplyStatus(BoardState state, HexGameObject entity, StatusEffect effect)
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
        public int ApplyCombo(BoardState state, HexGameObject targetEntity, DamageComboAction combo)
        {
            if (!targetEntity.HasStatus)
                return 0;

            ComboEvent?.Invoke(this, new ComboEventArgs(targetEntity.Id, combo));

            //if the player scores a combo they gain potential.
            if (BoardState.ActiveCharacter.IsHero)
                GainPotential(state, 2);
            
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

        public void ApplyPush(BoardState state, HexGameObject targetEntity, HexCoordinate direction, int distance = 0)
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
                    ApplyDamage(state, targetEntity, distance * 15, "IMPACT");
                    distance = 0;
                }
                else if (BoardState.IsHexPassable(state, destinationPos))
                {
                    MoveEntity(state, targetEntity, new List<HexCoordinate> { destinationPos });

                    var tile = BoardState[destinationPos];

                    if (tile.TerrainType != TerrainType.Ice && tile.TerrainType != TerrainType.ThinIce)
                        distance--;

                    destinationPos = destinationPos + direction;
                }
                else if (!BoardState.IsTileEmpty(state, destinationPos))
                {
                    var objectCharacter = BoardState.GetEntityAtCoordinate(state, destinationPos);
                    ApplyDamage(state, targetEntity, distance * CollisionDamage, "Collision");
                    ApplyDamage(state, objectCharacter, distance * CollisionDamage, "Collision");
                    distance = 0;
                }
                else
                {
                    ApplyDamage(state, targetEntity, distance * ImpactDamage, "IMPACT");
                    distance = 0;
                }
            }
        }

        public void GainPotential(BoardState state, int potential = 1)
        {
            if (BoardState.Potential + potential <= BoardState.MaxPotential)
            {
                BoardState.Potential += potential;
            }

            PotentialChangeEvent?.Invoke(this, new PotentialEventArgs(potential));
        }

        public void LosePotential(BoardState state, int potential = 1)
        {
            if (BoardState.Potential >= potential)
            {
                BoardState.Potential -= potential;
            }

            PotentialChangeEvent?.Invoke(this, new PotentialEventArgs(-potential));
        }

        public BoardState CreateTileEffect(BoardState state, TileEffect effect, HexCoordinate location)
        {
            if (!BoardState.IsHexInMap(location))
                return null;

            //don't create a tile effect on unpassable tiles, occupied tiles or tiles that already have effects
            if (!BoardState.IsHexWalkable(state, location) || !BoardState.IsTileEmpty(state, location) || state.TileEffects.Any(te => te.Position == location))
                return null;

            var newState = state.Copy();

            var tileEffect = new TileEffect(effect);
            tileEffect.Position = location;
            BoardState.Entities.Add(tileEffect);

            SpawnEntityEvent?.Invoke(this, new EntityEventArgs
            {
                Entity = tileEffect
            });

            return state;
        }

        public void CompleteAction(Character ch, HexAction action)
        {
            ch.HasActed = true;
            ch.CanAttack = false;
            ActionEvent?.Invoke(this, new ActionEventArgs { Action = action });
        }

        #endregion

        #region Public Accessor Methods

        public bool IsValidTarget(BoardState state, Character objectCharacter, HexCoordinate targetPosition, int range, TargetType targetType)
        {
            return BoardState.IsValidTarget(state, objectCharacter, targetPosition, range, targetType);
        }

        #endregion

        #region Private Helper Methods

        private void SendMessage(string message, HexGameObject targetCharacter)
        {
            MessageEvent?.Invoke(this, new MessageEventArgs(message, targetCharacter));
        }

        private bool IsInEnemySpawnArea(HexCoordinate coord)
        {
            return coord.Z <= 0 && coord.Z >= -3 && coord.X >= -4 && coord.X <= 4;
        }

        private Character FindCharacterById(Guid characterId)
        {
            return BoardState.Characters.FirstOrDefault(cha => cha.Id == characterId);
        }

        #endregion

        #endregion

        #region Movement Attibutes

        public static readonly int[] MovementSpeedSlow = { 0, 1, 2, 2, 3, 3, 3 };
        public static readonly int[] MovementSpeedNormal = { 0, 1, 1, 2, 3, 3, 3 };
        public static readonly int[] MovementSpeedFast = { 0, 0, 1, 2, 2, 3, 3 };

        public static int GetMoveSpeedCost(MovementSpeed ms, int distance)
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
    }
}