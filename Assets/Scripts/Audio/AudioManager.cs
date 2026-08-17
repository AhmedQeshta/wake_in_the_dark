using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class AudioManager : MonoBehaviour
{
    // ==================================================
    // INSTANCE
    // ==================================================

    public static AudioManager Instance { get; private set; }


    // ==================================================
    // MUSIC SOURCES
    // ==================================================

    [Header("Music Sources")]

    [SerializeField]
    private AudioSource musicSourceA;


    [SerializeField]
    private AudioSource musicSourceB;


    // ==================================================
    // LEVEL DETECTION
    // ==================================================

    [Header("Level Detection")]

    [SerializeField]
    private string levelScenePrefix =
        "Level_";


    // ==================================================
    // DEFAULTS
    // ==================================================

    [Header("Defaults")]

    [SerializeField, Range(0f, 1f)]
    private float defaultMusicVolume =
        0.6f;


    [SerializeField, Min(0f)]
    private float defaultCrossfadeDuration =
        0.8f;


    // ==================================================
    // USER MUSIC VOLUME
    // ==================================================

    [Header("User Music Volume")]

    [Tooltip(
        "Global user multiplier for level music. " +
        "1 = use the level's configured volume, 0 = mute."
    )]
    [SerializeField, Range(0f, 1f)]
    private float levelMusicVolume =
        1f;


    private const string
        LevelMusicVolumeKey =
            "Settings.LevelMusicVolume";


    // ==================================================
    // STATE
    // ==================================================

    private AudioSource activeMusicSource;

    private AudioSource inactiveMusicSource;


    private LevelMusicSettings currentLevelSettings;


    private Coroutine musicTransitionRoutine;


    private bool gameplayMusicEnabled;


    // ==================================================
    // PUBLIC
    // ==================================================

    public bool GameplayMusicEnabled =>
        gameplayMusicEnabled;


    public AudioClip CurrentMusicClip =>
        activeMusicSource != null
            ? activeMusicSource.clip
            : null;


    public float LevelMusicVolume =>
        levelMusicVolume;


    // ==================================================
    // AWAKE
    // ==================================================

    private void Awake()
    {
        // ----------------------------------------------
        // SINGLE INSTANCE
        // ----------------------------------------------

        if (Instance != null &&
            Instance != this)
        {
            Debug.LogError(
                "Duplicate AudioManager found.",
                this
            );


            Destroy(gameObject);

            return;
        }


        Instance =
            this;


        // ----------------------------------------------
        // USER MUSIC VOLUME
        // ----------------------------------------------

        levelMusicVolume =
            Mathf.Clamp01(
                PlayerPrefs.GetFloat(
                    LevelMusicVolumeKey,
                    levelMusicVolume
                )
            );


        /*
         * No DontDestroyOnLoad.
         *
         * AudioManager is already inside
         * the persistent Bootstrap scene.
         */


        // ----------------------------------------------
        // SOURCES
        // ----------------------------------------------

        ConfigureMusicSource(
            musicSourceA
        );


        ConfigureMusicSource(
            musicSourceB
        );


        activeMusicSource =
            musicSourceA;


        inactiveMusicSource =
            musicSourceB;
    }


    // ==================================================
    // ENABLE / DISABLE
    // ==================================================

    private void OnEnable()
    {
        /*
         * Bootstrap remains loaded, so this callback
         * will receive Level_01, Level_02, Level_03...
         *
         * Unity invokes sceneLoaded when a new
         * scene has completed loading.
         */
        SceneManager.sceneLoaded +=
            HandleSceneLoaded;
    }


    private void OnDisable()
    {
        SceneManager.sceneLoaded -=
            HandleSceneLoaded;
    }


    // ==================================================
    // START
    // ==================================================

    private void Start()
    {
        /*
         * Useful if testing Bootstrap +
         * a Level already opened together.
         */
        Scene activeScene =
            SceneManager.GetActiveScene();


        if (IsLevelScene(
                activeScene))
        {
            FindLevelMusicSettings(
                activeScene
            );
        }
    }


    // ==================================================
    // SCENE LOADED
    // ==================================================

    private void HandleSceneLoaded(
        Scene scene,
        LoadSceneMode loadMode)
    {
        if (!IsLevelScene(
                scene))
        {
            return;
        }


        Debug.Log(
            "AudioManager detected level: " +
            scene.name,
            this
        );


        FindLevelMusicSettings(
            scene
        );
    }


    // ==================================================
    // FIND LEVEL SETTINGS
    // ==================================================

    private void FindLevelMusicSettings(
        Scene scene)
    {
        currentLevelSettings =
            FindComponentInScene
                <LevelMusicSettings>(
                    scene
                );


        if (currentLevelSettings == null)
        {
            Debug.LogWarning(
                "No LevelMusicSettings found in " +
                scene.name +
                ".",
                this
            );


            if (gameplayMusicEnabled)
            {
                FadeOutMusic(
                    defaultCrossfadeDuration
                );
            }


            return;
        }


        Debug.Log(
            "Music configured for " +
            scene.name +
            ": " +
            (
                currentLevelSettings.MusicClip != null
                    ? currentLevelSettings.MusicClip.name
                    : "NONE"
            ),
            currentLevelSettings
        );


        /*
         * Initial Start Menu:
         *
         * We remember the Level 1 music,
         * but we do NOT play it until the
         * player presses Start.
         */
        if (!gameplayMusicEnabled)
        {
            return;
        }


        PlayCurrentLevelMusic();
    }


    // ==================================================
    // USER LEVEL MUSIC VOLUME
    // ==================================================

    public void SetLevelMusicVolume(
        float volume)
    {
        levelMusicVolume =
            Mathf.Clamp01(
                volume
            );


        PlayerPrefs.SetFloat(
            LevelMusicVolumeKey,
            levelMusicVolume
        );


        PlayerPrefs.Save();


        ApplyLevelMusicVolumeImmediately();
    }


    private void
        ApplyLevelMusicVolumeImmediately()
    {
        /*
         * LevelMusicSettings contains the artistic/base
         * volume for each level.
         *
         * The user's setting is a multiplier on top.
         *
         * Example:
         * Level volume = 0.7
         * User slider  = 0.5
         * Final volume = 0.35
         */
        float baseVolume =
            currentLevelSettings != null
                ? currentLevelSettings.MusicVolume
                : defaultMusicVolume;


        float finalVolume =
            baseVolume *
            levelMusicVolume;


        if (activeMusicSource != null &&
            activeMusicSource.clip != null)
        {
            activeMusicSource.volume =
                finalVolume;
        }


        /*
         * Usually the inactive source is silent.
         * If a crossfade happens to be running while
         * the setting changes, keep it inside the new
         * user volume limit too.
         */
        if (inactiveMusicSource != null &&
            inactiveMusicSource.isPlaying)
        {
            inactiveMusicSource.volume =
                Mathf.Min(
                    inactiveMusicSource.volume,
                    finalVolume
                );
        }
    }


    // ==================================================
    // START GAMEPLAY MUSIC
    // ==================================================

    public void StartGameplayMusic()
    {
        gameplayMusicEnabled =
            true;


        /*
         * If no settings were found yet,
         * search the active level.
         */
        if (currentLevelSettings == null)
        {
            Scene activeScene =
                SceneManager.GetActiveScene();


            if (IsLevelScene(
                    activeScene))
            {
                currentLevelSettings =
                    FindComponentInScene
                        <LevelMusicSettings>(
                            activeScene
                        );
            }
        }


        PlayCurrentLevelMusic();
    }


    // ==================================================
    // STOP GAMEPLAY MUSIC
    // ==================================================

    public void StopGameplayMusic(
        bool immediate = false)
    {
        gameplayMusicEnabled =
            false;


        if (musicTransitionRoutine != null)
        {
            StopCoroutine(
                musicTransitionRoutine
            );


            musicTransitionRoutine =
                null;
        }


        if (immediate)
        {
            StopSource(
                musicSourceA
            );


            StopSource(
                musicSourceB
            );


            return;
        }


        FadeOutMusic(
            defaultCrossfadeDuration
        );
    }


    // ==================================================
    // PLAY CURRENT LEVEL
    // ==================================================

    private void PlayCurrentLevelMusic()
    {
        if (!gameplayMusicEnabled)
            return;


        if (currentLevelSettings == null)
            return;


        AudioClip newClip =
            currentLevelSettings.MusicClip;


        if (newClip == null)
        {
            FadeOutMusic(
                currentLevelSettings.CrossfadeDuration
            );


            return;
        }


        float targetVolume =
            currentLevelSettings.MusicVolume *
            levelMusicVolume;


        float transitionDuration =
            currentLevelSettings.CrossfadeDuration;


        // ==================================================
        // SAME MUSIC
        // ==================================================

        if (activeMusicSource != null &&
            activeMusicSource.clip ==
                newClip)
        {
            activeMusicSource.loop =
                currentLevelSettings.Loop;


            /*
             * Resetting a level does not need
             * to restart the song unless requested.
             */
            if (currentLevelSettings
                .RestartOnReload)
            {
                RestartCurrentTrack(
                    newClip,
                    targetVolume,
                    transitionDuration,
                    currentLevelSettings.Loop
                );


                return;
            }


            activeMusicSource.volume =
                targetVolume;


            if (!activeMusicSource.isPlaying)
            {
                activeMusicSource.Play();
            }


            return;
        }


        // ==================================================
        // NEW LEVEL MUSIC
        // ==================================================

        CrossfadeTo(
            newClip,
            targetVolume,
            transitionDuration,
            currentLevelSettings.Loop
        );
    }


    // ==================================================
    // CROSSFADE
    // ==================================================

    private void CrossfadeTo(
        AudioClip newClip,
        float targetVolume,
        float duration,
        bool loop)
    {
        if (musicTransitionRoutine != null)
        {
            StopCoroutine(
                musicTransitionRoutine
            );
        }


        musicTransitionRoutine =
            StartCoroutine(
                CrossfadeRoutine(
                    newClip,
                    targetVolume,
                    duration,
                    loop
                )
            );
    }


    private IEnumerator CrossfadeRoutine(
        AudioClip newClip,
        float targetVolume,
        float duration,
        bool loop)
    {
        EnsureSources();


        AudioSource oldSource =
            activeMusicSource;


        AudioSource newSource =
            inactiveMusicSource;


        // ----------------------------------------------
        // PREPARE NEW SOURCE
        // ----------------------------------------------

        newSource.Stop();


        newSource.clip =
            newClip;


        newSource.loop =
            loop;


        newSource.volume =
            0f;


        newSource.Play();


        // ----------------------------------------------
        // INSTANT
        // ----------------------------------------------

        if (duration <= 0f)
        {
            if (oldSource != null)
            {
                oldSource.Stop();
                oldSource.volume = 0f;
            }


            newSource.volume =
                targetVolume;


            SwapMusicSources();


            musicTransitionRoutine =
                null;


            yield break;
        }


        // ----------------------------------------------
        // CROSSFADE
        // ----------------------------------------------

        float elapsed =
            0f;


        float oldStartVolume =
            oldSource != null
                ? oldSource.volume
                : 0f;


        while (elapsed < duration)
        {
            elapsed +=
                Time.unscaledDeltaTime;


            float t =
                Mathf.Clamp01(
                    elapsed /
                    duration
                );


            if (oldSource != null)
            {
                oldSource.volume =
                    Mathf.Lerp(
                        oldStartVolume,
                        0f,
                        t
                    );
            }


            newSource.volume =
                Mathf.Lerp(
                    0f,
                    targetVolume,
                    t
                );


            yield return null;
        }


        // ----------------------------------------------
        // FINISH
        // ----------------------------------------------

        if (oldSource != null)
        {
            oldSource.Stop();

            oldSource.volume =
                0f;
        }


        newSource.volume =
            targetVolume;


        SwapMusicSources();


        musicTransitionRoutine =
            null;
    }


    // ==================================================
    // RESTART CURRENT TRACK
    // ==================================================

    private void RestartCurrentTrack(
        AudioClip clip,
        float targetVolume,
        float duration,
        bool loop)
    {
        if (musicTransitionRoutine != null)
        {
            StopCoroutine(
                musicTransitionRoutine
            );
        }


        musicTransitionRoutine =
            StartCoroutine(
                RestartCurrentTrackRoutine(
                    clip,
                    targetVolume,
                    duration,
                    loop
                )
            );
    }


    private IEnumerator RestartCurrentTrackRoutine(
        AudioClip clip,
        float targetVolume,
        float duration,
        bool loop)
    {
        EnsureSources();


        AudioSource source =
            activeMusicSource;


        source.Stop();


        source.clip =
            clip;


        source.loop =
            loop;


        source.volume =
            0f;


        source.Play();


        if (duration <= 0f)
        {
            source.volume =
                targetVolume;


            musicTransitionRoutine =
                null;


            yield break;
        }


        float elapsed =
            0f;


        while (elapsed < duration)
        {
            elapsed +=
                Time.unscaledDeltaTime;


            float t =
                Mathf.Clamp01(
                    elapsed /
                    duration
                );


            source.volume =
                Mathf.Lerp(
                    0f,
                    targetVolume,
                    t
                );


            yield return null;
        }


        source.volume =
            targetVolume;


        musicTransitionRoutine =
            null;
    }


    // ==================================================
    // FADE OUT
    // ==================================================

    private void FadeOutMusic(
        float duration)
    {
        if (musicTransitionRoutine != null)
        {
            StopCoroutine(
                musicTransitionRoutine
            );
        }


        musicTransitionRoutine =
            StartCoroutine(
                FadeOutMusicRoutine(
                    duration
                )
            );
    }


    private IEnumerator FadeOutMusicRoutine(
        float duration)
    {
        EnsureSources();


        AudioSource sourceA =
            musicSourceA;


        AudioSource sourceB =
            musicSourceB;


        float startA =
            sourceA != null
                ? sourceA.volume
                : 0f;


        float startB =
            sourceB != null
                ? sourceB.volume
                : 0f;


        if (duration <= 0f)
        {
            StopSource(
                sourceA
            );


            StopSource(
                sourceB
            );


            musicTransitionRoutine =
                null;


            yield break;
        }


        float elapsed =
            0f;


        while (elapsed < duration)
        {
            elapsed +=
                Time.unscaledDeltaTime;


            float t =
                Mathf.Clamp01(
                    elapsed /
                    duration
                );


            if (sourceA != null)
            {
                sourceA.volume =
                    Mathf.Lerp(
                        startA,
                        0f,
                        t
                    );
            }


            if (sourceB != null)
            {
                sourceB.volume =
                    Mathf.Lerp(
                        startB,
                        0f,
                        t
                    );
            }


            yield return null;
        }


        StopSource(
            sourceA
        );


        StopSource(
            sourceB
        );


        musicTransitionRoutine =
            null;
    }


    // ==================================================
    // SOURCE SETUP
    // ==================================================

    private void ConfigureMusicSource(
        AudioSource source)
    {
        if (source == null)
            return;


        source.playOnAwake =
            false;


        source.loop =
            true;


        /*
         * Music is 2D.
         */
        source.spatialBlend =
            0f;


        /*
         * IMPORTANT:
         *
         * When UIManager uses:
         *
         * AudioListener.pause = true
         *
         * level music should pause too.
         */
        source.ignoreListenerPause =
            false;


        source.volume =
            0f;
    }


    private void EnsureSources()
    {
        if (activeMusicSource == null)
        {
            activeMusicSource =
                musicSourceA;
        }


        if (inactiveMusicSource == null)
        {
            inactiveMusicSource =
                activeMusicSource ==
                    musicSourceA
                    ? musicSourceB
                    : musicSourceA;
        }
    }


    private void SwapMusicSources()
    {
        AudioSource temp =
            activeMusicSource;


        activeMusicSource =
            inactiveMusicSource;


        inactiveMusicSource =
            temp;
    }


    private void StopSource(
        AudioSource source)
    {
        if (source == null)
            return;


        source.Stop();


        source.volume =
            0f;
    }


    // ==================================================
    // LEVEL CHECK
    // ==================================================

    private bool IsLevelScene(
        Scene scene)
    {
        if (!scene.IsValid() ||
            !scene.isLoaded)
        {
            return false;
        }


        return
            scene.name.StartsWith(
                levelScenePrefix,
                StringComparison.OrdinalIgnoreCase
            );
    }


    // ==================================================
    // FIND COMPONENT IN SCENE
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


            T component =
                root.GetComponentInChildren<T>(
                    true
                );


            if (component != null)
            {
                return component;
            }
        }


        return null;
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
    // VALIDATION
    // ==================================================

    private void OnValidate()
    {
        defaultMusicVolume =
            Mathf.Clamp01(
                defaultMusicVolume
            );


        defaultCrossfadeDuration =
            Mathf.Max(
                0f,
                defaultCrossfadeDuration
            );


        levelMusicVolume =
            Mathf.Clamp01(
                levelMusicVolume
            );


        if (string.IsNullOrWhiteSpace(
                levelScenePrefix))
        {
            levelScenePrefix =
                "Level_";
        }
    }
}