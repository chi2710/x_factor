using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Tab Bản đồ: các điểm đánh dấu nhiệm vụ trên bản đồ + bảng thông tin khu vực bên phải.
// Trỏ tới điểm nào (D-pad / chuột) thì hiện icon và tên nhiệm vụ của điểm đó.
public class MapTabUI : MonoBehaviour, ITabContent
{
    public string areaName = "KHU DÂN CƯ";
    [Tooltip("Tiến độ khám phá khu vực (0-1). Đặt < 0 để tự tính theo tỉ lệ nhiệm vụ đã hoàn thành (Mission.completed).")]
    public float progress = 0.6f;

    public TextMeshProUGUI areaNameText;
    public TextMeshProUGUI progressText;
    public Image missionIcon;
    public TextMeshProUGUI missionText;
    [Tooltip("Điểm được chọn sẵn khi mở tab lần đầu.")]
    public MapMarkerUI defaultMarker;

    private MapMarkerUI[] markers;
    private MapMarkerUI current;

    private void Awake()
    {
        markers = GetComponentsInChildren<MapMarkerUI>(true);
        foreach (var marker in markers)
            marker.Selected += OnMarkerSelected;
    }

    // Start: chạy sau Awake của các điểm (vốn reset highlight về false).
    private void Start()
    {
        if (areaNameText != null)
            areaNameText.text = areaName;
        RefreshProgress();
        if (current == null)
            Show(defaultMarker != null ? defaultMarker : markers.FirstOrDefault());
    }

    public void RefreshProgress()
    {
        float value = progress;
        if (value < 0f)
        {
            var missions = markers.Where(m => m.mission != null).Select(m => m.mission).Distinct().ToList();
            value = missions.Count == 0 ? 0f : (float)missions.Count(m => m.completed) / missions.Count;
        }
        if (progressText != null)
            progressText.text = Mathf.RoundToInt(Mathf.Clamp01(value) * 100f) + "%";
    }

    private void OnMarkerSelected(SlotUI slot) => Show((MapMarkerUI)slot);

    private void Show(MapMarkerUI marker)
    {
        if (current != null && current != marker)
            current.SetHighlighted(false);
        current = marker;
        if (marker == null)
        {
            SetMission(null, "");
            return;
        }
        marker.SetHighlighted(true);
        SetMission(marker.icon != null ? marker.icon.sprite : null, marker.mission != null ? marker.mission.title : "");
    }

    private void SetMission(Sprite sprite, string title)
    {
        if (missionIcon != null)
        {
            missionIcon.sprite = sprite;
            missionIcon.enabled = sprite != null;
        }
        if (missionText != null)
            missionText.text = title;
    }

    public GameObject GetFocusTarget()
    {
        if (markers == null)
            return null; // Chưa Awake.
        if (current != null)
            return current.gameObject;
        var first = defaultMarker != null ? defaultMarker : markers.FirstOrDefault();
        return first != null ? first.gameObject : null;
    }

    public bool HandleBack() => false;
}
