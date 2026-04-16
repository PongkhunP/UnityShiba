using UnityEngine;

/// <summary>
/// ติดไว้บน GameObject ที่เก็บได้จากพื้น
/// - มี ItemSO + จำนวน
/// - ลอยขึ้นลง (Floating animation)
/// - เล่น VFX + SFX ตอนเก็บ
/// </summary>
public class Pickupable : MonoBehaviour
{
    [Header("Item")]
    public ItemSO itemData;
    [Min(1)]
    public int amount = 1;

    [Header("Floating Animation")]
    public bool enableFloat = true;
    public float floatAmplitude = 0.15f;   // ความสูงที่ลอยขึ้นลง
    public float floatSpeed     = 2f;       // ความเร็วการลอย
    public float rotateSpeed    = 90f;      // หมุนกี่องศาต่อวินาที (0 = ไม่หมุน)

    [Header("VFX / SFX")]
    [Tooltip("Particle ที่เล่นตอนเก็บ — ถ้าว่างจะข้าม")]
    public GameObject pickupVFX;
    [Tooltip("เสียงตอนเก็บ")]
    public AudioClip pickupSFX;
    [Range(0f, 1f)]
    public float sfxVolume = 0.8f;

    // Runtime
    Vector3 _startPos;
    float   _floatTimer;

    void Start()
    {
        _startPos    = transform.position;
        _floatTimer  = Random.Range(0f, Mathf.PI * 2f); // offset ให้แต่ละ item ไม่ลอยพร้อมกัน
    }

    void Update()
    {
        if (!enableFloat) return;

        // ลอยขึ้นลง
        _floatTimer += Time.deltaTime * floatSpeed;
        float newY = _startPos.y + Mathf.Sin(_floatTimer) * floatAmplitude;
        transform.position = new Vector3(transform.position.x, newY, transform.position.z);

        // หมุน
        if (rotateSpeed != 0f)
            transform.Rotate(Vector3.up, rotateSpeed * Time.deltaTime);
    }

    /// <summary>เรียกจาก PlayerPickup ก่อน Destroy เพื่อเล่น VFX/SFX</summary>
    public void PlayPickupEffects()
    {
        // VFX
        if (pickupVFX != null)
            Instantiate(pickupVFX, transform.position, Quaternion.identity);

        // SFX
        if (pickupSFX != null)
            AudioSource.PlayClipAtPoint(pickupSFX, transform.position, sfxVolume);
    }
}
