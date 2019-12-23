using System;
using System.Collections.Generic;
using HexWork.Gameplay.Actions;
using HexWork.Gameplay.Interfaces;

namespace HexWork.Gameplay
{
    public enum MonsterType
    {
        Zombie,
        ZombieKing
    }

    public class Character
    {
        #region Attributes

        #region Stats

        //Feeds into damage
        public int Power = 5;

        //indicates how much potential your team can have if this character is your commander.
        public int Command = 0;

        public int Health;
        public int MaxHealth;

        #endregion

        public List<HexAction> Actions = new List<HexAction>();
        public List<StatusEffect> StatusEffects = new List<StatusEffect>(); 

        public Guid Id = Guid.NewGuid();
        public MonsterType MonsterType;
	    public string Name;
        public HexCoordinate Position;
        
        public int Movement;
        public MovementType MovementType = MovementType.NormalMove;

        public bool IsHero = false;
        public bool IsAlive = true;
        public bool CanAttack = false;
        public bool CanMove = false;
        public int RangeModifier = 0;
        public bool HasActed = false;
        public bool IsActive = false;

        public int TurnCooldown;
	    public int TurnTimer;

        private static Random _rand;

        #endregion

        #region Properties

        public bool HasStatus => StatusEffects.Count > 0;
        
        #endregion

        #region Methods

        public Character(string name, int maxHealth, int turnLength, int speed, int potential)
        {
            if(_rand == null)
                _rand = new Random(DateTime.Now.Millisecond);
            
            //just create the character WAY off the map.
            Position = new HexCoordinate(100,100);

            Name = name;
            MaxHealth = maxHealth;
            Health = maxHealth;
            TurnCooldown = turnLength;
            Movement = speed;
            Command = potential;

            TurnTimer = _rand.Next(0, TurnCooldown);
        }
        
        public void StartTurn()
        {
            CanMove = true;
            HasActed = false;
            IsActive = true;
            CanAttack = true;
        }

        public void EndTurn()
        {
            CanMove = false;
            HasActed = true;
            IsActive = false;
            CanAttack = false;
        }

        public void SpawnAt(HexCoordinate position)
        {
            Position = position;
            IsAlive = true;
        }

        public void MoveTo(HexCoordinate position)
        {
            CanMove = false;
            Position = position;
        }

        public void AddAction(HexAction action)
        {
            if(!Actions.Contains(action))
                Actions.Add(action);
        }
        
        public void ApplyStatusEffect(StatusEffect effect)
        {
            if (!StatusEffects.Contains(effect))
                StatusEffects.Add(effect);
        }

        #endregion
    }
}
