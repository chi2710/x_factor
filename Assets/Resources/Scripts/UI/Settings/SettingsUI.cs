using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Màn Tùy chỉnh cài đặt: 5 tab bên trái (LB/RB hoặc click), nội dung tab bên phải.
// Tab Âm thanh sửa bản nháp; chỉ khi bấm Áp dụng mới ghi vào GameSettings. Đóng mà chưa Áp dụng thì bỏ thay đổi.
// Mở/đóng bằng nút Start/Menu (O), B (Esc) để thoát. Không mở cùng lúc với menu Tab.
public class SettingsUI : MonoBehaviour
{
    [Tooltip("Phần hiển thị được ẩn/hiện khi đóng/mở.")]
    public GameObject panel;
    public TabButtonUI[] tabs;
    [Tooltip("Nội dung của từng tab, cùng thứ tự với tabs.")]
    public GameObject[] pages;
    public int startTab = 2;

    [Header("Âm thanh")]
    public SettingRowUI masterRow;
    public SettingRowUI musicRow;
    public SettingRowUI sfxRow;
    public SettingRowUI ambientRow;

    [Header("Nút")]
    public Button applyButton;
    public Button defaultButton;

    [SerializeField] private InputAction toggleAction = new InputAction("ToggleSettings", InputActionType.Button);
    [SerializeField] private InputAction previousTabAction = new InputAction("SettingsPreviousTab", InputActionType.Button);
    [SerializeField] private InputAction nextTabAction = new InputAction("SettingsNextTab", InputActionType.Button);
    [SerializeField] private InputAction closeAction = new InputAction("CloseSettings", InputActionType.Button);

    public int CurrentTab { get; private set; } = -1;
    public bool IsOpen { get; private set; }
    public event Action<bool> OpenChanged;

    private TabMenuUI tabMenu;

    private void Awake()
    {
        if (toggleAction.bindings.Count == 0)
        {
            toggleAction.AddBinding("<Gamepad>/start");
            toggleAction.AddBinding("<Keyboard>/o");
        }
        if (previousTabAction.bindings.Count == 0)
        {
            previousTabAction.AddBinding("<Gamepad>/leftShoulder");
            previousTabAction.AddBinding("<Keyboard>/q");
        }
        if (nextTabAction.bindings.Count == 0)
        {
            nextTabAction.AddBinding("<Gamepad>/rightShoulder");
            nextTabAction.AddBinding("<Keyboard>/e");
        }
        if (closeAction.bindings.Count == 0)
        {
            closeAction.AddBinding("<Gamepad>/buttonEast");
            closeAction.AddBinding("<Keyboard>/escape");
        }

        for (int i = 0; i < tabs.Length; i++)
        {
            int index = i;
            tabs[i].Clicked += _ => SelectTab(index);
        }
        if (applyButton != null) applyButton.onClick.AddListener(ApplyChanges);
        if (defaultButton != null) defaultButton.onClick.AddListener(ResetToDefault);

        tabMenu = FindAnyObjectByType<TabMenuUI>(FindObjectsInactive.Include);
    }

    private void OnEnable()
    {
        toggleAction.performed += OnToggle;
        previousTabAction.performed += OnPreviousTab;
        nextTabAction.performed += OnNextTab;
        closeAction.performed += OnClose;
        toggleAction.Enable();
        previousTabAction.Enable();
        nextTabAction.Enable();
        closeAction.Enable();
        if (tabMenu != null)
            tabMenu.OpenChanged += OnTabMenuOpenChanged;
    }

    private void OnDisable()
    {
        toggleAction.performed -= OnToggle;
        previousTabAction.performed -= OnPreviousTab;
        nextTabAction.performed -= OnNextTab;
        closeAction.performed -= OnClose;
        toggleAction.Disable();
        previousTabAction.Disable();
        nextTabAction.Disable();
        closeAction.Disable();
        if (tabMenu != null)
            tabMenu.OpenChanged -= OnTabMenuOpenChanged;
    }

    private void Start()
    {
        IsOpen = true; // để Close() chạy đủ và báo OpenChanged(false)
        Close();
    }

    private void Update()
    {
        // Click vào vùng trống làm EventSystem bỏ chọn: trả focus về nội dung tab để tay cầm vẫn điều hướng được.
        if (!IsOpen || EventSystem.current == null)
            return;
        var selected = EventSystem.current.currentSelectedGameObject;
        if (selected == null || !selected.activeInHierarchy)
            FocusPage();
    }

    private void OnToggle(InputAction.CallbackContext _) { if (IsOpen) Close(); else Open(); }
    private void OnPreviousTab(InputAction.CallbackContext _) { if (IsOpen) Step(-1); }
    private void OnNextTab(InputAction.CallbackContext _) { if (IsOpen) Step(1); }
    private void OnClose(InputAction.CallbackContext _) { if (IsOpen) Close(); }

