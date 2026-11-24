using UnityEngine;
using UnityEngine.UI;

// Ô trong tab Trang bị: dùng cho ô vũ khí, ô thiết bị và ô "Đang trang bị".
// Khóa -> locked. Đang được chọn -> selected. Còn lại -> normal (hoặc empty nếu ô trống và có gán empty, vd. ô "+").
public class EquipmentSlotUI : SlotUI
{
    public enum Kind { Weapon, Device, Equipped }

    public Kind kind;
    public Item item;
    public Image icon;
    public GameObject normal;
    public GameObject selected;
    public GameObject locked;
    [Tooltip("Hiện thay cho normal khi ô trống (vd. ô \"+\" ở Đang trang bị). Để trống thì luôn dùng normal.")]
    public GameObject empty;
    public bool isLocked;
    [Tooltip("Màu icon khi ô bị khóa (vũ khí khóa vẫn hiện mờ dưới khung khóa).")]
    public Color lockedIconColor = new Color(1f, 1f, 1f, 0.4f);
    [Tooltip("Hình khung chọn khi ô trống. Để trống thì luôn dùng hình gốc của selected.")]
    public Sprite selectedEmptySprite;

    private bool highlighted;
    private Image selectedImage;
    private Sprite selectedFilledSprite;

    public override Item Data => item;
    // Vũ khí bị khóa vẫn trỏ tới được để xem thông tin (nhưng không nâng cấp được); ô khóa khác thì bỏ qua.
    public override bool CanSelect => !isLocked || (kind == Kind.Weapon && item != null);

    public override void Refresh()
    {
        base.Refresh();
        UpdateVisuals();
    }

    public override void SetHighlighted(bool highlighted)
    {
        this.highlighted = highlighted;
        UpdateVisuals();
    }

    private void UpdateVisuals()
    {
        bool isEmpty = item == null;
        bool showEmpty = !isLocked && !highlighted && isEmpty && empty != null;

        if (locked != null)
        {
            locked.SetActive(isLocked);
            // Khung khóa (có ổ khóa ở giữa) nằm trên icon, như mockup.
            if (isLocked)
                locked.transform.SetAsLastSibling();
        }
        if (selected != null)
        {
            UpdateSelectedSprite(isEmpty);
            selected.SetActive(CanSelect && highlighted);
        }
        if (empty != null)
            empty.SetActive(showEmpty);
        if (normal != null)
            normal.SetActive(!isLocked && !highlighted && !showEmpty);
        if (icon != null)
        {
            bool hasIcon = !isEmpty && item.icon != null;
            icon.sprite = hasIcon ? item.icon : null;
            icon.color = isLocked ? lockedIconColor : Color.white;
            icon.gameObject.SetActive(hasIcon);
        }
    }

    private void UpdateSelectedSprite(bool isEmpty)
    {
        if (selectedEmptySprite == null)
            return;
        if (selectedImage == null)
        {
            selectedImage = selected.GetComponent<Image>();
            if (selectedImage == null)
                return;
            selectedFilledSprite = selectedImage.sprite;
        }
        selectedImage.sprite = isEmpty ? selectedEmptySprite : selectedFilledSprite;
    }
}
