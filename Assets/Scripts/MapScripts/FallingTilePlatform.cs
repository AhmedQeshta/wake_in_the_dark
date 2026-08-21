using System.Collections;
using UnityEngine;
using UnityEngine.Tilemaps;

public enum PlatformRespawnMode
{
    Never,
    AfterDelay
}

[RequireComponent(typeof(Tilemap))]
[RequireComponent(typeof(TilemapRenderer))]
[RequireComponent(typeof(TilemapCollider2D))]
[RequireComponent(typeof(CompositeCollider2D))] // Injected for Performance
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(AudioSource))]
public class FallingTilePlatform : MonoBehaviour
{
    private enum PlatformState
    {
        Ready,
        Shaking,
        Falling,
        FadingOut,
        Hidden,
        Respawning
    }

    [Header("Activation")]
    [SerializeField] private string playerTag = "Player";
    [SerializeField, Range(0f, 1f)] private float topContactThreshold = 0.5f;
    [SerializeField, Min(0.01f)] private float landingTolerance = 0.2f;

    [Header("Fall")]
    [SerializeField, Min(0f)] private float fallDelay = 3f;
    [SerializeField] private bool shakeBeforeFalling = true;
    [SerializeField, Min(0f)] private float shakeStrength = 0.04f;
    [SerializeField, Min(0f)] private float shakeSpeed = 25f;
    [SerializeField, Min(0f)] private float fallingGravityScale = 2f;

    [Header("Fade Animation")]
    [SerializeField, Min(0.01f)] private float fadeOutDuration = 0.5f;
    [SerializeField, Min(0.01f)] private float fadeInDuration = 0.75f;
    [SerializeField] private AnimationCurve fadeCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Disappear")]
    [SerializeField] private bool disappearAfterFalling = true;
    [SerializeField, Min(0f)] private float disappearDelay = 2f;

    [Header("Respawn")]
    [SerializeField] private PlatformRespawnMode respawnMode = PlatformRespawnMode.AfterDelay;
    [SerializeField, Min(0f)] private float respawnDelay = 10f;

    [Header("Audio")]
    [SerializeField] private AudioClip activationSound;
    [SerializeField, Range(0f, 1f)] private float activationSoundVolume = 1f;
    [SerializeField] private bool randomizeSoundPitch = false;
    [SerializeField, Range(0.5f, 1.5f)] private float minimumPitch = 0.95f;
    [SerializeField, Range(0.5f, 1.5f)] private float maximumPitch = 1.05f;

    [Header("Debug")]
    [SerializeField] private bool enableKeyboardTest = false;

    private Tilemap tilemap;
    private TilemapRenderer tilemapRenderer;
    private TilemapCollider2D tilemapCollider;
    private CompositeCollider2D compositeCollider;
    private Rigidbody2D rb;
    private AudioSource audioSource;

    private Color originalTilemapColor;
    private Vector3 originalLocalPosition;
    private Quaternion originalLocalRotation;

    private Coroutine activeRoutine;
    private bool isActivated;
    private bool hasPlayedActivationSound;
    private PlatformState platformState = PlatformState.Ready;

    private const float FallingVelocityThreshold = 0.1f;


    [Header("Attached Objects")]
    [SerializeField] private GameObject attachmentsRoot;
    [SerializeField] private Collider2D[] attachmentInteractionColliders;
    [SerializeField] private LeverSwitch attachedLever;

    private SpriteRenderer[] attachmentRenderers;
    private Color[] originalAttachmentColors;


