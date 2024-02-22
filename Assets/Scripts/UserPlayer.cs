namespace Game
{
    using Game.ScriptableObjects.Card;
    using System;
    using System.Collections.Generic;
    using UnityEngine;

    public class UserPlayer : ActorAndPlayers.Player
    {
        [SerializeField] List<CardTypeScriptableObject> currentLoadout;
        private void Start()
        {
            GameHandler.instance.OnGameSystemsInitialized += Initialize;
        }
        private void OnDestroy()
        {
            GameHandler.instance.OnGameSystemsInitialized -= Initialize;
        }

        private void Initialize()
        {
            Initialize(Guid.NewGuid().ToString(),GameHandler.instance.mainCamera, currentLoadout);
            InitializeCards();
        }
    }
}