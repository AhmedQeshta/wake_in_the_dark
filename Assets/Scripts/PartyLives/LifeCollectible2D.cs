using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class LifeCollectible2D : MonoBehaviour
{
    // TARGET CHARACTER

    [Header("Life Target")]
    [Tooltip("The character who RECEIVES the life. Either Player 1 or Wife may physically collect this item.")]
    [SerializeField] private PartyCharacterRole lifeTarget = PartyCharacterRole.Player1;

    [Tooltip("How many lives this pickup tries to add. The result is always clamped to PartyLivesManager.MaxLives.")]
    [SerializeField, Min(1)] private int lifeAmount = 1;

    // TARGET SPRITE
    [Header("Target Sprite")]
    [Tooltip("SpriteRenderer used by this collectible. Leave empty to auto-find one.")]
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Tooltip("Optional first/default sprite for the Player 1 collectible.")]
    [SerializeField] private Sprite player1Sprite;

    [Tooltip("Optional first/default sprite for the Wife collectible.")]
    [SerializeField] private Sprite wifeSprite;

    [Tooltip("If ON, the first/default sprite is updated from Life Target. The Animator can still replace SpriteRenderer.sprite every animation frame.")]
    [SerializeField] private bool autoApplyTargetSprite = true;


    // ANIMATION
    [Header("Animation")]

    [Tooltip("Animator on this collectible. Player and Wife may each use a different Animator Controller.")]
    [SerializeField] private Animator animator;

    [Tooltip("Trigger used for Idle -> Pickup. Use the same parameter name in both Player and Wife controllers.")]
    [SerializeField] private string pickupTriggerName = "Pickup";


    [Tooltip("Safety fallback if FinishPickupAnimation is not added as an Animation Event. Set this slightly longer than the longest pickup animation.")]
    [SerializeField, Min(0.1f)] private float pickupAnimationFallbackDelay = 2f;


    // PICKUP SOUND
    [Header("Pickup Sound")]

    [SerializeField] private AudioClip pickupSound;
    [SerializeField, Range(0f, 1f)] private float pickupVolume = 1f;

    [SerializeField] private bool randomizePitch = false;
    [SerializeField, Range(0.5f, 1.5f)] private float minimumPitch = 0.95f;
    [SerializeField, Range(0.5f, 1.5f)] private float maximumPitch = 1.05f;


    // OPTIONS
    [Header("Options")]

    [Tooltip("If the target already has Max Lives, the collectible stays available and keeps playing Idle.")]
    [SerializeField] private bool keepCollectibleWhenTargetIsFull = true;


    [Tooltip("After Pickup animation finishes: ON = destroy GameObject, OFF = keep GameObject disabled/hidden.")]
    [SerializeField] private bool destroyAfterSuccessfulPickup = true;


    [SerializeField] private bool debugLogs = false;


    // STATE
    private Collider2D[] pickupColliders;
    private bool collected;
    private bool pickupAnimationStarted;
    private int pickupTriggerHash;
    private Coroutine pickupFallbackRoutine;


    // PUBLIC
    public PartyCharacterRole LifeTarget => lifeTarget;

    public bool IsCollected => collected;


    // AWAKE
    private void Awake()
    {
        ResolveReferences();
        ConfigureColliders();
        RebuildAnimatorHash();
        ApplyTargetSprite();
        collected = false;
        pickupAnimationStarted = false;
    }


    // TRIGGER

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (collected || pickupAnimationStarted || other == null)
            return;

        PlayerDeath collector = FindPartyCharacter(other);

        if (collector == null)
            return;

        TryCollect(collector);
    }


    // COLLECT

    private void TryCollect(PlayerDeath collector)
    {
        PartyLivesManager manager = PartyLivesManager.Instance;

        if (manager == null)
            return;

        bool lifeAdded = manager.TryAddLife(lifeTarget, lifeAmount);

        if (!lifeAdded)
        {
            if (manager.IsAtMaxLives(lifeTarget) && !keepCollectibleWhenTargetIsFull)
                BeginPickupAnimation();

            return;
        }

        collected = true;


        PlayPickupSoundDetached();
        SetPickupCollidersEnabled(false);
        BeginPickupAnimation();
    }


    // PICKUP ANIMATION
    private void BeginPickupAnimation()
    {
        if (pickupAnimationStarted)
            return;

        pickupAnimationStarted = true;

        SetPickupCollidersEnabled(false);

        if (animator == null)
        {
            FinishPickupAnimation();
            return;
        }

        if (!HasAnimatorTrigger(pickupTriggerName))
        {
            FinishPickupAnimation();
            return;
        }

        animator.ResetTrigger(pickupTriggerHash);
        animator.SetTrigger(pickupTriggerHash);

        if (pickupFallbackRoutine != null)
            StopCoroutine(pickupFallbackRoutine);

        pickupFallbackRoutine = StartCoroutine(PickupAnimationFallbackRoutine());
    }


    private IEnumerator PickupAnimationFallbackRoutine()
    {
        yield return new WaitForSecondsRealtime(pickupAnimationFallbackDelay);

        pickupFallbackRoutine = null;

        if (pickupAnimationStarted)
        {
            FinishPickupAnimation();
        }
    }


    // ANIMATION EVENT
    public void FinishPickupAnimation()
    {
        if (!pickupAnimationStarted)
            return;

        pickupAnimationStarted = false;

        if (pickupFallbackRoutine != null)
        {
            StopCoroutine(pickupFallbackRoutine);

            pickupFallbackRoutine = null;
        }

        if (destroyAfterSuccessfulPickup)
        {
            Destroy(gameObject);
            return;
        }

        DisableCollectibleVisualAndInteraction();
    }


    // FIND PARTY CHARACTER
    private PlayerDeath FindPartyCharacter(Collider2D other)
    {
        if (other == null)
            return null;

        PlayerDeath death = other.GetComponentInParent<PlayerDeath>();

        if (death != null)
            return death;

        Rigidbody2D attachedBody = other.attachedRigidbody;

        if (attachedBody == null)
            return null;

        death = attachedBody.GetComponent<PlayerDeath>();

        if (death != null)
            return death;

        return attachedBody.GetComponentInParent<PlayerDeath>();
    }


    // SOUND

    private void PlayPickupSoundDetached()
    {
        if (pickupSound == null) return;

        float pitch = randomizePitch ? Random.Range(minimumPitch, maximumPitch) : 1f;

        GameObject soundObject = new GameObject("LifeCollectible_PickupSound");

        soundObject.transform.position = transform.position;

        AudioSource source = soundObject.AddComponent<AudioSource>();

        source.playOnAwake = false;
        source.loop = false;
        source.spatialBlend = 0f;
        source.volume = pickupVolume;
        source.pitch = pitch;
        source.clip = pickupSound;

        source.Play();

        float soundDuration = pickupSound.length / Mathf.Max(Mathf.Abs(pitch), 0.01f);

        Destroy(soundObject, soundDuration + 0.1f);
    }


    // COLLIDERS

    private void ConfigureColliders()
    {
        pickupColliders = GetComponentsInChildren<Collider2D>(true);

        foreach (Collider2D pickupCollider in pickupColliders)
            if (pickupCollider != null)
                pickupCollider.isTrigger = true;

    }


    private void SetPickupCollidersEnabled(bool enabledState)
    {
        if (pickupColliders == null)
            return;

        foreach (Collider2D pickupCollider in pickupColliders)
            if (pickupCollider != null)
                pickupCollider.enabled = enabledState;
    }


    // DISABLE AFTER PICKUP

    private void DisableCollectibleVisualAndInteraction()
    {
        SetPickupCollidersEnabled(false);

        if (animator != null)
            animator.enabled = false;

        SpriteRenderer[] renderers = GetComponentsInChildren<SpriteRenderer>(true);

        foreach (SpriteRenderer renderer in renderers)
            if (renderer != null)
                renderer.enabled = false;
    }


    // TARGET SPRITE
    private void ApplyTargetSprite()
    {
        if (!autoApplyTargetSprite || spriteRenderer == null)
            return;

        Sprite targetSprite = lifeTarget == PartyCharacterRole.Wife ? wifeSprite : player1Sprite;

        if (targetSprite != null)
            spriteRenderer.sprite = targetSprite;
    }


    // PUBLIC TARGET HELPER

    public void SetLifeTarget(PartyCharacterRole newTarget)
    {
        lifeTarget = newTarget;

        ApplyTargetSprite();
    }


    // ANIMATOR

    private bool HasAnimatorTrigger(string parameterName)
    {
        if (animator == null || string.IsNullOrWhiteSpace(parameterName))
            return false;

        AnimatorControllerParameter[] parameters = animator.parameters;

        foreach (AnimatorControllerParameter parameter in parameters)
            if (parameter.type == AnimatorControllerParameterType.Trigger && parameter.name == parameterName)
                return true;


        return false;
    }


    private void RebuildAnimatorHash()
    {
        if (string.IsNullOrWhiteSpace(pickupTriggerName))
            pickupTriggerName = "Pickup";

        pickupTriggerHash = Animator.StringToHash(pickupTriggerName);
    }


    // REFERENCES

    private void ResolveReferences()
    {
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();

            if (spriteRenderer == null)
                spriteRenderer = GetComponentInChildren<SpriteRenderer>(true);
        }

        if (animator == null)
        {
            animator = GetComponent<Animator>();

            if (animator == null)
                animator = GetComponentInChildren<Animator>(true);
        }
    }


    // RESET

    private void Reset()
    {
        ResolveReferences();
        ConfigureColliders();
        RebuildAnimatorHash();
        ApplyTargetSprite();
    }


    // VALIDATE

    private void OnValidate()
    {
        lifeAmount = Mathf.Max(1, lifeAmount);
        pickupAnimationFallbackDelay = Mathf.Max(0.1f, pickupAnimationFallbackDelay);
        pickupVolume = Mathf.Clamp01(pickupVolume);
        minimumPitch = Mathf.Clamp(minimumPitch, 0.5f, 1.5f);
        maximumPitch = Mathf.Clamp(maximumPitch, 0.5f, 1.5f);

        if (minimumPitch > maximumPitch)
        {
            float temp = minimumPitch;
            minimumPitch = maximumPitch;
            maximumPitch = temp;
        }

        ResolveReferences();
        ConfigureColliders();
        RebuildAnimatorHash();
        ApplyTargetSprite();
    }


    // DESTROY

    private void OnDestroy()
    {
        if (pickupFallbackRoutine != null)
        {
            StopCoroutine(pickupFallbackRoutine);
            pickupFallbackRoutine = null;
        }
    }
}
