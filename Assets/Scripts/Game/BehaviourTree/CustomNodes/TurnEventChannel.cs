using Game.ActorAndPlayers;
using System;
using Unity.Behavior;
using UnityEngine;
using Unity.Properties;

#if UNITY_EDITOR
[CreateAssetMenu(menuName = "Behavior/Event Channels/TurnEventChannel")]
#endif
[Serializable, GeneratePropertyBag]
[EventChannelDescription(name: "TurnEventChannel", message: "[Actor] Turn", category: "Events", id: "0cda10c872336cf5bd60f585686a4d4d")]
public sealed partial class TurnEventChannel : EventChannel<AbstractActor> { }

