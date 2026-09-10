using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Tilemaps;


[RequireComponent(typeof(Tilemap))]
[RequireComponent(typeof(TilemapRenderer))]
[RequireComponent(typeof(TilemapCollider2D))]
[RequireComponent(typeof(CompositeCollider2D))]
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(AudioSource))]
public class FallingTilePlatform : MonoBehaviour
{
    // STATE TYPES
    private enum PlatformState { Ready, Shaking, Falling, FadingOut, Hidden, Respawning }
    public enum PlatformRespawnMode { Never, AfterDelay }

    // ACTIVATION
    [Header("Activation")]
    [SerializeField] private string playerTag = "Player";
    [SerializeField, Range(0f, 1f)] private float topContactThreshold = 0.5f;
    [SerializeField, Min(0.01f)] private float landingTolerance = 0.2f;

    // FALL
    [Header("Fall")]
    [SerializeField, Min(0f)] private float fallDelay = 3f;
    [SerializeField] private bool shakeBeforeFalling = true;
    [SerializeField, Min(0f)] private float shakeStrength = 0.04f;
    [SerializeField, Min(0f)] private float shakeSpeed = 25f;
    [SerializeField, Min(0f)] private float fallingGravityScale = 2f;


    // FADE
    [Header("Fade Animation")]
    [SerializeField, Min(0.01f)] private float fadeOutDuration = 0.5f;
    [SerializeField, Min(0.01f)] private float fadeInDuration = 0.75f;
    [SerializeField] private AnimationCurve fadeCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    // DISAPPEAR
    [Header("Disappear")]
    [SerializeField] private bool disappearAfterFalling = true;
    [SerializeField, Min(0f)] private float disappearDelay = 2f;


    // RESPAWN

    [Header("Respawn")]
    [SerializeField] private PlatformRespawnMode respawnMode = PlatformRespawnMode.AfterDelay;
    [SerializeField, Min(0f)] private float respawnDelay = 10f;

    // AUDIO
    [Header("Audio")]
    [SerializeField] private AudioClip activationSound;
    [SerializeField, Range(0f, 1f)] private float activationSoundVolume = 1f;
    [SerializeField] private bool randomizeSoundPitch = false;
    [SerializeField, Range(0.5f, 1.5f)] private float minimumPitch = 0.95f;


    [SerializeField, Range(0.5f, 1.5f)] private float maximumPitch = 1.05f;
    // ATTACHED OBJECTS
    [Header("Attached Objects")]

    [Tooltip("The root that contains everything riding on this falling platform: " + "land, traps, decoration, lever, dangerous lights, smoke, etc.")]
    [SerializeField] private GameObject attachmentsRoot;

    [Tooltip("Legacy/manual collider list. Kept for compatibility. " + "When Auto Manage All Attachment Colliders is ON, every collider " + "under Attachments Root is handled automatically.")]
    [SerializeField] private Collider2D[] attachmentInteractionColliders;

    [SerializeField] private LeverSwitch attachedLever;
    [Tooltip("Recommended ON. Automatically disables every Collider2D under " + "Attachments Root while hidden and restores each collider's original state.")]
    [SerializeField] private bool autoManageAllAttachmentColliders = true;

    [Tooltip("Fade SpriteRenderers, child Tilemaps, 2D Lights, CanvasGroups, " + "and compatible generic Renderers under Attachments Root.")]
    [SerializeField] private bool fadeAllAttachmentVisuals = true;


    // DEBUG

    [Header("Debug")]
    [SerializeField] private bool enableKeyboardTest = false;


    // MAIN COMPONENTS

    private Tilemap tilemap;
    private TilemapRenderer tilemapRenderer;
    private TilemapCollider2D tilemapCollider;
    private CompositeCollider2D compositeCollider;
    private Rigidbody2D rb;
    private AudioSource audioSource;


    // MAIN ORIGINAL STATE

    private Color originalTilemapColor;
    private Vector3 originalLocalPosition;
    private Quaternion originalLocalRotation;


    // PLATFORM STATE

    private Coroutine activeRoutine;

