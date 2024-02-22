
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
        [SerializeField] Image cardIcon;
        [SerializeField] TextMeshProUGUI cardName;
        [SerializeField] TextMeshProUGUI cardEnergy;
        public void InitializeCard(CardTypeScriptableObject cardBehaviourAndData)
        {
            this.cardBehaviourAndData = cardBehaviourAndData;
            cardIcon.overrideSprite = Sprite.Create(this.cardBehaviourAndData.metaData.cardTexture, new Rect(0, 0, this.cardBehaviourAndData.metaData.cardTexture.width, this.cardBehaviourAndData.metaData.cardTexture.height), Vector2.one / 2);
            cardName.text = this.cardBehaviourAndData.metaData.name;
            cardEnergy.text = this.cardBehaviourAndData.energyUsage.ToString();
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
