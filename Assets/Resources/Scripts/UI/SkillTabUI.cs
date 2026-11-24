using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;

// Tab Kĩ năng: "Đang sử dụng" (Using) + "Kho kĩ năng" (Storage).
// - Trỏ tới ô nào thì hiện chi tiết ở SkillDetailUI.
// - Nhấn A ở ô Đang sử dụng: tháo kĩ năng ở ô đó (nếu có) xuống kho, focus chuyển xuống kho để chọn kĩ năng thay thế.
// - Đang chọn thay thế, nhấn A ở một kĩ năng trong kho: lắp kĩ năng đó vào ô vừa tháo. Nhấn B: hủy, quay lại ô đó.
// - Không chọn ô nào ở trên, nhấn A ở một kĩ năng trong kho: lắp vào ô trống đầu tiên (hết ô trống thì không làm gì).
// Kho chỉ chứa các kĩ năng chưa được lắp, xếp liền nhau từ ô đầu tiên.
public class SkillTabUI : MonoBehaviour, ITabContent
{
    public SkillDetailUI detail;
    [Tooltip("Để trống thì tự tìm con tên \"Using\" / \"Storage\".")]
    public Transform usingPanel;
    public Transform storagePanel;

    private SkillUI[] usingSlots;
    private SkillUI[] storageSlots;
    private readonly List<Skill> storage = new List<Skill>();

    private SkillUI pickTarget;   // Ô Đang sử dụng đang chờ chọn kĩ năng thay thế.
    private SkillUI highlighted;
    private SkillUI lastFocused;

    public bool IsPicking => pickTarget != null;

    private void Awake()
    {
        if (usingPanel == null)
            usingPanel = transform.Find("Using");
        if (storagePanel == null)
            storagePanel = transform.Find("Storage");
        if (detail == null)
            detail = GetComponentInChildren<SkillDetailUI>(true);

        usingSlots = usingPanel != null ? usingPanel.GetComponentsInChildren<SkillUI>(true) : new SkillUI[0];
        storageSlots = storagePanel != null ? storagePanel.GetComponentsInChildren<SkillUI>(true) : new SkillUI[0];

        storage.AddRange(storageSlots.Where(s => s.skill != null).Select(s => s.skill));

        // Chưa gán kĩ năng nào trong scene: đưa toàn bộ kĩ năng trong Resources/Data/Skills vào kho.
        if (storage.Count == 0 && usingSlots.All(s => s.skill == null))
            storage.AddRange(Resources.LoadAll<Skill>("Data/Skills").OrderBy(s => s.title));

        foreach (var slot in usingSlots.Concat(storageSlots))
        {
            slot.Selected += OnSlotSelected;
            slot.Submitted += OnSlotSubmitted;
        }
    }

    // Start (không phải Awake): Refresh của các ô cần Selectable được tạo trong Awake của chính ô đó.
    private void Start()
    {
        RenderStorage();
        detail?.Clear();
    }

    private void OnDisable()
    {
        // Rời tab giữa chừng thì hủy thao tác chọn.
        if (IsPicking)
            EndPick(false);
    }

    private void OnSlotSelected(SlotUI slot)
    {
        var skillSlot = (SkillUI)slot;
        lastFocused = skillSlot;
        if (highlighted != null && highlighted != skillSlot && highlighted != pickTarget)
            highlighted.SetHighlighted(false);
        highlighted = skillSlot;
        highlighted.SetHighlighted(true);
        detail?.Show(skillSlot.skill);
    }

    private void OnSlotSubmitted(SlotUI slot)
    {
        var skillSlot = (SkillUI)slot;
        bool isUsing = usingSlots.Contains(skillSlot);

        if (!IsPicking && isUsing)
            BeginPick(skillSlot);
        else if (IsPicking && !isUsing)
        {
            // Kĩ năng đã lắp ở ô khác: bỏ qua, vẫn ở chế độ chọn để chọn kĩ năng khác.
            if (IsEquipped(skillSlot.skill))
                return;
            if (skillSlot.skill != null)
            {
                storage.Remove(skillSlot.skill);
                pickTarget.skill = skillSlot.skill;
            }
            EndPick(true);
        }
        else if (!IsPicking && !isUsing && skillSlot.skill != null)
            EquipToFirstEmpty(skillSlot);
    }

