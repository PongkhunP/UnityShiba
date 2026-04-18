using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// ระบบสิ้นวัน — ตี 2 จะ:
/// 1. หยุดเวลา + Lock player
/// 2. แสดง Summary แบ่งตาม Farming / Fishing / Ore / Other + รายการไอเท็ม
/// 3. กด "นอนหลับ" → ขึ้น Day Banner → วันใหม่ 6:00 AM
///
/// ── Prefab ที่ต้องสร้าง 2 ชิ้น ──────────────────────────────────────
///
/// [CategoryHeaderPrefab]  (height ~55)
///   ├── CatLabel  (TMP)  — "Farming"         anchor ซ้าย
///   └── CatValue  (TMP)  — "¥990"            anchor ขวา
///
/// [ItemRowPrefab]  (height ~45)
///   ├── ItemIcon  (Image)                    anchor ซ้าย, 40x40
///   ├── ItemLabel (TMP)  — "Onion x99 ..."   anchor stretch ตรงกลาง
///   └── ItemValue (TMP)  — "¥990"            anchor ขวา
/// </summary>
public class DayEndSystem : MonoBehaviour
{
    public static DayEndSystem Instance { get; private set; }

    // ─── Config ───────────────────────────────────────────────────────
    [Header("Bedtime / Wake")]
    [Range(0, 5)] public int bedtimeHour = 2;
    [Range(4, 12)] public int wakeHour   = 6;
    [Range(0, 59)] public int wakeMinute = 0;

    // ─── Summary Panel ────────────────────────────────────────────────
    [Header("Summary Panel")]
    public GameObject summaryPanel;
    public TextMeshProUGUI summaryTitleText;

    [Tooltip("Parent ที่ spawn rows ทั้งหมด (Vertical Layout Group)")]
    public Transform rowsParent;

    [Tooltip("Prefab หัว category — ต้องมี TMP ชื่อ 'CatLabel' และ 'CatValue'")]
    public GameObject categoryHeaderPrefab;

    [Tooltip("Prefab แถวไอเท็ม — ต้องมี Image 'ItemIcon', TMP 'ItemLabel', TMP 'ItemValue'")]
    public GameObject itemRowPrefab;

    public TextMeshProUGUI totalText;
    public Button sleepButton;

    // ─── Divider (optional) ───────────────────────────────────────────
    [Tooltip("เส้นคั่น (Prefab Image บางๆ) — ใส่ไว้ระหว่าง category (optional)")]
    public GameObject dividerPrefab;

    // ─── Day Banner ───────────────────────────────────────────────────
    [Header("Day Banner")]
    public GameObject bannerPanel;
    public TextMeshProUGUI bannerText;
    public float bannerDuration = 2.5f;
    public float bannerFadeTime = 0.5f;

    [Header("SFX")]
    public AudioSource audioSource;
    public AudioClip sleepSound;
    public AudioClip morningSound;

    // ─── Runtime ──────────────────────────────────────────────────────
    bool _triggeredToday;
    bool _isSummaryOpen;

    static readonly SellCategory[] ALL_CATEGORIES =
    {
        SellCategory.Farming,
        SellCategory.Fishing,
        SellCategory.Ore,
        SellCategory.Other,
    };

    static readonly string[] CATEGORY_NAMES =
    {
        "Farming",
        "Fishing",
        "Ore",
        "Other",
    };

