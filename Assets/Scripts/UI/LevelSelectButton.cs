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

    [Tooltip(
        "Exact Scene name, for example Level_01."
    )]
    [SerializeField]
    private string sceneName;


    // ==================================================
    // OPTIONS
    // ==================================================

    [Header("Options")]

    [Tooltip(
        "Disable this button when its level " +
        "is already the active scene."
    )]
    [SerializeField]
    private bool disableIfCurrentLevel = true;


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


    // ==================================================
    // AWAKE
    // ==================================================

    private void Awake()
    {
        button =
            GetComponent<Button>();


        if (levelLoader == null)
        {
            levelLoader =
                FindAnyObjectByType<LevelLoader>();
        }


        button.onClick.AddListener(
            LoadSelectedLevel
        );
    }


    // ==================================================
    // START
    // ==================================================

    private void Start()
    {
        UpdateButtonState();
    }


    // ==================================================
    // ENABLE
    // ==================================================

    private void OnEnable()
    {
        /*
         * Awake may not have run yet in some
         * editor setup situations.
         */
        if (button == null)
        {
            button =
                GetComponent<Button>();
        }


        UpdateButtonState();
    }


    // ==================================================
    // UPDATE BUTTON
    // ==================================================

    private void UpdateButtonState()
    {
        if (button == null)
            return;


        if (string.IsNullOrWhiteSpace(sceneName))
        {
            button.interactable = false;

            return;
        }


        if (!disableIfCurrentLevel)
        {
            button.interactable = true;

            return;
        }


        /*
         * Get the currently playing scene.
         */
        Scene currentScene =
            SceneManager.GetActiveScene();


        bool isCurrentLevel =
            string.Equals(
                currentScene.name,
                sceneName,
                System.StringComparison.OrdinalIgnoreCase
            );


        /*
         * Current level cannot be selected.
         */
        button.interactable =
            !isCurrentLevel;
    }


    // ==================================================
    // LOAD
    // ==================================================

    private void LoadSelectedLevel()
    {
        if (button != null &&
            !button.interactable)
        {
            return;
        }


        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogError(
                "LevelSelectButton: Scene Name is empty.",
                this
            );

            return;
        }


        if (levelLoader == null)
        {
            levelLoader =
                FindAnyObjectByType<LevelLoader>();
        }


        if (levelLoader == null)
        {
            Debug.LogError(
                "LevelSelectButton: LevelLoader not found.",
                this
            );

            return;
        }


        Debug.Log(
            "Level menu selected: " +
            sceneName,
            this
        );


        /*
         * LevelLoader marks gameplay as started
         * BEFORE changing scenes.
         *
         * Therefore the target scene does not
         * show Start_Menu.
         */
        levelLoader.LoadLevel(
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
    }
}