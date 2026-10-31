using System;
using Game.ActorAndPlayers;
using Unity.Behavior;
using UnityEngine;

[Serializable, Unity.Properties.GeneratePropertyBag]
[Condition(name: "CanCastSkill", story: "can [this] cast [Skill]", category: "Conditions", id: "8224394a5c80128b35d1fd22029f8194")]
public partial class CanCastSkillCondition : Condition
{
    [SerializeReference] public BlackboardVariable<AbstractActor> This;
    [SerializeReference] public BlackboardVariable<SkillType> Skill;
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
