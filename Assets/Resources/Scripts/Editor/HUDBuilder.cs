using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// Tools > Menu UI > Build HUD: dựng lại HUD/StatusBar ở góc trái trên từ sprite trong UI/HUD.
// Toạ độ theo canvas 3840x2160, đo từ ảnh mẫu (khung, thanh HP, thanh Mana, 3 ô kĩ năng).
// Ảnh khung (Asset 165) đã đặt tay trực tiếp dưới HUD sẽ bị ẩn để không bị trùng.
// Kèm cụm coin + EXP ở góc phải trên (object "Currency"), dựng riêng được bằng Tools > Menu UI > Build HUD Currency.
public static class HUDBuilder
{
    const string RootName = "StatusBar";
    const string SpriteFolder = "Assets/Resources/UI/HUD/";
    const string ManaMaterialPath = "Assets/Resources/Shaders/ManaBar.mat";
    const string HPMaterialPath = "Assets/Resources/Shaders/HPBar.mat";
    static readonly Color ManaColor = new Color32(0x19, 0xFE, 0xFE, 0xFF);
    static readonly Color HPColor = new Color32(0xEF, 0x3F, 0x50, 0xFF); // màu gốc của sprite "hp full"

    const string CurrencyName = "Currency";
    const string FontPath = "Assets/Resources/Fonts/Ethnocentric-Regular SDF.asset";
    const float CurrencyFontSize = 44f;
    static readonly Color CurrencyTextColor = new Color32(0x5C, 0xE1, 0xE6, 0xFF); // xanh ngọc như số trong mockup

    [MenuItem("Tools/Menu UI/Build HUD")]
    public static void Build()
    {
        var hud = Object.FindObjectsByType<RectTransform>(FindObjectsInactive.Include).FirstOrDefault(t => t.name == "HUD");
        if (hud == null)
        {
            EditorUtility.DisplayDialog("HUD", "Không tìm thấy object \"HUD\" trong canvas.", "OK");
            return;
        }

        // HUD phủ toàn canvas để neo theo góc màn hình.
        Undo.RecordObject(hud, "Build HUD");
        hud.anchorMin = Vector2.zero; hud.anchorMax = Vector2.one;
        hud.offsetMin = hud.offsetMax = Vector2.zero;
        hud.pivot = new Vector2(0.5f, 0.5f);

        var old = hud.Find(RootName);
        if (old != null)
            Undo.DestroyObjectImmediate(old.gameObject);
        foreach (Transform child in hud)
        {
            var img = child.GetComponent<Image>();
            if (img != null && img.sprite != null && img.sprite.name.StartsWith("Asset 165"))
            {
                Undo.RecordObject(child.gameObject, "Build HUD");
                child.gameObject.SetActive(false);
            }
        }

        var root = NewRect(RootName, hud, Vector2.zero, Vector2.zero);
        Undo.RegisterCreatedObjectUndo(root.gameObject, "Build HUD");

        NewImage("Frame", root, "Asset 165", new Vector2(80, -50), new Vector2(809, 219));

        NewImage("HPBack", root, "hp", new Vector2(307, -141), new Vector2(511, 32));
        var hpFill = NewImage("HPFill", root, "hp full", new Vector2(312, -144), new Vector2(501, 25));
        ApplyHPShader(hpFill);
        var hpGlow = NewImage("HPGlow", root, "Asset 170", new Vector2(813, -144), new Vector2(110, 25));
        hpGlow.rectTransform.pivot = new Vector2(1, 1);

        NewImage("ManaBack", root, "manna", new Vector2(350, -184), new Vector2(507, 23));
        var manaFill = NewImage("ManaFill", root, "hp full", new Vector2(355, -187), new Vector2(497, 17));
        ApplyManaShader(manaFill);
        var manaGlow = NewImage("ManaGlow", root, "Asset 169", new Vector2(852, -188), new Vector2(120, 14));
        manaGlow.rectTransform.pivot = new Vector2(1, 1);

        var skills = NewRect("Skills", root, new Vector2(344, -236), new Vector2(370, 110));
        var icons = new[]
        {
            NewImage("Skill1", skills, "Asset 164", new Vector2(0, 0), new Vector2(109, 109)),
            NewImage("Skill2", skills, "Asset 163", new Vector2(131, 0), new Vector2(111, 110)),
            NewImage("Skill3", skills, "Asset 162", new Vector2(261, 0), new Vector2(112, 111)),
        };

        var hudUI = hud.GetComponent<HUDUI>();
        if (hudUI == null)
            hudUI = Undo.AddComponent<HUDUI>(hud.gameObject);
        Undo.RecordObject(hudUI, "Build HUD");
        hudUI.hpFill = hpFill;
        hudUI.hpGlow = hpGlow.rectTransform;
        hudUI.manaFill = manaFill;
        hudUI.manaGlow = manaGlow.rectTransform;
        hudUI.skillIcons = icons;
        EditorUtility.SetDirty(hudUI);

        BuildCurrency(hud);

        Selection.activeGameObject = root.gameObject;
        EditorSceneManager.MarkSceneDirty(hud.gameObject.scene);
    }

