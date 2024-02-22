namespace Game.ActorAndPlayers
{
    using TMPro;
    using UnityEngine;
    using UnityEngine.UI;

    public class PlayerStatusOverheadUI : MonoBehaviour
    {
        Player player;
        [SerializeField] RectTransform rectTransform;
        [SerializeField] Canvas threeDCanvas;
        [SerializeField] Slider healthBar;
        [SerializeField] TextMeshProUGUI playerNameText;
        [SerializeField] TextMeshProUGUI healthText;
        [SerializeField] TextMeshProUGUI armorCountText;

        public void LoadStausUI(Player player, Camera worldCamera,Vector3 worldPosition)
        {
            this.player = player;
            //this.transform.position = worldPosition;
            healthBar.maxValue = this.player.MaxHealth;
            healthBar.minValue = 0;
            this.threeDCanvas.worldCamera = worldCamera;
            this.threeDCanvas.transform.position = worldPosition;
            healthBar.value = this.player.Health;
            playerNameText.text = player.name;
            armorCountText.text = player.Armor.ToString();
            healthBar.onValueChanged.AddListener(OnSliderUpdate);
            player.OnPlayerStatusUpdate += StatusUpdate;
        }

        private void StatusUpdate(bool alive, int health, int armor)
        {
            healthBar.value = health;
            playerNameText.text = alive ? player.name : $"<s>{player.name}</s>";
            armorCountText.text = armor.ToString();
        }

        private void OnSliderUpdate(float incomingValue) { healthText.text = $"{(int)incomingValue}/{(int)healthBar.maxValue}"; }

        private void ResetComponent()
        {
            healthBar.onValueChanged.RemoveListener(OnSliderUpdate);
            healthBar.onValueChanged.RemoveAllListeners();
            player.OnPlayerStatusUpdate -= StatusUpdate;
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
