using System.Linq;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Tools > Camera > Setup Cinemachine 2D: gắn CinemachineBrain lên Main Camera, tạo "CM Player Camera"
// bám theo Player (Position Composer, có damping, dead zone, lookahead), và giới hạn camera trong
// "CameraBounds" (PolygonCollider2D bao quanh các collider con của Grid) bằng Confiner 2D.
// Chạy lại được: dùng lại object đã có, chỉ cập nhật target, bounds giữ nguyên nếu đã tồn tại.
public static class CameraSetup
{
    const string VCamName = "CM Player Camera";
    const string BoundsName = "CameraBounds";

    [MenuItem("Tools/Camera/Setup Cinemachine 2D")]
    public static void Setup()
    {
        var player = Object.FindAnyObjectByType<Player>(FindObjectsInactive.Include);
        if (player == null)
        {
            EditorUtility.DisplayDialog("Camera", "Không tìm thấy object có script Player trong scene.", "OK");
            return;
        }

        Camera mainCam = Camera.main != null ? Camera.main : Object.FindAnyObjectByType<Camera>();
        if (mainCam == null)
        {
            EditorUtility.DisplayDialog("Camera", "Không tìm thấy Camera trong scene.", "OK");
            return;
        }

        // Brain trên Main Camera: camera thật sẽ đi theo vcam đang active.
        if (mainCam.GetComponent<CinemachineBrain>() == null)
            Undo.AddComponent<CinemachineBrain>(mainCam.gameObject);

        // Virtual camera
        var vcamGO = GameObject.Find(VCamName);
        bool created = vcamGO == null;
        if (created)
        {
            vcamGO = new GameObject(VCamName);
            Undo.RegisterCreatedObjectUndo(vcamGO, "Setup Cinemachine 2D");
        }

        var vcam = GetOrAdd<CinemachineCamera>(vcamGO);
        Undo.RecordObject(vcam, "Setup Cinemachine 2D");
        vcam.Target.TrackingTarget = player.transform;
        if (created)
        {
            var lens = vcam.Lens;
            lens.ModeOverride = LensSettings.OverrideModes.Orthographic;
            lens.OrthographicSize = mainCam.orthographicSize;
            lens.NearClipPlane = mainCam.nearClipPlane;
            lens.FarClipPlane = mainCam.farClipPlane;
            vcam.Lens = lens;
            vcamGO.transform.position = new Vector3(player.transform.position.x, player.transform.position.y, mainCam.transform.position.z);
        }
        EditorUtility.SetDirty(vcam);

        var composer = GetOrAdd<CinemachinePositionComposer>(vcamGO);
        if (created)
        {
            Undo.RecordObject(composer, "Setup Cinemachine 2D");
            composer.CameraDistance = Mathf.Abs(mainCam.transform.position.z - player.transform.position.z);
            if (composer.CameraDistance < 1f) composer.CameraDistance = 10f;
            composer.Damping = new Vector3(0.5f, 0.6f, 0f);
            // Đặt nhân vật hơi thấp hơn tâm màn hình để thấy nhiều phía trên hơn.
            var comp = composer.Composition;
            comp.ScreenPosition = new Vector2(0f, -0.1f);
            comp.DeadZone.Enabled = true;
            comp.DeadZone.Size = new Vector2(0.1f, 0.15f);
            composer.Composition = comp;
            composer.Lookahead = new LookaheadSettings { Enabled = true, Time = 0.3f, Smoothing = 8f, IgnoreY = true };
            EditorUtility.SetDirty(composer);
        }

        // Confiner: giới hạn camera trong vùng màn chơi.
        string boundsInfo = SetupConfiner(vcamGO);

        Selection.activeGameObject = vcamGO;
        EditorSceneManager.MarkSceneDirty(vcamGO.scene);
        Debug.Log($"[CameraSetup] {VCamName} bám theo {player.name}. {boundsInfo}");
    }

    static string SetupConfiner(GameObject vcamGO)
    {
        var boundsGO = GameObject.Find(BoundsName);
        if (boundsGO == null)
        {
            // Lấy vùng hình vẽ (renderer) dưới Grid: ngoài vùng này là nền trống của camera.
            var grid = GameObject.Find("Grid");
            var areas = grid != null
                ? grid.GetComponentsInChildren<Renderer>().Select(r => r.bounds)
                    .Concat(grid.GetComponentsInChildren<Collider2D>().Where(c => !c.isTrigger).Select(c => c.bounds))
                    .ToArray()
                : new Bounds[0];
            if (areas.Length == 0)
                return "Không tìm thấy renderer/collider dưới Grid nên chưa tạo Confiner. Tự tạo object \"CameraBounds\" có PolygonCollider2D rồi chạy lại.";

            Bounds b = areas[0];
            foreach (var a in areas) b.Encapsulate(a);

            boundsGO = new GameObject(BoundsName);
            Undo.RegisterCreatedObjectUndo(boundsGO, "Setup Cinemachine 2D");
            // Ignore Raycast: tránh ground/wall check của Player (Physics2D.Raycast mặc định trúng cả trigger) chạm vào vùng này.
            boundsGO.layer = LayerMask.NameToLayer("Ignore Raycast");
            var poly = boundsGO.AddComponent<PolygonCollider2D>();
            poly.isTrigger = true; // không va chạm với nhân vật
            poly.points = new[]
            {
                new Vector2(b.min.x, b.min.y), new Vector2(b.min.x, b.max.y),
                new Vector2(b.max.x, b.max.y), new Vector2(b.max.x, b.min.y),
            };
        }

        var shape = boundsGO.GetComponent<Collider2D>();
        if (shape == null)
            return $"\"{BoundsName}\" không có Collider2D nên chưa gắn Confiner.";

        var confiner = GetOrAdd<CinemachineConfiner2D>(vcamGO);
        Undo.RecordObject(confiner, "Setup Cinemachine 2D");
        confiner.BoundingShape2D = shape;
        confiner.InvalidateBoundingShapeCache();
        EditorUtility.SetDirty(confiner);
        return $"Confiner dùng \"{BoundsName}\", chỉnh polygon nếu vùng chưa đúng.";
    }

    static T GetOrAdd<T>(GameObject go) where T : Component
    {
        var c = go.GetComponent<T>();
        return c != null ? c : Undo.AddComponent<T>(go);
    }
}
