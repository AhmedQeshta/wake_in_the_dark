using UnityEngine;
using UnityEngine.Audio;

[DisallowMultipleComponent]
public class GameAudioMixerSettings : MonoBehaviour
{
    // INSTANCE
    public static GameAudioMixerSettings Instance { get; private set; }


    // MIXER
    [Header("Audio Mixer")]
    [Tooltip("Main AudioMixer asset used by the whole game.")]
    [SerializeField] private AudioMixer audioMixer;


    // EXPOSED PARAMETER NAMES
    [Header("Exposed Parameter Names")]
    [SerializeField] private string masterVolumeParameter = "MasterVolume";
    [SerializeField] private string menuMusicVolumeParameter = "MenuMusicVolume";
    [SerializeField] private string levelMusicVolumeParameter = "LevelMusicVolume";
    [SerializeField] private string endGameMusicVolumeParameter = "EndGameMusicVolume";
    [SerializeField] private string videoVolumeParameter = "VideoVolume";
    [SerializeField] private string vfxVolumeParameter = "VFXVolume";


    // DEFAULT VALUES
    [Header("Default Linear Volumes")]
    [SerializeField, Range(0f, 1f)] private float defaultMasterVolume = 1f;
    [SerializeField, Range(0f, 1f)] private float defaultMenuMusicVolume = 0.5f;
    [SerializeField, Range(0f, 1f)] private float defaultLevelMusicVolume = 1f;
    [SerializeField, Range(0f, 1f)] private float defaultEndGameMusicVolume = 0.7f;
    [SerializeField, Range(0f, 1f)] private float defaultVideoVolume = 0.5f;
    [SerializeField, Range(0f, 1f)] private float defaultVFXVolume = 0.8f;


    // PLAYER PREF KEYS

    public const string MasterVolumeKey = "Settings.MasterVolume";

    public const string MenuMusicVolumeKey = "Settings.MenuMusicVolume";

    /*
     * Keep the same keys your old AudioManager and
     * AudioSettingsUI already used.
     */
    public const string LevelMusicVolumeKey = "Settings.LevelMusicVolume";
    public const string EndGameMusicVolumeKey = "Settings.EndGameMusicVolume";
    public const string VideoVolumeKey = "Settings.LevelIntroVideoVolume";
    public const string VFXVolumeKey = "Settings.VFXVolume";


    // STATE
    private float masterVolume;
    private float menuMusicVolume;
    private float levelMusicVolume;
    private float endGameMusicVolume;
    private float videoVolume;
    private float vfxVolume;


    // PUBLIC VALUES
    public float MasterVolume => masterVolume;
    public float MenuMusicVolume => menuMusicVolume;
    public float LevelMusicVolume => levelMusicVolume;
    public float EndGameMusicVolume => endGameMusicVolume;
    public float VideoVolume => videoVolume;
    public float VFXVolume => vfxVolume;


    // AWAKE

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;

