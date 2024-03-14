namespace Game
{
    using UnityEngine;
    using Game.ActorAndPlayers;
    using Game.ScriptableObjects.Card;
    using System.Collections.Generic;
    using Game.ActorAndPlayers.AI;
    using Game.ActorAndPlayers.AI.Behaviour;
    using System;
    using Game.DataStructures;

    public class BotPlayer : Player
    {
        [SerializeField] List<CardTypeScriptableObject> currentLoadout;
        [SerializeField] BehaviourNode rootNode;
        Player opponentPlayer;
        BehaviourTree behaviourTree;
        private void Start() => GameHandler.instance.OnGameSystemsInitialized += Initialize;
        private void OnDestroy() => GameHandler.instance.OnGameSystemsInitialized -= Initialize;

        private void Initialize()
        {
            if (!this.intialized)
            {
                behaviourTree = new BehaviourTree(rootNode, this);
                this.Initialize("BOT", GameHandler.instance.mainCamera, currentLoadout);
            }
        }
        public void FetchAndUseCard(CardActionType incomingType, Action OnSuccess, Action OnFail)
        {
            int index = currentHand.FindIndex(x => x.cardAction.actionType == incomingType);
            if (index == -1)
            {
                OnFail?.Invoke();
                return;
            }
            else
            {
                if (incomingType == CardActionType.Heal || incomingType == CardActionType.Armor)
                {
                    GameHandler.instance.ProcessCardAction(this, currentHand[index].cardAction);
                    GameHandler.instance.ProcessCardAction(this, currentHand[index].postCardAction);
                }
                else
                {
                    GameHandler.instance.ProcessCardAction(opponentPlayer, currentHand[index].cardAction);
                    GameHandler.instance.ProcessCardAction(opponentPlayer, currentHand[index].postCardAction);
                }
                OnSuccess?.Invoke();
            }
        }
        public void Flee(Action OnSuccess, Action OnFail) => currentHand.FindIndex(x => x.cardAction.actionType == DataStructures.CardActionType.Heal);
        public void Enhance(Action OnSuccess, Action OnFail) => currentHand.FindIndex(x => x.cardAction.actionType == DataStructures.CardActionType.Heal);
        public void Debuff(Action OnSuccess, Action OnFail) => currentHand.FindIndex(x => x.cardAction.actionType == DataStructures.CardActionType.Heal);
    }

}

