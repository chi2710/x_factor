using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// Tools > Menu UI > Build Equipment Tab: dựng layout tab Trang bị trong EquipmentTab theo ảnh thiết kế,
// dùng sprite trong Assets/Resources/UI/3. TRANG BỊ (bỏ qua ảnh nhân vật và ảnh vũ khí chưa có).
// Chạy lại sẽ xóa và dựng lại object "EquipView" (chỉ object này).
// Toạ độ theo canvas tham chiếu 3840x2160, tính từ tâm của EquipmentTab.
public static class EquipmentTabBuilder
{
    private const string SpriteFolder = "Assets/Resources/UI/3. TRANG BỊ";
    private const string RootName = "EquipView";

    private static readonly float[] GridRowsY = { 534, 301, 69 };
    private static readonly float[] DeviceColsX = { 670, 900, 1131, 1361 };
    private static readonly float[] EquippedColsX = { 772, 1004, 1238 };
    private const int DefaultUnlockedWeapons = 2;
    private const int DefaultUnlockedDevices = 3;

    [MenuItem("Tools/Menu UI/Build Equipment Tab")]
    public static void Build()
    {
        var tab = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include).FirstOrDefault(t => t.name == "EquipmentTab");
        if (tab == null)
        {
            EditorUtility.DisplayDialog("Menu UI", "Không tìm thấy EquipmentTab trong scene. Chạy Tools > Menu UI > Setup Scene trước.", "OK");
            return;
        }

        var old = tab.Find(RootName);
        if (old != null)
            Undo.DestroyObjectImmediate(old.gameObject);
        var placeholder = tab.Find("Placeholder");
        if (placeholder != null)
        {
            Undo.RecordObject(placeholder.gameObject, "Build Equipment Tab");
            placeholder.gameObject.SetActive(false);
        }

        // Font/màu lấy từ tab Vật phẩm để đồng bộ.
        var itemDetail = Object.FindAnyObjectByType<ItemDetailUI>(FindObjectsInactive.Include);
        var nameStyle = itemDetail != null ? itemDetail.nameText : null;
        var bodyStyle = itemDetail != null ? itemDetail.descriptionText : null;
        var headerStyle = Object.FindObjectsByType<TextMeshProUGUI>(FindObjectsInactive.Include)
            .FirstOrDefault(t => t.text.Contains("VẬT PHẨM N")) ?? nameStyle;

        var weapons = MenuUIDataImporter.AssetsInSheetOrder<Item>("TRANG BỊ", "Equipment").Where(i => i.location == "VŨ KHÍ").ToList();
        var devices = MenuUIDataImporter.AssetsInSheetOrder<Item>("TRANG BỊ", "Equipment").Where(i => i.location == "THIẾT BỊ").ToList();

        var root = NewRect(RootName, tab);
        Undo.RegisterCreatedObjectUndo(root.gameObject, "Build Equipment Tab");
        root.anchorMin = Vector2.zero; root.anchorMax = Vector2.one;
        root.offsetMin = root.offsetMax = Vector2.zero;
        root.SetAsFirstSibling();

        // ----- Vũ khí (trái) -----
        NewImage("WeaponHeader", root, "Asset 118", new Vector2(-960, 699), new Vector2(950, 82), false);
        NewText("WeaponHeaderText", root, headerStyle, "VŨ KHÍ", 36, TextAlignmentOptions.Left, new Vector2(-1163, 694), new Vector2(400, 70));

        // Theo mockup: 2 vũ khí đầu mở, vũ khí thứ 3 (dao) khóa nhưng vẫn hiện mờ và xem được thông tin.
        int unlockedWeapons = DefaultUnlockedWeapons;
        for (int i = 0; i < GridRowsY.Length; i++)
        {
            var slot = BuildSlot($"Weapon_{i}", root, EquipmentSlotUI.Kind.Weapon, new Vector2(-954, GridRowsY[i]), new Vector2(906, 219),
                "Asset 51", "Asset 117", "Asset 52", null, new Vector2(780, 180));
            slot.isLocked = i >= unlockedWeapons;
            slot.item = i < weapons.Count ? weapons[i] : null;
        }

        var weaponName = NewText("WeaponName", root, nameStyle, "", 48, TextAlignmentOptions.Left, new Vector2(-902, -150), new Vector2(1000, 80));
        weaponName.color = new Color32(0xFF, 0x6B, 0x2B, 0xFF); // cam như mockup
        var weaponDesc = NewText("WeaponDescription", root, bodyStyle, "", 30, TextAlignmentOptions.TopLeft, new Vector2(-952, -373), new Vector2(900, 344));
        weaponDesc.textWrappingMode = TextWrappingModes.Normal;
        weaponDesc.color = Color.white;
        var upgradeButton = NewImage("UpgradeButton", root, "Asset 50", new Vector2(-1194, -668), new Vector2(426, 135), false);

        // ----- Thiết bị (phải) -----
        NewImage("DeviceHeader", root, "Asset 118", new Vector2(1020, 699), new Vector2(918, 82), false);
        NewText("DeviceHeaderText", root, headerStyle, "THIẾT BỊ", 36, TextAlignmentOptions.Left, new Vector2(801, 694), new Vector2(400, 70));

