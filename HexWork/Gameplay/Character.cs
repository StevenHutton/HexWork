using System;
using System.Collections.Generic;
using HexWork.Gameplay.Actions;
using HexWork.Gameplay.Interfaces;

namespace HexWork.Gameplay
{
    public enum MonsterType
    {
        Zombie,
        ZombieKing,
        Majin,
        Gunner,
        Barbarian,
        IronSoul,
        Ninja
    }

    public class Character
    {
        #region Attributes
        
        public bool HasActed = false;
        public bool IsActive = false; 
        public bool IsHero = false;
        public bool IsAlive = false;
        public bool CanAttack = false;
        public bool CanMove = false;

        private List<HexAction> _actions = new List<HexAction>();
        
        private int _turnCooldown = 100;
        
        private readonly int _potential = 0;
		
	    public MonsterType MonsterType;

        public Guid Id = Guid.NewGuid();

	    public string Name;
       
        public HexCoordinate Position { get; set; } = new HexCoordinate(0, 0, 0);

        public int Health;

        public int MaxHealth;

	    public int TurnTimer;

        public int TurnCooldown => _turnCooldown;

        public int Movement;
        
        public int RangeModifier = 0;

        public static Random rand;

        public List<HexAction> Actions => _actions;

        public List<StatusEffect> StatusEffects = new List<StatusEffect>();

        public int Potential => _potential;

	    public MovementType MovementType = MovementType.NormalMove;

        public bool HasStatus => StatusEffects.Count > 0;
		
		#endregion

        #region Methods

        public Character(string name, int maxHealth, int turnLength, int speed, int potential)
        {
            if(rand == null)
                rand = new Random(DateTime.Now.Millisecond);
            
            //just create the character WAY off the map.
            Position = new HexCoordinate(100,100);

            Name = name;
            MaxHealth = maxHealth;
            Health = maxHealth;
            _turnCooldown = turnLength;
            Movement = speed;
            _potential = potential;

            TurnTimer = rand.Next(0, _turnCooldown);
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
            _actions.Add(action);
        }
        
        public void ApplyStatusEffect(StatusEffect effect)
        {
            if (!StatusEffects.Contains(effect))
                StatusEffects.Add(effect);
        }

        #endregion
    }
}
