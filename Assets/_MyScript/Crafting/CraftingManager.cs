using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ระบบคราฟ (Crafting System) — core logic
/// - เก็บรายการสูตรทั้งหมด
/// - ตรวจสอบวัตถุดิบใน Inventory
/// - ทำการคราฟ → ลบวัตถุดิบ + เพิ่มผลลัพธ์
/// - จัดการสูตรที่เรียนรู้แล้ว
/// </summary>
public class CraftingManager : MonoBehaviour
{
    public static CraftingManager Instance { get; private set; }

    [Header("Recipes Database")]
    [Tooltip("สูตรคราฟทั้งหมดในเกม")]
    public CraftingRecipeSO[] allRecipes;

    [Header("Refs")]
    public InventoryMainUI inventory;

    [Header("Runtime — สูตรที่ปลดล็อกแล้ว")]
    [SerializeField] private List<string> learnedRecipes = new List<string>();

    // === Events ===
    public event Action<CraftingRecipeSO> OnRecipeCrafted;
    public event Action<string> OnRecipeLearned;

    // ================================================================
    // Lifecycle
    // ================================================================

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning($"[CraftingManager] พบ Instance ซ้ำบน '{gameObject.name}' — ลบ Component นี้ออก");
            Destroy(this);   // ลบแค่ Component ไม่ทำลาย GameObject ทั้งก้อน
            return;
        }
        Instance = this;

        if (!inventory) inventory = InventoryMainUI.Instance;
    }

    // ================================================================
    // Recipe Query
    // ================================================================

    /// <summary>รายการสูตรที่ผู้เล่นสามารถคราฟได้ (ปลดล็อกแล้ว หรือไม่ต้องเรียน)</summary>
    public List<CraftingRecipeSO> GetAvailableRecipes(int workbenchLevel = 0)
    {
        var list = new List<CraftingRecipeSO>();
        foreach (var recipe in allRecipes)
        {
            if (recipe.requiresLearning && !learnedRecipes.Contains(recipe.recipeName))
                continue;
            if (recipe.minWorkbenchLevel > workbenchLevel)
                continue;
            list.Add(recipe);
        }
        return list;
    }

    /// <summary>ตรวจสอบว่าเรียนรู้สูตรนี้แล้วหรือยัง</summary>
    public bool HasLearned(string recipeName)
    {
        return learnedRecipes.Contains(recipeName);
    }

    /// <summary>เรียนรู้สูตรใหม่</summary>
    public void LearnRecipe(string recipeName)
    {
        if (!learnedRecipes.Contains(recipeName))
        {
            learnedRecipes.Add(recipeName);
            OnRecipeLearned?.Invoke(recipeName);
            Debug.Log($"[Crafting] เรียนรู้สูตรใหม่: {recipeName}!");
        }
    }

    // ================================================================
    // Inventory Helpers
    // ================================================================

    /// <summary>นับจำนวนไอเท็มใน Inventory + Hotbar</summary>
    public int CountItem(ItemSO item)
    {
        if (inventory == null) inventory = InventoryMainUI.Instance;

        int count = 0;

        // นับจาก Inventory
        if (inventory != null)
        {
            foreach (var slot in inventory.slots)
            {
                if (slot != null && slot.item == item)
                    count += slot.amount;
            }
        }

        // นับจาก Hotbar
        if (HotbarUI.Instance != null)
        {
            foreach (var slot in HotbarUI.Instance.slots)
            {
                if (slot != null && slot.item == item)
                    count += slot.amount;
            }
        }

        return count;
    }

    /// <summary>ลบไอเท็มจาก Inventory + Hotbar จำนวน amount (ลบ Inventory ก่อน)</summary>
    bool ConsumeItem(ItemSO item, int amount)
    {
        int remaining = amount;

        // ลบจาก Inventory ก่อน
        if (inventory != null)
        {
            foreach (var slot in inventory.slots)
            {
                if (remaining <= 0) break;
                if (slot == null || slot.item != item) continue;

                int take = Mathf.Min(remaining, slot.amount);
                slot.DecreaseAmount(take);
                remaining -= take;

                if (slot.amount <= 0) slot.Clear();
            }
        }
        // ถ้ายังไม่พอ → ลบจาก Hotbar ต่อ
        if (remaining > 0 && HotbarUI.Instance != null)
        {
            foreach (var slot in HotbarUI.Instance.slots)
            {
                if (remaining <= 0) break;
                if (slot == null || slot.item != item) continue;

                int take = Mathf.Min(remaining, slot.amount);

                // --- แก้ไขตรงนี้ครับ ---
                slot.amount -= take;  // สั่งหักลบตัวเลขตรงๆ
                slot.UpdateUI();      // สั่งให้อัปเดตภาพ UI
                // ---------------------

                remaining -= take;

                if (slot.amount <= 0) slot.Clear();
            }
        }

        return remaining <= 0;
    }

    // ================================================================
    // Crafting Logic
    // ================================================================

    /// <summary>
    /// ตรวจสอบว่าสามารถคราฟสูตรนี้ได้ไหม
    /// </summary>
    public CraftResult CanCraft(CraftingRecipeSO recipe)
    {
        if (recipe == null) return CraftResult.InvalidRecipe;
        if (inventory == null) return CraftResult.NoInventory;

        // ตรวจสูตรที่ต้องเรียน
        if (recipe.requiresLearning && !learnedRecipes.Contains(recipe.recipeName))
            return CraftResult.NotLearned;

        // ตรวจวัตถุดิบ
        if (recipe.ingredients == null) return CraftResult.InvalidRecipe;
        foreach (var ing in recipe.ingredients)
        {
            if (ing.item == null) continue;
            if (CountItem(ing.item) < ing.amount)
                return CraftResult.NotEnoughMaterials;
        }

        // ตรวจ Inventory ว่ามีที่ว่างไหม
        bool hasSpace = false;
        foreach (var slot in inventory.slots)
        {
            if (slot.item == null) { hasSpace = true; break; }
            if (slot.item == recipe.resultItem && slot.amount + recipe.resultAmount <= slot.item.maxStack)
            { hasSpace = true; break; }
        }
        if (!hasSpace) return CraftResult.InventoryFull;

        // ตรวจพลังงาน (optional)
        // var energy = FindObjectOfType<PlayerEnergySystem>();
        // if (energy != null && energy.CurrentEnergy < recipe.energyCost)
        //     return CraftResult.NotEnoughEnergy;

        return CraftResult.Success;
    }

    /// <summary>
    /// ทำการคราฟ! ลบวัตถุดิบ → เพิ่มผลลัพธ์
    /// </summary>
    public CraftResult Craft(CraftingRecipeSO recipe)
    {
        CraftResult check = CanCraft(recipe);
        if (check != CraftResult.Success) return check;

        // 1) ลบวัตถุดิบ
        foreach (var ing in recipe.ingredients)
        {
            if (ing.item == null) continue;
            ConsumeItem(ing.item, ing.amount);
        }

        // 2) เพิ่มผลลัพธ์ลง Inventory
        bool added = inventory.AddItemToInventory(recipe.resultItem, recipe.resultAmount);
        if (!added)
        {
            // กรณีหายาก — วัตถุดิบถูกลบไปแล้วแต่เพิ่มไม่ได้ → ไม่ควรเกิดเพราะเช็คแล้ว
            Debug.LogError("[Crafting] เพิ่มผลลัพธ์ไม่ได้ (ไม่ควรเกิด)!");
            return CraftResult.InventoryFull;
        }

        // 3) ลดพลังงาน (optional)
        // var energy = FindObjectOfType<PlayerEnergySystem>();
        // if (energy != null) energy.Use(recipe.energyCost);

        Debug.Log($"[Crafting] คราฟ {recipe.recipeName} สำเร็จ! ได้ {recipe.resultItem.itemName} x{recipe.resultAmount}");
        OnRecipeCrafted?.Invoke(recipe);

        return CraftResult.Success;
    }

    // ================================================================
    // Save / Load
    // ================================================================

    public string[] GetLearnedRecipes() => learnedRecipes.ToArray();

    public void SetLearnedRecipes(string[] recipes)
    {
        learnedRecipes.Clear();
        if (recipes != null)
            learnedRecipes.AddRange(recipes);
    }
}

/// <summary>ผลลัพธ์การคราฟ</summary>
public enum CraftResult
{
    Success,            // คราฟสำเร็จ!
    NotEnoughMaterials, // วัตถุดิบไม่พอ
    NotLearned,         // ยังไม่ได้เรียนรู้สูตร
    InventoryFull,      // Inventory เต็ม
    NotEnoughEnergy,    // พลังงานไม่พอ
    InvalidRecipe,      // สูตรผิด
    NoInventory,        // ไม่มี InventoryUI
}
