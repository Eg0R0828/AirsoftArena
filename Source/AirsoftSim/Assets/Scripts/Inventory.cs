using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Inventory : MonoBehaviour {

    public bool isMenu = true;

    // Элементы интерфейса склада (хранилища) и непосредственно игрового инвентара
    [SerializeField] RectTransform storageContent, ammunitionContent;
    [SerializeField] GameObject cellPref;
    [SerializeField] Text ammunition_load;
    [SerializeField] Button mainWeaponSlot, secondaryWeaponSlot;
    [SerializeField] Sprite default_cell_image;

    // Индикация шаров и ресурсов (денег)
    [SerializeField] Text resources;

    // Элементы манипуляции над выбранным предметом. Информация
    [SerializeField] GameObject[] item_action_guis;
    [SerializeField] string[] item_action_descr;
    public string current_selected_item_id = "";
    public Module current_selected_item_module;
    [SerializeField] Text current_selected_item_info;

    // Элементы манипуляции модулем на выбранном оружии
    [SerializeField] GameObject module_action_panel;
    GameObject selected_slot;
    public bool adding_module = false;

    // Обзор выбранного предмета
    [HideInInspector] public List<GameObject> modules = new List<GameObject>();
    [SerializeField] Transform view_point;
    [SerializeField] GameObject rotate_point;

    // Прочее (полезные сслыки)
    GameManager game_manager;
    PlayerSetup player_setup;

    void Start() {
        // Стартовая инициализация данных
        game_manager = gameObject.GetComponent<GameManager>();
        rotate_point.transform.eulerAngles = new Vector3(0, 0, 0);
        foreach (Module moduleComponent in transform.GetComponentsInChildren<Module>(true)) modules.Add(moduleComponent.gameObject);
    }

    void Update() {
        if (!player_setup && game_manager && game_manager.localPlayer) player_setup = game_manager.localPlayer.GetComponent<PlayerSetup>();
        if (!isMenu && player_setup && !player_setup.inventory) return;

        resources.text = "Rounds: " + game_manager.current_data.rounds.ToString() + "; Money: " + game_manager.current_data.money.ToString() + " $";
        ammunition_load.text = "Ammunition load: " + OverWeight() + " kg";
        if (current_selected_item_module) {
            /*current_selected_item_info.text = current_selected_item_module.shop_name + "\nManufacturer: " + current_selected_item_module.manufacturer +
                "\nCategory: " + current_selected_item_module.category;
            float damage = 1.0f;
            if (current_selected_item_module.category == "Main weapon" || current_selected_item_module.category == "Secondary weapon")
                damage = int.Parse(current_selected_item_id.Split()[1]) / 10000.0f;
            current_selected_item_info.text += "\nSale value: " + ((int)(current_selected_item_module.cost * 0.95f * damage)).ToString() + " $";
            current_selected_item_info.text += "\nRepair cost: " + ((int)(current_selected_item_module.cost * 0.5f * (1 - damage))).ToString() + " $";*/
        } else current_selected_item_info.text = "Some information about current selected item ...";

        // Поворот текущего предмета при обзоре
        if (rotate_point.activeSelf && Input.GetMouseButton(0) && current_selected_item_id != "" && (isMenu || game_manager.inventory_cam.activeSelf) &&
            Input.mousePosition[0] >= Screen.width * 0.2f && Input.mousePosition[0] <= Screen.width * 0.8f &&
            Input.mousePosition[1] >= Screen.height * 0.4f && Input.mousePosition[1] <= Screen.height * 0.8f)
                rotate_point.transform.eulerAngles = new Vector3(rotate_point.transform.eulerAngles.x,
                    rotate_point.transform.eulerAngles.y - Input.GetAxis("Mouse X") * 10,
                    rotate_point.transform.eulerAngles.z - Input.GetAxis("Mouse Y") * 10);

        if (Input.GetMouseButtonUp(0) && (isMenu || game_manager.inventory_cam.activeSelf)) {
            RaycastHit hit;
            Ray ray = game_manager.inventory_cam.GetComponent<Camera>().ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out hit, 1.5f) && hit.collider.gameObject) selected_slot = hit.collider.transform.parent.transform.gameObject;
        }
        if (selected_slot) {
            module_action_panel.SetActive(true);
            module_action_panel.transform.GetChild(0).gameObject.GetComponent<Text>().text = selected_slot.GetComponent<ModulePointer>().GetModuleShopName;
        } else module_action_panel.SetActive(false);
    }

    // Обработчики кнопок манипуляции текущим предметом или модулем
    // Перемещение предмета
    public void ReplaceItem(string from_to) {
        string from = from_to.Split()[0];
        string to = from_to.Split()[1];
        if (to == "storage") game_manager.current_data.storage.Add(current_selected_item_id);
        if (to == "ammunition") game_manager.current_data.items.Add(current_selected_item_id);
        if (to == "1stweapon") {
            if (game_manager.current_data.first_weapon != "") ReplaceItem("1stweapon storage");
            game_manager.current_data.first_weapon = current_selected_item_id;
        }
        if (to == "2ndweapon") {
            if (game_manager.current_data.second_weapon != "") ReplaceItem("2ndweapon storage");
            game_manager.current_data.second_weapon = current_selected_item_id;
        }
        if (from == "storage") game_manager.current_data.storage.Remove(current_selected_item_id);
        if (from == "ammunition") game_manager.current_data.items.Remove(current_selected_item_id);
        if (from == "1stweapon") game_manager.current_data.first_weapon = "";
        if (from == "2ndweapon") game_manager.current_data.second_weapon = "";
        SaveInventory();
        UpdateInventoryGUI();
    }

    // Починка текущего оружия
    public void FixCurrentWeapon(string weapon_slot) {
        int repair_cost = (int)(current_selected_item_module.cost * 0.5f * (1 - int.Parse(current_selected_item_id.Split()[1]) / 10000));
        if (repair_cost > game_manager.current_data.money) return;
        if (weapon_slot == "1stweapon") game_manager.current_data.first_weapon = game_manager.current_data.first_weapon.Split()[0] + " 10000 " +
                game_manager.current_data.first_weapon.Split()[2] + " " + game_manager.current_data.first_weapon.Split()[3];
        if (weapon_slot == "2ndweapon") game_manager.current_data.second_weapon = game_manager.current_data.second_weapon.Split()[0] + " 10000 " +
                game_manager.current_data.second_weapon.Split()[2] + " " + game_manager.current_data.second_weapon.Split()[3];
        game_manager.current_data.money -= repair_cost;
        SaveInventory();
        UpdateInventoryGUI();
    }

    // Продажа предмета
    public void SellCurrentItem(string from) {
        float damage = 1.0f;
        if (current_selected_item_module.category == "Main weapon" || current_selected_item_module.category == "Secondary weapon")
            damage = int.Parse(current_selected_item_id.Split()[1]) / 10000;
        int sale_value = (int)(current_selected_item_module.cost * 0.95f * damage);
        game_manager.current_data.money += sale_value;
        if (from == "storage") game_manager.current_data.storage.Remove(current_selected_item_id);
        if (from == "ammunition") game_manager.current_data.items.Remove(current_selected_item_id);
        if (from == "1stweapon") game_manager.current_data.first_weapon = "";
        if (from == "2ndweapon") game_manager.current_data.second_weapon = "";
        SaveInventory();
        UpdateInventoryGUI();
    }

    public void DropCurrentItem(string from) {
        if (current_selected_item_id == "") return;
        if (from == "ammunition") game_manager.current_data.items.Remove(current_selected_item_id);
        if (from == "1stweapon") {
            player_setup.WeaponIdManip("first", "w");
            current_selected_item_id = game_manager.current_data.first_weapon;
            game_manager.current_data.first_weapon = "";
        }
        if (from == "2ndweapon") {
            player_setup.WeaponIdManip("second", "w");
            current_selected_item_id = game_manager.current_data.second_weapon;
            game_manager.current_data.second_weapon = "";
        }
        player_setup.LocalDrop(current_selected_item_id);
        SaveInventory();
        UpdateInventoryGUI();
    }

    // Удаление / установка модуля
    public void RemoveModule() {
        if (module_action_panel.transform.GetChild(0).gameObject.GetComponent<Text>().text != "Empty slot") {
            List<string> storage;
            if (isMenu) storage = game_manager.current_data.storage;
            else storage = game_manager.current_data.items;
            if (current_selected_item_module.category == "Main weapon") {
                current_selected_item_module.RemoveModule(ref game_manager.current_data.first_weapon, storage, selected_slot.GetComponent<ModulePointer>().module);
                if (!isMenu && selected_slot.GetComponent<ModulePointer>().module.category == "Magazines") {
                    game_manager.current_data.rounds += player_setup.current_round_count_1stweapon;
                    player_setup.current_round_count_1stweapon = 0;
                } else if (!isMenu && selected_slot.GetComponent<ModulePointer>().module.category == "Batteries") player_setup.current_battery_charge_1stweapon = 0;
            } else if (current_selected_item_module.category == "Secondary weapon") {
                current_selected_item_module.RemoveModule(ref game_manager.current_data.second_weapon, storage, selected_slot.GetComponent<ModulePointer>().module);
                if (!isMenu && selected_slot.GetComponent<ModulePointer>().module.category == "Magazines") {
                    game_manager.current_data.rounds += player_setup.current_round_count_1stweapon;
                    player_setup.current_round_count_1stweapon = 0;
                } else if (!isMenu && selected_slot.GetComponent<ModulePointer>().module.category == "Batteries") player_setup.current_battery_charge_1stweapon = 0;
            }
            SaveInventory();
            UpdateInventoryGUI();
        }
    }

    public void AddModule() {
        adding_module = true;
    }

    public void AddModule(string cell_info, string new_module_id, Module new_module_script) {
        foreach (Module.Slot slot in selected_slot.GetComponent<ModulePointer>().parentModule.slots) {
            if (slot.transform_info.name == selected_slot.transform.parent.name) {
                foreach (GameObject avail_module in slot.avail_modules) {
                    if (avail_module.name == new_module_id) {
                        if (current_selected_item_module.category == "Main weapon" && cell_info == "storage")
                            game_manager.current_data.first_weapon = current_selected_item_module.AddModule(current_selected_item_id, game_manager.current_data.storage, new_module_script, selected_slot.transform.parent);
                        if (current_selected_item_module.category == "Main weapon" && cell_info == "ammunition")
                            game_manager.current_data.first_weapon = current_selected_item_module.AddModule(current_selected_item_id, game_manager.current_data.items, new_module_script, selected_slot.transform.parent);
                        if (current_selected_item_module.category == "Secondary weapon" && cell_info == "storage")
                            game_manager.current_data.second_weapon = current_selected_item_module.AddModule(current_selected_item_id, game_manager.current_data.storage, new_module_script, selected_slot.transform.parent);
                        if (current_selected_item_module.category == "Secondary weapon" && cell_info == "ammunition")
                            game_manager.current_data.second_weapon = current_selected_item_module.AddModule(current_selected_item_id, game_manager.current_data.items, new_module_script, selected_slot.transform.parent);
                        break;
                    }
                }
                break;
            }
        }
        SaveInventory();
        UpdateInventoryGUI();
    }

    // Обновление инвентаря (при открытии или перемещении/продаже объектов)
    public void UpdateInventoryGUI() {
        current_selected_item_id = "";
        current_selected_item_module = null;
        foreach (GameObject go in modules) {
            go.transform.position = new Vector3(view_point.position.x, view_point.position.y, view_point.position.z);
            go.GetComponent<Module>().DeactivatePointers();
            go.SetActive(false);
        }
        // Заполнение "склада"
        if (isMenu) ToolsGUI.CreateStorage(game_manager.current_data.storage, modules, storageContent, cellPref);
        // Заполнение "разгруза" (игрового инвентаря)
        ToolsGUI.CreateStorage(game_manager.current_data.items, modules, ammunitionContent, cellPref, cellsPerRow:4, cellInfo:"ammunition");
        // Заполнение слотов для оружия
        foreach (GameObject module in modules) {
            Sprite icon = module.GetComponent<Module>().icon;
            if (game_manager.current_data.first_weapon != "" && module.name == game_manager.current_data.first_weapon.Substring(0, game_manager.current_data.first_weapon.IndexOf("{"))) {
                mainWeaponSlot.image.sprite = icon;
                mainWeaponSlot.gameObject.GetComponent<InventoryCell>().item_id = game_manager.current_data.first_weapon;
                mainWeaponSlot.gameObject.GetComponent<InventoryCell>().module_script = module.GetComponent<Module>();
            } else if (game_manager.current_data.second_weapon != "" && module.name == game_manager.current_data.second_weapon.Substring(0, game_manager.current_data.second_weapon.IndexOf("{"))) {
                secondaryWeaponSlot.image.sprite = icon;
                secondaryWeaponSlot.gameObject.GetComponent<InventoryCell>().item_id = game_manager.current_data.second_weapon;
                secondaryWeaponSlot.gameObject.GetComponent<InventoryCell>().module_script = module.GetComponent<Module>();
            }
        }
        ActionButtons();
    }

    // Отображение текущего выбранного модуля
    public void ViewModule(string id) {
        // "Очистка обзорной области" - выключение всех модулей
        foreach (GameObject go in modules) {
            go.transform.position = new Vector3(view_point.position.x, view_point.position.y, view_point.position.z);
            go.GetComponent<Module>().DeactivatePointers();
            go.SetActive(false);
        }
        // Включение нужных
        foreach (GameObject go in modules) {
            if (go.name == id || (id.IndexOf("{") >= 0 && go.name == id.Substring(0, id.IndexOf("{")))) {
                go.SetActive(true);
                Module moduleScript = go.GetComponent<Module>();
                current_selected_item_info.text = moduleScript.BuildWeapon(id, id.IndexOf("{") >= 0).SpecificationsExport(moduleScript, current_selected_item_id);
                break;
            }
        }
    }

    public void ActionButtons(string place_id="") {
        selected_slot = null;
        adding_module = false;
        foreach (GameObject gui_element in item_action_guis) gui_element.SetActive(false);
        if (!current_selected_item_module) return;
        for (int i = 0; i < item_action_descr.Length; i++)
            if (item_action_descr[i] == place_id + " " + current_selected_item_module.category) {
                item_action_guis[i].SetActive(true);
                break;
            }
    }

    // Подсчет нагруженности
    public float OverWeight() {
        float total_weight = 0.0f;
        foreach (GameObject module in modules) {
            foreach (string item_id in game_manager.current_data.items)
                if (module.name == item_id) total_weight += module.GetComponent<Module>().specificationsChange.weight_increase;
            if (game_manager.current_data.first_weapon != "" &&
                module.name == game_manager.current_data.first_weapon.Substring(0, game_manager.current_data.first_weapon.IndexOf("{")))
                total_weight += WeaponWeight(game_manager.current_data.first_weapon.Split()[0]);
            if (game_manager.current_data.second_weapon != "" &&
                module.name == game_manager.current_data.second_weapon.Substring(0, game_manager.current_data.second_weapon.IndexOf("{")))
                total_weight += WeaponWeight(game_manager.current_data.second_weapon.Split()[0]);
        }
        return total_weight;
    }

    float WeaponWeight(string weapon_id) {
        float weapon_weight = 0.0f;
        weapon_id = weapon_id.Replace("-{}", "").Replace("{}", " ").Replace("{", " ").Replace("}", " ");
        foreach (string module_name in weapon_id.Split()) {
            foreach (GameObject module in modules) {
                if (module.name == module_name) {
                    weapon_weight += module.GetComponent<Module>().specificationsChange.weight_increase;
                    break;
                }
            }
        }
        return weapon_weight;
    }

    // Сохранение изменений во всем инвентаре
    public void SaveInventory() {
        mainWeaponSlot.image.sprite = secondaryWeaponSlot.image.sprite = default_cell_image;
        mainWeaponSlot.GetComponent<InventoryCell>().item_id = secondaryWeaponSlot.GetComponent<InventoryCell>().item_id = "";
        mainWeaponSlot.GetComponent<InventoryCell>().module_script = secondaryWeaponSlot.GetComponent<InventoryCell>().module_script = null;
        game_manager.SaveUserData();
        foreach (GameObject go in modules) {
            go.transform.position = new Vector3(view_point.position.x, view_point.position.y, view_point.position.z);
            go.SetActive(false);
        }
    }
}
