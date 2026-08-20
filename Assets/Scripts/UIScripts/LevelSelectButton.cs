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

    private bool subscribed;


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
                FindAnyObjectByType<LevelLoader>();
        }
    }


    // ==================================================
    // SUBSCRIBE
    // ==================================================

    private void Subscribe()
    {
        if (subscribed)
            return;


        if (levelLoader == null)
            return;


        levelLoader.LevelLoaded +=
            HandleLevelLoaded;


        subscribed =
            true;
    }


    // ==================================================
    // LEVEL LOADED
    // ==================================================

    private void HandleLevelLoaded(
        string loadedSceneName)
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


        // ----------------------------------------------
        // INVALID NAME
        // ----------------------------------------------

        if (string.IsNullOrWhiteSpace(
                sceneName))
        {
            button.interactable =
                false;

            return;
        }


        // ----------------------------------------------
        // SCENE DOESN'T EXIST
        // ----------------------------------------------

        if (disableIfSceneMissing &&
            !Application.CanStreamedLevelBeLoaded(
                sceneName))
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


        // ----------------------------------------------
        // FIND CURRENT LEVEL
        // ----------------------------------------------

        string currentSceneName =
            string.Empty;


        ResolveLoader();


        if (levelLoader != null)
        {
            currentSceneName =
                levelLoader.CurrentLevelSceneName;
        }


        /*
         * Safety fallback.
         */
        if (string.IsNullOrWhiteSpace(
                currentSceneName))
        {
            currentSceneName =
                SceneManager
                    .GetActiveScene()
                    .name;
        }


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
    // LOAD LEVEL
    // ==================================================

    private void LoadSelectedLevel()
    {
        if (button != null &&
            !button.interactable)
        {
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


        if (string.IsNullOrWhiteSpace(
                sceneName))
        {
            Debug.LogError(
                "LevelSelectButton: Scene name is empty.",
                this
            );

            return;
        }


        /*
         * FAST transition specifically for
         * the Levels menu.
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


        if (subscribed &&
            levelLoader != null)
        {
            levelLoader.LevelLoaded -=
                HandleLevelLoaded;
        }


        subscribed =
            false;
    }
}