using UnityEngine;
using UnityEngine.UI;

public class ItemUI : SlotUI
{
    public Item item;
    public Image icon;
    public GameObject selected;
    public GameObject locked;
    public GameObject normal;
    public bool isLocked = false;

    public override Item Data => item;
    // Ô trống vẫn chọn được (ô Thông tin sẽ để trống); chỉ ô bị khóa là không chọn được.
    public override bool CanSelect => !isLocked;

    public override void Refresh()
    {
        base.Refresh();
        bool hasIcon = !isLocked && item != null && item.icon != null;
        icon.sprite = hasIcon ? item.icon : null;
        icon.enabled = hasIcon;
        locked.SetActive(isLocked);
    }

    public override void SetHighlighted(bool highlighted)
    {
        if (isLocked)
        {
            selected.SetActive(false);
            normal.SetActive(false);
            return;
        }
        selected.SetActive(highlighted);
        normal.SetActive(!highlighted);
    }
}
