using UnityEngine;

public class ItemMagnet : MonoBehaviour
{
    [Header("Settings")]
    public float delayBeforeMagnet = 0.5f; // รอแป๊บนึงค่อยดูด (ให้มันกระจายก่อน)
    public float magnetSpeed = 10f;
    public float pickupRadius = 1f; // ระยะที่จะถือว่าเก็บได้
    public ItemSO itemToGive;       // ไอเทมที่จะเข้ากระเป๋า
    public int amount = 1;

    private Transform player;
    private float spawnTime;
    private bool isSucking = false;

    void Start()
    {
        spawnTime = Time.time;
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p) player = p.transform;
    }

    void Update()
    {
        if (!player) return;

        // รอเวลาดีเลย์ก่อนค่อยเริ่มดูด
        if (Time.time < spawnTime + delayBeforeMagnet) return;

        // คำนวณระยะห่าง
        float dist = Vector3.Distance(transform.position, player.position);

        // ถ้าอยู่ใกล้มาก -> เก็บของเข้าตัว
        if (dist <= pickupRadius)
        {
            Collect();
            return;
        }

        // เคลื่อนที่เข้าหาผู้เล่น
        transform.position = Vector3.MoveTowards(transform.position, player.position + Vector3.up, magnetSpeed * Time.deltaTime);
    }

    void Collect()
    {
        if (InventoryUI.Instance)
        {
            InventoryUI.Instance.AddItemToInventory(itemToGive, amount);
        }
        else if (HotbarUI.Instance)
        {
            HotbarUI.Instance.AddItemToFirstEmptySlot(itemToGive, amount);
        }

        // เล่นเสียงเก็บของ (ถ้ามี) แล้วทำลาย object ทิ้ง
        Destroy(gameObject);
    }
}