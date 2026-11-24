using UnityEngine;

// Item đặt ngoài level: Player chạm vào là nhặt, item vào Inventory (popup do ItemPickupPopup hiện).
[RequireComponent(typeof(Collider2D))]
public class ItemPickup : MonoBehaviour
{
    public Item item;

    [Header("Bob")]
    [Tooltip("Biên độ nhấp nhô lên xuống (đơn vị world).")]
    [SerializeField] private float bobHeight = 0.1f;
    [Tooltip("Thời gian một chu kỳ lên xuống (giây).")]
    [SerializeField] private float bobPeriod = 1.5f;

    private Vector3 basePosition;

    private void Start()
    {
        basePosition = transform.localPosition;
    }

    private void Update()
    {
        if (bobPeriod <= 0f)
            return;
        float y = Mathf.Sin(Time.time * 2f * Mathf.PI / bobPeriod) * bobHeight;
        transform.localPosition = basePosition + new Vector3(0f, y, 0f);
    }

    private void Reset()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponentInParent<Player>() == null)
            return;
        Inventory.Add(item);
        gameObject.SetActive(false);
    }
}
