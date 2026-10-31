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

    public class BotPlayer : AbstractActor
    {
        Player opponentPlayer;
        BehaviorGraphAgent _agent;
        public List<Skill> bot_Attack_Skill;
        public List<Skill> bot_Defense_Skill;
        public List<Skill> bot_heal_Skill;
        private void Start() => GameHandler.OnGameSystemsInitialized += Initialize;
        private void OnDestroy() => GameHandler.OnGameSystemsInitialized -= Initialize;

        private void Initialize()
        {
            _agent.Graph.Start();
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