    private bool isActivated;
    private bool hasPlayedActivationSound;

    private PlatformState platformState = PlatformState.Ready;


    private const float FallingVelocityThreshold = 0.1f;


    // ATTACHMENT VISUAL CACHE

    private SpriteRenderer[] attachmentSpriteRenderers = Array.Empty<SpriteRenderer>();

    private Color[] originalAttachmentSpriteColors = Array.Empty<Color>();


    private Tilemap[] attachmentTilemaps = Array.Empty<Tilemap>();

    private Color[] originalAttachmentTilemapColors = Array.Empty<Color>();


    private Light2D[] attachmentLights = Array.Empty<Light2D>();

    private float[] originalAttachmentLightIntensities = Array.Empty<float>();


    private CanvasGroup[] attachmentCanvasGroups = Array.Empty<CanvasGroup>();
    private float[] originalAttachmentCanvasAlphas = Array.Empty<float>();


    /*
     * Generic Renderers cover things such as ParticleSystemRenderer,
     * LineRenderer, TrailRenderer, or other renderers whose material
     * exposes _BaseColor or _Color.
     */
    private Renderer[] attachmentGenericRenderers = Array.Empty<Renderer>();
    private Color[] originalGenericRendererColors = Array.Empty<Color>();
    private int[] genericRendererColorPropertyIds = Array.Empty<int>();
    private MaterialPropertyBlock genericRendererPropertyBlock;


    // ATTACHMENT SHADOW CACHE

    private ShadowCaster2D[] attachmentShadowCasters = Array.Empty<ShadowCaster2D>();
    private bool[] originalShadowCastingStates = Array.Empty<bool>();


    // ATTACHMENT COLLIDER CACHE

    private Collider2D[] managedAttachmentColliders = Array.Empty<Collider2D>();
    private bool[] originalAttachmentColliderStates = Array.Empty<bool>();


    // AWAKE

    private void Awake()
    {
        tilemap = GetComponent<Tilemap>();
        tilemapRenderer = GetComponent<TilemapRenderer>();
        tilemapCollider = GetComponent<TilemapCollider2D>();
        compositeCollider = GetComponent<CompositeCollider2D>();
        rb = GetComponent<Rigidbody2D>();
        audioSource = GetComponent<AudioSource>();

        if (tilemap == null || tilemapRenderer == null || tilemapCollider == null || compositeCollider == null || rb == null || audioSource == null)
        {
            Debug.LogError("FallingTilePlatform: Required component is missing.", this);
            enabled = false;
            return;
        }

        originalTilemapColor = tilemap.color;
        originalLocalPosition = transform.localPosition;
        originalLocalRotation = transform.localRotation;

        // PHYSICS SETUP
        tilemapCollider.usedByComposite = true;
        compositeCollider.geometryType = CompositeCollider2D.GeometryType.Polygons;

        rb.simulated = true;
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Discrete;
        rb.gravityScale = 0f;
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;

        // AUDIO SETUP
        audioSource.playOnAwake = false;
        audioSource.loop = false;

        // ATTACHMENTS
        CacheAttachmentState();

        if (attachmentsRoot != null)
            attachmentsRoot.SetActive(true);


        SetVisualOpacity(1f);

        SetAttachmentShadowCasting(true);

        RestoreAttachmentColliderStates();

        if (attachedLever != null)
            attachedLever.RestoreVisualState();

        // INITIAL PLATFORM STATE
        tilemapRenderer.enabled = true;
        tilemapCollider.enabled = true;
        isActivated = false;
        hasPlayedActivationSound = false;
        platformState = PlatformState.Ready;
        activeRoutine = null;
    }


    // CACHE ATTACHMENTS

