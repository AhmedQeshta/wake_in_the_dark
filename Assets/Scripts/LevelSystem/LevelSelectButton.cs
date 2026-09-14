using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class LevelSelectButton : MonoBehaviour
{
    // LEVEL

    [Header("Level")]

    [SerializeField] private string sceneName;


    // LOCK UI

    [Header("Lock UI")]

    [Tooltip("Root GameObject for the locked visual. For your current card hierarchy assign BTN/bg_Locked_Icon. If left empty, the script can find it automatically by name.")]
    [SerializeField] private GameObject lockedRoot;


    [Tooltip("Automatically find a child named bg_Locked_Icon when Locked Root is empty.")]
    [SerializeField] private bool autoFindLockedRoot = true;


    [SerializeField] private string lockedRootName = "bg_Locked_Icon";


    // OPTIONS

    [Header("Options")]

    [Tooltip("During gameplay, disable this card if it represents the currently loaded level. " + "On the INITIAL Bootstrap menu, the already-loaded Level_01 remains selectable.")]
    [SerializeField] private bool disableIfCurrentLevel = true;


    [SerializeField] private bool disableIfSceneMissing = true;


    [Tooltip("When selecting a different level from the Levels menu, use LevelVideoIntroManager first when available.")]
    [SerializeField] private bool useLevelVideoIntroManager = true;


    // REFERENCES

    [Header("References")]

    [SerializeField] private LevelLoader levelLoader;


    [SerializeField] private LevelProgressManager levelProgressManager;


    // COMPONENTS

    private Button button;

    private bool loaderSubscribed;
    private bool progressSubscribed;


    // AWAKE
    private void Awake()
    {
        button = GetComponent<Button>();

        ResolveLockedRoot();

        button.onClick.AddListener(LoadSelectedLevel);
    }


    // START

    private void Start()
    {
        ResolveReferences();
        Subscribe();
        UpdateButtonState();
    }


    private void OnEnable()
    {
        /*  
          * The Levels menu can be opened after progression changed. 
          * Refresh every time this card becomes active.  
         */
        if (button != null)
        {
            ResolveReferences();
            Subscribe();
            UpdateButtonState();
        }
    }


    // REFERENCES

    private void ResolveReferences()
    {
        ResolveLoader();
        ResolveProgressManager();
        ResolveLockedRoot();
    }


    private void ResolveLoader()
    {
        if (levelLoader != null)
            return;

        levelLoader = LevelLoader.Instance;

        if (levelLoader == null)
            levelLoader = FindAnyObjectByType<LevelLoader>();
    }


    private void ResolveProgressManager()
    {
        if (levelProgressManager != null)
            return;

        levelProgressManager = LevelProgressManager.Instance;

        if (levelProgressManager == null)
            levelProgressManager = FindAnyObjectByType<LevelProgressManager>();
    }


    private void ResolveLockedRoot()
    {
        if (lockedRoot != null || !autoFindLockedRoot || string.IsNullOrWhiteSpace(lockedRootName))
            return;

        Transform found = FindChildRecursive(transform, lockedRootName);

        if (found != null)
            lockedRoot = found.gameObject;
    }


    // SUBSCRIBE

    private void Subscribe()
    {
        if (!loaderSubscribed && levelLoader != null)
        {
            levelLoader.LevelLoaded += HandleLevelLoaded;
            loaderSubscribed = true;
        }

        if (!progressSubscribed && levelProgressManager != null)
        {
            levelProgressManager.ProgressChanged += HandleProgressChanged;
            progressSubscribed = true;
        }
    }


    private void HandleLevelLoaded(string loadedSceneName)
    {
        UpdateButtonState();
    }


    private void HandleProgressChanged()
    {
        UpdateButtonState();
    }


    // BUTTON STATE

    public void UpdateButtonState()
    {
        if (button == null)
            return;

        ResolveReferences();

        // INVALID SCENE NAME 
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            SetLockedVisual(true);
            button.interactable = false;
            return;
        }

        // SCENE EXISTS 
        if (disableIfSceneMissing && !Application.CanStreamedLevelBeLoaded(sceneName))
        {
            SetLockedVisual(true);
            button.interactable = false;
            return;
        }

        // PROGRESSION LOCK 
        bool unlocked = IsSceneUnlocked();

        /*  * Lock visual represents progression ONLY.  *  * A current level can be temporarily non-interactable without  * showing the padlock, because it is still genuinely unlocked.  */
        SetLockedVisual(!unlocked);

        if (!unlocked)
        {
            button.interactable = false;
            return;
        }

        // CURRENT LEVEL 
        if (!disableIfCurrentLevel)
        {
            button.interactable = true;
            return;
        }

        string currentSceneName = levelLoader != null ? levelLoader.CurrentLevelSceneName : string.Empty;

        if (string.IsNullOrWhiteSpace(currentSceneName))
            currentSceneName = SceneManager.GetActiveScene().name;

        bool isCurrent = string.Equals(currentSceneName, sceneName, StringComparison.OrdinalIgnoreCase);

        /* 
          * Bootstrap starts with Level_01 already loaded behind the 
          * Start Menu. That does NOT mean the player is currently 
          * playing Level_01. 
          * Therefore Level_01 stays selectable on the initial menu. 
        */
        bool initialMainMenu = UIManager.Instance != null && UIManager.Instance.IsInitialMainMenu;

        if (isCurrent && initialMainMenu)
        {
            button.interactable = true;
            return;
        }

        button.interactable = !isCurrent;
    }


    // UNLOCK CHECK

    private bool IsSceneUnlocked()
    {
        if (levelProgressManager != null)
            return levelProgressManager.IsSceneUnlocked(sceneName);

        return string.Equals(sceneName, "Level_01", StringComparison.OrdinalIgnoreCase);
    }


    // LOCK VISUAL
    private void SetLockedVisual(bool locked)
    {
        ResolveLockedRoot();

        if (lockedRoot != null && lockedRoot.activeSelf != locked)
            lockedRoot.SetActive(locked);
    }


    // LOAD LEVEL

    private void LoadSelectedLevel()
    {
        ResolveReferences();

        /* 
         * Never rely only on Button.interactable.  
         * Re-check progression at click time too. 
        */
        if (!IsSceneUnlocked())
        {
            UpdateButtonState();
            return;
        }


        if (levelLoader == null || string.IsNullOrWhiteSpace(sceneName) || (button != null && !button.interactable))
            return;

        bool isCurrent = string.Equals(levelLoader.CurrentLevelSceneName, sceneName, StringComparison.OrdinalIgnoreCase);

        bool initialMainMenu = UIManager.Instance != null && UIManager.Instance.IsInitialMainMenu;

        /* 
          * Level_01 is already loaded behind Bootstrap. 
          * Starting it from the initial Levels menu should not try to  
          * load a second copy of Level_01. 
        */
        if (isCurrent && initialMainMenu)
        {
            if (LevelVideoIntroManager.Instance != null)
            {
                LevelVideoIntroManager.Instance.RequestStartLevel(sceneName);
                return;
            }

            UIManager.Instance?.StartAlreadyLoadedLevelFromLevelsMenu();
            return;
        }

        // DIFFERENT LEVEL SELECTED 
        if (useLevelVideoIntroManager && LevelVideoIntroManager.Instance != null)
        {
            LevelVideoIntroManager.Instance.RequestLevelFromMenu(sceneName);
            return;
        }

        levelLoader.LoadLevelFromMenu(sceneName);
    }


    // CHILD SEARCH

    private static Transform FindChildRecursive(Transform parent, string childName)
    {
        if (parent == null)
            return null;

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);

            if (string.Equals(child.name, childName, StringComparison.Ordinal))
                return child;

            Transform nested = FindChildRecursive(child, childName);

            if (nested != null)
                return nested;
        }

        return null;
    }


    // VALIDATE

    private void OnValidate()
    {
        if (string.IsNullOrWhiteSpace(lockedRootName))
            lockedRootName = "bg_Locked_Icon";

        if (button == null)
            button = GetComponent<Button>();

        ResolveLockedRoot();
    }


    // CLEANUP

    private void OnDestroy()
    {
        if (button != null)
            button.onClick.RemoveListener(LoadSelectedLevel);

        if (loaderSubscribed && levelLoader != null)
            levelLoader.LevelLoaded -= HandleLevelLoaded;

        if (progressSubscribed && levelProgressManager != null)
            levelProgressManager.ProgressChanged -= HandleProgressChanged;


        loaderSubscribed = false;
        progressSubscribed = false;
    }
}
