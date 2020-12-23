using System;
using System.Collections.Generic;
using HexWork.Gameplay;
using HexWork.Gameplay.Actions;
using HexWork.Gameplay.GameObject;
using HexWork.Gameplay.GameObject.Characters;
using HexWork.Gameplay.Interfaces;

namespace HexWork.GameplayEvents
{
    #region Event Args

    public class MessageEventArgs : EventArgs
    {
        public MessageEventArgs() { }

        public MessageEventArgs(string message)
        {
            Message = message;
        }

        public MessageEventArgs(string message, HexGameObject targetCharacter)
        {
            Message = message;
            Character = targetCharacter;
        }

        public string Message;
        public HexGameObject Character;
    }

    public class MoveEventArgs : EventArgs
    {
        public Guid CharacterId;
        public HexCoordinate Destination;
    }

    public class InteractionRequestEventArgs : EventArgs
    {
        public Guid TargetCharacterId;

        public HexCoordinate TargetPosition;
    }

    public class EntityEventArgs : EventArgs
    {
        public HexGameObject Entity;
    }
    
    public class EndTurnEventArgs : EventArgs
    {
        public List<Character> InitativeOrder { get; set; }

        public EndTurnEventArgs(List<Character> initList)
        {
            InitativeOrder = initList;
        }
    }

    public class DamageTakenEventArgs : EventArgs
    {
        public Guid TargetCharacterId;

        public int DamageTaken;
    }

    public class ActionEventArgs : EventArgs
    {
        public HexAction Action;
    }

    public class StatusEventArgs : EventArgs
    {
        public Guid TargetCharacterId;

        public Element StatusEffectType;

        public StatusEventArgs(Guid targetId, Element effectType)
        {
            TargetCharacterId = targetId;
            StatusEffectType = effectType;
        }
    }

    public class ComboEventArgs : EventArgs
    {
        public Guid TargetCharacterId;

        public DamageComboAction ComboEffect;

        public ComboEventArgs(Guid targetId, DamageComboAction effect)
        {
            TargetCharacterId = targetId;
            ComboEffect = effect;
        }
    }

    public class PotentialEventArgs : EventArgs
    {
        public int PotentialChange;

        public PotentialEventArgs(int potentialChange)
        {
            PotentialChange = potentialChange;
        }
    }

    #endregion
}