    private void CacheAttachmentState()
    {
        if (attachmentsRoot == null)
        {
            ClearAttachmentCache();
            return;
        }

        // SPRITES
        attachmentSpriteRenderers = attachmentsRoot.GetComponentsInChildren<SpriteRenderer>(true);

        originalAttachmentSpriteColors = new Color[attachmentSpriteRenderers.Length];

        for (int i = 0; i < attachmentSpriteRenderers.Length; i++)
        {
            SpriteRenderer renderer = attachmentSpriteRenderers[i];

            originalAttachmentSpriteColors[i] = renderer != null ? renderer.color : Color.white;
        }

        // TILEMAPS
        attachmentTilemaps = attachmentsRoot.GetComponentsInChildren<Tilemap>(true);

        originalAttachmentTilemapColors = new Color[attachmentTilemaps.Length];

        for (int i = 0; i < attachmentTilemaps.Length; i++)
        {
            Tilemap childTilemap = attachmentTilemaps[i];

            originalAttachmentTilemapColors[i] = childTilemap != null ? childTilemap.color : Color.white;
        }

        // 2D LIGHTS
        attachmentLights = attachmentsRoot.GetComponentsInChildren<Light2D>(true);

        originalAttachmentLightIntensities = new float[attachmentLights.Length];

        for (int i = 0; i < attachmentLights.Length; i++)
        {
            Light2D light = attachmentLights[i];

            originalAttachmentLightIntensities[i] = light != null ? light.intensity : 0f;
        }

        // CANVAS GROUPS
        attachmentCanvasGroups = attachmentsRoot.GetComponentsInChildren<CanvasGroup>(true);

        originalAttachmentCanvasAlphas = new float[attachmentCanvasGroups.Length];

        for (int i = 0; i < attachmentCanvasGroups.Length; i++)
        {
            CanvasGroup group = attachmentCanvasGroups[i];

            originalAttachmentCanvasAlphas[i] = group != null ? group.alpha : 1f;
        }

        // GENERIC RENDERERS
        CacheGenericRenderers();

        // SHADOW CASTERS
        attachmentShadowCasters = attachmentsRoot.GetComponentsInChildren<ShadowCaster2D>(true);

        originalShadowCastingStates = new bool[attachmentShadowCasters.Length];

        for (int i = 0; i < attachmentShadowCasters.Length; i++)
        {
            ShadowCaster2D shadowCaster = attachmentShadowCasters[i];

            originalShadowCastingStates[i] = shadowCaster != null && shadowCaster.castsShadows;
        }

        // COLLIDERS
        managedAttachmentColliders = attachmentsRoot.GetComponentsInChildren<Collider2D>(true);

        originalAttachmentColliderStates = new bool[managedAttachmentColliders.Length];

        for (int i = 0; i < managedAttachmentColliders.Length; i++)
        {
            Collider2D childCollider = managedAttachmentColliders[i];

            originalAttachmentColliderStates[i] = childCollider != null && childCollider.enabled;
        }
    }


    private void CacheGenericRenderers()
    {
        Renderer[] allRenderers = attachmentsRoot.GetComponentsInChildren<Renderer>(true);

        List<Renderer> rendererList = new List<Renderer>();

        List<Color> colorList = new List<Color>();

        List<int> propertyIdList = new List<int>();

        int baseColorId = Shader.PropertyToID("_BaseColor");

        int colorId = Shader.PropertyToID("_Color");

        foreach (Renderer renderer in allRenderers)
        {
            if (renderer == null || renderer is SpriteRenderer || renderer is TilemapRenderer)
                continue;

            Material material = renderer.sharedMaterial;

            if (material == null)
                continue;

            int propertyId = 0;

            if (material.HasProperty(baseColorId))
                propertyId = baseColorId;
            else if (material.HasProperty(colorId))
                propertyId = colorId;

            if (propertyId == 0)
                continue;

            rendererList.Add(renderer);

            colorList.Add(material.GetColor(propertyId));

            propertyIdList.Add(propertyId);
        }

        attachmentGenericRenderers = rendererList.ToArray();
        originalGenericRendererColors = colorList.ToArray();
        genericRendererColorPropertyIds = propertyIdList.ToArray();
        genericRendererPropertyBlock = new MaterialPropertyBlock();
    }


