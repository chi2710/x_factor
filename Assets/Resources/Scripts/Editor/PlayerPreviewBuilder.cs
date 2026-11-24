using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

// Tools > Menu UI > Build Player Preview: ảnh idle của nhân vật ở giữa tab Trang bị, kèm VFX vfx_menu_Ring dưới chân.
// Canvas là Screen Space - Overlay nên particle không vẽ trực tiếp lên UI được:
// - "MenuVFXStage" (ngoài Canvas, ở xa map): camera riêng chỉ chụp layer MenuVFX vào RenderTexture.
// - "PlayerPreview" (trong EquipmentTab): RawImage hiện RenderTexture (shader UI/Additive), ảnh nhân vật đè lên trên.
// Chạy lại sẽ xóa và dựng lại hai object này (không đụng EquipView).
// Toạ độ theo canvas tham chiếu 3840x2160, tính từ tâm của EquipmentTab.
public static class PlayerPreviewBuilder
{
    private const string LayerName = "MenuVFX";
    private const string StageName = "MenuVFXStage";
    private const string RootName = "PlayerPreview";
    private const string VfxPrefabPath = "Assets/Phong Design/Prefabs/Prefabs_2/vfx_menu_Ring.prefab";
    private const string PlayerSpritePath = "Assets/Resources/Sprites/Player/Idle/IDLE - 00.png";
    private const string PlayerSpriteName = "IDLE - 00_0";
    private const string TexturePath = "Assets/Resources/UI/MenuVFX.renderTexture";
    private const string MaterialPath = "Assets/Resources/Shaders/UIAdditive.mat";

    // Sân khấu đặt xa map để camera game không bao giờ thấy (dù đã loại layer khỏi camera game).
    private static readonly Vector3 StagePosition = new Vector3(0, -1000, 0);
    // Camera VFX: nhìn thẳng như camera game; vòng nằm ở 1/4 dưới của khung hình.
    private const float CameraSize = 5f;
    private const int TextureSize = 1024;

    private static readonly Vector2 FeetPosition = new Vector2(0, -760);
    private const float PlayerHeight = 1300f;
    private const float VfxSize = 1800f;

    [MenuItem("Tools/Menu UI/Build Player Preview")]
    public static void Build()
    {
        var tab = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include).FirstOrDefault(t => t.name == "EquipmentTab");
        if (tab == null)
        {
            EditorUtility.DisplayDialog("Menu UI", "Không tìm thấy EquipmentTab trong scene.", "OK");
            return;
        }
        var vfxPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(VfxPrefabPath);
        var playerSprite = AssetDatabase.LoadAllAssetsAtPath(PlayerSpritePath).OfType<Sprite>().FirstOrDefault(s => s.name == PlayerSpriteName);
        if (vfxPrefab == null || playerSprite == null)
        {
            EditorUtility.DisplayDialog("Menu UI", $"Không tìm thấy {VfxPrefabPath} hoặc sprite {PlayerSpriteName}.", "OK");
            return;
        }
        int layer = EnsureLayer(LayerName);
        if (layer < 0)
        {
            EditorUtility.DisplayDialog("Menu UI", "Hết layer trống để tạo layer MenuVFX.", "OK");
            return;
        }
        var texture = GetOrCreateTexture();
        var material = GetOrCreateMaterial();
        if (material == null)
            return;

