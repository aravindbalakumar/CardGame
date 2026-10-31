using Game.ActorAndPlayers;
using System;
using Game;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "EndTurn", story: "[bot] ends turn", category: "Action", id: "c7b3ec4eebc2376a6dcb66a0fbc51205")]
public partial class EndTurnAction : Action
{
    [SerializeReference] public BlackboardVariable<BotPlayer> bot;

    protected override Status OnStart()
    {
        if (bot.Value == null)
        {
            return Status.Uninitialized;
        }
        
        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        bot.Value.EndTurn();
        return Status.Success;
    }

    protected override void OnEnd()
    {
    }
}

