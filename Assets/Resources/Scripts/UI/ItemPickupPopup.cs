using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// Popup "Nhận được vật phẩm": hiện mỗi khi Inventory nhận item mới, dừng game cho tới khi đóng.
// Nhặt liền nhiều item thì hiện lần lượt. A / Enter / click để đóng.
public class ItemPickupPopup : MonoBehaviour
{
    [Tooltip("Phần hiển thị được bật/tắt.")]
    public GameObject panel;
    public ItemDetailUI detail;

    [SerializeField] private InputAction closeAction = new InputAction("ClosePickupPopup", InputActionType.Button);

    public bool IsOpen { get; private set; }
    public event Action<bool> OpenChanged;

    private readonly Queue<Item> queue = new Queue<Item>();
    private float previousTimeScale = 1f;

    private void Awake()
    {
        if (closeAction.bindings.Count == 0)
        {
            closeAction.AddBinding("<Gamepad>/buttonSouth");
            closeAction.AddBinding("<Keyboard>/enter");
            closeAction.AddBinding("<Mouse>/leftButton");
        }
        closeAction.performed += OnClose;
        if (panel != null)
            panel.SetActive(false);
        Inventory.ItemAdded += Show;
    }

    private void OnDestroy()
    {
        Inventory.ItemAdded -= Show;
        closeAction.performed -= OnClose;
        closeAction.Disable();
        if (IsOpen)
            Time.timeScale = previousTimeScale;
    }

    public void Show(Item item)
    {
        if (item == null)
            return;
        queue.Enqueue(item);
        if (!IsOpen)
            ShowNext();
    }

    private void ShowNext()
    {
        if (detail != null)
            detail.showDetail(queue.Dequeue());
        if (panel != null)
            panel.SetActive(true);
        if (IsOpen)
            return;
        IsOpen = true;
        previousTimeScale = Time.timeScale;
        Time.timeScale = 0f;
        closeAction.Enable();
        OpenChanged?.Invoke(true);
    }

    private void OnClose(InputAction.CallbackContext _)
    {
        if (!IsOpen)
            return;
        if (queue.Count > 0)
        {
            ShowNext();
            return;
        }
        IsOpen = false;
        if (panel != null)
            panel.SetActive(false);
        closeAction.Disable();
        Time.timeScale = previousTimeScale;
        OpenChanged?.Invoke(false);
    }
}
