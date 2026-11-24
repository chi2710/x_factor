using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// Tools > Menu UI > Build Map Tab: dựng layout tab Bản đồ trong MapTab theo ảnh thiết kế,
// dùng sprite trong Assets/Resources/UI/1.BẢN ĐỒ. Chạy lại sẽ xóa và dựng lại object "MapView" (chỉ object này).
// Toạ độ theo canvas tham chiếu 3840x2160, tính từ tâm của MapTab.
public static class MapTabBuilder
{
    private const string SpriteFolder = "Assets/Resources/UI/1.BẢN ĐỒ";
    private const string RootName = "MapView";

    private struct MarkerDef
    {
        public string name, sprite, missionTitle;
        public Vector2 pos, size;
        public bool isDefault;
    }

    private static readonly MarkerDef[] Markers =
    {
        new MarkerDef { name = "Marker_KhuPheThai", sprite = "Asset 95", missionTitle = "KHÁM PHÁ KHU PHẾ THẢI", pos = new Vector2(307, 434), size = new Vector2(140, 72) },
        new MarkerDef { name = "Marker_TaiChe", sprite = "Asset 96", missionTitle = "TÁI CHẾ VẬT LIỆU CŨ", pos = new Vector2(-835, 236), size = new Vector2(90, 81) },
        new MarkerDef { name = "Marker_KeDich", sprite = "Asset 94", missionTitle = "ĐÁNH BẠI LIÊN TIẾP 15 KẺ ĐỊCH", pos = new Vector2(-35, 6), size = new Vector2(103, 104), isDefault = true },
        new MarkerDef { name = "Marker_KhoiBui", sprite = "Asset 97", missionTitle = "GIẢM KHÓI BỤI", pos = new Vector2(-1060, -532), size = new Vector2(96, 77) },
        // Điểm "!" chưa gắn nhiệm vụ (mockup không ghi rõ), gán Mission trong Inspector nếu cần.
        new MarkerDef { name = "Marker_Alert1", sprite = "Asset 151", pos = new Vector2(-461, 741), size = new Vector2(90, 90) },
        new MarkerDef { name = "Marker_Alert2", sprite = "Asset 151", pos = new Vector2(-897, -219), size = new Vector2(90, 90) },
        new MarkerDef { name = "Marker_Alert3", sprite = "Asset 151", pos = new Vector2(-27, -363), size = new Vector2(90, 90) },
    };

    [MenuItem("Tools/Menu UI/Build Map Tab")]
    public static void Build()
    {
        var tab = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include).FirstOrDefault(t => t.name == "MapTab");
        if (tab == null)
        {
            EditorUtility.DisplayDialog("Menu UI", "Không tìm thấy MapTab trong scene. Chạy Tools > Menu UI > Setup Scene trước.", "OK");
            return;
        }

        var old = tab.Find(RootName);
        if (old != null)
            Undo.DestroyObjectImmediate(old.gameObject);
        var placeholder = tab.Find("Placeholder");
        if (placeholder != null)
        {
            Undo.RecordObject(placeholder.gameObject, "Build Map Tab");
            placeholder.gameObject.SetActive(false);
        }

        // Font/màu lấy từ ô Thông tin vật phẩm để đồng bộ với tab Vật phẩm.
        var itemDetail = Object.FindAnyObjectByType<ItemDetailUI>(FindObjectsInactive.Include);
        var titleStyle = itemDetail != null ? itemDetail.nameText : null;
        var labelStyle = itemDetail != null ? itemDetail.locationText : null;

        var root = NewRect(RootName, tab);
        Undo.RegisterCreatedObjectUndo(root.gameObject, "Build Map Tab");
        root.anchorMin = Vector2.zero; root.anchorMax = Vector2.one;
        root.offsetMin = root.offsetMax = Vector2.zero;
        root.SetAsFirstSibling();

        NewImage("Grid", root, "Asset 93", new Vector2(0, 50), new Vector2(3341, 1642), false);
        NewImage("Map", root, "Asset 48", new Vector2(-303, 98), new Vector2(2621, 1411), false);

        var markersRoot = NewRect("Markers", root);
        markersRoot.anchorMin = markersRoot.anchorMax = new Vector2(0.5f, 0.5f);
        markersRoot.sizeDelta = Vector2.zero;
        MapMarkerUI defaultMarker = null;
        foreach (var def in Markers)
        {
            var marker = BuildMarker(markersRoot, def);
            if (def.isDefault)
                defaultMarker = marker;
        }

