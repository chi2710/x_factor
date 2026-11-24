using UnityEngine;

// Nội dung của một tab trong TabMenuUI.
public interface ITabContent
{
    // Ô sẽ nhận focus của tay cầm khi tab được mở.
    GameObject GetFocusTarget();

    // Xử lý nút B trước khi menu đóng. Trả về true nếu tab đã dùng nút B (vd. hủy thao tác đang dở).
    bool HandleBack();
}
