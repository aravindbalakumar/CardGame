using System;
using Unity.Behavior;
using UnityEngine;

[Serializable, Unity.Properties.GeneratePropertyBag]
[Condition(name: "StatCompare", story: "[Stats] is [condition] [number]", category: "Conditions", id: "984ffd873becbf85e96951d1b1578112")]
public partial class StatCompareCondition : Condition
{
    [SerializeReference] public BlackboardVariable<float> Stats;
    [Comparison(comparisonType: ComparisonType.All)]
    [SerializeReference] public BlackboardVariable<ConditionOperator> Condition;
    [SerializeReference] public BlackboardVariable<float> Number;

    public override bool IsTrue()
    {
        return true;
    }

    public override void OnStart()
    {
    }

    public override void OnEnd()
    {
    }
}
