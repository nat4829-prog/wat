using UnityEngine;

[RequireComponent(typeof(Collider))]
public class Coin : MonoBehaviour
{
    [Header("Settings")]
    public int scoreValue = 1;
    public float rotateSpeed = 120f;
    public float bobSpeed = 2f;
    public float bobHeight = 0.15f;

    [Header("Optional")]
    public AudioClip collectSound;

    private Vector3 startPos;

    private void Reset()
    {
        // ตั้งให้ Collider เป็น Trigger อัตโนมัติ
        GetComponent<Collider>().isTrigger = true;
    }

    private void Start()
    {
        startPos = transform.position;
        GetComponent<Collider>().isTrigger = true;

        // ลงทะเบียนให้มินิแมพรู้จักเหรียญนี้
        if (MinimapManager.Instance != null)
            MinimapManager.Instance.RegisterCoin(this);
    }

    private void Update()
    {
        // หมุนและลอยขึ้นลงให้ดูน่าเก็บ
        transform.Rotate(Vector3.up, rotateSpeed * Time.deltaTime, Space.World);
        float y = Mathf.Sin(Time.time * bobSpeed) * bobHeight;
        transform.position = startPos + new Vector3(0f, y, 0f);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        if (ScoreManager.Instance != null)
            ScoreManager.Instance.AddScore(scoreValue);

        if (collectSound != null)
            AudioSource.PlayClipAtPoint(collectSound, transform.position);

        if (MinimapManager.Instance != null)
            MinimapManager.Instance.UnregisterCoin(this);

        Destroy(gameObject); // เหรียญหายไป
    }
}