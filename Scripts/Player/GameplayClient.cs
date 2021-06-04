namespace Game
{
    using System.Collections.Generic;
    using UnityEngine;
    using Mirror;

    /// <summary>
    /// A client in the gameplay scene. When the host in a room starts the game, each
    /// player ends up as a client with a GameplayClient in the gameplay scene. The 
    /// host's GameplayClient can have up to four ControllerPlayers, each representing
    /// a player-controlled character in the game, allocated one per gamepad. Remote
    /// clients' GameplayClients can have one ControllerPlayer.
    /// </summary>
    public class GameplayClient : NetworkBehaviour
    {

        [SyncVar] public int index = 0;
        [SyncVar] public string playerName = "Player";
        [SyncVar] public int numPlayers;

        public Character characterPrefab;

        public List<ControllerPlayer> controllerPlayers = new List<ControllerPlayer>();

        public override void OnStartServer()
        {
            base.OnStartServer();
            numPlayers = NetworkServer.connections.Count;
        }

        public override void OnStartClient()
        {
            gameObject.name = playerName;
            var hudCanvas = ServiceLocator.Get<HudCanvas>();
            var hud = hudCanvas.PlayerHuds[index];
            hud.gameObject.SetActive(true);
            hud.PlayerName.text = playerName;
            hud.SetGameplayElementsVisible(false);
        }

        public override void OnStartLocalPlayer()
        {
            AddControllerPlayer(0);
        }

        private void AddControllerPlayer(int rewiredPlayerId)
        {
            var controllerPlayer = new ControllerPlayer(this, controllerPlayers.Count, rewiredPlayerId);
            controllerPlayers.Add(controllerPlayer);
        }

        /// <summary>
        /// Get HUD index of a controller player. Initial controller player 0 of 
        /// each client uses client index. Additional controller players on server
        /// use indices beyond player count.
        /// </summary>
        /// <param name="controllerPlayerIndex"></param>
        /// <returns></returns>
        public int GetHudIndex(int controllerPlayerIndex)
        {
            return (controllerPlayerIndex == 0) ? index : (numPlayers + controllerPlayerIndex - 1);

        }

        [Command]
        public void CmdSpawnCharacter(int controllerPlayerIndex, int bodyIndex, int eyesIndex, int hairIndex, int hairColorIndex, int classIndex)
        {
            var character = GameObject.Instantiate(characterPrefab, transform.position, Quaternion.identity);
            character.owner = gameObject;
            character.controllerPlayerIndex = controllerPlayerIndex;
            character.bodyIndex = bodyIndex;
            character.eyesIndex = eyesIndex;
            character.hairIndex = hairIndex;
            character.hairColorIndex = hairColorIndex;
            character.classIndex = classIndex;
            character.coins = 0;
            character.health = character.maxHealth = Character.StartingHealth(classIndex);
            character.mana = character.maxMana = Character.StartingMana(classIndex);
            NetworkServer.Spawn(character.gameObject, netIdentity.connectionToClient);
        }

        private void Update()
        {
            if (!isLocalPlayer) return;

            controllerPlayers.ForEach(x => x.OnUpdate());

            if (isServer)
            {
                for (int i = 0; i < Rewired.ReInput.players.playerCount; i++) //[TODO] Optimize
                {
                    var rewiredPlayer = Rewired.ReInput.players.GetPlayer(i);
                    if (controllerPlayers.Find(x => x.rewiredPlayer == rewiredPlayer) == null)
                    {
                        if (rewiredPlayer.GetButtonDown(RewiredActions.Use))
                        {
                            AddControllerPlayer(i);
                        }
                    }
                }
            }
        }

    }
}
