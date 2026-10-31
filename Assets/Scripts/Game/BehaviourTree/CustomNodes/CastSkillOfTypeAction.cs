using Game.ActorAndPlayers;
using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "CastSkillOfType", story: "[this] casts [skill] [target]", category: "Action", id: "edca82e51fbae149f280916410746f49")]
public partial class CastSkillOfTypeAction : Action
{
    [SerializeReference] public BlackboardVariable<AbstractActor> This;
    [SerializeReference] public BlackboardVariable<SkillType> Skill;
    [SerializeReference] public BlackboardVariable<AbstractActor> Target;

    protected override Status OnStart()
    {
        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        return Status.Success;
    }

    protected override void OnEnd()
    {
    }
}

