
namespace Game.ActorAndPlayers
{
    using Game.ScriptableObjects.Card;
    using Game.DataStructures;
    using System.Collections.Generic;
    using UnityEngine;
    using System.Linq;
    [System.Serializable]
    public class Player : AbstractActor
    {
        List<CardTypeScriptableObject> loadout;
        public List<CardTypeScriptableObject> currentHand;

        int loadOutIndex = 0, maxLimit = 0;
        public Collider collider;
        public  override void Initialize()
        {
            this.loadout = loadout;
            base.Initialize();
            // playerOverUI.LoadStausUI(this, worldCamera, new Vector3(this.transform.position.x, (collider.bounds.max.y + 1.15f)));
        }
        
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
    }
}
