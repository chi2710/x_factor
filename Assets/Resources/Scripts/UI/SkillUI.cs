using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SkillUI : SlotUI
{
    public Skill skill;
    public Sprite emptySelected;
    public Image icon;
    public Image iconSelected;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI typeText;
    public GameObject empty;
    public GameObject locked;
    public bool isLocked = false;

    private bool highlighted;
    // Ô trong kho có kĩ năng đã lắp vào ô Đang sử dụng: hiện iconGray và không chọn/bấm được.
    private bool equipped;

    public void SetEquipped(bool equipped)
    {
        this.equipped = equipped;
        Refresh();
    }

    public override Item Data => skill;
    // Giống ItemUI: ô trống vẫn chọn được (ô Thông tin để trống), ô khóa hoặc đã lắp thì không.
    public override bool CanSelect => !isLocked && !equipped;

    public override void Refresh()
    {
        base.Refresh();
        bool hasSkill = !isLocked && skill != null;

        if (icon != null)
            icon.sprite = !hasSkill ? null : equipped && skill.iconGray != null ? skill.iconGray : skill.icon;
        if (nameText != null)
            nameText.text = hasSkill ? skill.title : "";
        if (typeText != null)
            typeText.text = hasSkill ? skill.type : "";
        UpdateVisuals();
    }

    public override void SetHighlighted(bool highlighted)
    {
        this.highlighted = highlighted;
        UpdateVisuals();
    }

    // Khóa -> locked. Không có skill -> empty (khi được chọn: iconSelected với sprite emptySelected, nếu có gán).
    // Có skill -> icon, hoặc iconSelected với sprite iconSelected của skill khi đang được chọn.
    private void UpdateVisuals()
    {
        bool hasSkill = !isLocked && skill != null;
        bool isEmpty = !isLocked && skill == null;
        // Ô đã lắp không bao giờ hiện trạng thái được chọn.
        bool highlighted = this.highlighted && !equipped;
        bool showEmptySelected = isEmpty && highlighted && emptySelected != null;

        if (locked != null)
            locked.SetActive(isLocked);
        if (empty != null)
            empty.SetActive(isEmpty && !showEmptySelected);
        if (icon != null)
            icon.gameObject.SetActive(hasSkill && !highlighted);
        if (iconSelected != null)
        {
            if (hasSkill)
                iconSelected.sprite = skill.iconSelected != null ? skill.iconSelected : skill.icon;
            else if (showEmptySelected)
                iconSelected.sprite = emptySelected;
            iconSelected.gameObject.SetActive((hasSkill || showEmptySelected) && highlighted);
        }
    }
}
