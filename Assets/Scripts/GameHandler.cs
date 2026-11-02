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
        // Optional presentation hook. Card effects remain owned by ProcessCardAction.
        public Func<AbstractActor, CardTypeScriptableObject, bool> CardActionAnimation { get; set; }
        // Presentation may claim a bot turn and finish it through that same actor's EndTurn.
        public Func<BotPlayer, bool> BotTurnAnimation { get; set; }
        public bool CardActionInProgress { get; internal set; }
        public AbstractActor CurrentTurnActor => currentTurnActor;
        public bool IsPlayerTurn => currentTurnActor == player && player != null && player.Health > 0;
        public event System.Action<AbstractActor> TurnChanged;
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
        private LayerMask defaultEventMask;
        private PhysicsRaycaster worldRaycaster;
        private LayerMask defaultWorldEventMask;
        private void Awake()
        {
            if (_instance != null)
            {
                Destroy(this.gameObject);
                return;
            }
            _instance = this;
            defaultEventMask = raycaster.eventMask;
            foreach (var candidate in mainCamera.GetComponents<PhysicsRaycaster>())
            {
                if (candidate is Physics2DRaycaster) continue;
                worldRaycaster = candidate;
                defaultWorldEventMask = candidate.eventMask;
                break;
            }
            OnAbstractActorClickedWithCard += HandleCardAction;
            // StartCoroutine(InitializeSupportSystems());
        }

        private void HandleCardAction(AbstractActor target, CardTypeScriptableObject cardData)
        {
            if (CardActionInProgress || !IsPlayerTurn || target == null || target.Health <= 0 || cardData == null || cardData.skill == null) return;
            if (CardActionAnimation != null && CardActionAnimation(target, cardData)) return;
            if (cardData.skill.targetGroup.Contains(target.gameObject.layer)) ProcessCardAction(target, cardData);
        }

        public void ProcessCardAction(AbstractActor abstractActor, CardTypeScriptableObject cardData)
        {
            if (cardData?.skill == null) return;
            ProcessSkillAction(abstractActor, cardData.skill);
        }

        public void ProcessSkillAction(AbstractActor abstractActor, Skill skill)
        {
            if (abstractActor == null || skill == null) return;
            var action = skill.cardAction;
            switch (action.actionType)
            {
                case ActionType.Damage:
                    abstractActor.TakeDamage(action.value);
                    break;
                case ActionType.StatusEffect:
                    abstractActor.ApplyStatus(action.statusType, action.numberOfTurns, action.value);
                    break;
                case ActionType.Heal:
                    abstractActor.AddHealth(action.value);
                    break;
                case ActionType.Armor:
                    abstractActor.AddArmor(action.value);
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
            if (_instance != this) return;
            OnAbstractActorClickedWithCard -= HandleCardAction;
            CardActionAnimation = null;
            BotTurnAnimation = null;
            CardActionInProgress = false;
            OnGameSystemsClosed?.Invoke();
            _instance = null;
        }


        public void EnterTargettingMode(LayerMask? incomingLayerMask)
        {
            raycaster.eventMask = incomingLayerMask ?? defaultEventMask;
            if (worldRaycaster != null)
                worldRaycaster.eventMask = incomingLayerMask ?? defaultWorldEventMask;
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
            if (players == null || startIndex < 0 || startIndex >= players.Count || players[startIndex] == null) return;
            curentTurnIndex = startIndex;
            currentTurnActor = players[curentTurnIndex];
            foreach (var actor in players) if (actor != null) actor.isMyTurn = actor == currentTurnActor;
            TurnChanged?.Invoke(currentTurnActor);
            currentTurnActor.StartTurn();
        }

        public void EndTurn()
        {
            TryEndTurn(currentTurnActor);
        }

        public bool TryEndTurn(AbstractActor actor)
        {
            if (CardActionInProgress || actor == null || actor != currentTurnActor || players == null || players.Count == 0) return false;
            actor.OnTurnEnd();
            actor.isMyTurn = false;
            if (player != null) player.UpdateSelectedCard(null);
            int living = 0;
            foreach (var candidate in players)
                if (candidate != null && candidate.isActiveAndEnabled && candidate.Health > 0) living++;
            if (living < 2)
            {
                currentTurnActor = null;
                curentTurnIndex = -1;
                TurnChanged?.Invoke(null);
                return true;
            }
            for (int i = 0; i < players.Count; i++)
            {
                curentTurnIndex = (curentTurnIndex + 1) % players.Count;
                var next = players[curentTurnIndex];
                if (next == null || !next.isActiveAndEnabled || next.Health <= 0) continue;
                currentTurnActor = next;
                next.isMyTurn = true;
                TurnChanged?.Invoke(next);
                next.StartTurn();
                return true;
            }
            return false;
        }
    }
}
