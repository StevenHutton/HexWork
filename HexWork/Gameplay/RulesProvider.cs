using System;
using System.Collections.Generic;
using System.Linq;
using HexWork.Gameplay.Actions;
using HexWork.Gameplay.GameObject;
using HexWork.Gameplay.GameObject.Characters;
using HexWork.Gameplay.Interfaces;
using HexWork.Gameplay.StatusEffects;
using HexWork.GameplayEvents;

namespace HexWork.Gameplay
{
    public class RulesProvider : IRulesProvider
    {
        #region Attributes

        public const int ImpactDamage = 25;

        public const int CollisionDamage = 15;

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
        }

        public BoardState CreateCharacters(BoardState state, int difficulty = 1)
        {
            var newState = state.Copy();
            newState.Entities.AddRange(CharacterFactory.CreateHeroes());
            newState.Entities.AddRange(CharacterFactory.CreateEnemies(difficulty));
            return newState;
        }

        #endregion

        #region Game Start

        public BoardState StartGame(BoardState state)
        {
            var newState = state.Copy();
            var characters = newState.Characters;

            //spawn enemies
            foreach (var character in newState.Characters.Where(c => !c.IsHero))
            {
                var coordinate = BoardState.GetRandomCoordinateInMap();

                //one unit per tile and only deploy to walkable spaces.
                while (characters.Select(cha => cha.Position).Contains(coordinate) || !newState[coordinate].IsWalkable || !IsInEnemySpawnArea(coordinate))
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
            foreach (var character in newState.Heroes)
            {
                var position = spawnPoint;
                var validTile = BoardState.IsWalkableAndEmpty(newState, position);

                while (!validTile)
                {
                    position = BoardState.GetNearestEmptyNeighbourRecursive(newState, position, 9);
                    //tiles are only valid so long as they're walkable and have at least one passable neighbor.
                    validTile = BoardState.IsWalkableAndEmpty(newState, position) &&
                                BoardState.GetNeighbours(position).Any(d => BoardState.IsWalkableAndEmpty(newState, d));
                }

                character.SpawnAt(position);
                SpawnEntityEvent?.Invoke(this,
                    new EntityEventArgs()
                    {
                        Entity = character
                    });
            }

            newState = NextTurn(newState, BoardState.GetCharacterAtInitiative(newState, 0).Id);
            return newState;
        }

        #endregion

        #region Metegame

        #endregion

        #region Public Update Methods

        public BoardState Update(BoardState state)
        {
            var newState = state.Copy();
            if (newState.ActiveCharacter == null)
                return NextTurn(newState, new Guid());

            if (newState.ActiveCharacter.IsHero)
                return newState;

            //if they can't act, end turn
            if (newState.ActiveCharacterHasAttacked)
            {
                newState = NextTurn(newState, newState.ActiveCharacter.Id);
                return newState;
            }

            newState = newState.ActiveCharacter.DoTurn(newState, this, newState.ActiveCharacter);
            newState.ActiveCharacterHasAttacked = true;
            return newState;
        }

        public BoardState NextTurn(BoardState state, Guid activeCharacterId)
        {
            var newState = state.Copy();
            var activeCharacter = newState.Characters.FirstOrDefault(data => data.Id == activeCharacterId);

            //if we have an active character then update all the initiative values
            if (activeCharacter != null)
            {
                newState = ResolveTileEffect(newState, activeCharacter.Position);
                newState = ResolveTerrainEffects(newState, activeCharacter.Position);
                activeCharacter = newState.Characters.FirstOrDefault(data => data.Id == activeCharacterId);

                var deltaTime = activeCharacter.TurnTimer;
                foreach (var character in newState.Characters)
                {
                    character.TurnTimer -= deltaTime;
                }

                activeCharacter.TurnTimer += activeCharacter.TurnCooldown;
            }

            //get the new active character
            newState.ActiveCharacter = BoardState.GetCharacterAtInitiative(newState, 0);
            activeCharacter = newState.ActiveCharacter;
            activeCharacterId = activeCharacter.Id;
            newState.ActiveCharacterId = activeCharacter.Id;
            newState.ActiveCharacterHasAttacked = false;
            newState.ActiveCharacterHasMoved = false;

            //apply any status effects for the new active character that trigger at the start of thier turn.
            foreach (var statusEffect in activeCharacter.StatusEffects.ToList())
            {
                newState = statusEffect.StartTurn(newState, activeCharacterId, this);
            }

            EndTurnEvent?.Invoke(this, new EndTurnEventArgs(BoardState.GetInitiativeList(newState).ToList()));

            return newState;
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

            TeleportEntityTo(newState, entity.Id, entity.Position);

            return newState;
        }

        public BoardState MoveEntity(BoardState state, Guid id, List<HexCoordinate> path)
        {
            var newState = state.Copy();

            foreach (var coordinate in path)
            {
                var entity = newState.Entities.First(ent => ent.Id == id);
                entity.MoveTo(coordinate);
                CharacterMoveEvent?.Invoke(this, new MoveEventArgs
                {
                    CharacterId = entity.Id,
                    Destination = coordinate
                });

                newState = ResolveTileEffect(newState, coordinate);
                newState = ResolveTerrainEffects(newState, coordinate);
            }

            return newState;
        }

        public BoardState TeleportEntityTo(BoardState state, Guid entityId, HexCoordinate position)
        {
            var newState = state.Copy();

            var gameObject = newState.Entities.FirstOrDefault(ent => ent.Id == entityId);
            if (gameObject == null)
                return state;

            gameObject.MoveTo(position);

            CharacterTeleportEvent?.Invoke(this,
                new MoveEventArgs
                {
                    CharacterId = gameObject.Id,
                    Destination = position
                });

            newState = ResolveTileEffect(newState, position);
            newState = ResolveTerrainEffects(newState, position);

            return newState;
        }

        public BoardState ResolveTileEffect(BoardState state, HexCoordinate position)
        {
            var newState = state.Copy();
            var tileEffect = newState.TileEffects.FirstOrDefault(data => data.Position == position);
            if (tileEffect == null)
                return state;

            newState = tileEffect.TriggerEffect(newState, this).Result;
            newState = RemoveTileEffect(newState, tileEffect.Id);

            return newState;
        }

        public BoardState RemoveTileEffect(BoardState state, Guid id)
        {
            var newState = state.Copy();
            var te = newState.TileEffects.FirstOrDefault(data => data.Id == id);
            if (te == null)
                return state;

            newState.Entities.Remove(te);
            RemoveEntityEvent?.Invoke(this, new EntityEventArgs() { Entity = te });
            return newState;
        }

        //when a character moves into a tile check to see if there're any terrain effects for moving into that tile.
        private BoardState ResolveTerrainEffects(BoardState state, HexCoordinate position)
        {
            var newState = state.Copy();
            var character = newState.Characters.FirstOrDefault(ch => ch.Position == position);
            if (character == null)
                return state;

            switch (state[position].TerrainType)
            {
                case TerrainType.Ground:
                    break;
                case TerrainType.Water:
                    break;
                case TerrainType.Lava:
                    newState = ApplyStatus(newState, character.Id,
                        new DotEffect
                        {
                            Damage = 5,
                            Name = "Fire",
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

            return newState;
        }

        public BoardState ApplyDamage(BoardState state, Guid entityId, int damage)
        {
            var newState = state.Copy();

            var entity = newState.Entities.FirstOrDefault(ent => ent.Id == entityId);
            if (entity == null)
                return state;

            entity.Health -= damage;

            TakeDamageEvent?.Invoke(this,
                new DamageTakenEventArgs { DamageTaken = damage, TargetCharacterId = entityId });

            newState = CheckDied(newState, entityId);

            return newState;
        }

        public BoardState ApplyHealing(BoardState state, Guid id, int healing)
        {
            var newState = state.Copy();
            var character = newState.Characters.FirstOrDefault(data => data.Id == id);
            if (character == null)
                return state;

            character.Health += healing;
            if (character.Health >= character.MaxHealth)
                character.Health = character.MaxHealth;

            TakeDamageEvent?.Invoke(this, new DamageTakenEventArgs()
            {
                DamageTaken = -healing,
                TargetCharacterId = character.Id
            });

            return newState;
        }

        public BoardState CheckDied(BoardState state, Guid id)
        {
            var newState = state.Copy();
            var character = newState.Entities.FirstOrDefault(data => data.Id == id);

            //check to see if they died.
            if (character.Health <= 0)
            {
                newState.Entities.Remove(character);
                RemoveEntityEvent?.Invoke(this, new EntityEventArgs() { Entity = character });
                return newState;
            }

            return state;
        }

        public BoardState ApplyStatus(BoardState state, Guid entityId, StatusEffect effect)
        {
            //todo - apply status effects based on status damage
            //for now we just always apply any relevant status effects
            if (effect == null) return state;

            var newState = state.Copy();

            var entity = newState.GetEntityById(entityId);
            if (entity == null)
                return state;

            var effectToApply = effect.Copy();
            entity.StatusEffects.Add(effectToApply);

            StatusAppliedEvent?.Invoke(this, new StatusEventArgs(entity.Id, effectToApply));
            return newState;
        }

        /// <summary>
        /// Apply a combo effect to a target character if they're currently suffering a status effect.
        /// todo - Remove the current status effect from the character.
        /// </summary>
        /// <param name="targetCharacter"></param>
        /// <param name="combo"></param>
        public BoardState ApplyCombo(BoardState state, Guid targetEntid, DamageComboAction combo, out int damage)
        {
            damage = 0;

            var newState = state.Copy();
            var entity = newState.Characters.FirstOrDefault(data => data.Id == targetEntid);
            if (entity == null)
                return state;

            if (!entity.HasStatus)
                return state;

            ComboEvent?.Invoke(this, new ComboEventArgs(targetEntid, combo));

            //if the player scores a combo they gain potential.
            if (newState.ActiveCharacter.IsHero)
                GainPotential(newState, 2);

            var status = entity.StatusEffects.First();

            damage = entity.StatusEffects.Count(e => e.StatusEffectType == status.StatusEffectType);

            foreach (var statusEffect in entity.StatusEffects.Where(s =>
                s.StatusEffectType == status.StatusEffectType).ToList())
            {
                entity.StatusEffects.Remove(statusEffect);
                StatusRemovedEvent?.Invoke(this, new StatusEventArgs(targetEntid, statusEffect));
            }

            return newState;
        }

        public BoardState ApplyPush(BoardState state, Guid targetEntId, HexCoordinate direction, int distance = 0)
        {
            var newState = state.Copy();
            var targetEntity = newState.Entities.FirstOrDefault(data => data.Id == targetEntId);
            if (targetEntity == null)
                return state;

            var targetCharacterPos = targetEntity.Position;
            var destinationPos = targetCharacterPos + direction;
            while (distance > 0)
            {
                if (!newState.ContainsKey(destinationPos))
                {
                    newState = ApplyDamage(newState, targetEntity.Id, distance * 15);
                    distance = 0;
                }
                else if (BoardState.IsWalkableAndEmpty(newState, destinationPos))
                {
                    newState = MoveEntity(newState, targetEntity.Id, new List<HexCoordinate> { destinationPos });

                    var tile = newState[destinationPos];

                    if (tile.TerrainType != TerrainType.Ice && tile.TerrainType != TerrainType.ThinIce)
                        distance--;

                    destinationPos = destinationPos + direction;
                }
                else if (!BoardState.IsTileEmpty(newState, destinationPos))
                {
                    var objectCharacter = BoardState.GetEntityAtCoordinate(newState, destinationPos);
                    newState = ApplyDamage(newState, targetEntity.Id, distance * CollisionDamage);
                    newState = ApplyDamage(newState, objectCharacter.Id, distance * CollisionDamage);
                    distance = 0;
                }
                else
                {
                    newState = ApplyDamage(newState, targetEntity.Id, distance * ImpactDamage);
                    distance = 0;
                }
            }

            return newState;
        }

        public BoardState GainPotential(BoardState state, int potential = 1)
        {
            if (state.Potential + potential <= state.MaxPotential)
            {
                state.Potential += potential;
            }

            PotentialChangeEvent?.Invoke(this, new PotentialEventArgs(potential));
            return state;
        }

        public BoardState LosePotential(BoardState state, int potential = 1)
        {
            if (state.Potential >= potential)
            {
                state.Potential -= potential;
            }

            PotentialChangeEvent?.Invoke(this, new PotentialEventArgs(-potential));
            return state;
        }

        public BoardState CreateTileEffect(BoardState state, TileEffect effect, HexCoordinate location)
        {
            if (!BoardState.IsHexInMap(location))
                return state;

            //don't create a tile effect on unpassable tiles, occupied tiles or tiles that already have effects
            if (!BoardState.IsHexWalkable(state, location) || !BoardState.IsTileEmpty(state, location) || state.TileEffects.Any(te => te.Position == location))
                return state;

            var newState = state.Copy();

            var tileEffect = new TileEffect(effect);
            tileEffect.Position = location;
            newState.Entities.Add(tileEffect);

            SpawnEntityEvent?.Invoke(this, new EntityEventArgs
            {
                Entity = tileEffect
            });

            return newState;
        }

        public BoardState CompleteAction(BoardState state, Guid id, HexAction action)
        {
            var newState = state.Copy();
            var ch = newState.Characters.FirstOrDefault(data => data.Id == id);
            if (ch == null)
                return state;

            ActionEvent?.Invoke(this, new ActionEventArgs { Action = action });

            newState.ActiveCharacterHasAttacked = true;

            return newState;
        }

        #endregion

        #region Public Accessor Methods

        //public bool IsValidTarget(BoardState state, Character objectCharacter, HexCoordinate targetPosition, int range, TargetType targetType)
        //{
        //    return BoardState.IsValidTarget(state, objectCharacter, targetPosition, range, targetType);
        //}

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

        public static readonly int[] AttackRangeCostNormal = { 0, 0, 1, 0, 1, 0, 1 };

        public static int GetRangeCost(int distance)
        {
            if (distance >= 7)
                return 10;

            int cost = 0;
            for(int i = 0; i<=distance; i++)
            {
                cost += AttackRangeCostNormal[i];
            }
            return cost;
        }

        #endregion

        #endregion
    }
}