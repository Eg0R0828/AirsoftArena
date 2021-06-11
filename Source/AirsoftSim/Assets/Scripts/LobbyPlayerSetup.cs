using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class LobbyPlayerSetup : NetworkBehaviour {

    NetworkLobbyManager network_manager;
    GameObject gameManager;
    LobbyController lobby_controller;
    GameManager game_manager;

    [SyncVar(hook = nameof(SyncPlayerName))] string playerName;
    [SyncVar(hook = nameof(SyncLobbyPosition))] int lobbyPosition = -1;
    [SyncVar(hook = nameof(SyncPlaymode))] string playmode = "";

    void Update() {
        if (isLocalPlayer && !network_manager && !gameManager) {
            while (!network_manager) network_manager = GameObject.Find("NetworkManager").GetComponent<NetworkLobbyManager>();
            while (!gameManager) gameManager = GameObject.Find("GameManager");
            game_manager = gameManager.GetComponent<GameManager>();
            lobby_controller = gameManager.GetComponent<LobbyController>();
            CmdUpdateName(game_manager.current_settings.userName);

            if (lobby_controller.isMenu) {
                if (isServer) {
                    lobby_controller.start_button.SetActive(true);
                    lobby_controller.match_mode_dropdown.interactable = true;
                    ClientSetPlaymode(lobby_controller.match_mode_dropdown.options[lobby_controller.match_mode_dropdown.value].text);
                } else {
                    lobby_controller.start_button.SetActive(false);
                    lobby_controller.match_mode_dropdown.interactable = false;
                    foreach (NetworkLobbyPlayer player in network_manager.lobbySlots)
                        if (player && player.GetComponent<LobbyPlayerSetup>().GetPlaymode != "")
                            ClientSetPlaymode(player.GetComponent<LobbyPlayerSetup>().GetPlaymode);
                }
            }
        }

        if (isLocalPlayer && lobby_controller) {
            lobby_controller.localLobbyPlayer = this;
            if (lobby_controller.isMenu) lobby_controller.UpdateLobbyGUI(network_manager.lobbySlots);
        }

        if (GetLobbyPosition != -1) gameObject.GetComponent<NetworkLobbyPlayer>().readyToBegin = true;
        else gameObject.GetComponent<NetworkLobbyPlayer>().readyToBegin = false;
        gameObject.name = GetPlayerName;
    }

    public void SetLobbyPosition(int position) {
        CmdUpdatePosition(position);
    }

    public void HostSetPlaymode(string new_play_mode) {
        foreach (NetworkLobbyPlayer player in network_manager.lobbySlots) if (player)
                player.gameObject.GetComponent<LobbyPlayerSetup>().ClientSetPlaymode(new_play_mode);
    }

    public void ClientSetPlaymode(string new_play_mode) {
        CmdSetPlaymode(new_play_mode);
    }

    //=================================================================================================================================
    // "Интерфейс" управления SyncVar-переменными
    [Command] void CmdUpdateName(string player_name) { SyncPlayerName(player_name); }
    void SyncPlayerName(string newName) { playerName = newName; }
    public string GetPlayerName => playerName;

    [Command] void CmdUpdatePosition(int player_position) { SyncLobbyPosition(player_position); }
    void SyncLobbyPosition(int newName) { lobbyPosition = newName; }
    public int GetLobbyPosition => lobbyPosition;

    [Command] void CmdSetPlaymode(string new_playmode) { SyncPlaymode(new_playmode); }
    void SyncPlaymode(string newName) {
        playmode = newName;
        if (isLocalPlayer) {
            for (int i = 0; i < lobby_controller.match_mode_dropdown.options.Count; i++) {
                if (lobby_controller.match_mode_dropdown.options[i].text == newName) {
                    lobby_controller.match_mode_dropdown.value = i;
                    break;
                }
            }
        }
    }
    public string GetPlaymode => playmode;
}