        int unlockedDevices = Mathf.Max(DefaultUnlockedDevices, devices.Count);
        int index = 0;
        foreach (float y in GridRowsY)
            foreach (float x in DeviceColsX)
            {
                var slot = BuildSlot($"Device_{index}", root, EquipmentSlotUI.Kind.Device, new Vector2(x, y), new Vector2(217, 215),
                    "Asset 54", "Asset 65", "Asset 57", null, new Vector2(170, 170));
                slot.isLocked = index >= unlockedDevices;
                slot.item = index < devices.Count ? devices[index] : null;
                index++;
            }

        // ----- Đang trang bị -----
        NewText("EquippedHeader", root, headerStyle, "ĐANG TRANG BỊ", 34, TextAlignmentOptions.Center, new Vector2(1004, -152), new Vector2(700, 70));
        NewImage("EquippedDivider", root, "Asset 56", new Vector2(1010, -183), new Vector2(610, 2), false).preserveAspect = false;
        // Đường nối trang trí: tiêu đề Vũ khí -> nhân vật, lưới Thiết bị -> nhân vật.
        NewImage("WeaponLine", root, "Asset 249@2x", new Vector2(-282, 662), new Vector2(360, 62), false);
        NewImage("DeviceLine", root, "Asset 248@2x", new Vector2(453, -83), new Vector2(530, 49), false);
        for (int i = 0; i < EquippedColsX.Length; i++)
        {
            var slot = BuildSlot($"Equipped_{i}", root, EquipmentSlotUI.Kind.Equipped, new Vector2(EquippedColsX[i], -325), new Vector2(211, 211),
                "Asset 54", "Asset 256@2x", null, "Asset 55", new Vector2(165, 165));
            // Asset 256@2x có viền sáng bao quanh (khung thật ~216/277px) nên phóng ảnh to hơn ô để khung khớp 211.
            ((RectTransform)slot.selected.transform).sizeDelta = new Vector2(271, 271);
        }

        // ----- Khung thông tin thiết bị (đặt cuối để nằm trên các ô) -----
        var tooltip = NewImage("DeviceTooltip", root, "Asset 114", new Vector2(902, 666), new Vector2(371, 206), false);
        var tooltipText = NewText("Text", tooltip.transform, bodyStyle, "", 22, TextAlignmentOptions.TopLeft, new Vector2(0, -22), new Vector2(330, 130));
        tooltipText.textWrappingMode = TextWrappingModes.Normal;
        tooltipText.color = Color.white;
        tooltip.gameObject.SetActive(false);

        var tabUI = tab.GetComponent<EquipmentTabUI>();
        if (tabUI == null)
            tabUI = Undo.AddComponent<EquipmentTabUI>(tab.gameObject);
        Undo.RecordObject(tabUI, "Build Equipment Tab");
        tabUI.weaponNameText = weaponName;
        tabUI.weaponDescriptionText = weaponDesc;
        tabUI.tooltip = (RectTransform)tooltip.transform;
        tabUI.tooltipText = tooltipText;
        tabUI.upgradeButton = upgradeButton.gameObject;
        EditorUtility.SetDirty(tabUI);

        EditorSceneManager.MarkSceneDirty(tab.gameObject.scene);
        Selection.activeGameObject = root.gameObject;
        Debug.Log($"[Menu UI] Đã dựng tab Trang bị (EquipmentTab/EquipView): {weapons.Count} vũ khí, {devices.Count} thiết bị.");
    }

    // Ô gồm: nền trong suốt nhận click + các lớp Normal/Selected/Locked/Empty + Icon.
    private static EquipmentSlotUI BuildSlot(string name, Transform parent, EquipmentSlotUI.Kind kind, Vector2 pos, Vector2 size,
        string normal, string selected, string locked, string empty, Vector2 iconSize)
    {
        var rt = NewRect(name, parent);
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        var hit = rt.gameObject.AddComponent<Image>();
        hit.color = new Color(1, 1, 1, 0);

        var slot = rt.gameObject.AddComponent<EquipmentSlotUI>();
        slot.kind = kind;
        slot.normal = Layer("Normal", rt, normal, size);
        slot.selected = Layer("Selected", rt, selected, size);
        slot.locked = Layer("Locked", rt, locked, size);
        slot.empty = Layer("Empty", rt, empty, size);
        slot.icon = NewImage("Icon", rt, null, Vector2.zero, iconSize, false);
        return slot;
    }

    private static GameObject Layer(string name, Transform parent, string sprite, Vector2 size)
    {
        if (sprite == null)
            return null;
        var image = NewImage(name, parent, sprite, Vector2.zero, size, false);
        image.preserveAspect = false;
        return image.gameObject;
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
        if (sprite != null)
        {
            image.sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{SpriteFolder}/{sprite}.png");
            if (image.sprite == null)
                Debug.LogWarning($"[Menu UI] Không tìm thấy sprite {SpriteFolder}/{sprite}.png");
        }
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
