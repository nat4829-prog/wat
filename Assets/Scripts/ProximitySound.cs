using UnityEngine;

/// <summary>
/// เสียงที่ดังเมื่อ Player เดินเข้าระยะที่กำหนด แปะที่วัตถุอะไรก็ได้ (ศัตรู, ไอเท็ม, ประตู, จุดว่างเปล่า)
/// - ต้องมี Player ที่ตั้ง Tag = "Player"
/// - ปรับความดังได้ที่ช่อง Volume (0-1) และเลือกให้เสียงค่อย ๆ ดังขึ้นเมื่อเข้าใกล้ได้
/// - ไม่ต้องใช้ Collider หรือ AudioSource ใส่เอง สคริปต์สร้าง AudioSource ให้
/// - เปลี่ยนความดังจากโค้ดได้: GetComponent&lt;ProximitySound&gt;().Volume = 0.5f;
/// </summary>
public class ProximitySound : MonoBehaviour
{
    public enum PlayMode
    {
        OncePerEnter,   // เล่น 1 ครั้งทุกครั้งที่เดินเข้าระยะ
        OnlyFirstTime,  // เล่นแค่ครั้งแรกครั้งเดียวตลอดเกม
        LoopWhileInside // เล่นวนไปเรื่อย ๆ ขณะอยู่ในระยะ หยุดเมื่อออกนอกระยะ
    }

    [Header("เสียง")]
    public AudioClip clip;
    [Range(0f, 1f)] public float volume = 0.8f;
    public PlayMode mode = PlayMode.OncePerEnter;

    [Header("ระยะ")]
    [Tooltip("ระยะที่เริ่มมีเสียง (หน่วยเมตร)")]
    public float triggerRadius = 6f;
    [Tooltip("นับเฉพาะระยะบนพื้น (แกน X,Z) ไม่สนความสูง")]
    public bool ignoreHeight = true;
    [Tooltip("ให้ความดังค่อย ๆ เพิ่มเมื่อเข้าใกล้ (ไกลสุดเบา ใกล้สุดดังเต็ม Volume)")]
    public bool fadeByDistance = false;

    [Header("อื่น ๆ")]
    [Tooltip("ห้ามเล่นซ้ำภายในกี่วินาที หลังเล่นล่าสุด (เฉพาะโหมด OncePerEnter)")]
    public float cooldown = 1f;
    [Tooltip("เปลี่ยนเป็น 0 ถ้าอยากให้เสียงเป็น 2D ฟังเท่ากันทุกทิศ, 1 = เสียง 3D มีทิศทาง")]
    [Range(0f, 1f)] public float spatialBlend = 0f;
    public Transform player;                 // เว้นว่างไว้ ระบบหาเองจาก Tag "Player"
    public bool showGizmo = true;

    AudioSource source;
    bool inside;
    bool played;
    float lastPlayTime = -999f;

    /// <summary>ความดังปัจจุบัน (0-1) ตั้งค่าจากโค้ดได้</summary>
    public float Volume
    {
        get { return volume; }
        set { volume = Mathf.Clamp01(value); }
    }

    void Awake()
    {
        source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.spatialBlend = spatialBlend;
        source.loop = false;
    }

    void Update()
    {
        if (clip == null) return;

        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p == null) return;
            player = p.transform;
        }

        float dist = Distance(player.position);
        bool nowInside = dist <= triggerRadius;

        float vol = volume;
        if (fadeByDistance && triggerRadius > 0.001f)
            vol *= Mathf.Clamp01(1f - dist / triggerRadius);

        if (nowInside && !inside) OnEnter(vol);
        else if (!nowInside && inside) OnExit();

        inside = nowInside;

        // ปรับความดังตามระยะ/ตามค่าที่ผู้เล่นแก้ ระหว่างเสียงกำลังเล่น
        if (source.isPlaying) source.volume = vol;
    }

    void OnEnter(float vol)
    {
        switch (mode)
        {
            case PlayMode.OnlyFirstTime:
                if (played) return;
                break;
            case PlayMode.OncePerEnter:
                if (Time.time - lastPlayTime < cooldown) return;
                break;
        }

        played = true;
        lastPlayTime = Time.time;
        source.spatialBlend = spatialBlend;
        source.clip = clip;
        source.volume = vol;
        source.loop = (mode == PlayMode.LoopWhileInside);
        source.Play();
    }

    void OnExit()
    {
        if (mode == PlayMode.LoopWhileInside) source.Stop();    // โหมดอื่นให้เล่นจนจบเสียงเอง
    }

    float Distance(Vector3 playerPos)
    {
        Vector3 d = playerPos - transform.position;
        if (ignoreHeight) d.y = 0f;
        return d.magnitude;
    }

    void OnDrawGizmosSelected()
    {
        if (!showGizmo) return;
        Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.9f);
        Gizmos.DrawWireSphere(transform.position, triggerRadius);
    }
}
