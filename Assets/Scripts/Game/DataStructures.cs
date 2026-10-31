namespace Game.DataStructures
{
    using System;
    using UnityEngine;
    [Serializable]
    public enum ActionType { Damage=1, Heal=2,Armor=3, StatusEffect=4, Attribute=5}
    public enum StatusType { RestoreArmor, RestoreHealth, ShatterArmor, Bleed }
    public enum StatusDurationType { Turn, Match }
    public enum StatusStartPoint { StartOfTurn, EndOfTurn }

    [Serializable, Flags]
    public enum TargetGroup { self = 1, others = 2, }
    [Serializable]
    public struct CardMetaData
    {
        public string name;
        public Texture2D cardTexture;
        public string animationTriggerWord;
    }

    [Serializable]
    public struct Action
    {
        public ActionType actionType;
        public int value;
        public bool isAStatusType;
        public StatusType statusType;
        public StatusDurationType durationType;
        public StatusStartPoint startPoint;
        public int numberOfTurns;
    }
    [Serializable]
    public struct PlayerMetaData
    {
        public string name;
        public string ID;
    }
    [Serializable]
    public struct PlayerStats
    {
        public int MaxHealth;
        public int Health;
        public int Armor;
        public int MaxArmor;
        public int Damage;
    }

    public enum ComparisonMethod
    {
        GREATERTHAN,
        LESSTHAN,
        GREATERTHANEQUAL,
        LESSTHANEQUAL,
        NOTEQUAL,
        EQUAL,
        DIRECTPOSITIVE,
        DIRECTNEGATIVE,
    }

    public enum BotAction
    {
        heal,
        attack,
        defend,
        flee
    }
    public enum NodeType
    {
        comparison,
        execution
    }

    public enum Attribute
    {
        health,
        armor,
        debuffCount,
        healthPercent,
        armorPercent
    }
}