using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InventorySlot : MonoBehaviour
{
    [Header("UI refs")]
    public Image iconImage;
    public TextMeshProUGUI amountText;

    [Header("Runtime")]
    public ItemSO item;
    public int amount;

    public bool IsEmpty => item == null || amount <= 0;

    // ---------- Public API ----------
    public void SetItem(ItemSO newItem) => SetItem(newItem, 1);

    public void SetItem(ItemSO newItem, int newAmount)
    {
        item = newItem;
        amount = Mathf.Max(1, newAmount);
        UpdateUI();
    }

    public void Clear()
    {
        item = null;
        amount = 0;
        UpdateUI();
    }

    public void IncreaseAmount(int value)
    {
        if (value <= 0 || item == null) return;
        amount += value;
        UpdateUI();
    }

    public void DecreaseAmount(int value)
    {
        if (value <= 0 || item == null) return;
        amount -= value;
        if (amount <= 0) Clear();
        else UpdateUI();
    }

    public void UpdateUI()
    {
        // Icon
        if (iconImage)
        {
            if (item != null)
            {
                iconImage.sprite = item.icon;
                iconImage.enabled = true;
            }
            else
            {
                iconImage.sprite = null;
                iconImage.enabled = false;
            }
        }

        // Amount text (จะแสดง 1 หรือซ่อนได้ตามต้องการ)
        if (amountText)
        {
            if (item == null || amount <= 0) amountText.text = "";
            else amountText.text = amount.ToString();
        }
    }

    // ---------- WRAPPERS เพื่อให้โค้ดเก่าทำงานได้ ----------
    // โค้ดเก่าบางไฟล์เรียกชื่อเดิมพวกนี้อยู่
    public void ClearSlot() => Clear();
    public void UpdateAmountText() => UpdateUI();
}
