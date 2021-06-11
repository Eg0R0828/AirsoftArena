using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;

public class LobbyController : MonoBehaviour {

    public bool isMenu = true;

    public GameObject start_button;
    public Dropdown match_mode_dropdown;
    public LobbyPlayerSetup localLobbyPlayer;

    [System.Serializable]
    public class SLOT {
        public GameObject join_button;
        public GameObject name_label;
    }
    [SerializeField] SLOT[] lobby_slots;

    public Transform[] singleSpawners;
    public Transform[] teamSpawners;
    public Transform neutralFlagSpawner;
    public Transform team1ItemSpawner;
    public Transform team2ItemSpawner;

    public void AddPlayerToTeam(int position) {
        if (localLobbyPlayer) localLobbyPlayer.SetLobbyPosition(position);
    }

    public void SetMatchMode() {
        localLobbyPlayer.HostSetPlaymode(match_mode_dropdown.options[match_mode_dropdown.value].text);
    }

    public void UpdateLobbyGUI(NetworkLobbyPlayer[] lobby_players) {
        for (int i = 0; i < lobby_slots.Length; i++) {
            lobby_slots[i].join_button.SetActive(true);
            lobby_slots[i].join_button.GetComponent<Button>().interactable = true;
            lobby_slots[i].name_label.SetActive(false);
        }
        foreach (NetworkLobbyPlayer player in lobby_players) {
            if (!player) continue;
            int position = player.gameObject.GetComponent<LobbyPlayerSetup>().GetLobbyPosition;
            if (position == -1) continue;
            string name = player.gameObject.GetComponent<LobbyPlayerSetup>().GetPlayerName;
            lobby_slots[position].join_button.SetActive(false);
            lobby_slots[position].name_label.SetActive(true);
            lobby_slots[position].name_label.GetComponent<Text>().text = name;
        }
        for (int i = 0; i < lobby_slots.Length; i++) {
            if (localLobbyPlayer.GetLobbyPosition != -1) lobby_slots[i].join_button.GetComponent<Button>().interactable = false;
            else lobby_slots[i].join_button.GetComponent<Button>().interactable = true;
        }
    }
}
