using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerDeath : MonoBehaviour
{
    // ==================================================
    // LIVES
    // ==================================================

    [Header("Lives")]
    [SerializeField, Min(1)]
    private int maxLives = 3;

    [SerializeField]
    private PlayerLivesUI livesUI;

    private int currentLives;


    // ==================================================
    // RESPAWN
    // ==================================================

    [Header("Respawn")]

    [Tooltip("Only RespawnPoint objects on these layers can be used.")]
    [SerializeField]
    private LayerMask respawnPointLayer;

    [Tooltip("Delay after death before the player starts respawning.")]
    [SerializeField, Min(0f)]
    private float respawnDelay = 0.25f;


    // ==================================================
    // DEATH SETTINGS
    // ==================================================

    [Header("Death Settings")]

    [Tooltip("Minimum amount of time the death sequence lasts.")]
    [SerializeField, Min(0f)]
    private float deathDelay = 0.8f;

    [Tooltip("Small delay added after the trap sound.")]
    [SerializeField, Min(0f)]
    private float soundEndPadding = 0.1f;


    // ==================================================
    // PLAYER FADE
    // ==================================================

    [Header("Player Fade")]

    [SerializeField, Min(0.01f)]
    private float fadeOutDuration = 0.4f;

    [SerializeField, Min(0.01f)]
    private float fadeInDuration = 0.35f;

    [SerializeField]
    private AnimationCurve fadeCurve =
        AnimationCurve.EaseInOut(
            0f,
            0f,
            1f,
            1f
        );


    // ==================================================
    // DEATH PARTICLES
    // ==================================================

    [Header("Death Effect")]

    [SerializeField]
    private ParticleSystem deathParticlePrefab;


    // ==================================================
    // PLAYER COMPONENTS
    // ==================================================

    [Header("Player Components")]

    [SerializeField]
    private PlayerMovement playerMovement;

    [SerializeField]
    private Rigidbody2D playerRigidbody;

    [SerializeField]
    private Animator playerAnimator;

    [SerializeField]
    private Collider2D[] playerColliders;

    [SerializeField]
    private SpriteRenderer[] playerRenderers;


    // ==================================================
    // LEVEL MANAGER
    // ==================================================

    [Header("Level Manager")]

    [SerializeField]
    private UIManager uiManager;


    // ==================================================
    // STATE
    // ==================================================

    private bool isDead;

    private Color[] originalRendererColors;

    private Vector3 initialSpawnPosition;


    // ==================================================
    // PUBLIC VALUES
    // ==================================================

    public bool IsDead => isDead;

    public int CurrentLives => currentLives;

    public int MaxLives => maxLives;


    // ==================================================
    // AWAKE
    // ==================================================

    private void Awake()
    {
        // ----------------------------------------------
        // SAVE INITIAL POSITION
        // ----------------------------------------------

        initialSpawnPosition =
            transform.position;


        // ----------------------------------------------
        // PLAYER MOVEMENT
        // ----------------------------------------------

        if (playerMovement == null)
        {
            playerMovement =
                GetComponent<PlayerMovement>();
        }


        // ----------------------------------------------
        // RIGIDBODY
        // ----------------------------------------------

        if (playerRigidbody == null)
        {
            playerRigidbody =
                GetComponent<Rigidbody2D>();
        }


        // ----------------------------------------------
        // ANIMATOR
        // ----------------------------------------------

        if (playerAnimator == null)
        {
            playerAnimator =
                GetComponent<Animator>();
        }


        // ----------------------------------------------
        // COLLIDERS
        // ----------------------------------------------

        if (playerColliders == null ||
            playerColliders.Length == 0)
        {
            playerColliders =
                GetComponentsInChildren<Collider2D>(
                    true
                );
        }


        // ----------------------------------------------
        // SPRITE RENDERERS
        // ----------------------------------------------

        if (playerRenderers == null ||
            playerRenderers.Length == 0)
        {
            playerRenderers =
                GetComponentsInChildren<SpriteRenderer>(
                    true
                );
        }


        // ----------------------------------------------
        // SAVE ORIGINAL PLAYER COLORS
        // ----------------------------------------------

        originalRendererColors =
            new Color[playerRenderers.Length];


        for (int i = 0;
             i < playerRenderers.Length;
             i++)
        {
            if (playerRenderers[i] != null)
            {
                originalRendererColors[i] =
                    playerRenderers[i].color;
            }
        }


        // ----------------------------------------------
        // UI MANAGER
        // ----------------------------------------------

        if (uiManager == null)
        {
            uiManager =
                FindAnyObjectByType<UIManager>();
        }


        // ----------------------------------------------
        // LIVES UI
        // ----------------------------------------------

        if (livesUI == null)
        {
            livesUI =
                FindAnyObjectByType<PlayerLivesUI>();
        }


        // ----------------------------------------------
        // STARTING LIVES
        // ----------------------------------------------

        currentLives =
            maxLives;


        UpdateLivesUI();


        /*
         * Player starts alive.
         */
        isDead =
            false;
    }


    // ==================================================
    // KILL PLAYER
    // ==================================================

    public void KillPlayer(
        float trapSoundDuration = 0f)
    {
        /*
         * IMPORTANT:
         *
         * Prevent multiple trap events while
         * the death sequence is already running.
         */
        if (isDead)
            return;


        // Player is now dying.
        isDead =
            true;


        // ----------------------------------------------
        // REMOVE ONE LIFE
        // ----------------------------------------------

        currentLives =
            Mathf.Max(
                0,
                currentLives - 1
            );


        UpdateLivesUI();


        // ----------------------------------------------
        // START DEATH ROUTINE
        // ----------------------------------------------

        StartCoroutine(
            DeathRoutine(
                trapSoundDuration
            )
        );
    }


    // ==================================================
    // DEATH ROUTINE
    // ==================================================

    private IEnumerator DeathRoutine(
        float trapSoundDuration)
    {
        /*
         * Save the position where the player died.
         *
         * Nearest RespawnPoint is calculated
         * from this position.
         */
        Vector3 deathPosition =
            transform.position;


        // ----------------------------------------------
        // DISABLE PLAYER MOVEMENT
        // ----------------------------------------------

        if (playerMovement != null)
        {
            playerMovement.enabled =
                false;
        }


        // ----------------------------------------------
        // STOP PHYSICS
        // ----------------------------------------------

        if (playerRigidbody != null)
        {
            playerRigidbody.linearVelocity =
                Vector2.zero;

            playerRigidbody.angularVelocity =
                0f;

            playerRigidbody.simulated =
                false;
        }


        // ----------------------------------------------
        // STOP ANIMATOR
        // ----------------------------------------------

        if (playerAnimator != null)
        {
            playerAnimator.enabled =
                false;
        }


        // ----------------------------------------------
        // DISABLE PLAYER COLLIDERS
        // ----------------------------------------------

        SetPlayerColliders(
            false
        );


        // ----------------------------------------------
        // SPAWN DEATH PARTICLES
        // ----------------------------------------------

        SpawnDeathParticles();


        // ----------------------------------------------
        // FADE PLAYER OUT
        // ----------------------------------------------

        yield return StartCoroutine(
            FadePlayer(
                1f,
                0f,
                fadeOutDuration
            )
        );


        // ----------------------------------------------
        // WAIT FOR DEATH SOUND / EFFECT
        // ----------------------------------------------

        float requiredDeathTime =
            Mathf.Max(
                deathDelay,
                trapSoundDuration +
                soundEndPadding
            );


        float remainingWait =
            Mathf.Max(
                0f,
                requiredDeathTime -
                fadeOutDuration
            );


        if (remainingWait > 0f)
        {
            yield return new WaitForSecondsRealtime(
                remainingWait
            );
        }


        // ==================================================
        // NO LIVES LEFT
        // ==================================================

        if (currentLives <= 0)
        {
            ReloadLevelAfterFinalDeath();

            yield break;
        }


        // ==================================================
        // FIND NEAREST RESPAWN
        // ==================================================

        RespawnPoint nearestPoint =
            FindNearestRespawnPoint(
                deathPosition
            );


        Vector3 respawnPosition;


        if (nearestPoint != null)
        {
            respawnPosition =
                nearestPoint.GetRespawnPosition();


            Debug.Log(
                "Respawning at nearest point: " +
                nearestPoint.name,
                nearestPoint
            );
        }
        else
        {
            /*
             * Safety fallback:
             *
             * If no respawn point is available,
             * use the player's original position.
             */
            respawnPosition =
                initialSpawnPosition;


            Debug.LogWarning(
                "No valid RespawnPoint found. " +
                "Using initial player position.",
                this
            );
        }


        // ----------------------------------------------
        // RESPAWN DELAY
        // ----------------------------------------------

        if (respawnDelay > 0f)
        {
            yield return new WaitForSecondsRealtime(
                respawnDelay
            );
        }


        // ----------------------------------------------
        // RESPAWN PLAYER
        // ----------------------------------------------

        yield return StartCoroutine(
            RespawnPlayer(
                respawnPosition
            )
        );
    }


    // ==================================================
    // RESPAWN PLAYER
    // ==================================================

    private IEnumerator RespawnPlayer(
        Vector3 respawnPosition)
    {
        // ----------------------------------------------
        // MOVE PLAYER
        // ----------------------------------------------

        transform.position =
            respawnPosition;


        // ----------------------------------------------
        // RESET RIGIDBODY VALUES
        // ----------------------------------------------

        if (playerRigidbody != null)
        {
            playerRigidbody.linearVelocity =
                Vector2.zero;

            playerRigidbody.angularVelocity =
                0f;
        }


        // ----------------------------------------------
        // PLAYER STARTS INVISIBLE
        // ----------------------------------------------

        SetPlayerOpacity(
            0f
        );


        // ----------------------------------------------
        // ENABLE ANIMATOR
        // ----------------------------------------------

        if (playerAnimator != null)
        {
            playerAnimator.enabled =
                true;
        }


        // ----------------------------------------------
        // FADE PLAYER BACK IN
        // ----------------------------------------------

        yield return StartCoroutine(
            FadePlayer(
                0f,
                1f,
                fadeInDuration
            )
        );


        // ==================================================
        // RESTORE PLAYER PHYSICS
        // ==================================================

        if (playerRigidbody != null)
        {
            /*
             * VERY IMPORTANT:
             *
             * Rigidbody must become simulated again
             * or triggers/collisions will not work.
             */
            playerRigidbody.simulated =
                true;


            playerRigidbody.linearVelocity =
                Vector2.zero;

            playerRigidbody.angularVelocity =
                0f;
        }


        // ==================================================
        // RESTORE COLLIDERS
        // ==================================================

        /*
         * VERY IMPORTANT:
         *
         * The trap needs these colliders enabled
         * to detect the player again.
         */
        SetPlayerColliders(
            true
        );


        // ==================================================
        // RESTORE MOVEMENT
        // ==================================================

        if (playerMovement != null)
        {
            playerMovement.enabled =
                true;
        }


        // ==================================================
        // PLAYER IS ALIVE AGAIN
        // ==================================================

        /*
         * MOST IMPORTANT FIX:
         *
         * TrapHazard checks:
         *
         * if (playerDeath.IsDead)
         *     return;
         *
         * Therefore we MUST reset this to false
         * after every successful respawn.
         */
        isDead =
            false;


        Debug.Log(
            "Player respawn complete. " +
            "Player can die again. Lives remaining: " +
            currentLives,
            this
        );
    }


    // ==================================================
    // FIND NEAREST RESPAWN POINT
    // ==================================================

    private RespawnPoint FindNearestRespawnPoint(
        Vector3 deathPosition)
    {
        RespawnPoint[] respawnPoints =
            FindObjectsByType<RespawnPoint>(
                FindObjectsSortMode.None
            );


        RespawnPoint nearestPoint =
            null;


        float nearestDistanceSquared =
            float.PositiveInfinity;


        foreach (
            RespawnPoint point
            in respawnPoints
        )
        {
            if (point == null)
                continue;


            // ------------------------------------------
            // CHECK RESPAWN LAYER
            // ------------------------------------------

            if (!IsRespawnLayerAllowed(
                    point.gameObject.layer
                ))
            {
                continue;
            }


            Vector3 pointPosition =
                point.GetRespawnPosition();


            float distanceSquared =
                (
                    pointPosition -
                    deathPosition
                ).sqrMagnitude;


            if (distanceSquared <
                nearestDistanceSquared)
            {
                nearestDistanceSquared =
                    distanceSquared;

                nearestPoint =
                    point;
            }
        }


        return nearestPoint;
    }


    // ==================================================
    // RESPAWN LAYER CHECK
    // ==================================================

    private bool IsRespawnLayerAllowed(
        int objectLayer)
    {
        /*
         * If no LayerMask is selected,
         * allow every RespawnPoint.
         */
        if (respawnPointLayer.value == 0)
        {
            return true;
        }


        int objectLayerMask =
            1 << objectLayer;


        return (
            respawnPointLayer.value &
            objectLayerMask
        ) != 0;
    }


    // ==================================================
    // DEATH PARTICLES
    // ==================================================

    private void SpawnDeathParticles()
    {
        if (deathParticlePrefab == null)
            return;


        ParticleSystem particles =
            Instantiate(
                deathParticlePrefab,
                transform.position,
                Quaternion.identity
            );


        particles.Play();


        /*
         * Cleanup temporary particle object.
         */
        Destroy(
            particles.gameObject,
            5f
        );
    }


    // ==================================================
    // PLAYER COLLIDERS
    // ==================================================

    private void SetPlayerColliders(
        bool enabledState)
    {
        if (playerColliders == null)
            return;


        foreach (
            Collider2D playerCollider
            in playerColliders
        )
        {
            if (playerCollider == null)
                continue;


            playerCollider.enabled =
                enabledState;
        }
    }


    // ==================================================
    // PLAYER FADE
    // ==================================================

    private IEnumerator FadePlayer(
        float startOpacity,
        float targetOpacity,
        float duration)
    {
        if (playerRenderers == null ||
            playerRenderers.Length == 0)
        {
            yield break;
        }


        if (duration <= 0f)
        {
            SetPlayerOpacity(
                targetOpacity
            );

            yield break;
        }


        float elapsed =
            0f;


        while (elapsed < duration)
        {
            elapsed +=
                Time.unscaledDeltaTime;


            float normalizedTime =
                Mathf.Clamp01(
                    elapsed /
                    duration
                );


            float curveValue;


            if (fadeCurve != null &&
                fadeCurve.length > 0)
            {
                curveValue =
                    fadeCurve.Evaluate(
                        normalizedTime
                    );
            }
            else
            {
                curveValue =
                    normalizedTime;
            }


            float opacity =
                Mathf.Lerp(
                    startOpacity,
                    targetOpacity,
                    curveValue
                );


            SetPlayerOpacity(
                opacity
            );


            yield return null;
        }


        SetPlayerOpacity(
            targetOpacity
        );
    }


    // ==================================================
    // SET PLAYER OPACITY
    // ==================================================

    private void SetPlayerOpacity(
        float opacity)
    {
        if (playerRenderers == null)
            return;


        opacity =
            Mathf.Clamp01(
                opacity
            );


        for (int i = 0;
             i < playerRenderers.Length;
             i++)
        {
            SpriteRenderer renderer =
                playerRenderers[i];


            if (renderer == null)
                continue;


            Color originalColor;


            if (originalRendererColors != null &&
                i <
                originalRendererColors.Length)
            {
                originalColor =
                    originalRendererColors[i];
            }
            else
            {
                originalColor =
                    renderer.color;
            }


            originalColor.a *=
                opacity;


            renderer.color =
                originalColor;
        }
    }


    // ==================================================
    // FINAL DEATH
    // ==================================================

    private void ReloadLevelAfterFinalDeath()
    {
        /*
         * Preferred reload method.
         */
        if (uiManager != null)
        {
            uiManager.ReloadAfterPlayerDeath();

            return;
        }


        /*
         * Safety fallback if UIManager
         * was not found.
         */
        Time.timeScale =
            1f;


        AudioListener.pause =
            false;


        Scene currentScene =
            SceneManager.GetActiveScene();


        SceneManager.LoadScene(
            currentScene.buildIndex
        );
    }


    // ==================================================
    // LIVES UI
    // ==================================================

    private void UpdateLivesUI()
    {
        if (livesUI == null)
            return;


        livesUI.UpdateLives(
            currentLives
        );
    }


    // ==================================================
    // VALIDATION
    // ==================================================

    private void OnValidate()
    {
        maxLives =
            Mathf.Max(
                1,
                maxLives
            );


        respawnDelay =
            Mathf.Max(
                0f,
                respawnDelay
            );


        deathDelay =
            Mathf.Max(
                0f,
                deathDelay
            );


        soundEndPadding =
            Mathf.Max(
                0f,
                soundEndPadding
            );


        fadeOutDuration =
            Mathf.Max(
                0.01f,
                fadeOutDuration
            );


        fadeInDuration =
            Mathf.Max(
                0.01f,
                fadeInDuration
            );


        if (fadeCurve == null ||
            fadeCurve.length == 0)
        {
            fadeCurve =
                AnimationCurve.EaseInOut(
                    0f,
                    0f,
                    1f,
                    1f
                );
        }
    }
}