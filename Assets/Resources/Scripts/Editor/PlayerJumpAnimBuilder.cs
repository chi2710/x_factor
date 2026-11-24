using System.Linq;
using UnityEditor;
using UnityEngine;

// Tools > Animation > Build Player Jump/Fall/Land: ghi lại frame cho Player_Jump, Player_Fall, Player_Land
// từ sheet Sprites/Player/Jump/JUMP - 00x.png. Sửa trên clip có sẵn nên GUID giữ nguyên, Animator không phải gán lại.
//
// Thứ tự frame trong sheet (trái → phải, theo tên slice):
//   0 đứng, 1 hạ người, 9 hạ sâu, 2 bật lên, 3 bay lên, 4 gần đỉnh, 5 đỉnh, 6-7 rơi, 10-11 tiếp đất, 12 đứng dậy, 8 đứng.
// Bỏ 2 frame lấy đà (1, 9) vì Player bật lên ngay khi bấm nhảy, chèn lấy đà sẽ thấy trễ.
public static class PlayerJumpAnimBuilder
{
    const string SheetPath = "Assets/Resources/Sprites/Player/Jump/JUMP - 00x.png";
    const string ClipFolder = "Assets/Resources/Animation/Player/";
    const float FrameRate = 12f;

    static readonly (string clip, int[] frames)[] Clips =
    {
        ("Player_Jump", new[] { 2, 3, 4 }),
        ("Player_Fall", new[] { 5, 6, 7 }),
        ("Player_Land", new[] { 10, 11, 12, 8 }),
    };

    [MenuItem("Tools/Animation/Build Player Jump-Fall-Land")]
    public static void Build()
    {
        var sprites = AssetDatabase.LoadAllAssetsAtPath(SheetPath).OfType<Sprite>().ToDictionary(s => s.name);
        if (sprites.Count == 0)
        {
            EditorUtility.DisplayDialog("Animation", $"Không tìm thấy sprite trong {SheetPath}.", "OK");
            return;
        }

        var binding = EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite");
        foreach (var (clipName, frames) in Clips)
        {
            string path = ClipFolder + clipName + ".anim";
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (clip == null)
            {
                clip = new AnimationClip();
                AssetDatabase.CreateAsset(clip, path);
            }

            var missing = frames.Where(i => !sprites.ContainsKey(SpriteName(i))).ToArray();
            if (missing.Length > 0)
            {
                Debug.LogWarning($"[PlayerJumpAnimBuilder] {clipName}: thiếu slice {string.Join(", ", missing.Select(SpriteName))}, bỏ qua.");
                continue;
            }

            // Thêm 1 key lặp lại frame cuối để frame cuối cũng được hiện đủ 1/12 giây.
            var keys = new ObjectReferenceKeyframe[frames.Length + 1];
            for (int i = 0; i < frames.Length; i++)
                keys[i] = new ObjectReferenceKeyframe { time = i / FrameRate, value = sprites[SpriteName(frames[i])] };
            keys[frames.Length] = new ObjectReferenceKeyframe { time = frames.Length / FrameRate, value = keys[frames.Length - 1].value };

            Undo.RecordObject(clip, "Build Player Jump Clips");
            clip.frameRate = FrameRate;
            AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);

            // Không lặp: Jump/Fall giữ frame cuối tới khi đổi state, Land chạy một lần.
            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = false;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            EditorUtility.SetDirty(clip);
        }

        AssetDatabase.SaveAssets();
        Debug.Log("[PlayerJumpAnimBuilder] Đã cập nhật Player_Jump, Player_Fall, Player_Land.");
    }

    static string SpriteName(int index) => "JUMP - 00x_" + index;
}
