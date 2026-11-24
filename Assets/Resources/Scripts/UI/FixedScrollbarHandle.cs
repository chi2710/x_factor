using UnityEngine;
using UnityEngine.UI;

// ScrollRect tự tính Scrollbar.size = khung nhìn / nội dung mỗi frame, nên chỉnh size trong Inspector không giữ được.
// Component này ghi đè lại size sau khi ScrollRect cập nhật, để handle dài cố định như mockup.
[RequireComponent(typeof(Scrollbar))]
public class FixedScrollbarHandle : MonoBehaviour
{
    [Range(0.01f, 1f)]
    [Tooltip("Độ dài handle so với cả thanh (0–1).")]
    public float size = 0.5f;

    private Scrollbar scrollbar;

    private void Awake()
    {
        scrollbar = GetComponent<Scrollbar>();
    }

    // willRenderCanvases chạy sau LateUpdate và sau lượt rebuild layout của ScrollRect (cũng gán size).
    private void OnEnable()
    {
        Canvas.willRenderCanvases += Apply;
    }

    private void OnDisable()
    {
        Canvas.willRenderCanvases -= Apply;
    }

    private void Apply()
    {
        if (!Mathf.Approximately(scrollbar.size, size))
            scrollbar.size = size;
    }
}
