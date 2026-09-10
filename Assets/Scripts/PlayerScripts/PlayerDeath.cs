using System.Collections;
using UnityEngine;

public class PlayerDeath : MonoBehaviour
{
    // PARTY CHARACTER
    [Header("Party Character")]
    [Tooltip("Player1 for the main player. Wife for Player 2.")]
    [SerializeField] private PartyCharacterRole characterRole = PartyCharacterRole.Player1;

    [Tooltip("Recommended ON. Automatically uses Player1 for tag 'Player' and Wife for tag 'wife'.")]
    [SerializeField] private bool autoDetectCharacterRoleFromTag = true;

    [SerializeField] private string mainPlayerTag = "Player";

    [SerializeField] private string wifeTag = "wife";


    // LIVES FALLBACK
    [Header("Lives")]
    [Tooltip("Fallback only if PartyLivesManager is missing.")]
    [SerializeField, Min(1)] private int maxLives = 3;

    [Tooltip("Optional fallback UI. PartyLivesManager can use this to auto-fill its UI reference.")]
    [SerializeField] private PlayerLivesUI livesUI;

    private int currentLives;


    // RESPAWN
    [Header("Respawn")]

    [Tooltip("Only RespawnPoint objects on these layers can be used.")]
    [SerializeField] private LayerMask respawnPointLayer;


    [Tooltip("Delay before this character reappears when they still have lives.")]
    [SerializeField, Min(0f)] private float respawnDelay = 0.25f;


    // DEATH SETTINGS
    [Header("Death Settings")]

    [Tooltip("Minimum amount of time the death sequence lasts.")]
    [SerializeField, Min(0f)] private float deathDelay = 0.8f;


    [Tooltip("Small delay added after the trap sound.")]
    [SerializeField, Min(0f)] private float soundEndPadding = 0.1f;


    // PLAYER FADE
    [Header("Player Fade")]
    [SerializeField, Min(0.01f)] private float fadeOutDuration = 0.4f;

    [SerializeField, Min(0.01f)] private float fadeInDuration = 0.35f;

