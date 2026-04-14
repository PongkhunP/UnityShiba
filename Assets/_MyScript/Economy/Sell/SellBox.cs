using UnityEngine;
using TMPro;

/// <summary>
/// [UPGRADED] ระบบขายของ — ใช้ราคาตลาด (MarketPriceSystem) แทน fixed price
/// - ขายแล้ว → บันทึกลง MarketPriceSystem → ราคาลดลง
/// - แสดง feedback ราคาที่ขายได้
/// </summary>
public class SellBox : MonoBehaviour
{
    [Header("Keys")]
    public KeyCode sellSelectedKey = KeyCode.F;
    public KeyCode sellAllKey = KeyCode.G;

    [Header("Price")]
    [Tooltip("ตัวคูณราคาเพิ่มเติม (1 = ปกติ, 1.2 = แพงขึ้น 20%)")]
    public float sellMultiplier = 1f;

    [Header("UI Prompt")]
    public GameObject promptPanel;
    public TextMeshProUGUI promptLabel;

    [Header("Sell Feedback (optional)")]
    public TextMeshProUGUI feedbackLabel;
    public float feedbackDuration = 2f;

    [Header("SFX")]
    public AudioSource sfxSource;
    public AudioClip sellSfx;
    [Range(0f, 1f)] public float sfxVolume = 1f;

    private bool playerInRange;
    private float feedbackTimer;

    private void Start()
    {
        if (promptPanel) promptPanel.SetActive(false);
        if (feedbackLabel) feedbackLabel.text = "";
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
            SellFromHotbarSelected();

        if (Input.GetKeyDown(sellAllKey))
            SellAllFromInventory();

        // Feedback timer
        if (feedbackTimer > 0)
        {
            feedbackTimer -= Time.deltaTime;
            if (feedbackTimer <= 0 && feedbackLabel)
                feedbackLabel.text = "";
        }
    }

    // ================================================================
    // Sell Selected (Hotbar)
    // ================================================================

    void SellFromHotbarSelected()
    {
        var hb = HotbarUI.Instance;
        if (hb == null) return;

        var slot = hb.GetSelectedSlot();
        if (slot == null || slot.item == null || slot.amount <= 0) return;

        int pricePer = GetSellPrice(slot.item);
        if (pricePer <= 0)
        {
            ShowFeedback($"{slot.item.itemName} ขายไม่ได้!", Color.gray);
            return;
        }

        int total = pricePer * slot.amount;
        string itemName = slot.item.itemName;
        int amount = slot.amount;

        PlayerWallet.Instance?.Add(total);

        // [NEW] บันทึกการขายลงตลาด
        RecordSale(itemName, amount);

        slot.Clear();

        ShowFeedback($"ขาย {itemName} x{amount} — ¥{total:N0}", new Color(0.2f, 0.8f, 0.2f));
        PlaySfx();
    }

    // ================================================================
    // Sell All (Inventory)
    // ================================================================

    void SellAllFromInventory()
    {
        var inv = InventoryUI.Instance;
        if (inv == null) return;

        int total = 0;
        int itemCount = 0;

        foreach (var s in inv.slots)
        {
            if (s.item != null && s.amount > 0)
            {
                int pricePer = GetSellPrice(s.item);
                if (pricePer > 0)
                {
                    int earned = pricePer * s.amount;
                    total += earned;
                    itemCount += s.amount;

                    // [NEW] บันทึกการขายลงตลาด
                    RecordSale(s.item.itemName, s.amount);

                    s.Clear();
                }
            }
        }

        if (total > 0)
        {
            PlayerWallet.Instance?.Add(total);
            ShowFeedback($"ขายทั้งหมด {itemCount} ชิ้น — ¥{total:N0}", new Color(0.2f, 0.8f, 0.2f));
            PlaySfx();
        }
        else
        {
            ShowFeedback("ไม่มีของที่ขายได้", Color.gray);
        }
    }

    // ================================================================
    // Price Calculation — ใช้ตลาด
    // ================================================================

    int GetSellPrice(ItemSO item)
    {
        if (item == null || !item.sellable) return 0;

        // [UPGRADED] ใช้ MarketPriceSystem ถ้ามี
        if (MarketPriceSystem.Instance != null)
            return MarketPriceSystem.Instance.GetSellPrice(item, sellMultiplier);

        // Fallback — ราคา fixed แบบเดิม
        return Mathf.RoundToInt(item.sellPrice * sellMultiplier);
    }

    // ================================================================
    // Market Recording
    // ================================================================

    void RecordSale(string itemName, int amount)
    {
        if (MarketPriceSystem.Instance != null)
            MarketPriceSystem.Instance.RecordSale(itemName, amount);
    }

    // ================================================================
    // Feedback
    // ================================================================

    void ShowFeedback(string msg, Color c)
    {
        if (feedbackLabel)
        {
            feedbackLabel.text = msg;
            feedbackLabel.color = c;
            feedbackTimer = feedbackDuration;
        }
        Debug.Log($"[SellBox] {msg}");
    }

    void PlaySfx()
    {
        if (sfxSource && sellSfx)
            sfxSource.PlayOneShot(sellSfx, sfxVolume);
    }
}