    // Tools > Menu UI > Build HUD Currency: chỉ dựng lại cụm coin + EXP ở góc phải trên, không đụng StatusBar.
    [MenuItem("Tools/Menu UI/Build HUD Currency")]
    public static void BuildCurrencyMenu()
    {
        var hud = Object.FindObjectsByType<RectTransform>(FindObjectsInactive.Include).FirstOrDefault(t => t.name == "HUD");
        if (hud == null)
        {
            EditorUtility.DisplayDialog("HUD", "Không tìm thấy object \"HUD\" trong canvas.", "OK");
            return;
        }
        BuildCurrency(hud);
        EditorSceneManager.MarkSceneDirty(hud.gameObject.scene);
    }

    // Cụm coin + EXP theo mockup: neo góc phải trên, toạ độ là tâm icon / mép trái chữ tính từ góc phải trên (canvas 3840x2160).
    static void BuildCurrency(RectTransform hud)
    {
        var old = hud.Find(CurrencyName);
        if (old != null)
            Undo.DestroyObjectImmediate(old.gameObject);

        var root = NewRect(CurrencyName, hud, Vector2.zero, Vector2.zero);
        Undo.RegisterCreatedObjectUndo(root.gameObject, "Build HUD Currency");

        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        if (font == null)
            Debug.LogWarning($"[HUDBuilder] Không tìm thấy font {FontPath}");

        NewTopRightImage("CoinIcon", root, "coin", new Vector2(-778, -119), new Vector2(170, 156));
        var coinText = NewTopRightText("CoinText", root, font, new Vector2(-687, -123));
        NewTopRightImage("ExpIcon", root, "exp", new Vector2(-403, -119), new Vector2(159, 150));
        var expText = NewTopRightText("ExpText", root, font, new Vector2(-317, -123));

        var hudUI = hud.GetComponent<HUDUI>();
        if (hudUI == null)
            hudUI = Undo.AddComponent<HUDUI>(hud.gameObject);
        Undo.RecordObject(hudUI, "Build HUD Currency");
        hudUI.coinText = coinText;
        hudUI.expText = expText;
        EditorUtility.SetDirty(hudUI);
    }

    static Image NewTopRightImage(string name, Transform parent, string sprite, Vector2 center, Vector2 size)
    {
        var img = NewImage(name, parent, sprite, Vector2.zero, size);
        var rt = img.rectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(1, 1);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = center;
        img.preserveAspect = true;
        return img;
    }

    // Chữ căn trái, neo góc phải trên; pos là mép trái giữa dòng.
    static TextMeshProUGUI NewTopRightText(string name, Transform parent, TMP_FontAsset font, Vector2 leftMiddle)
    {
        var rt = NewRect(name, parent, Vector2.zero, new Vector2(300, 80));
        rt.anchorMin = rt.anchorMax = new Vector2(1, 1);
        rt.pivot = new Vector2(0, 0.5f);
        rt.anchoredPosition = leftMiddle;
        var tmp = rt.gameObject.AddComponent<TextMeshProUGUI>();
        if (font != null)
            tmp.font = font;
        tmp.fontSize = CurrencyFontSize;
        tmp.color = CurrencyTextColor;
        tmp.alignment = TextAlignmentOptions.MidlineLeft;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
        tmp.raycastTarget = false;
        tmp.text = "0";
        return tmp;
    }