    [SerializeField] private AnimationCurve fadeCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);


    // DEATH EFFECT
    [Header("Death Effect")]
    [SerializeField] private ParticleSystem deathParticlePrefab;


    // CHARACTER COMPONENTS

    [Header("Character Components")]

    [Tooltip("Main Player uses this. Wife may keep PlayerMovement assigned while the component itself stays disabled.")]
    [SerializeField] private PlayerMovement playerMovement;

    [Tooltip("Wife / Player 2 follower controller. Leave empty on Player 1.")]
    [SerializeField] private CompanionFollower2D companionFollower;

    [SerializeField] private Rigidbody2D playerRigidbody;

    [SerializeField] private Animator playerAnimator;
    [SerializeField] private Collider2D[] playerColliders;
    [SerializeField] private SpriteRenderer[] playerRenderers;


    // LEVEL MANAGER FALLBACK
    [Header("Level Manager")]
    [SerializeField] private UIManager UIManagerObj;


    // STATE
    private bool isDead;
    private Color[] originalRendererColors;
    private Vector3 initialSpawnPosition;
    private bool initialPlayerMovementEnabled;
    private bool initialCompanionFollowerEnabled;
    private bool initialAnimatorEnabled;
    private bool initialRigidbodySimulated;
    private bool[] initialColliderEnabledStates;
    private bool partyFreezeApplied;
    private PartyLivesManager partyLivesManager;


    // PUBLIC
    public bool IsDead => isDead;


    public PartyCharacterRole CharacterRole => characterRole;


    public PlayerLivesUI LivesUI => livesUI;


    public int CurrentLives
    {
        get
        {
            if (partyLivesManager != null)
                return partyLivesManager.GetLives(characterRole);

            return
                currentLives;
        }
    }


    public int MaxLives
    {
        get
        {
            if (partyLivesManager != null)
                return partyLivesManager.MaxLives;

            return
                maxLives;
        }
    }


    // AWAKE
    private void Awake()
    {
        ResolveCharacterRole();

        initialSpawnPosition = transform.position;


        if (playerMovement == null)
            playerMovement = GetComponent<PlayerMovement>();

        if (companionFollower == null)
            companionFollower = GetComponent<CompanionFollower2D>();

        if (playerRigidbody == null)
            playerRigidbody = GetComponent<Rigidbody2D>();

        if (playerAnimator == null)
            playerAnimator = GetComponent<Animator>();

        if (playerColliders == null || playerColliders.Length == 0)
            playerColliders = GetComponentsInChildren<Collider2D>(true);


        if (playerRenderers == null || playerRenderers.Length == 0)
            playerRenderers = GetComponentsInChildren<SpriteRenderer>(true);


        originalRendererColors = new Color[playerRenderers.Length];


        for (int i = 0; i < playerRenderers.Length; i++)
            if (playerRenderers[i] != null)
                originalRendererColors[i] = playerRenderers[i].color;


        initialPlayerMovementEnabled = playerMovement != null && playerMovement.enabled;
        initialCompanionFollowerEnabled = companionFollower != null && companionFollower.enabled;
        initialAnimatorEnabled = playerAnimator != null && playerAnimator.enabled;
        initialRigidbodySimulated = playerRigidbody != null && playerRigidbody.simulated;
        initialColliderEnabledStates = new bool[playerColliders.Length];


        for (int i = 0; i < playerColliders.Length; i++)
            initialColliderEnabledStates[i] = playerColliders[i] != null && playerColliders[i].enabled;


        if (UIManagerObj == null)
            UIManagerObj = FindAnyObjectByType<UIManager>();


        partyLivesManager = PartyLivesManager.Instance;


        if (partyLivesManager == null)
            partyLivesManager = FindAnyObjectByType<PartyLivesManager>();


        currentLives = maxLives;


        isDead = false;


        if (partyLivesManager != null)
            partyLivesManager.RegisterCharacter(this);
        else
            UpdateFallbackLivesUI();
    }


    // CHARACTER ROLE
    private void ResolveCharacterRole()
    {
        if (!autoDetectCharacterRoleFromTag)
            return;


        string currentTag = gameObject.tag;


        if (!string.IsNullOrWhiteSpace(wifeTag) && currentTag == wifeTag)
        {
            characterRole = PartyCharacterRole.Wife;
            return;
        }


        if (!string.IsNullOrWhiteSpace(mainPlayerTag) && currentTag == mainPlayerTag)
            characterRole = PartyCharacterRole.Player1;
    }


    // DESTROY
    private void OnDestroy()
    {
        if (partyLivesManager != null)
            partyLivesManager.UnregisterCharacter(this);
    }


    // KILL CHARACTER
    public void KillPlayer(float trapSoundDuration = 0f)
    {
        if (isDead)
            return;



        // PARTY MODE
        if (partyLivesManager != null)
        {
            if (partyLivesManager.IsPartyDeathInProgress)
                return;


            Vector3 deathPosition = transform.position;
            RespawnPoint nearestPoint = FindNearestRespawnPoint(deathPosition);

            bool started = partyLivesManager.TryBeginPartyDeath(this, nearestPoint);
            if (!started)
                return;

            isDead = true;
            StartCoroutine(PartyDeathRoutine(trapSoundDuration, nearestPoint));

            return;
        }



        // FALLBACK MODE
        isDead = true;
        currentLives = Mathf.Max(0, currentLives - 1);
        UpdateFallbackLivesUI();
        StartCoroutine(LegacyDeathRoutine(trapSoundDuration));
    }


    // PARTY DEATH ROUTINE

    private IEnumerator PartyDeathRoutine(float trapSoundDuration, RespawnPoint nearestPoint)
    {
        SpawnDeathParticles();

        yield return StartCoroutine(FadePlayer(1f, 0f, fadeOutDuration));

        float requiredDeathTime = Mathf.Max(deathDelay, trapSoundDuration + soundEndPadding);
        float remainingWait = Mathf.Max(0f, requiredDeathTime - fadeOutDuration);


        if (remainingWait > 0f)
            yield return new WaitForSecondsRealtime(remainingWait);


        // NO LIVES LEFT -> RELOAD WHOLE LEVEL
        if (partyLivesManager != null && partyLivesManager.CurrentDeathRequiresLevelReload)
        {
            partyLivesManager.ReloadAfterDeathSequence();
            yield break;
        }



        // LIVES REMAIN -> RESPAWN ONLY THIS CHARACTER


        Vector3 respawnPosition = initialSpawnPosition;

        if (partyLivesManager != null && partyLivesManager.ShouldUseRespawnPoint() && nearestPoint != null)
            respawnPosition = nearestPoint.GetRespawnPosition() + partyLivesManager.GetRespawnOffset(characterRole);
        else if (nearestPoint != null)
            respawnPosition = nearestPoint.GetRespawnPosition();


        if (respawnDelay > 0f)
            yield return new WaitForSecondsRealtime(respawnDelay);


        yield return StartCoroutine(RespawnAfterRemainingLife(respawnPosition));


        if (partyLivesManager != null)
            partyLivesManager.CompleteNonFatalDeath(this);
    }


    // RESPAWN WHILE LIVES REMAIN

    private IEnumerator RespawnAfterRemainingLife(Vector3 respawnPosition)
    {
        PlaceAtPartyRespawn(respawnPosition);


        SetPlayerOpacity(0f);


        /*
         * Allow the idle animation to be visible during fade-in,
         * but movement/follower/colliders stay frozen until the fade finishes.
         */
        if (playerAnimator != null)
            playerAnimator.enabled = initialAnimatorEnabled;

        yield return StartCoroutine(FadePlayer(0f, 1f, fadeInDuration));


        RestoreAfterPartyReload();
    }


    // FREEZE FOR DEATH

    public void FreezeForPartyDeath()
    {
        if (partyFreezeApplied)
            return;


        partyFreezeApplied = true;
        isDead = true;


        if (playerMovement != null)
            playerMovement.enabled = false;

        if (companionFollower != null)
            companionFollower.enabled = false;


        if (playerRigidbody != null)
            playerRigidbody.linearVelocity = Vector2.zero;
        playerRigidbody.angularVelocity = 0f;
        playerRigidbody.simulated = false;


        if (playerAnimator != null)
            playerAnimator.enabled = false;


        SetPlayerColliders(false);
    }


    // PLACE AT RESPAWN
    public void PlaceAtPartyRespawn(Vector3 respawnPosition)
    {
        transform.position = respawnPosition;

        if (playerRigidbody != null)
        {
            playerRigidbody.position = new Vector2(respawnPosition.x, respawnPosition.y);
            playerRigidbody.linearVelocity = Vector2.zero;
            playerRigidbody.angularVelocity = 0f;
        }
    }


    // RESTORE AFTER RESPAWN / RELOAD
    public void RestoreAfterPartyReload()
    {
        SetPlayerOpacity(1f);


        if (playerRigidbody != null)
        {
            playerRigidbody.linearVelocity = Vector2.zero;
            playerRigidbody.angularVelocity = 0f;
            playerRigidbody.simulated = initialRigidbodySimulated;
        }


        RestoreInitialColliderStates();


        if (playerAnimator != null)
            playerAnimator.enabled = initialAnimatorEnabled;


        /*  
            * Wife's PlayerMovement remains disabled if it  * was disabled in the prefab/scene.
          */
        if (playerMovement != null)
            playerMovement.enabled = initialPlayerMovementEnabled;



        if (companionFollower != null)
            companionFollower.enabled = initialCompanionFollowerEnabled;


        partyFreezeApplied = false;
        isDead = false;
    }


    // FALLBACK DEATH
    private IEnumerator LegacyDeathRoutine(float trapSoundDuration)
    {
        Vector3 deathPosition = transform.position;


        FreezeForPartyDeath();

        SpawnDeathParticles();


        yield return StartCoroutine(FadePlayer(1f, 0f, fadeOutDuration));


        float requiredDeathTime = Mathf.Max(deathDelay, trapSoundDuration + soundEndPadding);


        float remainingWait = Mathf.Max(0f, requiredDeathTime - fadeOutDuration);


        if (remainingWait > 0f)
            yield return new WaitForSecondsRealtime(remainingWait);



        if (currentLives <= 0)
        {
            ReloadLevelAfterFinalDeath();
            yield break;
        }


        RespawnPoint nearestPoint = FindNearestRespawnPoint(deathPosition);
        Vector3 respawnPosition = nearestPoint != null ? nearestPoint.GetRespawnPosition() : initialSpawnPosition;

        if (respawnDelay > 0f)
            yield return new WaitForSecondsRealtime(respawnDelay);

        yield return StartCoroutine(RespawnAfterRemainingLife(respawnPosition));
    }


    // FIND NEAREST RESPAWN POINT

    private RespawnPoint FindNearestRespawnPoint(Vector3 deathPosition)
    {
        RespawnPoint[] respawnPoints = FindObjectsByType<RespawnPoint>(FindObjectsSortMode.None);
        RespawnPoint nearestPoint = null;
        float nearestDistanceSquared = float.PositiveInfinity;

        foreach (RespawnPoint point in respawnPoints)
        {
            if (point == null || !IsRespawnLayerAllowed(point.gameObject.layer))
                continue;

            Vector3 pointPosition = point.GetRespawnPosition();

            float distanceSquared = (pointPosition - deathPosition).sqrMagnitude;

            if (distanceSquared < nearestDistanceSquared)
            {
                nearestDistanceSquared = distanceSquared;
                nearestPoint = point;
            }
        }


        return nearestPoint;
    }


    private bool IsRespawnLayerAllowed(int objectLayer)
    {
        if (respawnPointLayer.value == 0)
            return true;
        int objectLayerMask = 1 << objectLayer;
        return (respawnPointLayer.value & objectLayerMask) != 0;
    }


    // DEATH PARTICLES

    private void SpawnDeathParticles()
    {
        if (deathParticlePrefab == null)
            return;


        ParticleSystem particles = Instantiate(deathParticlePrefab, transform.position, Quaternion.identity);
        particles.Play();
        Destroy(particles.gameObject, 5f);
    }


    // COLLIDERS

    private void SetPlayerColliders(bool enabledState)
    {
        if (playerColliders == null)
            return;

        foreach (Collider2D playerCollider in playerColliders)
        {
            if (playerCollider == null)
                continue;

            playerCollider.enabled = enabledState;
        }
    }


    private void RestoreInitialColliderStates()
    {
        if (playerColliders == null)
            return;


        for (int i = 0; i < playerColliders.Length; i++)
        {
            Collider2D playerCollider = playerColliders[i];

            if (playerCollider == null)
                continue;

            bool enabledState = initialColliderEnabledStates != null && i < initialColliderEnabledStates.Length ? initialColliderEnabledStates[i] : true;

            playerCollider.enabled = enabledState;
        }
    }


    // FADE

    private IEnumerator FadePlayer(float startOpacity, float targetOpacity, float duration)
    {
        if (playerRenderers == null || playerRenderers.Length == 0)
            yield break;


        if (duration <= 0f)
        {
            SetPlayerOpacity(targetOpacity);
            yield break;
        }


        float elapsed = 0f;


        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;

            float normalizedTime = Mathf.Clamp01(elapsed / duration);

            float curveValue = fadeCurve != null && fadeCurve.length > 0 ? fadeCurve.Evaluate(normalizedTime) : normalizedTime;

            float opacity = Mathf.Lerp(startOpacity, targetOpacity, curveValue);

            SetPlayerOpacity(opacity);

            yield return null;
        }


        SetPlayerOpacity(targetOpacity);
    }


    private void SetPlayerOpacity(float opacity)
    {
        if (playerRenderers == null)
            return;


        opacity = Mathf.Clamp01(opacity);


        for (int i = 0; i < playerRenderers.Length; i++)
        {
            SpriteRenderer renderer = playerRenderers[i];

            if (renderer == null)
                continue;

            Color originalColor = originalRendererColors != null && i < originalRendererColors.Length ? originalRendererColors[i] : renderer.color;
            originalColor.a *= opacity;
            renderer.color = originalColor;
        }
    }


    // FALLBACK RELOAD
    private void ReloadLevelAfterFinalDeath()
    {
        if (UIManagerObj != null)
        {
            UIManagerObj.ReloadAfterPlayerDeath();
            return;
        }


        if (UIManager.Instance != null)
        {
            UIManager.Instance.ReloadAfterPlayerDeath();
            return;
        }


        if (LevelLoader.Instance != null)
        {
            UIManager.MarkGameplayStarted();
            LevelLoader.Instance.ReloadCurrentLevel();
        }
    }


    // FALLBACK UI
    private void UpdateFallbackLivesUI()
    {
        if (livesUI == null)
            return;

        livesUI.UpdateLives(currentLives);
    }


    // VALIDATE
    private void OnValidate()
    {
        if (string.IsNullOrWhiteSpace(mainPlayerTag))
            mainPlayerTag = "Player";


        if (string.IsNullOrWhiteSpace(wifeTag))
            wifeTag = "wife";


        maxLives = Mathf.Max(1, maxLives);
        respawnDelay = Mathf.Max(0f, respawnDelay);
        deathDelay = Mathf.Max(0f, deathDelay);
        soundEndPadding = Mathf.Max(0f, soundEndPadding);
        fadeOutDuration = Mathf.Max(0.01f, fadeOutDuration);
        fadeInDuration = Mathf.Max(0.01f, fadeInDuration);

        if (fadeCurve == null || fadeCurve.length == 0)
            fadeCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    }
}