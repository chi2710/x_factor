using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Tools > Menu UI > Setup Scene: gắn và nối các script (TabMenuUI, TabButtonUI, ItemTabUI, ItemUI, MissionUI)
// vào hierarchy "UI" có sẵn trong scene, tạo panel tạm cho 3 tab còn lại, rồi nạp dữ liệu từ sheet (MenuUIDataImporter).
// Chạy lại nhiều lần được: chỉ thêm cái còn thiếu, không ghi đè dữ liệu đã gán.
public static class MenuUISetup
{
    private static readonly string[] TabNames = { "BẢN ĐỒ", "VẬT PHẨM", "TRANG BỊ", "KĨ NĂNG" };
    private static readonly string[] TabObjectNames = { "MapTab", "ItemTab", "EquipmentTab", "SkillTab" };

    [MenuItem("Tools/Menu UI/Setup Scene")]
    public static void Setup()
    {
        GameObject root = SceneManager.GetActiveScene().GetRootGameObjects().FirstOrDefault(g => g.name == "UI");
        if (root == null)
        {
            EditorUtility.DisplayDialog("Menu UI", "Không tìm thấy GameObject \"UI\" trong scene đang mở.", "OK");
            return;
        }

        Transform itemTab = Find(root.transform, "ItemTab");
        Transform buttonRow = Find(root.transform, "TabSelection")?.Find("ButtonTab");
        if (itemTab == null || buttonRow == null || buttonRow.childCount < 4)
        {
            EditorUtility.DisplayDialog("Menu UI", "Cần có UI/.../ItemTab và TabSelection/ButtonTab với 4 nút con.", "OK");
            return;
        }

        SetupItemTab(itemTab);

        var menu = GetOrAdd<TabMenuUI>(root);
        var tabs = new TabMenuUI.Tab[4];
        for (int i = 0; i < 4; i++)
        {
            Transform b = buttonRow.GetChild(i);
            GetOrAdd<Button>(b.gameObject);
            var tabButton = GetOrAdd<TabButtonUI>(b.gameObject);
            tabButton.onSelected = b.Find("OnSelected")?.gameObject;
            tabButton.label = b.Find("Name")?.GetComponent<TextMeshProUGUI>();
            if (tabButton.label != null)
            {
                Undo.RecordObject(tabButton.label, "Setup Menu UI");
                tabButton.label.text = TabNames[i];
            }
            EditorUtility.SetDirty(tabButton);

            GameObject content = i == 1 ? itemTab.gameObject : GetOrCreatePlaceholderTab(itemTab, i);
            tabs[i] = new TabMenuUI.Tab { button = tabButton, content = content };
        }
        Undo.RecordObject(menu, "Setup Menu UI");
        menu.tabs = tabs;
        menu.startTab = 1;
        EditorUtility.SetDirty(menu);

        MenuUIDataImporter.ImportAndAssign();
        EditorSceneManager.MarkSceneDirty(root.scene);
        Debug.Log("[Menu UI] Setup xong. Nhấn Play, dùng chuột/LB/RB (hoặc Q/E) để đổi tab.");
    }

    private static void SetupItemTab(Transform itemTab)
    {
        var tabUI = GetOrAdd<ItemTabUI>(itemTab.gameObject);
        tabUI.detail = itemTab.GetComponentInChildren<ItemDetailUI>(true);
        EditorUtility.SetDirty(tabUI);

        // Missions
        // Container có thể nằm thẳng dưới Mission hoặc trong Mission/Scroll View/Viewport.
        Transform missionPanel = Find(itemTab, "Mission");
        Transform container = missionPanel != null ? Find(missionPanel, "Container") : null;
        if (container != null)
        {
            foreach (Transform m in container)
            {
                var ui = GetOrAdd<MissionUI>(m.gameObject);
                Transform selected = m.Find("Selected");
                ui.selected = selected?.gameObject;
                ui.normal = m.Cast<Transform>().Where(c => c != selected).Select(c => c.gameObject).ToArray();
                ui.titleTexts = m.GetComponentsInChildren<TextMeshProUGUI>(true);
                EditorUtility.SetDirty(ui);
            }
        }

        // Item grids: mỗi ScrollRect trong ItemTab là một lưới.
        foreach (var scroll in itemTab.GetComponentsInChildren<ScrollRect>(true))
        {
            Transform panel = scroll.transform.parent;
            if (panel == missionPanel)
                continue; // Scroll View của danh sách nhiệm vụ, không phải lưới vật phẩm.
            string title = panel.GetComponentInChildren<TextMeshProUGUI>(true)?.text ?? "";
            bool isQuestItems = title.ToUpperInvariant().Contains("NHI"); // "VẬT PHẨM NHIỆM VỤ" vs "VẬT PHẨM NÂNG CẤP"
            int unlocked = isQuestItems ? 7 : 2;

            int index = 0;
            foreach (Transform slot in scroll.content)
            {
                bool isNew = slot.GetComponent<ItemUI>() == null;
                var ui = GetOrAdd<ItemUI>(slot.gameObject);
                ui.icon = slot.Find("Icon")?.GetComponent<Image>();
                ui.selected = slot.Find("Selected")?.gameObject;
                ui.locked = slot.Find("Locked")?.gameObject;
                ui.normal = slot.Find("BG")?.gameObject;
                if (isNew)
                    ui.isLocked = index >= unlocked;
                EditorUtility.SetDirty(ui);
                index++;
            }
        }
    }

    private static GameObject GetOrCreatePlaceholderTab(Transform itemTab, int index)
    {
        Transform parent = itemTab.parent;
        Transform existing = parent.Find(TabObjectNames[index]);
        if (existing != null)
            return existing.gameObject;

        var go = new GameObject(TabObjectNames[index], typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(go, "Setup Menu UI");
        go.transform.SetParent(parent, false);
        go.transform.SetSiblingIndex(itemTab.GetSiblingIndex() + 1);
        var rt = (RectTransform)go.transform;
        var src = (RectTransform)itemTab;
        rt.anchorMin = src.anchorMin; rt.anchorMax = src.anchorMax;
        rt.pivot = src.pivot; rt.sizeDelta = src.sizeDelta; rt.anchoredPosition = src.anchoredPosition;

        var textGO = new GameObject("Placeholder", typeof(RectTransform));
        textGO.transform.SetParent(go.transform, false);
        var trt = (RectTransform)textGO.transform;
        trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
        trt.offsetMin = trt.offsetMax = Vector2.zero;
        var text = textGO.AddComponent<TextMeshProUGUI>();
        var fontSource = itemTab.GetComponentInChildren<TextMeshProUGUI>(true);
        if (fontSource != null)
        {
            text.font = fontSource.font;
            text.color = fontSource.color;
        }
        text.text = TabNames[index];
        text.fontSize = 48;
        text.alignment = TextAlignmentOptions.Center;
        text.raycastTarget = false;

        go.SetActive(false);
        return go;
    }

    // ---------- Helpers ----------

    private static T GetOrAdd<T>(GameObject go) where T : Component
    {
        var c = go.GetComponent<T>();
        return c != null ? c : Undo.AddComponent<T>(go);
    }

    private static Transform Find(Transform parent, string name)
    {
        var queue = new Queue<Transform>();
        queue.Enqueue(parent);
        while (queue.Count > 0)
        {
            var t = queue.Dequeue();
            if (t.name == name)
                return t;
            foreach (Transform c in t)
                queue.Enqueue(c);
        }
        return null;
    }
}
