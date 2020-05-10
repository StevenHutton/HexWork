using System;
using System.Collections.Generic;
using HexWork.Gameplay;
using HexWork.Gameplay.Actions;
using HexWork.Gameplay.Characters;
using HexWork.Gameplay.GameObject;
using HexWork.Gameplay.GameObject.Characters;

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

    public class SpawnTileEffectEventArgs : EventArgs
    {
        public Guid Id;

        public TileEffect Effect;

        public HexCoordinate Position;
    }

    public class RemoveTileEffectEventArgs : EventArgs
    {
        public Guid Id;
    }

    public class SpawnChracterEventArgs
    {
        public Character Character;

        public MonsterType MonsterType;
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

        public StatusEffect StatusEffect;

        public StatusEventArgs(Guid targetId, StatusEffect effect)
        {
            TargetCharacterId = targetId;
            StatusEffect = effect;
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
