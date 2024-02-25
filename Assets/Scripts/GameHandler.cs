namespace Game
{
    using System.Collections.Generic;
    using UnityEngine;
    using Game.ActorAndPlayers;
    using Engine;
    using System;
    using System.Collections;
    using Game.DataStructures;

    public class GameHandler : MonoBehaviour
    {
        [SerializeField] Card cardPrefab;
        [SerializeField] Transform cardParent;
        [SerializeField] UserPlayer currentLocalPlayer;
        [SerializeField] List<Player> opponents;
        int playerLayerInt, otherLayerInt;
        Card selectedCard;
        bool isCardSelected = false;
        [Header("Support Systems")]
        public Camera mainCamera;
        [SerializeField] InputHandler _inputHandler;
        #region Events
        public Action OnGameSystemsInitialized;
        #endregion
        public InputHandler inputHandler
        {
            get
            {
                return _inputHandler;
            }
            private set { }
        }

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
            StartCoroutine(InitializeSupportSystems());
            inputHandler.OnRaycastHit.AddListener(OnRaycastHit);
        }
        private void OnDestroy()
        {
            inputHandler.OnRaycastHit.RemoveListener(OnRaycastHit);
        }

        private void OnRaycastHit(GameObject hitObject)
        {
            var playerComp = hitObject.GetComponent<Player>();
            if (playerComp == null && selectedCard == null)
            {
                return;
            }
        }

        private void ProcessTheCard()
        {

        }
        private IEnumerator InitializeSupportSystems()
        {
            yield return new WaitUntil(() => _inputHandler.initialized);
            OnGameSystemsInitialized?.Invoke();
            UpdateCardDeck();
        }

        public void Start()
        {
            playerLayerInt = LayerMask.NameToLayer("Player");
            otherLayerInt = LayerMask.NameToLayer("Enemies");
        }

        public void UpdateCardDeck()
        {
            foreach (var cardData in currentLocalPlayer.currentHand)
            {
                Card card = Instantiate(cardPrefab, cardParent);
                card.InitializeCard(cardData);
            }
        }

        public void UpdateSelectedCard(Card selectedCard)
        {
            if (this.selectedCard != null)
            {
                this.selectedCard.DeselectCard();
            }
            EnterTargettingMode(null);
            if (selectedCard == this.selectedCard)
            {
                this.selectedCard = null;
                return;
            }
            else
            {
                this.selectedCard = selectedCard;
                EnterTargettingMode(selectedCard.cardTargetGroup);
                isCardSelected = true;
                _inputHandler.shouldTrackInput = isCardSelected;
            }
        }

        private void EnterTargettingMode(TargetGroup? incomingTargetGroup)
        {
            if (incomingTargetGroup == null)
            {
                _inputHandler.layerMask.value &= ~(1 << playerLayerInt);
                _inputHandler.layerMask.value &= ~(1 << otherLayerInt);
                currentLocalPlayer.Highlight(false);
                opponents.ForEach(x => x.Highlight(false));
                return;
            }
            else
            {
                if (incomingTargetGroup.Value.HasFlag(DataStructures.TargetGroup.self))
                {
                    currentLocalPlayer.Highlight(true);
                    _inputHandler.layerMask.value |= (1 << playerLayerInt);
                }
                if (incomingTargetGroup.Value.HasFlag(DataStructures.TargetGroup.others))
                {
                    opponents.ForEach(x => x.Highlight(true));
                    _inputHandler.layerMask.value |= (1 << otherLayerInt);
                }
            }
        }
    }
}
