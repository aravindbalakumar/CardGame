
namespace Game.ActorAndPlayers
{
    using Game.ScriptableObjects.Card;
    using Game.DataStructures;
    using System.Collections.Generic;
    using UnityEngine;
    using Engine;
    [System.Serializable]
    public class Player : MonoBehaviour, IActor
    {
        public System.Action<bool, int, int> OnPlayerStatusUpdate;
        public string playerID
        {
            get
            {
                return metaData.ID;
            }
            private set { }
        }
        public string Name
        {
            get
            {
                return metaData.name;
            }
            private set { }
        }
        public int MaxHealth
        {
            get { return stats.MaxHealth; }
            private set { }
        }
        public int Health
        {
            get
            {
                return stats.Health;
            }
            protected set
            {
                if (value > stats.MaxHealth)
                {
                    stats.Health = stats.MaxHealth;
                }
                else
                {
                    if (value <= 0)
                    {
                        Die();
                    }
                    stats.Health = value;
                }
                OnPlayerStatusUpdate?.Invoke(alive, Health, Armor);
            }
        }
        public int Armor
        {
            get
            {
                return stats.Armor;
            }
            protected set
            {
                if (value > stats.MaxArmor)
                {
                    stats.Armor = stats.MaxArmor;
                }
                else
                {
                    if (value <= 0)
                    {
                        Die();
                    }
                    stats.Armor = value;
                }
                OnPlayerStatusUpdate?.Invoke(alive, Health, Armor);
            }
        }


        int loadOutIndex = 0, maxLimit = 0;
        List<StatusEffects> statusEffects;
        List<CardTypeScriptableObject> loadout;
        bool alive;

        [Header("Inspector References")]
        [SerializeReference] PlayerStatusOverheadUI playerOverUI;
        [SerializeField] private Outline outline;
        public Collider _collider;
        [Header("Player Values")]
        public List<CardTypeScriptableObject> currentHand;
        [SerializeField] PlayerMetaData metaData;
        [SerializeField] PlayerStats stats;
        public bool intialized = false;
        public virtual void Die() { alive = false; }

        private void OnDestroy() { GameHandler.instance.inputHandler.OnRaycastHit.RemoveListener(OnRaycastHit); }
        private void OnDisable() { GameHandler.instance.inputHandler.OnRaycastHit.RemoveListener(OnRaycastHit); }

        public void Initialize(string uniquePlayerID, Camera worldCamera, List<CardTypeScriptableObject> loadout = null)
        {
            metaData.ID = uniquePlayerID;
            alive = true;
            if (loadout != null)
            {
                this.loadout = loadout;
            }
            playerOverUI.LoadStausUI(this, worldCamera,  new Vector3(this.transform.position.x,(_collider.bounds.max.y + 1.15f)));
            GameHandler.instance.inputHandler.OnRaycastHit.AddListener(OnRaycastHit);
        }
        public void TakeDamage(int incomingDamage)
        {
            int armorAbsorbtion = Mathf.RoundToInt(incomingDamage * .65f);
            int rawDamage = Mathf.RoundToInt(incomingDamage * .35f);
            int damage = 0;
            if (stats.Armor < armorAbsorbtion)
            {
                damage = (armorAbsorbtion - stats.Armor) + rawDamage;
            }
            {
                damage = rawDamage;
            }
            Health = Health - damage;
        }
        public void Heal(int healAmount) { Health = Health + healAmount; }
        public void AddArmor(int armorAmount) { Armor = Armor + armorAmount; }
        public void InitializeCards()
        {
            currentHand = new List<CardTypeScriptableObject>();
            maxLimit = loadout.Count;
            for (int i = 0; i < Defaults.HANDCARDLIMIT; i++)
            {
                currentHand.Add(loadout[loadOutIndex]);
                loadOutIndex = loadOutIndex < maxLimit - 1 ? loadOutIndex + 1 : 0;
            }
        }
        public void Highlight(bool incomingBool)
        {
            if (outline != null)
            {
                outline.enabled = incomingBool;
            }
        }
        protected virtual void OnRaycastHit(GameObject gameObject)
        {
            if (gameObject == this.gameObject)
            {
                OnSelect();
            }
        }
        public virtual void OnSelect()
        {

        }
    }
}
