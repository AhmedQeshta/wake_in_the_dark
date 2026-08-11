using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelLoader : MonoBehaviour
{
    // ==================================================
    // SINGLETON
    // ==================================================

    private static LevelLoader instance;


    // ==================================================
    // TRANSITION
    // ==================================================

    [Header("Scene Fade")]

    [SerializeField]
    private CanvasGroup fadeGroup;

    [SerializeField, Min(0.01f)]
    private float fadeOutDuration = 0.45f;

    [SerializeField, Min(0.01f)]
    private float fadeInDuration = 0.45f;

    [SerializeField]
    private AnimationCurve fadeCurve =
        AnimationCurve.EaseInOut(
            0f,
            0f,
            1f,
            1f
        );


    // ==================================================
    // STATE
    // ==================================================

    private bool isLoading;


    public bool IsLoading => isLoading;


    // ==================================================
    // AWAKE
    // ==================================================

    private void Awake()
    {
        // ----------------------------------------------
        // SINGLETON
        // ----------------------------------------------

        if (instance != null &&
            instance != this)
        {
            Destroy(gameObject);
            return;
        }


        instance = this;


        /*
         * This object survives scene loading.
         *
         * TransitionCanvas and FadeImage are children,
         * so they survive too.
         */
        DontDestroyOnLoad(
            gameObject
        );


        // ----------------------------------------------
        // INITIAL FADE STATE
        // ----------------------------------------------

        if (fadeGroup != null)
        {
            fadeGroup.alpha = 0f;
            fadeGroup.interactable = false;
            fadeGroup.blocksRaycasts = false;
        }
    }


    // ==================================================
    // LOAD LEVEL
    // ==================================================

    public void LoadLevel(
        string sceneName)
    {
        if (isLoading)
            return;


        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogError(
                "LevelLoader: Scene name is empty.",
                this
            );

            return;
        }


        if (!Application.CanStreamedLevelBeLoaded(
                sceneName
            ))
        {
            Debug.LogError(
                "LevelLoader: Scene '" +
                sceneName +
                "' cannot be loaded. " +
                "Check Build Profiles > Scene List.",
                this
            );

            return;
        }


        /*
         * A selected/next level should start
         * directly in gameplay instead of
         * reopening the Start menu.
         */
        UIManager.MarkGameplayStarted();


        StartCoroutine(
            LoadLevelRoutine(
                sceneName
            )
        );
    }


    // ==================================================
    // LOAD ROUTINE
    // ==================================================

    private IEnumerator LoadLevelRoutine(
        string sceneName)
    {
        isLoading = true;


        // ----------------------------------------------
        // BLOCK INPUT DURING TRANSITION
        // ----------------------------------------------

        if (fadeGroup != null)
        {
            fadeGroup.blocksRaycasts = true;
        }


        // ----------------------------------------------
        // FADE OLD SCENE TO BLACK
        // ----------------------------------------------

        yield return StartCoroutine(
            Fade(
                0f,
                1f,
                fadeOutDuration
            )
        );


        // ----------------------------------------------
        // RESTORE GLOBAL GAME STATE
        // ----------------------------------------------

        Time.timeScale = 1f;
        AudioListener.pause = false;


        // ----------------------------------------------
        // LOAD NEXT SCENE
        // ----------------------------------------------

        AsyncOperation operation =
            SceneManager.LoadSceneAsync(
                sceneName,
                LoadSceneMode.Single
            );


        if (operation == null)
        {
            Debug.LogError(
                "LevelLoader: LoadSceneAsync failed for: " +
                sceneName,
                this
            );


            yield return StartCoroutine(
                Fade(
                    1f,
                    0f,
                    fadeInDuration
                )
            );


            isLoading = false;
            yield break;
        }


        /*
         * Unity recommends asynchronous loading
         * for smoother scene transitions.
         */
        while (!operation.isDone)
        {
            yield return null;
        }


        /*
         * Give the new scene one frame to initialize:
         *
         * Player
         * UIManager
         * Camera
         * Lighting
         * Audio
         * etc.
         */
        yield return null;


        // ----------------------------------------------
        // FADE NEW SCENE IN
        // ----------------------------------------------

        yield return StartCoroutine(
            Fade(
                1f,
                0f,
                fadeInDuration
            )
        );


        // ----------------------------------------------
        // RESTORE INPUT
        // ----------------------------------------------

        if (fadeGroup != null)
        {
            fadeGroup.blocksRaycasts = false;
        }


        isLoading = false;
    }


    // ==================================================
    // FADE
    // ==================================================

    private IEnumerator Fade(
        float from,
        float to,
        float duration)
    {
        if (fadeGroup == null)
            yield break;


        if (duration <= 0f)
        {
            fadeGroup.alpha = to;
            yield break;
        }


        float elapsed = 0f;


        fadeGroup.alpha = from;


        while (elapsed < duration)
        {
            /*
             * Unscaled time means this still works
             * if a scene was loaded from a paused menu.
             */
            elapsed +=
                Time.unscaledDeltaTime;


            float normalized =
                Mathf.Clamp01(
                    elapsed /
                    duration
                );


            float curved =
                fadeCurve != null &&
                fadeCurve.length > 0
                    ? fadeCurve.Evaluate(
                        normalized
                    )
                    : normalized;


            fadeGroup.alpha =
                Mathf.Lerp(
                    from,
                    to,
                    curved
                );


            yield return null;
        }


        fadeGroup.alpha = to;
    }


    // ==================================================
    // VALIDATION
    // ==================================================

    private void OnValidate()
    {
        fadeOutDuration =
            Mathf.Max(
                0.01f,
                fadeOutDuration
            );


        fadeInDuration =
            Mathf.Max(
                0.01f,
                fadeInDuration
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