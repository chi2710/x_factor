using TMPro;
using UnityEngine;

public class SkillDetailUI : MonoBehaviour
{
    [Tooltip("Để trống thì tự tìm text con tên Name / Type / Description / Activation / Cost.")]
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI typeText;
    public TextMeshProUGUI descriptionText;
    public TextMeshProUGUI activationText;
    public TextMeshProUGUI costText;

    private bool resolved;

    public void Show(Skill skill)
    {
        if (skill == null)
        {
            Clear();
            return;
        }
        Resolve();
        Set(nameText, skill.title);
        Set(typeText, $"<color=#19FEFE>Loại:</color> {skill.type}\n<color=#19FEFE>Cơ chế:</color> {skill.mech}");
        Set(descriptionText, $"<color=#19FEFE>Mô tả: </color> {skill.description}");
        Set(activationText, $"<color=#19FEFE>Cách kích hoạt: </color> {skill.activation}");
        Set(costText, $"<color=#19FEFE>Tiêu hao năng lượng:</color> {skill.cost}\n<color=#19FEFE>Ứng dụng: </color> {skill.application}");
    }

    public void Clear()
    {
        Resolve();
        Set(nameText, "");
        Set(typeText, "");
        Set(descriptionText, "");
        Set(activationText, "");
        Set(costText, "");
    }

    private static void Set(TextMeshProUGUI text, string value)
    {
        if (text != null)
            text.text = value;
    }

    // Tìm lười (không trong Awake) vì SkillTabUI có thể gọi Clear trước Awake của component này.
    private void Resolve()
    {
        if (resolved)
            return;
        resolved = true;
        foreach (var text in GetComponentsInChildren<TextMeshProUGUI>(true))
        {
            switch (text.name)
            {
                case "Name": if (nameText == null) nameText = text; break;
                case "Type": if (typeText == null) typeText = text; break;
                case "Description": if (descriptionText == null) descriptionText = text; break;
                case "Activation": if (activationText == null) activationText = text; break;
                case "Cost": if (costText == null) costText = text; break;
            }
        }
    }
}
