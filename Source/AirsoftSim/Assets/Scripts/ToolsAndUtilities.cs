using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ToolsGUI : MonoBehaviour {
    public static void CreateStorage(List<string> storageData, List<GameObject> modules, RectTransform viewportContent, GameObject cellPrefab, int cellsPerRow=5, bool isForShop=false, string cellInfo="storage") {
        // Очистка старых ячеек
        InventoryCell[] cells = viewportContent.transform.GetComponentsInChildren<InventoryCell>();
        foreach (InventoryCell cell in cells) Destroy(cell.gameObject);
        
        // Задание параметров для формирования новых ячеек
        float delta = viewportContent.rect.width / cellsPerRow;
        float cellSize = delta * 0.8f;
        for (int i = 0; i < storageData.Count; i++) {
            foreach (GameObject module in modules) {
                if (module.name == storageData[i] || (storageData[i].IndexOf("{") != -1 && module.name == storageData[i].Substring(0, storageData[i].IndexOf("{")))) {
                    // Создание и позиционирование ячейки
                    GameObject newCell = Instantiate(cellPrefab, viewportContent.transform);
                    newCell.transform.SetParent(viewportContent.transform);
                    newCell.GetComponent<RectTransform>().sizeDelta = new Vector2(cellSize, cellSize);
                    newCell.GetComponent<RectTransform>().anchoredPosition = new Vector2(delta / 2 + delta * (i % cellsPerRow), -delta / 2 - delta * (i / cellsPerRow));
                    viewportContent.sizeDelta = new Vector2(1, delta + delta * (i / cellsPerRow));

                    // Установка параметров ячейки в зависимости от демонстрируемого предмета
                    newCell.GetComponent<Button>().image.sprite = module.GetComponent<Module>().icon;
                    InventoryCell cellComponent = newCell.GetComponent<InventoryCell>();
                    cellComponent.item_id = storageData[i];
                    cellComponent.module_script = module.GetComponent<Module>();
                    cellComponent.isForShop = isForShop;
                    cellComponent.cell_info = cellInfo;
                    break;
                }
            }            
        }
    }
}
