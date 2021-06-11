using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class ObjectLifetime : NetworkBehaviour {

    public float lifetime = 2f;
    private float timer = 0f;

    void Start() {
        
    }

    void Update() {
        timer += Time.deltaTime;
        if (timer >= lifetime) Destroy(gameObject);
    }

    [Client]
    void DestroyObj(GameObject obj) {
        CmdDestroy(obj);
    }

    [Command]
    void CmdDestroy(GameObject obj) {
        NetworkServer.Destroy(obj);
    }
}
