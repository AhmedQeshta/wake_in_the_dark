using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class LevelSelectButton : MonoBehaviour
{
    // ==================================================
    // LEVEL
    // ==================================================

    [Header("Level")]

    [SerializeField]
    private string sceneName;


    // ==================================================
    // LOCK VISUAL
    // ==================================================

    [Header("Lock Visual")]

    [Tooltip(
        "Optional separate object shown while this level is locked. " +
        "Do not assign the BTN GameObject itself."
    )]
    [SerializeField]
    private GameObject lockedVisual;


    // ==================================================
    // OPTIONS
    // ==================================================

    [Header("Options")]

    [SerializeField]
    private bool disableIfCurrentLevel =
        true;


    [SerializeField]
    private bool disableIfSceneMissing =
        true;


    // ==================================================
    // REFERENCES
    // ==================================================

    [Header("References")]

    [SerializeField]
    private LevelLoader levelLoader;


    // ==================================================
    // COMPONENTS
    // ==================================================

    private Button button;

    private bool loaderSubscribed;

    private bool progressSubscribed;


    // ==================================================
    // AWAKE
    // ==================================================

    private void Awake()
    {
        button =
            GetComponent<Button>();


        button.onClick.AddListener(
            LoadSelectedLevel
        );
    }


    // ==================================================
    // START
    // ==================================================

    private void Start()
    {
        ResolveLoader();


        Subscribe();


        UpdateButtonState();
    }


    // ==================================================
    // LOADER
    // ==================================================

    private void ResolveLoader()
    {
        if (levelLoader != null)
            return;


        levelLoader =
            LevelLoader.Instance;


        if (levelLoader == null)
        {
            levelLoader =
                FindAnyObjectByType
                    <LevelLoader>();
        }
    }


    // ==================================================
    // SUBSCRIBE
    // ==================================================

    private void Subscribe()
    {
        if (!loaderSubscribed)
        {
            ResolveLoader();


            if (levelLoader != null)
            {
                levelLoader.LevelLoaded +=
                    HandleLevelLoaded;


                loaderSubscribed =
                    true;
            }
        }


        if (!progressSubscribed &&
            LevelProgressManager.Instance != null)
        {
            LevelProgressManager.Instance
                .ProgressChanged +=
                HandleProgressChanged;


            progressSubscribed =
                true;
        }
    }


    // ==================================================
    // EVENTS
    // ==================================================

    private void HandleLevelLoaded(
        string loadedSceneName)
    {
        UpdateButtonState();
    }


    private void HandleProgressChanged()
    {
        UpdateButtonState();
    }


    // ==================================================
    // BUTTON STATE
    // ==================================================

    public void UpdateButtonState()
    {
        if (button == null)
            return;


        Subscribe();


        if (string.IsNullOrWhiteSpace(
                sceneName))
        {
            SetLockedVisual(
                false
            );


            button.interactable =
                false;


            return;
        }


        if (disableIfSceneMissing &&
            !Application.CanStreamedLevelBeLoaded(
                sceneName))
        {
            SetLockedVisual(
                false
            );


            button.interactable =
                false;


            return;
        }


        LevelProgressManager progress =
            LevelProgressManager.Instance;


        bool isUnlocked =
            progress == null ||
            progress.IsSceneUnlocked(
                sceneName
            );


        SetLockedVisual(
            !isUnlocked
        );


        if (!isUnlocked)
        {
            button.interactable =
                false;


            return;
        }


        if (!disableIfCurrentLevel)
        {
            button.interactable =
                true;


            return;
        }


        string currentSceneName =
            GetCurrentLevelSceneName();


        bool isCurrent =
            string.Equals(
                currentSceneName,
                sceneName,
                System.StringComparison.OrdinalIgnoreCase
            );


        button.interactable =
            !isCurrent;
    }


    // ==================================================
    // CURRENT LEVEL
    // ==================================================

    private string GetCurrentLevelSceneName()
    {
        ResolveLoader();


        if (levelLoader != null &&
            !string.IsNullOrWhiteSpace(
                levelLoader.CurrentLevelSceneName))
        {
            return
                levelLoader.CurrentLevelSceneName;
        }


        return
            SceneManager
                .GetActiveScene()
                .name;
    }


    // ==================================================
    // LOCK VISUAL
    // ==================================================

    private void SetLockedVisual(
        bool locked)
    {
        if (lockedVisual == null)
            return;


        lockedVisual.SetActive(
            locked
        );
    }


    // ==================================================
    // LOAD SELECTED LEVEL
    // ==================================================

    private void LoadSelectedLevel()
    {
        if (button != null &&
            !button.interactable)
        {
            return;
        }


        if (string.IsNullOrWhiteSpace(
                sceneName))
        {
            Debug.LogError(
                "LevelSelectButton: Scene name is empty.",
                this
            );

            return;
        }


        LevelProgressManager progress =
            LevelProgressManager.Instance;


        if (progress != null &&
            !progress.IsSceneUnlocked(
                sceneName))
        {
            Debug.LogWarning(
                "LevelSelectButton: Level is locked: " +
                sceneName,
                this
            );


            UpdateButtonState();


            return;
        }


        ResolveLoader();


        if (levelLoader == null)
        {
            Debug.LogError(
                "LevelSelectButton: LevelLoader not found.",
                this
            );

            return;
        }


        // ----------------------------------------------
        // NEW: VIDEO BEFORE MENU LEVEL LOAD
        // ----------------------------------------------

        LevelVideoIntroManager videoIntro =
            LevelVideoIntroManager.Instance;


        if (videoIntro != null)
        {
            videoIntro.RequestLevelFromMenu(
                sceneName
            );


            return;
        }


        /*
         * Safe fallback to the old behavior.
         */
        levelLoader.LoadLevelFromMenu(
            sceneName
        );
    }


    // ==================================================
    // CLEANUP
    // ==================================================

    private void OnDestroy()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(
                LoadSelectedLevel
            );
        }


        if (loaderSubscribed &&
            levelLoader != null)
        {
            levelLoader.LevelLoaded -=
                HandleLevelLoaded;
        }


        if (progressSubscribed &&
            LevelProgressManager.Instance != null)
        {
            LevelProgressManager.Instance
                .ProgressChanged -=
                HandleProgressChanged;
        }


        loaderSubscribed =
            false;


        progressSubscribed =
            false;
    }
}
