using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;

// Tools > Fonts > Rebuild Font Atlases: dựng lại atlas của các font SDF tĩnh với padding ~10% cỡ lấy mẫu.
// Padding cũ (2px trên cỡ ~90pt) quá nhỏ: khi chữ hiển thị nhỏ (bản build ở 1080p/1440p), viền SDF hẹp hơn 1 pixel
// nên chữ bị răng cưa. Giữ nguyên bộ ký tự, cỡ lấy mẫu và GUID asset, nên mọi text đang dùng font không phải gán lại.
public static class FontAtlasRebuilder
{
    static readonly string[] FontPaths =
    {
        "Assets/Resources/Fonts/CascadiaCode-Regular SDF.asset",
        "Assets/Resources/Fonts/CascadiaCode-SemiBold SDF.asset",
        "Assets/Resources/Fonts/CascadiaCode-Bold SDF.asset",
        "Assets/Resources/Fonts/Ethnocentric-Regular SDF.asset",
    };
    const float PaddingRatio = 0.1f;
    const int AtlasSize = 2048; // 1024 không đủ chỗ cho ~237 ký tự khi padding lớn hơn

    [MenuItem("Tools/Fonts/Rebuild Font Atlases")]
    public static void RebuildAll()
    {
        ShaderUtilities.GetShaderPropertyIDs(); // các ID_* chỉ có giá trị sau lần gọi này
        foreach (var path in FontPaths)
        {
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
            if (font == null)
            {
                Debug.LogWarning($"[FontAtlasRebuilder] Không tìm thấy {path}");
                continue;
            }
            Rebuild(font);
        }
        AssetDatabase.SaveAssets();
    }

    static void Rebuild(TMP_FontAsset font)
    {
        uint[] unicodes = font.characterTable.Select(c => c.unicode).ToArray();
        int padding = Mathf.Max(4, Mathf.RoundToInt(font.faceInfo.pointSize * PaddingRatio));
        var oldAtlas = font.atlasTexture;

        var so = new SerializedObject(font);
        so.FindProperty("m_AtlasPadding").intValue = padding;
        so.FindProperty("m_AtlasWidth").intValue = AtlasSize;
        so.FindProperty("m_AtlasHeight").intValue = AtlasSize;
        so.ApplyModifiedPropertiesWithoutUndo();

        // Material chính và các preset (vd. Glow) dùng chung atlas: cập nhật kích thước atlas và độ rộng gradient SDF.
        var materials = AssetDatabase.FindAssets("t:Material")
            .Select(g => AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(g)))
            .Where(m => m != null && m.HasProperty(ShaderUtilities.ID_MainTex) && m.GetTexture(ShaderUtilities.ID_MainTex) == oldAtlas)
            .Append(font.material)
            .Distinct()
            .ToList();
        foreach (var mat in materials)
        {
            mat.SetFloat(ShaderUtilities.ID_TextureWidth, AtlasSize);
            mat.SetFloat(ShaderUtilities.ID_TextureHeight, AtlasSize);
            if (mat.HasProperty(ShaderUtilities.ID_GradientScale))
                mat.SetFloat(ShaderUtilities.ID_GradientScale, padding + 1);
            EditorUtility.SetDirty(mat);
        }

        // Thêm ký tự chỉ chạy được ở chế độ Dynamic (TMP tự nạp file font gốc trong Editor), xong trả về Static.
        font.atlasPopulationMode = AtlasPopulationMode.Dynamic;
        font.ClearFontAssetData(true);
        font.TryAddCharacters(unicodes, out uint[] missing);
        font.atlasPopulationMode = AtlasPopulationMode.Static;

        EditorUtility.SetDirty(font);
        if (font.atlasTexture != null)
            EditorUtility.SetDirty(font.atlasTexture);

        if (missing != null && missing.Length > 0)
            Debug.LogWarning($"[FontAtlasRebuilder] {font.name}: {missing.Length} ký tự không vào được atlas: {new string(missing.Select(u => (char)u).ToArray())}", font);
        Debug.Log($"[FontAtlasRebuilder] {font.name}: padding {padding}, atlas {AtlasSize}x{AtlasSize}, {font.characterTable.Count}/{unicodes.Length} ký tự, {materials.Count} material.", font);
    }
}
