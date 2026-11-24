using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

// Tools > Enemy AI > Build Animator: tạo Enemy_Attack.anim + Enemy.controller từ Sprites/Enemy/0.png
// rồi gắn SpriteRenderer + Animator vào GameObject đang chọn (hoặc "Enemy" trong scene).
// State tên "Attack" khớp biến AttackAnimation của EnemyTreeBuilder.
public static class EnemyAnimatorBuilder
{
    private const string SpriteSheet = "Assets/Resources/Sprites/Enemy/0.png";
    private const string Folder = "Assets/Resources/Animation/Enemy";
    private const string ClipPath = Folder + "/Enemy_Attack.anim";
    private const string ControllerPath = Folder + "/Enemy.controller";
    private const float FrameRate = 12f;

    [MenuItem("Tools/Enemy AI/Build Animator")]
    private static void Build()
    {
        var go = Selection.activeGameObject != null ? Selection.activeGameObject : GameObject.Find("Enemy");
        if (go == null)
        {
            EditorUtility.DisplayDialog("Enemy AI", "Chọn GameObject enemy trong Hierarchy trước.", "OK");
            return;
        }

        var sprites = AssetDatabase.LoadAllAssetsAtPath(SpriteSheet).OfType<Sprite>()
            .OrderBy(s => int.Parse(s.name.Substring(s.name.LastIndexOf('_') + 1)))
            .ToArray();
        if (sprites.Length == 0)
        {
            EditorUtility.DisplayDialog("Enemy AI", $"Không tìm thấy sprite trong {SpriteSheet}.", "OK");
            return;
        }

        if (!AssetDatabase.IsValidFolder(Folder))
            AssetDatabase.CreateFolder("Assets/Resources/Animation", "Enemy");

        // Attack clip
        var clip = new AnimationClip { name = "Enemy_Attack", frameRate = FrameRate };
        var keys = new ObjectReferenceKeyframe[sprites.Length + 1];
        for (int i = 0; i < sprites.Length; i++)
            keys[i] = new ObjectReferenceKeyframe { time = i / FrameRate, value = sprites[i] };
        keys[sprites.Length] = new ObjectReferenceKeyframe { time = sprites.Length / FrameRate, value = sprites[sprites.Length - 1] };
        var binding = EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite");
        AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);
        var settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = false;
        AnimationUtility.SetAnimationClipSettings(clip, settings);
        AssetDatabase.DeleteAsset(ClipPath);
        AssetDatabase.CreateAsset(clip, ClipPath);

        // Animator controller
        AssetDatabase.DeleteAsset(ControllerPath);
        var controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
        var attack = controller.layers[0].stateMachine.AddState("Attack");
        attack.motion = clip;
        AssetDatabase.SaveAssets();

        // Gắn vào Enemy
        var sr = go.GetComponent<SpriteRenderer>();
        if (sr == null) sr = Undo.AddComponent<SpriteRenderer>(go);
        Undo.RecordObject(sr, "Build Enemy Animator");
        sr.sprite = sprites[0];

        var animator = go.GetComponent<Animator>();
        if (animator == null) animator = Undo.AddComponent<Animator>(go);
        Undo.RecordObject(animator, "Build Enemy Animator");
        animator.runtimeAnimatorController = controller;

        EditorUtility.SetDirty(go);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(go.scene);
        Debug.Log($"Enemy animator built: {sprites.Length} frames -> {ClipPath}, {ControllerPath}, gắn vào '{go.name}'.");
    }
}
