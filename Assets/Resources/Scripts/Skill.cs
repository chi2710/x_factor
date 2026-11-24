using UnityEngine;

// Kĩ năng kế thừa Item (icon, tên, mô tả) để hiển thị được ở ô Thông tin như vật phẩm.
// description = "Mô tả" trong sheet.
[CreateAssetMenu(fileName = "Skill", menuName = "Skill", order = 2)]
public class Skill : Item
{
    [Tooltip("Icon xám, hiện ở ô trong kho khi kĩ năng đã được lắp vào ô Đang sử dụng.")]
    public Sprite iconGray;
    [Tooltip("Loại")]
    public string type;
    [Tooltip("Cơ chế")]
    public string mech;
    [Tooltip("Cách kích hoạt")]
    [TextArea] public string activation;
    [Tooltip("Tiêu hao năng lượng")]
    [TextArea] public string cost;
    [Tooltip("Thời gian hồi")]
    [TextArea] public string cooldown;
    [Tooltip("Ứng dụng")]
    [TextArea] public string application;
    [Tooltip("Các mục khác trong sheet không thuộc trường nào ở trên (vd. Giới hạn)")]
    [TextArea] public string notes;
}
