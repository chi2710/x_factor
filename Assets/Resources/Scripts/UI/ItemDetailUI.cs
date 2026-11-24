using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ItemDetailUI : MonoBehaviour
{
    public Image icon;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI locationText;
    public TextMeshProUGUI descriptionText;

    public void showDetail (Item item)
    {
        if (item == null)
        {
            clear();
            return;
        }
        // Ảnh chi tiết dùng iconSelected; item chưa có iconSelected thì dùng icon thường.
        icon.sprite = item.iconSelected != null ? item.iconSelected : item.icon;
        icon.enabled = icon.sprite != null;
        nameText.text = item.title;
        if (item is Skill skill)
        {
            locationText.text = skill.type;
            descriptionText.text = SkillDescription(skill);
        }
        else
        {
            locationText.text = item.location;
            descriptionText.text = item.description;
        }
        UpdateLocationVisible();
    }

    // Ẩn cả cụm Location (kèm 2 mũi tên con) khi không có chữ, để không còn hai mũi tên trơ trọi.
    private void UpdateLocationVisible()
    {
        locationText.gameObject.SetActive(!string.IsNullOrEmpty(locationText.text));
    }

    // Ghép các trường của kĩ năng thành một đoạn, bỏ qua trường trống.
    private static string SkillDescription(Skill skill)
    {
        var sb = new System.Text.StringBuilder(skill.description);
        void Add(string label, string value)
        {
            if (string.IsNullOrEmpty(value))
                return;
            if (sb.Length > 0)
                sb.Append("\n\n");
            sb.Append("<b>").Append(label).Append(":</b> ").Append(value);
        }
        Add("Cơ chế", skill.mech);
        Add("Cách kích hoạt", skill.activation);
        Add("Tiêu hao năng lượng", skill.cost);
        Add("Thời gian hồi", skill.cooldown);
        Add("Ứng dụng", skill.application);
        Add("Ghi chú", skill.notes);
        return sb.ToString();
    }

    public void clear()
    {
        icon.sprite = null;
        icon.enabled = false;
        nameText.text = "";
        locationText.text = "";
        descriptionText.text = "";
        UpdateLocationVisible();
    }
}
