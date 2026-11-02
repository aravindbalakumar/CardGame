
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
        private Sprite generatedIcon;
        public TargetGroup cardTargetGroup
        {
            get
            {
                return cardBehaviourAndData.targetGroup;
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
        [SerializeField] TextMeshProUGUI cardName;
        [SerializeField] TextMeshProUGUI cardEnergy;
        public void InitializeCard(CardTypeScriptableObject cardBehaviourAndData)
        {
            this.cardBehaviourAndData = cardBehaviourAndData;
            if (generatedIcon != null) Destroy(generatedIcon);
            var texture = cardBehaviourAndData.metaData.cardTexture != null
                ? cardBehaviourAndData.metaData.cardTexture : cardBehaviourAndData.fullCardTexture;
            generatedIcon = texture != null
                ? Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.one / 2) : null;
            cardIcon.overrideSprite = generatedIcon;
            cardName.text = this.cardBehaviourAndData.metaData.name;
            cardEnergy.text = this.cardBehaviourAndData.energyUsage.ToString();
        }
        private void OnDestroy()
        {
            if (generatedIcon != null) Destroy(generatedIcon);
        }

        public void OnCardSelected()
        {
            Debug.Log("CARD IS SELECTED");
            this.transform.localScale = Vector3.one * 1.1f;
            GameHandler.instance.UpdateSelectedCard(this);
        }
        public void DeselectCard()
        {
            Debug.Log("CARD IS DESELECTED");
            this.transform.localScale = Vector3.one;
        }
    }
}
