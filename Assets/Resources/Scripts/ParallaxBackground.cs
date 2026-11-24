using Unity.Cinemachine;
using UnityEngine;

// Gắn vào một lớp nền (BG FAR / BG MID / BG NEAR). Lớp nền trượt theo camera với tỉ lệ parallax:
// 0 = đứng yên như mặt đất, 1 = dính chặt vào camera (xa vô cực). Lớp càng xa thì để số càng lớn.
// Vị trí đặt trong Scene là vị trí khi camera nằm đúng tâm lớp nền; camera càng lệch thì lớp nền càng trượt theo.
public class ParallaxBackground : MonoBehaviour
{
    [Range(0f, 1f)] public float parallaxX = 0.5f;
    [Range(0f, 1f)] public float parallaxY = 0.5f;

    Transform cam;
    Vector3 startPos;

    void Awake()
    {
        cam = Camera.main.transform;
        startPos = transform.position;
    }

    // Chạy sau khi Cinemachine đã di chuyển camera trong frame, tránh nền bị giật.
    void OnEnable() => CinemachineCore.CameraUpdatedEvent.AddListener(OnCameraUpdated);
    void OnDisable() => CinemachineCore.CameraUpdatedEvent.RemoveListener(OnCameraUpdated);

    void OnCameraUpdated(CinemachineBrain brain)
    {
        Vector3 delta = cam.position - startPos;
        transform.position = new Vector3(
            startPos.x + delta.x * parallaxX,
            startPos.y + delta.y * parallaxY,
            startPos.z);
    }
}
