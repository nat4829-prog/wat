using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// แปะบนวัตถุเหรียญแต่ละเหรียญ (หรือบน Prefab เหรียญ ที่วัตถุบนสุด)
/// เมื่อผู้เล่น (Tag "Player") เดินเข้ามาใกล้ เหรียญจะ:
///   1. เพิ่มคะแนนใน ScoreManager
///   2. หายจากโลก 3D และหายจากมินิแมปทันที (MinimapMarker ปิดตัวเองตอนวัตถุถูกทำลาย)
/// ตรวจด้วยระยะห่าง จึงไม่ต้องตั้ง Collider หรือ Rigidbody
/// </summary>
public class CoinPickup : MonoBehaviour
{
    [Tooltip("คะแนนที่ได้ต่อเหรียญ")]
    public int points = 10;

    [Tooltip("ระยะที่ถือว่าเก็บได้ (หน่วยเมตร วัดบนพื้นราบ)")]
    public float pickupRadius = 1.3f;

    [Tooltip("เปิด = ทำลายวัตถุ / ปิด = แค่ซ่อน (SetActive false)")]
    public bool destroyOnCollect = true;

    [Header("ไม่บังคับ")]
    public AudioClip pickupSound;
    [Range(0f, 1f)]
    public float soundVolume = 0.8f;
    public GameObject pickupEffect;          // Particle Prefab ที่จะเกิดตรงตำแหน่งเหรียญ

    /// <summary>เหรียญที่ยังเก็บไม่ได้และเปิดใช้งานอยู่ตอนนี้ทั้งหมด (อัปเดตเองเมื่อมีเหรียญเกิด/ถูกเก็บ/ถูกลบ/ถูกปิด)</summary>
    public static readonly List<CoinPickup> Active = new List<CoinPickup>();

    /// <summary>จำนวนเหรียญที่ต้องเก็บในตอนนี้</summary>
    public static int Remaining { get { return Active.Count; } }

    /// <summary>ถูกเรียกทุกครั้งที่จำนวนเหรียญที่ต้องเก็บเปลี่ยน</summary>
    public static event Action CountChanged;

    static Transform player;
    static float nextFindTime;

    bool collected;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        player = null;
        nextFindTime = 0f;
        Active.Clear();
        CountChanged = null;
    }

    static void RaiseCountChanged()
    {
        if (CountChanged != null) CountChanged();
    }

    void OnEnable()
    {
        if (collected) return;
        Active.Add(this);
        RaiseCountChanged();
    }

    void OnDisable()
    {
        Unregister();
    }

    void Unregister()
    {
        if (Active.Remove(this)) RaiseCountChanged();
    }

    static Transform GetPlayer()
    {
        if (player == null && Time.unscaledTime >= nextFindTime)
        {
            nextFindTime = Time.unscaledTime + 0.5f;     // ค้นหาแค่ครึ่งวินาทีละครั้ง
            GameObject go = GameObject.FindGameObjectWithTag("Player");
            if (go != null) player = go.transform;
        }
        return player;
    }

    void Update()
    {
        if (collected) return;
        Transform p = GetPlayer();
        if (p == null) return;

        float dx = p.position.x - transform.position.x;
        float dz = p.position.z - transform.position.z;
        if (dx * dx + dz * dz <= pickupRadius * pickupRadius) Collect();
    }

    void Collect()
    {
        collected = true;
        Unregister();                                    // ตัวเลข "ที่ต้องเก็บ" ลดลงทันที ไม่ต้องรอ Destroy

        ScoreManager.GetOrCreate().AddCoin(points);

        if (pickupSound != null) AudioSource.PlayClipAtPoint(pickupSound, transform.position, soundVolume);
        if (pickupEffect != null) Instantiate(pickupEffect, transform.position, Quaternion.identity);

        // วัตถุถูกปิด/ทำลาย -> MinimapMarker.OnDisable ทำงาน -> ไอคอนหายจากแผนที่
        if (destroyOnCollect) Destroy(gameObject);
        else gameObject.SetActive(false);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.84f, 0.1f, 0.8f);
        Gizmos.DrawWireSphere(transform.position, pickupRadius);
    }
}
