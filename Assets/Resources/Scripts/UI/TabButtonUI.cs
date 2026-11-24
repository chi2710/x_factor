using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class TabButtonUI : MonoBehaviour
{
    public GameObject onSelected;
    public TextMeshProUGUI label;
    public Color normalColor = new Color(0.45f, 0.75f, 0.75f);
    public Color selectedColor = new Color(0.3f, 1f, 0.85f);
    [Tooltip("Material preset (có Glow) cho chữ khi tab được chọn. Để trống thì chỉ đổi màu.")]
    public Material selectedMaterial;
    [Tooltip("Dịch chữ sang phải khi được chọn (chừa chỗ cho mũi tên trong onSelected).")]
    public float selectedLabelOffset = 0f;
    [Tooltip("Icon của tab: alpha 255 khi được chọn, 128 khi không.")]
    public Graphic icon;

    public event Action<TabButtonUI> Clicked;

    private Material normalMaterial;
    private Vector2 labelBasePos;
    private bool captured;

    private void Awake()
    {
        CaptureLabel();
        var button = GetComponent<Button>();
        // Tab chỉ đổi bằng click hoặc LB/RB, không cho D-pad điều hướng lên thanh tab.
        button.navigation = new Navigation { mode = Navigation.Mode.None };
        button.onClick.AddListener(() => Clicked?.Invoke(this));
    }

    public void SetSelected(bool isSelected)
    {
        if (onSelected != null)
            onSelected.SetActive(isSelected);
        if (icon != null)
        {
            var c = icon.color;
            c.a = isSelected ? 1f : 128f / 255f;
            icon.color = c;
        }
        if (label == null)
            return;
        CaptureLabel();
        label.color = isSelected ? selectedColor : normalColor;
        label.rectTransform.anchoredPosition = labelBasePos + new Vector2(isSelected ? selectedLabelOffset : 0f, 0f);
        // Đổi sharedMaterial (không tạo instance) để chỉnh preset Glow là mọi tab đổi theo.
        if (selectedMaterial != null && normalMaterial != null)
            label.fontSharedMaterial = isSelected ? selectedMaterial : normalMaterial;
    }

    // Lưu trạng thái gốc của chữ một lần. Gọi cả từ SetSelected vì tab nằm trong panel đang tắt
    // có thể được SetSelected trước khi Awake chạy.
    private void CaptureLabel()
    {
        if (captured || label == null)
            return;
        captured = true;
        normalMaterial = label.fontSharedMaterial;
        labelBasePos = label.rectTransform.anchoredPosition;
    }
}