    // Neo góc trái trên, pivot trái trên: pos là khoảng cách từ góc trái trên của cha.
    static RectTransform NewRect(string name, Transform parent, Vector2 pos, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.layer = LayerMask.NameToLayer("UI");
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        if (size == Vector2.zero)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }
        else
        {
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
        }
        return rt;
    }

    static Image NewImage(string name, Transform parent, string sprite, Vector2 pos, Vector2 size)
    {
        var rt = NewRect(name, parent, pos, size);
        var img = rt.gameObject.AddComponent<Image>();
        img.sprite = LoadSprite(sprite);
        img.raycastTarget = false;
        if (img.sprite == null)
            Debug.LogWarning($"[HUDBuilder] Không tìm thấy sprite {SpriteFolder}{sprite}.png");
        return img;
    }

    // Tools > Menu UI > Apply Bar Shaders: chỉ gắn shader cho HPFill và ManaFill có sẵn, không dựng lại HUD.
    [MenuItem("Tools/Menu UI/Apply Bar Shaders")]
    public static void ApplyBarShadersMenu()
    {
        var hudUI = Object.FindAnyObjectByType<HUDUI>(FindObjectsInactive.Include);
        if (hudUI == null || (hudUI.manaFill == null && hudUI.hpFill == null))
        {
            EditorUtility.DisplayDialog("HUD", "Không tìm thấy HUDUI có HPFill/ManaFill. Chạy Tools > Menu UI > Build HUD trước.", "OK");
            return;
        }
        if (hudUI.hpFill != null)
        {
            Undo.RecordObject(hudUI.hpFill, "Apply Bar Shaders");
            ApplyHPShader(hudUI.hpFill);
            EditorUtility.SetDirty(hudUI.hpFill);
        }
        if (hudUI.manaFill != null)
        {
            Undo.RecordObject(hudUI.manaFill, "Apply Bar Shaders");
            ApplyManaShader(hudUI.manaFill);
            EditorUtility.SetDirty(hudUI.manaFill);
        }
        EditorSceneManager.MarkSceneDirty(hudUI.gameObject.scene);
    }

    static void ApplyManaShader(Image img) => ApplyBarShader(img, ManaMaterialPath, ManaColor, null);

    // HP dùng chung shader nhưng tông đỏ: flow đỏ hồng, mép sáng hồng trắng, phần bị mất trắng ngà.
    static void ApplyHPShader(Image img) => ApplyBarShader(img, HPMaterialPath, HPColor, mat =>
    {
        mat.SetColor("_FlowColor", new Color(1.6f, 0.35f, 0.45f, 1f));
        mat.SetColor("_EdgeColor", new Color(2.5f, 1.3f, 1.4f, 1f));
        mat.SetColor("_DrainColor", new Color(1f, 0.92f, 0.8f, 0.8f));
    });

    // Shader UI/ManaBar tự cắt theo _Fill nên Image để Simple; sprite chỉ làm mask hình vát.
    // Material chỉ tạo khi chưa có, nên thông số đã chỉnh tay trên .mat được giữ nguyên.
    static void ApplyBarShader(Image img, string matPath, Color color, System.Action<Material> initMaterial)
    {
        var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        if (mat == null)
        {
            var shader = Shader.Find("UI/ManaBar");
            if (shader == null)
            {
                Debug.LogWarning($"[HUDBuilder] Không tìm thấy shader UI/ManaBar, {img.name} giữ kiểu Filled.");
                img.color = color;
                MakeFilled(img);
                return;
            }
            mat = new Material(shader);
            initMaterial?.Invoke(mat);
            AssetDatabase.CreateAsset(mat, matPath);
        }
        img.material = mat;
        img.type = Image.Type.Simple;
        img.color = color;
    }

    static void MakeFilled(Image img)
    {
        img.type = Image.Type.Filled;
        img.fillMethod = Image.FillMethod.Horizontal;
        img.fillOrigin = (int)Image.OriginHorizontal.Left;
        img.fillAmount = 1f;
    }

    static Sprite LoadSprite(string name) =>
        AssetDatabase.LoadAllAssetsAtPath(SpriteFolder + name + ".png").OfType<Sprite>().FirstOrDefault();
}