        LoadAndApplyAll();
    }


    // LOAD

    public void LoadAndApplyAll()
    {
        masterVolume = GetSavedVolume(MasterVolumeKey, defaultMasterVolume);
        menuMusicVolume = GetSavedVolume(MenuMusicVolumeKey, defaultMenuMusicVolume);
        levelMusicVolume = GetSavedVolume(LevelMusicVolumeKey, defaultLevelMusicVolume);
        endGameMusicVolume = GetSavedVolume(EndGameMusicVolumeKey, defaultEndGameMusicVolume);
        videoVolume = GetSavedVolume(VideoVolumeKey, defaultVideoVolume);
        vfxVolume = GetSavedVolume(VFXVolumeKey, defaultVFXVolume);
        ApplyAllToMixer();
    }


    // SETTERS
    public void SetMasterVolume(float value)
    {
        masterVolume = SaveAndApply(MasterVolumeKey, masterVolumeParameter, value);
    }


    public void SetMenuMusicVolume(float value)
    {
        menuMusicVolume = SaveAndApply(MenuMusicVolumeKey, menuMusicVolumeParameter, value);
    }


    public void SetLevelMusicVolume(float value)
    {
        levelMusicVolume = SaveAndApply(LevelMusicVolumeKey, levelMusicVolumeParameter, value);
    }


    public void SetEndGameMusicVolume(float value)
    {
        endGameMusicVolume = SaveAndApply(EndGameMusicVolumeKey, endGameMusicVolumeParameter, value);
    }


    public void SetVideoVolume(float value)
    {
        videoVolume = SaveAndApply(VideoVolumeKey, videoVolumeParameter, value);
    }


    public void SetVFXVolume(float value)
    {
        vfxVolume = SaveAndApply(VFXVolumeKey, vfxVolumeParameter, value);
    }


    // APPLY ALL
    private void ApplyAllToMixer()
    {
        SetMixerVolume(masterVolumeParameter, masterVolume);
        SetMixerVolume(menuMusicVolumeParameter, menuMusicVolume);
        SetMixerVolume(levelMusicVolumeParameter, levelMusicVolume);
        SetMixerVolume(endGameMusicVolumeParameter, endGameMusicVolume);
        SetMixerVolume(videoVolumeParameter, videoVolume);
        SetMixerVolume(vfxVolumeParameter, vfxVolume);
    }


    // SAVE + APPLY
    private float SaveAndApply(string playerPrefsKey, string mixerParameter, float value)
    {
        value = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat(playerPrefsKey, value);

        /* 
         * Explicit save is useful for WebGL because we do not  
         * rely on the browser/application shutting down cleanly.  
         */

        PlayerPrefs.Save();
        SetMixerVolume(mixerParameter, value);
        return value;
    }


    // MIXER
    private void SetMixerVolume(string parameterName, float linearVolume)
    {
        if (audioMixer == null || string.IsNullOrWhiteSpace(parameterName))
            return;

        float decibels = LinearToDecibels(linearVolume);

        if (!audioMixer.SetFloat(parameterName, decibels))
        {
            Debug.LogWarning("GameAudioMixerSettings: AudioMixer parameter '"
            + parameterName
            + "' was not found. Make sure the group Volume is exposed and renamed exactly.",
            this);
        }
    }


    public static float LinearToDecibels(float linearVolume)
    {
        linearVolume = Mathf.Clamp01(linearVolume);

        if (linearVolume <= 0.0001f)
            return -80f;

        return Mathf.Log10(linearVolume) * 20f;
    }


    // SAVED VALUE

    private static float GetSavedVolume(string key, float defaultValue)
    {
        return Mathf.Clamp01(PlayerPrefs.GetFloat(key, Mathf.Clamp01(defaultValue)));
    }


    // VALIDATE

    private void OnValidate()
    {
        defaultMasterVolume = Mathf.Clamp01(defaultMasterVolume);
        defaultMenuMusicVolume = Mathf.Clamp01(defaultMenuMusicVolume);
        defaultLevelMusicVolume = Mathf.Clamp01(defaultLevelMusicVolume);
        defaultEndGameMusicVolume = Mathf.Clamp01(defaultEndGameMusicVolume);
        defaultVideoVolume = Mathf.Clamp01(defaultVideoVolume);
        defaultVFXVolume = Mathf.Clamp01(defaultVFXVolume);

        if (string.IsNullOrWhiteSpace(masterVolumeParameter))
            masterVolumeParameter = "MasterVolume";

        if (string.IsNullOrWhiteSpace(menuMusicVolumeParameter))
            menuMusicVolumeParameter = "MenuMusicVolume";

        if (string.IsNullOrWhiteSpace(levelMusicVolumeParameter))
            levelMusicVolumeParameter = "LevelMusicVolume";

        if (string.IsNullOrWhiteSpace(endGameMusicVolumeParameter))
            endGameMusicVolumeParameter = "EndGameMusicVolume";

        if (string.IsNullOrWhiteSpace(videoVolumeParameter))
            videoVolumeParameter = "VideoVolume";

        if (string.IsNullOrWhiteSpace(vfxVolumeParameter))
            vfxVolumeParameter = "VFXVolume";
    }


    // DESTROY

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}
