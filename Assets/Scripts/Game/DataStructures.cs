namespace Game.DataStructures
{
    using System;
    using UnityEngine;
    [Serializable]
    public enum CardType { offensive, defensive, utiility }
    [Serializable]
    public enum CardActionType { Attack, Heal, Debuff,Buff }

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
        public int value;
        public int persistance;
    }
    [Serializable]
    public struct StatusEffects
    {
        public CardActionType EffectorProperty;
        public EffectInvokeTime timeOfInvoke;

    }
    [Serializable]
    public enum EffectInvokeTime
    {
        preBeginTurn,
        postBeginTurn,
        preEndTurn,
        postEndTurn

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