    private void Awake()
    {
        tilemap = GetComponent<Tilemap>();
        tilemapRenderer = GetComponent<TilemapRenderer>();
        tilemapCollider = GetComponent<TilemapCollider2D>();
        compositeCollider = GetComponent<CompositeCollider2D>();
        rb = GetComponent<Rigidbody2D>();
        audioSource = GetComponent<AudioSource>();

        if (tilemap == null || tilemapRenderer == null || tilemapCollider == null || rb == null || audioSource == null)
        {
            Debug.LogError($"{nameof(FallingTilePlatform)} requires Tilemap, TilemapRenderer, TilemapCollider2D, Rigidbody2D, and AudioSource on the same GameObject.", this);
            enabled = false;
            return;
        }

        originalTilemapColor = tilemap.color;
        originalLocalPosition = transform.localPosition;
        originalLocalRotation = transform.localRotation;

        // --- PERFORMANCE OPTIMIZATIONS ---
        tilemapCollider.usedByComposite = true;
        compositeCollider.geometryType = CompositeCollider2D.GeometryType.Polygons;

        rb.simulated = true;
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Discrete;
        rb.gravityScale = 0f;
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
        // ---------------------------------

        audioSource.playOnAwake = false;
        audioSource.loop = false;

        // Cache attachment SpriteRenderers before initial setup.
        if (attachmentsRoot != null)
        {
            attachmentRenderers = attachmentsRoot.GetComponentsInChildren<SpriteRenderer>(true);
            originalAttachmentColors = new Color[attachmentRenderers.Length];

            for (int i = 0; i < attachmentRenderers.Length; i++)
            {
                originalAttachmentColors[i] = attachmentRenderers[i].color;
            }
        }
        else
        {
            attachmentRenderers = System.Array.Empty<SpriteRenderer>();
            originalAttachmentColors = System.Array.Empty<Color>();
        }

        // Initial setup after caching attachments.
        SetVisualOpacity(1f);
        tilemapRenderer.enabled = true;
        tilemapCollider.enabled = true;
        isActivated = false;
        hasPlayedActivationSound = false;
        platformState = PlatformState.Ready;
        activeRoutine = null;

        // Enable attachments in initial state.
        if (attachmentsRoot != null)
        {
            attachmentsRoot.SetActive(true);
        }

        if (attachedLever != null)
        {
            attachedLever.RestoreVisualState();
        }

        SetAttachmentsInteractive(true);
    }

