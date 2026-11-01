

using System;

namespace Game.ActorAndPlayers
{
    using Game.ScriptableObjects.Card;
    using Game.DataStructures;
    using System.Collections.Generic;
    using UnityEngine;
    using System.Linq;
    public abstract class AbstractActor :MonoBehaviour,IActor
    {

        public bool isMyTurn = false;
        public System.Action<bool, int, int> OnPlayerUIUpdate;
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
        public int MaxArmor
        {
            get { return stats.MaxArmor; }
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
                OnPlayerUIUpdate?.Invoke(alive, Health, Armor);
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
                OnPlayerUIUpdate?.Invoke(alive, Health, Armor);
            }
        }

        Dictionary<StatusType, Vector2Int> statusEffects;
      
        bool alive;

        [Header("Inspector References")]
        [SerializeReference] protected PlayerStatusOverheadUI playerOverUI;
        [SerializeField] protected Outline outline;
        [Header("Player Values")]
        [SerializeField] PlayerMetaData metaData;
        [SerializeField] PlayerStats stats;
        public bool intialized = false;
        public virtual void Die() { alive = false; }

        private void Awake()
        {
            GameHandler.OnGameSystemsInitialized += OnMainSystemsInitialized;
            GameHandler.OnGameSystemsClosed += OnMainSystemsClosed;
        }

        private void OnMainSystemsInitialized()
        {
            Initialize();
            GameHandler.OnTargettingModeChanged+=UpdateVisualSystem;
            GameHandler.OnAbstractActorClicked+=OnActorClicked;
        }

        private void OnMainSystemsClosed()
        {
            GameHandler.OnTargettingModeChanged-=UpdateVisualSystem;
            GameHandler.OnAbstractActorClicked-=OnActorClicked;
        }
        private void OnDestroy()
        {
            GameHandler.OnGameSystemsInitialized -= OnMainSystemsInitialized;
            GameHandler.OnGameSystemsClosed -= OnMainSystemsClosed;
           
        }

        public virtual void UpdateVisualSystem(LayerMask? mask)
        {
            
        }  
        public virtual void OnActorClicked(AbstractActor actor)
        {
            
        }
        public virtual void StartTurn(){}

        public virtual void EndTurn()
        {
            GameHandler.instance.EndTurn();
        }
        public virtual void Initialize()
        {
            statusEffects = new Dictionary<StatusType, Vector2Int>();
             
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
        public void ApplyStatus(StatusType statusType, int duration, int value)
        {
            if (statusEffects.ContainsKey(statusType))
            {
                statusEffects[statusType] = new Vector2Int(duration, value);
            }
            else
            {
                statusEffects.Add(statusType, new Vector2Int(duration, value));
            }
        }

        /// <summary>
        /// Called when the player turn ends
        /// </summary>
        public virtual void OnTurnEnd()
        {
            if (statusEffects.Count > 0)
            {
                List<StatusType> keysToPurge = null;
                for (int i = 0; i < statusEffects.Count; i++)
                {
                    var element = statusEffects.ElementAt(i);
                    ProcessStatusEffect(element.Key, element.Value.y);
                    int newCount = element.Value.x - 1;
                    if (newCount > 0)
                    {
                        statusEffects[element.Key] = new Vector2Int(newCount,element.Value.y);
                    }
                    else
                    {
                        if (keysToPurge == null)
                        {
                            keysToPurge = new List<StatusType>();
                        }
                        keysToPurge.Add(element.Key);
                    }
                }
                if (keysToPurge != null && keysToPurge.Count > 0)
                {
                    keysToPurge.ForEach(x => statusEffects.Remove(x));
                }
            }
        }

        /// <summary>
        /// Processes the status effect accordingly
        /// </summary>
        /// <param name="type">the statusType</param>
        /// <param name="value">how much value status is applied</param>
        private void ProcessStatusEffect(StatusType type, int value)
        {
            switch (type)
            {
                case StatusType.RestoreArmor:
                    AddArmor(value);
                    break;
                case StatusType.RestoreHealth:
                    AddHealth(value);
                    break;
                case StatusType.Bleed:
                    AddHealth(-value);
                    break;
                case StatusType.ShatterArmor:
                    AddArmor(-value);
                    break;
            }
        }
        public void AddHealth(int healAmount) { Health = Health + healAmount; }
        public void AddArmor(int armorAmount) { Armor = Armor + armorAmount; }
      
        public void Highlight(bool incomingBool)
        {
            if (outline != null)
            {
                outline.enabled = incomingBool;
            }
        }
    }
    }