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
        public bool CanMove = false;
        public int Health;
        public int MaxHealth;
        public bool IsHero = false;
        
        public List<StatusEffect> StatusEffects = new List<StatusEffect>();
        public bool HasStatus => StatusEffects.Count > 0;

        public HexGameObject()
        { }

        public HexGameObject(string name, int maxHealth)
        {
            Name = name;
            MaxHealth = maxHealth;
            Health = maxHealth;
        }

        public void MoveTo(HexCoordinate position)
        {
            CanMove = false;
            Position = position;
        }
    }
}