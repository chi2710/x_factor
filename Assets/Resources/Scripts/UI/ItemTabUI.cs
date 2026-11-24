using System.Linq;
using TMPro;
using UnityEngine;

// Tab Vật phẩm: danh sách nhiệm vụ + 2 lưới vật phẩm.
// Nhiệm vụ và vật phẩm được chọn độc lập: chọn vật phẩm không bỏ chọn nhiệm vụ và ngược lại.
// Nhiệm vụ đang chọn hiện ở text MissionDetail, vật phẩm đang chọn hiện ở ItemDetailUI.
public class ItemTabUI : MonoBehaviour, ITabContent
{
    public ItemDetailUI detail;
    [Tooltip("Để trống thì tự tìm TextMeshPro tên \"MissionDetail\" trong tab.")]
    public TextMeshProUGUI missionDetail;
    [Tooltip("Lưới nhận item nhặt ngoài level (Inventory): item mới vào ô trống, chưa khóa, đầu tiên.")]
    public Transform pickupSlots;

    private SlotUI[] slots;
    private SlotUI currentMission;
    private SlotUI currentItem;
    private SlotUI lastFocused;

    private void Awake()
    {
        if (missionDetail == null)
            missionDetail = GetComponentsInChildren<TextMeshProUGUI>(true).FirstOrDefault(t => t.name == "MissionDetail");

        slots = GetComponentsInChildren<SlotUI>(true);
        foreach (var slot in slots)
            slot.Selected += OnSlotSelected;
        detail.clear();
    }

    // Tab chỉ bật khi mở menu, nên mỗi lần bật thì đưa các item mới nhặt vào ô.
    private void OnEnable()
    {
        if (pickupSlots == null)
            return;
        var gridSlots = pickupSlots.GetComponentsInChildren<ItemUI>(true);
        foreach (var item in Inventory.Items)
        {
            if (System.Array.Exists(gridSlots, s => s.item == item))
                continue;
            var empty = System.Array.Find(gridSlots, s => !s.isLocked && s.item == null);
            if (empty == null)
                return;
            empty.item = item;
            empty.Refresh();
        }
    }

    // Start (không phải Awake) để chạy sau Awake của các ô, vốn reset highlight về false.
    private void Start()
    {
        if (currentMission != null)
            return;
        // Mặc định chọn sẵn nhiệm vụ đầu tiên (chỉ highlight, không lấy focus của tay cầm).
        var firstMission = slots.FirstOrDefault(s => s is MissionUI && s.Data != null);
        if (firstMission != null)
            ShowMission(firstMission);
        else
            SetMissionText("");
    }

    private void OnSlotSelected(SlotUI slot)
    {
        lastFocused = slot;
        if (slot is MissionUI mission)
        {
            mission.KeepDownInList();
            ShowMission(slot);
        }
        else
            ShowItem(slot);
    }

    private void ShowMission(SlotUI slot)
    {
        if (currentMission != null && currentMission != slot)
            currentMission.SetHighlighted(false);
        currentMission = slot;
        currentMission.SetHighlighted(true);
        SetMissionText(slot.Data != null ? slot.Data.description : "");
    }

    private void ShowItem(SlotUI slot)
    {
        if (currentItem != null && currentItem != slot)
            currentItem.SetHighlighted(false);
        currentItem = slot;
        currentItem.SetHighlighted(true);
        detail.showDetail(slot.Data);
    }

    private void SetMissionText(string text)
    {
        if (missionDetail != null)
            missionDetail.text = text;
    }

    // Khi quay lại tab, giữ ô đang được focus; lần đầu thì chọn ô vật phẩm đầu tiên có dữ liệu.
    public GameObject GetFocusTarget()
    {
        if (slots == null)
            return null; // Chưa Awake.
        if (lastFocused != null && lastFocused.CanSelect)
            return lastFocused.gameObject;
        // Ưu tiên ô có dữ liệu để lần đầu mở tab không rơi vào một ô trống.
        foreach (var slot in slots)
            if (!(slot is MissionUI) && slot.CanSelect && slot.Data != null)
                return slot.gameObject;
        foreach (var slot in slots)
            if (slot.CanSelect)
                return slot.gameObject;
        return null;
    }

    public bool HandleBack() => false;
}
