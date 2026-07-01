using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// Add this to a single empty GameObject in your first scene.
// It survives scene loads and handles all fade-to/from-black transitions.
public class ScreenFader : MonoBehaviour
{
    public static ScreenFader Instance { get; private set; }

    private Image fadeImage;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        BuildCanvas();
    }

    void BuildCanvas()
    {
        Canvas canvas        = gameObject.AddComponent<Canvas>();
        canvas.renderMode    = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder  = 999;
        gameObject.AddComponent<CanvasScaler>();
        gameObject.AddComponent<GraphicRaycaster>();

        GameObject img = new GameObject("FadePanel");
        img.transform.SetParent(transform, false);

        RectTransform rt = img.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.sizeDelta = Vector2.zero;

        fadeImage       = img.AddComponent<Image>();
        fadeImage.color = new Color(0f, 0f, 0f, 0f);
        img.AddComponent<CanvasGroup>().blocksRaycasts = false;
    }

    // ── Public API ──────────────────────────────────────────────────────────

    public IEnumerator FadeToBlack(float duration)   => Fade(0f, 1f, duration);
    public IEnumerator FadeFromBlack(float duration)  => Fade(1f, 0f, duration);

    // Call at the start of a destination scene to fade in from solid black.
    public void StartWithFadeIn(float duration)
    {
        SetAlpha(1f);
        StartCoroutine(FadeFromBlack(duration));
    }

    public void SetAlpha(float a)
    {
        Color c = fadeImage.color;
        c.a = a;
        fadeImage.color = c;
    }

    // ── Internal ────────────────────────────────────────────────────────────

    IEnumerator Fade(float from, float to, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            SetAlpha(Mathf.Lerp(from, to, elapsed / duration));
            yield return null;
        }
        SetAlpha(to);
    }
}
