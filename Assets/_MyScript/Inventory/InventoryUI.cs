using System.Collections.Generic;
using UnityEngine;

// Starter Assets
using StarterAssets;

// Cinemachine (ถ้าใช้)
using Cinemachine;

// ถ้าใช้ Input System ใหม่ ให้เพิ่ม Scripting Define Symbol: ENABLE_INPUT_SYSTEM
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class InventoryUI : MonoBehaviour
{
    [Header("Slots")]
    public InventorySlot[] slots;

    [Header("UI Root")]
    public GameObject inventoryPanel;          // ลาก GameObject ที่เป็นหน้ากากอินเวนต์มาใส่ (เช่น InventoryPanel)

    [Header("Freeze Control while open")]
    public Transform player;                   // ลาก Player (ตัวที่มี ThirdPersonController/StarterAssetsInputs)
    public bool unlockCursorOnOpen = true;     // เปิดเมาส์เมื่อเปิดอินเวนต์
    public Behaviour[] extraDisable;           // ถ้ามีสคริปต์ custom ที่ควรปิดตอนเปิด UI

    [Header("(Optional) หยุดเวลาเกมตอนเปิด")]
    public bool pauseWithTimeScale = false;

    public static InventoryUI Instance { get; private set; }
    public static bool IsOpen { get; private set; }

    readonly List<Behaviour> _toDisable = new List<Behaviour>();
    bool[] _wasEnabled;
    float _prevTimeScale = 1f;

    void Awake()
    {
        Instance = this;
        if (inventoryPanel) inventoryPanel.SetActive(false);

        BuildDisableList();            // รวมคอมโพเนนต์ที่จะปิดไว้ล่วงหน้า
        _wasEnabled = new bool[_toDisable.Count];
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.I)) Toggle();
        if (IsOpen && Input.GetKeyDown(KeyCode.Escape)) Close();
    }

    // ---------------------------------------
    // รวบรวมคอมโพเนนต์ที่จะ "ปิดทับ" ตอนเปิด UI
    // ---------------------------------------
    void BuildDisableList()
    {
        _toDisable.Clear();

        // -------- ฝั่ง Player --------
        if (player != null)
        {
            var tpc = player.GetComponent<ThirdPersonController>();
            if (tpc) _toDisable.Add(tpc);

            var sai = player.GetComponent<StarterAssetsInputs>();
            if (sai) _toDisable.Add(sai);

#if ENABLE_INPUT_SYSTEM
            // ปิด PlayerInput = ตัดอินพุตทั้งหมด
            var pi = player.GetComponent<PlayerInput>();
            if (pi) _toDisable.Add(pi);
#endif
            // NOTE: CharacterController ไม่ใช่ Behaviour ที่เปิด/ปิดได้ จึงไม่ใส่ลงลิสต์
        }

        // -------- ฝั่งกล้อง/Cinemachine --------
        var cam = Camera.main;
        if (cam)
        {
            var brain = cam.GetComponent<CinemachineBrain>();
            if (brain) _toDisable.Add(brain);

            var cip = cam.GetComponent<CinemachineInputProvider>();
            if (cip) _toDisable.Add(cip);
        }

        // กรณีวาง InputProvider ไว้บน Virtual Camera
        var vcam = FindAnyObjectByType<CinemachineVirtualCamera>();
        if (vcam)
        {
            var cip2 = vcam.GetComponent<CinemachineInputProvider>();
            if (cip2) _toDisable.Add(cip2);
        }

        // -------- อะไรที่อยากเพิ่มเอง --------
        if (extraDisable != null)
        {
            foreach (var b in extraDisable)
                if (b && !_toDisable.Contains(b))
                    _toDisable.Add(b);
        }
    }

    // ---------------------------------------
    // Toggle / Open / Close
    // ---------------------------------------
    public void Toggle()
    {
        if (IsOpen) Close();
        else Open();
    }

    public void Open()
    {
        if (IsOpen) return;
        IsOpen = true;

        if (inventoryPanel) inventoryPanel.SetActive(true);

        // เผื่อมีการปรับอ้างอิงตอนรัน
        if (_toDisable.Count == 0) BuildDisableList();
        if (_wasEnabled == null || _wasEnabled.Length != _toDisable.Count)
            _wasEnabled = new bool[_toDisable.Count];

        // ปิดคอมโพเนนต์ควบคุมการเคลื่อนที่/กล้อง
        for (int i = 0; i < _toDisable.Count; i++)
        {
            var b = _toDisable[i];
            if (!b) continue;
            _wasEnabled[i] = b.enabled;
            b.enabled = false;
        }

        if (pauseWithTimeScale)
        {
            _prevTimeScale = Time.timeScale;
            Time.timeScale = 0f;
        }

        if (unlockCursorOnOpen)
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
    }

    public void Close()
    {
        if (!IsOpen) return;
        IsOpen = false;

        if (inventoryPanel) inventoryPanel.SetActive(false);

        // เปิดคอมโพเนนต์กลับตามสถานะเดิม
        for (int i = 0; i < _toDisable.Count; i++)
        {
            var b = _toDisable[i];
            if (!b) continue;
            bool back = (i < _wasEnabled.Length) ? _wasEnabled[i] : true;
            b.enabled = back;
        }

        if (pauseWithTimeScale)
            Time.timeScale = _prevTimeScale;

        if (unlockCursorOnOpen)
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }
    }

    // ---------------------------------------
    // ฟังก์ชันเดิม: เพิ่มไอเทม + รวม stack
    // (เรียกจาก pickup ฯลฯ)
    // ---------------------------------------
    public bool AddItemToInventory(ItemSO item) => AddItemToInventory(item, 1);

    public bool AddItemToInventory(ItemSO item, int amount)
    {
        if (item == null || amount <= 0) return false;

        // 1) รวมกับช่องเดิมก่อน ถ้า stack ได้
        if (item.isStackable)
        {
            foreach (var slot in slots)
            {
                if (slot.item == item)
                {
                    int max = Mathf.Max(1, item.maxStack);
                    int canAdd = Mathf.Min(amount, max - slot.amount);
                    if (canAdd > 0)
                    {
                        slot.amount += canAdd;
                        slot.UpdateAmountText(); // ใช้วิธีเดิมของคุณ
                        amount -= canAdd;
                        if (amount <= 0) return true;
                    }
                }
            }
        }

        // 2) หาช่องว่าง
        foreach (var slot in slots)
        {
            if (slot.item == null)
            {
                int give = item.isStackable ? Mathf.Min(amount, item.maxStack) : 1;
                slot.SetItem(item, give);
                amount -= give;
                if (amount <= 0) return true;
            }
        }

        Debug.Log("Inventory เต็ม!");
        return amount <= 0;
    }
}
