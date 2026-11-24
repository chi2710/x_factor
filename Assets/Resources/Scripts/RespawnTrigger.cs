using Unity.Cinemachine;
using UnityEngine;

// Gắn lên object có collider trigger (vd. đáy màn Scene01): Player chạm vào thì màn hình fade đen
// và Player được đưa về vị trí lúc bắt đầu game (lưu trong Start).
public class RespawnTrigger : MonoBehaviour
{
    [Tooltip("Tổng thời gian fade: nửa đầu tối dần, dịch chuyển lúc đen hẳn, nửa sau sáng lại.")]
    [SerializeField] private float fadeDuration = 0.5f;
    [Tooltip("Để trống thì tự tìm ScreenFader trong scene.")]
    [SerializeField] private ScreenFader fader;

    private Player player;
    private Vector3 startPosition;

    private void Start()
    {
        player = FindAnyObjectByType<Player>();
        if (player != null)
            startPosition = player.transform.position;
        if (fader == null)
            fader = FindAnyObjectByType<ScreenFader>(FindObjectsInactive.Include);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (player == null || other.GetComponentInParent<Player>() != player)
            return;
        if (fader == null)
            Respawn();
        else if (!fader.IsFading)
            fader.FadeOutIn(fadeDuration, Respawn);
    }

    private void Respawn()
    {
        Vector3 delta = startPosition - player.transform.position;
        var rb = player.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.position = startPosition;
        }
        player.transform.position = startPosition;
        // Báo cho camera biết Player vừa dịch chuyển tức thời, để camera nhảy theo thay vì trôi từ chỗ cũ.
        foreach (var cam in FindObjectsByType<CinemachineCamera>(FindObjectsInactive.Exclude))
            cam.OnTargetObjectWarped(player.transform, delta);
    }
}
