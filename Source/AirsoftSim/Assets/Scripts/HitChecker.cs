using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class HitChecker : NetworkBehaviour {

    public Shooting playerShootingScript;
    [SerializeField] LayerMask mask;
    bool hitted = false;

    void OnCollisionEnter(Collision collision) {
        // Если это первое столкновение шара с объектом на сцене; определен лок. игрок, который произвел выстрел; также объект не прин. к игнорируемым слоям
        if (!hitted && playerShootingScript && playerShootingScript.isLocalPlayer && (mask.value & (1 << collision.gameObject.layer)) != 0) {
            hitted = true; // Попадание совершено - ост. коллизии будут проигнорированы
            ContactPoint collisionContactPoint = collision.GetContact(0); // Ссылка на точку коллизии (соприкосновения)
            Vector3 pos = collisionContactPoint.point + collisionContactPoint.normal * 0.03f; // Расчет позиции для размещения объекта "трещины" от попадания
            Quaternion rot = Quaternion.LookRotation(-collisionContactPoint.normal); // Расчет поворота объекта "трещины" от попадания
            playerShootingScript.LocalHittedObjectProccessing(collision.gameObject, pos, rot); // Команда лок. игроку с данными об объекте, в который сов. попадание
        }
    }
}
