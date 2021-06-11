using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Module : MonoBehaviour {

    // Информация о слотах и доступных для установки на них модулях
    [System.Serializable]
    public class Slot {
        public Transform transform_info;
        public GameObject[] avail_modules;
        public GameObject pointer;

        public Module UpdateTransform(string moduleID) {
            foreach (GameObject availModule in avail_modules) {
                if (availModule.name == moduleID) {
                    availModule.SetActive(true);
                    availModule.transform.position = new Vector3(transform_info.position.x, transform_info.position.y, transform_info.position.z);
                    availModule.transform.Rotate(transform_info.localRotation.x, transform_info.localRotation.y, transform_info.localRotation.z);
                    return availModule.GetComponent<Module>();
                }
            }
            return null;
        }

        public void PointerOn() {
            if (transform_info.childCount > 0) pointer = transform_info.GetChild(0).gameObject;
            if (!pointer) return;
            pointer.SetActive(true);
            foreach (GameObject go in avail_modules) {
                if (go.activeSelf) {
                    pointer.GetComponent<ModulePointer>().moduleID = go.name;
                    pointer.GetComponent<ModulePointer>().InitPointer(go.GetComponent<Module>());
                    return;
                }
            }
            pointer.GetComponent<ModulePointer>().moduleID = "";
            pointer.GetComponent<ModulePointer>().InitPointer(null);
        }

        public void PointerOff() { if (pointer) pointer.SetActive(false); }
    }
    public Slot[] slots;

    // Влияние модуля на общие ТТХ оружия
    [System.Serializable] public class SpecificationsChange {
        public int mag_capacity_increase = 0, battery_capacity_increase = 0, destruction_rate = 0;
        public float weight_increase = 0.0f, deviation_increase = 0.0f, shottime_delta_increase = 0.0f, deviation_coeff = 1.0f, shottime_delta_coeff = 1.0f;
    }
    public SpecificationsChange specificationsChange;

    [System.Serializable] public class Specifications {
        public int mag_capacity = 0, battery_capacity = 0, destruction_rate = 0;
        public float weight = 0f, deviation = 0f, shottime_delta = 0f;

        public void UpdateSpecifications(SpecificationsChange newChanges) {
            mag_capacity += newChanges.mag_capacity_increase;
            battery_capacity += newChanges.battery_capacity_increase;
            destruction_rate += newChanges.destruction_rate;
            weight += newChanges.weight_increase;
            deviation = newChanges.deviation_coeff * deviation + newChanges.deviation_increase;
            shottime_delta = newChanges.shottime_delta_coeff * shottime_delta + newChanges.shottime_delta_increase;
        }

        public string SpecificationsExport(Module parentModule, string weaponID) {
            float damage = 1.0f;
            if (parentModule.category == "Main weapon" || parentModule.category == "Secondary weapon")
                damage = int.Parse(weaponID.Split()[1]) / 10000.0f;
            return parentModule.shop_name + "\n\nManufacturer: " + parentModule.manufacturer + "\nCategory: " + parentModule.category +
                "\nSale value: " + ((int)(parentModule.cost * 0.95f * damage)).ToString() + " $" +
                "\nRepair cost: " + ((int)(parentModule.cost * 0.5f * (1 - damage))).ToString() + " $" +
                "\n\nMag capacity: " + mag_capacity.ToString() + "\nBattery capacity: " + battery_capacity.ToString() + "\nDestruction rate: " +
                destruction_rate.ToString() + "\nWeight: " + System.Math.Round(weight, 3).ToString() + "\nMax deviation angle: " +
                System.Math.Round(deviation, 3).ToString() + "\nShottime delta, s: " + System.Math.Round(shottime_delta, 3).ToString();
        }
    }

    // Торговая информация о модуле
    public int cost;
    public string shop_name, manufacturer, category;

    // Информация для отображения (в GUI)
    public Sprite icon;
    public Transform menu_camera;

    // Особые возможности
    [SerializeField] GameObject lightObj;

    void Update() {
        if (lightObj && Input.GetKeyDown(KeyCode.L)) lightObj.SetActive(!lightObj.activeSelf);
    }
 
    // Отображение / сокрытие указателей на слотах (для обзора модуля в инвентаре)
    public void ActivatePointers() { foreach (Slot slot in slots) slot.PointerOn(); }

    public void DeactivatePointers() { foreach (Slot slot in slots) slot.PointerOff(); }

    // Подключение модулей и обновление ТТХ текущего оружия (модуля) для меню или игры
    public Specifications BuildWeapon(string moduleID, bool isWeaponPart=true, Specifications weaponSpecifications=null) {
        if (weaponSpecifications == null) weaponSpecifications = new Specifications();
        weaponSpecifications.UpdateSpecifications(specificationsChange);
        if (isWeaponPart) {
            string children = moduleID.Split()[0];
            children = children.Substring(children.IndexOf("{") + 1, children.Length - children.IndexOf("{") - 2);
            int bracketCount = 0, slotIndex = 0, childIndex = 0;
            for (int i = 0; i < children.Length; i++) {
                if (children[i] == '{') bracketCount++;
                else if (children[i] == '}') {
                    bracketCount--;
                    if (bracketCount == 0) {
                        string child = children.Substring(childIndex, i + 1 - childIndex);
                        if (child != "-{}") weaponSpecifications = slots[slotIndex].UpdateTransform(child.Substring(0, child.IndexOf("{"))).BuildWeapon(child, isWeaponPart, weaponSpecifications);
                        childIndex = i + 1; slotIndex++;
                    }
                }
            }
            ActivatePointers();
        }
        return weaponSpecifications;
    }

    // Удаление указанного модуля
    public void RemoveModule(ref string weaponID, List<string> storage, Module moduleToRemove) {
        int targetPosition = 0, currentPosition = 0, bracketsCount = 0, startIndex = -1, finishIndex = -1;
        FindModulePositionToRemove(moduleToRemove, ref targetPosition);
        for (int i = 0; i < weaponID.Length; i++) {
            if (startIndex == -1) {
                if (weaponID[i] == '{') currentPosition++;
                if ((weaponID[i] == '{' || weaponID[i] == '}') && weaponID[i + 1] != '}' && currentPosition == targetPosition) {
                    startIndex = i + 1;
                    continue;
                }
            } else {
                if (weaponID[i] == '{') bracketsCount++;
                else if (weaponID[i] == '}') {
                    bracketsCount--;
                    if (bracketsCount == 0) {
                        finishIndex = i + 1;
                        break;
                    }
                }
            }
        }
        string[] modulesToRemove = weaponID.Substring(startIndex, finishIndex - startIndex).Replace("-{}", "").Replace("{}", " ").Replace("{", " ").Replace("}", "").Split();
        foreach (string module in modulesToRemove) if (module != "") storage.Add(module);
        weaponID = weaponID.Substring(0, startIndex) + "-{}" + weaponID.Substring(finishIndex);
    }

    public bool FindModulePositionToRemove(Module module, ref int moduleNumber) {
        // Обход слотов текущего объекта (по умолчанию - корпуса оружия)
        foreach (Slot slot in slots) {
            moduleNumber++; // Накопление счетчика позиции, на которой наход. модуль для удаления
            // Поиск установленного на рассматриваемый слот модуля (он будет включен)
            foreach (GameObject avail_module in slot.avail_modules) if (avail_module.activeSelf) {
                    if (avail_module.GetComponent<Module>() == module) return true; // Искомый модуль для удаления найден
                    if (avail_module.GetComponent<Module>().FindModulePositionToRemove(module, ref moduleNumber)) return true;
                    // Искомый модуль для удаления найден у одного из дочерних (рекурсивный обход)
                    break;
                }
        }
        return false; // У данного род. объекта и всех его дочерних искомого модуля не обнаружено
    }

    // Добавление указанного модуля
    public string AddModule(string weaponID, List<string> storage, Module moduleToAdd, Transform targetSlot) {
        int targetPosition = 0, currentPosition = 0, indexToAdd = -1;
        FindModulePositionToAdd(targetSlot, ref targetPosition); // Определение позиции, в которую необходимо добавить модуль
        for (int i = 0; i < weaponID.Length; i++) {
            if (weaponID[i] == '{') currentPosition++;
            if ((weaponID[i] == '{' || weaponID[i] == '}') && weaponID[i + 1] != '}' && currentPosition == targetPosition) {
                indexToAdd = i + 1;
                break;
            }
        }
        if (weaponID[indexToAdd] != '-') return weaponID; // Случай, когда слот, куда нужно добавить модуль, уже непуст
        string moduleConfig = moduleToAdd.gameObject.name + "{";
        foreach (Slot slot in moduleToAdd.slots) moduleConfig += "-{}"; // Добавление к ID добавленного модуля пустых слотов (по умолчанию)
        storage.Remove(moduleToAdd.gameObject.name); // Удаление добавленного на оружие модуля из хранилища
        return weaponID.Substring(0, indexToAdd) + moduleConfig + "}" + weaponID.Substring(indexToAdd + 3); // Возврат обновленного ID оружия
    }

    public bool FindModulePositionToAdd(Transform targetSlot, ref int moduleNumber) {
        // Обход слотов текущего объекта (по умолчанию - корпуса оружия)
        foreach (Slot slot in slots) {
            moduleNumber++; // Накопление счетчика позиции, на которой наход. слот для добавления модуля
            if (slot.transform_info == targetSlot) return true; // Искомый слот для добавления модуля найден
            // Рекурсивный обход всех дочерних объектов (если такие есть - они будут включены) для поиска необходимого слота
            foreach (GameObject avail_module in slot.avail_modules) if (avail_module.activeSelf) {
                    if (avail_module.GetComponent<Module>().FindModulePositionToAdd(targetSlot, ref moduleNumber)) return true;
                    // Искомый слот найден у дочернего объекта
                    break;
                }
        }
        return false; // У данного род. объекта и всех его дочерних искомый слот не обнаружен
    }
}
