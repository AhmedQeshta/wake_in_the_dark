using System;
using System.Collections;
using UnityEngine;

[DefaultExecutionOrder(-500)]
public class PartyLivesManager : MonoBehaviour
{
    // INSTANCE
    public static PartyLivesManager Instance { get; private set; }

    // LIVES
    [Header("Lives")]
    [SerializeField, Min(1)] private int maxLives = 3;


    [Tooltip("If Player 1 OR Wife reaches 0 lives, reload the current level and restore BOTH life counters to Max Lives.")]
    [SerializeField] private bool resetBothLivesWhenOneReachesZero = true;


    [Tooltip("Entering a different level normally starts both characters with full lives.")]
    [SerializeField] private bool resetLivesWhenEnteringDifferentLevel = true;


    // LIVES UI
    [Header("Lives UI")]
    [Tooltip("Bootstrap > HUD_Canvas > Player > Lives")]
    [SerializeField] private PlayerLivesUI player1LivesUI;

    [Tooltip("Bootstrap > HUD_Canvas > Wife > Lives")]
    [SerializeField] private PlayerLivesUI wifeLivesUI;

    [Tooltip("Hide Wife lives when no Wife character exists in the loaded level.")]
    [SerializeField] private bool hideWifeUIWhenWifeIsMissing = true;

    // RESPAWN WHILE LIVES REMAIN
    [Header("Respawn While Lives Remain")]
    [Tooltip("If ON, a character who dies but still has lives respawns at the nearest RespawnPoint. The level is NOT reloaded.")]
    [InspectorName("Use Respawn Point")]
    [SerializeField] private bool useSharedRespawnPoint = true;


    [Tooltip("Small offset for Player 1 when respawning at the shared RespawnPoint.")]
    [SerializeField] private Vector3 player1RespawnOffset = new Vector3(-0.35f, 0f, 0f);


    [Tooltip("Small offset for Wife when respawning at the shared RespawnPoint.")]
    [SerializeField] private Vector3 wifeRespawnOffset = new Vector3(0.35f, 0f, 0f);


    // DEBUG
    [Header("Debug")]
    [SerializeField] private bool debugLogs = false;


    // STATE
    private int player1Lives;
    private int wifeLives;
    private bool initialized;
    private bool deathInProgress;
    private bool currentDeathRequiresLevelReload;
    private PartyCharacterRole currentVictimRole;
    private bool resetLivesAfterReload;
    private string trackedLevelSceneName;
    private PlayerDeath player1Death;
    private PlayerDeath wifeDeath;
    private LevelLoader levelLoader;
    private Coroutine finalizeLevelRoutine;

    // PUBLIC STATE
    public int MaxLives => maxLives;
    public bool IsPartyDeathInProgress => deathInProgress;
    public bool CurrentDeathRequiresLevelReload => currentDeathRequiresLevelReload;
    public int Player1Lives => player1Lives;
    public int WifeLives => wifeLives;


    // AWAKE
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        EnsureInitialized();

        RefreshAllLivesUI(true);

