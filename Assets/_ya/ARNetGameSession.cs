using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using Unity.Collections;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Runtime.InteropServices;
#if UNITY_IOS
using UnityEngine.XR.ARKit;
#endif

public enum ARNetGameState
{
	Offline,
	Connecting,
	Lobby,
    SendLocationSync,
    WaitForLocationSync,
	Gaming,
	GameOver
}

public class ARNetGameSession : NetworkBehaviour
{
	public Text gameStateField;
	public Text gameRulesField;

	public static ARNetGameSession instance;

	ARNetCanvas networkListener;
	List<ARNetPlayer> players;
	string specialMessage = "";
    NetworkTransmitter _networkTransmitter;
    ARWorldMapController _arWorldMapController;

	[SyncVar]
	public ARNetGameState gameState;

	[SyncVar]
	public string message = "";
    public void OnDestroy()
	{
		if (gameStateField != null) {
			gameStateField.text = "";
			gameStateField.gameObject.SetActive(false);
		}
		if (gameRulesField != null) {
			gameRulesField.gameObject.SetActive(false);
		}

        networkListener.LocalplayerMsg("");
        networkListener.ActiveGameControl(true); 
    }

	[Server]
	public override void OnStartServer()
	{
        networkListener = FindObjectOfType<ARNetCanvas>();      
        _arWorldMapController = FindObjectOfType<ARWorldMapController>();
		gameState = ARNetGameState.Connecting;
	}

	[Server]
	public void OnStartGame(List<CaptainsMessPlayer> aStartingPlayers)
	{
		players = aStartingPlayers.Select(p => p as ARNetPlayer).ToList();

		RpcOnStartedGame();
		foreach (ARNetPlayer p in players) {
			p.RpcOnStartedGame();
		}

		StartCoroutine(RunGame());
	}

	[Server]
	public void OnAbortGame()
	{
		RpcOnAbortedGame();
	}

	[Client]
	public override void OnStartClient()
	{
		if (instance) {
			Debug.LogError("ERROR: Another GameSession!");
		}
		instance = this;

        networkListener = FindObjectOfType<ARNetCanvas>(); 
        networkListener.gameSession = this;

        _networkTransmitter = GetComponent<NetworkTransmitter>();

		if (gameState != ARNetGameState.Lobby) {
			gameState = ARNetGameState.Lobby;
		}

        if (!isServer) {
            networkListener.ActiveGameControl(false);
        }
        
    }         
    [Command]
    public void CmdSendWorldMap() {
        networkListener.LocalplayerMsg("开始发送地图信息");
        print("开始发送地图信息");

        //StopCoroutine("SendWorldMap2");
        StartCoroutine(SendWorldMap2());
    }
    IEnumerator SendWorldMap2() {
        ARWorldMap? arWorldMap = null;
        while(true) {
            yield return StartCoroutine(_arWorldMapController.StartGetCurARWorldMap());
            arWorldMap = _arWorldMapController.GetCurARWorldMap();
            if (arWorldMap != null) break;
            yield return new WaitForSeconds(.3f);//如果读取ARMap失败，0.3秒后重试
        }

        byte[] mapData = arWorldMap.Value.Serialize(Allocator.Temp).ToArray();//序列化ARMap
        //_networkTransmitter.StopCoroutine("SendBytesToClientsRoutine");
        StartCoroutine(_networkTransmitter.SendBytesToClientsRoutine(0, mapData));
    }

    [Client]
    void OnDataCompletelyReceived(int transmissionId, byte[] data) {
        networkListener.LocalplayerMsg("地图信息接收完毕");
        CaptainsMessNetworkManager networkManager = NetworkManager.singleton as CaptainsMessNetworkManager;
        ARNetPlayer p = networkManager.localPlayer as ARNetPlayer;
        print("地图信息接收完毕");
        if (p != null) {
            //byte[] 转为ARWorldMap,AR重新定位寻找
            p.RelocateDevice(data);
        }
    }

    [Client]
    void OnDataFragmentReceived(int transmissionId, byte[] data) {
        //每次接收到部分地图信息
    }

    public void OnJoinedLobby()
	{
		gameState = ARNetGameState.Lobby;
	}

	public void OnLeftLobby()
	{
		gameState = ARNetGameState.Offline;
	}

	public void OnCountdownStarted()
	{
		//gameState = ARNetGameState.Countdown;
	}

	public void OnCountdownCancelled()
	{
		gameState = ARNetGameState.Lobby;
	}

	[Server]
	IEnumerator RunGame()
	{

        gameState = ARNetGameState.WaitForLocationSync;

        while (!AllPlayersHaveSyncedLocation()) {
            yield return new WaitForEndOfFrame();
        }

        print("信息发送完毕");
        networkListener.LocalplayerMsg("所有人都已接收地图信息");

        while (true) {
            yield return new WaitForEndOfFrame();
        }

        // Reset game
        //foreach (ARNetPlayer p in players) {
        //	p.totalPoints = 0;
        //}

        //while (MaxScore() < 3)
        //{
        //	// Reset rolls
        //	foreach (ExamplePlayerScript p in players) {
        //		p.rollResult = 0;
        //	}

        //	//// Wait for all players to roll
        //	//gameState = ARNetGameState.WaitingForRolls;

        //	//while (!AllPlayersHaveRolled()) {
        //	//	yield return null;
        //	//}

        //	//// Award point to winner
        //	//gameState = ARNetGameState.Scoring;

        //	List<ExamplePlayerScript> scoringPlayers = PlayersWithHighestRoll();
        //	if (scoringPlayers.Count == 1)
        //	{
        //		scoringPlayers[0].totalPoints += 1;
        //		specialMessage = scoringPlayers[0].deviceName + " scores 1 point!";
        //	}
        //	else
        //	{
        //		specialMessage = "TIE! No points awarded.";
        //	}

        //	yield return new WaitForSeconds(2);
        //	specialMessage = "";
        //}

        //// Declare winner!
        //specialMessage = PlayerWithHighestScore().deviceName + " WINS!";
        //yield return new WaitForSeconds(3);
        //specialMessage = "";

        //// Game over
        //gameState = ARNetGameState.GameOver;

        //yield break;
	}

    [Server]
    bool AllPlayersHaveSyncedLocation() {
        return players.All(p => p.locationSynced);
    }

    [Server]
	public void PlayAgain()
	{
        StopCoroutine("RunGame");
		StartCoroutine(RunGame());
	}

	void Update()
	{
		if (isServer)
		{
			//if (gameState == ARNetGameState.Countdown)
			//{
			//	message = "Game Starting in " + Mathf.Ceil(networkListener.mess.CountdownTimer()) + "...";
			//}
			//else if (specialMessage != "")
			//{
			//	message = specialMessage;
			//}
			//else
			//{
			//	message = gameState.ToString();
			//}
		}

		gameStateField.text = message;
	}

	// Client RPCs

	[ClientRpc]
	public void RpcOnStartedGame()
	{
        _networkTransmitter = GetComponent<NetworkTransmitter>();
        _networkTransmitter.OnDataCompletelyReceived += OnDataCompletelyReceived;
        _networkTransmitter.OnDataFragmentSent += OnDataFragmentReceived;
        //gameRulesField.gameObject.SetActive(true);
    }




    [ClientRpc]
	public void RpcOnAbortedGame()
	{
        gameRulesField.gameObject.SetActive(false);
	}
}
