using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using UnityStandardAssets.Characters.FirstPerson;

public class PlayerSetup : NetworkBehaviour {

    [SyncVar(hook = nameof(SyncPlayerName))] private string playerName;

    public GameManager game_manager;
    public LobbyController lobby_controller;

    [SerializeField] Camera[] cameras;
    [SerializeField] AudioListener listener;
    [SerializeField] Shooting shooting_script;
    [SerializeField] FirstPersonController fpc_script;
    [SerializeField] PlayerStatus player_status;
    List<GameObject> modules = new List<GameObject>();
    [SerializeField] PlayerAnimations player_animations;

    public GameObject[] weapon_bases;

    public int current_mag_capacity = 0;
    public int current_battery_capacity = 0;
    public float current_weight = 0.0f;
    public float current_deviation = 0.0f;
    public float current_shottime_delta = 0.0f;
    public int current_destruction_rate = 0;

    public int current_round_count_1stweapon = 0, current_battery_charge_1stweapon = 0, current_strength_1stweapon = 0;
    public int current_round_count_2ndweapon = 0, current_battery_charge_2ndweapon = 0, current_strength_2ndweapon = 0;
    public string current_weapon_slot = "";
    [SyncVar(hook = nameof(SyncCurrentWeaponId))] string currentWeaponId = "";

    public bool runningIsAvailable = true;
    public bool walkingJumpingIsAvailable = true;
    public bool reloadingAimingIsAvailable = true;
    public bool firingIsAvailable = true;

    public bool pause = false;
    public bool inventory = false;
    public bool endMatch = false;
    GameObject pause_gui, inventory_gui, dragItemLabel, dislocationLabel, matchResults;
    bool isInit = false;

    void Start() {
        foreach (Module moduleComponent in transform.GetComponentsInChildren<Module>(true)) modules.Add(moduleComponent.gameObject);

        if (!isLocalPlayer) {
            foreach (Behaviour camera in cameras) camera.enabled = false;
            listener.enabled = false;
        }
    }