    // Menu Tab mở thì đóng Settings (hai màn dùng chung LB/RB và B).
    private void OnTabMenuOpenChanged(bool open)
    {
        if (open && IsOpen)
            Close();
    }

    public void Open()
    {
        // Đặt IsOpen trước khi đóng menu Tab, để Player vẫn giữ trạng thái khóa input.
        IsOpen = true;
        if (tabMenu != null && tabMenu.IsOpen)
            tabMenu.Close();
        if (panel != null)
            panel.SetActive(true);

        LoadDraft();
        int tab = CurrentTab >= 0 ? CurrentTab : startTab;
        CurrentTab = -1;
        SelectTab(tab);
        OpenChanged?.Invoke(true);
    }

    public void Close()
    {
        if (!IsOpen)
            return;
        IsOpen = false;
        if (panel != null)
            panel.SetActive(false);
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);
        OpenChanged?.Invoke(false);
    }

    private void Step(int direction)
    {
        if (tabs.Length == 0)
            return;
        SelectTab((CurrentTab + direction + tabs.Length) % tabs.Length);
    }

    public void SelectTab(int index)
    {
        if (index < 0 || index >= tabs.Length || index == CurrentTab)
            return;
        CurrentTab = index;
        for (int i = 0; i < tabs.Length; i++)
        {
            tabs[i].SetSelected(i == index);
            if (i < pages.Length && pages[i] != null)
                pages[i].SetActive(i == index);
        }
        LinkNavigation();
        FocusPage();
    }

    // Nối điều hướng theo tab đang mở: các dòng xếp dọc, dòng cuối xuống hai nút, hai nút trái/phải với nhau.
    // Đặt lúc chạy vì mỗi tab có số dòng khác nhau; trỏ tới dòng của tab đang ẩn sẽ làm mất focus.
    private void LinkNavigation()
    {
        var rows = CurrentTab < pages.Length && pages[CurrentTab] != null
            ? pages[CurrentTab].GetComponentsInChildren<SettingRowUI>()
            : new SettingRowUI[0];
        Selectable last = rows.Length > 0 ? rows[rows.Length - 1] : null;

        for (int i = 0; i < rows.Length; i++)
        {
            rows[i].navigation = new Navigation
            {
                mode = Navigation.Mode.Explicit,
                selectOnUp = i > 0 ? rows[i - 1] : null,
                selectOnDown = i < rows.Length - 1 ? rows[i + 1] : (Selectable)applyButton,
            };
        }
        if (applyButton != null)
            applyButton.navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnUp = last, selectOnRight = defaultButton };
        if (defaultButton != null)
            defaultButton.navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnUp = last, selectOnLeft = applyButton };
    }

    // Focus vào dòng đầu tiên của tab; tab không có dòng nào (trống, Điều khiển) thì focus nút Áp dụng.
    private void FocusPage()
    {
        if (EventSystem.current == null || CurrentTab < 0)
            return;
        GameObject target = null;
        if (CurrentTab < pages.Length && pages[CurrentTab] != null)
        {
            var row = pages[CurrentTab].GetComponentInChildren<SettingRowUI>();
            if (row != null)
                target = row.gameObject;
        }
        if (target == null && applyButton != null)
            target = applyButton.gameObject;
        EventSystem.current.SetSelectedGameObject(target);
    }

    private void LoadDraft()
    {
        SetRows(GameSettings.MasterVolume, GameSettings.Music, GameSettings.Sfx, GameSettings.AmbientVolume);
    }

    public void ApplyChanges()
    {
        GameSettings.Set(
            masterRow != null ? masterRow.Value : GameSettings.MasterVolume,
            musicRow != null ? musicRow.IsOn : GameSettings.Music,
            sfxRow != null ? sfxRow.IsOn : GameSettings.Sfx,
            ambientRow != null ? ambientRow.Value : GameSettings.AmbientVolume);
    }

    // Chỉ đưa bản nháp về mặc định; bấm Áp dụng để lưu.
    public void ResetToDefault()
    {
        SetRows(GameSettings.DefaultMasterVolume, GameSettings.DefaultMusic, GameSettings.DefaultSfx, GameSettings.DefaultAmbientVolume);
    }

    private void SetRows(int master, bool music, bool sfx, int ambient)
    {
        if (masterRow != null) masterRow.SetValue(master);
        if (musicRow != null) musicRow.SetValue(music ? 1 : 0);
        if (sfxRow != null) sfxRow.SetValue(sfx ? 1 : 0);
        if (ambientRow != null) ambientRow.SetValue(ambient);
    }
}
