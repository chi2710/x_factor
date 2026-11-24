using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

// Tools > Animation > Build Player Dash: ghi lại Player_Dash và tạo Player_DashEnd từ sheet Sprites/Player/Dash/DASH - 00.png.
// Sửa trên clip có sẵn nên GUID giữ nguyên; thêm state Player_DashEnd vào Player.controller nếu chưa có.
//
// Thứ tự frame trong sheet (trái → phải, theo tên slice):
//   0-1 lấy đà, 2-3-6-7-8 lướt, 9 phanh, 10-11 đứng dậy. (Không có slice 4, 5.)
// Dash chỉ kéo dài dashDuration (0.15s) nên chạy nhanh hơn (24 fps); phần phanh chạy sau khi dash xong trên mặt đất.
public static class PlayerDashAnimBuilder
{
    const string SheetPath = "Assets/Resources/Sprites/Player/Dash/DASH - 00.png";
    const string ClipFolder = "Assets/Resources/Animation/Player/";
    const string ControllerPath = "Assets/Resources/Animation/Player/Player.controller";

    static readonly (string clip, int[] frames, float fps)[] Clips =
    {
        ("Player_Dash", new[] { 0, 1, 2, 3, 6, 7, 8 }, 24f),
        ("Player_DashEnd", new[] { 9, 10, 11 }, 12f),
    };

    [MenuItem("Tools/Animation/Build Player Dash")]
    public static void Build()
    {
        var sprites = AssetDatabase.LoadAllAssetsAtPath(SheetPath).OfType<Sprite>().ToDictionary(s => s.name);
        if (sprites.Count == 0)
        {
            EditorUtility.DisplayDialog("Animation", $"Không tìm thấy sprite trong {SheetPath}.", "OK");
            return;
        }

        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        var binding = EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite");

        foreach (var (clipName, frames, fps) in Clips)
        {
            var missing = frames.Where(i => !sprites.ContainsKey(SpriteName(i))).ToArray();
            if (missing.Length > 0)
            {
                Debug.LogWarning($"[PlayerDashAnimBuilder] {clipName}: thiếu slice {string.Join(", ", missing.Select(SpriteName))}, bỏ qua.");
                continue;
            }

            string path = ClipFolder + clipName + ".anim";
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (clip == null)
            {
                clip = new AnimationClip();
                AssetDatabase.CreateAsset(clip, path);
            }

            // Thêm 1 key lặp lại frame cuối để frame cuối cũng được hiện đủ một frame.
            var keys = new ObjectReferenceKeyframe[frames.Length + 1];
            for (int i = 0; i < frames.Length; i++)
                keys[i] = new ObjectReferenceKeyframe { time = i / fps, value = sprites[SpriteName(frames[i])] };
            keys[frames.Length] = new ObjectReferenceKeyframe { time = frames.Length / fps, value = keys[frames.Length - 1].value };

            Undo.RecordObject(clip, "Build Player Dash Clips");
            clip.frameRate = fps;
            AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);

            // Không lặp: Dash giữ frame lướt cuối, DashEnd chạy một lần.
            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = false;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            EditorUtility.SetDirty(clip);

            if (controller != null)
                EnsureState(controller, clipName, clip);
        }

        if (controller == null)
            Debug.LogWarning($"[PlayerDashAnimBuilder] Không tìm thấy {ControllerPath}, chưa thêm state.");

        AssetDatabase.SaveAssets();
        Debug.Log("[PlayerDashAnimBuilder] Đã cập nhật Player_Dash, Player_DashEnd.");
    }

    // Player gọi animator.Play(tên state) nên chỉ cần state đúng tên, không cần transition.
    static void EnsureState(AnimatorController controller, string name, AnimationClip clip)
    {
        var machine = controller.layers[0].stateMachine;
        var state = machine.states.Select(s => s.state).FirstOrDefault(s => s.name == name);
        if (state == null)
        {
            Undo.RecordObject(machine, "Add Player Dash State");
            state = machine.AddState(name);
        }
        state.motion = clip;
        EditorUtility.SetDirty(state);
        EditorUtility.SetDirty(controller);
    }

    static string SpriteName(int index) => "DASH - 00_" + index;
}
