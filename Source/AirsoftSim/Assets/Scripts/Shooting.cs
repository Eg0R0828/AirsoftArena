using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class Shooting : NetworkBehaviour {

    public bool isJammed = false;

    public Transform balls_spawner;
    public LayerMask mask;

    public GameObject ball;
    public GameObject[] cracks; // 0 - metal; 1 - wood; 2 - terrain (land); 3 - glass
    public List<PhysicMaterial> physicsMaterials = new List<PhysicMaterial>();
    public GameObject fire_sound, jam_sound;

    [SerializeField] PlayerSetup player_setup;
    [SerializeField] PlayerStatus player_status;
    float timer = 0.0f;
    Vector3 deviation;

    void Start() {
        if (isLocalPlayer) {
            Transform[] localObjs = transform.GetComponentsInChildren<Transform>(true);
            foreach (Transform localObj in localObjs) if(localObj != transform) localObj.gameObject.layer = 11;
        } else enabled = false;
    }

    void Update() {
        if (player_setup.firingIsAvailable && player_status.GetStatus == "alive") {
            if (player_setup.current_shottime_delta == 0.0f && Input.GetMouseButtonDown(0) &&
                ((player_setup.current_weapon_slot == "first" && player_setup.current_battery_charge_1stweapon > 0) ||
                (player_setup.current_weapon_slot == "second" && player_setup.current_battery_charge_2ndweapon > 0))) {
                LocalShooting();
            } else if (isJammed && player_setup.current_shottime_delta > 0.0f && Input.GetMouseButtonDown(0) &&
                ((player_setup.current_weapon_slot == "first" && player_setup.current_battery_charge_1stweapon > 0) ||
                (player_setup.current_weapon_slot == "second" && player_setup.current_battery_charge_2ndweapon > 0))) LocalShooting();
            else if (!isJammed && player_setup.current_shottime_delta > 0.0f && Input.GetMouseButton(0) &&
                ((player_setup.current_weapon_slot == "first" && player_setup.current_battery_charge_1stweapon > 0) ||
                (player_setup.current_weapon_slot == "second" && player_setup.current_battery_charge_2ndweapon > 0))) {
                timer += Time.deltaTime;
                if (timer >= player_setup.current_shottime_delta) {
                    LocalShooting();
                    timer = 0.0f;
                }
            }
        }
    }

    public void UpdateBallsSpawner(Transform new_spawner) {
        balls_spawner = new_spawner;
    }

    void WeaponResManip() {
        int k = 1;
        if (player_setup.current_weapon_slot == "first") {
            if (isJammed || player_setup.current_round_count_1stweapon == 0) k++;
            player_setup.current_battery_charge_1stweapon -= 1;
            player_setup.current_strength_1stweapon -= (player_setup.current_strength_1stweapon >= k * player_setup.current_destruction_rate) ?
                k * player_setup.current_destruction_rate : player_setup.current_strength_1stweapon;
        } if (player_setup.current_weapon_slot == "second") {
            if (isJammed || player_setup.current_round_count_2ndweapon == 0) k++;
            player_setup.current_battery_charge_2ndweapon -= 1;
            player_setup.current_strength_2ndweapon -= (player_setup.current_strength_2ndweapon >= k * player_setup.current_destruction_rate) ?
                k * player_setup.current_destruction_rate : player_setup.current_strength_2ndweapon;
        }
    }

    [Client] void LocalShooting() {
        WeaponResManip();
        if (isJammed) {
            CmdJamSound();
            return;
        }
        CmdFireSound();

        if ((player_setup.current_weapon_slot == "first" && player_setup.current_round_count_1stweapon <= 0) ||
            (player_setup.current_weapon_slot == "second" && player_setup.current_round_count_2ndweapon <= 0)) return;

        deviation = new Vector3(Random.Range(-player_setup.current_deviation, player_setup.current_deviation),
            Random.Range(-player_setup.current_deviation, player_setup.current_deviation),
            Random.Range(-player_setup.current_deviation, player_setup.current_deviation));
        CmdBalls(balls_spawner.transform.position);
        if (player_setup.current_weapon_slot == "first") {
            player_setup.current_round_count_1stweapon -= 1;
            if (Random.Range(0, player_setup.current_strength_1stweapon + 1) == 0) {
                isJammed = true;
                CmdJamSound();
            }
        }
        if (player_setup.current_weapon_slot == "second") {
            player_setup.current_round_count_2ndweapon -= 1;
            if (Random.Range(0, player_setup.current_strength_2ndweapon + 1) == 0) {
                isJammed = true;
                CmdJamSound();
            }
        }
    }

    [Client] public void LocalHittedObjectProccessing(GameObject hittedObject, Vector3 crackPosition, Quaternion crackRotation) {
        if (hittedObject.tag != "Player") {
            int materialTypeIndex = 2;
            if (hittedObject.GetComponent<Collider>() && physicsMaterials.IndexOf(hittedObject.GetComponent<Collider>().sharedMaterial) != -1)
                materialTypeIndex = physicsMaterials.IndexOf(hittedObject.GetComponent<Collider>().sharedMaterial);
            CmdCrack(crackPosition, crackRotation, hittedObject, cracks[materialTypeIndex]);
        } else if (hittedObject.tag == "Player" && hittedObject.GetComponent<PlayerStatus>().GetStatus == "alive" && hittedObject.GetComponent<PlayerStatus>().GetTeam != player_status.GetTeam) {
            CmdShooting(hittedObject.name);
            player_status.AddKill();
        }
    }

    [Command] void CmdFireSound() {
        GameObject new_sound = Instantiate(fire_sound, balls_spawner.position, Quaternion.identity);
        if (new_sound) NetworkServer.Spawn(new_sound);
    }

    [Command] void CmdJamSound() {
        GameObject new_sound = Instantiate(jam_sound, balls_spawner.position, Quaternion.identity);
        if (new_sound) NetworkServer.Spawn(new_sound);
    }

    [Command] void CmdBalls(Vector3 pos) {
        GameObject new_ball = Instantiate(ball, pos, Quaternion.identity);
        if (new_ball) NetworkServer.Spawn(new_ball);
        RpcBallFlying(new_ball);
    }

    [Command] void CmdCrack(Vector3 pos, Quaternion rot, GameObject parent, GameObject crackObj) {
        GameObject new_crack = Instantiate(crackObj);
        new_crack.transform.position = pos;
        new_crack.transform.rotation = rot;
        new_crack.transform.SetParent(parent.transform);
        if (new_crack) NetworkServer.Spawn(new_crack);
    }

    [ClientRpc] void RpcBallFlying(GameObject new_ball) {
        new_ball.GetComponent<HitChecker>().playerShootingScript = this;
        new_ball.GetComponent<Rigidbody>().AddForce((balls_spawner.transform.forward + deviation) * 20f);
    }

    [Command] void CmdShooting(string player_name) {
        GameManager.GetPlayer(player_name).GetComponent<PlayerStatus>().Penetration();
    }
}
