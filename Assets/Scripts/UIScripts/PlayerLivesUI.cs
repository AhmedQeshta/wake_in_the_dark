using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class PlayerLivesUI : MonoBehaviour
{
    // LIFE ICONS
    [Header("Life Icons")]
    [SerializeField] private Image[] lifeIcons;

    // FADE SETTINGS
    [Header("Life Fade")]
    [Tooltip("How long a lost life takes to fade away.")]
    [SerializeField, Min(0.01f)] private float fadeDuration = 0.25f;
    [SerializeField] private AnimationCurve fadeCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);


    // INTERNAL
    private Color[] originalColors;
    private Coroutine[] fadeRoutines;

    private int displayedLives;
    private bool initialized;


    // AWAKE
    private void Awake()
    {
        EnsureInitialized();
    }


    // INITIALIZE

    private void EnsureInitialized()
    {
        if (initialized)
            return;

        /*
         * PartyLivesManager has an early execution order and can call
         * UpdateLives() before this component's normal Awake().
         * Therefore initialization must also be safe when called
         * manually from UpdateLives().
         */

        int iconCount = lifeIcons != null ? lifeIcons.Length : 0;
        originalColors = new Color[iconCount];
        fadeRoutines = new Coroutine[iconCount];

        /*
         * At initialization all configured icons represent full lives.
         */
        displayedLives = iconCount;

        for (int i = 0; i < iconCount; i++)
        {
            Image icon = lifeIcons[i];
            if (icon == null)
                continue;
            originalColors[i] = icon.color;
            icon.gameObject.SetActive(true);
            SetIconAlphaInternal(i, 1f);
        }

        initialized = true;
    }


    // UPDATE LIVES
    public void UpdateLives(int currentLives)
    {
        EnsureInitialized();

        if (lifeIcons == null || lifeIcons.Length == 0)
            return;

        currentLives = Mathf.Clamp(currentLives, 0, lifeIcons.Length);


        // LOST LIVES
        if (currentLives < displayedLives)
            for (int i = currentLives; i < displayedLives; i++)
                FadeOutLife(i);
        // RESTORED LIVES
        else if (currentLives > displayedLives)
            for (int i = displayedLives; i < currentLives; i++)
                ShowLifeImmediately(i);


        displayedLives = currentLives;
    }


    // FORCE REFRESH
    public void SetLivesImmediately(int currentLives)
    {
        EnsureInitialized();

        if (lifeIcons == null)
            return;

        currentLives = Mathf.Clamp(currentLives, 0, lifeIcons.Length);


        for (int i = 0; i < lifeIcons.Length; i++)
        {
            if (i < currentLives)
                ShowLifeImmediately(i);
            else
                HideLifeImmediately(i);
        }


        displayedLives = currentLives;
    }


    // FADE OUT
    private void FadeOutLife(int index)
    {
        EnsureInitialized();

        if (!TryGetLifeIcon(index, out Image icon))
            return;

        StopFadeRoutine(index);

        icon.gameObject.SetActive(true);

        fadeRoutines[index] = StartCoroutine(FadeOutRoutine(index));
    }


    private IEnumerator FadeOutRoutine(int index)
    {
        if (!TryGetLifeIcon(index, out Image icon))
            yield break;


        float startAlpha = icon.color.a;
        float elapsed = 0f;


        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;

            float normalizedTime = Mathf.Clamp01(elapsed / fadeDuration);
            float curveValue = fadeCurve != null && fadeCurve.length > 0 ? fadeCurve.Evaluate(normalizedTime) : normalizedTime;
            float alpha = Mathf.Lerp(startAlpha, 0f, curveValue);

            SetIconAlpha(index, alpha);
            yield return null;
        }


        SetIconAlpha(index, 0f);

        icon.gameObject.SetActive(false);

        if (fadeRoutines != null && index < fadeRoutines.Length)
            fadeRoutines[index] = null;
    }


    // SHOW LIFE
    private void ShowLifeImmediately(int index)
    {
        EnsureInitialized();

        if (!TryGetLifeIcon(index, out Image icon))
            return;

        StopFadeRoutine(index);

        icon.gameObject.SetActive(true);

        SetIconAlpha(index, 1f);
    }


    // HIDE LIFE

    private void HideLifeImmediately(int index)
    {
        EnsureInitialized();

        if (!TryGetLifeIcon(index, out Image icon))
            return;


        StopFadeRoutine(index);

        SetIconAlpha(index, 0f);

        icon.gameObject.SetActive(false);
    }


    // SET ICON ALPHA

    private void SetIconAlpha(int index, float normalizedAlpha)
    {
        EnsureInitialized();

        SetIconAlphaInternal(index, normalizedAlpha);
    }


    private void SetIconAlphaInternal(int index, float normalizedAlpha)
    {
        if (!TryGetLifeIcon(index, out Image icon))
            return;

        Color color;
        if (originalColors != null && index < originalColors.Length)
            color = originalColors[index];
        else
            color = icon.color;


        color.a *= Mathf.Clamp01(normalizedAlpha);
        icon.color = color;
    }


    // VALID INDEX
    private bool IsValidIndex(int index)
    {
        return lifeIcons != null && index >= 0 && index < lifeIcons.Length;
    }


    // VALIDATE
    private void OnValidate()
    {
        fadeDuration = Mathf.Max(0.01f, fadeDuration);

        if (fadeCurve == null || fadeCurve.length == 0)
            fadeCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    }

    // GET LIFE ICON
    private bool TryGetLifeIcon(int index, out Image icon)
    {
        icon = null;

        if (!IsValidIndex(index))
            return false;

        icon = lifeIcons[index];

        return icon != null;
    }


    // STOP FADE ROUTINE
    private void StopFadeRoutine(int index)
    {
        // Ensure the fade routines array is initialized
        if (fadeRoutines == null || index < 0 || index >= fadeRoutines.Length || fadeRoutines[index] == null)
            return;

        StopCoroutine(fadeRoutines[index]);

        fadeRoutines[index] = null;
    }
}