    private void ClearAttachmentCache()
    {
        attachmentSpriteRenderers = Array.Empty<SpriteRenderer>();
        originalAttachmentSpriteColors = Array.Empty<Color>();
        attachmentTilemaps = Array.Empty<Tilemap>();
        originalAttachmentTilemapColors = Array.Empty<Color>();
        attachmentLights = Array.Empty<Light2D>();
        originalAttachmentLightIntensities = Array.Empty<float>();
        attachmentCanvasGroups = Array.Empty<CanvasGroup>();
        originalAttachmentCanvasAlphas = Array.Empty<float>();
        attachmentGenericRenderers = Array.Empty<Renderer>();
        originalGenericRendererColors = Array.Empty<Color>();
        genericRendererColorPropertyIds = Array.Empty<int>();
        attachmentShadowCasters = Array.Empty<ShadowCaster2D>();
        originalShadowCastingStates = Array.Empty<bool>();
        managedAttachmentColliders = Array.Empty<Collider2D>();
        originalAttachmentColliderStates = Array.Empty<bool>();
    }


    // UPDATE

    private void Update()
    {
        if (enableKeyboardTest && Input.GetKeyDown(KeyCode.F) && !isActivated)
            ActivatePlatform();
    }


    // PLAYER LANDING

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (isActivated || platformState != PlatformState.Ready || collision == null || !collision.gameObject.CompareTag(playerTag) || !IsPlayerLandingFromAbove(collision))
            return;

