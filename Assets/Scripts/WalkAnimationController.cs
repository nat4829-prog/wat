using UnityEngine;

/// <summary>
/// ควบคุมอนิเมชันเดิน/ยืน/วิ่ง จากความเร็วที่ตัวละครเคลื่อนที่จริง
/// ใช้ได้กับ Player (CharacterController / Rigidbody / สคริปต์เดินอะไรก็ได้) และศัตรู (NavMeshAgent)
/// เพราะวัดจากการเปลี่ยนตำแหน่งของวัตถุ ไม่ต้องไปแก้สคริปต์เดินเดิม
///
/// วิธีใช้: แปะที่วัตถุที่มี Animator (หรือให้ลาก Animator ใส่ช่อง Animator)
/// ใน Animator Controller สร้างพารามิเตอร์ตามชื่อที่ตั้งไว้ในสคริปต์นี้ (ตัวที่ไม่มีจะถูกข้ามเอง ไม่ขึ้น Error)
///   Speed      (Float) : ความเร็วปัจจุบัน ใช้ทำ Blend Tree Idle -> Walk -> Run
///   IsWalking  (Bool)  : true เมื่อกำลังเดิน ใช้เป็นเงื่อนไขเปลี่ยนสถานะ Idle <-> Walk
///   IsRunning  (Bool)  : true เมื่อเร็วเกินความเร็วเดิน
/// </summary>
[DisallowMultipleComponent]
public class WalkAnimationController : MonoBehaviour
{
    [Header("อ้างอิง")]
    [Tooltip("เว้นว่างไว้ ระบบหา Animator ที่วัตถุนี้หรือลูกให้เอง")]
    public Animator animator;

    [Header("ชื่อพารามิเตอร์ใน Animator")]
    public string speedParam = "Speed";
    public string isWalkingParam = "IsWalking";
    public string isRunningParam = "IsRunning";

    [Header("เกณฑ์ความเร็ว (เมตร/วินาที)")]
    [Tooltip("ช้ากว่านี้ถือว่ายืนนิ่ง")]
    public float walkThreshold = 0.1f;
    [Tooltip("เร็วกว่านี้ถือว่ากำลังวิ่ง")]
    public float runThreshold = 4.5f;

    [Header("ความนุ่มนวล")]
    [Tooltip("ยิ่งมากยิ่งเปลี่ยนเร็ว ยิ่งน้อยยิ่งนุ่ม (กันอนิเมชันกระตุก)")]
    public float smoothing = 10f;
    [Tooltip("ปรับความเร็วเล่นอนิเมชันตามความเร็วจริง ให้เท้าไม่ไถลพื้น")]
    public bool scaleAnimationSpeed = false;
    [Tooltip("ความเร็วเดินที่อนิเมชันถูกทำมาให้พอดี (ใช้กับ Scale Animation Speed)")]
    public float referenceWalkSpeed = 2f;
    public Vector2 animationSpeedRange = new Vector2(0.6f, 1.6f);

    [Header("อื่น ๆ")]
    [Tooltip("นับเฉพาะการเคลื่อนที่บนพื้น (X,Z) ไม่รวมการตก/กระโดด")]
    public bool ignoreVertical = true;

    Vector3 lastPos;
    float smoothedSpeed;
    int speedHash, walkHash, runHash;
    bool hasSpeed, hasWalk, hasRun;

    /// <summary>ความเร็วที่คำนวณได้ตอนนี้ (หลังทำให้นุ่มแล้ว)</summary>
    public float CurrentSpeed { get { return smoothedSpeed; } }
    public bool IsWalking { get { return smoothedSpeed > walkThreshold; } }
    public bool IsRunning { get { return smoothedSpeed > runThreshold; } }

    void Awake()
    {
        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (animator == null)
        {
            Debug.LogWarning("[WalkAnimationController] ไม่พบ Animator ที่ " + name + " (ลากใส่ช่อง Animator ได้)");
            enabled = false;
            return;
        }

        speedHash = Animator.StringToHash(speedParam);
        walkHash = Animator.StringToHash(isWalkingParam);
        runHash = Animator.StringToHash(isRunningParam);
        CacheParameters();
        lastPos = transform.position;
    }

    // เช็กว่ามีพารามิเตอร์ชื่อนั้นจริง ไม่งั้น Unity จะเตือนทุกเฟรม
    void CacheParameters()
    {
        hasSpeed = hasWalk = hasRun = false;
        foreach (AnimatorControllerParameter p in animator.parameters)
        {
            if (p.nameHash == speedHash && p.type == AnimatorControllerParameterType.Float) hasSpeed = true;
            else if (p.nameHash == walkHash && p.type == AnimatorControllerParameterType.Bool) hasWalk = true;
            else if (p.nameHash == runHash && p.type == AnimatorControllerParameterType.Bool) hasRun = true;
        }
    }

    void OnEnable()
    {
        lastPos = transform.position;
        smoothedSpeed = 0f;
    }

    void Update()
    {
        float dt = Time.deltaTime;
        if (dt <= 0f) return;                            // เกมหยุด (timeScale = 0) ไม่ต้องอัปเดต

        Vector3 delta = transform.position - lastPos;
        lastPos = transform.position;
        if (ignoreVertical) delta.y = 0f;

        float rawSpeed = delta.magnitude / dt;
        smoothedSpeed = Mathf.Lerp(smoothedSpeed, rawSpeed, 1f - Mathf.Exp(-smoothing * dt));
        if (smoothedSpeed < 0.01f) smoothedSpeed = 0f;

        bool walking = smoothedSpeed > walkThreshold;
        bool running = smoothedSpeed > runThreshold;

        if (hasSpeed) animator.SetFloat(speedHash, smoothedSpeed);
        if (hasWalk) animator.SetBool(walkHash, walking);
        if (hasRun) animator.SetBool(runHash, running);

        if (scaleAnimationSpeed)
        {
            float k = walking && referenceWalkSpeed > 0.01f ? smoothedSpeed / referenceWalkSpeed : 1f;
            animator.speed = Mathf.Clamp(k, animationSpeedRange.x, animationSpeedRange.y);
        }
    }
}
