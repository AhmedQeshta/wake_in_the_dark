using System.Collections;
using UnityEngine;

public class PlayerDeath : MonoBehaviour
{
    // ==================================================
    // DEATH SETTINGS
    // ==================================================

    [Header("Death Settings")]

    [Tooltip("Minimum time before reloading the level.")]
    [SerializeField, Min(0f)]
    private float reloadDelay = 1f;

    [Tooltip("Small extra delay after the trap sound finishes.")]
    [SerializeField, Min(0f)]
    private float soundEndPadding = 0.1f;


    // ==================================================
    // PLAYER FADE
    // ==================================================

    [Header("Player Fade")]

    [Tooltip("How long the player takes to fade away.")]
    [SerializeField, Min(0.01f)]
    private float playerFadeDuration = 0.4f;

    [Tooltip("Controls how smooth the fade looks.")]
    [SerializeField]
    private AnimationCurve fadeCurve =
        AnimationCurve.EaseInOut(
            0f,
            0f,
            1f,
            1f
        );


    // ==================================================
    // PARTICLES
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
    // UI MANAGER
    // ==================================================

    [Header("Level Manager")]

    [SerializeField]
    private UIManager uiManager;


    // ==================================================
    // STATE
    // ==================================================

    private bool isDead;


    public bool IsDead => isDead;


    // ==================================================
    // AWAKE
    // ==================================================

    private void Awake()
    {
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
        // UI MANAGER
        // ----------------------------------------------

        if (uiManager == null)
        {
            uiManager =
                FindFirstObjectByType<UIManager>();
        }
    }


    // ==================================================
    // KILL PLAYER
    // ==================================================

    public void KillPlayer(
        float trapSoundDuration = 0f)
    {
        /*
         * Don't kill the player twice.
         */
        if (isDead)
            return;


        isDead = true;


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
        // ----------------------------------------------
        // STOP PLAYER MOVEMENT
        // ----------------------------------------------

        if (playerMovement != null)
        {
            playerMovement.enabled = false;
        }


        // ----------------------------------------------
        // STOP PLAYER PHYSICS
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
        // STOP PLAYER ANIMATION
        // ----------------------------------------------

        if (playerAnimator != null)
        {
            playerAnimator.enabled =
                false;
        }


        // ----------------------------------------------
        // DISABLE COLLIDERS
        // ----------------------------------------------

        if (playerColliders != null)
        {
            foreach (
                Collider2D playerCollider
                in playerColliders
            )
            {
                if (playerCollider == null)
                    continue;


                playerCollider.enabled =
                    false;
            }
        }


        // ----------------------------------------------
        // SPAWN PARTICLE EFFECT
        // ----------------------------------------------

        if (deathParticlePrefab != null)
        {
            ParticleSystem particles =
                Instantiate(
                    deathParticlePrefab,
                    transform.position,
                    Quaternion.identity
                );


            particles.Play();


            Destroy(
                particles.gameObject,
                5f
            );
        }


        // ----------------------------------------------
        // FADE PLAYER
        // ----------------------------------------------

        /*
         * Start player fade at the same time
         * as the particle effect.
         */
        StartCoroutine(
            FadePlayer()
        );


        // ----------------------------------------------
        // WAIT BEFORE RELOAD
        // ----------------------------------------------

        /*
         * Wait long enough for:
         *
         * - minimum reload delay
         * - trap sound
         * - player fade
         *
         * whichever is longest.
         */

        float totalWaitTime =
            Mathf.Max(
                reloadDelay,
                trapSoundDuration +
                soundEndPadding,
                playerFadeDuration
            );


        yield return new WaitForSecondsRealtime(
            totalWaitTime
        );


        // ----------------------------------------------
        // RELOAD LEVEL
        // ----------------------------------------------

        if (uiManager != null)
        {
            uiManager.ReloadAfterPlayerDeath();
        }
        else
        {
            Debug.LogError(
                "PlayerDeath: UIManager was not found.",
                this
            );
        }
    }


    // ==================================================
    // PLAYER FADE
    // ==================================================

    private IEnumerator FadePlayer()
    {
        if (playerRenderers == null ||
            playerRenderers.Length == 0)
        {
            yield break;
        }


        /*
         * Save each renderer's original color.
         *
         * This lets us keep its RGB color
         * while only changing alpha.
         */
        Color[] startingColors =
            new Color[playerRenderers.Length];


        for (int i = 0;
             i < playerRenderers.Length;
             i++)
        {
            if (playerRenderers[i] != null)
            {
                startingColors[i] =
                    playerRenderers[i].color;
            }
        }


        float elapsed = 0f;


        while (elapsed < playerFadeDuration)
        {
            elapsed +=
                Time.unscaledDeltaTime;


            float normalizedTime =
                Mathf.Clamp01(
                    elapsed /
                    playerFadeDuration
                );


            float curveValue =
                fadeCurve != null
                ? fadeCurve.Evaluate(
                    normalizedTime
                )
                : normalizedTime;


            /*
             * 1 → 0
             */
            float alpha =
                1f - curveValue;


            for (int i = 0;
                 i < playerRenderers.Length;
                 i++)
            {
                SpriteRenderer renderer =
                    playerRenderers[i];


                if (renderer == null)
                    continue;


                Color color =
                    startingColors[i];


                /*
                 * Preserve original alpha too.
                 */
                color.a =
                    startingColors[i].a *
                    alpha;


                renderer.color =
                    color;
            }


            yield return null;
        }


        // ----------------------------------------------
        // FORCE FULL TRANSPARENCY
        // ----------------------------------------------

        for (int i = 0;
             i < playerRenderers.Length;
             i++)
        {
            SpriteRenderer renderer =
                playerRenderers[i];


            if (renderer == null)
                continue;


            Color color =
                renderer.color;


            color.a = 0f;


            renderer.color =
                color;
        }
    }


    // ==================================================
    // VALIDATION
    // ==================================================

    private void OnValidate()
    {
        reloadDelay =
            Mathf.Max(
                0f,
                reloadDelay
            );


        soundEndPadding =
            Mathf.Max(
                0f,
                soundEndPadding
            );


        playerFadeDuration =
            Mathf.Max(
                0.01f,
                playerFadeDuration
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