    void Update() {
        if (!isInit && isLocalPlayer) {
            isInit = true;

            while (!game_manager) game_manager = GameObject.Find("GameManager").GetComponent<GameManager>();
            while (!pause_gui) pause_gui = GameObject.Find("Canvas/Game/Pause");
            while (!inventory_gui) inventory_gui = GameObject.Find("Canvas/Game/Inventory");
            while (!dragItemLabel) dragItemLabel = GameObject.Find("Canvas/DragItemLabel");
            while (!dislocationLabel) dislocationLabel = GameObject.Find("Canvas/Dislocation");
            while (!matchResults) matchResults = GameObject.Find("Canvas/Game/MatchResults");

            lobby_controller = game_manager.gameObject.GetComponent<LobbyController>();
            foreach (NetworkLobbyPlayer player in game_manager.network_manager.lobbySlots) {
                if (player && player.gameObject.name == game_manager.current_settings.userName) {
                    lobby_controller.localLobbyPlayer = player.gameObject.GetComponent<LobbyPlayerSetup>();
                    break;
                }
            }
            int position = lobby_controller.localLobbyPlayer.GetLobbyPosition;
            gameObject.GetComponent<CharacterController>().enabled = false;
            if (lobby_controller.localLobbyPlayer.GetPlaymode == "Grand battle")
                transform.position = game_manager.GetComponent<LobbyController>().singleSpawners[position].position;
            else transform.position = game_manager.GetComponent<LobbyController>().teamSpawners[position].position;
            gameObject.GetComponent<CharacterController>().enabled = true;
            player_status.position = position;
            player_status.playmode = lobby_controller.localLobbyPlayer.GetPlaymode;
            if (isServer && (lobby_controller.localLobbyPlayer.GetPlaymode == "Capturing flag" || lobby_controller.localLobbyPlayer.GetPlaymode == "Capturing flags"))
                CmdSpawnFlags(lobby_controller.localLobbyPlayer.GetPlaymode);

            dragItemLabel.SetActive(false);
            pause_gui.SetActive(false);
            inventory_gui.SetActive(false);
            matchResults.SetActive(false);
            game_manager.localPlayer = gameObject;
            UpdateWeapon(current_weapon_slot);
            WeaponIdManip("first", "r");
            WeaponIdManip("second", "r");
            CmdSetName(game_manager.current_settings.userName);
        }
        gameObject.name = GetPlayerName + " (" + gameObject.GetComponent<NetworkIdentity>().netId + ")";

        if (isLocalPlayer) {
            if (Input.GetKeyDown(KeyCode.Alpha1) && current_weapon_slot != "first") {
                current_weapon_slot = "first";
                UpdateWeapon(current_weapon_slot);
            } else if (Input.GetKeyDown(KeyCode.Alpha2) && current_weapon_slot != "second") {
                current_weapon_slot = "second";
                UpdateWeapon(current_weapon_slot);
            }

            fpc_script.runningIsAvailable = runningIsAvailable;
            fpc_script.walkingJumpingIsAvailable = walkingJumpingIsAvailable;

            if (pause_gui && inventory_gui) {
                if (pause && !pause_gui.activeSelf) pause = false;
                if (inventory && !inventory_gui.activeSelf) inventory = false;
                if (Input.GetKeyDown(KeyCode.I) && !pause && player_status.GetStatus != "dead" && !endMatch) {
                    if (!inventory) {
                        foreach (Behaviour camera in cameras) camera.enabled = false;
                        game_manager.GetComponent<Inventory>().UpdateInventoryGUI();
                        inventory = true;
                    } else InventoryExit();
                }
                if (Input.GetKeyDown(KeyCode.Escape) && !inventory && !endMatch) pause = !pause;
                if (Input.GetKeyDown(KeyCode.Escape) && inventory) InventoryExit();
                if (pause) pause_gui.SetActive(true);
                else pause_gui.SetActive(false);
                if (inventory) inventory_gui.SetActive(true);
                else inventory_gui.SetActive(false);
            }

            if (pause || inventory || endMatch) {
                fpc_script.block = true;
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            } else {
                fpc_script.block = false;
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
            game_manager.inventory_cam.SetActive(inventory);
            fpc_script.ammunitionLoad = game_manager.gameObject.GetComponent<Inventory>().OverWeight();
        }
    }

    public void InventoryExit() {
        foreach (Behaviour camera in cameras) camera.enabled = true;
        UpdateWeapon(current_weapon_slot);
        inventory = false;
    }

    void OnTriggerEnter(Collider other) {
        if (isLocalPlayer && other.gameObject.tag == "Dislocation" && dislocationLabel) dislocationLabel.GetComponent<Text>().text = other.gameObject.name;
        else if (isLocalPlayer && other.gameObject.tag == "DroppedItem" && player_status.GetStatus != "dead") dragItemLabel.SetActive(true);
    }

    void OnTriggerStay(Collider other) {
        if (!isLocalPlayer || player_status.GetStatus == "dead") return;
        if (other.gameObject.tag == "NeutralFlag" || other.gameObject.tag == "Team1Flag" || other.gameObject.tag == "Team2Flag" || other.gameObject.tag == "DroppedItem") {
            dragItemLabel.SetActive(true);
            if (!Input.GetKeyDown(KeyCode.F)) return;
        } else return;
        dragItemLabel.SetActive(false);
        if (other.gameObject.tag == "NeutralFlag") {
            player_status.DragFlag("neutral flag");
            LocalDragItem(other.gameObject);
        } else if (((other.gameObject.tag == "Team1Flag" && player_status.GetTeam == 1) || (other.gameObject.tag == "Team2Flag" && player_status.GetTeam == 2)) && player_status.GetPlayProgress == "without flag") {
            player_status.DragFlag("allied flag");
            LocalDragItem(other.gameObject);
        } else if (((other.gameObject.tag == "Team1Flag" && player_status.GetTeam != 1) || (other.gameObject.tag == "Team2Flag" && player_status.GetTeam != 2)) && player_status.GetPlayProgress == "without flag") {
            player_status.DragFlag("enemy flag");
            LocalDragItem(other.gameObject);
        } else if (other.gameObject.tag == "DroppedItem") {
            if (other.gameObject.name.Contains("Base")) {
                foreach (GameObject weapon_base in weapon_bases) {
                    if (weapon_base.name == other.name.Substring(0, other.name.IndexOf('{')) &&
                        weapon_base.GetComponent<Module>().category == "Main weapon" && game_manager.current_data.first_weapon == "") {
                        game_manager.current_data.first_weapon = other.gameObject.name;
                        LocalDragItem(other.gameObject);
                        UpdateWeapon(current_weapon_slot);
                        WeaponIdManip("first", "r");
                        return;
                    } else if (weapon_base.name == other.name.Substring(0, other.name.IndexOf('{')) &&
                        weapon_base.GetComponent<Module>().category == "Secondary weapon" && game_manager.current_data.second_weapon == "") {
                        game_manager.current_data.second_weapon = other.gameObject.name;
                        LocalDragItem(other.gameObject);
                        UpdateWeapon(current_weapon_slot);
                        WeaponIdManip("second", "r");
                        return;
                    }
                }
            } else {
                game_manager.current_data.items.Add(other.gameObject.name);
                LocalDragItem(other.gameObject);
            }
        }
    }

    void OnTriggerExit(Collider other) {
        if (isLocalPlayer && other.gameObject.tag == "DroppedItem") dragItemLabel.SetActive(false);
    }

    public bool MagReloadCheck() {
        return !(GetCurrentWeaponId == "" || player_status.GetStatus == "dead") && game_manager.current_data.rounds > 0 && !inventory && !pause && !endMatch;
    }

    public bool BatteryReloadCheck() {
        return !(GetCurrentWeaponId == "" || player_status.GetStatus == "dead") && !inventory && !pause && !endMatch;
    }

    public void MagReload() {
        if (current_weapon_slot == "first") {
            if (game_manager.current_data.rounds >= (current_mag_capacity - current_round_count_1stweapon)) {
                game_manager.current_data.rounds -= current_mag_capacity - current_round_count_1stweapon;
                current_round_count_1stweapon += current_mag_capacity - current_round_count_1stweapon;
            } else {
                current_round_count_1stweapon += game_manager.current_data.rounds;
                game_manager.current_data.rounds = 0;
            }
        }
        if (current_weapon_slot == "second") {
            if (game_manager.current_data.rounds >= (current_mag_capacity - current_round_count_2ndweapon)) {
                game_manager.current_data.rounds -= current_mag_capacity - current_round_count_2ndweapon;
                current_round_count_2ndweapon += current_mag_capacity - current_round_count_2ndweapon;
            } else {
                current_round_count_2ndweapon += game_manager.current_data.rounds;
                game_manager.current_data.rounds = 0;
            }
        }
        shooting_script.isJammed = false;
    }

    public void BatteryReload() {
        if (current_weapon_slot == "first") current_battery_charge_1stweapon = current_battery_capacity;
        if (current_weapon_slot == "second") current_battery_charge_2ndweapon = current_battery_capacity;
    }

    public void UpdateWeapon(string new_current_weapon) {
        string weapon_id = "";
        if (new_current_weapon == "first") weapon_id = game_manager.current_data.first_weapon;
        if (new_current_weapon == "second") weapon_id = game_manager.current_data.second_weapon;
        CmdSetCurrentWeaponId(weapon_id);
    }

    void ViewWeapon(string id) {
        for (int i = 0; i < modules.Count; i++) modules[i].SetActive(false);
        if (id == "") {
            current_weapon_slot = "";
            return;
        }
        string weapon_name = id.Substring(0, id.IndexOf('{'));
        for (int i = 0; i < weapon_bases.Length; i++) {
            if (weapon_bases[i].name == weapon_name) {
                weapon_bases[i].SetActive(true);
                shooting_script.UpdateBallsSpawner(weapon_bases[i].transform.GetChild(0).transform);
                Module.Specifications specifications = weapon_bases[i].GetComponent<Module>().BuildWeapon(id);
                current_mag_capacity = specifications.mag_capacity;
                current_battery_capacity = specifications.battery_capacity;
                current_weight = specifications.weight;
                current_deviation = specifications.deviation;
                current_shottime_delta = specifications.shottime_delta;
                current_destruction_rate = specifications.destruction_rate;
                //weapon_bases[i].GetComponent<Module>().Apply(id.Substring(id.IndexOf("{") + 1, id.Length - 2 - id.IndexOf("{")), this);
                break;
            }
        }
    }

    public void WeaponIdManip(string slot, string manipMode) {
        if (manipMode == "w") {
            if (slot == "first" && game_manager.current_data.first_weapon != "") {
                game_manager.current_data.first_weapon = game_manager.current_data.first_weapon.Split()[0] + " " +
                    current_strength_1stweapon.ToString() + " " + current_round_count_1stweapon.ToString() + " " + current_battery_charge_1stweapon.ToString();
                current_strength_1stweapon = 0; current_round_count_1stweapon = 0; current_battery_charge_1stweapon = 0;
            } else if (slot == "second" && game_manager.current_data.second_weapon != "") {
                game_manager.current_data.second_weapon = game_manager.current_data.second_weapon.Split()[0] + " " +
                    current_strength_2ndweapon.ToString() + " " + current_round_count_2ndweapon.ToString() + " " + current_battery_charge_2ndweapon.ToString();
                current_strength_2ndweapon = 0; current_round_count_2ndweapon = 0; current_battery_charge_2ndweapon = 0;
            }
        } else if (manipMode == "r") {
            if (slot == "first" && game_manager.current_data.first_weapon != "") {
                current_strength_1stweapon = int.Parse(game_manager.current_data.first_weapon.Split()[1]);
                current_round_count_1stweapon = int.Parse(game_manager.current_data.first_weapon.Split()[2]);
                current_battery_charge_1stweapon = int.Parse(game_manager.current_data.first_weapon.Split()[3]);
            } else  if (slot == "second" && game_manager.current_data.second_weapon != "") {
                current_strength_2ndweapon = int.Parse(game_manager.current_data.second_weapon.Split()[1]);
                current_round_count_2ndweapon = int.Parse(game_manager.current_data.second_weapon.Split()[2]);
                current_battery_charge_2ndweapon = int.Parse(game_manager.current_data.second_weapon.Split()[3]);
            }
        }
    }

    [Command]
    void CmdSpawnFlags(string playmode) {
        GameObject flag;
        if (playmode == "Capturing flag") {
            flag = Instantiate(player_status.neutralFlagPref, lobby_controller.neutralFlagSpawner.position, Quaternion.identity);
            if (flag) NetworkServer.Spawn(flag);
        } else {
            flag = Instantiate(player_status.team1FlagPref, lobby_controller.team1ItemSpawner.position, Quaternion.identity);
            if (flag) NetworkServer.Spawn(flag);
            flag = Instantiate(player_status.team2FlagPref, lobby_controller.team2ItemSpawner.position, Quaternion.identity);
            if (flag) NetworkServer.Spawn(flag);
        }
    }

    public void MatchResults(bool isVictory, int match_earnings, int kills) {
        if (inventory) InventoryExit();
        pause = false;
        endMatch = true;
        matchResults.SetActive(true);
        if (isVictory) matchResults.transform.GetChild(0).GetChild(0).gameObject.GetComponent<Text>().text = "VICTORY!";
        else matchResults.transform.GetChild(0).GetChild(0).gameObject.GetComponent<Text>().text = "DEFEAT!";
        matchResults.transform.GetChild(0).GetChild(1).gameObject.GetComponent<Text>().text = "Match earnings: " + match_earnings.ToString() + " $";
        matchResults.transform.GetChild(0).GetChild(2).gameObject.GetComponent<Text>().text = "Kills: " + kills.ToString();
        game_manager.current_data.money += match_earnings;
    }

    void OnDestroy() {
        if (!isLocalPlayer) return;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        GameManager.UnregisterPlayer(gameObject.name);
        WeaponIdManip("first", "w");
        WeaponIdManip("second", "w");
        game_manager.SaveUserData();
    }

    //=================================================================================================================================
    // Drag / Drop предметов
    void LocalDragItem(GameObject item) { CmdDragItem(item); }
    [Command] void CmdDragItem(GameObject item) { NetworkServer.Destroy(item); }

    [Client] public void LocalDrop(string current_selected_item_id) { CmdSpawnDroppedItem(current_selected_item_id); }
    [Command] void CmdSpawnDroppedItem(string current_selected_item_id) {
        GameObject item = Instantiate(Resources.Load<GameObject>("DropableItem"), transform.position, Quaternion.identity);
        if (item) NetworkServer.Spawn(item);
        RpcDropItem(item, current_selected_item_id);
    }
    [ClientRpc] void RpcDropItem(GameObject item, string current_selected_item_id) {
        item.name = current_selected_item_id;
        for (int i = 0; i < item.transform.childCount; i++) {
            GameObject module = item.transform.GetChild(i).gameObject;
            if ((current_selected_item_id.IndexOf('{') != -1 && module.name == current_selected_item_id.Substring(0, current_selected_item_id.IndexOf('{'))) ||
                module.name == current_selected_item_id) {
                module.SetActive(true);
                item.GetComponent<BoxCollider>().size = module.GetComponent<BoxCollider>().size;
                item.GetComponent<BoxCollider>().center = module.GetComponent<BoxCollider>().center;
                module.GetComponent<Module>().BuildWeapon(current_selected_item_id, current_selected_item_id.IndexOf('{') != -1);
                //module.GetComponent<Module>().Apply(current_selected_item_id.Substring(current_selected_item_id.IndexOf("{") + 1, current_selected_item_id.Length - 2 - current_selected_item_id.IndexOf("{")));
                break;
            }
        }
    }

    //=================================================================================================================================
    // "Интерфейс" управления SyncVar-переменными
    [Command] void CmdSetName(string new_name) { SyncPlayerName(new_name); }
    void SyncPlayerName(string newName) {
        playerName = newName;
        gameObject.name = newName + " (" + gameObject.GetComponent<NetworkIdentity>().netId + ")";
        GameManager.RegisterPlayer(gameObject.name, gameObject);
    }
    public string GetPlayerName => playerName;

    [Command] void CmdSetCurrentWeaponId(string new_weapon_id) { SyncCurrentWeaponId(new_weapon_id); }
    private void SyncCurrentWeaponId(string newName) {
        currentWeaponId = newName;
        ViewWeapon(newName);
        if (isLocalPlayer) player_animations.ChangeWeaponAnimationData(newName);
    }
    public string GetCurrentWeaponId => currentWeaponId;
}
