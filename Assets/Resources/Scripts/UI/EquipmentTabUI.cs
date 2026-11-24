using System.Linq;
using System.Text;
using TMPro;
using UnityEngine;

// Tab Trang bị: lưới Vũ khí (trái), lưới Thiết bị + "Đang trang bị" (phải).
// - Vũ khí và thiết bị được chọn độc lập (giống nhiệm vụ/vật phẩm ở tab Vật phẩm).
// - Trỏ tới vũ khí: hiện tên + mô tả ở phần chi tiết bên trái.
// - Trỏ tới thiết bị có dữ liệu: hiện khung "Thông tin thiết bị" cạnh ô đó.
// - Nhấn A ở thiết bị: lắp vào ô "Đang trang bị" trống đầu tiên (không trùng). Nhấn A ở ô "Đang trang bị": tháo ra.
public class EquipmentTabUI : MonoBehaviour, ITabContent
{
    [Header("Chi tiết vũ khí")]
    public TextMeshProUGUI weaponNameText;
    public TextMeshProUGUI weaponDescriptionText;
    [Tooltip("Màu nhãn \"Loại:\", \"Sát thương:\"... trong mô tả.")]
    public Color labelColor = new Color32(0x19, 0xFE, 0xFE, 0xFF);
    [Tooltip("Nút Nâng cấp: chỉ hiện khi vũ khí đang chọn chưa bị khóa.")]
    public GameObject upgradeButton;

    [Header("Khung thông tin thiết bị")]
    public RectTransform tooltip;
    public TextMeshProUGUI tooltipText;
    [Tooltip("Vị trí khung so với tâm ô thiết bị đang trỏ tới.")]
    public Vector2 tooltipOffset = new Vector2(232, 132);

    private EquipmentSlotUI[] slots;
    private EquipmentSlotUI currentWeapon;
    private EquipmentSlotUI currentDevice;
    private EquipmentSlotUI lastFocused;

    private void Awake()
    {
        slots = GetComponentsInChildren<EquipmentSlotUI>(true);
        foreach (var slot in slots)
        {
            slot.Selected += OnSlotSelected;
            slot.Submitted += OnSlotSubmitted;
        }
    }

    // Start: chạy sau Awake của các ô (vốn reset highlight về false).
    private void Start()
    {
        HideTooltip();
        if (currentWeapon == null)
            ShowWeapon(slots.FirstOrDefault(s => s.kind == EquipmentSlotUI.Kind.Weapon && s.item != null));
    }

    private void OnSlotSelected(SlotUI slot)
    {
        var s = (EquipmentSlotUI)slot;
        lastFocused = s;
        if (s.kind == EquipmentSlotUI.Kind.Weapon)
        {
            ShowWeapon(s);
            HideTooltip();
        }
        else
            ShowDevice(s);
    }

    private void OnSlotSubmitted(SlotUI slot)
    {
        var s = (EquipmentSlotUI)slot;
        if (s.kind == EquipmentSlotUI.Kind.Device && s.item != null)
        {
            if (slots.Any(e => e.kind == EquipmentSlotUI.Kind.Equipped && e.item == s.item))
                return;
            var emptySlot = slots.FirstOrDefault(e => e.kind == EquipmentSlotUI.Kind.Equipped && !e.isLocked && e.item == null);
            if (emptySlot == null)
                return;
            emptySlot.item = s.item;
            emptySlot.Refresh();
        }
        else if (s.kind == EquipmentSlotUI.Kind.Equipped && s.item != null)
        {
            s.item = null;
            s.Refresh();
            ShowDevice(s);
        }
    }

    private void ShowWeapon(EquipmentSlotUI slot)
    {
        if (currentWeapon != null && currentWeapon != slot)
            currentWeapon.SetHighlighted(false);
        currentWeapon = slot;
        if (slot != null)
            slot.SetHighlighted(true);

        var item = slot != null ? slot.item : null;
        if (weaponNameText != null)
            weaponNameText.text = item != null ? item.title : "";
        if (weaponDescriptionText != null)
            weaponDescriptionText.text = item != null ? ColorLabels(item.description) : "";
        if (upgradeButton != null)
            upgradeButton.SetActive(item != null && !slot.isLocked);
    }

    private void ShowDevice(EquipmentSlotUI slot)
    {
        if (currentDevice != null && currentDevice != slot)
            currentDevice.SetHighlighted(false);
        currentDevice = slot;
        slot.SetHighlighted(true);

        if (slot.item == null || tooltip == null)
        {
            HideTooltip();
            return;
        }
        tooltip.gameObject.SetActive(true);
        tooltip.position = slot.transform.position;
        tooltip.anchoredPosition += tooltipOffset;
        if (tooltipText != null)
            tooltipText.text = $"<b>{slot.item.title}:</b> {slot.item.description}";
    }

    private void HideTooltip()
    {
        if (tooltip != null)
            tooltip.gameObject.SetActive(false);
    }

    // "Loại: Vũ khí cận chiến" -> nhãn "Loại:" được tô màu. Dòng không có nhãn giữ nguyên.
    private string ColorLabels(string text)
    {
        if (string.IsNullOrEmpty(text))
            return "";
        string hex = ColorUtility.ToHtmlStringRGB(labelColor);
        var sb = new StringBuilder();
        bool prevLabeled = true;
        foreach (var line in text.Replace("\r", "").Split('\n'))
        {
            int colon = line.IndexOf(':');
            bool labeled = colon > 0 && colon <= 25;
            if (sb.Length > 0)
                // Dòng trống ngăn đoạn mô tả với khối chỉ số "Nhãn: giá trị" (như mockup).
                sb.Append(labeled && !prevLabeled ? "\n\n" : "\n");
            if (labeled)
                sb.Append($"<color=#{hex}>{line.Substring(0, colon + 1)}</color>{line.Substring(colon + 1)}");
            else
                sb.Append(line);
            prevLabeled = labeled;
        }
        return sb.ToString();
    }

    public GameObject GetFocusTarget()
    {
        if (slots == null)
            return null; // Chưa Awake.
        if (lastFocused != null && lastFocused.CanSelect)
            return lastFocused.gameObject;
        var first = currentWeapon != null ? currentWeapon : slots.FirstOrDefault(s => s.CanSelect);
        return first != null ? first.gameObject : null;
    }

    public bool HandleBack() => false;
}
