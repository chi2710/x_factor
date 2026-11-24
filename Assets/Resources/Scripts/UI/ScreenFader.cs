using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// Tấm đen phủ toàn màn hình: tối dần, gọi atBlack lúc đen hẳn, rồi sáng lại.
// Dùng thời gian thực (unscaled) để vẫn chạy khi game đang dừng.
[RequireComponent(typeof(Image))]
public class ScreenFader : MonoBehaviour
{
    private Image overlay;

    public bool IsFading { get; private set; }

    private void Awake()
    {
        overlay = GetComponent<Image>();
        overlay.raycastTarget = false;
        SetAlpha(0f);
    }

    // duration là tổng thời gian: nửa đầu tối dần, nửa sau sáng lại.
    public void FadeOutIn(float duration, Action atBlack)
    {
        if (IsFading)
            return;
        StartCoroutine(Run(duration * 0.5f, atBlack));
    }

    private IEnumerator Run(float half, Action atBlack)
    {
        IsFading = true;
        yield return Fade(0f, 1f, half);
        atBlack?.Invoke();
        yield return Fade(1f, 0f, half);
        IsFading = false;
    }

    private IEnumerator Fade(float from, float to, float time)
    {
        for (float t = 0f; t < time; t += Time.unscaledDeltaTime)
        {
            SetAlpha(Mathf.Lerp(from, to, t / time));
            yield return null;
        }
        SetAlpha(to);
    }

    private void SetAlpha(float alpha)
    {
        var c = overlay.color;
        c.a = alpha;
        overlay.color = c;
    }
}
