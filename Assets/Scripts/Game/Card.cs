
namespace Game
{
    using Game.ScriptableObjects.Card;
    using UnityEngine;
    using UnityEngine.UI;
    using UnityEngine.EventSystems;
    using TMPro;
    using Game.DataStructures;

    public class Card : MonoBehaviour
    {
        CardTypeScriptableObject cardBehaviourAndData;
        public LayerMask cardTargetGroup
        {
            get
            {
                return cardBehaviourAndData.skill.targetGroup;
            }
            private set
            {

            }
        }
        public CardTypeScriptableObject cardData
        {
            get
            {
                return cardBehaviourAndData;
            }
        }
        [SerializeField] Image cardIcon;
        // [SerializeField] TextMeshProUGUI cardName;
        // [SerializeField] TextMeshProUGUI cardEnergy;
        [SerializeField] RectTransform _rectTransform;

        public RectTransform RectTransform
        {
            get
            {
                return _rectTransform;
            }
        }
        public void InitializeCard(CardTypeScriptableObject cardBehaviourAndData)
        {
            this.cardBehaviourAndData = cardBehaviourAndData;
            var texture = cardBehaviourAndData != null ? cardBehaviourAndData.metaData.cardTexture : null;
            cardIcon.overrideSprite = texture != null
                ? Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.one / 2)
                : null;
            // cardName.text = this.cardBehaviourAndData.metaData.name;
            // cardEnergy.text = this.cardBehaviourAndData.energyUsage.ToString();
        }
        public void OnCardSelected()
        {
            if (GameHandler.instance == null || GameHandler.instance.CardActionInProgress) return;
            Debug.Log("CARD IS SELECTED");
            this.transform.localScale = Vector3.one * 1.1f;
            GameHandler.instance.player.UpdateSelectedCard(this);
        }
        public void DeselectCard()
        {
            Debug.Log("CARD IS DESELECTED");
            this.transform.localScale = Vector3.one;
        }
    }
}
