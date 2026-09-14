using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AudioSettingsUI : MonoBehaviour
{
    // MIXER SETTINGS
    [Header("Mixer Settings")]
    [Tooltip("Persistent GameAudioMixerSettings component. Leave empty to auto-find the singleton.")]
    [SerializeField] private GameAudioMixerSettings mixerSettings;


    // MASTER
    [Header("Master")]
    [SerializeField] private Slider masterVolumeSlider;
    [SerializeField] private TMP_Text masterVolumeValueText;


    // MENU MUSIC
    [Header("Menu Music")]
    [SerializeField] private Slider menuMusicSlider;
    [SerializeField] private TMP_Text menuMusicValueText;


    // LEVEL MUSIC
    [Header("Level Music")]
    [SerializeField] private Slider levelMusicSlider;
    [SerializeField] private TMP_Text levelMusicValueText;


    // END GAME MUSIC
    [Header("End Game Music")]
    [SerializeField] private Slider endGameMusicSlider;
    [SerializeField] private TMP_Text endGameMusicValueText;


    // VIDEO AUDIO
    [Header("Video Audio")]
    [SerializeField] private Slider videoVolumeSlider;
    [SerializeField] private TMP_Text videoVolumeValueText;


    // VFX
    [Header("VFX")]
    [SerializeField] private Slider vfxVolumeSlider;
    [SerializeField] private TMP_Text vfxVolumeValueText;


    // FALLBACK DEFAULTS
    [Header("Fallback Defaults")]
    [SerializeField, Range(0f, 1f)] private float defaultMasterVolume = 1f;
    [SerializeField, Range(0f, 1f)] private float defaultMenuMusicVolume = 0.5f;
    [SerializeField, Range(0f, 1f)] private float defaultLevelMusicVolume = 1f;
    [SerializeField, Range(0f, 1f)] private float defaultEndGameMusicVolume = 0.7f;
    [SerializeField, Range(0f, 1f)] private float defaultVideoVolume = 0.5f;
    [SerializeField, Range(0f, 1f)] private float defaultVFXVolume = 0.8f;


    // AWAKE
    private void Awake()
    {
        ConfigureSlider(masterVolumeSlider);
        ConfigureSlider(menuMusicSlider);
        ConfigureSlider(levelMusicSlider);
        ConfigureSlider(endGameMusicSlider);
        ConfigureSlider(videoVolumeSlider);
        ConfigureSlider(vfxVolumeSlider);

        ResolveMixerSettings();
        LoadValues();
        SetupListeners();
    }


    // START
    private void Start()
    { /*  
        * Handles any Awake execution-order difference between  
        * Audio Settings UI and Game Audio Mixer Settings. 
     */
        if (mixerSettings == null)
        {
            ResolveMixerSettings();
            LoadValues();
        }
    }


    // RESOLVE
    private void ResolveMixerSettings()
    {
        if (mixerSettings != null)
            return;

        mixerSettings = GameAudioMixerSettings.Instance;

        if (mixerSettings == null)
            mixerSettings = FindAnyObjectByType<GameAudioMixerSettings>();
    }


    // LOAD VALUES
    public void LoadValues()
    {
        float master = GetCurrentOrSaved(mixerSettings != null ? mixerSettings.MasterVolume : -1f, GameAudioMixerSettings.MasterVolumeKey, defaultMasterVolume);
        float menu = GetCurrentOrSaved(mixerSettings != null ? mixerSettings.MenuMusicVolume : -1f, GameAudioMixerSettings.MenuMusicVolumeKey, defaultMenuMusicVolume);
        float level = GetCurrentOrSaved(mixerSettings != null ? mixerSettings.LevelMusicVolume : -1f, GameAudioMixerSettings.LevelMusicVolumeKey, defaultLevelMusicVolume);
        float endGame = GetCurrentOrSaved(mixerSettings != null ? mixerSettings.EndGameMusicVolume : -1f, GameAudioMixerSettings.EndGameMusicVolumeKey, defaultEndGameMusicVolume);
        float video = GetCurrentOrSaved(mixerSettings != null ? mixerSettings.VideoVolume : -1f, GameAudioMixerSettings.VideoVolumeKey, defaultVideoVolume);
        float vfx = GetCurrentOrSaved(mixerSettings != null ? mixerSettings.VFXVolume : -1f, GameAudioMixerSettings.VFXVolumeKey, defaultVFXVolume);

        SetSliderWithoutNotify(masterVolumeSlider, master);
        SetSliderWithoutNotify(menuMusicSlider, menu);
        SetSliderWithoutNotify(levelMusicSlider, level);
        SetSliderWithoutNotify(endGameMusicSlider, endGame);
        SetSliderWithoutNotify(videoVolumeSlider, video);
        SetSliderWithoutNotify(vfxVolumeSlider, vfx);

        UpdatePercentageText(masterVolumeValueText, master);
        UpdatePercentageText(menuMusicValueText, menu);
        UpdatePercentageText(levelMusicValueText, level);
        UpdatePercentageText(endGameMusicValueText, endGame);
        UpdatePercentageText(videoVolumeValueText, video);
        UpdatePercentageText(vfxVolumeValueText, vfx);
    }


    // LISTENERS
    private void SetupListeners()
    {
        if (masterVolumeSlider != null)
            masterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeChanged);

        if (menuMusicSlider != null)
            menuMusicSlider.onValueChanged.AddListener(OnMenuMusicChanged);

        if (levelMusicSlider != null)
            levelMusicSlider.onValueChanged.AddListener(OnLevelMusicChanged);

        if (endGameMusicSlider != null)
            endGameMusicSlider.onValueChanged.AddListener(OnEndGameMusicChanged);

        if (videoVolumeSlider != null)
            videoVolumeSlider.onValueChanged.AddListener(OnVideoVolumeChanged);

        if (vfxVolumeSlider != null)
            vfxVolumeSlider.onValueChanged.AddListener(OnVFXVolumeChanged);
    }


    // MASTER
    private void OnMasterVolumeChanged(float value)
    {
        value = Mathf.Clamp01(value);

        if (mixerSettings != null)
            mixerSettings.SetMasterVolume(value);
        else
            SaveFallback(GameAudioMixerSettings.MasterVolumeKey, value);


        UpdatePercentageText(masterVolumeValueText, value);
    }


    // MENU MUSIC
    private void OnMenuMusicChanged(float value)
    {
        value = Mathf.Clamp01(value);

        if (mixerSettings != null)
            mixerSettings.SetMenuMusicVolume(value);
        else
            SaveFallback(GameAudioMixerSettings.MenuMusicVolumeKey, value);


        UpdatePercentageText(menuMusicValueText, value);
    }


    // LEVEL MUSIC
    private void OnLevelMusicChanged(float value)
    {
        value = Mathf.Clamp01(value);

        if (mixerSettings != null)
            mixerSettings.SetLevelMusicVolume(value);
        else
            SaveFallback(GameAudioMixerSettings.LevelMusicVolumeKey, value);


        /*  
          * Keep AudioManager's public compatibility state synchronized.  
          * In the mixer version AudioManager no longer multiplies the  
            * AudioSource by this user value, so this does NOT double-volume.
        */

        if (AudioManager.Instance != null)
            AudioManager.Instance.SetLevelMusicVolume(value);

        UpdatePercentageText(levelMusicValueText, value);
    }


    // END GAME MUSIC

    private void OnEndGameMusicChanged(float value)
    {
        value = Mathf.Clamp01(value);

        if (mixerSettings != null)
            mixerSettings.SetEndGameMusicVolume(value);
        else
            SaveFallback(GameAudioMixerSettings.EndGameMusicVolumeKey, value);


        UpdatePercentageText(endGameMusicValueText, value);
    }


    // VIDEO

    private void OnVideoVolumeChanged(float value)
    {
        value = Mathf.Clamp01(value);

        if (mixerSettings != null)
            mixerSettings.SetVideoVolume(value);
        else
            SaveFallback(GameAudioMixerSettings.VideoVolumeKey, value);

        /*  
          * Keep LevelVideoIntroManager's public compatibility state  
          * synchronized. Its AudioSource remains at full/base volume  
          * while the mixer applies the user's setting.  
        */

        if (LevelVideoIntroManager.Instance != null)
            LevelVideoIntroManager.Instance.SetVideoVolume(value);

        UpdatePercentageText(videoVolumeValueText, value);
    }


    // VFX

    private void OnVFXVolumeChanged(float value)
    {
        value = Mathf.Clamp01(value);

        if (mixerSettings != null)
            mixerSettings.SetVFXVolume(value);
        else
            SaveFallback(GameAudioMixerSettings.VFXVolumeKey, value);


        UpdatePercentageText(vfxVolumeValueText, value);
    }


    // HELPERS

    private static float GetCurrentOrSaved(float currentValue, string key, float defaultValue)
    {
        if (currentValue >= 0f)
            return Mathf.Clamp01(currentValue);

        return Mathf.Clamp01(PlayerPrefs.GetFloat(key, Mathf.Clamp01(defaultValue)));
    }


    private static void SaveFallback(string key, float value)
    {
        PlayerPrefs.SetFloat(key, Mathf.Clamp01(value));

        PlayerPrefs.Save();
    }


    private static void SetSliderWithoutNotify(Slider slider, float value)
    {
        if (slider == null)
            return;

        slider.SetValueWithoutNotify(Mathf.Clamp01(value));
    }


    private static void UpdatePercentageText(TMP_Text text, float value)
    {
        if (text == null)
            return;

        int percent = Mathf.RoundToInt(Mathf.Clamp01(value) * 100f);

        text.text = $"{percent} %";
    }


    private static void ConfigureSlider(Slider slider)
    {
        if (slider == null)
            return;

        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.wholeNumbers = false;
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
    }


    // CLEANUP

    private void OnDestroy()
    {
        if (masterVolumeSlider != null)
            masterVolumeSlider.onValueChanged.RemoveListener(OnMasterVolumeChanged);

        if (menuMusicSlider != null)
            menuMusicSlider.onValueChanged.RemoveListener(OnMenuMusicChanged);

        if (levelMusicSlider != null)
            levelMusicSlider.onValueChanged.RemoveListener(OnLevelMusicChanged);

        if (endGameMusicSlider != null)
            endGameMusicSlider.onValueChanged.RemoveListener(OnEndGameMusicChanged);

        if (videoVolumeSlider != null)
            videoVolumeSlider.onValueChanged.RemoveListener(OnVideoVolumeChanged);

        if (vfxVolumeSlider != null)
            vfxVolumeSlider.onValueChanged.RemoveListener(OnVFXVolumeChanged);
    }
}