        if (hideWifeUIWhenWifeIsMissing && wifeLivesUI != null)
            wifeLivesUI.gameObject.SetActive(false);
    }


    // START
    private IEnumerator Start()
    {
        while (LevelLoader.Instance == null)
        {
            yield return null;
        }

        levelLoader = LevelLoader.Instance;
        levelLoader.LevelLoaded += OnLevelLoaded;

        if (!string.IsNullOrWhiteSpace(levelLoader.CurrentLevelSceneName))
            OnLevelLoaded(levelLoader.CurrentLevelSceneName);


    }


    // INITIALIZE
    private void EnsureInitialized()
    {
        if (initialized)
            return;

        player1Lives = maxLives;
        wifeLives = maxLives;
        initialized = true;
    }


    // REGISTER CHARACTER

    public void RegisterCharacter(PlayerDeath character)
    {
        if (character == null)
            return;

        EnsureInitialized();

        if (character.CharacterRole == PartyCharacterRole.Wife)
        {
            WarnAboutDuplicateRole(wifeDeath, character, PartyCharacterRole.Wife);
            wifeDeath = character;
            if (wifeLivesUI == null && character.LivesUI != null)
                wifeLivesUI = character.LivesUI;

            if (wifeLivesUI != null)
            {
                wifeLivesUI.gameObject.SetActive(true);
                wifeLivesUI.SetLivesImmediately(wifeLives);
            }
        }
        else
        {
            WarnAboutDuplicateRole(player1Death, character, PartyCharacterRole.Player1);
            player1Death = character;

            if (player1LivesUI == null && character.LivesUI != null)
                player1LivesUI = character.LivesUI;

            if (player1LivesUI != null)
            {
                player1LivesUI.gameObject.SetActive(true);
                player1LivesUI.SetLivesImmediately(player1Lives);
            }
        }

        /*
         * This only matters during a FULL level reload caused by  one character reaching 0 lives.
         */
        if (deathInProgress && currentDeathRequiresLevelReload)
            character.FreezeForPartyDeath();
    }


    private void WarnAboutDuplicateRole(PlayerDeath existing, PlayerDeath incoming, PartyCharacterRole role)
    {
        if (existing == null || existing == incoming || !existing.gameObject.activeInHierarchy)
            return;

        Debug.LogWarning("PartyLivesManager: More than one active PlayerDeath " + "is registered as " + role + ". Existing=" + existing.name + ", Incoming=" + incoming.name + ". Check PlayerDeath > Party Character.", incoming
        );
    }


    public void UnregisterCharacter(PlayerDeath character)
    {
        if (character == null)
            return;

        if (player1Death == character)
            player1Death = null;

        if (wifeDeath == character)
            wifeDeath = null;
    }


    // GET LIVES

    public int GetLives(PartyCharacterRole role)
    {
        EnsureInitialized();

        return role == PartyCharacterRole.Wife ? wifeLives : player1Lives;
    }


    // ADD LIFE

    /// <summary>
    /// Tries to add lives to the selected party character.
    /// Returns true only when at least one life was actually added.
    ///
    /// This is intentionally public so independent systems such as
    /// LifeCollectible2D can safely award a life without directly
    /// changing PartyLivesManager's private state.
    /// </summary>
    public bool TryAddLife(
        PartyCharacterRole role,
        int amount = 1)
    {
        EnsureInitialized();


        /*
         * Do not change life counters while a death / respawn / full
         * level-reload sequence is already in progress.
         */
        if (deathInProgress ||
            amount <= 0)
        {
            return false;
        }


        int currentLives =
            GetLives(role);


        /*
         * Already full:
         * collectible should NOT be consumed.
         */
        if (currentLives >=
            maxLives)
        {
            return false;
        }


        int newLives =
            Mathf.Min(
                maxLives,
                currentLives + amount
            );


        if (role ==
            PartyCharacterRole.Wife)
        {
            wifeLives =
                newLives;
        }
        else
        {
            player1Lives =
                newLives;
        }


        /*
         * Update only the character who received the life.
         * PlayerLivesUI restores gained icons immediately.
         */
        RefreshLivesUI(
            role,
            false
        );


        if (debugLogs)
        {
            Debug.Log(
                "PartyLivesManager: Added life to " +
                role +
                ". Lives=" +
                newLives +
                "/" +
                maxLives +
                ".",
                this
            );
        }


        return
            newLives >
            currentLives;
    }


    public bool IsAtMaxLives(
        PartyCharacterRole role)
    {
        EnsureInitialized();

        return
            GetLives(role) >=
            maxLives;
    }


    // BEGIN DEATH

    public bool TryBeginPartyDeath(PlayerDeath victim, RespawnPoint respawnPoint)
    {
        if (victim == null || deathInProgress)
            return false;

        EnsureInitialized();

        deathInProgress = true;
        currentVictimRole = victim.CharacterRole;
        DecreaseLife(currentVictimRole);

        /*
         * THIS IS THE IMPORTANT CONDITION:
         * Lives > 0
         *     -> do NOT reload the level.
         *     -> only the dead character respawns.
         *
         * Lives == 0
         *     -> reload the whole current level.
         */
        currentDeathRequiresLevelReload = GetLives(currentVictimRole) <= 0;


        if (currentDeathRequiresLevelReload)
        {
            resetLivesAfterReload = true;
            // Level will be reloaded, so freeze both characters.
            FreezeAllRegisteredCharacters();
        }
        else
        {
            /*
             * Lives remain, so only freeze the character who died.  The other character stays exactly where they are.
             */
            victim.FreezeForPartyDeath();
        }

        return true;
    }


    // DECREASE LIFE
    private void DecreaseLife(PartyCharacterRole role)
    {
        if (role == PartyCharacterRole.Wife)
        {
            wifeLives = Mathf.Max(0, wifeLives - 1);

            /*
             * Only Wife UI changes.
             */
            RefreshLivesUI(PartyCharacterRole.Wife, false);

            return;
        }

        player1Lives = Mathf.Max(0, player1Lives - 1);

        /*
         * Only Player 1 UI changes.
         */
        RefreshLivesUI(PartyCharacterRole.Player1, false);
    }


    // RESPAWN SETTINGS
    public bool ShouldUseRespawnPoint()
    {
        return useSharedRespawnPoint;
    }


    public Vector3 GetRespawnOffset(PartyCharacterRole role)
    {
        return role == PartyCharacterRole.Wife ? wifeRespawnOffset : player1RespawnOffset;
    }


    // COMPLETE NON-FATAL DEATH

    public void CompleteNonFatalDeath(
        PlayerDeath character)
    {
        if (!deathInProgress || currentDeathRequiresLevelReload || character == null)
            return;


        /*
         * Force the correct remaining lives after the respawn.
         * This does NOT restore the lost life icon.
         */
        RefreshLivesUI(character.CharacterRole, true);
        deathInProgress = false;
        currentDeathRequiresLevelReload = false;

    }


    // FREEZE BOTH CHARACTERS
    private void FreezeAllRegisteredCharacters()
    {
        if (player1Death != null)
            player1Death.FreezeForPartyDeath();

        if (wifeDeath != null)
            wifeDeath.FreezeForPartyDeath();
    }


    // FULL LEVEL RELOAD

    public void ReloadAfterDeathSequence()
    {
        if (!deathInProgress || !currentDeathRequiresLevelReload)
            return;


        if (UIManager.Instance != null)
        {
            UIManager.Instance.ReloadAfterPlayerDeath();
            return;
        }

        if (LevelLoader.Instance != null)
        {
            UIManager.MarkGameplayStarted();
            LevelLoader.Instance.ReloadCurrentLevel();
            return;
        }

        deathInProgress = false;

        currentDeathRequiresLevelReload = false;
    }


    // LEVEL LOADED

    private void OnLevelLoaded(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
            return;
        bool enteringDifferentLevel = !string.IsNullOrWhiteSpace(trackedLevelSceneName) && !string.Equals(trackedLevelSceneName, sceneName, StringComparison.OrdinalIgnoreCase);

        if (enteringDifferentLevel && resetLivesWhenEnteringDifferentLevel)
        {
            ResetAllLivesInternal();
            resetLivesAfterReload = false;
            deathInProgress = false;
            currentDeathRequiresLevelReload = false;
            RefreshAllLivesUI(true);
        }


        trackedLevelSceneName = sceneName;


        if (finalizeLevelRoutine != null)
            StopCoroutine(finalizeLevelRoutine);



        finalizeLevelRoutine = StartCoroutine(FinalizeLoadedLevelRoutine(sceneName));
    }


    // FINALIZE FULL RELOAD

    private IEnumerator FinalizeLoadedLevelRoutine(string sceneName)
    {
        /*
         * Allow freshly loaded PlayerDeath components to Awake/Register.
         */
        yield return null;


        ResolveCharactersInScene(sceneName);


        /*
         * A level reload caused by 0 lives resets the counters.
         */
        if (resetLivesAfterReload)
        {
            if (resetBothLivesWhenOneReachesZero)
                ResetAllLivesInternal();
            else
                ResetCharacterLives(currentVictimRole);

            resetLivesAfterReload = false;
        }


        /*
         * IMPORTANT:
         * A full level reset uses the positions saved in the scene.
         * We do NOT move the characters to the checkpoint here.
         */
        if (player1Death != null)
            player1Death.RestoreAfterPartyReload();


        if (wifeDeath != null)
            wifeDeath.RestoreAfterPartyReload();



        deathInProgress = false;
        currentDeathRequiresLevelReload = false;
        RefreshAllLivesUI(true);
        UpdateWifeUIVisibility();
        finalizeLevelRoutine = null;
    }


    // RESOLVE CHARACTERS

    private void ResolveCharactersInScene(string sceneName)
    {
        PlayerDeath[] characters = FindObjectsByType<PlayerDeath>(FindObjectsSortMode.None);


        foreach (PlayerDeath character in characters)
        {
            if (character == null || (!string.Equals(character.gameObject.scene.name, sceneName, StringComparison.OrdinalIgnoreCase)))
                continue;


            if (character.CharacterRole == PartyCharacterRole.Wife)
                wifeDeath = character;
            else
                player1Death = character;
        }
    }


    // UI

    private void RefreshLivesUI(PartyCharacterRole role, bool immediate)
    {
        if (role == PartyCharacterRole.Wife)
        {
            if (wifeLivesUI != null)
            {
                if (immediate)
                    wifeLivesUI.SetLivesImmediately(wifeLives);

                else
                    wifeLivesUI.UpdateLives(wifeLives);
            }

            return;
        }


        if (player1LivesUI != null)
        {
            if (immediate)
                player1LivesUI.SetLivesImmediately(player1Lives);

            else
                player1LivesUI.UpdateLives(player1Lives);
        }
    }


    private void RefreshAllLivesUI(bool immediate)
    {
        RefreshLivesUI(PartyCharacterRole.Player1, immediate);
        RefreshLivesUI(PartyCharacterRole.Wife, immediate);
    }


    private void UpdateWifeUIVisibility()
    {
        if (!hideWifeUIWhenWifeIsMissing || wifeLivesUI == null)
            return;

        wifeLivesUI.gameObject.SetActive(wifeDeath != null);
    }


    // RESET LIVES
    public void ResetAllLives()
    {
        ResetAllLivesInternal();
        RefreshAllLivesUI(true);
    }


    private void ResetAllLivesInternal()
    {
        player1Lives = maxLives;

        wifeLives = maxLives;
    }


    private void ResetCharacterLives(PartyCharacterRole role)
    {
        if (role == PartyCharacterRole.Wife)
        {
            wifeLives = maxLives;
            return;
        }

        player1Lives = maxLives;
    }


    // VALIDATE

    private void OnValidate()
    {
        maxLives = Mathf.Max(1, maxLives);
    }


    // DESTROY

    private void OnDestroy()
    {
        if (levelLoader != null)
            levelLoader.LevelLoaded -= OnLevelLoaded;

        if (Instance == this)
            Instance = null;

    }
}
