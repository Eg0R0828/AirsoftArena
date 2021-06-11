using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityStandardAssets.Characters.FirstPerson;

public class PlayerAnimations : NetworkBehaviour {

    public Animator player_animator;
    CapsuleCollider capsuleCollider;
    CharacterController characterController;
    FirstPersonController fpc_script;
    PlayerSetup player_setup;
    [SerializeField] Camera playerCam;
    float height_coll, height_contr;
    public float crouch_height_coll, crouch_height_contr;

    int down_moving_mode = 0, down_action = 0, up_action = 0;
    int weapon_series = 0, mag_type = 0; bool tactical_grip = false, receiver_cover_modules = false;

    void Start() {
        if (!isLocalPlayer) enabled = false;
        capsuleCollider = gameObject.GetComponent<CapsuleCollider>();
        characterController = gameObject.GetComponent<CharacterController>();
        fpc_script = gameObject.GetComponent<FirstPersonController>();
        height_coll = capsuleCollider.height;
        height_contr = characterController.height;
        player_setup = gameObject.GetComponent<PlayerSetup>();
    }

    void Update() {
        player_animator.SetInteger("down_moving_mode", down_moving_mode);
        player_animator.SetInteger("down_action", down_action);
        player_animator.SetInteger("up_action", up_action);
        player_animator.SetInteger("weapon_series", weapon_series);
        player_animator.SetInteger("mag_type", mag_type);
        player_animator.SetBool("tactical_grip", tactical_grip);
        player_animator.SetBool("receiver_cover_modules", receiver_cover_modules);

        if (Input.GetAxis("Jump") != 0 && !player_setup.pause && !player_setup.inventory && !player_setup.endMatch && up_action != 1 && up_action != 2 && fpc_script.IsJumpingAvailable()) {
            up_action = 0;
            down_moving_mode = 1;
        } else if (down_moving_mode != 1 && up_action != 1 && up_action != 2) {
            down_moving_mode = 0;
            if ((Input.GetAxis("Vertical") != 0.0f || Input.GetAxis("Horizontal") != 0.0f) && !player_setup.pause && !player_setup.inventory &&
                !player_setup.endMatch && up_action != 1 && up_action != 2) {
                up_action = 0;
                down_action = 1;
                if (Input.GetAxis("Run") != 0 && fpc_script.IsRunningAvailable()) down_moving_mode = 2;
                else if (Input.GetAxis("Crouch") != 0) down_moving_mode = 3;
                else down_moving_mode = 0;
            } else {
                if (Input.GetAxis("Crouch") != 0 && !player_setup.pause && !player_setup.inventory && !player_setup.endMatch) down_moving_mode = 3;
                down_action = 0;
            }
        }

        if (down_moving_mode == 3) {
            capsuleCollider.height = crouch_height_coll;
            characterController.height = crouch_height_contr;
            fpc_script.crouching = true;
        } else {
            capsuleCollider.height = height_coll;
            characterController.height = height_contr;
            fpc_script.crouching = false;
        }

        if (down_moving_mode == 2 || down_moving_mode == 1 || ((down_moving_mode == 0 || down_moving_mode == 3) && down_action != 0)) up_action = 0;
        else {
            if (Input.GetKeyDown(KeyCode.R) && player_setup.MagReloadCheck()) up_action = 1;
            else if (Input.GetKeyDown(KeyCode.T) && player_setup.BatteryReloadCheck()) up_action = 2;
            else if (Input.GetAxis("Aim") != 0 && !player_setup.pause && !player_setup.inventory && !player_setup.endMatch) up_action = 3;
            else if (up_action != 1 && up_action != 2) up_action = 0;
        }

        if (up_action == 1 || up_action == 2) {
            player_setup.reloadingAimingIsAvailable = false;
            player_setup.walkingJumpingIsAvailable = false;
            player_setup.runningIsAvailable = false;
        } else {
            player_setup.reloadingAimingIsAvailable = true;
            player_setup.walkingJumpingIsAvailable = true;
            player_setup.runningIsAvailable = true;
        }
        if (down_moving_mode == 2 || down_moving_mode == 1 || up_action == 1 || up_action == 2 || player_setup.pause || player_setup.inventory || player_setup.endMatch)
            player_setup.firingIsAvailable = false;
        else player_setup.firingIsAvailable = true;

        if (up_action == 3) playerCam.nearClipPlane = 0.01f;
        else playerCam.nearClipPlane = 0.075f;
    }

    public void ChangeWeaponAnimationData(string weapon_id) {
        up_action = 0;
        weapon_series = 0;
        tactical_grip = false;
        receiver_cover_modules = false;
        mag_type = 0;
        if (weapon_id == "") return;
        if (weapon_id.IndexOf("Base_AK") != -1) weapon_series = 1;
    }

    public void EndJump() { down_moving_mode = 0; }
    public void EndMagReload() {
        up_action = 0;
        player_setup.MagReload();
    }
    public void EndBatteryReload() {
        up_action = 0;
        player_setup.BatteryReload();
    }
    public void PlayWeaponManipSounds(string audiofileName) {
        LocalWeaponManipSounds(audiofileName);
    }
    [Client] void LocalWeaponManipSounds(string audiofileName) {
        CmdWeaponManipSounds(audiofileName);
    }
    [Command] void CmdWeaponManipSounds(string audiofileName) {
        GameObject sound;
        sound = Instantiate(Resources.Load<GameObject>(audiofileName), transform.position, Quaternion.identity);
        if (sound) NetworkServer.Spawn(sound);
    }
}
