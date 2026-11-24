using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Base cho mọi ô có thể chọn (vật phẩm, nhiệm vụ). Dùng Selectable của Unity nên
// chuột (click) và tay cầm (D-pad / stick) đều đi qua cùng một luồng OnSelect.
public abstract class SlotUI : MonoBehaviour, ISelectHandler, ISubmitHandler, IPointerClickHandler
{
    public event Action<SlotUI> Selected;
    // Nút A (tay cầm) / Enter, hoặc click chuột.
    public event Action<SlotUI> Submitted;

    public abstract Item Data { get; }
    public virtual bool CanSelect => Data != null;

    protected Selectable selectable;
    private ScrollRect scrollRect;

    protected virtual void Awake()
    {
        selectable = GetComponent<Selectable>();
        if (selectable == null)
            selectable = gameObject.AddComponent<Selectable>();
        selectable.transition = Selectable.Transition.None;
        scrollRect = GetComponentInParent<ScrollRect>(true);
        // Khởi tạo trong Awake (không phải Start) để TabMenuUI.Start có thể focus ô ngay frame đầu.
        Refresh();
        SetHighlighted(false);
    }

    public virtual void Refresh()
    {
        selectable.interactable = CanSelect;
    }

    public void Select()
    {
        if (CanSelect)
            EventSystem.current.SetSelectedGameObject(gameObject);
    }

    public void OnSelect(BaseEventData eventData)
    {
        if (!CanSelect)
            return;
        ScrollIntoView();
        Selected?.Invoke(this);
    }

    public void OnSubmit(BaseEventData eventData)
    {
        if (selectable.IsInteractable())
            Submitted?.Invoke(this);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left && selectable.IsInteractable())
            Submitted?.Invoke(this);
    }

    // Tạm chặn/cho phép điều hướng tới ô (vẫn tôn trọng CanSelect).
    public void SetNavigable(bool navigable)
    {
        selectable.interactable = navigable && CanSelect;
    }

    public abstract void SetHighlighted(bool highlighted);

    // Khi điều hướng bằng tay cầm tới ô nằm ngoài vùng nhìn thấy, cuộn ScrollRect cho ô hiện ra.
    private void ScrollIntoView()
    {
        if (scrollRect == null || scrollRect.content == null || !scrollRect.vertical)
            return;

        RectTransform viewport = scrollRect.viewport != null ? scrollRect.viewport : (RectTransform)scrollRect.transform;
        RectTransform content = scrollRect.content;
        RectTransform self = (RectTransform)transform;

        Bounds slot = RectTransformUtility.CalculateRelativeRectTransformBounds(viewport, self);
        Rect view = viewport.rect;
        float offset = 0f;
        if (slot.max.y > view.yMax)
            offset = slot.max.y - view.yMax;
        else if (slot.min.y < view.yMin)
            offset = slot.min.y - view.yMin;

        if (!Mathf.Approximately(offset, 0f))
            content.anchoredPosition -= new Vector2(0f, offset);
    }
}
