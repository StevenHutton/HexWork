using HexWork.Gameplay.Interfaces;
using System;
using System.Collections.Generic;

namespace HexWork.Gameplay.GameObject
{
    public class HexGameObject
    {
        public Guid Id = Guid.NewGuid();
        public string Name;
        public HexCoordinate Position;
        public bool BlocksMovement = true;
        public int Health;
        public int MaxHealth;
        public bool IsHero = false;
        
        public List<Element> StatusEffects = new List<Element>();
        public bool HasStatus => StatusEffects.Count > 0;

        public HexGameObject()
        { }

        //copy constructor
        public HexGameObject(HexGameObject toCopy)
        {
            Id = toCopy.Id;
            Name = toCopy.Name;
            Position = toCopy.Position;
            BlocksMovement = toCopy.BlocksMovement;
            Health = toCopy.Health;
            MaxHealth = toCopy.MaxHealth;
            IsHero = toCopy.IsHero;
            StatusEffects = toCopy.StatusEffects;
        }

        public HexGameObject(string name, int maxHealth)
        {
            Name = name;
            MaxHealth = maxHealth;
            Health = maxHealth;
        }

        public void MoveTo(HexCoordinate position)
        {
            Position = position;
        }

        public virtual HexGameObject Copy()
        {
            return new HexGameObject(this);
        }
    }
}