        // Bảng thông tin khu vực bên phải.
        var info = NewRect("Info", root);
        info.anchorMin = info.anchorMax = new Vector2(0.5f, 0.5f);
        info.sizeDelta = Vector2.zero;
        var areaName = NewText("AreaName", info, titleStyle, "KHU DÂN CƯ", 80, TextAlignmentOptions.Center, new Vector2(1171, 726), new Vector2(900, 120));
        // Asset 91 đã có sẵn chữ "60%" nên ProgressText bị ẩn để không hiện trùng.
        NewImage("ProgressIcon", info, "Asset 91", new Vector2(1123, 622), new Vector2(199, 74), false);
        var progress = NewText("ProgressText", info, titleStyle, "60%", 44, TextAlignmentOptions.Left, new Vector2(1210, 622), new Vector2(200, 80));
        progress.gameObject.SetActive(false);
        var missionIcon = NewImage("MissionIcon", info, "Asset 94", new Vector2(839, 539), new Vector2(77, 77), false);
        var missionText = NewText("MissionText", info, labelStyle, "ĐÁNH BẠI LIÊN TIẾP 15 KẺ ĐỊCH", 36, TextAlignmentOptions.Left, new Vector2(1290, 539), new Vector2(780, 80));
        missionText.color = Color.white;

        // Thanh gợi ý nút góc phải dưới (Điều hướng / Y Dịch chuyển / B Quay lại), sprite 1920 nên ×2.
        NewImage("ButtonHints", root, "Asset 152", new Vector2(1219, -839), new Vector2(918, 68), false);

        var tabUI = tab.GetComponent<MapTabUI>();
        if (tabUI == null)
            tabUI = Undo.AddComponent<MapTabUI>(tab.gameObject);
        Undo.RecordObject(tabUI, "Build Map Tab");
        tabUI.areaNameText = areaName;
        tabUI.progressText = progress;
        tabUI.missionIcon = missionIcon;
        tabUI.missionText = missionText;
        tabUI.defaultMarker = defaultMarker;
        EditorUtility.SetDirty(tabUI);

        EditorSceneManager.MarkSceneDirty(tab.gameObject.scene);
        Selection.activeGameObject = root.gameObject;
        Debug.Log("[Menu UI] Đã dựng tab Bản đồ (MapTab/MapView).");
    }

    private static MapMarkerUI BuildMarker(Transform parent, MarkerDef def)
    {
        var image = NewImage(def.name, parent, def.sprite, def.pos, def.size, true);

        // Khung chọn đỏ (Asset 89) bao quanh điểm, ẩn cho tới khi được trỏ tới.
        float side = Mathf.Max(def.size.x, def.size.y) * 1.3f;
        var selected = NewImage("Selected", image.transform, "Asset 89", Vector2.zero, new Vector2(side, side), false);
        selected.preserveAspect = false;
        selected.gameObject.SetActive(false);

        var marker = image.gameObject.AddComponent<MapMarkerUI>();
        marker.icon = image;
        marker.selected = selected.gameObject;
        if (def.missionTitle == null)
            return marker;
        marker.mission = FindMission(def.missionTitle);
        if (marker.mission == null)
            Debug.LogWarning($"[Menu UI] Không tìm thấy Mission \"{def.missionTitle}\" trong Assets/Resources/Data/Missions (chạy Import Data From CSV).");
        return marker;
    }

    private static Mission FindMission(string title)
    {
        return AssetDatabase.FindAssets("t:Mission", new[] { "Assets/Resources/Data/Missions" })
            .Select(g => AssetDatabase.LoadAssetAtPath<Mission>(AssetDatabase.GUIDToAssetPath(g)))
            .FirstOrDefault(m => m != null && m.title == title);
    }

    // ---------- Helpers ----------

    private static RectTransform NewRect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.layer = parent.gameObject.layer;
        go.transform.SetParent(parent, false);
        return (RectTransform)go.transform;
    }

    private static Image NewImage(string name, Transform parent, string sprite, Vector2 pos, Vector2 size, bool raycast)
    {
        var rt = NewRect(name, parent);
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        var image = rt.gameObject.AddComponent<Image>();
        image.sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{SpriteFolder}/{sprite}.png");
        if (image.sprite == null)
            Debug.LogWarning($"[Menu UI] Không tìm thấy sprite {SpriteFolder}/{sprite}.png");
        image.preserveAspect = true;
        image.raycastTarget = raycast;
        return image;
    }

    private static TextMeshProUGUI NewText(string name, Transform parent, TextMeshProUGUI style, string text, float size,
        TextAlignmentOptions align, Vector2 pos, Vector2 box)
    {
        var rt = NewRect(name, parent);
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = box;
        var tmp = rt.gameObject.AddComponent<TextMeshProUGUI>();
        if (style != null)
        {
            tmp.font = style.font;
            tmp.fontSharedMaterial = style.fontSharedMaterial;
            tmp.color = style.color;
            tmp.fontStyle = style.fontStyle;
            tmp.characterSpacing = style.characterSpacing;
        }
        tmp.text = text;
        tmp.fontSize = size;
        tmp.alignment = align;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
        tmp.raycastTarget = false;
        return tmp;
    }
}
