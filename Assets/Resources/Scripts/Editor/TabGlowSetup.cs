using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Tools > Menu UI > Setup Tab Glow: tạo material preset "<font> Glow" (bật Glow của TMP) cạnh font asset
// và gán vào TabButtonUI.selectedMaterial, MissionUI.selectedMaterial, để chữ của tab / tên nhiệm vụ đang chọn phát sáng.
// Preset đã có thì dùng lại, không ghi đè thông số đã chỉnh tay.
public static class TabGlowSetup
{
    static readonly Color GlowColor = new Color32(0x19, 0xFE, 0xFE, 0xB0);

    [MenuItem("Tools/Menu UI/Setup Tab Glow")]
    public static void Setup()
    {
        var buttons = Object.FindObjectsByType<TabButtonUI>(FindObjectsInactive.Include);
        int assigned = 0;
        foreach (var button in buttons)
        {
            if (button.label == null || button.label.font == null)
                continue;
            var glow = GetOrCreateGlowPreset(button.label.font);
            if (glow == null)
                continue;
            Undo.RecordObject(button, "Setup Tab Glow");
            button.selectedMaterial = glow;
            EditorUtility.SetDirty(button);
            assigned++;
        }

        // Tên nhiệm vụ cũng phát sáng khi được chọn: dùng preset Glow của font chữ tên.
        var missions = Object.FindObjectsByType<MissionUI>(FindObjectsInactive.Include);
        int missionAssigned = 0;
        foreach (var mission in missions)
        {
            var text = mission.titleTexts != null ? System.Array.Find(mission.titleTexts, t => t != null && t.font != null) : null;
            if (text == null)
                continue;
            var glow = GetOrCreateGlowPreset(text.font);
            if (glow == null)
                continue;
            Undo.RecordObject(mission, "Setup Tab Glow");
            mission.selectedMaterial = glow;
            EditorUtility.SetDirty(mission);
            missionAssigned++;
        }

        if (assigned > 0)
            EditorSceneManager.MarkSceneDirty(buttons[0].gameObject.scene);
        if (missionAssigned > 0)
            EditorSceneManager.MarkSceneDirty(missions[0].gameObject.scene);
        Debug.Log($"[TabGlowSetup] Đã gán Glow cho {assigned}/{buttons.Length} tab, {missionAssigned}/{missions.Length} nhiệm vụ.");
    }

    public static Material GetOrCreateGlowPreset(TMP_FontAsset font)
    {
        string fontPath = AssetDatabase.GetAssetPath(font);
        string path = Path.Combine(Path.GetDirectoryName(fontPath), font.name + " Glow.mat").Replace('\\', '/');
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat != null)
            return mat;

        // Copy từ material gốc của font để giữ atlas và thông số SDF; tên bắt đầu bằng tên font
        // để hiện trong dropdown Material Preset của TMP.
        mat = new Material(font.material) { name = font.name + " Glow" };
        // Shader Mobile không có Glow: chuyển sang Distance Field đầy đủ.
        if (!mat.HasProperty(ShaderUtilities.ID_GlowColor))
        {
            var shader = Shader.Find("TextMeshPro/Distance Field");
            if (shader == null)
            {
                Debug.LogWarning("[TabGlowSetup] Không tìm thấy shader TextMeshPro/Distance Field.");
                return null;
            }
            mat.shader = shader;
        }

        mat.EnableKeyword(ShaderUtilities.Keyword_Glow);
        mat.SetColor(ShaderUtilities.ID_GlowColor, GlowColor);
        mat.SetFloat(ShaderUtilities.ID_GlowOffset, 0f);
        mat.SetFloat(ShaderUtilities.ID_GlowInner, 0.1f);
        mat.SetFloat(ShaderUtilities.ID_GlowOuter, 0.35f);
        mat.SetFloat(ShaderUtilities.ID_GlowPower, 0.75f);

        AssetDatabase.CreateAsset(mat, path);
        return mat;
    }
}
