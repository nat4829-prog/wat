using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// AI ซอมบี้: เดินสุ่ม -> เห็น Player แล้ววิ่งไล่ -> ถึงระยะแล้วโจมตีลดเลือด
/// แปะที่ FreeZombie (ต้องมี NavMeshAgent และต้อง Bake NavMesh ในแมพก่อน)
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class ZombieAI : MonoBehaviour
{
    private enum State { Wander, Chase, Attack }

    [Header("Target")]
    [SerializeField] private Transform player;          // ถ้าเว้นว่างจะหาจาก Tag = Player

    [Header("Detection")]
    [SerializeField] private float sightRange = 12f;    // ระยะมองเห็น
    [SerializeField, Range(10f, 360f)] private float fieldOfView = 120f;
    [SerializeField] private float senseRange = 3f;     // ใกล้กว่านี้รู้ตัวทันที แม้อยู่ข้างหลัง
    [SerializeField] private float loseSightTime = 3f;  // เสียเป้านานเท่านี้แล้วเลิกไล่
    [SerializeField] private float eyeHeight = 1.6f;
    [SerializeField] private float targetHeight = 1.0f;

    [Header("Movement")]
    [SerializeField] private float walkSpeed = 1.5f;
    [SerializeField] private float runSpeed = 3.8f;
    [SerializeField] private float wanderRadius = 20f;
    [SerializeField] private Vector2 wanderWaitTime = new Vector2(1f, 4f);

    [Header("Attack")]
    [SerializeField] private float attackRange = 1.8f;
    [SerializeField] private float attackDamage = 10f;
    [SerializeField] private float attackCooldown = 1.2f;

    [Header("Animation (ไม่บังคับ)")]
    [SerializeField] private Animator animator;        // Float "Speed", Trigger "Attack"

    private NavMeshAgent agent;
    private PlayerHealth playerHealth;
    private State state = State.Wander;
    private float lastSeenTime = -999f;
    private float nextAttackTime;
    private float wanderWaitTimer;
    private bool hasSpeedParam, hasAttackParam;

    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int AttackHash = Animator.StringToHash("Attack");

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
        CacheAnimatorParams();
    }

    private void Start()
    {
        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }
        if (player != null) playerHealth = player.GetComponentInParent<PlayerHealth>();

        agent.speed = walkSpeed;
        PickWanderPoint();
    }

    private void Update()
    {
        if (player == null) return;

        bool playerAlive = playerHealth == null || !playerHealth.IsDead;
        bool canSee = playerAlive && CanSeePlayer();
        if (canSee) lastSeenTime = Time.time;

        float dist = Vector3.Distance(transform.position, player.position);

        switch (state)
        {
            case State.Wander:
                if (canSee) { EnterChase(); break; }
                UpdateWander();
                break;

            case State.Chase:
                if (!playerAlive || Time.time - lastSeenTime > loseSightTime) { EnterWander(); break; }
                if (dist <= attackRange) { state = State.Attack; agent.isStopped = true; break; }
                agent.SetDestination(player.position);
                break;

            case State.Attack:
                if (!playerAlive) { EnterWander(); break; }
                if (dist > attackRange * 1.2f) { EnterChase(); break; }
                FacePlayer();
                if (Time.time >= nextAttackTime) DoAttack();
                break;
        }

        UpdateAnimator();
    }

    // ---------- Detection ----------
    private bool CanSeePlayer()
    {
        Vector3 eye = transform.position + Vector3.up * eyeHeight;
        Vector3 target = player.position + Vector3.up * targetHeight;
        Vector3 toPlayer = target - eye;
        float dist = toPlayer.magnitude;

        if (dist > sightRange) return false;

        // ใกล้มากๆ ถือว่าได้ยิน/รู้สึกตัว ไม่ต้องอยู่ในมุมมอง
        bool inView = dist <= senseRange ||
                      Vector3.Angle(transform.forward, toPlayer) <= fieldOfView * 0.5f;
        if (!inView) return false;

        // เช็คว่ามีกำแพงบังไหม
        if (Physics.Raycast(eye, toPlayer.normalized, out RaycastHit hit, dist + 0.5f,
                            ~0, QueryTriggerInteraction.Ignore))
        {
            return hit.transform.root == player.root;
        }
        return false;
    }

    // ---------- States ----------
    private void EnterChase()
    {
        state = State.Chase;
        agent.isStopped = false;
        agent.speed = runSpeed;
    }

    private void EnterWander()
    {
        state = State.Wander;
        agent.isStopped = false;
        agent.speed = walkSpeed;
        PickWanderPoint();
    }

    private void UpdateWander()
    {
        if (agent.pathPending) return;

        if (agent.remainingDistance <= agent.stoppingDistance + 0.3f)
        {
            wanderWaitTimer -= Time.deltaTime;
            if (wanderWaitTimer <= 0f) PickWanderPoint();
        }
    }

    private void PickWanderPoint()
    {
        wanderWaitTimer = Random.Range(wanderWaitTime.x, wanderWaitTime.y);

        for (int i = 0; i < 10; i++)
        {
            Vector3 random = transform.position + Random.insideUnitSphere * wanderRadius;
            if (NavMesh.SamplePosition(random, out NavMeshHit hit, 5f, NavMesh.AllAreas))
            {
                agent.SetDestination(hit.position);
                return;
            }
        }
    }

    private void FacePlayer()
    {
        Vector3 dir = player.position - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.01f) return;
        Quaternion look = Quaternion.LookRotation(dir);
        transform.rotation = Quaternion.Slerp(transform.rotation, look, 10f * Time.deltaTime);
    }

    private void DoAttack()
    {
        nextAttackTime = Time.time + attackCooldown;
        if (animator != null && hasAttackParam) animator.SetTrigger(AttackHash);

        // ถ้าใช้ Animation Event ให้ย้ายบรรทัดนี้ไปเรียกจาก Event แทน
        if (playerHealth != null) playerHealth.TakeDamage(attackDamage);
    }

    // ---------- Animator ----------
    private void CacheAnimatorParams()
    {
        if (animator == null) return;
        foreach (var p in animator.parameters)
        {
            if (p.nameHash == SpeedHash && p.type == AnimatorControllerParameterType.Float) hasSpeedParam = true;
            if (p.nameHash == AttackHash && p.type == AnimatorControllerParameterType.Trigger) hasAttackParam = true;
        }
    }

    private void UpdateAnimator()
    {
        if (animator != null && hasSpeedParam)
            animator.SetFloat(SpeedHash, agent.velocity.magnitude, 0.1f, Time.deltaTime);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow; Gizmos.DrawWireSphere(transform.position, sightRange);
        Gizmos.color = Color.red;    Gizmos.DrawWireSphere(transform.position, attackRange);
        Gizmos.color = Color.cyan;   Gizmos.DrawWireSphere(transform.position, senseRange);
    }
}
