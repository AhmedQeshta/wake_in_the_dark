using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AudioSettingsUI : MonoBehaviour
{
    // ==================================================
    // PLAYER PREF KEYS
    // ==================================================

    private const string MenuMusicVolumeKey = "Settings.MenuMusicVolume";

    private const string LevelMusicVolumeKey = "Settings.LevelMusicVolume";

    private const string VideoVolumeKey = "Settings.LevelIntroVideoVolume";


    // ==================================================
    // MENU MUSIC
    // ==================================================

    [Header("Menu Music")]

    [Tooltip("AudioSource used by UIManager for menu music.")]
    [SerializeField] private AudioSource menuMusicSource;

    [SerializeField] private Slider menuMusicSlider;

    [SerializeField] private TMP_Text menuMusicValueText;


    // ==================================================
    // LEVEL MUSIC
    // ==================================================

    [Header("Level Music")]

    [SerializeField] private Slider levelMusicSlider;

    [SerializeField] private TMP_Text levelMusicValueText;


    // ==================================================
    // VIDEO AUDIO
    // ==================================================

    [Header("Video Audio")]

    [Tooltip("Volume slider for the videos shown before each level.")]
    [SerializeField] private Slider videoVolumeSlider;

    [Tooltip("Percentage text shown beside the video volume slider.")]
    [SerializeField] private TMP_Text videoVolumeValueText;


    // ==================================================
    // AWAKE
    // ==================================================

    private void Awake()
    {
        ConfigureSlider(menuMusicSlider);
        ConfigureSlider(levelMusicSlider);
        ConfigureSlider(videoVolumeSlider);

        LoadValues();

        SetupListeners();


        /*
         * Menu music can be applied immediately because
         * its AudioSource is referenced directly.
         */
        ApplyMenuMusicVolume(menuMusicSlider != null ? menuMusicSlider.value : GetSavedMenuVolume());
    }


    // ==================================================
    // START
    // ==================================================

    private void Start()
    {
        ApplyLevelMusicVolume(levelMusicSlider != null ? levelMusicSlider.value : GetSavedLevelVolume());
        ApplyVideoVolume(videoVolumeSlider != null ? videoVolumeSlider.value : GetSavedVideoVolume());
    }


    // ==================================================
    // LOAD VALUES
    // ==================================================

    private void LoadValues()
    {
        float menuVolume = GetSavedMenuVolume();

        float levelVolume = GetSavedLevelVolume();

        float videoVolume = GetSavedVideoVolume();


        if (menuMusicSlider != null)
            menuMusicSlider.SetValueWithoutNotify(menuVolume);

        if (levelMusicSlider != null)
            levelMusicSlider.SetValueWithoutNotify(levelVolume);

        if (videoVolumeSlider != null)
            videoVolumeSlider.SetValueWithoutNotify(videoVolume);


        UpdatePercentageText(menuMusicValueText, menuVolume);
        UpdatePercentageText(levelMusicValueText, levelVolume);
        UpdatePercentageText(videoVolumeValueText, videoVolume);
    }


    // ==================================================
    // GET SAVED MENU VOLUME
    // ==================================================

    private float GetSavedMenuVolume()
    {
        /*
         * On the first launch, keep the current volume
         * configured on the menu AudioSource.
         */
        float defaultValue = menuMusicSource != null ? menuMusicSource.volume : 1f;


        return Mathf.Clamp01(PlayerPrefs.GetFloat(MenuMusicVolumeKey, defaultValue));
    }


    // ==================================================
    // GET SAVED LEVEL VOLUME
    // ==================================================

    private float GetSavedLevelVolume()
    {
        if (AudioManager.Instance != null)
            return Mathf.Clamp01(AudioManager.Instance.LevelMusicVolume);

        return Mathf.Clamp01(PlayerPrefs.GetFloat(LevelMusicVolumeKey, 1f));
    }


    // ==================================================
    // GET SAVED VIDEO VOLUME
    // ==================================================

    private float GetSavedVideoVolume()
    {
        if (LevelVideoIntroManager.Instance != null)
            return Mathf.Clamp01(LevelVideoIntroManager.Instance.VideoVolume);


        return Mathf.Clamp01(PlayerPrefs.GetFloat(VideoVolumeKey, 1f));
    }


    // ==================================================
    // LISTENERS
    // ==================================================

    private void SetupListeners()
    {
        if (menuMusicSlider != null)
            menuMusicSlider.onValueChanged.AddListener(OnMenuMusicSliderChanged);

        if (levelMusicSlider != null)
            levelMusicSlider.onValueChanged.AddListener(OnLevelMusicSliderChanged);

        if (videoVolumeSlider != null)
            videoVolumeSlider.onValueChanged.AddListener(OnVideoVolumeSliderChanged);
    }


    // ==================================================
    // MENU MUSIC
    // ==================================================

    private void OnMenuMusicSliderChanged(float value)
    {
        ApplyMenuMusicVolume(value);
        PlayerPrefs.SetFloat(MenuMusicVolumeKey, Mathf.Clamp01(value));
        PlayerPrefs.Save();
    }


    private void ApplyMenuMusicVolume(float value)
    {
        value = Mathf.Clamp01(value);

        if (menuMusicSource != null)
            menuMusicSource.volume = value;

        UpdatePercentageText(menuMusicValueText, value);
    }


    // ==================================================
    // LEVEL MUSIC
    // ==================================================

    private void OnLevelMusicSliderChanged(float value)
    {
        ApplyLevelMusicVolume(value);
    }


    private void ApplyLevelMusicVolume(float value)
    {
        value = Mathf.Clamp01(value);


        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetLevelMusicVolume(value);
        }
        else
        {
            /*
             * Fallback if AudioManager is not ready.
             */
            PlayerPrefs.SetFloat(LevelMusicVolumeKey, value);
            PlayerPrefs.Save();
        }


        UpdatePercentageText(levelMusicValueText, value);
    }


    // ==================================================
    // VIDEO AUDIO
    // ==================================================

    private void OnVideoVolumeSliderChanged(float value)
    {
        ApplyVideoVolume(value);
    }


    private void ApplyVideoVolume(float value)
    {
        value = Mathf.Clamp01(value);


        /*
        * This method:
        * - changes the current VideoPlayer AudioSource volume
        * - saves the value to PlayerPrefs
        * - and on the else Fallback if LevelVideoIntroManager has not completed Awake yet.
        */

        if (LevelVideoIntroManager.Instance != null)
        {
            LevelVideoIntroManager.Instance.SetVideoVolume(value);
        }
        else
        {
            PlayerPrefs.SetFloat(VideoVolumeKey, value);
            PlayerPrefs.Save();
        }


        UpdatePercentageText(videoVolumeValueText, value);
    }


    // ==================================================
    // PERCENTAGE TEXT
    // ==================================================

    private void UpdatePercentageText(TMP_Text text, float value)
    {
        if (text == null) return;

        int percent = Mathf.RoundToInt(Mathf.Clamp01(value) * 100f);

        text.text = $"{percent} %";
    }


    // ==================================================
    // SLIDER SETUP
    // ==================================================

    private void ConfigureSlider(Slider slider)
    {
        if (slider == null)
            return;

        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.wholeNumbers = false;
    }


    // ==================================================
    // CLEANUP
    // ==================================================

    private void OnDestroy()
    {
        if (menuMusicSlider != null)
            menuMusicSlider.onValueChanged.RemoveListener(OnMenuMusicSliderChanged);

        if (levelMusicSlider != null)
            levelMusicSlider.onValueChanged.RemoveListener(OnLevelMusicSliderChanged);

        if (videoVolumeSlider != null)
            videoVolumeSlider.onValueChanged.RemoveListener(OnVideoVolumeSliderChanged);

    }
}
