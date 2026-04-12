using UnityEngine;

public class SoilTile : MonoBehaviour
{
    [Header("State")]
    public bool isTilled;
    public bool isWatered;

    [Header("Crop Runtime")]
    public CropSO crop;
    public int stageIndex;
    public float stageTimer;

    [Header("Refs")]
    [Tooltip("จุดเกิดต้นไม้ (ถ้าเว้นว่างจะใช้ตำแหน่ง GameObject นี้)")]
    public Transform cropParent;

    [Header("Ground Visuals")]
    [Tooltip("จุดเกิด prefab ดิน (ถ้าเว้นว่างจะใช้ตำแหน่ง GameObject นี้)")]
    public Transform groundParent;

    [Tooltip("Prefab ดินแห้ง (หลังพวนดิน)")]
    public GameObject groundDryPrefab;

    [Tooltip("Prefab ดินเปียก (หลังรดน้ำ)")]
    public GameObject groundWetPrefab;

    private GameObject currentCropObj;
    private GameObject currentGroundObj;

    // ================== STATE CHANGE ==================

    /// <summary>
    /// พวนดินครั้งแรก -> ให้ดินกลายเป็นดินแห้ง
    /// </summary>
    public void Till()
    {
        if (isTilled) return;           // ไถแค่ครั้งแรก
        isTilled = true;
        isWatered = false;
        UpdateGroundVisual();
    }

    /// <summary>
    /// รดน้ำ -> ต้องไถก่อนถึงจะรดได้, แล้วเปลี่ยนดินเป็นดินเปียก
    /// </summary>
    public void Water()
    {
        if (!isTilled) return;          // ยังไม่พวนดิน ห้ามรด
        isWatered = true;
        UpdateGroundVisual();
    }

    public bool CanPlant(CropSO c)
    {
        // ยังใช้เงื่อนไขเดิม: ต้องไถแล้ว, ไม่มีพืชอยู่, และ Crop ไม่เป็น null
        // ถ้าอยากให้ "ต้องรดน้ำก่อนปลูก" ให้เพิ่ม && isWatered เข้าไปได้
        return isTilled && crop == null && c != null;
    }

    public void Plant(CropSO c)
    {
        if (!CanPlant(c)) return;

        crop = c;
        stageIndex = 0;
        stageTimer = 0f;
        // ไม่รีเซ็ต isWatered เพื่อให้เลือก flow ได้ทั้ง
        // - ไถ -> ปลูก -> รดน้ำ (แบบ Stardew)
        // - ไถ -> รดน้ำ -> ปลูก (แบบที่คุณอยากทำ)
        SpawnCropStage();
    }

    // ================== HARVEST ==================

    public bool CanHarvest()
    {
        if (crop == null) return false;
        return stageIndex >= crop.growthPrefabs.Length - 1 && crop.harvestItem != null;
    }

    public int HarvestToInventory(System.Func<ItemSO, int, bool> addFunc)
    {
        if (!CanHarvest()) return 0;

        int amount = Random.Range(crop.yieldRange.x, crop.yieldRange.y + 1);
        amount = Mathf.Max(0, amount);

        bool added = addFunc?.Invoke(crop.harvestItem, amount) ?? false;

        if (added && crop.destroyOnHarvest)
        {
            // เก็บแล้วดินกลับเป็น "ยังไม่พวน"
            ClearCrop();
        }
        else if (added)
        {
            // ถ้าไม่ทำลาย ให้ย้อนกลับไป stage ก่อนสุดท้าย
            stageIndex = Mathf.Max(0, crop.growthPrefabs.Length - 2);
            stageTimer = 0f;
            isWatered = false;
            SpawnCropStage();
        }

        return added ? amount : 0;
    }

    public void ClearCrop()
    {
        crop = null;
        stageIndex = 0;
        stageTimer = 0f;
        isWatered = false;
        isTilled = false;

        if (currentCropObj) Destroy(currentCropObj);
        currentCropObj = null;

        UpdateGroundVisual(); // เคลียร์ดินให้หายไปด้วย
    }

    // ================== UPDATE GROWTH ==================

    void Update()
    {
        if (crop == null) return;
        if (!isTilled) return;

        // โตได้เฉพาะตอนที่รดน้ำแล้ว (ถ้า Crop ระบุว่าต้องการ)
        bool canGrow = !crop.requiresWaterEachStage || isWatered;
        if (!canGrow) return;

        stageTimer += Time.deltaTime;
        float target = crop.stageDurations[Mathf.Clamp(stageIndex, 0, crop.stageDurations.Length - 1)];

        if (stageTimer >= target)
        {
            stageTimer = 0f;
            isWatered = false;

            if (stageIndex < crop.growthPrefabs.Length - 1)
            {
                stageIndex++;
                SpawnCropStage();
            }
        }
    }

    void SpawnCropStage()
    {
        if (currentCropObj) Destroy(currentCropObj);

        if (crop != null && stageIndex < crop.growthPrefabs.Length)
        {
            var prefab = crop.growthPrefabs[stageIndex];
            if (!prefab) return;

            Transform parent = cropParent ? cropParent : transform;
            currentCropObj = Instantiate(
                prefab,
                parent.position,
                Quaternion.identity,
                parent
            );
        }
    }

    // ================== GROUND VISUALS ==================

    void UpdateGroundVisual()
    {
        if (currentGroundObj) Destroy(currentGroundObj);

        if (!isTilled)
        {
            // ยังไม่พวนดิน -> ไม่มี prefab ดิน
            return;
        }

        GameObject prefab = isWatered ? groundWetPrefab : groundDryPrefab;
        if (!prefab) return;

        Transform parent = groundParent ? groundParent : transform;
        currentGroundObj = Instantiate(prefab, parent.position, Quaternion.identity, parent);
    }

    // ================== SAVE / LOAD SUPPORT ==================

    public SoilTileData GetSaveData()
    {
        SoilTileData d = new SoilTileData();

        Vector3 p = transform.position;
        d.posX = p.x;
        d.posY = p.y;
        d.posZ = p.z;

        d.isTilled = isTilled;
        d.isWatered = isWatered;

        d.cropName = crop ? crop.cropName : "";
        d.stageIndex = stageIndex;
        d.stageTimer = stageTimer;

        return d;
    }

    public void ApplySaveData(SoilTileData d, CropSO[] allCrops)
    {
        isTilled = d.isTilled;
        isWatered = d.isWatered;

        if (string.IsNullOrEmpty(d.cropName))
        {
            ClearCrop();
            return;
        }

        CropSO target = null;
        foreach (var c in allCrops)
        {
            if (c != null && c.cropName == d.cropName)
            {
                target = c;
                break;
            }
        }

        if (target == null)
        {
            Debug.LogWarning($"Crop '{d.cropName}' not found!");
            ClearCrop();
            return;
        }

        crop = target;
        stageIndex = Mathf.Clamp(d.stageIndex, 0, crop.growthPrefabs.Length - 1);
        stageTimer = d.stageTimer;

        SpawnCropStage();
        UpdateGroundVisual(); // ให้ดินกลับมาอยู่ตามสถานะที่เซฟไว้
    }
}
