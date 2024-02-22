
namespace Game.ScriptableObjects.Card
{
    using Game.DataStructures;
    using System;
    using System.Collections.Generic;
    using UnityEngine;
    [CreateAssetMenu(fileName ="card",menuName ="Create Card",order =2)]
    public class CardTypeScriptableObject : ScriptableObject
    {
        public CardMetaData metaData;
        public CardType type;
        public TargetGroup targetGroup;
        public List<CardAction> selfCardActions;
        public List<CardAction> otherCardActions;
        [Range(1, 10)] public int energyUsage = 1;
    }
}

