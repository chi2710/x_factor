using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Tools > Menu UI > Setup Skill Tab: gắn SkillTabUI lên SkillTab, SkillDetailUI lên SkillDetail,
// và nếu chưa ô nào có kĩ năng thì xếp các kĩ năng (theo thứ tự trong sheet) vào Kho kĩ năng.
// Chỉ đụng tới SkillTab, không ảnh hưởng các tab khác.
public static class SkillTabSetup
{
    [MenuItem("Tools/Menu UI/Setup Skill Tab")]
    public static void Setup()
    {
        var tab = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include)
            .FirstOrDefault(t => t.name == "SkillTab" && t.Find("Using") != null && t.Find("Storage") != null);
        if (tab == null)
        {
            EditorUtility.DisplayDialog("Menu UI", "Không tìm thấy SkillTab có con \"Using\" và \"Storage\".", "OK");
            return;
        }

        var tabUI = GetOrAdd<SkillTabUI>(tab.gameObject);
        tabUI.usingPanel = tab.Find("Using");
        tabUI.storagePanel = tab.Find("Storage");

        Transform detailGO = tab.Find("SkillDetail");
        if (detailGO != null)
            tabUI.detail = GetOrAdd<SkillDetailUI>(detailGO.gameObject);
        EditorUtility.SetDirty(tabUI);

        var usingSlots = tabUI.usingPanel.GetComponentsInChildren<SkillUI>(true);
        var storageSlots = tabUI.storagePanel.GetComponentsInChildren<SkillUI>(true);
        int assigned = 0;
        if (usingSlots.Concat(storageSlots).All(s => s.skill == null))
        {
            var skills = MenuUIDataImporter.SkillsInSheetOrder();
            for (int i = 0; i < storageSlots.Length && i < skills.Count; i++)
            {
                Undo.RecordObject(storageSlots[i], "Setup Skill Tab");
                storageSlots[i].skill = skills[i];
                EditorUtility.SetDirty(storageSlots[i]);
                assigned++;
            }
        }

        EditorSceneManager.MarkSceneDirty(tab.gameObject.scene);
        Debug.Log($"[Menu UI] Setup Skill Tab xong. {(assigned > 0 ? $"Đã đưa {assigned} kĩ năng vào kho." : "Giữ nguyên kĩ năng đã gán.")}");
    }

    private static T GetOrAdd<T>(GameObject go) where T : Component
    {
        var c = go.GetComponent<T>();
        return c != null ? c : Undo.AddComponent<T>(go);
    }
}
