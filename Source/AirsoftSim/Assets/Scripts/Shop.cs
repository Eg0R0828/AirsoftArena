using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Shop : MonoBehaviour {

    public string current_selected_item_id = "";
    public Text info;
    GameManager game_manager;
    Inventory inventory;

    [SerializeField] GameObject content;
    [SerializeField] GameObject cell_pref;
    [SerializeField] public static Dictionary<string, Module> items_costs = new Dictionary<string, Module>();
    [System.Serializable]
    public class DICT {
        public string id;
        public Module module_component;

        public void Init() {
            if (!items_costs.ContainsKey(id)) items_costs.Add(id, module_component);
        }
    }
    [SerializeField] DICT[] dict;

    [System.Serializable]
    public class SHOP {
        Dictionary<string, Module> items_costs = new Dictionary<string, Module>();

        public SHOP(Dictionary<string, Module> out_items_costs) {
            items_costs = out_items_costs;
        }

        public void ShowInfo(Text info, string current_selected_item_id) {
            if (info && current_selected_item_id != "") {
                info.gameObject.SetActive(true);
                info.text = items_costs[current_selected_item_id].shop_name + "\n\nManufacturer: " + items_costs[current_selected_item_id].manufacturer +
                    "\nCost: " + items_costs[current_selected_item_id].cost + " $" +
                    "\n\nMag capacity: " + items_costs[current_selected_item_id].specificationsChange.mag_capacity_increase.ToString() +
                    "\nBattery capacity: " + items_costs[current_selected_item_id].specificationsChange.battery_capacity_increase.ToString() +
                    "\nDestruction rate: " + items_costs[current_selected_item_id].specificationsChange.destruction_rate.ToString() +
                    "\nWeight: " + System.Math.Round(items_costs[current_selected_item_id].specificationsChange.weight_increase, 3).ToString() +
                    "\nMax deviation angle coeff.: " + System.Math.Round(items_costs[current_selected_item_id].specificationsChange.deviation_coeff, 3).ToString() +
                    "\nShottime delta coeff.: " + System.Math.Round(items_costs[current_selected_item_id].specificationsChange.shottime_delta_coeff, 3).ToString();
            }
        }
    }
    public SHOP shop;

    void Start() {
        game_manager = gameObject.GetComponent<GameManager>();
        inventory = gameObject.GetComponent<Inventory>();
        foreach (DICT dict_element in dict) dict_element.Init();
        ToolsGUI.CreateStorage(new List<string>(items_costs.Keys), inventory.modules, content.GetComponent<RectTransform>(), cell_pref, isForShop:true);
        shop = new SHOP(items_costs);
    }

    public void Buy() {
        if (game_manager.current_data.money - items_costs[current_selected_item_id].cost >= 0) {
            game_manager.current_data.money -= items_costs[current_selected_item_id].cost;
            game_manager.current_data.storage.Add(current_selected_item_id);
        }
        game_manager.SaveUserData();
    }

    public void OnOpenShop() {
        current_selected_item_id = "";
        info.gameObject.SetActive(false);
    }

    public void BuyRounds(int count) {
        int total_cost = (int)(count * 0.01f + 1.0f);
        if (total_cost > game_manager.current_data.money) return;
        game_manager.current_data.money -= total_cost;
        game_manager.current_data.rounds += count;
        game_manager.SaveUserData();
    }
}
