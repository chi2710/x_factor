using UnityEngine;
using UnityEngine.UI;

// Điểm đánh dấu nhiệm vụ trên bản đồ. Trỏ tới thì hiện khung chọn (selected) và thông tin nhiệm vụ ở MapTabUI.
public class MapMarkerUI : SlotUI
{
    public Mission mission;
    public Image icon;
    public GameObject selected;
    [Tooltip("Tỉ lệ phóng to khi điểm đang được chọn. Điểm không được chọn giữ kích thước gốc.")]
    public float selectedScale = 1.4f;

    // Chỉ điểm có gán nhiệm vụ mới chọn được (CanSelect mặc định của SlotUI: Data != null).
    // Điểm "!" đỏ chưa có nhiệm vụ nên D-pad bỏ qua và click không chọn.
    public override Item Data => mission;

    public override void SetHighlighted(bool highlighted)
    {
        if (selected != null)
            selected.SetActive(highlighted);
        transform.localScale = Vector3.one * (highlighted ? selectedScale : 1f);
    }
}
