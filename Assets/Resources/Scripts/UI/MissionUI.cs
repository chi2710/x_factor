using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MissionUI : SlotUI
{
    public Mission mission;
    public GameObject selected;
    public Image icon;
    public Image icon1;
    public GameObject[] normal;
    public TextMeshProUGUI[] titleTexts;
    [Tooltip("Material preset (có Glow) cho tên nhiệm vụ khi được chọn. Để trống thì không đổi.")]
    public Material selectedMaterial;
    [Tooltip("Màu tên nhiệm vụ khi được chọn.")]
    public Color selectedColor = Color.white;

    private Material[] normalMaterials;
    private Color[] normalColors;

    public override Item Data => mission;

    public override void Refresh()
    {
        base.Refresh();
        if (mission == null)
            return;
        foreach (var text in titleTexts)
            text.text = mission.title;
        icon.sprite = mission.icon;
        icon1.sprite = mission.iconSelected;
        icon.SetNativeSize();
        icon1.SetNativeSize();
    }

    // Bấm xuống ở nhiệm vụ cuối không nhảy ra ngoài danh sách (sang lưới vật phẩm nâng cấp).
    // Tính lại mỗi lần ô được chọn vì vị trí đổi khi danh sách cuộn: lấy hàng xóm theo kiểu Automatic,
    // rồi chuyển sang Explicit và bỏ hướng xuống nếu ô đó không phải nhiệm vụ.
    public void KeepDownInList()
    {
        selectable.navigation = new Navigation { mode = Navigation.Mode.Automatic };
        var up = selectable.FindSelectableOnUp();
        var left = selectable.FindSelectableOnLeft();
        var right = selectable.FindSelectableOnRight();
        var down = selectable.FindSelectableOnDown();
        if (down != null && down.GetComponent<MissionUI>() == null)
            down = null;
        selectable.navigation = new Navigation
        {
            mode = Navigation.Mode.Explicit,
            selectOnUp = up,
            selectOnLeft = left,
            selectOnRight = right,
            selectOnDown = down,
        };
    }

    public override void SetHighlighted(bool highlighted)
    {
        selected.SetActive(highlighted);
        foreach (var go in normal)
            go.SetActive(!highlighted);

        // Lưu material và màu gốc lần đầu (SetHighlighted(false) được gọi từ Awake), rồi đổi sharedMaterial như TabButtonUI.
        if (normalMaterials == null)
        {
            normalMaterials = new Material[titleTexts.Length];
            normalColors = new Color[titleTexts.Length];
            for (int i = 0; i < titleTexts.Length; i++)
            {
                normalMaterials[i] = titleTexts[i].fontSharedMaterial;
                normalColors[i] = titleTexts[i].color;
            }
        }
        for (int i = 0; i < titleTexts.Length; i++)
        {
            titleTexts[i].color = highlighted ? selectedColor : normalColors[i];
            if (selectedMaterial != null)
                titleTexts[i].fontSharedMaterial = highlighted ? selectedMaterial : normalMaterials[i];
        }
    }
}
