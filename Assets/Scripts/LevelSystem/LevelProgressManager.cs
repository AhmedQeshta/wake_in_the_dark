using System;
using UnityEngine;

public class LevelProgressManager : MonoBehaviour
{
    // ==================================================
    // INSTANCE
    // ==================================================

    public static LevelProgressManager Instance { get; private set; }


    // ==================================================
    // LEVEL ORDER
    // ==================================================

    [Header("Level Order")]

    [Tooltip("Progress order from first level to final level. Index 0 is unlocked on a new game.")]
    [SerializeField]
    private string[] levelSceneNames =
    {
        "Level_01",
        "Level_02",
        "Level_03",
        "Level_04",
        "Level_05",
        "Level_06",
        "Level_07",
        "Level_Final"
    };


    // ==================================================
    // SAVE
    // ==================================================

    [Header("Save")]

    [SerializeField]
    private string highestUnlockedKey = "WakeInTheDark.HighestUnlockedLevelIndex";


    // ==================================================
    // STATE
    // ==================================================

    private int highestUnlockedLevelIndex;


    // ==================================================
    // PUBLIC
    // ==================================================

    public int HighestUnlockedLevelIndex =>
        highestUnlockedLevelIndex;


    public int LevelCount =>
        levelSceneNames != null
            ? levelSceneNames.Length
            : 0;


    public event Action ProgressChanged;


    // ==================================================
    // AWAKE
    // ==================================================

    private void Awake()
    {
        if (Instance != null &&
            Instance != this)
        {
            Debug.LogWarning(
                "Duplicate LevelProgressManager found. Removing duplicate.",
                this
            );

            Destroy(gameObject);

            return;
        }


        Instance =
            this;


        LoadProgress();
    }


    // ==================================================
    // LOAD
    // ==================================================

    private void LoadProgress()
    {
        /*
         * New game:
         *
         * Index 0 = Level_01
         * and is always unlocked.
         */
        int savedIndex =
            PlayerPrefs.GetInt(
                highestUnlockedKey,
                0
            );


        highestUnlockedLevelIndex =
            ClampLevelIndex(
                savedIndex
            );


        /*
         * Keep PlayerPrefs valid if the level list changed.
         */
        SaveProgress();
    }


    // ==================================================
    // SAVE
    // ==================================================

    private void SaveProgress()
    {
        PlayerPrefs.SetInt(
            highestUnlockedKey,
            highestUnlockedLevelIndex
        );


        PlayerPrefs.Save();
    }


    // ==================================================
    // CHECK LEVEL
    // ==================================================

    public bool IsLevelUnlocked(
        int levelIndex)
    {
        if (!IsValidLevelIndex(
                levelIndex))
        {
            return false;
        }


        return
            levelIndex <=
            highestUnlockedLevelIndex;
    }


    public bool IsSceneUnlocked(
        string sceneName)
    {
        int levelIndex =
            GetLevelIndex(
                sceneName
            );


        return
            IsLevelUnlocked(
                levelIndex
            );
    }


    // ==================================================
    // GET LEVEL INDEX
    // ==================================================

    public int GetLevelIndex(
        string sceneName)
    {
        if (string.IsNullOrWhiteSpace(
                sceneName) ||
            levelSceneNames == null)
        {
            return -1;
        }


        for (int i = 0;
             i < levelSceneNames.Length;
             i++)
        {
            if (string.Equals(
                    levelSceneNames[i],
                    sceneName,
                    StringComparison.OrdinalIgnoreCase))
            {
                return i;
            }
        }


        return -1;
    }


    // ==================================================
    // COMPLETE LEVEL
    // ==================================================

    public bool CompleteLevelAndUnlockNext(
        string completedSceneName,
        string nextSceneName)
    {
        int completedIndex =
            GetLevelIndex(
                completedSceneName
            );


        int nextIndex =
            GetLevelIndex(
                nextSceneName
            );


        if (!IsValidLevelIndex(
                completedIndex))
        {
            Debug.LogError(
                "LevelProgressManager: Completed scene '" +
                completedSceneName +
                "' is not in Level Order.",
                this
            );

            return false;
        }


        if (!IsValidLevelIndex(
                nextIndex))
        {
            Debug.LogError(
                "LevelProgressManager: Next scene '" +
                nextSceneName +
                "' is not in Level Order.",
                this
            );

            return false;
        }


        /*
         * The player should only move:
         *
         * Level_01 -> Level_02
         * Level_02 -> Level_03
         * ...
         */
        if (nextIndex !=
            completedIndex + 1)
        {
            Debug.LogError(
                "LevelProgressManager: Invalid progression. " +
                completedSceneName +
                " cannot unlock " +
                nextSceneName +
                ".",
                this
            );

            return false;
        }


        /*
         * If an old unlocked level is replayed,
         * its next level may already be unlocked.
         *
         * That is valid and should still allow the door.
         */
        if (nextIndex <=
            highestUnlockedLevelIndex)
        {
            return true;
        }


        /*
         * Do not allow a locked/skipped level to unlock
         * something further ahead.
         */
        if (completedIndex >
            highestUnlockedLevelIndex)
        {
            Debug.LogWarning(
                "LevelProgressManager: Cannot complete a level " +
                "that is not currently unlocked.",
                this
            );

            return false;
        }


        highestUnlockedLevelIndex =
            nextIndex;


        SaveProgress();


        ProgressChanged?.Invoke();


        Debug.Log(
            "Progress updated. Unlocked: " +
            nextSceneName +
            " (index " +
            nextIndex +
            ")",
            this
        );


        return true;
    }


    // ==================================================
    // RESET AFTER ENDING
    // ==================================================

    public void ResetProgress()
    {
        highestUnlockedLevelIndex =
            0;


        SaveProgress();


        ProgressChanged?.Invoke();


        Debug.Log(
            "Game progression reset. Only Level_01 is unlocked.",
            this
        );
    }


    // ==================================================
    // OPTIONAL TEST HELPERS
    // ==================================================

    [ContextMenu("DEBUG - Unlock All Levels")]
    private void DebugUnlockAllLevels()
    {
        if (LevelCount <= 0)
            return;


        highestUnlockedLevelIndex =
            LevelCount - 1;


        SaveProgress();


        ProgressChanged?.Invoke();


        Debug.Log(
            "DEBUG: All levels unlocked.",
            this
        );
    }


    [ContextMenu("DEBUG - Reset Progress")]
    private void DebugResetProgress()
    {
        ResetProgress();
    }


    // ==================================================
    // HELPERS
    // ==================================================

    private bool IsValidLevelIndex(
        int levelIndex)
    {
        return
            levelSceneNames != null &&
            levelIndex >= 0 &&
            levelIndex <
            levelSceneNames.Length;
    }


    private int ClampLevelIndex(
        int levelIndex)
    {
        if (LevelCount <= 0)
            return 0;


        return
            Mathf.Clamp(
                levelIndex,
                0,
                LevelCount - 1
            );
    }


    // ==================================================
    // VALIDATE
    // ==================================================

    private void OnValidate()
    {
        if (string.IsNullOrWhiteSpace(
                highestUnlockedKey))
        {
            highestUnlockedKey =
                "WakeInTheDark.HighestUnlockedLevelIndex";
        }
    }


    // ==================================================
    // CLEANUP
    // ==================================================

    private void OnDestroy()
    {
        if (Instance ==
            this)
        {
            Instance =
                null;
        }
    }
}
