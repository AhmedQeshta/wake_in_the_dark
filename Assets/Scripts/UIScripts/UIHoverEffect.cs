using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class UIHoverEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
{
    [Header("Target")]
    [Tooltip("RectTransform that scales on hover. Leave empty to use this GameObject.")]
    [SerializeField] private RectTransform hoverTarget;

    [Header("Scale")]
    [SerializeField, Min(1f)] private float hoverScale = 1.06f;
    [SerializeField, Min(0f)] private float hoverInDuration = 0.08f;
    [SerializeField, Min(0f)] private float hoverOutDuration = 0.10f;
    [SerializeField] private AnimationCurve scaleCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Options")]
    [Tooltip("Optional Button/Selectable used to decide whether this UI is interactable. For a level card, assign its BTN here. Leave empty on normal buttons.")]
    [SerializeField] private Selectable interactableSource;

    [Tooltip("If the assigned Button/Selectable is locked, do not hover.")]
    [SerializeField] private bool ignoreWhenNotInteractable = true;

    [SerializeField] private bool animateSelection = true;

    private Selectable selectable;
    private Vector3 normalScale;
    private Coroutine scaleRoutine;
    private bool pointerInside;
    private bool selected;

    private void Awake()
    {
        ResolveReferences();
        CacheNormalScale();
    }

    private void OnEnable()
    {
        ResolveReferences();
        CacheNormalScale();
        ApplyNormalScaleImmediate();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        pointerInside = true;
        RefreshHoverState();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        pointerInside = false;
        RefreshHoverState();
    }

    public void OnSelect(BaseEventData eventData)
    {
        if (!animateSelection)
            return;

        selected = true;
        RefreshHoverState();
    }

    public void OnDeselect(BaseEventData eventData)
    {
        if (!animateSelection)
            return;

        selected = false;
        RefreshHoverState();
    }

    private void RefreshHoverState()
    {
        bool wantsHover = pointerInside || (animateSelection && selected);

        if (wantsHover && CanHover())
        {
            AnimateScale(normalScale * hoverScale, hoverInDuration);
            return;
        }

        AnimateScale(normalScale, hoverOutDuration);
    }

    private bool CanHover()
    {
        if (!ignoreWhenNotInteractable || selectable == null)
            return true;

        return selectable.IsInteractable();
    }

    private void AnimateScale(Vector3 targetScale, float duration)
    {
        if (hoverTarget == null)
            return;

        if (scaleRoutine != null)
            StopCoroutine(scaleRoutine);

        if (duration <= 0f)
        {
            hoverTarget.localScale = targetScale;
            scaleRoutine = null;
            return;
        }

        scaleRoutine = StartCoroutine(ScaleRoutine(targetScale, duration));
    }

    private IEnumerator ScaleRoutine(Vector3 targetScale, float duration)
    {
        Vector3 startScale = hoverTarget.localScale;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float curvedT = scaleCurve != null && scaleCurve.length > 0 ? scaleCurve.Evaluate(t) : t;
            hoverTarget.localScale = Vector3.LerpUnclamped(startScale, targetScale, curvedT);
            yield return null;
        }

        hoverTarget.localScale = targetScale;
        scaleRoutine = null;
    }

    private void ResolveReferences()
    {
        if (hoverTarget == null)
            hoverTarget = transform as RectTransform;

        if (interactableSource != null)
        {
            selectable = interactableSource;
            return;
        }

        if (selectable == null)
            selectable = GetComponent<Selectable>();
    }

    private void CacheNormalScale()
    {
        if (hoverTarget == null)
            return;

        if (!pointerInside && !selected)
            normalScale = hoverTarget.localScale;

        if (normalScale == Vector3.zero)
            normalScale = Vector3.one;
    }

    private void ApplyNormalScaleImmediate()
    {
        if (hoverTarget == null)
            return;

        if (scaleRoutine != null)
        {
            StopCoroutine(scaleRoutine);
            scaleRoutine = null;
        }

        pointerInside = false;
        selected = false;
        hoverTarget.localScale = normalScale;
    }

    private void OnDisable()
    {
        ApplyNormalScaleImmediate();
    }

    private void OnValidate()
    {
        hoverScale = Mathf.Max(1f, hoverScale);
        hoverInDuration = Mathf.Max(0f, hoverInDuration);
        hoverOutDuration = Mathf.Max(0f, hoverOutDuration);

        if (scaleCurve == null || scaleCurve.length == 0)
            scaleCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        if (hoverTarget == null)
            hoverTarget = transform as RectTransform;

        if (interactableSource == null)
            interactableSource = GetComponent<Selectable>();
    }
}
