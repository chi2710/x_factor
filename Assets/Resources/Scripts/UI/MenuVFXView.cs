using UnityEngine;

// Đặt trên ô hiển thị VFX của menu (RawImage dùng RenderTexture).
// Chỉ bật "sân khấu" VFX (camera + particle) khi ô đang hiện, để menu đóng thì không tốn render.
public class MenuVFXView : MonoBehaviour
{
    [Tooltip("Object chứa camera VFX và particle, nằm ngoài Canvas.")]
    public GameObject stage;

    private void OnEnable()
    {
        if (stage != null)
            stage.SetActive(true);
    }

    private void OnDisable()
    {
        if (stage != null)
            stage.SetActive(false);
    }
}
