using System;
using System.Collections;
using UnityEngine;

// Máu của enemy tính bằng số đòn: trúng đủ maxHits thì chết.
// Mỗi lần trúng chạy anim Hurt rồi về Idle. Lần trúng cuối: tắt va chạm, chạy Hurt xong thì tắt object.
[RequireComponent(typeof(Animator))]
public class EnemyHealth : MonoBehaviour
{
    [SerializeField] private int maxHits = 4;
    [SerializeField] private string idleState = "Enemy_Idle";
    [SerializeField] private string hurtState = "Enemy_Hurt";
    [Tooltip("Thời gian giữ anim Hurt trước khi về Idle (hoặc biến mất nếu chết). Enemy_Hurt: 2 sprite, mỗi sprite giữ 2 frame ở 12 fps.")]
    [SerializeField] private float hurtDuration = 4f / 12f;

    [Header("Phần thưởng khi chết")]
    [Tooltip("Vàng và EXP mỗi loại random riêng trong khoảng [min, max], tính cả hai đầu.")]
    [SerializeField] private int minReward = 100;
    [SerializeField] private int maxReward = 1000;

    public bool IsDead => hits >= maxHits;
    public event Action<EnemyHealth> Died;

    private Animator animator;
    private int hits;
    private Coroutine hurtRoutine;

    private void Awake()
    {
        animator = GetComponent<Animator>();
    }

    public void TakeHit()
    {
        if (IsDead)
            return;
        hits++;
        animator.Play(hurtState, 0, 0f);
        if (hurtRoutine != null)
            StopCoroutine(hurtRoutine);

        if (IsDead)
        {
            foreach (var col in GetComponents<Collider2D>())
                col.enabled = false;
            var rb = GetComponent<Rigidbody2D>();
            if (rb != null)
                rb.simulated = false;
            GiveReward();
            Died?.Invoke(this);
        }
        hurtRoutine = StartCoroutine(AfterHurt());
    }

    // Cộng vàng và EXP vào HUD (HUD đang giữ số coin / exp của người chơi).
    private void GiveReward()
    {
        var hud = FindAnyObjectByType<HUDUI>(FindObjectsInactive.Include);
        if (hud == null)
            return;
        int gold = UnityEngine.Random.Range(minReward, maxReward + 1);
        int xp = UnityEngine.Random.Range(minReward, maxReward + 1);
        hud.SetCoins(hud.coins + gold);
        hud.SetExp(hud.exp + xp);
    }

    private IEnumerator AfterHurt()
    {
        yield return new WaitForSeconds(hurtDuration);
        hurtRoutine = null;
        if (IsDead)
            gameObject.SetActive(false);
        else
            animator.Play(idleState, 0, 0f);
    }
}
