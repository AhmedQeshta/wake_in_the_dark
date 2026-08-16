using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelLoader : MonoBehaviour
{
    // ==================================================
    // INSTANCE
    // ==================================================

    public static LevelLoader Instance { get; private set; }


    // ==================================================
    // INITIAL LEVEL
    // ==================================================

    [Header("Initial Level")]

    [SerializeField]
    private string firstLevelSceneName =
        "Level_01";

    [SerializeField]
    private string levelScenePrefix =
        "Level_";


    // ==================================================
    // BOOTSTRAP CAMERA
    // ==================================================

    [Header("Bootstrap Camera")]

    [SerializeField]
    private Camera bootstrapCamera;


    // ==================================================
    // NORMAL FADE
    // ==================================================

    [Header("Normal Scene Fade")]

    [SerializeField]
    private CanvasGroup fadeGroup;

    [SerializeField, Min(0.01f)]
    private float fadeOutDuration =
        0.25f;

    [SerializeField, Min(0.01f)]
    private float fadeInDuration =
        0.30f;


    // ==================================================
    // FAST MENU TRANSITION
    // ==================================================

    [Header("Fast Level Menu Transition")]

    [SerializeField, Min(0.01f)]
    private float fastFadeOutDuration =
        0.10f;

    [SerializeField, Min(0.01f)]
    private float fastFadeInDuration =
        0.18f;


    // ==================================================
    // FADE CURVE
    // ==================================================

    [Header("Fade Curve")]

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

    private Scene bootstrapScene;

    private string currentLevelSceneName;

    private bool isLoading;


    // ==================================================
    // PUBLIC
    // ==================================================

    public bool IsLoading =>
        isLoading;


    public string CurrentLevelSceneName =>
        currentLevelSceneName;


    public event Action<string> LevelLoaded;


    // ==================================================
    // AWAKE
    // ==================================================

    private void Awake()
    {
        if (Instance != null &&
            Instance != this)
        {
            Destroy(gameObject);

            return;
        }


        Instance =
            this;


        bootstrapScene =
            gameObject.scene;


        SetBootstrapCamera(
            true
        );


        SetFadeImmediate(
            1f,
            true
        );
    }


    // ==================================================
    // START
    // ==================================================

    private IEnumerator Start()
    {
        Scene existingLevel =
            FindLoadedLevelScene();


        if (existingLevel.IsValid() &&
            existingLevel.isLoaded)
        {
            currentLevelSceneName =
                existingLevel.name;


            SceneManager.SetActiveScene(
                existingLevel
            );


            yield return StartCoroutine(
                PrepareLoadedLevel(
                    existingLevel,
                    true
                )
            );


            LevelLoaded?.Invoke(
                currentLevelSceneName
            );


            SetBootstrapCamera(
                false
            );


            yield return StartCoroutine(
                Fade(
                    1f,
                    0f,
                    fadeInDuration
                )
            );


            FinishFade();

            yield break;
        }


        yield return StartCoroutine(
            LoadInitialLevelRoutine()
        );
    }


    // ==================================================
    // INITIAL LEVEL
    // ==================================================

    private IEnumerator LoadInitialLevelRoutine()
    {
        if (!ValidateSceneName(
                firstLevelSceneName))
        {
            yield break;
        }


        isLoading =
            true;


        SetBootstrapCamera(
            true
        );


        AsyncOperation load =
            SceneManager.LoadSceneAsync(
                firstLevelSceneName,
                LoadSceneMode.Additive
            );


        if (load == null)
        {
            isLoading =
                false;

            yield break;
        }


        while (!load.isDone)
        {
            yield return null;
        }


        Scene level =
            SceneManager.GetSceneByName(
                firstLevelSceneName
            );


        if (!ValidateLoadedScene(
                level))
        {
            isLoading =
                false;

            yield break;
        }


        SceneManager.SetActiveScene(
            level
        );


        currentLevelSceneName =
            level.name;


        /*
         * Prepare IntroCamera,
         * but DON'T play it yet.
         *
         * Level 1 still has the initial
         * Start Menu open.
         */
        yield return StartCoroutine(
            PrepareLoadedLevel(
                level,
                true
            )
        );


        LevelLoaded?.Invoke(
            currentLevelSceneName
        );


        SetBootstrapCamera(
            false
        );


        yield return StartCoroutine(
            Fade(
                1f,
                0f,
                fadeInDuration
            )
        );


        FinishFade();


        isLoading =
            false;
    }


    // ==================================================
    // PLAY CURRENT LEVEL INTRO
    // ==================================================

    public void PlayCurrentLevelIntro()
    {
        StartCoroutine(
            PlayCurrentLevelIntroWhenReady()
        );
    }


    private IEnumerator PlayCurrentLevelIntroWhenReady()
    {
        /*
         * Bootstrap may still be loading Level_01
         * when the player presses Start.
         *
         * Wait instead of discarding the intro request.
         */
        while (isLoading)
        {
            yield return null;
        }


        if (string.IsNullOrWhiteSpace(
                currentLevelSceneName))
        {
            yield break;
        }


        Scene scene =
            SceneManager.GetSceneByName(
                currentLevelSceneName
            );


        if (!scene.IsValid() ||
            !scene.isLoaded)
        {
            yield break;
        }


        LevelCameraDirector director =
            FindComponentInScene<LevelCameraDirector>(
                scene
            );


        if (director == null ||
            !director.PlayOnLevelEnter)
        {
            yield break;
        }


        yield return StartCoroutine(
            director.PlayIntroRoutine()
        );
    }


    // ==================================================
    // LEVEL LOAD
    // ==================================================

    public void LoadLevel(
        string sceneName)
    {
        StartLevelChange(
            sceneName,
            false
        );
    }


    public void LoadLevelFromMenu(
        string sceneName)
    {
        StartLevelChange(
            sceneName,
            true
        );
    }


    private void StartLevelChange(
        string sceneName,
        bool fast)
    {
        if (isLoading)
            return;


        if (!ValidateSceneName(
                sceneName))
        {
            return;
        }


        if (string.Equals(
                sceneName,
                currentLevelSceneName,
                StringComparison.OrdinalIgnoreCase))
        {
            return;
        }


        UIManager.MarkGameplayStarted();


        if (UIManager.Instance != null)
        {
            UIManager.Instance
                .PrepareForLevelChange();
        }


        StartCoroutine(
            ChangeLevelRoutine(
                sceneName,
                fast
            )
        );
    }


    // ==================================================
    // CHANGE LEVEL
    // ==================================================

    private IEnumerator ChangeLevelRoutine(
        string newSceneName,
        bool fast)
    {
        isLoading =
            true;


        float outDuration =
            fast
                ? fastFadeOutDuration
                : fadeOutDuration;


        float inDuration =
            fast
                ? fastFadeInDuration
                : fadeInDuration;


        PrepareFade();


        yield return StartCoroutine(
            Fade(
                GetFadeAlpha(),
                1f,
                outDuration
            )
        );


        Time.timeScale =
            1f;


        AudioListener.pause =
            false;


        SetBootstrapCamera(
            true
        );


        string oldLevelName =
            currentLevelSceneName;


        if (bootstrapScene.IsValid() &&
            bootstrapScene.isLoaded)
        {
            SceneManager.SetActiveScene(
                bootstrapScene
            );
        }


        // ==================================================
        // UNLOAD OLD LEVEL
        // ==================================================

        if (!string.IsNullOrWhiteSpace(
                oldLevelName))
        {
            Scene oldLevel =
                SceneManager.GetSceneByName(
                    oldLevelName
                );


            if (oldLevel.IsValid() &&
                oldLevel.isLoaded)
            {
                AsyncOperation unload =
                    SceneManager.UnloadSceneAsync(
                        oldLevel
                    );


                if (unload != null)
                {
                    while (!unload.isDone)
                    {
                        yield return null;
                    }
                }
            }
        }


        // ==================================================
        // LOAD NEW LEVEL
        // ==================================================

        AsyncOperation load =
            SceneManager.LoadSceneAsync(
                newSceneName,
                LoadSceneMode.Additive
            );


        if (load == null)
        {
            HandleLoadFailure(
                newSceneName
            );

            yield break;
        }


        while (!load.isDone)
        {
            yield return null;
        }


        Scene newLevel =
            SceneManager.GetSceneByName(
                newSceneName
            );


        if (!ValidateLoadedScene(
                newLevel))
        {
            HandleLoadFailure(
                newSceneName
            );

            yield break;
        }


        SceneManager.SetActiveScene(
            newLevel
        );


        currentLevelSceneName =
            newLevel.name;


        // ==================================================
        // PREPARE NEW CAMERA
        // ==================================================

        yield return StartCoroutine(
            PrepareLoadedLevel(
                newLevel,
                true
            )
        );


        LevelLoaded?.Invoke(
            currentLevelSceneName
        );


        if (HasGameplayUnityCamera(
                newLevel))
        {
            SetBootstrapCamera(
                false
            );
        }


        // ==================================================
        // REVEAL WIDE INTRO
        // ==================================================

        yield return StartCoroutine(
            Fade(
                1f,
                0f,
                inDuration
            )
        );


        FinishFade();


        // ==================================================
        // WIDE → PLAYER
        // ==================================================

        LevelCameraDirector director =
            FindComponentInScene<LevelCameraDirector>(
                newLevel
            );


        if (director != null &&
            director.PlayOnLevelEnter)
        {
            yield return StartCoroutine(
                director.PlayIntroRoutine()
            );
        }


        isLoading =
            false;


        if (UIManager.Instance != null)
        {
            UIManager.Instance
                .OnLevelLoadFinished();
        }
    }


    // ==================================================
    // RELOAD
    // ==================================================

    public void ReloadCurrentLevel()
    {
        if (isLoading)
            return;


        if (string.IsNullOrWhiteSpace(
                currentLevelSceneName))
        {
            return;
        }


        UIManager.MarkGameplayStarted();


        if (UIManager.Instance != null)
        {
            UIManager.Instance
                .PrepareForLevelChange();
        }


        StartCoroutine(
            ReloadCurrentLevelRoutine()
        );
    }


    private IEnumerator ReloadCurrentLevelRoutine()
    {
        isLoading =
            true;


        string levelName =
            currentLevelSceneName;


        PrepareFade();


        yield return StartCoroutine(
            Fade(
                GetFadeAlpha(),
                1f,
                fadeOutDuration
            )
        );


        Time.timeScale =
            1f;


        AudioListener.pause =
            false;


        SetBootstrapCamera(
            true
        );


        if (bootstrapScene.IsValid() &&
            bootstrapScene.isLoaded)
        {
            SceneManager.SetActiveScene(
                bootstrapScene
            );
        }


        Scene oldLevel =
            SceneManager.GetSceneByName(
                levelName
            );


        if (oldLevel.IsValid() &&
            oldLevel.isLoaded)
        {
            AsyncOperation unload =
                SceneManager.UnloadSceneAsync(
                    oldLevel
                );


            if (unload != null)
            {
                while (!unload.isDone)
                {
                    yield return null;
                }
            }
        }


        AsyncOperation load =
            SceneManager.LoadSceneAsync(
                levelName,
                LoadSceneMode.Additive
            );


        if (load == null)
        {
            HandleLoadFailure(
                levelName
            );

            yield break;
        }


        while (!load.isDone)
        {
            yield return null;
        }


        Scene reloaded =
            SceneManager.GetSceneByName(
                levelName
            );


        if (!ValidateLoadedScene(
                reloaded))
        {
            HandleLoadFailure(
                levelName
            );

            yield break;
        }


        SceneManager.SetActiveScene(
            reloaded
        );


        currentLevelSceneName =
            reloaded.name;


        LevelCameraDirector director =
            FindComponentInScene<LevelCameraDirector>(
                reloaded
            );


        bool playIntro =
            director != null &&
            director.PlayOnReload;


        yield return StartCoroutine(
            PrepareLoadedLevel(
                reloaded,
                playIntro
            )
        );


        LevelLoaded?.Invoke(
            currentLevelSceneName
        );


        if (HasGameplayUnityCamera(
                reloaded))
        {
            SetBootstrapCamera(
                false
            );
        }


        yield return StartCoroutine(
            Fade(
                1f,
                0f,
                fadeInDuration
            )
        );


        FinishFade();


        if (playIntro &&
            director != null)
        {
            yield return StartCoroutine(
                director.PlayIntroRoutine()
            );
        }


        isLoading =
            false;


        if (UIManager.Instance != null)
        {
            UIManager.Instance
                .OnLevelLoadFinished();
        }
    }


    // ==================================================
    // PREPARE LEVEL
    // ==================================================
    private IEnumerator PrepareLoadedLevel(
        Scene scene,
        bool prepareIntro)
    {
        /*
         * Allow Awake / OnEnable to finish.
         *
         * This is important because level scenes are
         * loaded additively from Bootstrap.
         */
        yield return null;


        // ==================================================
        // OPTIONAL LEVEL CAMERA SETUP
        // ==================================================

        /*
         * Levels 1-3 have LevelCameraSetup because they use:
         *
         * Door -> Whole Map -> Gameplay.
         *
         * Levels 4-5 intentionally do not require it because
         * they use:
         *
         * Door -> Gameplay
         *
         * with Pixel Perfect Camera + Parallax.
         */

        LevelCameraSetup cameraSetup =
            FindComponentInScene<LevelCameraSetup>(
                scene
            );


        if (cameraSetup != null)
        {
            cameraSetup.BindScene();
        }


        // ==================================================
        // CAMERA DIRECTOR
        // ==================================================

        LevelCameraDirector director =
            FindComponentInScene<LevelCameraDirector>(
                scene
            );


        if (director != null)
        {
            if (prepareIntro &&
                director.PlayOnLevelEnter)
            {
                director.PrepareIntro();
            }
            else
            {
                director.SkipToGameplayImmediate();
            }
        }
        else if (cameraSetup == null)
        {
            /*
             * No custom camera system at all.
             *
             * This is not fatal because a level may simply
             * use a normal Unity Camera.
             */
            Debug.LogWarning(
                "LevelLoader: No LevelCameraDirector or " +
                "LevelCameraSetup found in " +
                scene.name +
                ". Using the level camera as-is.",
                this
            );
        }


        /*
         * Allow Cinemachine one frame
         * to apply priorities and tracking.
         */
        yield return null;
    }


    // ==================================================
    // FIND IN SCENE
    // ==================================================

    private T FindComponentInScene<T>(
        Scene scene)
        where T : Component
    {
        if (!scene.IsValid() ||
            !scene.isLoaded)
        {
            return null;
        }


        GameObject[] roots =
            scene.GetRootGameObjects();


        foreach (GameObject root in roots)
        {
            if (root == null)
                continue;


            T result =
                root.GetComponentInChildren<T>(
                    true
                );


            if (result != null)
            {
                return result;
            }
        }


        return null;
    }


    // ==================================================
    // CAMERA CHECK
    // ==================================================

    private bool HasGameplayUnityCamera(
        Scene scene)
    {
        Camera camera =
            FindComponentInScene<Camera>(
                scene
            );


        return
            camera != null &&
            camera.enabled &&
            camera.gameObject.activeInHierarchy;
    }


    // ==================================================
    // FIND LOADED LEVEL
    // ==================================================

    private Scene FindLoadedLevelScene()
    {
        for (int i = 0;
             i < SceneManager.sceneCount;
             i++)
        {
            Scene scene =
                SceneManager.GetSceneAt(i);


            if (!scene.IsValid() ||
                !scene.isLoaded)
            {
                continue;
            }


            if (scene.handle ==
                bootstrapScene.handle)
            {
                continue;
            }


            if (scene.name.StartsWith(
                    levelScenePrefix,
                    StringComparison.OrdinalIgnoreCase))
            {
                return scene;
            }
        }


        return default;
    }


    // ==================================================
    // VALIDATION
    // ==================================================

    private bool ValidateSceneName(
        string sceneName)
    {
        if (string.IsNullOrWhiteSpace(
                sceneName))
        {
            Debug.LogError(
                "LevelLoader: Empty scene name.",
                this
            );

            return false;
        }


        if (!Application.CanStreamedLevelBeLoaded(
                sceneName))
        {
            Debug.LogError(
                "Scene '" +
                sceneName +
                "' is not available in Build Profiles.",
                this
            );

            return false;
        }


        return true;
    }


    private bool ValidateLoadedScene(
        Scene scene)
    {
        return
            scene.IsValid() &&
            scene.isLoaded;
    }


    private void HandleLoadFailure(
        string sceneName)
    {
        Debug.LogError(
            "Failed loading " +
            sceneName,
            this
        );


        SetBootstrapCamera(
            true
        );


        FinishFade();


        isLoading =
            false;


        if (UIManager.Instance != null)
        {
            UIManager.Instance
                .OnLevelLoadFinished();
        }
    }


    // ==================================================
    // BOOTSTRAP CAMERA
    // ==================================================

    private void SetBootstrapCamera(
        bool state)
    {
        if (bootstrapCamera == null)
            return;


        // Enable/disable the Bootstrap Camera itself.
        bootstrapCamera.enabled =
            state;


        // Keep exactly one AudioListener active.
        //
        // When the Bootstrap camera is disabled because a
        // level camera has taken over, its AudioListener must
        // also be disabled. Otherwise Unity reports:
        // "There are 2 audio listeners in the scene."
        AudioListener listener =
            bootstrapCamera.GetComponent<AudioListener>();


        if (listener != null)
        {
            listener.enabled =
                state;
        }


        bootstrapCamera.rect =
            new Rect(
                0f,
                0f,
                1f,
                1f
            );
    }


    // ==================================================
    // FADE
    // ==================================================

    private void PrepareFade()
    {
        if (fadeGroup == null)
            return;


        fadeGroup.blocksRaycasts =
            true;


        fadeGroup.interactable =
            false;
    }


    private float GetFadeAlpha()
    {
        return
            fadeGroup != null
                ? fadeGroup.alpha
                : 0f;
    }


    private IEnumerator Fade(
        float from,
        float to,
        float duration)
    {
        if (fadeGroup == null)
            yield break;


        if (duration <= 0f)
        {
            fadeGroup.alpha =
                to;

            yield break;
        }


        float elapsed =
            0f;


        fadeGroup.alpha =
            from;


        while (elapsed < duration)
        {
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


        fadeGroup.alpha =
            to;
    }


    private void SetFadeImmediate(
        float alpha,
        bool block)
    {
        if (fadeGroup == null)
            return;


        fadeGroup.alpha =
            Mathf.Clamp01(
                alpha
            );


        fadeGroup.interactable =
            false;


        fadeGroup.blocksRaycasts =
            block;
    }


    private void FinishFade()
    {
        if (fadeGroup == null)
            return;


        fadeGroup.alpha =
            0f;


        fadeGroup.interactable =
            false;


        fadeGroup.blocksRaycasts =
            false;
    }


    // ==================================================
    // CLEANUP
    // ==================================================

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance =
                null;
        }
    }


    // ==================================================
    // VALIDATE
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


        fastFadeOutDuration =
            Mathf.Max(
                0.01f,
                fastFadeOutDuration
            );


        fastFadeInDuration =
            Mathf.Max(
                0.01f,
                fastFadeInDuration
            );
    }
}