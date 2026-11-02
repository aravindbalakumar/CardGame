namespace Game
{
    using System.Collections.Generic;
    using Game.ScriptableObjects.Card;
    using UnityEngine;

    // Restores the deleted script referenced by the existing legacy Player prefab.
    public class UserPlayer : ActorAndPlayers.Player
    {
        [SerializeField] private List<CardTypeScriptableObject> currentLoadout;

        public override void Initialize()
        {
            loadout = currentLoadout;
            base.Initialize();
            InitializeCards();
        }
    }
}
