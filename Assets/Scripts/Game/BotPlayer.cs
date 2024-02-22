namespace Game
{
    using UnityEngine;
    using Game.ActorAndPlayers;
    public class BotPlayer : Player
    {
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
            if (!this.intialized)
            {
                this.Initialize("BOT", GameHandler.instance.mainCamera, null);
            }
        }
    }
}