    private void Update()
    {
        // Temporary keyboard test for Rigidbody behavior; disable after testing.
        if (enableKeyboardTest && Input.GetKeyDown(KeyCode.F) && !isActivated)
        {
            isActivated = true;
            ActivatePlatform();
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (isActivated || platformState != PlatformState.Ready)
        {
            return;
        }

        if (!collision.gameObject.CompareTag(playerTag))
        {
            return;
        }

        if (!IsPlayerLandingFromAbove(collision))
        {
            return;
        }

        ActivatePlatform();
    }

    private bool IsPlayerLandingFromAbove(Collision2D collision)
    {
        Rigidbody2D playerRb = collision.gameObject.GetComponent<Rigidbody2D>();
        if (playerRb == null)
        {
            return false;
        }

        if (playerRb.linearVelocity.y > FallingVelocityThreshold)
        {
            return false;
        }

        float platformTop = tilemapCollider.bounds.max.y;

        for (int index = 0; index < collision.contactCount; index++)
        {
            ContactPoint2D contact = collision.GetContact(index);
            if (contact.point.y >= platformTop - landingTolerance)
            {
                return true;
            }
        }

        if (topContactThreshold > 0f)
        {
            for (int index = 0; index < collision.contactCount; index++)
            {
                ContactPoint2D contact = collision.GetContact(index);
                if (contact.normal.y >= topContactThreshold)
                {
                    return true;
                }
            }
        }

        return false;
    }

    private void ActivatePlatform()
    {
        if (isActivated || activeRoutine != null || platformState != PlatformState.Ready)
        {
            return;
        }

        isActivated = true;
        PlayActivationSound();
        platformState = PlatformState.Shaking;
        activeRoutine = StartCoroutine(FallSequence());
    }

    private void PlayActivationSound()
    {
        if (hasPlayedActivationSound)
        {
            return;
        }

        hasPlayedActivationSound = true;

        if (activationSound == null || audioSource == null)
        {
            return;
        }

        audioSource.pitch = randomizeSoundPitch
            ? Random.Range(minimumPitch, maximumPitch)
            : 1f;

        audioSource.PlayOneShot(activationSound, activationSoundVolume);
    }

    private IEnumerator FallSequence()
    {
        platformState = PlatformState.Shaking;

        if (shakeBeforeFalling && fallDelay > 0f)
        {
            float elapsed = 0f;
            while (elapsed < fallDelay)
            {
                ShakePlatform(elapsed);
                elapsed += Time.deltaTime;
                yield return null;
            }
        }
        else if (fallDelay > 0f)
        {
            yield return new WaitForSeconds(fallDelay);
        }

        transform.localPosition = originalLocalPosition;
        transform.localRotation = originalLocalRotation;

        BeginFalling();

        if (respawnMode == PlatformRespawnMode.Never)
        {
            activeRoutine = null;
            yield break;
        }

        if (disappearDelay > 0f)
        {
            yield return new WaitForSeconds(disappearDelay);
        }

        if (disappearAfterFalling)
        {
            yield return StartCoroutine(FadeOutAndHide());

            if (respawnMode == PlatformRespawnMode.AfterDelay)
            {
                if (respawnDelay > 0f)
                {
                    yield return new WaitForSeconds(respawnDelay);
                }

                yield return StartCoroutine(RespawnWithFade());
            }
        }
        else
        {
            if (respawnMode == PlatformRespawnMode.AfterDelay)
            {
                if (respawnDelay > 0f)
                {
                    yield return new WaitForSeconds(respawnDelay);
                }

                yield return StartCoroutine(RespawnWithFade());
            }
        }

        activeRoutine = null;
    }

    private void ShakePlatform(float elapsed)
    {
        float duration = Mathf.Max(fallDelay, 0.0001f);
        float progress = Mathf.Clamp01(elapsed / duration);
        float strength = Mathf.Lerp(shakeStrength, shakeStrength * 1.5f, progress);

        // Calculate the random offset
        Vector2 offset = Random.insideUnitCircle * strength * Mathf.Sin(elapsed * shakeSpeed);

        // Apply it directly to the transform for immediate visual updates
        transform.localPosition = originalLocalPosition + new Vector3(offset.x, offset.y, 0f);
        transform.localRotation = originalLocalRotation;
    }

    private void BeginFalling()
    {
        transform.localPosition = originalLocalPosition;
        transform.localRotation = originalLocalRotation;

        rb.simulated = true;
        rb.bodyType = RigidbodyType2D.Dynamic;

        // When dynamic, switch to continuous to prevent falling through the floor map bounds
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        rb.gravityScale = fallingGravityScale;
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;

        platformState = PlatformState.Falling;
    }

    private void SetVisualOpacity(float opacity)
    {
        float normalizedOpacity = Mathf.Clamp01(opacity);

        // Fade the platform Tilemap.
        Color tileColor = originalTilemapColor;
        tileColor.a = originalTilemapColor.a * normalizedOpacity;
        tilemap.color = tileColor;

        // Fade the lever and other attached sprites.
        for (int i = 0; i < attachmentRenderers.Length; i++)
        {
            if (attachmentRenderers[i] == null)
                continue;

            Color attachmentColor = originalAttachmentColors[i];
            attachmentColor.a = originalAttachmentColors[i].a * normalizedOpacity;
            attachmentRenderers[i].color = attachmentColor;
        }
    }

    private void SetAttachmentsInteractive(bool interactive)
    {
        if (attachmentInteractionColliders == null)
            return;

        foreach (Collider2D interactionCollider in attachmentInteractionColliders)
        {
            if (interactionCollider != null)
            {
                interactionCollider.enabled = interactive;
            }
        }
    }

    private IEnumerator FadeOpacity(float startOpacity, float targetOpacity, float duration)
    {
        if (duration <= 0f)
        {
            SetVisualOpacity(targetOpacity);
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            float normalizedTime = Mathf.Clamp01(elapsed / duration);
            float curveValue = fadeCurve != null && fadeCurve.length > 0
                ? fadeCurve.Evaluate(normalizedTime)
                : normalizedTime;

            float opacity = Mathf.Lerp(startOpacity, targetOpacity, curveValue);
            SetVisualOpacity(opacity);
            elapsed += Time.deltaTime;
            yield return null;
        }

        SetVisualOpacity(targetOpacity);
    }

    private IEnumerator FadeOutAndHide()
    {
        if (platformState == PlatformState.Hidden)
        {
            yield break;
        }

        platformState = PlatformState.FadingOut;
        tilemapCollider.enabled = false;
        SetAttachmentsInteractive(false);

        // Keep the lever active while fading.
        yield return FadeOpacity(1f, 0f, fadeOutDuration);

        tilemapRenderer.enabled = false;
        rb.simulated = false;
        platformState = PlatformState.Hidden;

        // Hide the lever only after opacity reaches zero.
        if (attachmentsRoot != null)
        {
            attachmentsRoot.SetActive(false);
        }
    }

    private IEnumerator RespawnWithFade()
    {
        platformState = PlatformState.Respawning;

        rb.simulated = false;
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Discrete;
        rb.gravityScale = 0f;
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;

        transform.localPosition = originalLocalPosition;
        transform.localRotation = originalLocalRotation;

        SetVisualOpacity(0f);
        tilemapRenderer.enabled = true;
        tilemapCollider.enabled = false;

        if (attachmentsRoot != null)
        {
            attachmentsRoot.SetActive(true);
        }

        if (attachedLever != null)
        {
            attachedLever.RestoreVisualState();
        }

        SetAttachmentsInteractive(false);

        yield return FadeOpacity(0f, 1f, fadeInDuration);

        SetVisualOpacity(1f);
        tilemapCollider.enabled = true;
        rb.simulated = true;
        hasPlayedActivationSound = false;
        platformState = PlatformState.Ready;

        // Enable the lever only when fully visible.
        SetAttachmentsInteractive(true);

        isActivated = false;
        activeRoutine = null;
    }

    private void ResetPlatformImmediately()
    {
        rb.simulated = false;
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
        transform.localPosition = originalLocalPosition;
        transform.localRotation = originalLocalRotation;
        SetVisualOpacity(1f);
        tilemapRenderer.enabled = true;
        tilemapCollider.enabled = true;

        // Restore attachment visibility and interactivity.
        if (attachmentsRoot != null)
        {
            attachmentsRoot.SetActive(true);
        }

        if (attachedLever != null)
        {
            attachedLever.RestoreVisualState();
        }

        SetAttachmentsInteractive(true);

        rb.simulated = true;
        isActivated = false;
        hasPlayedActivationSound = false;
        platformState = PlatformState.Ready;
        activeRoutine = null;
    }

    private void OnDisable()
    {
        if (activeRoutine != null)
        {
            StopCoroutine(activeRoutine);
            activeRoutine = null;
        }

        if (audioSource != null)
        {
            audioSource.pitch = 1f;
        }

        if (tilemap != null)
        {
            SetVisualOpacity(1f);
        }
    }

    private void OnValidate()
    {
        if (string.IsNullOrWhiteSpace(playerTag))
        {
            playerTag = "Player";
        }

        fadeOutDuration = Mathf.Max(0.01f, fadeOutDuration);
        fadeInDuration = Mathf.Max(0.01f, fadeInDuration);
        fallDelay = Mathf.Max(0f, fallDelay);
        shakeStrength = Mathf.Max(0f, shakeStrength);
        shakeSpeed = Mathf.Max(0f, shakeSpeed);
        fallingGravityScale = Mathf.Max(0f, fallingGravityScale);
        disappearDelay = Mathf.Max(0f, disappearDelay);
        respawnDelay = Mathf.Max(0f, respawnDelay);
        landingTolerance = Mathf.Max(0.01f, landingTolerance);
        activationSoundVolume = Mathf.Clamp01(activationSoundVolume);
        minimumPitch = Mathf.Max(0.5f, minimumPitch);
        maximumPitch = Mathf.Min(1.5f, maximumPitch);

        if (minimumPitch > maximumPitch)
        {
            float pitchSwap = minimumPitch;
            minimumPitch = maximumPitch;
            maximumPitch = pitchSwap;
        }

        if (fadeCurve == null || fadeCurve.length == 0)
        {
            fadeCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        }

        if (!Application.isPlaying)
        {
            tilemap = GetComponent<Tilemap>();
            tilemapRenderer = GetComponent<TilemapRenderer>();
            tilemapCollider = GetComponent<TilemapCollider2D>();
            rb = GetComponent<Rigidbody2D>();

            // Auto-configure the composite collider requirement so it doesn't break
            if (tilemapCollider != null)
            {
                tilemapCollider.usedByComposite = true;
            }
        }
    }
}