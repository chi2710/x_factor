using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Một dòng cài đặt: nhãn bên trái, giá trị bên phải.
// Lên/xuống điều hướng như Selectable thường; trái/phải (D-pad, stick, mũi tên) đổi giá trị.
// Click chuột vào nửa trái/phải vùng giá trị cũng giảm/tăng.
public class SettingRowUI : Selectable, IPointerClickHandler
{
    public enum Kind { Slider, Toggle }

    public Kind kind = Kind.Slider;
    public TextMeshProUGUI label;
    public TextMeshProUGUI valueText;

    [Header("Highlight")]
    public Color textColor = new Color32(0x3C, 0xE8, 0xE8, 0xFF);
    public Color selectedTextColor = Color.white;
    public GameObject highlight;       // nền đỏ khi dòng đang chọn
    public GameObject arrowsNormal;    // mũi tên cyan (Toggle)
    public GameObject arrowsSelected;  // mũi tên trắng (Toggle)

    [Header("Slider")]
    public RectTransform track;        // thanh trượt, mép trái là 0, mép phải là max
    public RectTransform marker;       // vạch đỏ dưới con số
    public int min = 0;
    public int max = 100;
    public int step = 5;

    [Header("Toggle")]
    public string onText = "BẬT";
    public string offText = "TẮT";

    public int Value { get; private set; }
    public bool IsOn => Value != 0;

    public event Action<SettingRowUI> ValueChanged;

    protected override void Awake()
    {
        base.Awake();
        transition = Transition.None;
        SetHighlighted(false);
    }

    // Đặt giá trị từ code (không bắn ValueChanged).
    public void SetValue(int value)
    {
        Value = kind == Kind.Toggle ? (value != 0 ? 1 : 0) : Mathf.Clamp(value, min, max);
        Refresh();
    }

    public void Step(int direction)
    {
        int old = Value;
        if (kind == Kind.Toggle)
            Value = Value != 0 ? 0 : 1;
        else
            Value = Mathf.Clamp(Value + direction * step, min, max);
        if (Value == old)
            return;
        Refresh();
        ValueChanged?.Invoke(this);
    }

    public override void OnMove(AxisEventData eventData)
    {
        if (eventData.moveDir == MoveDirection.Left || eventData.moveDir == MoveDirection.Right)
        {
            Step(eventData.moveDir == MoveDirection.Right ? 1 : -1);
            eventData.Use();
            return;
        }
        base.OnMove(eventData);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left || valueText == null)
            return;
        // So với tâm vùng giá trị: bên phải tăng, bên trái giảm. Click vào nhãn thì chỉ chọn dòng.
        RectTransform area = kind == Kind.Slider && track != null ? track : valueText.rectTransform;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(area, eventData.position, eventData.pressEventCamera, out var local))
            return;
        Rect r = area.rect;
        if (local.x < r.xMin - 60f)
            return;
        Step(local.x >= r.center.x ? 1 : -1);
    }

    public override void OnSelect(BaseEventData eventData)
    {
        base.OnSelect(eventData);
        SetHighlighted(true);
    }

    public override void OnDeselect(BaseEventData eventData)
    {
        base.OnDeselect(eventData);
        SetHighlighted(false);
    }

    private void SetHighlighted(bool on)
    {
        if (highlight != null) highlight.SetActive(on);
        if (arrowsNormal != null) arrowsNormal.SetActive(!on);
        if (arrowsSelected != null) arrowsSelected.SetActive(on);
        // Dòng Slider giữ số màu cyan như ảnh mẫu; chỉ nhãn (và giá trị Toggle) đổi sang trắng.
        if (label != null) label.color = on ? selectedTextColor : textColor;
        if (valueText != null && kind == Kind.Toggle) valueText.color = on ? selectedTextColor : textColor;
    }

    private void Refresh()
    {
        if (kind == Kind.Toggle)
        {
            if (valueText != null)
                valueText.text = IsOn ? onText : offText;
            return;
        }

        if (valueText != null)
            valueText.text = Value.ToString();
        if (track == null || marker == null)
            return;

        // Vạch đỏ chạy từ mép trái (min) tới mép phải (max), không tràn ra ngoài thanh; số nằm ngay trên vạch.
        float t = max > min ? (Value - min) / (float)(max - min) : 0f;
        float width = track.rect.width - marker.rect.width;
        float x = track.anchoredPosition.x - track.rect.width * track.pivot.x + marker.rect.width * 0.5f + width * t;
        marker.anchoredPosition = new Vector2(x, marker.anchoredPosition.y);
        if (valueText != null)
            valueText.rectTransform.anchoredPosition = new Vector2(x, valueText.rectTransform.anchoredPosition.y);
    }
}
