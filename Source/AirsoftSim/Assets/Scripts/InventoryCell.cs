using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InventoryCell : MonoBehaviour {

    public string cell_info = "storage";
    public string item_id;
    public Module module_script;
    private Inventory inventory;
    private Shop shop;
    public bool isForShop = false;
    GameObject game_manager;

    void Awake() {
        if (!game_manager) game_manager = GameObject.Find("GameManager");
        inventory = game_manager.GetComponent<Inventory>();
        shop = game_manager.GetComponent<Shop>();
    }

    void Update() {
        
    }

    public void OnClick() {
        if (item_id != "") {
            if (isForShop) {
                shop.current_selected_item_id = item_id;
                shop.shop.ShowInfo(shop.info, shop.current_selected_item_id);
                return;
            }
            if (inventory.adding_module) {
                inventory.AddModule(cell_info, item_id, module_script);
                return;
            }
            inventory.current_selected_item_id = item_id;
            inventory.current_selected_item_module = module_script;
            inventory.ViewModule(item_id);
            inventory.ActionButtons(cell_info);
        }
    }
}
