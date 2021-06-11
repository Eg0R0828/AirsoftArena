using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ModulePointer : MonoBehaviour {

    public Module module = null;
    public string moduleID = "";
    public Module parentModule;
    [SerializeField] Transform menuCam;
    [SerializeField] bool isFrontal = true;

    void Start() {
        parentModule = transform.parent.parent.gameObject.GetComponent<Module>();
    }

    void Update() {
        transform.LookAt(menuCam);
        transform.eulerAngles = new Vector3(transform.localRotation.x, transform.localRotation.y - 90, transform.localRotation.z);
        if ((isFrontal && (parentModule.transform.eulerAngles.z < 75f || parentModule.transform.eulerAngles.z > 285f) &&
            parentModule.transform.eulerAngles.y > 195f && parentModule.transform.eulerAngles.y < 345f) ||
            (!isFrontal && (parentModule.transform.eulerAngles.z < 255f && parentModule.transform.eulerAngles.z > 105f ||
            parentModule.transform.eulerAngles.y > 15f && parentModule.transform.eulerAngles.y < 165f)))
            for (int i = 0; i < transform.childCount; i++) transform.GetChild(i).gameObject.SetActive(true);
        else for (int i = 0; i < transform.childCount; i++) transform.GetChild(i).gameObject.SetActive(false);
    }

    public void InitPointer(Module parentModule) {
        module = parentModule;
        if (module) {
            transform.GetChild(0).gameObject.GetComponent<TextMesh>().text = module.shop_name;
            transform.GetChild(0).gameObject.GetComponent<TextMesh>().color = new Vector4(0, 1, 0, 1);
        } else {
            transform.GetChild(0).gameObject.GetComponent<TextMesh>().text = "Empty slot";
            transform.GetChild(0).gameObject.GetComponent<TextMesh>().color = new Vector4(1, 0, 0, 1);
        }
    }

    public string GetModuleShopName => transform.GetChild(0).gameObject.GetComponent<TextMesh>().text;

    public string GetModuleID => moduleID;
}
