using UnityEngine.EventSystems;

namespace Game
{
    using UnityEngine;
    using Game.ActorAndPlayers;
    using Game.ScriptableObjects.Card;
    using System.Collections.Generic;
    // using Game.ActorAndPlayers.AI;
    // using Game.ActorAndPlayers.AI.Behaviour;
    using System;
    using Unity.Behavior;
    using Game.DataStructures;

    public class BotPlayer : AbstractActor,IPointerDownHandler
    {
        Player opponentPlayer;
        [SerializeField] BehaviorGraphAgent _agent;
        public List<Skill> bot_Attack_Skill;
        public List<Skill> bot_Defense_Skill;
        public List<Skill> bot_heal_Skill;
        private void Start() => GameHandler.OnGameSystemsInitialized += Initialize;
        private void OnDestroy() => GameHandler.OnGameSystemsInitialized -= Initialize;

        private void Initialize()
        {
            _agent.BlackboardReference.SetVariableValue("isMyTurn", false);
            _agent.BlackboardReference.SetVariableValue("health", this.Health);
            _agent.BlackboardReference.SetVariableValue("armor", this.MaxArmor);
            _agent.BlackboardReference.SetVariableValue("opponentHealth",  GameHandler.instance.player.Health);
            _agent.BlackboardReference.SetVariableValue("targetActor", GameHandler.instance.player);
            _agent.BlackboardReference.SetVariableValue("Self", this.gameObject);
            _agent.BlackboardReference.SetVariableValue("Self_Reference", this);
            _agent.Graph.Start();
        }
        public void OnPointerDown(PointerEventData eventData)
        {
            if (GameHandler.instance.player.SelectedCard != null)
            {
                if(GameHandler.instance.player.SelectedCard.cardTargetGroup.Contains(this.gameObject.layer))
                {
                    GameHandler.OnAbstractActorClicked.Invoke(this);
                }
            }
        }
        public void UseSkill(Skill skill,AbstractActor target)
        {
            
        }

    public override void StartTurn()
        {
            _agent.BlackboardReference.SetVariableValue("isMyTurn", "true");
        }

        public override void EndTurn()
        {
            
        }
        
    
    }


}


