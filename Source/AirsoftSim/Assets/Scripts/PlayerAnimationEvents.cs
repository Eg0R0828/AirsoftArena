using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityStandardAssets.Characters.FirstPerson;

public class PlayerAnimationEvents : MonoBehaviour {

    [SerializeField] PlayerAnimations player_animations;
    [SerializeField] FirstPersonController fpc_script;

    public void Rest() { fpc_script.Rest(); }

    public void EndJump() { player_animations.EndJump(); }
    public void EndMagReload() { player_animations.EndMagReload(); }
    public void EndBatteryReload() { player_animations.EndBatteryReload(); }
    public void PlayLeftStepSound() { fpc_script.PlayFootStepSound(true); }
    public void PlayRightStepSound() { fpc_script.PlayFootStepSound(false); }
    public void InsertMag() { player_animations.PlayWeaponManipSounds("InsertMagSound"); }
    public void PullOutMag() { player_animations.PlayWeaponManipSounds("PullOutMagSound"); }
    public void InsertReceiverCover() { player_animations.PlayWeaponManipSounds("InsertReceiverCoverSound"); }
    public void PullOutReceiverCover() { player_animations.PlayWeaponManipSounds("PullOutReceiverCoverSound"); }
    public void InsertBattery() { player_animations.PlayWeaponManipSounds("InsertBatterySound"); }
    public void PullOutBattery() { player_animations.PlayWeaponManipSounds("PullOutBatterySound"); }
    public void BunkerMagClicks() { player_animations.PlayWeaponManipSounds("BunkerMagClicksSound"); }
}
