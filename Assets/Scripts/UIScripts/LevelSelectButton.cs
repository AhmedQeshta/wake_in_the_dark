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

    [Tooltip("Optional object shown while this level is locked. Example: a lock icon or dark overlay.")]
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


        // ----------------------------------------------
        // INVALID NAME
        // ----------------------------------------------

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


        // ----------------------------------------------
        // SCENE DOES NOT EXIST
        // ----------------------------------------------

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


        // ----------------------------------------------
        // PROGRESSION LOCK
        // ----------------------------------------------

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


        // ----------------------------------------------
        // CURRENT LEVEL
        // ----------------------------------------------

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
                System.StringComparison
                    .OrdinalIgnoreCase
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
                levelLoader
                    .CurrentLevelSceneName;
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
    // LOAD LEVEL
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


        // ----------------------------------------------
        // RECHECK PROGRESSION
        // ----------------------------------------------

        LevelProgressManager progress =
            LevelProgressManager.Instance;


        if (progress != null &&
            !progress.IsSceneUnlocked(
                sceneName))
        {
            UpdateButtonState();
            return;
        }


        // ----------------------------------------------
        // LOAD
        // ----------------------------------------------

        ResolveLoader();


        if (levelLoader == null)
            return;


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
