using UnityEngine;

// Cuộn ảnh bản đồ để vị trí Player luôn nằm ở icon Location (giữa khung Container).
// Vùng worldBounds trong thế giới ứng với vùng mapArea trên ảnh Map.
public class MinimapUI : MonoBehaviour
{
    [Tooltip("Ảnh bản đồ (con của Container, neo giữa).")]
    public RectTransform map;
    [Tooltip("Icon vị trí người chơi; lật ngang theo hướng mặt của Player.")]
    public RectTransform location;
    [Tooltip("Vùng thế giới mà ảnh Map thể hiện (vd. Scene01/CameraBounds).")]
    public Collider2D worldBounds;
    [Tooltip("Vùng trên ảnh Map ứng với worldBounds, tỉ lệ 0..1 tính từ góc trái dưới của ảnh.")]
    public Rect mapArea = new Rect(0f, 0f, 1f, 1f);
    [Tooltip("Để trống thì tự tìm Player trong scene.")]
    public Transform player;

    private float locationScaleX = 1f;

    private void Start()
    {
        if (player == null)
        {
            var p = FindAnyObjectByType<Player>();
            if (p != null)
                player = p.transform;
        }
        if (location != null)
            locationScaleX = Mathf.Abs(location.localScale.x);
    }

    private void LateUpdate()
    {
        if (map == null || player == null || worldBounds == null)
            return;

        Bounds b = worldBounds.bounds;
        if (b.size.x <= 0f || b.size.y <= 0f)
            return;

        // Vị trí Player trong vùng (0..1 mỗi trục), đổi sang tọa độ 0..1 trên cả ảnh Map qua mapArea.
        float u = Mathf.Lerp(mapArea.xMin, mapArea.xMax, Mathf.InverseLerp(b.min.x, b.max.x, player.position.x));
        float v = Mathf.Lerp(mapArea.yMin, mapArea.yMax, Mathf.InverseLerp(b.min.y, b.max.y, player.position.y));

        // Dời ảnh sao cho điểm (u, v) trên ảnh trùng với tâm của Location.
        Vector2 size = map.rect.size;
        Vector2 pointOnMap = new Vector2((u - map.pivot.x) * size.x, (v - map.pivot.y) * size.y);
        Vector2 target = location != null ? location.anchoredPosition : Vector2.zero;
        map.anchoredPosition = target - pointOnMap;

        if (location != null)
        {
            // Player quay mặt bằng cách xoay 180 độ quanh Y, nên transform.right.x âm là đang nhìn sang trái.
            var s = location.localScale;
            s.x = player.right.x < 0f ? -locationScaleX : locationScaleX;
            location.localScale = s;
        }
    }
}
