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
        [SerializeField] Card cardPrefab;
        [SerializeField] Transform cardParent;
        [SerializeField] UserPlayer currentLocalPlayer;
        [SerializeField] List<Player> opponents;
        int playerLayerInt, otherLayerInt;
        Card selectedCard;
        // Read-only view access keeps the parallel Toolkit hand in sync with Canvas selection.
        public Card SelectedCard => selectedCard;
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
            if (playerComp == null || selectedCard == null || selectedCard.cardData == null)
            {
                return;
            }
            ProcessCardAction(playerComp, selectedCard.cardData.cardAction);
            ProcessCardAction(playerComp, selectedCard.cardData.postCardAction);
            currentLocalPlayer.OnTurnEnd();
        }

        public void ProcessCardAction(Player playerComp, CardAction cardAction)
        {

            switch (cardAction.actionType)
            {
                case CardActionType.Attack:
                    playerComp.TakeDamage(cardAction.value);
                    break;
                case CardActionType.StatusEffect:
                    playerComp.ApplyStatus(cardAction.statusType, cardAction.duration, cardAction.value);
                    break;
                case CardActionType.Heal:
                    playerComp.AddHealth(cardAction.value);
                    break;
                case CardActionType.Armor:
                    playerComp.AddArmor(cardAction.value);
                    break;
            }
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
            if (currentLocalPlayer == null || currentLocalPlayer.currentHand == null || cardParent == null || cardPrefab == null) return;
            var existing = new List<Card>(cardParent.GetComponentsInChildren<Card>(true));
            foreach (var cardData in currentLocalPlayer.currentHand)
            {
                if (cardData == null) continue;
                int index = existing.FindIndex(card => card != null && card.cardData == cardData);
                Card card;
                if (index >= 0)
                {
                    card = existing[index];
                    existing.RemoveAt(index);
                }
                else
                {
                    card = Instantiate(cardPrefab, cardParent);
                    card.InitializeCard(cardData);
                }
                card.transform.SetAsLastSibling();
            }
            foreach (var card in existing)
            {
                if (card == null) continue;
                if (card == selectedCard) UpdateSelectedCard(card);
                card.gameObject.SetActive(false);
                Destroy(card.gameObject);
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
                isCardSelected = false;
                _inputHandler.shouldTrackInput = false;
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
                _inputHandler.UpdateRaycastLayer();
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
                _inputHandler.UpdateRaycastLayer();
            }
        }
    }
}
