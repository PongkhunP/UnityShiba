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

    [Tooltip("����Դ = �͹���������ͤ�������Ы�͹�������� (����͹ TPS)\n��һԴ = �͹�������������������ʹ (����СѺ����ԡ�ӿ����)")]
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

        // ���੾�� component �� Player �����ҡ�Դ�͹ Pause
        // (��Ҥس������� extraDisable �������� �������繵�ͧ��������ç���)
        var behaviours = player.GetComponentsInChildren<Behaviour>(true);
        foreach (var b in behaviours)
        {
            if (b == null) continue;

            // ���һԴ����ͧ ������һԴ UI
            if (b == this) continue;

            // �ѹ��Ҵ: ����� InventoryUI ���� PauseMenuUI ������ͧ�Դ
            // (���������ѹ compile error �������դ���)
            if (b.GetType().Name == "InventoryUI") continue;
            if (b.GetType().Name == "PauseMenuUI") continue;

            // ����� ���ͤس��ҡ���͹Ҥ�
            // _autoDisable.Add(b);
        }
    }

    private void Update()
    {
        // ��� Inventory �Դ���� ��� ESC �ӧҹ�Ѻ Inventory (��������������س)
        // �������Ҩ���� ESC �Դ/�Դ Pause ੾�е͹ Inventory ����Դ
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (InventoryMainUI.IsOpen) return;

            // �������� Settings ���ǡ� ESC = ��Ѻ�˹�� Pause
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
        // �ѹ�ó�ʤ�Ի��������ͤ�����Ѻ (������ ���ԡ�����������)
        ApplyCursorState();
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        // ���� alt-tab ���ͤ�ԡ��Ѻ����� ���ʶҹ������١��ͧ����
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

        // �Դ component ���س�ҡ������ͧ�͹ pause
        for (int i = 0; i < extraDisable.Count; i++)
        {
            var b = extraDisable[i];
            if (b != null) b.enabled = !paused;
        }

        // (����͹Ҥ�) ��Ҥس��ҡ��� auto disable �ӧҹ ���Դ��÷Ѵ���
        // for (int i = 0; i < _autoDisable.Count; i++) if (_autoDisable[i] != null) _autoDisable[i].enabled = !paused;

        ApplyCursorState();
    }

    private void ApplyCursorState()
    {
        bool anyMenuOpen = _isPaused || InventoryMainUI.IsOpen;

        if (anyMenuOpen)
        {
            // �����Դ -> ��ͧ��������
            if (unlockCursorOnOpen)
            {
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
            }
            return;
        }

        // ���ٻԴ -> ʶҹ������͹�����
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
        if (GameDataManager.Singleton != null)
            GameDataManager.Singleton.SaveGame();

        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }

    public void OnButton_QuitApp()
    {
        Application.Quit();
    }
}
