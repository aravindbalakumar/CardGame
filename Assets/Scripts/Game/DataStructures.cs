namespace Game.DataStructures
{
    using System;
    using UnityEngine;
    [Serializable]
    public enum CardActionType { Attack, Heal, StatusEffect }
    public enum StatusType { RestoreArmor, RestoreHealth, ShatterArmor, Bleed }

    [Serializable, Flags]
    public enum TargetGroup { self = 1, others = 2, }
    [Serializable]
    public struct CardMetaData
    {
        public string name;
        public Texture2D cardTexture;
    }

    [Serializable]
    public struct CardAction
    {
        public CardActionType actionType;
        public StatusType statusType;
        public int value;
        public int duration;
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
}