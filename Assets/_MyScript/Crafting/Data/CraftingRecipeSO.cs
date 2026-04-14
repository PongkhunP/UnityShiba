using UnityEngine;

/// <summary>
/// สูตรคราฟ (Crafting Recipe) — สร้างได้ที่โต๊ะคราฟ (Workbench)
/// แต่ละสูตรกำหนดวัตถุดิบ + ผลลัพธ์ (ได้ ItemSO ออกมา)
/// </summary>
[CreateAssetMenu(menuName = "Crafting/Recipe")]
public class CraftingRecipeSO : ScriptableObject
{
    [Header("Recipe Info")]
    [Tooltip("ชื่อสูตร (ไทย/อังกฤษ)")]
    public string recipeName;

    [Tooltip("คำอธิบาย")]
    [TextArea(2, 4)]
    public string description;

    [Tooltip("ไอคอนสูตร (ใช้ไอคอนของ result ก็ได้)")]
    public Sprite icon;

    [Header("Ingredients — วัตถุดิบ")]
    public CraftingIngredient[] ingredients;

    [Header("Result — ผลลัพธ์")]
    [Tooltip("ไอเท็มที่ได้หลังคราฟ")]
    public ItemSO resultItem;

    [Tooltip("จำนวนที่ได้")]
    [Min(1)]
    public int resultAmount = 1;

    [Header("Requirements")]
    [Tooltip("ต้องเรียนรู้สูตรก่อนถึงจะคราฟได้? (false = รู้ตั้งแต่เริ่มเกม)")]
    public bool requiresLearning = false;

    [Tooltip("ระดับ Workbench ขั้นต่ำ (0 = ไม่จำกัด)")]
    [Min(0)]
    public int minWorkbenchLevel = 0;

    [Tooltip("พลังงานที่ใช้ในการคราฟ")]
    [Min(0f)]
    public float energyCost = 5f;

    /// <summary>ตรวจสอบว่ามีวัตถุดิบครบหรือไม่</summary>
    public bool CanCraft(System.Func<ItemSO, int> getItemCount)
    {
        if (ingredients == null) return false;
        foreach (var ing in ingredients)
        {
            if (ing.item == null) continue;
            if (getItemCount(ing.item) < ing.amount) return false;
        }
        return true;
    }
}

/// <summary>วัตถุดิบ 1 รายการ (ไอเท็ม + จำนวน)</summary>
[System.Serializable]
public class CraftingIngredient
{
    public ItemSO item;
    [Min(1)]
    public int amount = 1;
}
