using System;
using System.Collections.Generic;
using HexWork.Gameplay.Actions;
using HexWork.Gameplay.Interfaces;

namespace HexWork.Gameplay.GameObject.Characters
{
    public enum CharacterType
    {
        Zombie,
        ZombieKing,
        Majin,
        Gunner,
        IronSoul,
        Barbarian,
        Ninja
    }

    public class Character : HexGameObject
    {
        #region Attributes

        #region Stats

        //Feeds into damage - all damage dealt by this character is multiplied by this number.
        public int Power = 5;

        #endregion

        public List<HexAction> Actions = new List<HexAction>();

        public CharacterType CharacterType;
        
        public MovementType MovementType = MovementType.NormalMove;
        public MovementSpeed MovementSpeed = MovementSpeed.Normal;

        public bool IsAlive = true;
        public bool CanAttack = false;
        public int RangeModifier = 0;
        public bool HasActed = false;
        public bool IsActive = false;

        public int TurnCooldown;
	    public int TurnTimer;

        private static Random _rand;

        public Action<IGameStateObject, Character> DoTurn { get; set; }

        #endregion
        
        #region Methods

        public Character(string name, int maxHealth, int turnLength) : base()
        {
            if(_rand == null)
                _rand = new Random(DateTime.Now.Millisecond);
            
            //just create the character WAY off the map.
            Position = new HexCoordinate(100,100);

            Name = name;
            MaxHealth = maxHealth;
            Health = maxHealth;
            TurnCooldown = turnLength;

            TurnTimer = _rand.Next(0, TurnCooldown);
        }

        //copy constructor
        public Character(Character ch): base(ch)
        {
            Power = ch.Power;
            foreach(var action in ch.Actions)
            {
                Actions.Add(action);
            }

            CharacterType = ch.CharacterType;

            MovementType = ch.MovementType;
            MovementSpeed = ch.MovementSpeed;

            IsAlive = ch.IsAlive;
            CanAttack = ch.CanAttack;
            RangeModifier = ch.RangeModifier;
            HasActed = ch.HasActed;
            IsActive = ch.IsActive;

            TurnCooldown = ch.TurnCooldown;
            TurnTimer = ch.TurnTimer;
            DoTurn = ch.DoTurn;
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

        public void AddAction(HexAction action)
        {
            if(!Actions.Contains(action))
                Actions.Add(action);
        }

        public override HexGameObject Copy()
        {
            return new Character(this);
        }

        #endregion
    }
}
