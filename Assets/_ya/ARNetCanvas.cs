using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class ARNetCanvas : CaptainsMessListener {
    public enum NetworkState {
        Init,
        Offline,
        Connecting,
        Connected,
        Disrupted
    };
    [HideInInspector]
    public NetworkState networkState = NetworkState.Init;

    public CaptainsMessNetworkManager networkManager;

    public Text networkStateField;

    public GameObject gameSessionPrefab;
    public ARNetGameSession gameSession;



    public CanvasGroup[] menus;//0:主菜单 1:广播中/链接中 2:已连接

    public void Start() {
        networkState = NetworkState.Offline;

        ClientScene.RegisterPrefab(gameSessionPrefab);
    }

    private void Update() {
        networkStateField.text = networkState.ToString();
        if (networkManager.IsConnected()) {
            ShowMenu(menus[2]);
        } else if (networkManager.IsBroadcasting() || networkManager.IsJoining()) {
            ShowMenu(menus[1]);
        } else {//主菜单
            ShowMenu(menus[0]);
        }
    }

    void ShowMenu(CanvasGroup group) {
        if (group.alpha >= 1 && group.interactable && group.blocksRaycasts) return;
        HideAllMenus();
        group.alpha = 1;
        group.interactable = true;
        group.blocksRaycasts = true;
    }

    void HideAllMenus() {
        foreach(CanvasGroup group in menus) {
            group.alpha = 0;
            group.interactable = false;
            group.blocksRaycasts = false;
        }
    }

    public void StartHost() {
        mess.StartHosting();
    }
    public void StartJoin() {
        mess.StartJoining();
    }
    public void CancelConect() {
        mess.Cancel();
    }



    //监听网络状态
    public override void OnStartConnecting() {
        networkState = NetworkState.Connecting;
    }

    public override void OnStopConnecting() {
        networkState = NetworkState.Offline;
    }

    public override void OnServerCreated() {
        // Create game session
        ExampleGameSession oldSession = FindObjectOfType<ExampleGameSession>();
        if (oldSession == null) {
            GameObject serverSession = Instantiate(gameSessionPrefab);
            NetworkServer.Spawn(serverSession);
        } else {
            Debug.LogError("GameSession already exists!");
        }
    }

    public override void OnJoinedLobby() {
        networkState = NetworkState.Connected;

        gameSession = FindObjectOfType<ARNetGameSession>();
        if (gameSession) {
            gameSession.OnJoinedLobby();
        }
    }

    public override void OnLeftLobby() {
        networkState = NetworkState.Offline;

        gameSession.OnLeftLobby();
    }

    public override void OnCountdownStarted() {
        gameSession.OnCountdownStarted();
    }

    public override void OnCountdownCancelled() {
        gameSession.OnCountdownCancelled();
    }

    public override void OnStartGame(List<CaptainsMessPlayer> aStartingPlayers) {
        Debug.Log("GO!");
        print("~~~~" + gameSession);
        gameSession.OnStartGame(aStartingPlayers);
    }

    public override void OnAbortGame() {
        Debug.Log("ABORT!");
        gameSession.OnAbortGame();
    }

}
