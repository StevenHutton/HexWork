﻿using System;
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

        public int RangeModifier = 0;
        public int TurnCooldown;
	    public int TurnTimer;

        private static Random _rand;

        public Func<BoardState, IRulesProvider, Character, BoardState> DoTurn { get; set; }

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
            Id = ch.Id;
            Power = ch.Power;
            Actions = ch.Actions;

            CharacterType = ch.CharacterType;

            MovementType = ch.MovementType;
            MovementSpeed = ch.MovementSpeed;

            RangeModifier = ch.RangeModifier;

            TurnCooldown = ch.TurnCooldown;
            TurnTimer = ch.TurnTimer;
            DoTurn = ch.DoTurn;
        }
        
        public void SpawnAt(HexCoordinate position)
        {
            Position = position;
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