        ActivatePlatform();
    }


    private bool IsPlayerLandingFromAbove(Collision2D collision)
    {
        Rigidbody2D playerRb = collision.gameObject.GetComponent<Rigidbody2D>();

        if (playerRb == null || playerRb.linearVelocity.y > FallingVelocityThreshold)
            return false;

        float platformTop = tilemapCollider.bounds.max.y;

        for (int index = 0; index < collision.contactCount; index++)
        {
            ContactPoint2D contact = collision.GetContact(index);

            if (contact.point.y >= platformTop - landingTolerance)
                return true;
        }

        if (topContactThreshold > 0f)
        {
            for (int index = 0; index < collision.contactCount; index++)
            {
                ContactPoint2D contact = collision.GetContact(index);

                if (contact.normal.y >= topContactThreshold)
                    return true;
            }
        }

        return false;
    }


    // ACTIVATE

    private void ActivatePlatform()
    {
        if (isActivated || activeRoutine != null || platformState != PlatformState.Ready)
            return;

        isActivated = true;
        PlayActivationSound();
        platformState = PlatformState.Shaking;
        activeRoutine = StartCoroutine(FallSequence());
    }


    // AUDIO

    private void PlayActivationSound()
    {
        if (hasPlayedActivationSound || activationSound == null || audioSource == null)
            return;

        hasPlayedActivationSound = true;
        audioSource.pitch = randomizeSoundPitch ? UnityEngine.Random.Range(minimumPitch, maximumPitch) : 1f;
        audioSource.PlayOneShot(activationSound, activationSoundVolume);
    }


    // FALL SEQUENCE

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

        if (disappearDelay > 0f)
            yield return new WaitForSeconds(disappearDelay);

        // FADE / HIDE
        if (disappearAfterFalling)
            yield return StartCoroutine(FadeOutAndHide());

        // NEVER RESPAWN
        if (respawnMode == PlatformRespawnMode.Never)
        {
            activeRoutine = null;
            yield break;
        }

        // RESPAWN DELAY
        if (respawnDelay > 0f)
            yield return new WaitForSeconds(respawnDelay);

        yield return StartCoroutine(RespawnWithFade());

        activeRoutine = null;
    }


    // SHAKE

    private void ShakePlatform(float elapsed)
    {
        float duration = Mathf.Max(fallDelay, 0.0001f);
        float progress = Mathf.Clamp01(elapsed / duration);
        float strength = Mathf.Lerp(shakeStrength, shakeStrength * 1.5f, progress);
        Vector2 offset = UnityEngine.Random.insideUnitCircle * strength * Mathf.Sin(elapsed * shakeSpeed);
        transform.localPosition = originalLocalPosition + new Vector3(offset.x, offset.y, 0f);
        transform.localRotation = originalLocalRotation;
    }


    // BEGIN FALL

    private void BeginFalling()
    {
        transform.localPosition = originalLocalPosition;
        transform.localRotation = originalLocalRotation;

        rb.simulated = true;
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.gravityScale = fallingGravityScale;
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;

        platformState = PlatformState.Falling;
    }


    // VISUAL OPACITY

    private void SetVisualOpacity(float opacity)
    {
        float normalizedOpacity = Mathf.Clamp01(opacity);

        // MAIN PLATFORM TILEMAP
        if (tilemap != null)
        {
            Color tileColor = originalTilemapColor;
            tileColor.a = originalTilemapColor.a * normalizedOpacity;
            tilemap.color = tileColor;
        }

        if (!fadeAllAttachmentVisuals)
            return;

        // ATTACHMENT SPRITES
        for (int i = 0; i < attachmentSpriteRenderers.Length; i++)
        {
            SpriteRenderer renderer = attachmentSpriteRenderers[i];

            if (renderer == null)
                continue;

            Color color = originalAttachmentSpriteColors[i];
            color.a = originalAttachmentSpriteColors[i].a * normalizedOpacity;
            renderer.color = color;
        }

        // ATTACHMENT TILEMAPS
        for (int i = 0; i < attachmentTilemaps.Length; i++)
        {
            Tilemap childTilemap = attachmentTilemaps[i];

            if (childTilemap == null)
                continue;

            Color color = originalAttachmentTilemapColors[i];
            color.a = originalAttachmentTilemapColors[i].a * normalizedOpacity;
            childTilemap.color = color;
        }

        // ATTACHMENT 2D LIGHTS
        for (int i = 0; i < attachmentLights.Length; i++)
        {
            Light2D light = attachmentLights[i];

            if (light == null)
                continue;
            light.intensity = originalAttachmentLightIntensities[i] * normalizedOpacity;
        }

        // ATTACHMENT CANVAS GROUPS
        for (int i = 0; i < attachmentCanvasGroups.Length; i++)
        {
            CanvasGroup group = attachmentCanvasGroups[i];

            if (group == null)
                continue;

            group.alpha = originalAttachmentCanvasAlphas[i] * normalizedOpacity;
        }

        // GENERIC RENDERERS
        for (int i = 0; i < attachmentGenericRenderers.Length; i++)
        {
            Renderer renderer = attachmentGenericRenderers[i];

            if (renderer == null)
                continue;

            int propertyId = genericRendererColorPropertyIds[i];

            if (propertyId == 0)
                continue;

            Color color = originalGenericRendererColors[i];
            color.a = originalGenericRendererColors[i].a * normalizedOpacity;
            renderer.GetPropertyBlock(genericRendererPropertyBlock);
            genericRendererPropertyBlock.SetColor(propertyId, color);
            renderer.SetPropertyBlock(genericRendererPropertyBlock);
        }
    }


    // ATTACHMENT SHADOWS

    private void SetAttachmentShadowCasting(bool visible)
    {
        for (int i = 0; i < attachmentShadowCasters.Length; i++)
        {
            ShadowCaster2D shadowCaster = attachmentShadowCasters[i];

            if (shadowCaster == null)
                continue;

            shadowCaster.castsShadows = visible && originalShadowCastingStates[i];
        }
    }


    // ATTACHMENT COLLIDERS

    private void SetAttachmentsInteractive(bool interactive)
    {
        if (autoManageAllAttachmentColliders)
        {
            if (!interactive)
            {
                for (int i = 0; i < managedAttachmentColliders.Length; i++)
                {
                    Collider2D childCollider = managedAttachmentColliders[i];

                    if (childCollider != null)
                        childCollider.enabled = false;
                }
            }
            else
            {
                RestoreAttachmentColliderStates();
            }

            return;
        }

        /*  * Legacy/manual mode.  */
        if (attachmentInteractionColliders == null)
            return;

        foreach (Collider2D interactionCollider in attachmentInteractionColliders)
        {
            if (interactionCollider != null)
                interactionCollider.enabled = interactive;
        }
    }


    private void RestoreAttachmentColliderStates()
    {
        if (!autoManageAllAttachmentColliders)
        {
            if (attachmentInteractionColliders != null)
            {
                foreach (Collider2D interactionCollider in attachmentInteractionColliders)
                {
                    if (interactionCollider != null)
                    {
                        interactionCollider.enabled = true;
                    }
                }
            }

            return;
        }

        for (int i = 0; i < managedAttachmentColliders.Length; i++)
        {
            Collider2D childCollider = managedAttachmentColliders[i];

            if (childCollider == null)
                continue;

            bool originalState = i < originalAttachmentColliderStates.Length && originalAttachmentColliderStates[i];

            childCollider.enabled = originalState;
        }
    }


    // FADE

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
            float curveValue = fadeCurve != null && fadeCurve.length > 0 ? fadeCurve.Evaluate(normalizedTime) : normalizedTime;
            float opacity = Mathf.Lerp(startOpacity, targetOpacity, curveValue);
            SetVisualOpacity(opacity);
            elapsed += Time.deltaTime;
            yield return null;
        }

        SetVisualOpacity(targetOpacity);
    }


    // FADE OUT / HIDE
    private IEnumerator FadeOutAndHide()
    {
        if (platformState == PlatformState.Hidden)
            yield break;

        platformState = PlatformState.FadingOut;

        tilemapCollider.enabled = false;

        /*  
        * Disable traps, lever interaction, dangerous-light triggers,  
        * and all other attachment colliders as soon as the platform  
        * starts disappearing.
          */
        SetAttachmentsInteractive(false);

        /*  
        * ShadowCaster2D has no opacity control, so disable attached  
        * shadow casting while the visual group fades out. 
         */
        SetAttachmentShadowCasting(false);

        yield return FadeOpacity(1f, 0f, fadeOutDuration);
        tilemapRenderer.enabled = false;
        rb.simulated = false;
        platformState = PlatformState.Hidden;

        if (attachmentsRoot != null)
            attachmentsRoot.SetActive(false);
    }


    // RESPAWN
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

        tilemapRenderer.enabled = true;
        tilemapCollider.enabled = false;

        if (attachmentsRoot != null)
            attachmentsRoot.SetActive(true);

        /*  
        * The root must already be active before setting 0 opacity,  
        * otherwise child visuals can flash for one frame. 
         */
        SetVisualOpacity(0f);
        SetAttachmentShadowCasting(false);
        SetAttachmentsInteractive(false);

        if (attachedLever != null)
            attachedLever.RestoreVisualState();

        yield return FadeOpacity(0f, 1f, fadeInDuration);

        SetVisualOpacity(1f);

        /* 
         * Bring shadows and interactions back only when the group is  
         * fully visible again.  
         */
        SetAttachmentShadowCasting(true);

        tilemapCollider.enabled = true;
        rb.simulated = true;
        hasPlayedActivationSound = false;
        platformState = PlatformState.Ready;
        SetAttachmentsInteractive(true);
        isActivated = false;
        activeRoutine = null;
    }


    // DISABLE

    private void OnDisable()
    {
        if (activeRoutine != null)
        {
            StopCoroutine(activeRoutine);

            activeRoutine = null;
        }

        if (audioSource != null)
            audioSource.pitch = 1f;

        if (tilemap != null)
            SetVisualOpacity(1f);

        SetAttachmentShadowCasting(true);
    }


    // VALIDATION

    private void OnValidate()
    {
        if (string.IsNullOrWhiteSpace(playerTag))
            playerTag = "Player";

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
            fadeCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        if (!Application.isPlaying)
        {
            tilemap = GetComponent<Tilemap>();
            tilemapRenderer = GetComponent<TilemapRenderer>();
            tilemapCollider = GetComponent<TilemapCollider2D>();
            compositeCollider = GetComponent<CompositeCollider2D>();
            rb = GetComponent<Rigidbody2D>();

            if (tilemapCollider != null)
                tilemapCollider.usedByComposite = true;
        }
    }
}
