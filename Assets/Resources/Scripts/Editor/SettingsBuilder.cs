using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// Tools > Menu UI > Build Settings: dựng lại UI/Settings (màn Tùy chỉnh cài đặt) từ sprite trong UI/SETTING.
// Sprite vẽ cho 1920x1080 nên trên canvas 3840x2160 nhân x2. Toạ độ đo từ ảnh mẫu 2000px, quy đổi x1.92,
// tính từ góc trái trên; các hàm New* nhận tâm (x, y) với y dương hướng xuống.
// Tab Âm thanh có các dòng cài đặt, tab Điều khiển chỉ có ảnh tay cầm, các tab khác để trống.
public static class SettingsBuilder
{
    const string RootName = "Settings";
    const string SpriteFolder = "Assets/Resources/UI/SETTING/";
    const string FontFolder = "Assets/Resources/Fonts/";

    static readonly Color TabColor = new Color32(0x2E, 0xE6, 0xE6, 0xFF);
    static readonly Color TabSelectedColor = new Color32(0xD8, 0xFF, 0xFF, 0xFF);
    static readonly Color TitleColor = new Color32(0x30, 0xF0, 0xF0, 0xFF);
    static readonly Color LineColor = new Color32(0x2A, 0x8C, 0x8C, 0xC0);

    static readonly string[] TabNames = { "HIỂN THỊ", "ĐỒ HỌA", "ÂM THANH", "ĐIỀU KHIỂN", "NGÔN NGỮ" };
    static readonly string[] PageNames = { "Display", "Graphics", "Audio", "Controls", "Language" };
    static readonly float[] TabY = { 586, 762, 935, 1110, 1286 };
    static readonly float[] RowY = { 772, 897, 1018, 1142 };

    static TMP_FontAsset boldFont, regularFont;