    // ──────────────────────────────────────────────────────────────────
    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;
    }

    void Start()
    {
        if (summaryPanel) summaryPanel.SetActive(false);
        if (bannerPanel)  bannerPanel.SetActive(false);

        if (sleepButton) sleepButton.onClick.AddListener(OnSleepPressed);

        if (CalendarSystem.Instance != null)
            CalendarSystem.Instance.OnDayEnded += _ => _triggeredToday = false;
    }

    void Update()
    {
        if (_triggeredToday || _isSummaryOpen) return;
        if (TimeOfDaySystem.Instance == null) return;

        if (TimeOfDaySystem.Instance.Hour == bedtimeHour)
        {
            _triggeredToday = true;
            StartCoroutine(TriggerDayEnd());
        }
    }

    // ─── Flow ─────────────────────────────────────────────────────────

    IEnumerator TriggerDayEnd()
    {
        if (TimeOfDaySystem.Instance) TimeOfDaySystem.Instance.IsPaused = true;

        var player = FindObjectOfType<PlayerController>();
        player?.SetBusy(true);

        if (audioSource && sleepSound) audioSource.PlayOneShot(sleepSound);

        yield return new WaitForSeconds(0.4f);

        ShowSummary();
    }

    // ─── Summary ──────────────────────────────────────────────────────

    void ShowSummary()
    {
        _isSummaryOpen = true;

        // ลบ rows เก่า
        foreach (Transform child in rowsParent)
            Destroy(child.gameObject);

        // Title
        if (summaryTitleText && CalendarSystem.Instance != null)
        {
            var d = CalendarSystem.Instance.date;
            summaryTitleText.text = $"Day {d.day}  —  Year {d.year}";
        }

        var tracker = DailyEconomyTracker.Instance;

        // ─── แสดงทุก category ────────────────────────────────────────
        for (int i = 0; i < ALL_CATEGORIES.Length; i++)
        {
            var cat      = ALL_CATEGORIES[i];
            var catName  = CATEGORY_NAMES[i];
            int catTotal = tracker != null ? tracker.GetCategoryTotal(cat) : 0;
            var records  = tracker != null ? tracker.GetRecordsByCategory(cat) : null;

            // ── หัว category ─────────────────────────────────────────
            SpawnCategoryHeader(catName, catTotal);

            // ── รายการไอเท็มใน category ──────────────────────────────
            if (records != null && records.Count > 0)
            {
                foreach (var rec in records)
                    SpawnItemRow(rec);
            }
            else
            {
                // ไม่มีการขาย → แสดง "—"
                SpawnItemRow(null, "(ไม่มีการขาย)", 0, 0);
            }

            // เส้นคั่น (ถ้ามี prefab)
            if (dividerPrefab && i < ALL_CATEGORIES.Length - 1)
                Instantiate(dividerPrefab, rowsParent);
        }

        // ─── Total ───────────────────────────────────────────────────
        int total = tracker != null ? tracker.TotalEarnedToday : 0;
        if (totalText) totalText.text = $"Total   ¥{total:N0}";

        if (summaryPanel) summaryPanel.SetActive(true);
    }

    // ─── Spawn Helpers ────────────────────────────────────────────────

    void SpawnCategoryHeader(string catName, int catTotal)
    {
        if (!categoryHeaderPrefab || !rowsParent) return;

        var obj = Instantiate(categoryHeaderPrefab, rowsParent);

        var label = obj.transform.Find("CatLabel")?.GetComponent<TextMeshProUGUI>();
        var value = obj.transform.Find("CatValue")?.GetComponent<TextMeshProUGUI>();

        if (label) label.text = catName;
        if (value)
        {
            value.text  = catTotal > 0 ? $"¥{catTotal:N0}" : "¥0";
            value.color = catTotal > 0 ? new Color(0.2f, 0.7f, 0.2f) : Color.gray;
        }
    }

    void SpawnItemRow(DailyEconomyTracker.SoldItemRecord rec)
    {
        if (rec == null) return;
        SpawnItemRow(rec.icon, rec.itemName, rec.amount, rec.totalPrice);
    }

    void SpawnItemRow(Sprite icon, string name, int amount, int price)
    {
        if (!itemRowPrefab || !rowsParent) return;

        var obj = Instantiate(itemRowPrefab, rowsParent);

        // Icon
        var iconImg = obj.transform.Find("ItemIcon")?.GetComponent<Image>();
        if (iconImg)
        {
            if (icon != null) { iconImg.sprite = icon; iconImg.enabled = true; }
            else iconImg.enabled = false;
        }

        // Label — "Onion x99 ............."
        var labelTmp = obj.transform.Find("ItemLabel")?.GetComponent<TextMeshProUGUI>();
        if (labelTmp)
        {
            if (amount > 0)
                labelTmp.text = $"{name}  x{amount}";
            else
                labelTmp.text = name; // "(ไม่มีการขาย)"

            labelTmp.color = amount > 0 ? Color.white : Color.gray;
        }

        // Value — "¥990"
        var valueTmp = obj.transform.Find("ItemValue")?.GetComponent<TextMeshProUGUI>();
        if (valueTmp)
        {
            valueTmp.text  = price > 0 ? $"¥{price:N0}" : "-";
            valueTmp.color = price > 0 ? new Color(1f, 0.9f, 0.3f) : Color.gray;
        }
    }

    // ─── Sleep ────────────────────────────────────────────────────────

    void OnSleepPressed()
    {
        if (!_isSummaryOpen) return;
        StartCoroutine(FinishDay());
    }

    IEnumerator FinishDay()
    {
        if (summaryPanel) summaryPanel.SetActive(false);
        _isSummaryOpen = false;

        DailyEconomyTracker.Instance?.ResetDaily();

        if (CalendarSystem.Instance != null)
            CalendarSystem.Instance.NextDay();

        if (TimeOfDaySystem.Instance)
            TimeOfDaySystem.Instance.SetTime(wakeHour, wakeMinute);

        if (audioSource && morningSound) audioSource.PlayOneShot(morningSound);

        var player = FindObjectOfType<PlayerController>();
        player?.SetBusy(false);

        if (TimeOfDaySystem.Instance) TimeOfDaySystem.Instance.IsPaused = false;

        yield return StartCoroutine(ShowDayBanner());
    }

    IEnumerator ShowDayBanner()
    {
        if (bannerPanel == null || bannerText == null) yield break;

        if (CalendarSystem.Instance != null)
        {
            var d = CalendarSystem.Instance.date;
            bannerText.text = $"Day {d.day}";
        }

        bannerPanel.SetActive(true);

        var cg = bannerPanel.GetComponent<CanvasGroup>();
        if (cg == null) cg = bannerPanel.AddComponent<CanvasGroup>();

        // Fade in
        cg.alpha = 0f;
        for (float t = 0; t < bannerFadeTime; t += Time.deltaTime)
        {
            cg.alpha = t / bannerFadeTime;
            yield return null;
        }
        cg.alpha = 1f;

        yield return new WaitForSeconds(bannerDuration);

        // Fade out
        for (float t = 0; t < bannerFadeTime; t += Time.deltaTime)
        {
            cg.alpha = 1f - t / bannerFadeTime;
            yield return null;
        }

        bannerPanel.SetActive(false);
    }

    public void ForceSleep()
    {
        if (_isSummaryOpen) return;
        _triggeredToday = true;
        StartCoroutine(TriggerDayEnd());
    }
}
