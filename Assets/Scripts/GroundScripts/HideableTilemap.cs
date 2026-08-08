using System.Collections;
using UnityEngine;
using UnityEngine.Tilemaps;

[RequireComponent(typeof(Tilemap))]
[RequireComponent(typeof(TilemapRenderer))]
[RequireComponent(typeof(TilemapCollider2D))]
public class HideableTilemap : MonoBehaviour
{
    [Header("Visibility")]
    [SerializeField] private bool startHidden;

    [Tooltip("Allow this Tilemap to become visible again.")]
    [SerializeField] private bool canReappear = true;

    [Header("Fade Effect")]
    [SerializeField, Min(0.01f)]
    private float fadeDuration = 0.5f;

    [SerializeField]
    private AnimationCurve fadeCurve =
        AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    private Tilemap tilemap;
    private TilemapRenderer tilemapRenderer;
    private TilemapCollider2D tilemapCollider;

    private Color originalColor;
    private float visibleAlpha;

    private Coroutine fadeCoroutine;

    private bool isHidden;
    private bool isTransitioning;

    public bool IsHidden => isHidden;
    public bool IsTransitioning => isTransitioning;

    private void Awake()
    {
        tilemap = GetComponent<Tilemap>();
        tilemapRenderer = GetComponent<TilemapRenderer>();
        tilemapCollider = GetComponent<TilemapCollider2D>();

        originalColor = tilemap.color;
        visibleAlpha = originalColor.a;

        if (startHidden)
        {
            ApplyHiddenStateImmediately();
        }
        else
        {
            ApplyVisibleStateImmediately();
        }
    }

    public bool Hide()
    {
        if (isHidden || isTransitioning)
            return false;

        StartVisibilityChange(false);
        return true;
    }

    public bool Show()
    {
        if (!isHidden || isTransitioning || !canReappear)
            return false;

        StartVisibilityChange(true);
        return true;
    }

    public bool Toggle()
    {
        return isHidden ? Show() : Hide();
    }

    private void StartVisibilityChange(bool makeVisible)
    {
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }

        fadeCoroutine = StartCoroutine(
            ChangeVisibility(makeVisible)
        );
    }

    private IEnumerator ChangeVisibility(bool makeVisible)
    {
        isTransitioning = true;

        if (makeVisible)
        {
            // The door is visible but not solid while fading in.
            tilemapRenderer.enabled = true;
            tilemapCollider.enabled = false;

            SetAlpha(0f);

            yield return FadeAlpha(
                0f,
                visibleAlpha
            );

            // Become solid only when fully visible.
            tilemapCollider.enabled = true;
            isHidden = false;
        }
        else
        {
            // Open the path as soon as the door starts fading.
            tilemapCollider.enabled = false;

            float currentAlpha = tilemap.color.a;

            yield return FadeAlpha(
                currentAlpha,
                0f
            );

            tilemapRenderer.enabled = false;
            isHidden = true;
        }

        isTransitioning = false;
        fadeCoroutine = null;
    }

    private IEnumerator FadeAlpha(
        float startAlpha,
        float targetAlpha)
    {
        float elapsedTime = 0f;

        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;

            float normalizedTime =
                Mathf.Clamp01(
                    elapsedTime / fadeDuration
                );

            float curvedTime =
                fadeCurve.Evaluate(normalizedTime);

            float alpha = Mathf.Lerp(
                startAlpha,
                targetAlpha,
                curvedTime
            );

            SetAlpha(alpha);

            yield return null;
        }

        SetAlpha(targetAlpha);
    }

    private void SetAlpha(float alpha)
    {
        Color color = originalColor;
        color.a = Mathf.Clamp01(alpha);

        tilemap.color = color;
    }

    private void ApplyHiddenStateImmediately()
    {
        SetAlpha(0f);

        tilemapRenderer.enabled = false;
        tilemapCollider.enabled = false;

        isHidden = true;
        isTransitioning = false;
    }

    private void ApplyVisibleStateImmediately()
    {
        SetAlpha(visibleAlpha);

        tilemapRenderer.enabled = true;
        tilemapCollider.enabled = true;

        isHidden = false;
        isTransitioning = false;
    }

    private void OnDisable()
    {
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
            fadeCoroutine = null;
        }

        isTransitioning = false;
    }

    private void OnValidate()
    {
        fadeDuration = Mathf.Max(
            0.01f,
            fadeDuration
        );

        if (fadeCurve == null ||
            fadeCurve.length == 0)
        {
            fadeCurve =
                AnimationCurve.EaseInOut(
                    0f,
                    0f,
                    1f,
                    1f
                );
        }
    }
}