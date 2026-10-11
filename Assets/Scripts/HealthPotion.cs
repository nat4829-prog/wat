using UnityEngine;

/// <summary>
/// โพชั่นฮีลเลือด — แปะที่ Prefab โพชั่น และติ๊ก Is Trigger ที่ Collider
/// </summary>
[RequireComponent(typeof(Collider))]
public class HealthPotion : MonoBehaviour
{
    [SerializeField] private float healAmount = 30f;
    [SerializeField] private AudioClip pickupSound;
    [SerializeField] private GameObject pickupEffect;

    private void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        PlayerHealth health = other.GetComponentInParent<PlayerHealth>();
        if (health == null || health.IsDead) return;

        // เลือดเต็มอยู่แล้วไม่ต้องเก็บ (ลบเงื่อนไขนี้ถ้าอยากให้เก็บได้เสมอ)
        if (health.CurrentHealth >= health.MaxHealth) return;

        health.Heal(healAmount);

        if (pickupSound != null)
            AudioSource.PlayClipAtPoint(pickupSound, transform.position);
        if (pickupEffect != null)
            Instantiate(pickupEffect, transform.position, Quaternion.identity);

        Destroy(gameObject);
    }
}
