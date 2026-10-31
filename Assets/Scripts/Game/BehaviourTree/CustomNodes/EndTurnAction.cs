using Game.ActorAndPlayers;
using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "EndTurn", story: "[this] ends turn", category: "Action", id: "c7b3ec4eebc2376a6dcb66a0fbc51205")]
public partial class EndTurnAction : Action
{
    [SerializeReference] public BlackboardVariable<AbstractActor> This;

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

