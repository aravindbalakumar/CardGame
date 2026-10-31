
namespace Game.ScriptableObjects.Card
{
    
    using UnityEngine;
    using Game.DataStructures;
    [System.Serializable]
    public class Skill
    {
        public TargetGroup targetGroup;
        public Action cardAction;
        public bool doesHavePostCardAction=false;
        public Action postCardAction;
        public bool doesHaveNegativeEffect=false;
        public Action negativeEffect;
    }
}