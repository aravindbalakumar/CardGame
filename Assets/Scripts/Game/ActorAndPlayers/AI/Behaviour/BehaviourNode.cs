namespace Game.ActorAndPlayers.AI.Behaviour
{
    using Game.DataStructures;
    using System.Collections.Generic;
    using UnityEngine;

    [CreateAssetMenu(fileName = "BehaviourNode", menuName = "AI/AIBehaviourNode")]
    public class BehaviourNode : ScriptableObject
    {
        public List<Comparison> conditions;
        public bool shouldIncludeAllCondition;//andCondition
        public BotAction botAction;
        public NodeType nodeType;
        public BehaviourNode negativeOutcome;
        public BehaviourNode outcome;
    }
    [System.Serializable]
    public class Comparison
    {

        public Attribute attributeToCompare;
        [SerializeField] ComparisonMethod comparisonMethod;
        [SerializeField] int thresholdValue;

        public bool Compare(int value)
        {
            switch (comparisonMethod)
            {
                case ComparisonMethod.GREATERTHAN: return value > thresholdValue;
                case ComparisonMethod.LESSTHAN: return value < thresholdValue;
                case ComparisonMethod.EQUAL: return value == thresholdValue;
                case ComparisonMethod.NOTEQUAL: return value != thresholdValue;
                case ComparisonMethod.LESSTHANEQUAL: return value <= thresholdValue;
                case ComparisonMethod.GREATERTHANEQUAL: return value > thresholdValue;
                case ComparisonMethod.DIRECTPOSITIVE: return true;
                case ComparisonMethod.DIRECTNEGATIVE: return false;
                default: return false;
            }
        }
    }
}
