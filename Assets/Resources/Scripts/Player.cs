using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class Player : MonoBehaviour
{
    private Animator animator;
    private string currentAnimaton;

    private const string IDLE = "Player_Idle";
    private const string RUN = "Player_Move";
    private const string DASH = "Player_Dash";
    private const string ATTACK = "Player_Attack";
    private const string ATTACK_2 = "Player_Attack_2";
    private const string ATTACK_3 = "Player_Attack_3";
    private const string DAMAGED = "damaged";
    private const string DEATH = "death";
    private const string JUMP = "Player_Jump";
    private const string FALL = "Player_Fall";
    private const string LAND = "Player_Land";
    private const string DASH_END = "Player_DashEnd";
    InputSystem_Actions inputActions;
    private bool isJumpPressed = false;
    private bool isGrounded = true; // true sẵn để không chạy anim Land ở frame đầu khi vào scene
    private bool isAttacking;
    private string attackAnimation = ATTACK;
    // Attack2 (D-pad xuống) / Attack3 (Y) lấy theo tên vì là action mới thêm trong InputSystem_Actions.inputactions.
    private InputAction attack2Action;
    private InputAction attack3Action;

    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private float walkSpeed = 2f;
    [SerializeField] private float jumpForce = 2f;
    [SerializeField] private LayerMask groundMask;
    [SerializeField] private Transform groundChecker;

    private float xAxis;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        inputActions = new InputSystem_Actions();
        attack2Action = inputActions.FindAction("Player/Attack2", true);
        attack3Action = inputActions.FindAction("Player/Attack3", true);

        vfxRenderers = GetComponentsInChildren<ParticleSystemRenderer>(true);
        vfxBaseFlip = new Vector3[vfxRenderers.Length];
        vfxSystems = new ParticleSystem[vfxRenderers.Length];
        vfxBaseFlipRotation = new float[vfxRenderers.Length];
        for (int i = 0; i < vfxRenderers.Length; i++)
        {
            vfxBaseFlip[i] = vfxRenderers[i].flip;
            vfxSystems[i] = vfxRenderers[i].GetComponent<ParticleSystem>();
            vfxBaseFlipRotation[i] = vfxSystems[i].main.flipRotation;
        }

        // Bụi chạy chỉ phát khi đang ở anim Move (xem ChangeAnimationState). Object tắt sẵn trong prefab nên bật lên rồi dừng phát.
        runDust = transform.Find("vfx_run_dust").GetComponent<ParticleSystem>();
        runDust.gameObject.SetActive(true);
        runDust.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    private ParticleSystem runDust;

    // Hạt VFX con dùng Render Alignment = View nên luôn quay mặt về camera, không lật theo rotation Y của Player.
    // Lật texture bằng flip.x (đảo so với giá trị gốc của prefab) để hình hạt cũng quay theo hướng mặt.
    private ParticleSystemRenderer[] vfxRenderers;
    private Vector3[] vfxBaseFlip;
    // Hạt có Start Rotation khác 0 (vd. vfx_chem_02, 03): lật gương cần cả đảo chiều xoay (θ -> -θ), không chỉ lật texture.
    // Flip Rotation = 1 đảo chiều xoay cho mọi hạt; chỉ áp dụng cho hạt phát ra sau đó.
    private ParticleSystem[] vfxSystems;
    private float[] vfxBaseFlipRotation;

    void SetFacing(int dir)
    {
        facingDir = dir;
        transform.rotation = dir < 0 ? Quaternion.Euler(0f, 180f, 0f) : Quaternion.identity;
        for (int i = 0; i < vfxRenderers.Length; i++)
        {
            Vector3 flip = vfxBaseFlip[i];
            if (dir < 0) flip.x = 1f - flip.x;
            vfxRenderers[i].flip = flip;

            var main = vfxSystems[i].main;
            main.flipRotation = dir < 0 ? 1f - vfxBaseFlipRotation[i] : vfxBaseFlipRotation[i];
        }
    }

    void ChangeAnimationState(string newAnimation)
    {
        if (currentAnimaton == newAnimation) return;

        animator.Play(newAnimation);
        currentAnimaton = newAnimation;

        // Rời anim Move thì chỉ ngừng phát, hạt bụi đã bay ra vẫn tan hết tự nhiên.
        // Emission của prefab chỉ 2 hạt/giây (hạt đầu ra sau 0.5 s), nên phát thêm 1 hạt ngay lúc bắt đầu chạy.
        if (newAnimation == RUN)
        {
            runDust.Play(true);
            runDust.Emit(1);
        }
        else
            runDust.Stop(true, ParticleSystemStopBehavior.StopEmitting);
    }

    private TabMenuUI menu;
    private SettingsUI settings;
    private ItemPickupPopup pickupPopup;

    private void OnEnable()
    {
        inputActions.Enable();

        // Khóa điều khiển nhân vật khi menu, Settings hoặc popup nhặt item đang mở.
        menu = FindAnyObjectByType<TabMenuUI>(FindObjectsInactive.Include);
        if (menu != null)
            menu.OpenChanged += RefreshInputLock;
        settings = FindAnyObjectByType<SettingsUI>(FindObjectsInactive.Include);
        if (settings != null)
            settings.OpenChanged += RefreshInputLock;
        pickupPopup = FindAnyObjectByType<ItemPickupPopup>(FindObjectsInactive.Include);
        if (pickupPopup != null)
            pickupPopup.OpenChanged += RefreshInputLock;
        RefreshInputLock(false);
    }

    private void OnDisable()
    {
        if (menu != null)
            menu.OpenChanged -= RefreshInputLock;
        if (settings != null)
            settings.OpenChanged -= RefreshInputLock;
        if (pickupPopup != null)
            pickupPopup.OpenChanged -= RefreshInputLock;
        SetInputLocked(false);
        inputActions.Disable();
    }

    // Bỏ qua tham số của event: đọc lại cả ba màn, vì mở Settings sẽ đóng menu Tab (và ngược lại).
    private void RefreshInputLock(bool _)
    {
        SetInputLocked((menu != null && menu.IsOpen) || (settings != null && settings.IsOpen)
            || (pickupPopup != null && pickupPopup.IsOpen));
    }

    // Tắt action map Player ở cả inputActions lẫn project-wide actions (Move đọc từ InputSystem.actions).
    // Map UI vẫn bật nên menu vẫn điều hướng được.
    private void SetInputLocked(bool locked)
    {
        var projectPlayerMap = InputSystem.actions?.FindActionMap("Player");
        if (locked)
        {
            inputActions.Player.Disable();
            projectPlayerMap?.Disable();
            isJumpPressed = false;
            dashPressed = false;
        }
        else
        {
            inputActions.Player.Enable();
            projectPlayerMap?.Enable();
        }
    }
    private void Update()
    {
        Vector2 moveDir = InputSystem.actions["Move"].ReadValue<Vector2>();
        xAxis = moveDir.x;

        if (inputActions.Player.Jump.triggered)
        {
            isJumpPressed = true;
        }

        // Thời gian khóa = độ dài clip: Attack 9 frame @12fps, Attack_2 39 nhịp @24fps, Attack_3 55 nhịp @24fps.
        if (!isAttacking)
        {
            if (inputActions.Player.Attack.triggered)
                StartAttack(ATTACK, 0.75f);
            else if (attack2Action.triggered)
                StartAttack(ATTACK_2, 39f / 24f, true);
            else if (attack3Action.triggered)
                StartAttack(ATTACK_3, 55f / 24f, true);
        }

        if (inputActions.Player.Dash.triggered)
        {
            dashPressed = true;
        }

        if (dashCooldownTimer > 0f)
            dashCooldownTimer -= Time.deltaTime;
    }
    bool isLanding = false;
    int facingDir = 1;

    [Header("Gravity Feel")]
    [SerializeField] private float baseGravity = 49f;       
    [SerializeField] private float fallMultiplier = 2.2f;   
    [SerializeField] private float lowJumpMultiplier = 2.5f; 
    [SerializeField] private float maxFallSpeed = 20f;
    [Header("Jump Hold")]
    [SerializeField] private float minJumpVelocity = 7f;   
    [SerializeField] private float maxJumpVelocity = 13f; 
    [SerializeField] private float maxJumpHoldTime = 0.25f;
    [Header("Apex Hang Time")]
    [SerializeField] private float apexThreshold = 1.5f;
    [SerializeField] private float apexGravityMultiplier = 0.6f; 
    [Tooltip("Bỏ qua ground check ngay sau khi nhảy, tránh ray còn chạm đất làm anim Move/Idle chen vào.")]
    [SerializeField] private float jumpGroundIgnoreTime = 0.1f;
    private float jumpHoldTimer = 0f;
    private float groundIgnoreTimer = 0f;
    [Tooltip("Thời gian chạy anim Land sau khi chạm đất (khi đứng yên). Player_Land: 4 frame ở 12 fps.")]
    [SerializeField] private float landDuration = 4f / 12f;
    private float landTimer = 0f;
    [Tooltip("Khoảng cách tối đa kéo nhân vật bám xuống mặt đất khi đi xuống dốc (đơn vị world).")]
    [SerializeField] private float slopeSnapDistance = 0.5f;
    [Tooltip("Tia dò đất bắt đầu cao hơn GroundCheck một đoạn này, để không xuất phát từ trong collider của đất.")]
    [SerializeField] private float groundProbeHeight = 0.5f;
    private Vector2 groundNormal = Vector2.up;
    [Tooltip("Khoảng dò tường phía trước khi đang ở trên không.")]
    [SerializeField] private float wallCheckDistance = 0.05f;
    private bool isJumping = false;

    [Header("Dash")]
    [SerializeField] private float dashSpeed = 12f;
    [SerializeField] private float dashDuration = 0.15f;
    [SerializeField] private float dashCooldown = 0.5f;
    [SerializeField] private int maxAirDashes = 1;
    [SerializeField] private LayerMask dashThroughMask;
    [Tooltip("Thời gian chạy anim phanh sau khi dash xong trên mặt đất (khi đứng yên). Player_DashEnd: 3 frame ở 12 fps.")]
    [SerializeField] private float dashEndDuration = 3f / 12f;
    private float dashEndTimer = 0f;

    [Header("Combo Lunge")]
    [Tooltip("Quãng đường lướt về phía trước khi ra đòn Attack_2 / Attack_3 (đơn vị world).")]
    [SerializeField] private float comboLungeDistance = 0.5f;
    [Tooltip("Thời gian lướt, tính từ lúc bắt đầu đòn.")]
    [SerializeField] private float comboLungeDuration = 0.15f;
    private float lungeTimer = 0f;

    [Header("Attack Hitbox")]
    [Tooltip("Tâm vùng đánh, tọa độ local của Player (x dương = phía trước mặt). Tự lật khi Player quay mặt.")]
    [SerializeField] private Vector2 attackHitboxOffset = new Vector2(2.5f, 1.6f);
    [SerializeField] private Vector2 attackHitboxSize = new Vector2(4.8f, 2.6f);

    private bool isDashing = false;
    private bool dashPressed = false;
    private float dashTimer = 0f;
    private float dashCooldownTimer = 0f;
    private int airDashesUsed = 0;
    private Vector2 dashDirection;
    private void FixedUpdate()
    {
        applyGravity();
        bool wasGrounded = isGrounded;
        if (groundIgnoreTimer > 0f)
            groundIgnoreTimer -= Time.fixedDeltaTime;

        // Dò đất bằng tia bắt đầu cao hơn GroundCheck (groundProbeHeight), để tia không xuất phát từ trong collider
        // và trả về đúng pháp tuyến của mặt dốc. groundDistance > 0: GroundCheck đang ở trên mặt đất.
        Vector2 probeOrigin = (Vector2)groundChecker.position + Vector2.up * groundProbeHeight;
        RaycastHit2D hit = Physics2D.Raycast(probeOrigin, Vector2.down, groundProbeHeight + slopeSnapDistance, groundMask);
        float groundDistance = hit.collider != null ? hit.distance - groundProbeHeight : float.MaxValue;
        groundNormal = hit.collider != null ? hit.normal : Vector2.up;
        isGrounded = groundDistance <= 0.1f && groundIgnoreTimer <= 0f;

        // Bám dốc: qua mép sàn xuống dốc (hoặc dốc gãy khúc) thì có thể hẫng khỏi mặt đất vài frame.
        // Frame trước còn đứng trên đất, không nhảy/dash, và đất bên dưới còn trong slopeSnapDistance
        // thì kéo xuống sát đất và coi như vẫn đứng. Hụt chân khỏi mép cao hơn khoảng này thì rơi bình thường.
        if (!isGrounded && wasGrounded && hit.collider != null && !isDashing && !isJumping
            && groundIgnoreTimer <= 0f && rb.linearVelocityY < 1f)
        {
            rb.position += Vector2.down * groundDistance;
            isGrounded = true;
        }

        if (isGrounded && !wasGrounded)
        {
            airDashesUsed = 0;
            landTimer = landDuration;
        }

        if(dashPressed && !isDashing && dashCooldownTimer <= 0f && !isAttacking)
        {
            bool canDash = isGrounded || airDashesUsed < maxAirDashes;
            if (canDash)
            {
                startDash();
                if (!isGrounded) airDashesUsed++;
            }
        }
        dashPressed = false;

        if (isDashing)
        {
            dashTimer -= Time.fixedDeltaTime;
            rb.linearVelocity = dashDirection * dashSpeed;

            if (dashTimer <= 0f)
            {
                endDash();
            }
            else
            {
                return;
            }
        }
            Debug.Log($"check dash: {isDashing}, {dashPressed}, {dashCooldownTimer <= 0f}");
        Vector2 vel = new Vector2(0, rb.linearVelocityY);

        // Đang attack: không chạy và không quay mặt (vel.x giữ 0).
        if (!isAttacking && xAxis < 0)
        {
            vel.x = -walkSpeed;
            SetFacing(-1);
        }
        else if (!isAttacking && xAxis > 0)
        {
            vel.x = walkSpeed;
            SetFacing(1);
        }

        // Combo 2/3: lướt về hướng mặt một đoạn ngắn lúc đầu đòn.
        if (lungeTimer > 0f)
        {
            lungeTimer -= Time.fixedDeltaTime;
            vel.x = facingDir * comboLungeDistance / comboLungeDuration;
        }

        // Trên không mà ép vào tường: bỏ vận tốc ngang để không bị ma sát giữ dính vào tường, cứ rơi xuống.
        if (!isGrounded && vel.x != 0f && IsBlockedHorizontally(Mathf.Sign(vel.x)))
            vel.x = 0f;

        // Đứng trên dốc: chạy dọc theo mặt dốc thay vì nằm ngang, để xuống dốc không bị phóng ra khỏi mặt đất.
        // Nhảy (startJump bên dưới) sẽ ghi đè vel.y nên không bị ảnh hưởng.
        if (isGrounded && vel.x != 0f && groundNormal.y < 0.999f && groundNormal.y > 0.3f)
        {
            Vector2 tangent = new Vector2(groundNormal.y, -groundNormal.x); // hướng sang phải dọc mặt dốc
            float speed = Mathf.Abs(vel.x); // walkSpeed khi chạy, tốc độ lướt khi đang lướt combo
            vel = tangent * (vel.x / tangent.x);
            vel = vel.normalized * speed;
        }

        if (isGrounded)
        {
            if (!isAttacking && !isDashing)
            {
                if (xAxis != 0)
                {
                    // Chạy luôn khi vừa tiếp đất mà đang bấm hướng, không chờ anim Land.
                    landTimer = 0f;
                    dashEndTimer = 0f;
                    ChangeAnimationState(RUN);
                }
                else if (dashEndTimer > 0f)
                {
                    dashEndTimer -= Time.fixedDeltaTime;
                    ChangeAnimationState(DASH_END);
                }
                else if (landTimer > 0f)
                {
                    landTimer -= Time.fixedDeltaTime;
                    ChangeAnimationState(LAND);
                }
                else
                {
                    ChangeAnimationState(IDLE);
                }
                if (isJumpPressed)
                {
                    Debug.Log("Jump");
                    startJump(ref vel);
                    isJumpPressed = false;
                    ChangeAnimationState(JUMP);
                }
            }
            else if (isAttacking)
            {
                ChangeAnimationState(attackAnimation);
            }
            else
            {
                ChangeAnimationState(DASH);
            }
        }
        else
        {
            if (vel.y < 0)
            {
                // Land chạy sau khi chạm đất (landTimer), không chạy trước lúc chạm đất trên không.
                ChangeAnimationState(FALL);
            }
            else
            {
                // Đang bay lên (hoặc đứng yên giữa không trung sau dash): không giữ anim của mặt đất.
                ChangeAnimationState(JUMP);
            }
        }
        handleJumpHold(ref vel);
        rb.linearVelocity = vel;
    }

    private readonly RaycastHit2D[] wallHits = new RaycastHit2D[4];

    // Cast toàn bộ collider của Player một đoạn ngắn theo hướng ngang. Chỉ tính mặt gần như thẳng đứng
    // (pháp tuyến ngược hướng di chuyển), để dốc và mép sàn bên dưới không bị coi là tường.
    private bool IsBlockedHorizontally(float dir)
    {
        var filter = new ContactFilter2D { useLayerMask = true, layerMask = groundMask, useTriggers = false };
        int count = rb.Cast(new Vector2(dir, 0f), filter, wallHits, wallCheckDistance);
        for (int i = 0; i < count; i++)
        {
            Vector2 n = wallHits[i].normal;
            if (n.x * dir < 0f && Mathf.Abs(n.y) < 0.3f)
                return true;
        }
        return false;
    }

    private void StartAttack(string animation, float duration, bool lunge = false)
    {
        isAttacking = true;
        attackAnimation = animation;
        lungeTimer = lunge ? comboLungeDuration : 0f;
        //doAttack
        StartCoroutine(renewAttack(duration));
    }

    // Gọi từ Animation Event "AttackHit" ở frame chém của Player_Attack / _2 / _3.
    // Mỗi event, mỗi enemy trong vùng đánh chỉ bị trúng 1 lần (enemy có nhiều collider vẫn tính 1).
    public void AttackHit()
    {
        Vector2 center = transform.TransformPoint(attackHitboxOffset);
        var hitEnemies = new System.Collections.Generic.HashSet<EnemyHealth>();
        foreach (var col in Physics2D.OverlapBoxAll(center, attackHitboxSize, 0f))
        {
            var enemy = col.GetComponentInParent<EnemyHealth>();
            if (enemy != null && hitEnemies.Add(enemy))
                enemy.TakeHit();
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(transform.TransformPoint(attackHitboxOffset), attackHitboxSize);
    }

    private IEnumerator renewAttack(float delay)
    {
        yield return new WaitForSeconds(delay);
        isAttacking = false;
    }

    private void applyGravity()
    {
        // Đang dash (kể cả trên không): bỏ trọng lực để lướt thẳng, không bị kéo xuống.
        // startDash cũng đặt 0, nhưng hàm này chạy đầu mỗi FixedUpdate nên phải giữ 0 ở đây.
        if (isDashing)
        {
            rb.gravityScale = 0f;
            return;
        }

        float gravityMultiplier;

        bool nearApex = Mathf.Abs(rb.linearVelocityY) < apexThreshold;

        if (nearApex)
        {
            gravityMultiplier = apexGravityMultiplier;
        }
        else if (rb.linearVelocityY < 0)
        {
            gravityMultiplier = fallMultiplier;
        }
        else if (rb.linearVelocityY > 0 && !inputActions.Player.Jump.IsPressed())
        {
            gravityMultiplier = lowJumpMultiplier;
        }
        else
        {
            gravityMultiplier = 1f;
        }

        rb.gravityScale = baseGravity * gravityMultiplier / Mathf.Abs(Physics2D.gravity.y);

        if (rb.linearVelocityY < -maxFallSpeed)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocityX, -maxFallSpeed);
        }
    }
    private void handleJumpHold(ref Vector2 vel)
    {
        if (!isJumping) return;

        bool jumpHeld = inputActions.Player.Jump.IsPressed();

        if (jumpHeld && jumpHoldTimer < maxJumpHoldTime && vel.y > 0)
        {
            jumpHoldTimer += Time.fixedDeltaTime;
            float t = jumpHoldTimer / maxJumpHoldTime;
            vel.y = Mathf.SmoothStep(minJumpVelocity, maxJumpVelocity, t);
            Debug.Log("On Jump Hold");
        }
        else
        {
            isJumping = false;
            Debug.Log($"End Jump Hold: {jumpHoldTimer < maxJumpHoldTime}, {rb.linearVelocityY > 0}");
        }

        if (isGrounded && vel.y <= 0)
        {
            isJumping = false;
            Debug.Log("Ground Jump Hold");
        }
    }

    private void startJump(ref Vector2 vel)
    {
        vel.y = minJumpVelocity;
        isJumping = true;
        jumpHoldTimer = 0f;
        groundIgnoreTimer = jumpGroundIgnoreTime;
        // Bụi chạy mô phỏng theo không gian Local nên sẽ bay theo Player lên không: xóa ngay khi nhảy.
        runDust.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    private void startDash()
    {
        isDashing = true;
        dashTimer = dashDuration;
        dashCooldownTimer = dashCooldown;
        isJumping = false; 

        Vector2 moveInput = InputSystem.actions["Move"].ReadValue<Vector2>();

        if (moveInput.sqrMagnitude > 0.01f)
        {
            dashDirection = moveInput.normalized;
        }
        else
        {
            dashDirection = new Vector2(facingDir, 0);
        }

        rb.gravityScale = 0f;
        ChangeAnimationState(DASH);
    }

    private void endDash()
    {
        isDashing = false;
        rb.linearVelocity = Vector2.zero;
        // Dash xong trên mặt đất thì chạy anim phanh; trên không thì rơi bình thường.
        dashEndTimer = isGrounded ? dashEndDuration : 0f;
    }
}
