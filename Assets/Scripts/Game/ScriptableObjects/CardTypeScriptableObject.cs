
namespace Game.ScriptableObjects.Card
{
    using Game.DataStructures;
    using System;
    using System.Collections.Generic;
    using UnityEngine;
    [CreateAssetMenu(fileName ="card",menuName = "Card", order =2)]
    public class CardTypeScriptableObject : ScriptableObject
    {
        public CardMetaData metaData;
        [Range(1, 10)] public int energyUsage = 1;
        public Skill skill;
    }
    
}

