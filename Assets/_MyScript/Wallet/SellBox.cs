using UnityEngine;
using TMPro; // ถ้าใช้ TMP

public class SellBox : MonoBehaviour
{
    [Header("Keys")]
    public KeyCode sellSelectedKey = KeyCode.F; // ขายของที่เลือกใน Hotbar
    public KeyCode sellAllKey = KeyCode.G; // ขายของทั้งหมดใน Inventory

    [Header("Price")]
    public float sellMultiplier = 1f;

    [Header("UI Prompt")]
    public GameObject promptPanel;          // <- ลาก Panel (SellPrompt) มาวางตรงนี้
    public TextMeshProUGUI promptLabel;     // (ไม่บังคับ) ถ้าจะเปลี่ยนข้อความ runtime

    private bool playerInRange;

    private void Start()
    {
        // เผื่อเผลอลืมปิดไว้
        if (promptPanel) promptPanel.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = true;
            if (promptPanel) promptPanel.SetActive(true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;
            if (promptPanel) promptPanel.SetActive(false);
        }
    }

    private void Update()
    {
        if (!playerInRange) return;

        if (Input.GetKeyDown(sellSelectedKey))
        {
            SellFromHotbarSelected();
        }

        if (Input.GetKeyDown(sellAllKey))
        {
            SellAllFromInventory();
        }
    }

    void SellFromHotbarSelected()
    {
        var hb = HotbarUI.Instance;
        if (hb == null) return;

        var slot = hb.GetSelectedSlot();
        if (slot == null || slot.item == null || slot.amount <= 0) return;

        int pricePer = GetSellPrice(slot.item);
        if (pricePer <= 0) return;

        int total = pricePer * slot.amount;
        PlayerWallet.Instance?.Add(total);

        slot.Clear(); // ล้างของในช่อง

        // (ไม่บังคับ) อัปเดตข้อความชั่วคราว
       
    }

    void SellAllFromInventory()
    {
        var inv = InventoryUI.Instance;
        if (inv == null) return;

        int total = 0;

        foreach (var s in inv.slots)
        {
            if (s.item != null && s.amount > 0)
            {
                int pricePer = GetSellPrice(s.item);
                if (pricePer > 0)
                {
                    total += pricePer * s.amount;
                    s.Clear();
                }
            }
        }

     
    }

    int GetSellPrice(ItemSO item)
    {
        if (item == null || !item.sellable) return 0;
        return Mathf.RoundToInt(item.sellPrice * sellMultiplier);
    }
}
