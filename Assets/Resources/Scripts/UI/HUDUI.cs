using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Thanh trạng thái góc trái trên: HP, Mana và vệt sáng chạy theo đầu thanh. Góc phải trên: số coin và EXP.
// Thanh nào dùng shader UI/ManaBar thì shader tự cắt (_Fill), kèm phần bị mất chạy trễ phía sau (_DrainFill);
// nếu không, Image Filled dùng fillAmount như bình thường.
public class HUDUI : MonoBehaviour
{
    public Image hpFill;
    public RectTransform hpGlow;
    public Image manaFill;
    public RectTransform manaGlow;
    public Image[] skillIcons;

    [Range(0, 1)] public float hp = 1f;
    [Range(0, 1)] public float mana = 1f;

    [Header("Currency")]
    public TextMeshProUGUI coinText;
    public TextMeshProUGUI expText;
    public int coins;
    public int exp;

    [Header("Drain")]
    [Tooltip("Giây chờ trước khi phần bị mất bắt đầu co lại.")]
    public float drainDelay = 0.4f;
    [Tooltip("Tốc độ co của phần bị mất (tỉ lệ thanh mỗi giây).")]
    public float drainSpeed = 0.6f;

    private static readonly int FillId = Shader.PropertyToID("_Fill");
    private static readonly int DrainFillId = Shader.PropertyToID("_DrainFill");

    // Trạng thái hiệu ứng của một thanh: material riêng và mức "bị mất" đang co lại.
    private class BarFx
    {
        public Material mat;
        public float drain = 1f;
        public float hold;
    }

    private BarFx hpFx, manaFx;

    private void Start()
    {
        hpFx = CreateFx(hpFill, hp);
        manaFx = CreateFx(manaFill, mana);
        Refresh();
    }

    private void OnDestroy()
    {
        if (hpFx?.mat != null) Destroy(hpFx.mat);
        if (manaFx?.mat != null) Destroy(manaFx.mat);
    }

    private void Update()
    {
        Tick(hpFx, hp);
        Tick(manaFx, mana);
    }

    private void OnValidate()
    {
        Refresh();
    }

    public void SetHealth(float current, float max)
    {
        hp = Set(hpFx, hp, max > 0 ? Mathf.Clamp01(current / max) : 0);
        Refresh();
    }

    public void SetMana(float current, float max)
    {
        mana = Set(manaFx, mana, max > 0 ? Mathf.Clamp01(current / max) : 0);
        Refresh();
    }

    public void SetCoins(int value)
    {
        coins = value;
        Refresh();
    }

    public void SetExp(int value)
    {
        exp = value;
        Refresh();
    }

    // Tạo bản sao material để không sửa vào asset dùng chung. Trả về null nếu thanh không dùng shader.
    private static BarFx CreateFx(Image fill, float amount)
    {
        if (fill == null || fill.material == null || !fill.material.HasProperty(FillId))
            return null;
        var fx = new BarFx { mat = new Material(fill.material) { name = fill.material.name + " (Instance)" }, drain = amount };
        fill.material = fx.mat;
        return fx;
    }

    private float Set(BarFx fx, float oldValue, float newValue)
    {
        if (fx != null && newValue < oldValue)
            fx.hold = drainDelay;
        return newValue;
    }

    private void Tick(BarFx fx, float amount)
    {
        if (fx == null) return;

        if (fx.drain > amount)
        {
            if (fx.hold > 0f)
                fx.hold -= Time.deltaTime;
            else
                fx.drain = Mathf.MoveTowards(fx.drain, amount, drainSpeed * Time.deltaTime);
        }
        else
        {
            fx.drain = amount; // hồi lại: phần bị mất theo ngay
        }

        fx.mat.SetFloat(FillId, amount);
        fx.mat.SetFloat(DrainFillId, fx.drain);
    }

    private void Refresh()
    {
        Apply(hpFill, hpGlow, hp);
        Apply(manaFill, manaGlow, mana);
        if (coinText != null)
            coinText.text = coins.ToString();
        if (expText != null)
            expText.text = exp.ToString();
    }

    // Vệt sáng đặt ở mép phải phần đã tô, pivot phải nên không tràn ra ngoài thanh.
    private static void Apply(Image fill, RectTransform glow, float amount)
    {
        if (fill == null) return;
        fill.fillAmount = amount;
        if (glow == null) return;
        var fillRect = fill.rectTransform;
        glow.anchoredPosition = new Vector2(fillRect.anchoredPosition.x + fillRect.rect.width * amount, glow.anchoredPosition.y);
        // Tắt Graphic thay vì SetActive: SetActive trong OnValidate làm Unity cảnh báo.
        var glowImage = glow.GetComponent<Graphic>();
        if (glowImage != null)
            glowImage.enabled = amount > 0.02f;
    }
}
