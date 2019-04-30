using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;
using UnityEngine;

public class ARNetPlayer : CaptainsMessPlayer
{

    [SyncVar]
    public bool locationSynced;

    private bool locationSent;
    ARWorldMapController _arWorldMapController;

    public override void OnStartLocalPlayer() {
        base.OnStartLocalPlayer();

        if (!isServer) {
            SendReadyToBeginMessage();//客户端自动准备好
        }

        locationSent = false;
        _arWorldMapController = FindObjectOfType<ARWorldMapController>();
    }

    public void RelocateDevice(byte[] receivedBytes) {//收到新ARMap并设置给自己
        if (!isServer) {//服务器所在设备 不更新
            if (!_arWorldMapController.SerializeFromByteArr(receivedBytes)) return;
        }

        CmdSetLocationSynced();
    }


    [Command]
    public void CmdSetLocationSynced() {

        locationSynced = true;
    }


    [ClientRpc]
    public void RpcOnStartedGame() {

        print("~~~~ startGame deviceID: " + deviceId);
        print(isClient);
    }

    private void Update() {
        if (!isLocalPlayer) return;

        ARNetGameSession gameSession = ARNetGameSession.instance;
        if (!gameSession) return;

        switch (gameSession.gameState) {
        case ARNetGameState.WaitForLocationSync:
            if (isServer && !locationSent) {
                gameSession.CmdSendWorldMap();
                locationSent = true;
            }
            break;
        }
    }

    void OnGUI() {
        if (isLocalPlayer&&isServer) {
            GUILayout.BeginArea(new Rect(0, Screen.height * 0.8f, Screen.width, 100));
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            ARNetGameSession gameSession = ARNetGameSession.instance;
            if (gameSession) {
                if (gameSession.gameState == ARNetGameState.Lobby) {
                    if (networkManager.NumPlayers()>1&&networkManager.NumPlayers() - networkManager.NumReadyPlayers() == 1) {
                        if (GUILayout.Button(IsReady() ? "Not ready" : "Ready", GUILayout.Width(Screen.width * 0.3f), GUILayout.Height(100))) {
                            SendReadyToBeginMessage();//服务端点击按钮准备
                        }
                    }
                }
            }

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }
    }
}
