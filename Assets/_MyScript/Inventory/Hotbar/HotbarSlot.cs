using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HotbarSlot : MonoBehaviour
{
    [Header("UI refs")]
    public Image iconImage;              // ไอคอนบนช่อง Hotbar
    public TextMeshProUGUI amountText;   // ตัวเลข (ถ้าไม่ใช้ ปล่อยว่างได้)

    [Header("Runtime")]
    public ItemSO item;
    public int amount;

    public bool HasStack => item != null && amount > 0;

    // ---------- API แบบเดิม (คงไว้ให้สคริปต์อื่นเรียกได้) ----------
    public void SetItem(ItemSO newItem) => SetShortcut(newItem);
    public void SetItem(ItemSO newItem, int newAmount) => SetStack(newItem, newAmount);
    // ---------------------------------------------------------------

    // ---------- API แนะนำ ----------
    // วางเป็น "ชอร์ตคัต" (ถือไอคอนอย่างเดียว ไม่ถือจำนวน)
    public void SetShortcut(ItemSO newItem)
    {
        item = newItem;
        amount = 0;
        UpdateUI();
    }

    // วางเป็น "ของจริง" (ถือจำนวน)
    public void SetStack(ItemSO newItem, int newAmount)
    {
        item = newItem;
        amount = Mathf.Max(0, newAmount);
        UpdateUI();
    }
    // -------------------------------

    public void Clear()
    {
        item = null;
        amount = 0;
        UpdateUI();
    }

    public void UpdateUI()
    {
        // Icon
        if (iconImage)
        {
            if (item != null) { iconImage.sprite = item.icon; iconImage.enabled = true; }
            else { iconImage.sprite = null; iconImage.enabled = false; }
        }

        // Amount text
        if (amountText)
        {
            if (amount > 1) amountText.text = amount.ToString();
            else if (amount == 1) amountText.text = "1";
            else amountText.text = ""; // ชอร์ตคัตหรือว่าง
        }
    }
}
