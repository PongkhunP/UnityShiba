using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseMenuUI : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private GameObject settingsPanel;

    [Header("Player (optional)")]
    [SerializeField] private Transform player;

    [Header("Behavior")]
    [SerializeField] private bool pauseWithTimeScale = true;
    [SerializeField] private bool unlockCursorOnOpen = true;

    [Tooltip("ถ้าเปิด = ตอนเล่นเกมจะล็อคเมาส์และซ่อนเคอร์เซอร์ (เหมือน TPS)\nถ้าปิด = ตอนเล่นเกมจะเห็นเคอร์เซอร์ตลอด (เหมาะกับเกมคลิกทำฟาร์ม)")]
    [SerializeField] private bool lockCursorWhenPlaying = false;

    [Header("UI Blockers (optional)")]
    [SerializeField] private List<GameObject> uiBlockers = new List<GameObject>();

    [Header("Freeze Components (optional)")]
    [SerializeField] private List<Behaviour> extraDisable = new List<Behaviour>();

    private readonly List<Behaviour> _autoDisable = new List<Behaviour>();
    private bool _isPaused;

    private bool AnyBlockerOpen()
    {
        for (int i = 0; i < uiBlockers.Count; i++)
        {
            var go = uiBlockers[i];
            if (go != null && go.activeInHierarchy) return true;
        }
        return false;
    }

    private void Awake()
    {
        BuildDisableList();
        SetPanels(false, false);
        ApplyPause(false);
    }

    private void BuildDisableList()
    {
        _autoDisable.Clear();

        if (player == null) return;

        // ใส่เฉพาะ component บน Player ที่อยากปิดตอน Pause
        // (ถ้าคุณใส่ไว้ใน extraDisable อยู่แล้ว ก็ไม่จำเป็นต้องใส่เพิ่มตรงนี้)
        var behaviours = player.GetComponentsInChildren<Behaviour>(true);
        foreach (var b in behaviours)
        {
            if (b == null) continue;

            // อย่าปิดตัวเอง และอย่าปิด UI
            if (b == this) continue;

            // กันพลาด: ถ้าเป็น InventoryUI หรือ PauseMenuUI ก็ไม่ต้องปิด
            // (ใส่ชื่อไว้กัน compile error ถ้าไม่มีคลาส)
            if (b.GetType().Name == "InventoryUI") continue;
            if (b.GetType().Name == "PauseMenuUI") continue;

            // เก็บไว้ เผื่อคุณอยากใช้ในอนาคต
            // _autoDisable.Add(b);
        }
    }

    private void Update()
    {
        // ถ้า Inventory เปิดอยู่ ให้ ESC ทำงานกับ Inventory (หรือไม่ก็แล้วแต่คุณ)
        // ที่นี่เราจะให้ ESC เปิด/ปิด Pause เฉพาะตอน Inventory ไม่เปิด
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (InventoryUI.IsOpen) return;

            // ถ้าอยู่ใน Settings แล้วกด ESC = กลับไปหน้า Pause
            if (_isPaused && settingsPanel != null && settingsPanel.activeSelf)
            {
                CloseSettings();
            }
            else
            {
                TogglePause();
            }
        }
    }

    private void LateUpdate()
    {
        // กันกรณีสคริปต์อื่นไปล็อคเมาส์ทับ (ช่วยแก้ “คลิกแล้วเมาส์หาย”)
        ApplyCursorState();
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        // เวลา alt-tab หรือคลิกกลับเข้าเกม ให้สถานะเมาส์ถูกต้องเสมอ
        if (hasFocus) ApplyCursorState();
    }

    public void TogglePause()
    {
        if (_isPaused) Resume();
        else Pause();
    }

    public void Pause()
    {
        if (AnyBlockerOpen()) return;

        _isPaused = true;
        SetPanels(true, false);
        ApplyPause(true);
    }

    public void Resume()
    {
        _isPaused = false;
        SetPanels(false, false);
        ApplyPause(false);
    }

    public void OpenSettings()
    {
        if (!_isPaused) Pause();
        SetPanels(false, true);
        ApplyPause(true);
    }

    public void CloseSettings()
    {
        if (!_isPaused) return;
        SetPanels(true, false);
        ApplyPause(true);
    }

    private void SetPanels(bool showPause, bool showSettings)
    {
        if (pausePanel != null) pausePanel.SetActive(showPause);
        if (settingsPanel != null) settingsPanel.SetActive(showSettings);
    }

    private void ApplyPause(bool paused)
    {
        if (pauseWithTimeScale)
            Time.timeScale = paused ? 0f : 1f;

        // ปิด component ที่คุณลากมาใส่เองตอน pause
        for (int i = 0; i < extraDisable.Count; i++)
        {
            var b = extraDisable[i];
            if (b != null) b.enabled = !paused;
        }

        // (เผื่ออนาคต) ถ้าคุณอยากให้ auto disable ทำงาน ก็เปิดบรรทัดนี้
        // for (int i = 0; i < _autoDisable.Count; i++) if (_autoDisable[i] != null) _autoDisable[i].enabled = !paused;

        ApplyCursorState();
    }

    private void ApplyCursorState()
    {
        bool anyMenuOpen = _isPaused || InventoryUI.IsOpen;

        if (anyMenuOpen)
        {
            // เมนูเปิด -> ต้องเห็นเมาส์
            if (unlockCursorOnOpen)
            {
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
            }
            return;
        }

        // เมนูปิด -> สถานะเมาส์ตอนเล่นเกม
        if (lockCursorWhenPlaying)
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }
        else
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
    }

    // ---------------- Buttons ----------------

    public void OnButton_Play()
    {
        Resume();
    }

    public void OnButton_ExitToMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }

    public void OnButton_SaveAndQuit()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.SaveGame();

        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }

    public void OnButton_QuitApp()
    {
        Application.Quit();
    }
}
