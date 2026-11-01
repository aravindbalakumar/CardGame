
using System;
using UnityEngine.EventSystems;

namespace Game.ActorAndPlayers
{
    using Game.ScriptableObjects.Card;
    using Game.DataStructures;
    using System.Collections.Generic;
    using UnityEngine;
    using System.Linq;
    [System.Serializable]
    public class Player2D : AbstractActor, IPointerDownHandler
    {
        public Collider2D collider;
        int loadOutIndex = 0, maxLimit = 0;
        
        Card selectedCard;
        public Card SelectedCard => selectedCard;
        bool isCardSelected = false;
        [SerializeField] Card cardPrefab;
        // [SerializeField] Transform cardParent;
        [SerializeField] private CardHandLayout handLayoutHandler;
        [SerializeField] List<CardTypeScriptableObject> loadout;
        [SerializeField] public List<CardTypeScriptableObject> currentHand;

        public List<CardTypeScriptableObject> CurrentHand
        {
            get { return currentHand; }
        }
        public  override void Initialize()
        {
            base.Initialize();
            if (loadout == null)
            {
                Debug.LogError("Loadout is null");
                return;
            }
            InitializeCards();
            // GameHandler.instance.inputHandler.OnRaycastHit.AddListener(  OnRaycastHit);
            // playerOverUI.LoadStausUI(this, worldCamera, new Vector3(this.transform.position.x, (collider.bounds.max.y + 1.15f)));
        }

        public void OnDestroy()
        {
            // GameHandler.instance.inputHandler.OnRaycastHit.RemoveListener( OnRaycastHit);
        }

        public void OnRaycastHit(GameObject hitObject)
        {
            AbstractActor abstractActor = hitObject.GetComponent<AbstractActor>();
            if (abstractActor == null)
            {
                return;
            }

            GameHandler.instance.ProcessCardAction(this,selectedCard.cardData);
        }

        public void InitializeCards()
        {
            currentHand = new List<CardTypeScriptableObject>();
            maxLimit = loadout.Count;
            for (int i = 0; i < Defaults.HANDCARDLIMIT; i++)
            {
                currentHand.Add(loadout[loadOutIndex]);
                Card card = Instantiate(cardPrefab);
                handLayoutHandler.AddCard(card.RectTransform);
                card.InitializeCard(loadout[loadOutIndex]);
                loadOutIndex = loadOutIndex < maxLimit - 1 ? loadOutIndex + 1 : 0;
            }
        }
        public void OnPointerDown(PointerEventData eventData)
        {
            Debug.Log("ENTERIN GHERE");
            if (GameHandler.instance.player.SelectedCard != null)
            {
                if(GameHandler.instance.player.SelectedCard.cardTargetGroup.Contains(this.gameObject.layer))
                {
                GameHandler.OnAbstractActorClicked.Invoke(this);
                }
            }
        }

        public override void OnActorClicked(AbstractActor actor)
        {
            base.OnActorClicked(actor);
            Debug.Log("ACTOR IS CLICKED");
        }

        public override void UpdateVisualSystem(LayerMask? mask)
        {
            base.UpdateVisualSystem(mask);
            if (mask.HasValue)
            {
                if (mask.Value.Contains(this.gameObject))
                {
                    outline.enabled = true;
                }
                else
                {
                    outline.enabled = false;
                }
            }
            else
            {
                outline.enabled = false;
            }
        }

        public void UpdateSelectedCard(Card selectedCard)
        {
            if (this.selectedCard != null)
            {
                this.selectedCard.DeselectCard();
            }
            GameHandler.instance.EnterTargettingMode(null);
            if (selectedCard == this.selectedCard)
            {
                this.selectedCard = null;
                return;
            }
            else
            {
                this.selectedCard = selectedCard;
                GameHandler.instance.EnterTargettingMode(selectedCard.cardTargetGroup);
                // GameHandler.instance.inputHandler.shouldTrackInput = true;
            }
        }
    }
    }

