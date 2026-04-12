using System.Collections;
using UnityEngine;

public class ChoppableCut_Tree : MonoBehaviour
{
    [Header("Health")]
    public float maxHealth = 3f;
    private float currentHealth;

    [Header("Fall Settings")]
    public float fallForce = 500f;
    public float destroyDelay = 5f;

    [Header("Drops (Logs)")]
    public GameObject logPrefab;
    public int dropCount = 3;

    [Header("Drops (Seeds)")]
    public GameObject seedDropPrefab;
    [Range(0f, 1f)] public float seedDropChance = 0.5f;

    [Header("VFX/SFX")]
    public ParticleSystem hitVFX;
    public AudioClip hitSound;
    public AudioClip fallSound;

    [HideInInspector] public SoilTile parentSoil;

    private bool isFalling = false;
    private Rigidbody rb;

    void Start()
    {
        currentHealth = maxHealth;
        rb = GetComponent<Rigidbody>();
        if (rb) rb.isKinematic = true;
    }

    public void GetHit(float damage)
    {
        if (isFalling) return;

        currentHealth -= damage;

        if (hitVFX) hitVFX.Play();

        // [แก้ไข] เปลี่ยนมาใช้ PlaySound2D แทน PlayClipAtPoint
        if (hitSound) PlaySound2D(hitSound);

        DOShake();

        if (currentHealth <= 0)
        {
            StartCoroutine(FallDown());
        }
    }

    IEnumerator FallDown()
    {
        isFalling = true;

        if (rb)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
            rb.AddForce(transform.forward * fallForce);
        }

        // [แก้ไข] เปลี่ยนมาใช้ PlaySound2D แทน PlayClipAtPoint
        if (fallSound) PlaySound2D(fallSound);

        if (parentSoil != null)
        {
            transform.SetParent(null);
            parentSoil.ClearCrop();
            parentSoil = null;
        }

        yield return new WaitForSeconds(2f);
        SpawnLogs();
        Destroy(gameObject, destroyDelay);
    }

    void SpawnLogs()
    {
        Vector3 spawnCenter = transform.position + Vector3.up * 1f;

        if (logPrefab)
        {
            for (int i = 0; i < dropCount; i++)
            {
                SpawnDrop(logPrefab, spawnCenter);
            }
        }

        if (seedDropPrefab && Random.value <= seedDropChance)
        {
            SpawnDrop(seedDropPrefab, spawnCenter);
        }
    }

    void SpawnDrop(GameObject prefab, Vector3 center)
    {
        Vector3 randomPos = center + Random.insideUnitSphere * 0.5f;
        GameObject drop = Instantiate(prefab, randomPos, Quaternion.identity);

        Rigidbody r = drop.GetComponent<Rigidbody>();
        if (r) r.AddExplosionForce(300f, transform.position, 3f);
    }

    private void DOShake() { StartCoroutine(ShakeRoutine()); }

    IEnumerator ShakeRoutine()
    {
        Quaternion originalRot = transform.rotation;
        float elapsed = 0f;
        while (elapsed < 0.2f)
        {
            elapsed += Time.deltaTime;
            float z = Mathf.Sin(elapsed * 50f) * 2f;
            transform.rotation = originalRot * Quaternion.Euler(0, 0, z);
            yield return null;
        }
        transform.rotation = originalRot;
    }

    // [เพิ่มใหม่] ฟังก์ชันสร้างเสียงแบบ 2D (ดังชัดเจน)
    void PlaySound2D(AudioClip clip)
    {
        GameObject go = new GameObject("TreeSFX");
        go.transform.position = transform.position;

        AudioSource src = go.AddComponent<AudioSource>();
        src.clip = clip;
        src.volume = 1f;
        src.spatialBlend = 0f; // <--- 0 คือ 2D (ได้ยินเต็มหู), 1 คือ 3D (ได้ยินตามระยะทาง)

        src.Play();
        Destroy(go, clip.length + 0.1f);
    }
}