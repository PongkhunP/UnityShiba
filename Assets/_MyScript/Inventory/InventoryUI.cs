using System.Collections.Generic;
using UnityEngine;

// Starter Assets
using StarterAssets;

// Cinemachine (�����)
using Unity.Cinemachine;

// ����� Input System ���� ������� Scripting Define Symbol: ENABLE_INPUT_SYSTEM
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class InventoryMainUI : MonoBehaviour
{
    [Header("Slots")]
    public InventorySlot[] slots;

    [Header("UI Root")]
    public GameObject inventoryPanel;          // �ҡ GameObject �����˹�ҡҡ�Թ�ǹ������� (�� InventoryPanel)

    [Header("Freeze Control while open")]
    public Transform player;                   // �ҡ Player (��Ƿ���� ThirdPersonController/StarterAssetsInputs)
    public bool unlockCursorOnOpen = true;     // �Դ�����������Դ�Թ�ǹ��
    public Behaviour[] extraDisable;           // �����ʤ�Ի�� custom ����ûԴ�͹�Դ UI

    [Header("(Optional) ��ش�������͹�Դ")]
    public bool pauseWithTimeScale = false;

    public static InventoryMainUI Instance { get; private set; }
    public static bool IsOpen { get; private set; }

    readonly List<Behaviour> _toDisable = new List<Behaviour>();
    bool[] _wasEnabled;
    float _prevTimeScale = 1f;

    void Awake()
    {
        Instance = this;
        if (inventoryPanel) inventoryPanel.SetActive(false);

        BuildDisableList();            // �������๹����лԴ�����ǧ˹��
        _wasEnabled = new bool[_toDisable.Count];
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.I)) Toggle();
        if (IsOpen && Input.GetKeyDown(KeyCode.Escape)) Close();
    }

    // ---------------------------------------
    // �Ǻ�������๹����� "�Դ�Ѻ" �͹�Դ UI
    // ---------------------------------------
    void BuildDisableList()
    {
        _toDisable.Clear();

        // -------- ��� Player --------
        if (player != null)
        {
            var tpc = player.GetComponent<ThirdPersonController>();
            if (tpc) _toDisable.Add(tpc);

            var sai = player.GetComponent<StarterAssetsInputs>();
            if (sai) _toDisable.Add(sai);

#if ENABLE_INPUT_SYSTEM
            // �Դ PlayerInput = �Ѵ�Թ�ص������
            var pi = player.GetComponent<PlayerInput>();
            if (pi) _toDisable.Add(pi);
#endif
            // NOTE: CharacterController ����� Behaviour ����Դ/�Դ�� �֧������ŧ��ʵ�
        }

        // -------- ��觡��ͧ/Cinemachine --------
        var cam = Camera.main;
        if (cam)
        {
            var brain = cam.GetComponent<CinemachineBrain>();
            if (brain) _toDisable.Add(brain);

            var cip = cam.GetComponent<CinemachineInputProvider>();
            if (cip) _toDisable.Add(cip);
        }

        // �ó��ҧ InputProvider ��麹 Virtual Camera
        var vcam = FindAnyObjectByType<CinemachineVirtualCamera>();
        if (vcam)
        {
            var cip2 = vcam.GetComponent<CinemachineInputProvider>();
            if (cip2) _toDisable.Add(cip2);
        }

        // -------- ���÷����ҡ�����ͧ --------
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

        // �����ա�û�Ѻ��ҧ�ԧ�͹�ѹ
        if (_toDisable.Count == 0) BuildDisableList();
        if (_wasEnabled == null || _wasEnabled.Length != _toDisable.Count)
            _wasEnabled = new bool[_toDisable.Count];

        // �Դ����๹��Ǻ����������͹���/���ͧ
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

        // �Դ����๹���Ѻ���ʶҹ����
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
    // �ѧ��ѹ���: �������� + ��� stack
    // (���¡�ҡ pickup ���)
    // ---------------------------------------
    public bool AddItemToInventory(ItemSO item) => AddItemToInventory(item, 1);

    public bool AddItemToInventory(ItemSO item, int amount)
    {
        if (item == null || amount <= 0) return false;

        // 1) ����Ѻ��ͧ�����͹ ��� stack ��
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
                        slot.UpdateAmountText(); // ���Ը�����ͧ�س
                        amount -= canAdd;
                        if (amount <= 0) return true;
                    }
                }
            }
        }

        // 2) �Ҫ�ͧ��ҧ
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

        Debug.Log("Inventory ���!");
        return amount <= 0;
    }
}