        // Xóa bản cũ.
        foreach (var old in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include)
                     .Where(t => (t.parent == null && t.name == StageName) || (t.parent == tab && t.name == RootName)).ToList())
            Undo.DestroyObjectImmediate(old.gameObject);

        // ----- Sân khấu VFX -----
        var stage = new GameObject(StageName);
        Undo.RegisterCreatedObjectUndo(stage, "Build Player Preview");
        stage.transform.position = StagePosition;
        stage.layer = layer;

        var camGo = new GameObject("MenuVFXCamera");
        camGo.layer = layer;
        camGo.transform.SetParent(stage.transform, false);
        camGo.transform.localPosition = new Vector3(0, CameraSize * 0.5f, -10);
        var cam = camGo.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = CameraSize;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0, 0, 0, 0);
        cam.cullingMask = 1 << layer;
        cam.targetTexture = texture;
        cam.allowHDR = false;
        cam.allowMSAA = false;
        var camData = camGo.AddComponent<UniversalAdditionalCameraData>();
        camData.renderPostProcessing = false;
        camData.renderShadows = false;

        var vfx = (GameObject)PrefabUtility.InstantiatePrefab(vfxPrefab, stage.transform);
        vfx.transform.localPosition = Vector3.zero;
        foreach (var t in vfx.GetComponentsInChildren<Transform>(true))
            t.gameObject.layer = layer;

        // Tắt sẵn; MenuVFXView bật khi tab Trang bị hiện.
        stage.SetActive(false);

        // Camera game không chụp layer VFX.
        foreach (var other in Object.FindObjectsByType<Camera>(FindObjectsInactive.Include).Where(c => c != cam))
        {
            Undo.RecordObject(other, "Build Player Preview");
            other.cullingMask &= ~(1 << layer);
        }

        // ----- UI -----
        var root = NewRect(RootName, tab);
        Undo.RegisterCreatedObjectUndo(root.gameObject, "Build Player Preview");
        root.anchorMin = Vector2.zero;
        root.anchorMax = Vector2.one;
        root.offsetMin = root.offsetMax = Vector2.zero;
        // Ngay sau EquipView: vẽ trên nền của tab.
        var equipView = tab.Find("EquipView");
        root.SetSiblingIndex(equipView != null ? equipView.GetSiblingIndex() + 1 : 0);
        root.gameObject.AddComponent<MenuVFXView>().stage = stage;

        // Vòng (gốc của prefab) nằm ở 1/4 dưới RawImage, trùng với chân nhân vật.
        var vfxRect = NewRect("VFX", root);
        vfxRect.anchorMin = vfxRect.anchorMax = new Vector2(0.5f, 0.5f);
        vfxRect.sizeDelta = new Vector2(VfxSize, VfxSize);
        vfxRect.anchoredPosition = FeetPosition + new Vector2(0, VfxSize * 0.25f);
        var raw = vfxRect.gameObject.AddComponent<RawImage>();
        raw.texture = texture;
        raw.material = material;
        raw.raycastTarget = false;

        var playerRect = NewRect("Player", root);
        playerRect.anchorMin = playerRect.anchorMax = new Vector2(0.5f, 0.5f);
        playerRect.pivot = new Vector2(0.5f, 0f);
        playerRect.anchoredPosition = FeetPosition;
        var spriteSize = playerSprite.rect.size;
        playerRect.sizeDelta = new Vector2(spriteSize.x * PlayerHeight / spriteSize.y, PlayerHeight);
        var image = playerRect.gameObject.AddComponent<Image>();
        image.sprite = playerSprite;
        image.preserveAspect = true;
        image.raycastTarget = false;

        EditorSceneManager.MarkSceneDirty(tab.gameObject.scene);
        Selection.activeGameObject = root.gameObject;
        Debug.Log($"[PlayerPreviewBuilder] Đã dựng {RootName} và {StageName} (layer {LayerName} = {layer}).");
    }

    // Trả về chỉ số layer; tạo ở ô trống đầu tiên (từ 8) nếu chưa có.
    private static int EnsureLayer(string name)
    {
        int existing = LayerMask.NameToLayer(name);
        if (existing >= 0)
            return existing;
        var tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        var layers = tagManager.FindProperty("layers");
        for (int i = 8; i < layers.arraySize; i++)
        {
            var element = layers.GetArrayElementAtIndex(i);
            if (!string.IsNullOrEmpty(element.stringValue))
                continue;
            element.stringValue = name;
            tagManager.ApplyModifiedProperties();
            return i;
        }
        return -1;
    }

    private static RenderTexture GetOrCreateTexture()
    {
        var texture = AssetDatabase.LoadAssetAtPath<RenderTexture>(TexturePath);
        if (texture != null)
            return texture;
        // Render Graph bắt buộc RenderTexture đích của camera có depth buffer (Renderer2D cần cả stencil): 24 = D24_UNorm_S8_UInt.
        texture = new RenderTexture(TextureSize, TextureSize, 24, RenderTextureFormat.ARGB32) { name = "MenuVFX" };
        AssetDatabase.CreateAsset(texture, TexturePath);
        return texture;
    }

    private static Material GetOrCreateMaterial()
    {
        var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        if (material != null)
            return material;
        var shader = Shader.Find("UI/Additive");
        if (shader == null)
        {
            EditorUtility.DisplayDialog("Menu UI", "Không tìm thấy shader UI/Additive.", "OK");
            return null;
        }
        material = new Material(shader) { name = "UIAdditive" };
        AssetDatabase.CreateAsset(material, MaterialPath);
        return material;
    }

    private static RectTransform NewRect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.layer = parent.gameObject.layer;
        go.transform.SetParent(parent, false);
        return (RectTransform)go.transform;
    }
}
