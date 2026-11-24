using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class TabMenuUI : MonoBehaviour
{
    [Serializable]
    public class Tab
    {
        public TabButtonUI button;
        public GameObject content;
        [Tooltip("Ẩn thanh gợi ý nút (buttonHints) khi tab này đang mở, vd. tab Bản đồ có gợi ý riêng.")]
        public bool hideButtonHints;
    }

    public Tab[] tabs;
    [Tooltip("Thanh gợi ý nút chung (Điều hướng / Chọn / Quay lại).")]
    public GameObject buttonHints;
    public int startTab = 1;
    public bool wrapAround = true;

    [Tooltip("Phần hiển thị của menu được ẩn/hiện khi đóng/mở. Để trống thì dùng con tên \"Content\".")]
    public GameObject menuRoot;
    public bool startOpen = true;

    [SerializeField] private InputAction previousTabAction = new InputAction("PreviousTab", InputActionType.Button);
    [SerializeField] private InputAction nextTabAction = new InputAction("NextTab", InputActionType.Button);
    [SerializeField] private InputAction closeAction = new InputAction("CloseMenu", InputActionType.Button);
    [SerializeField] private InputAction toggleAction = new InputAction("ToggleMenu", InputActionType.Button);

    public int CurrentTab { get; private set; } = -1;
    public bool IsOpen { get; private set; }
    public event Action<int> TabChanged;
    public event Action<bool> OpenChanged;

    private void Awake()
    {
        // Mặc định: LB/RB (Q/E) đổi tab, B (Esc) thoát, Select/View (Tab) bật/tắt menu. Có thể đổi trong Inspector.
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
        if (toggleAction.bindings.Count == 0)
        {
            toggleAction.AddBinding("<Gamepad>/select");
            toggleAction.AddBinding("<Keyboard>/tab");
        }

        // Tìm theo tên, không lấy con đầu tiên: HUD cũng là con của UI, nếu HUD đứng đầu thì menu
        // bị coi là luôn mở (HUD luôn hiện) và Player bị khóa input mãi.
        if (menuRoot == null)
        {
            var content = transform.Find("Content");
            if (content != null)
                menuRoot = content.gameObject;
            else
                Debug.LogWarning("[TabMenuUI] Chưa gán menuRoot và không có con tên \"Content\".", this);
        }

        for (int i = 0; i < tabs.Length; i++)
        {
            int index = i;
            tabs[i].button.Clicked += _ => SelectTab(index);
        }
    }

    private void OnEnable()
    {
        previousTabAction.performed += OnPreviousTab;
        nextTabAction.performed += OnNextTab;
        closeAction.performed += OnClose;
        toggleAction.performed += OnToggle;
        previousTabAction.Enable();
        nextTabAction.Enable();
        closeAction.Enable();
        toggleAction.Enable();
    }

    private void OnDisable()
    {
        previousTabAction.performed -= OnPreviousTab;
        nextTabAction.performed -= OnNextTab;
        closeAction.performed -= OnClose;
        toggleAction.performed -= OnToggle;
        previousTabAction.Disable();
        nextTabAction.Disable();
        closeAction.Disable();
        toggleAction.Disable();
    }

    private void Start()
    {
        SelectTab(startTab);
        // Nếu menu đang bị tắt sẵn trong scene (menuRoot inactive) thì giữ nguyên trạng thái đóng.
        if (startOpen && (menuRoot == null || menuRoot.activeSelf))
            Open();
        else
            Close();
    }

    private void Update()
    {
        // Menu bị bật/tắt từ bên ngoài (Inspector, script khác): đồng bộ lại trạng thái mở/đóng.
        bool visible = menuRoot == null || menuRoot.activeInHierarchy;
        if (visible != IsOpen)
        {
            if (visible)
                Open();
            else
                Close();
            return;
        }

        // Click chuột vào vùng trống (hoặc vào tab đang mở) làm EventSystem bỏ chọn ô hiện tại
        // (Deselect On Background Click), khi đó phím mũi tên/D-pad không còn tác dụng. Trả focus về tab đang mở.
        if (!IsOpen || CurrentTab < 0 || EventSystem.current == null)
            return;
        var selected = EventSystem.current.currentSelectedGameObject;
        if (selected == null || !selected.activeInHierarchy)
            FocusTabContent(tabs[CurrentTab].content);
    }

    private void OnPreviousTab(InputAction.CallbackContext _) { if (IsOpen) Step(-1); }
    private void OnNextTab(InputAction.CallbackContext _) { if (IsOpen) Step(1); }
    private void OnClose(InputAction.CallbackContext _)
    {
        if (!IsOpen)
            return;
        // Cho tab hiện tại dùng nút B trước (vd. hủy chọn kĩ năng thay thế); nếu không thì đóng menu.
        var content = CurrentTab >= 0 && tabs[CurrentTab].content != null
            ? tabs[CurrentTab].content.GetComponent<ITabContent>() : null;
        if (content != null && content.HandleBack())
            return;
        Close();
    }
    private void OnToggle(InputAction.CallbackContext _) { if (IsOpen) Close(); else Open(); }

    public void Open()
    {
        IsOpen = true;
        if (menuRoot != null)
            menuRoot.SetActive(true);
        if (CurrentTab >= 0)
            FocusTabContent(tabs[CurrentTab].content);
        OpenChanged?.Invoke(true);
    }

    public void Close()
    {
        IsOpen = false;
        if (menuRoot != null)
            menuRoot.SetActive(false);
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);
        OpenChanged?.Invoke(false);
    }

    private void Step(int direction)
    {
        int next = CurrentTab + direction;
        if (wrapAround)
            next = (next + tabs.Length) % tabs.Length;
        else
            next = Mathf.Clamp(next, 0, tabs.Length - 1);
        SelectTab(next);
    }

    public void SelectTab(int index)
    {
        if (index < 0 || index >= tabs.Length || index == CurrentTab)
            return;

        CurrentTab = index;
        for (int i = 0; i < tabs.Length; i++)
        {
            tabs[i].button.SetSelected(i == index);
            if (tabs[i].content != null)
                tabs[i].content.SetActive(i == index);
        }
        if (buttonHints != null)
            buttonHints.SetActive(!tabs[index].hideButtonHints);

        FocusTabContent(tabs[index].content);
        TabChanged?.Invoke(index);
    }

    // Đặt focus của EventSystem vào ô đầu tiên của tab để tay cầm điều hướng được ngay.
    private void FocusTabContent(GameObject content)
    {
        // Tab chưa hiển thị (vd. menu đang tắt): các ô chưa Awake, chưa thể focus.
        if (EventSystem.current == null || (content != null && !content.activeInHierarchy))
            return;

        GameObject target = null;
        if (content != null)
        {
            var tabContent = content.GetComponent<ITabContent>();
            if (tabContent != null)
                target = tabContent.GetFocusTarget();
            else
                foreach (var s in content.GetComponentsInChildren<Selectable>())
                    if (s.IsInteractable()) { target = s.gameObject; break; }
        }
        EventSystem.current.SetSelectedGameObject(target);
    }
}
