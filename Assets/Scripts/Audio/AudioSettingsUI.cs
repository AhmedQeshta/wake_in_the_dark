using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AudioSettingsUI : MonoBehaviour
{
    // ==================================================
    // MENU MUSIC
    // ==================================================

    [Header("Menu Music")]

    [Tooltip(
        "Drag the AudioSource used by UIManager for menu music. " +
        "In your Bootstrap scene this AudioSource is on UI_Manager."
    )]
    [SerializeField]
    private AudioSource menuMusicSource;


    [SerializeField]
    private Slider menuMusicSlider;


    [SerializeField]
    private TMP_Text menuMusicValueText;


    // ==================================================
    // LEVEL MUSIC
    // ==================================================

    [Header("Level Music")]

    [SerializeField]
    private Slider levelMusicSlider;


    [SerializeField]
    private TMP_Text levelMusicValueText;


    // ==================================================
    // PLAYER PREFS
    // ==================================================

    private const string
        MenuMusicVolumeKey =
            "Settings.MenuMusicVolume";


    // ==================================================
    // STATE
    // ==================================================

    private bool initialized;


    // ==================================================
    // AWAKE
    // ==================================================

    private void Awake()
    {
        ConfigureSlider(
            menuMusicSlider
        );


        ConfigureSlider(
            levelMusicSlider
        );


        LoadValues();


        SetupListeners();


        ApplyMenuMusicVolume(
            menuMusicSlider != null
                ? menuMusicSlider.value
                : GetSavedMenuVolume()
        );


        initialized =
            true;
    }


    // ==================================================
    // START
    // ==================================================

    private void Start()
    {
        /*
         * Awake order between this component and
         * AudioManager is not guaranteed.
         *
         * By Start(), AudioManager should exist.
         */
        ApplyLevelMusicVolume(
            levelMusicSlider != null
                ? levelMusicSlider.value
                : GetSavedLevelVolume()
        );
    }


    // ==================================================
    // LOAD
    // ==================================================

    private void LoadValues()
    {
        float menuVolume =
            GetSavedMenuVolume();


        float levelVolume =
            GetSavedLevelVolume();


        if (menuMusicSlider != null)
        {
            menuMusicSlider.SetValueWithoutNotify(
                menuVolume
            );
        }


        if (levelMusicSlider != null)
        {
            levelMusicSlider.SetValueWithoutNotify(
                levelVolume
            );
        }


        UpdatePercentageText(
            menuMusicValueText,
            menuVolume
        );


        UpdatePercentageText(
            levelMusicValueText,
            levelVolume
        );
    }


    // ==================================================
    // DEFAULT VALUES
    // ==================================================

    private float GetSavedMenuVolume()
    {
        /*
         * First launch:
         * keep your current menu AudioSource volume.
         *
         * Your Bootstrap currently uses a low menu
         * music volume, so we do not force it to 100%.
         */
        float defaultValue =
            menuMusicSource != null
                ? menuMusicSource.volume
                : 1f;


        return Mathf.Clamp01(
            PlayerPrefs.GetFloat(
                MenuMusicVolumeKey,
                defaultValue
            )
        );
    }


    private float GetSavedLevelVolume()
    {
        if (AudioManager.Instance != null)
        {
            return
                AudioManager.Instance
                    .LevelMusicVolume;
        }


        return Mathf.Clamp01(
            PlayerPrefs.GetFloat(
                "Settings.LevelMusicVolume",
                1f
            )
        );
    }


    // ==================================================
    // LISTENERS
    // ==================================================

    private void SetupListeners()
    {
        if (menuMusicSlider != null)
        {
            menuMusicSlider.onValueChanged
                .AddListener(
                    OnMenuMusicSliderChanged
                );
        }


        if (levelMusicSlider != null)
        {
            levelMusicSlider.onValueChanged
                .AddListener(
                    OnLevelMusicSliderChanged
                );
        }
    }


    // ==================================================
    // MENU MUSIC SLIDER
    // ==================================================

    private void OnMenuMusicSliderChanged(
        float value)
    {
        ApplyMenuMusicVolume(
            value
        );


        PlayerPrefs.SetFloat(
            MenuMusicVolumeKey,
            Mathf.Clamp01(value)
        );


        PlayerPrefs.Save();
    }


    private void ApplyMenuMusicVolume(
        float value)
    {
        value =
            Mathf.Clamp01(
                value
            );


        if (menuMusicSource != null)
        {
            menuMusicSource.volume =
                value;
        }


        UpdatePercentageText(
            menuMusicValueText,
            value
        );
    }


    // ==================================================
    // LEVEL MUSIC SLIDER
    // ==================================================

    private void OnLevelMusicSliderChanged(
        float value)
    {
        ApplyLevelMusicVolume(
            value
        );
    }


    private void ApplyLevelMusicVolume(
        float value)
    {
        value =
            Mathf.Clamp01(
                value
            );


        if (AudioManager.Instance != null)
        {
            AudioManager.Instance
                .SetLevelMusicVolume(
                    value
                );
        }
        else
        {
            /*
             * Safety fallback if AudioManager has not
             * completed Awake yet.
             */
            PlayerPrefs.SetFloat(
                "Settings.LevelMusicVolume",
                value
            );


            PlayerPrefs.Save();
        }


        UpdatePercentageText(
            levelMusicValueText,
            value
        );
    }


    // ==================================================
    // PERCENT TEXT
    // ==================================================

    private void UpdatePercentageText(
        TMP_Text text,
        float value)
    {
        if (text == null)
            return;


        int percent =
            Mathf.RoundToInt(
                Mathf.Clamp01(value) *
                100f
            );


        text.text =
            percent +
            "%";
    }


    // ==================================================
    // SLIDER SETUP
    // ==================================================

    private void ConfigureSlider(
        Slider slider)
    {
        if (slider == null)
            return;


        slider.minValue =
            0f;


        slider.maxValue =
            1f;


        slider.wholeNumbers =
            false;
    }


    // ==================================================
    // CLEANUP
    // ==================================================

    private void OnDestroy()
    {
        if (menuMusicSlider != null)
        {
            menuMusicSlider.onValueChanged
                .RemoveListener(
                    OnMenuMusicSliderChanged
                );
        }


        if (levelMusicSlider != null)
        {
            levelMusicSlider.onValueChanged
                .RemoveListener(
                    OnLevelMusicSliderChanged
                );
        }
    }
}