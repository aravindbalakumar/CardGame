namespace Game.ActorAndPlayers
{
    using TMPro;
    using UnityEngine;
    using UnityEngine.UI;

    public class PlayerStatusOverheadUI : MonoBehaviour
    {
        AbstractActor actor;
        [SerializeField] RectTransform rectTransform;
        [SerializeField] Canvas threeDCanvas;
        [SerializeField] Slider healthBar;
        [SerializeField] TextMeshProUGUI playerNameText;
        [SerializeField] TextMeshProUGUI healthText;
        // [SerializeField] TextMeshProUGUI armorCountText;

        public void LoadStausUI(AbstractActor actor, Camera worldCamera,Vector3 worldPosition)
        {
            ResetComponent();
            this.actor = actor;
            this.transform.position = worldPosition;
            healthBar.maxValue = this.actor.MaxHealth;
            healthBar.minValue = 0;
            this.threeDCanvas.worldCamera = worldCamera;
            this.threeDCanvas.transform.position = worldPosition;
            healthBar.value = this.actor.Health;
            playerNameText.text = actor.name;
            // armorCountText.text = player.Armor.ToString();
            OnSliderUpdate(healthBar.value);
            if (isActiveAndEnabled) BindActor();
        }

        private void StatusUpdate(bool alive, int health, int armor)
        {
            healthBar.value = health;
            playerNameText.text = alive ? actor.name : $"<s>{actor.name}</s>";
            // armorCountText.text = armor.ToString();
        }

        private void OnSliderUpdate(float incomingValue) { healthText.text = $"{(int)incomingValue}/{(int)healthBar.maxValue}"; }

        private void ResetComponent()
        {
            healthBar.onValueChanged.RemoveListener(OnSliderUpdate);
            if (actor != null) actor.OnPlayerUIUpdate -= StatusUpdate;
        }

        private void BindActor()
        {
            healthBar.onValueChanged.AddListener(OnSliderUpdate);
            actor.OnPlayerUIUpdate += StatusUpdate;
            StatusUpdate(actor.Health > 0, actor.Health, actor.Armor);
            OnSliderUpdate(healthBar.value);
        }

        private void OnEnable()
        {
            if (actor != null) BindActor();
        }

        private void OnDestroy()
        {
            ResetComponent();
        }
        private void OnDisable()
        {
            ResetComponent();
        }
    }
}
