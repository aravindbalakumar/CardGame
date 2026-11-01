using Game.ScriptableObjects.Card;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace Game
{
    using Engine;
    using Game.ActorAndPlayers;
    using Game.DataStructures;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;

    public class GameHandler : MonoBehaviour
    {
        [SerializeField] List<AbstractActor> players;
        [SerializeField] private AbstractActor currentTurnActor;
        [SerializeField] public Player2D player;
        public int curentTurnIndex = -1;
        [Header("Support Systems")]
        public Camera mainCamera;
        public Physics2DRaycaster raycaster;
        // [SerializeField] InputHandler _inputHandler;
        #region Events
        public static System.Action OnGameSystemsInitialized;
        public static System.Action OnGameSystemsClosed;
        public static System.Action<LayerMask?> OnTargettingModeChanged;
        public static System.Action<AbstractActor> OnAbstractActorClicked;
        public static System.Action<AbstractActor,CardTypeScriptableObject> OnAbstractActorClickedWithCard;
        #endregion
        // public InputHandler inputHandler
        // {
        //     get
        //     {
        //         return _inputHandler;
        //     }
        //     private set { }
        // }

        public static GameHandler instance
        {
            get
            {
                return _instance;
            }
            private set
            {

            }
        }
        private static GameHandler _instance;
        private void Awake()
        {
            if (_instance != null)
            {
                Destroy(this.gameObject);
                return;
            }
            _instance = this;
            OnAbstractActorClickedWithCard += ProcessCardAction;
            // StartCoroutine(InitializeSupportSystems());
        }

        public void ProcessCardAction(AbstractActor abstractActor, CardTypeScriptableObject cardData)
        {
            switch (cardData.skill.cardAction.actionType)
            {
                case ActionType.Damage:
                    abstractActor.TakeDamage(cardData.skill.cardAction.value);
                    break;
                case ActionType.StatusEffect:
                    abstractActor.ApplyStatus(cardData.skill.cardAction.statusType, cardData.skill.cardAction.numberOfTurns, cardData.skill.cardAction.value);
                    break;
                case ActionType.Heal:
                    abstractActor.AddHealth(cardData.skill.cardAction.value);
                    break;
                case ActionType.Armor:
                    abstractActor.AddArmor(cardData.skill.cardAction.value);
                    break;
            }
        }
        private IEnumerator Start()
        {
            yield return new WaitForSeconds(0.5f);
            // yield return new WaitUntil(() => _inputHandler.initialized);
            OnGameSystemsInitialized?.Invoke();
            StartGame();

        }

        private void OnDestroy()
        {
            OnAbstractActorClickedWithCard -= ProcessCardAction;
            OnGameSystemsClosed?.Invoke();
        }


        public void EnterTargettingMode(LayerMask? incomingLayerMask)
        {
            if (incomingLayerMask.HasValue)
            {
                raycaster.eventMask = incomingLayerMask.Value;
            }
            OnTargettingModeChanged?.Invoke(incomingLayerMask);
            // if (incomingLayerMask == null)
            // {
            //     // _inputHandler.layerMask.value &= ~(1 << playerLayerInt);
            //     // _inputHandler.layerMask.value &= ~(1 << otherLayerInt);
            //     // _inputHandler.UpdateRaycastLayer();
            //     players.ForEach(x => x.Highlight(false));
            //     return;
            // }
            // else
            // {
            //     OnTargettingModeChanged?.Invoke(incomingLayerMask);
            //     if (incomingTargetGroup.Value.HasFlag(DataStructures.TargetGroup.self))
            //     {
            //         actor.Highlight(true);
            //         // _inputHandler.layerMask.value |= (1 << playerLayerInt);
            //     }
            //     if (incomingTargetGroup.Value.HasFlag(DataStructures.TargetGroup.others))
            //     {
            //         players.ForEach(x =>
            //         {
            //             if (x.gameObject != actor.gameObject)
            //             {
            //                 x.Highlight(true);
            //             }
            //         });
            //         // _inputHandler.layerMask.value |= (1 << otherLayerInt);
            //     }
            //     // _inputHandler.UpdateRaycastLayer();
            // }
        }

        public void StartGame(int startIndex=0)
        {
            curentTurnIndex = startIndex;
            currentTurnActor = players[curentTurnIndex];
            currentTurnActor.StartTurn();
        }

        public void EndTurn()
        {
            curentTurnIndex=curentTurnIndex+1<players.Count? curentTurnIndex+1 :0;
            currentTurnActor = players[curentTurnIndex];
            currentTurnActor.StartTurn();
        }
    }
}