    // Chưa chọn ô Đang sử dụng nào: lắp vào ô trống (không khóa) đầu tiên; hết ô trống thì không làm gì.
    // Ô vừa lắp trong kho bị khóa tương tác, nên focus chuyển sang ô kho kế tiếp còn chọn được (hết thì về ô vừa lắp).
    private void EquipToFirstEmpty(SkillUI from)
    {
        var skill = from.skill;
        var emptySlot = usingSlots.FirstOrDefault(s => !s.isLocked && s.skill == null);
        if (emptySlot == null || IsEquipped(skill))
            return;
        storage.Remove(skill);
        emptySlot.skill = skill;
        emptySlot.Refresh();
        RenderStorage();

        int start = System.Array.IndexOf(storageSlots, from);
        var next = Enumerable.Range(1, storageSlots.Length)
            .Select(i => storageSlots[(start + i) % storageSlots.Length])
            .FirstOrDefault(s => s.skill != null && s.CanSelect);
        Focus(next != null ? next : emptySlot);
    }

    // Mỗi kĩ năng chỉ được lắp ở một ô Đang sử dụng.
    private bool IsEquipped(Skill skill) => skill != null && usingSlots.Any(s => s.skill == skill);

    private void BeginPick(SkillUI target)
    {
        if (target.skill != null)
        {
            storage.Add(target.skill);
            target.skill = null;
            target.Refresh();
        }
        pickTarget = target;
        pickTarget.SetHighlighted(true);
        detail?.Clear();
        RenderStorage();

        // Chỉ cho điều hướng trong kho cho tới khi chọn xong.
        foreach (var s in usingSlots)
            s.SetNavigable(false);

        var first = storageSlots.FirstOrDefault(s => s.skill != null && s.CanSelect) ?? storageSlots.FirstOrDefault(s => s.CanSelect);
        if (first == null)
        {
            EndPick(true);
            return;
        }
        Focus(first);
    }

    private void EndPick(bool focusTarget)
    {
        var target = pickTarget;
        pickTarget = null;
        foreach (var s in usingSlots)
            s.Refresh();
        target.Refresh();
        RenderStorage();

        if (highlighted != null && highlighted != target)
            highlighted.SetHighlighted(false);
        highlighted = target;
        target.SetHighlighted(true);
        lastFocused = target;

        if (focusTarget)
            Focus(target);
    }

    private void RenderStorage()
    {
        for (int i = 0; i < storageSlots.Length; i++)
        {
            //storageSlots[i].skill = i < storage.Count ? storage[i] : null;
            // Kĩ năng đã lắp vẫn nằm trong kho nhưng đổi sang icon xám và không tương tác được.
            storageSlots[i].SetEquipped(IsEquipped(storageSlots[i].skill));
        }
    }

    private static void Focus(SlotUI slot)
    {
        if (EventSystem.current == null)
            return;
        // Đặt null trước để OnSelect luôn được gọi lại, kể cả khi ô đã được chọn từ trước.
        EventSystem.current.SetSelectedGameObject(null);
        EventSystem.current.SetSelectedGameObject(slot.gameObject);
    }

    public GameObject GetFocusTarget()
    {
        if (usingSlots == null)
            return null; // Chưa Awake.
        if (lastFocused != null && lastFocused.CanSelect)
            return lastFocused.gameObject;
        var first = usingSlots.FirstOrDefault(s => s.CanSelect) ?? storageSlots.FirstOrDefault(s => s.CanSelect);
        return first != null ? first.gameObject : null;
    }

    public bool HandleBack()
    {
        if (!IsPicking)
            return false;
        EndPick(true);
        return true;
    }
}
