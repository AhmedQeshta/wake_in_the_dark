using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class AudioManager : MonoBehaviour
{
    // INSTANCE
    public static AudioManager Instance { get; private set; }


    // MUSIC SOURCES
    [Header("Music Sources")]
    [SerializeField] private AudioSource musicSourceA;
    [SerializeField] private AudioSource musicSourceB;


    // LEVEL DETECTION

    [Header("Level Detection")]
    [SerializeField] private string levelScenePrefix = "Level_";


    // DEFAULTS

    [Header("Defaults")]

    [SerializeField, Range(0f, 1f)] private float defaultMusicVolume = 0.6f;


    [SerializeField, Min(0f)] private float defaultCrossfadeDuration = 0.8f;


    // USER MUSIC VOLUME

    [Header("User Music Volume")]

    [Tooltip("Compatibility/fallback user value for level music. When GameAudioMixerSettings exists, the AudioMixer applies this setting and the AudioSource keeps only the level's artistic/base volume.")]
    [SerializeField, Range(0f, 1f)] private float levelMusicVolume = 1f;

    private const string LevelMusicVolumeKey = "Settings.LevelMusicVolume";


    // STATE

    private AudioSource activeMusicSource;

    private AudioSource inactiveMusicSource;


    private LevelMusicSettings currentLevelSettings;


    private Coroutine musicTransitionRoutine;


    private bool gameplayMusicEnabled;


    /*
     * True while a level-intro / ending video owns the audio transition.
     *
     * During this state:
     * - previous level audio has been stopped
     * - AudioListener is paused
     * - VideoPlayer audio is allowed to keep playing
     * - newly loaded level music waits until StartGameplayMusic()
     */
    private bool transitionSilenceActive;


    /*
     * True when the already-loaded level music was started early
     * specifically so it can play underneath its intro video.
     */
    private bool levelMusicPrestartedForIntro;


    // PUBLIC

    public bool GameplayMusicEnabled => gameplayMusicEnabled;


    public AudioClip CurrentMusicClip => activeMusicSource != null ? activeMusicSource.clip : null;


    public float LevelMusicVolume => GameAudioMixerSettings.Instance != null ? GameAudioMixerSettings.Instance.LevelMusicVolume : levelMusicVolume;


    public bool TransitionSilenceActive => transitionSilenceActive;


    // AWAKE
    private void Awake()
    {
        // SINGLE INSTANCE
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        // USER MUSIC VOLUME
        levelMusicVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(LevelMusicVolumeKey, levelMusicVolume));


        // SOURCES
        ConfigureMusicSource(musicSourceA);
        ConfigureMusicSource(musicSourceB);

        activeMusicSource = musicSourceA;
        inactiveMusicSource = musicSourceB;
    }


    // ENABLE / DISABLE

    private void OnEnable()
    {
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }


    private void OnDisable()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
    }


    // START

    private void Start()
    {

        Scene activeScene = SceneManager.GetActiveScene();


        if (IsLevelScene(activeScene))
            FindLevelMusicSettings(activeScene);
    }


    // SCENE LOADED

    private void HandleSceneLoaded(Scene scene, LoadSceneMode loadMode)
    {
        if (!IsLevelScene(scene))
            return;

        FindLevelMusicSettings(scene);
    }


    // FIND LEVEL SETTINGS

    private void FindLevelMusicSettings(Scene scene)
    {
        currentLevelSettings = FindComponentInScene<LevelMusicSettings>(scene);


        if (currentLevelSettings == null)
        {
            if (gameplayMusicEnabled)
                FadeOutMusic(defaultCrossfadeDuration);
            return;
        }

        if (!gameplayMusicEnabled)
            return;


        PlayCurrentLevelMusic();
    }


    // USER LEVEL MUSIC VOLUME

    public void SetLevelMusicVolume(float volume)
    {
        levelMusicVolume = Mathf.Clamp01(volume);


        if (GameAudioMixerSettings.Instance != null)
        {
            /*
             * The AudioMixer now owns the user's level-music volume.
             * The level AudioSources keep the artistic/base volume
             * from LevelMusicSettings so volume is not applied twice.
             */
            GameAudioMixerSettings.Instance.SetLevelMusicVolume(levelMusicVolume);
        }
        else
        {
            /*
             * Backward-compatible fallback when the mixer settings
             * component is missing.
             */
            PlayerPrefs.SetFloat(LevelMusicVolumeKey, levelMusicVolume);

            PlayerPrefs.Save();
        }


        ApplyLevelMusicVolumeImmediately();
    }


    private void ApplyLevelMusicVolumeImmediately()
    {
        float baseVolume = currentLevelSettings != null ? currentLevelSettings.MusicVolume : defaultMusicVolume;
        float sourceMultiplier = GameAudioMixerSettings.Instance != null ? 1f : levelMusicVolume;
        float finalSourceVolume = baseVolume * sourceMultiplier;

        if (activeMusicSource != null && activeMusicSource.clip != null)
            activeMusicSource.volume = finalSourceVolume;

        if (inactiveMusicSource != null && inactiveMusicSource.isPlaying)
            inactiveMusicSource.volume = Mathf.Min(inactiveMusicSource.volume, finalSourceVolume);

    }


    // START GAMEPLAY MUSIC

    public void StartGameplayMusic()
    {
        /*
         * Gameplay is officially active now.
         */
        transitionSilenceActive = false;

        AudioListener.pause = false;

        SetMusicSourcesIgnoreListenerPause(false);


        gameplayMusicEnabled = true;


        /*
         * If no settings were found yet,
         * search the active level.
         */
        if (currentLevelSettings == null)
        {
            Scene activeScene = SceneManager.GetActiveScene();

            if (IsLevelScene(activeScene))
                currentLevelSettings = FindComponentInScene<LevelMusicSettings>(activeScene);

        }


        /*
         * If this exact music was already started underneath
         * the intro video, do NOT restart it when gameplay begins.
         */
        bool musicWasPrestarted = levelMusicPrestartedForIntro;
        levelMusicPrestartedForIntro = false;

        PlayCurrentLevelMusic(false, !musicWasPrestarted);
    }


    // START LOADED LEVEL MUSIC DURING INTRO VIDEO
    public bool StartLoadedLevelMusicForIntro(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
            return false;


        Scene levelScene = SceneManager.GetSceneByName(sceneName);

        if (!IsLevelScene(levelScene))
            return false;

        LevelMusicSettings settings = FindComponentInScene<LevelMusicSettings>(levelScene);


        if (settings == null)
            return false;


        currentLevelSettings = settings;


        levelMusicPrestartedForIntro = true;


        /*
         * Force playback even though normal gameplay is not active yet.
         * RestartOnReload is intentionally ignored here.
         */
        PlayCurrentLevelMusic(true, false);


        return activeMusicSource != null && activeMusicSource.clip != null;
    }


    // STOP GAMEPLAY MUSIC

    public void StopGameplayMusic(bool immediate = false)
    {
        gameplayMusicEnabled = false;

        levelMusicPrestartedForIntro = false;

        SetMusicSourcesIgnoreListenerPause(false);


        if (musicTransitionRoutine != null)
        {
            StopCoroutine(musicTransitionRoutine);
            musicTransitionRoutine = null;
        }


        if (immediate)
        {
            StopSource(musicSourceA);
            StopSource(musicSourceB);
            return;
        }


        FadeOutMusic(defaultCrossfadeDuration);
    }


    // VIDEO / LEVEL TRANSITION AUDIO
    public void BeginVideoTransition(AudioSource allowedVideoAudioSource = null, bool keepCurrentLevelMusicPlaying = false)
    {
        transitionSilenceActive = true;


        if (keepCurrentLevelMusicPlaying)
        {
            /*
             * Level_01 is already loaded behind Bootstrap.
             * Keep its persistent music source alive and let it
             * ignore AudioListener.pause while the intro video plays.
             */
            SetMusicSourcesIgnoreListenerPause(true);
        }
        else
        {
            gameplayMusicEnabled = false;
            levelMusicPrestartedForIntro = false;

            if (musicTransitionRoutine != null)
            {
                StopCoroutine(musicTransitionRoutine);
                musicTransitionRoutine = null;
            }

            StopSource(musicSourceA);
            StopSource(musicSourceB);

            SetMusicSourcesIgnoreListenerPause(false);
        }


        /*
         * Stop world/menu/VFX audio.
         *
         * When requested, preserve:
         * - VideoPlayer AudioSource
         * - MusicSource_A
         * - MusicSource_B
         */
        StopAllLoadedAudioExcept(allowedVideoAudioSource, keepCurrentLevelMusicPlaying ? musicSourceA : null, keepCurrentLevelMusicPlaying ? musicSourceB : null);


        /*
         * Newly-started world audio remains paused.
         *
         * VideoPlayer source already uses ignoreListenerPause = true.
         * Preserved level-music sources temporarily do the same.
         */
        AudioListener.pause = true;
    }

    public void StopAllLoadedAudioExcept(AudioSource exceptionA = null, AudioSource exceptionB = null, AudioSource exceptionC = null)
    {
        AudioSource[] sources = FindObjectsByType<AudioSource>(FindObjectsInactive.Include, FindObjectsSortMode.None);


        foreach (AudioSource source in sources)
        {
            if (source == null || source == exceptionA || source == exceptionB || source == exceptionC)
                continue;


            Scene sourceScene = source.gameObject.scene;


            if (!sourceScene.IsValid() || !sourceScene.isLoaded)
                continue;

            source.Stop();
        }
    }

    public void CancelVideoTransitionSilence(bool restartGameplayMusic = false)
    {
        transitionSilenceActive = false;
        AudioListener.pause = false;
        SetMusicSourcesIgnoreListenerPause(false);

        if (restartGameplayMusic)
            StartGameplayMusic();

    }


    // PLAY CURRENT LEVEL

    private void PlayCurrentLevelMusic(bool forcePlayback = false, bool allowRestartOnReload = true)
    {
        if ((!gameplayMusicEnabled && !forcePlayback) || currentLevelSettings == null)
            return;


        AudioClip newClip = currentLevelSettings.MusicClip;


        if (newClip == null)
        {
            FadeOutMusic(currentLevelSettings.CrossfadeDuration);
            return;
        }


        /*
         * Mixer version:
         * - AudioSource volume = artistic/base level volume.
         * - AudioMixer LevelMusicVolume = user's setting.
         *
         * If GameAudioMixerSettings is missing, preserve the old
         * source-volume multiplier as a safe fallback.
         */
        float sourceUserMultiplier = GameAudioMixerSettings.Instance != null ? 1f : levelMusicVolume;
        float targetVolume = currentLevelSettings.MusicVolume * sourceUserMultiplier;
        float transitionDuration = currentLevelSettings.CrossfadeDuration;



        // SAME MUSIC


        if (activeMusicSource != null && activeMusicSource.clip == newClip)
        {
            activeMusicSource.loop = currentLevelSettings.Loop;


            /*
             * Resetting a level does not need
             * to restart the song unless requested.
             */
            if (allowRestartOnReload && currentLevelSettings.RestartOnReload)
            {
                RestartCurrentTrack(newClip, targetVolume, transitionDuration, currentLevelSettings.Loop);

                return;
            }

            activeMusicSource.volume = targetVolume;


            if (!activeMusicSource.isPlaying)
                activeMusicSource.Play();

            return;
        }



        // NEW LEVEL MUSIC


        CrossfadeTo(newClip, targetVolume, transitionDuration, currentLevelSettings.Loop);
    }


    // CROSSFADE

    private void CrossfadeTo(AudioClip newClip, float targetVolume, float duration, bool loop)
    {
        if (musicTransitionRoutine != null)
            StopCoroutine(musicTransitionRoutine);

        musicTransitionRoutine = StartCoroutine(CrossfadeRoutine(newClip, targetVolume, duration, loop));
    }


    private IEnumerator CrossfadeRoutine(AudioClip newClip, float targetVolume, float duration, bool loop)
    {
        EnsureSources();


        AudioSource oldSource = activeMusicSource;


        AudioSource newSource = inactiveMusicSource;


        // PREPARE NEW SOURCE
        newSource.Stop();
        newSource.clip = newClip;
        newSource.loop = loop;
        newSource.volume = 0f;
        newSource.Play();


        // INSTANT

        if (duration <= 0f)
        {
            if (oldSource != null)
            {
                oldSource.Stop();
                oldSource.volume = 0f;
            }
            newSource.volume = targetVolume;
            SwapMusicSources();
            musicTransitionRoutine = null;
            yield break;
        }


        // CROSSFADE

        float elapsed = 0f;


        float oldStartVolume = oldSource != null ? oldSource.volume : 0f;


        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;

            float t = Mathf.Clamp01(elapsed / duration);

            if (oldSource != null)
                oldSource.volume = Mathf.Lerp(oldStartVolume, 0f, t);

            newSource.volume = Mathf.Lerp(0f, targetVolume, t);

            yield return null;
        }


        // FINISH

        if (oldSource != null)
        {
            oldSource.Stop();

            oldSource.volume = 0f;
        }


        newSource.volume = targetVolume;


        SwapMusicSources();


        musicTransitionRoutine = null;
    }


    // RESTART CURRENT TRACK

    private void RestartCurrentTrack(AudioClip clip, float targetVolume, float duration, bool loop)
    {
        if (musicTransitionRoutine != null)
            StopCoroutine(musicTransitionRoutine);

        musicTransitionRoutine = StartCoroutine(RestartCurrentTrackRoutine(clip, targetVolume, duration, loop));
    }


    private IEnumerator RestartCurrentTrackRoutine(AudioClip clip, float targetVolume, float duration, bool loop)
    {
        EnsureSources();
        AudioSource source = activeMusicSource;

        source.Stop();
        source.clip = clip;
        source.loop = loop;
        source.volume = 0f;
        source.Play();


        if (duration <= 0f)
        {
            source.volume = targetVolume;
            musicTransitionRoutine = null;
            yield break;
        }


        float elapsed = 0f;


        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;

            float t = Mathf.Clamp01(elapsed / duration);

            source.volume = Mathf.Lerp(0f, targetVolume, t);

            yield return null;
        }


        source.volume = targetVolume;


        musicTransitionRoutine = null;
    }


    // FADE OUT

    private void FadeOutMusic(float duration)
    {
        if (musicTransitionRoutine != null)
            StopCoroutine(musicTransitionRoutine);
        musicTransitionRoutine = StartCoroutine(FadeOutMusicRoutine(duration));
    }


    private IEnumerator FadeOutMusicRoutine(float duration)
    {
        EnsureSources();

        AudioSource sourceA = musicSourceA;
        AudioSource sourceB = musicSourceB;

        float startA = sourceA != null ? sourceA.volume : 0f;
        float startB = sourceB != null ? sourceB.volume : 0f;


        if (duration <= 0f)
        {
            StopSource(sourceA);
            StopSource(sourceB);
            musicTransitionRoutine = null;
            yield break;
        }

        float elapsed = 0f;


        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;


            float t = Mathf.Clamp01(elapsed / duration);


            if (sourceA != null)
                sourceA.volume = Mathf.Lerp(startA, 0f, t);

            if (sourceB != null)
                sourceB.volume = Mathf.Lerp(startB, 0f, t);

            yield return null;
        }

        StopSource(sourceA);
        StopSource(sourceB);

        musicTransitionRoutine = null;
    }


    // MUSIC LISTENER-PAUSE MODE

    private void SetMusicSourcesIgnoreListenerPause(
        bool ignorePause)
    {
        if (musicSourceA != null)
        {
            musicSourceA.ignoreListenerPause =
                ignorePause;
        }


        if (musicSourceB != null)
        {
            musicSourceB.ignoreListenerPause =
                ignorePause;
        }
    }


    // SOURCE SETUP

    private void ConfigureMusicSource(AudioSource source)
    {
        if (source == null)
            return;

        source.playOnAwake = false;
        source.loop = true;
        source.spatialBlend = 0f;
        source.ignoreListenerPause = false;
        source.volume = 0f;
    }


    private void EnsureSources()
    {
        if (activeMusicSource == null)
            activeMusicSource = musicSourceA;

        if (inactiveMusicSource == null)
            inactiveMusicSource = activeMusicSource == musicSourceA ? musicSourceB : musicSourceA;
    }


    private void SwapMusicSources()
    {
        AudioSource temp = activeMusicSource;
        activeMusicSource = inactiveMusicSource;
        inactiveMusicSource = temp;
    }


    private void StopSource(AudioSource source)
    {
        if (source == null)
            return;
        source.Stop();
        source.volume = 0f;
    }


    // LEVEL CHECK

    private bool IsLevelScene(Scene scene)
    {
        if (!scene.IsValid() || !scene.isLoaded)
            return false;
        return scene.name.StartsWith(levelScenePrefix, StringComparison.OrdinalIgnoreCase);
    }


    // FIND COMPONENT IN SCENE

    private T FindComponentInScene<T>(Scene scene) where T : Component
    {
        if (!scene.IsValid() || !scene.isLoaded)
            return null;


        GameObject[] roots = scene.GetRootGameObjects();


        foreach (GameObject root in roots)
        {
            if (root == null)
                continue;

            T component = root.GetComponentInChildren<T>(true);

            if (component != null)
                return component;

        }
        return null;
    }


    // CLEANUP

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }


    // VALIDATION

    private void OnValidate()
    {
        defaultMusicVolume = Mathf.Clamp01(defaultMusicVolume);


        defaultCrossfadeDuration = Mathf.Max(0f, defaultCrossfadeDuration);


        levelMusicVolume = Mathf.Clamp01(levelMusicVolume);


        if (string.IsNullOrWhiteSpace(levelScenePrefix))
            levelScenePrefix = "Level_";

    }
}