using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class PlayerLivesUI : MonoBehaviour
{
    // ==================================================
    // LIFE ICONS
    // ==================================================

    [Header("Life Icons")]
    [SerializeField]
    private Image[] lifeIcons;


    // ==================================================
    // FADE SETTINGS
    // ==================================================

    [Header("Life Fade")]

    [Tooltip("How long a lost life takes to fade away.")]
    [SerializeField, Min(0.01f)]
    private float fadeDuration = 0.4f;


    [SerializeField]
    private AnimationCurve fadeCurve =
        AnimationCurve.EaseInOut(
            0f,
            0f,
            1f,
            1f
        );


    // ==================================================
    // INTERNAL
    // ==================================================

    private Color[] originalColors;
    private Coroutine[] fadeRoutines;

    private int displayedLives;


    // ==================================================
    // AWAKE
    // ==================================================

    private void Awake()
    {
        if (lifeIcons == null)
            return;


        originalColors =
            new Color[lifeIcons.Length];


        fadeRoutines =
            new Coroutine[lifeIcons.Length];


        /*
         * At scene start all configured life icons
         * begin visible.
         */
        displayedLives =
            lifeIcons.Length;


        for (int i = 0;
             i < lifeIcons.Length;
             i++)
        {
            Image icon =
                lifeIcons[i];


            if (icon == null)
                continue;


            originalColors[i] =
                icon.color;


            icon.gameObject.SetActive(
                true
            );


            SetIconAlpha(
                i,
                1f
            );
        }
    }


    // ==================================================
    // UPDATE LIVES
    // ==================================================

    public void UpdateLives(
        int currentLives)
    {
        if (lifeIcons == null)
            return;


        currentLives =
            Mathf.Clamp(
                currentLives,
                0,
                lifeIcons.Length
            );


        /*
         * Example:
         *
         * displayedLives = 3
         * currentLives   = 2
         *
         * Fade Life_03 only.
         */
        if (currentLives < displayedLives)
        {
            for (int i = currentLives;
                 i < displayedLives;
                 i++)
            {
                FadeOutLife(
                    i
                );
            }
        }


        /*
         * If lives are ever restored,
         * show those icons again.
         */
        else if (currentLives > displayedLives)
        {
            for (int i = displayedLives;
                 i < currentLives;
                 i++)
            {
                ShowLifeImmediately(
                    i
                );
            }
        }


        displayedLives =
            currentLives;
    }


    // ==================================================
    // FADE OUT
    // ==================================================

    private void FadeOutLife(
        int index)
    {
        if (!IsValidIndex(index))
            return;


        Image icon =
            lifeIcons[index];


        if (icon == null)
            return;


        /*
         * Stop an old fade on this icon if one
         * is somehow already running.
         */
        if (fadeRoutines[index] != null)
        {
            StopCoroutine(
                fadeRoutines[index]
            );
        }


        /*
         * Make sure it's active while fading.
         */
        icon.gameObject.SetActive(
            true
        );


        fadeRoutines[index] =
            StartCoroutine(
                FadeOutRoutine(index)
            );
    }


    private IEnumerator FadeOutRoutine(
        int index)
    {
        Image icon =
            lifeIcons[index];


        if (icon == null)
            yield break;


        float startAlpha =
            icon.color.a;


        float elapsed =
            0f;


        while (elapsed < fadeDuration)
        {
            elapsed +=
                Time.unscaledDeltaTime;


            float normalizedTime =
                Mathf.Clamp01(
                    elapsed /
                    fadeDuration
                );


            float curveValue =
                fadeCurve != null &&
                fadeCurve.length > 0
                    ? fadeCurve.Evaluate(
                        normalizedTime
                    )
                    : normalizedTime;


            float alpha =
                Mathf.Lerp(
                    startAlpha,
                    0f,
                    curveValue
                );


            SetIconAlpha(
                index,
                alpha
            );


            yield return null;
        }


        // Completely invisible.
        SetIconAlpha(
            index,
            0f
        );


        /*
         * Disable only AFTER fade finishes.
         */
        icon.gameObject.SetActive(
            false
        );


        fadeRoutines[index] =
            null;
    }


    // ==================================================
    // SHOW LIFE
    // ==================================================

    private void ShowLifeImmediately(
        int index)
    {
        if (!IsValidIndex(index))
            return;


        Image icon =
            lifeIcons[index];


        if (icon == null)
            return;


        if (fadeRoutines[index] != null)
        {
            StopCoroutine(
                fadeRoutines[index]
            );

            fadeRoutines[index] =
                null;
        }


        icon.gameObject.SetActive(
            true
        );


        SetIconAlpha(
            index,
            1f
        );
    }


    // ==================================================
    // SET ICON ALPHA
    // ==================================================

    private void SetIconAlpha(
        int index,
        float normalizedAlpha)
    {
        if (!IsValidIndex(index))
            return;


        Image icon =
            lifeIcons[index];


        if (icon == null)
            return;


        Color color;


        if (originalColors != null &&
            index < originalColors.Length)
        {
            color =
                originalColors[index];
        }
        else
        {
            color =
                icon.color;
        }


        color.a *=
            Mathf.Clamp01(
                normalizedAlpha
            );


        icon.color =
            color;
    }


    // ==================================================
    // VALIDATION
    // ==================================================

    private bool IsValidIndex(
        int index)
    {
        return
            lifeIcons != null &&
            index >= 0 &&
            index < lifeIcons.Length;
    }


    private void OnValidate()
    {
        fadeDuration =
            Mathf.Max(
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