using UnityEngine;

/// <summary>
/// โพชั่นวิ่งเร็ว — แปะที่ Prefab โพชั่น และติ๊ก Is Trigger ที่ Collider
/// </summary>
[RequireComponent(typeof(Collider))]
public class SpeedPotion : MonoBehaviour
{
    [SerializeField] private float speedMultiplier = 1.5f;  // คูณความเร็ว 1.5 เท่า
    [SerializeField] private float duration = 8f;           // นานกี่วินาที
    [SerializeField] private AudioClip pickupSound;
    [SerializeField] private GameObject pickupEffect;

    private void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        // เพิ่ม/หา PlayerSpeedBuff ที่ตัว Player (ไม่ใช้ Coroutine ที่โพชั่น เพราะโพชั่นจะถูกลบ)
        GameObject player = other.transform.root.gameObject;
        PlayerSpeedBuff buff = player.GetComponent<PlayerSpeedBuff>();
        if (buff == null) buff = player.AddComponent<PlayerSpeedBuff>();

        buff.Apply(speedMultiplier, duration);

        if (pickupSound != null)
            AudioSource.PlayClipAtPoint(pickupSound, transform.position);
        if (pickupEffect != null)
            Instantiate(pickupEffect, transform.position, Quaternion.identity);

        Destroy(gameObject);
    }
}
