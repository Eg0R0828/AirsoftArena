using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System.Runtime.Serialization.Formatters.Binary;

public class GameManager : MonoBehaviour {

    public GameObject localPlayer;

    [SerializeField] public static Dictionary<string, GameObject> players_base = new Dictionary<string, GameObject>();
    public NetworkLobbyManager network_manager;
    [SerializeField] Text address;
    [SerializeField] Text new_user_name;
    [SerializeField] Text saved_user_name;
    [SerializeField] Dropdown qualitySettings;
    [SerializeField] Toggle windowedMode;
    [SerializeField] GameObject respBorders;
    public GameObject inventory_cam;

    [System.Serializable]
    public class DATA {
        public int money;
        public int rounds;
        public List<string> items = new List<string>();
        public string first_weapon;
        public string second_weapon;
        public List<string> storage = new List<string>();
    }
    public DATA current_data;

    [System.Serializable]
    public class SETTINGS {
        public string userName = "Player";
        public int qualitySettings = 5;
        public bool windowedMode = false;
    }
    public SETTINGS current_settings;

    void Start() {
        network_manager = GameObject.Find("NetworkManager").GetComponent<NetworkLobbyManager>();
        current_data = new DATA();
        current_settings = new SETTINGS();
        LoadUserData();
        LoadUserSettings();
    }

    void Update() {
        if (respBorders && localPlayer && localPlayer.GetComponent<PlayerStatus>().GetStatus == "ready") respBorders.SetActive(true);
        else if (respBorders) respBorders.SetActive(false);
    }

    public void LoadUserData() {
        if (File.Exists(Application.dataPath + "/User/data.sv")) {
            FileStream fs = new FileStream(Application.dataPath + "/User/data.sv", FileMode.Open);
            BinaryFormatter formatter = new BinaryFormatter();
            try {
                DATA saved_data = (DATA)formatter.Deserialize(fs);
                current_data.money = saved_data.money;
                current_data.rounds = saved_data.rounds;
                current_data.items = saved_data.items;
                current_data.first_weapon = saved_data.first_weapon;
                current_data.second_weapon = saved_data.second_weapon;
                current_data.storage = saved_data.storage;
            } catch (System.Exception e) {
                Debug.Log(e.Message);
            } finally {
                fs.Close();
            }
        } else {
            Debug.Log("User data file not found!");
            current_data.money = 1000;
            current_data.rounds = 500;
            current_data.first_weapon = "Base_AK74{PistolGrip_AKPlasticOrange{}Magazine_AK545BunkerPlasticOrange{}Butt_AK74{}Forend_AK74{}ReceiverCover_AKRibbed{}-{}Battery_AKLipo1000{}} 10000 0 0";
            current_data.second_weapon = "";
        }
    }

    public void SaveUserData() {
        if (!Directory.Exists(Application.dataPath + "/User")) Directory.CreateDirectory(Application.dataPath + "/User");
        FileStream fs = new FileStream(Application.dataPath + "/User/data.sv", FileMode.Create);
        BinaryFormatter formatter = new BinaryFormatter();
        formatter.Serialize(fs, current_data);
        fs.Close();
    }

    public void LoadUserSettings() {
        if (File.Exists(Application.dataPath + "/User/settings.sv")) {
            FileStream fs = new FileStream(Application.dataPath + "/User/settings.sv", FileMode.Open);
            BinaryFormatter formatter = new BinaryFormatter();
            try {
                SETTINGS saved_settings = (SETTINGS)formatter.Deserialize(fs);
                current_settings.userName = saved_settings.userName;
                current_settings.qualitySettings = saved_settings.qualitySettings;
                current_settings.windowedMode = saved_settings.windowedMode;
            } catch (System.Exception e) {
                Debug.Log(e.Message);
            } finally {
                fs.Close();
            }
        } else Debug.Log("User settings file not found!");

        if (saved_user_name) {
            new_user_name.transform.parent.gameObject.GetComponent<InputField>().text = "";
            saved_user_name.text = current_settings.userName;
            qualitySettings.value = current_settings.qualitySettings;
            windowedMode.isOn = current_settings.windowedMode;
            ChangeGraphicsSettings();
        }
    }

    public void SaveUserSettings() {
        if (new_user_name.text != "" && !new_user_name.text.Contains(" ") && !new_user_name.text.Contains("#") && !new_user_name.text.Contains("-") &&
            !new_user_name.text.Contains("{") && !new_user_name.text.Contains("}")) current_settings.userName = new_user_name.text;
        else current_settings.userName = saved_user_name.text;
        current_settings.qualitySettings = qualitySettings.value;
        current_settings.windowedMode = windowedMode.isOn;

        if (!Directory.Exists(Application.dataPath + "/User")) Directory.CreateDirectory(Application.dataPath + "/User");
        FileStream fs = new FileStream(Application.dataPath + "/User/settings.sv", FileMode.Create);
        BinaryFormatter formatter = new BinaryFormatter();
        formatter.Serialize(fs, current_settings);
        fs.Close();

        new_user_name.transform.parent.gameObject.GetComponent<InputField>().text = "";
        saved_user_name.text = current_settings.userName;
    }

    public void ChangeGraphicsSettings() { QualitySettings.SetQualityLevel(qualitySettings.value, true); }

    public void WindowedMode() { Screen.fullScreen = !windowedMode.isOn; }

    public static void RegisterPlayer(string player_name, GameObject player) {
        players_base.Add(player_name, player);
        Debug.Log(player_name + " connected");
    }

    public static void UnregisterPlayer(string player_name) {
        players_base.Remove(player_name);
        Debug.Log(player_name + " disconnected");
    }

    public static GameObject GetPlayer(string player_name) {
        return players_base[player_name];
    }

    public void StartHost() {
        if (network_manager && !NetworkClient.active && !NetworkServer.active && !network_manager.matchMaker) network_manager.StartHost();
    }

    public void StartClient() {
        if (network_manager && !NetworkClient.active && !NetworkServer.active && !network_manager.matchMaker) {
            if (address.text == "") network_manager.networkAddress = "localhost";
            else network_manager.networkAddress = address.text;
            network_manager.StartClient();
        }
    }

    public void StopHost() {
        if (network_manager) network_manager.StopHost();
    }

    public void Ready() {
        if (network_manager) network_manager.CheckReadyToBegin();
    }

    public void ExitGame() {
        Application.Quit();
    }
}
