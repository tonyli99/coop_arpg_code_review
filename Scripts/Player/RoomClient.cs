namespace Game
{
    using UnityEngine;
    using UnityEngine.SceneManagement;
    using Mirror;

    /// <summary>
    /// A client in a room where a game is being set up.
    /// After a player in the lobby chooses a room to join, they end up in the
    /// room as a client with a RoomClient component in the room.
    /// </summary>
    public class RoomClient : NetworkRoomPlayer
    {

        [SyncVar] public string playerName;

        static readonly ILogger logger = LogFactory.GetLogger(typeof(RoomClient));

        public override void OnStartClient()
        {
            if (logger.LogEnabled()) logger.LogFormat(LogType.Log, "OnStartClient {0}", SceneManager.GetActiveScene().path);

            base.OnStartClient();

            if (isLocalPlayer)
            {
                playerName = PlayerPrefs.GetString(PlayerPrefsKeys.PlayerName, "Player " + (index + 1));
                CmdChangePlayerName(playerName);
            }
        }

        public override void OnClientEnterRoom()
        {
            if (logger.LogEnabled()) logger.LogFormat(LogType.Log, "OnClientEnterRoom {0}", SceneManager.GetActiveScene().path);
        }

        public override void OnClientExitRoom()
        {
            if (logger.LogEnabled()) logger.LogFormat(LogType.Log, "OnClientExitRoom {0}", SceneManager.GetActiveScene().path);

            if (isLocalPlayer)
            {
                PlayerPrefs.SetString(PlayerPrefsKeys.PlayerName, playerName);
            }
        }

        public override void ReadyStateChanged(bool oldReadyState, bool newReadyState)
        {
            if (logger.LogEnabled()) logger.LogFormat(LogType.Log, "ReadyStateChanged {0}", newReadyState);
        }

        #region Commands

        [Command]
        public void CmdChangePlayerName(string newName)
        {
            if (!isLocalPlayer)
            {
                playerName = newName;
            }
        }

        #endregion

        public override void OnGUI()
        {
            base.OnGUI();

            if (!showRoomGUI) return;
            NetworkRoomManager room = NetworkManager.singleton as NetworkRoomManager;
            if (!room) return;
            if (!room.showRoomGUI) return;
            if (!NetworkManager.IsSceneActive(room.RoomScene)) return;

            GUILayout.BeginArea(new Rect(20f + (index * 100), 270f, 90f, 30f));
            if (NetworkClient.active && isLocalPlayer)
            {
                var newName = GUILayout.TextField(playerName);
                if (newName != playerName)
                {
                    playerName = newName;
                    CmdChangePlayerName(newName);
                }
            }
            else
            {
                GUI.enabled = false;
                GUILayout.TextField(playerName);
                GUI.enabled = true;
            }
            GUILayout.EndArea();
        }
    }
}