    [MenuItem("Tools/Menu UI/Build Settings")]
    public static void Build()
    {
        var menu = Object.FindAnyObjectByType<TabMenuUI>(FindObjectsInactive.Include);
        if (menu == null)
        {
            EditorUtility.DisplayDialog("Settings", "Không tìm thấy object UI có TabMenuUI.", "OK");
            return;
        }
        boldFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontFolder + "CascadiaCode-Bold SDF.asset");
        regularFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontFolder + "CascadiaCode-Regular SDF.asset");

        var old = menu.transform.Find(RootName);
        if (old != null)
            Undo.DestroyObjectImmediate(old.gameObject);

        var root = Stretch(NewRect(RootName, menu.transform, Vector2.zero, Vector2.zero));
        Undo.RegisterCreatedObjectUndo(root.gameObject, "Build Settings");
        root.SetAsLastSibling(); // vẽ trên HUD và menu Tab

        var panel = Stretch(NewRect("Panel", root, Vector2.zero, Vector2.zero));
        Stretch(NewImage("Background", panel, "Asset 175", Vector2.zero, Vector2.zero).rectTransform);
        NewImage("LeftGlow", panel, "Asset 190", new Vector2(569, 1088), new Vector2(1138, 2176));
        NewImage("Header", panel, "Asset 176", new Vector2(1922, 242), new Vector2(3844, 484));
        NewImage("Bottom", panel, "Asset 180", new Vector2(1928, 1209), new Vector2(3824, 1902));
        NewImage("TabFrame", panel, "Asset 172", new Vector2(695, 1086), new Vector2(1140, 1372));

        var tabs = BuildTabs(panel);

        var pagesRoot = Stretch(NewRect("Pages", panel, Vector2.zero, Vector2.zero));
        var pages = PageNames.Select(n => Stretch(NewRect(n, pagesRoot, Vector2.zero, Vector2.zero)).gameObject).ToArray();
        var rows = BuildAudioPage(pages[2].transform);
        NewImage("Controller", pages[3].transform, "Asset 182", new Vector2(2433, 1060), new Vector2(1700, 1078));

        var apply = NewButton("ApplyButton", panel, "Asset 179", new Vector2(2784, 1924), new Vector2(390, 162));
        var reset = NewButton("DefaultButton", panel, "Asset 178", new Vector2(3314, 1924), new Vector2(620, 164));
        NewImage("ButtonHints", panel, "Asset 181", new Vector2(563, 1968), new Vector2(826, 68));

        var ui = Undo.AddComponent<SettingsUI>(root.gameObject);
        ui.panel = panel.gameObject;
        ui.tabs = tabs;
        ui.pages = pages;
        ui.startTab = 2;
        ui.masterRow = rows[0];
        ui.musicRow = rows[1];
        ui.sfxRow = rows[2];
        ui.ambientRow = rows[3];
        ui.applyButton = apply;
        ui.defaultButton = reset;

        // Trong Edit mode chỉ hiện tab Âm thanh để dễ chỉnh; lúc chạy SettingsUI tự ẩn panel.
        for (int i = 0; i < pages.Length; i++)
            pages[i].SetActive(i == ui.startTab);
        for (int i = 0; i < tabs.Length; i++)
            tabs[i].onSelected.SetActive(i == ui.startTab);
        EditorUtility.SetDirty(ui);

        Selection.activeGameObject = root.gameObject;
        EditorSceneManager.MarkSceneDirty(root.gameObject.scene);
    }

    static TabButtonUI[] BuildTabs(Transform panel)
    {
        var glow = boldFont != null ? TabGlowSetup.GetOrCreateGlowPreset(boldFont) : null;
        var container = Stretch(NewRect("Tabs", panel, Vector2.zero, Vector2.zero));
        var result = new TabButtonUI[TabNames.Length];
        for (int i = 0; i < TabNames.Length; i++)
        {
            var tab = NewRect("Tab_" + PageNames[i], container, new Vector2(637, TabY[i]), new Vector2(534, 122));
            // Image trong suốt để nhận click.
            var hit = tab.gameObject.AddComponent<Image>();
            hit.color = Color.clear;
            var button = tab.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.None;

            var selected = NewRect("Selected", tab, Vector2.zero, Vector2.zero);
            Stretch(selected);
            Stretch(NewImage("Frame", selected, "Asset 173", Vector2.zero, Vector2.zero).rectTransform);
            var arrow = NewImage("Arrow", selected, "Asset 174", Vector2.zero, new Vector2(29, 40));
            AnchorLeftMiddle(arrow.rectTransform, 48, 0);

            var label = NewText("Label", tab, boldFont, TabNames[i], 56, TextAlignmentOptions.Left, TabColor);
            label.characterSpacing = 12;
            AnchorLeftMiddle(label.rectTransform, 8, 0, new Vector2(520, 122), pivotX: 0);

            var tabUI = tab.gameObject.AddComponent<TabButtonUI>();
            tabUI.onSelected = selected.gameObject;
            tabUI.label = label;
            tabUI.normalColor = TabColor;
            tabUI.selectedColor = TabSelectedColor;
            tabUI.selectedMaterial = glow;
            tabUI.selectedLabelOffset = 92;
            result[i] = tabUI;
        }
        return result;
    }

    static SettingRowUI[] BuildAudioPage(Transform page)
    {
        var title = NewText("Title", page, boldFont, "TÙY CHỈNH ÂM THANH", 52, TextAlignmentOptions.Left, TitleColor);
        title.characterSpacing = 10;
        Place(title.rectTransform, new Vector2(2176, 589), new Vector2(1300, 90));
        NewImage("TitleArrows", page, "Asset 184", new Vector2(2231, 589), new Vector2(84, 32));
        var line = NewImage("Divider", page, null, new Vector2(2400, 668), new Vector2(1748, 3));
        line.color = LineColor;

        return new[]
        {
            BuildRow(page, "MasterVolume", "Âm lượng", SettingRowUI.Kind.Slider, RowY[0]),
            BuildRow(page, "Music", "Nhạc nền", SettingRowUI.Kind.Toggle, RowY[1]),
            BuildRow(page, "Sfx", "Hiệu ứng âm thanh", SettingRowUI.Kind.Toggle, RowY[2]),
            BuildRow(page, "AmbientVolume", "Âm lượng môi trường", SettingRowUI.Kind.Slider, RowY[3]),
        };
    }

    // Con của dòng neo theo mép trái giữa dòng: x tính từ mép trái dòng (canvas x 1500).
    static SettingRowUI BuildRow(Transform page, string name, string text, SettingRowUI.Kind kind, float y)
    {
        var row = NewRect("Row_" + name, page, new Vector2(2398, y), new Vector2(1796, 90));
        var hit = row.gameObject.AddComponent<Image>();
        hit.color = Color.clear;

        var highlight = NewImage("Highlight", row, "Asset 183", Vector2.zero, Vector2.zero);
        Stretch(highlight.rectTransform);

        var rowUI = row.gameObject.AddComponent<SettingRowUI>();
        rowUI.kind = kind;
        rowUI.highlight = highlight.gameObject;

        rowUI.label = NewText("Label", row, regularFont, text, 50, TextAlignmentOptions.Left, rowUI.textColor);
        rowUI.label.characterSpacing = 6;
        AnchorLeftMiddle(rowUI.label.rectTransform, 26, 0, new Vector2(1000, 90), pivotX: 0);

        if (kind == SettingRowUI.Kind.Toggle)
        {
            var normal = NewImage("Arrows", row, "Asset 188", Vector2.zero, new Vector2(784, 52));
            AnchorLeftMiddle(normal.rectTransform, 923 + 392, 0);
            var selected = NewImage("ArrowsSelected", row, "Asset 189", Vector2.zero, new Vector2(784, 54));
            AnchorLeftMiddle(selected.rectTransform, 923 + 392, 0);
            rowUI.arrowsNormal = normal.gameObject;
            rowUI.arrowsSelected = selected.gameObject;

            rowUI.valueText = NewText("Value", row, regularFont, "BẬT", 44, TextAlignmentOptions.Center, rowUI.textColor);
            AnchorLeftMiddle(rowUI.valueText.rectTransform, 923 + 392, 0, new Vector2(500, 90));
        }
        else
        {
            var track = NewImage("Track", row, null, Vector2.zero, new Vector2(780, 3));
            track.color = LineColor;
            AnchorLeftMiddle(track.rectTransform, 923, -4, pivotX: 0);
            var marker = NewImage("Marker", row, "Asset 185", Vector2.zero, new Vector2(144, 8));
            AnchorLeftMiddle(marker.rectTransform, 923 + 72, -8);
            rowUI.track = track.rectTransform;
            rowUI.marker = marker.rectTransform;

            rowUI.valueText = NewText("Value", row, regularFont, "60", 48, TextAlignmentOptions.Center, rowUI.textColor);
            AnchorLeftMiddle(rowUI.valueText.rectTransform, 923 + 72, 30, new Vector2(200, 70));
        }

        // Ẩn highlight ban đầu (SettingRowUI cũng làm vậy trong Awake).
        highlight.gameObject.SetActive(false);
        if (rowUI.arrowsSelected != null)
            rowUI.arrowsSelected.SetActive(false);
        return rowUI;
    }

    static Button NewButton(string name, Transform parent, string sprite, Vector2 center, Vector2 size)
    {
        var img = NewImage(name, parent, sprite, center, size);
        img.raycastTarget = true;
        var button = img.gameObject.AddComponent<Button>();
        button.targetGraphic = img;
        var colors = button.colors;
        colors.normalColor = new Color(0.72f, 0.72f, 0.72f);
        colors.highlightedColor = Color.white;
        colors.selectedColor = Color.white;
        colors.pressedColor = new Color(0.85f, 0.85f, 0.85f);
        button.colors = colors;
        return button;
    }

    // Neo góc trái trên của cha, pivot giữa; center tính từ góc trái trên (y dương hướng xuống).
    static RectTransform NewRect(string name, Transform parent, Vector2 center, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.layer = LayerMask.NameToLayer("UI");
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        Place(rt, center, size);
        return rt;
    }

    static void Place(RectTransform rt, Vector2 center, Vector2 size)
    {
        rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(center.x, -center.y);
        rt.sizeDelta = size;
    }

    static RectTransform Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        return rt;
    }

    // Neo mép trái giữa của cha (dùng cho con của tab/dòng). size giữ nguyên nếu không truyền.
    static void AnchorLeftMiddle(RectTransform rt, float x, float y, Vector2? size = null, float pivotX = 0.5f)
    {
        rt.anchorMin = rt.anchorMax = new Vector2(0, 0.5f);
        rt.pivot = new Vector2(pivotX, 0.5f);
        rt.anchoredPosition = new Vector2(x, y);
        if (size.HasValue)
            rt.sizeDelta = size.Value;
    }

    static Image NewImage(string name, Transform parent, string sprite, Vector2 center, Vector2 size)
    {
        var rt = NewRect(name, parent, center, size);
        var img = rt.gameObject.AddComponent<Image>();
        img.raycastTarget = false;
        if (sprite != null)
        {
            img.sprite = AssetDatabase.LoadAllAssetsAtPath(SpriteFolder + sprite + ".png").OfType<Sprite>().FirstOrDefault();
            if (img.sprite == null)
                Debug.LogWarning($"[SettingsBuilder] Không tìm thấy sprite {SpriteFolder}{sprite}.png");
        }
        return img;
    }

    static TextMeshProUGUI NewText(string name, Transform parent, TMP_FontAsset font, string text, float size,
        TextAlignmentOptions align, Color color)
    {
        var rt = NewRect(name, parent, Vector2.zero, new Vector2(400, 90));
        var tmp = rt.gameObject.AddComponent<TextMeshProUGUI>();
        if (font != null)
            tmp.font = font;
        tmp.text = text;
        tmp.fontSize = size;
        tmp.alignment = align;
        tmp.color = color;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
        tmp.raycastTarget = false;
        return tmp;
    }
}
