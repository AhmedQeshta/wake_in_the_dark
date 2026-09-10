using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(PlayerDeath))]
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Animator))]
public class PlayerLightExposure : MonoBehaviour
{
    // LIGHT EXPOSURE

    [Header("Light Exposure")]

    [Tooltip("How long this character may stay inside dangerous light before death starts.")]
    [SerializeField, Min(0.1f)] private float exposureDuration = 3f;


    [Tooltip("If ON, leaving every dangerous light resets exposure back to 0.")]
    [SerializeField] private bool resetExposureOnExit = true;


    // STONE TRANSFORMATION

    [Header("Stone Transformation")]

    [Tooltip("ON = play the TurnToStone animation before PlayerDeath. OFF = dangerous-light exposure kills the character directly after the timer.")]
    [SerializeField] private bool useStoneTransformationAnimation = true;


    [Tooltip("Animator Trigger used by the stone animation.")]
    [SerializeField] private string stoneTriggerName = "TurnToStone";


    [Tooltip("Optional Animator state to force after a normal respawn. Leave empty if not needed.")]
    [SerializeField] private string idleStateName = "idle";


    [SerializeField] private bool freezePhysicsDuringTransformation = true;


    [Tooltip("Safety fallback. Set slightly longer than the stone animation. If the Animation Event is missing, death still starts.")]
    [SerializeField, Min(0f)] private float animationEventFallbackDelay = 1.25f;


    // REFERENCES

    [Header("References")]

    [Tooltip("Player 1 movement. Optional for Wife.")]
    [SerializeField] private PlayerMovement playerMovement;


    [Tooltip("Wife / Player 2 follower. Optional for Player 1.")]
    [SerializeField] private CompanionFollower2D companionFollower;


    [SerializeField] private PlayerDeath playerDeath;


    [SerializeField] private Rigidbody2D playerRigidbody;


    [SerializeField] private Animator playerAnimator;


    // ACTIVE LIGHTS

    private readonly HashSet<DangerousLightZone> activeLightZones = new HashSet<DangerousLightZone>();


    // STATE

    private float exposureTime;

    private bool transformingToStone;

    private bool waitingForRespawn;

    private Coroutine fallbackDeathRoutine;

    private RigidbodyType2D previousBodyType;

    private bool physicsFrozenByExposure;

    private int stoneTriggerHash;

    private bool companionWasEnabledBeforeTransformation;


    // PUBLIC

    public float ExposureTime => exposureTime;


    public float Exposure01 => exposureDuration > 0f ? Mathf.Clamp01(exposureTime / exposureDuration) : 0f;


    public bool IsExposed => activeLightZones.Count > 0;


    public bool IsTransformingToStone => transformingToStone;


    // AWAKE

    private void Awake()
    {
        ResolveReferences();
        RebuildAnimatorHash();
    }


    // UPDATE

    private void Update()
    {
        // WAIT FOR PLAYERDEATH TO FINISH RESPAWN
        if (waitingForRespawn)
        {
            if (playerDeath != null && !playerDeath.IsDead)
                ResetAfterRespawn();
            return;
        }


        // EXTERNAL DEATH
        if (playerDeath != null && playerDeath.IsDead)
        {
            CancelExposureForExternalDeath();
            return;
        }


        // TRANSFORMATION ALREADY RUNNING
        if (transformingToStone)
            return;


        // NOT IN DANGEROUS LIGHT
        if (activeLightZones.Count == 0)
        {
            if (resetExposureOnExit)
                exposureTime = 0f;
            return;
        }


        // COUNT EXPOSURE
        exposureTime += Time.deltaTime;

        if (exposureTime >= exposureDuration)
        {
            exposureTime = exposureDuration;
            BeginDangerousLightDeath();
        }
    }


    // ENTER LIGHT

    public void EnterDangerousLight(DangerousLightZone lightZone)
    {
        if (lightZone == null || transformingToStone || waitingForRespawn || (playerDeath != null && playerDeath.IsDead))
            return;

        activeLightZones.Add(lightZone);
    }


    // EXIT LIGHT

    public void ExitDangerousLight(DangerousLightZone lightZone)
    {
        if (lightZone == null)
            return;

        activeLightZones.Remove(lightZone);

        /*  * Once death/stone transformation starts,  * leaving the collider does not cancel it.  */
        if (transformingToStone || waitingForRespawn)
            return;

        if (activeLightZones.Count == 0 && resetExposureOnExit)
            exposureTime = 0f;
    }


    // BEGIN LIGHT DEATH

    private void BeginDangerousLightDeath()
    {
        if (transformingToStone || waitingForRespawn)
            return;

        ResolveReferences();

        if (playerDeath == null)
            return;

        activeLightZones.Clear();


        // NO STONE ANIMATION
        if (!useStoneTransformationAnimation)
        {
            playerDeath.KillPlayer();
            waitingForRespawn = playerDeath.IsDead;
            exposureTime = 0f;
            return;
        }


        // VALIDATE ANIMATOR / TRIGGER
        if (playerAnimator == null || !HasAnimatorParameter(stoneTriggerName, AnimatorControllerParameterType.Trigger))
        {
            playerDeath.KillPlayer();
            waitingForRespawn = playerDeath.IsDead;
            exposureTime = 0f;
            return;
        }


        // BEGIN TRANSFORMATION
        transformingToStone = true;

        DisableCharacterControlForTransformation();
        FreezeCharacterPhysics();

        playerAnimator.ResetTrigger(stoneTriggerHash);

        playerAnimator.SetTrigger(stoneTriggerHash);

        if (fallbackDeathRoutine != null)
            StopCoroutine(fallbackDeathRoutine);

        if (animationEventFallbackDelay > 0f)
            fallbackDeathRoutine = StartCoroutine(AnimationEventFallbackRoutine());

    }


    // FINISH STONE TRANSFORMATION

    /*
     * Add this Animation Event to the LAST frame
     * of the stone animation.
     */
    public void FinishStoneTransformation()
    {
        if (!transformingToStone || waitingForRespawn) return;

        if (fallbackDeathRoutine != null)
        {
            StopCoroutine(fallbackDeathRoutine);
            fallbackDeathRoutine = null;
        }

        RestoreCharacterPhysics();

        if (playerDeath == null)
        {
            transformingToStone = false;

            RestoreCharacterControlAfterCancelledTransformation();
            return;
        }

        /*  * The shared PlayerDeath system now decides:  *  * lives remain -> respawn only this character  * lives == 0   -> reload whole level  */
        playerDeath.KillPlayer();

        waitingForRespawn = playerDeath.IsDead;

        transformingToStone = false;
    }


    // FALLBACK

    private IEnumerator AnimationEventFallbackRoutine()
    {
        yield return new WaitForSecondsRealtime(animationEventFallbackDelay);

        fallbackDeathRoutine = null;

        if (transformingToStone && !waitingForRespawn)
            FinishStoneTransformation();
    }


    // DISABLE CONTROL DURING STONE

    private void DisableCharacterControlForTransformation()
    { /*  * Player 1:  * use the existing control API instead of disabling the  * component, because Timeline and other systems also use it.  */
        if (playerDeath != null && playerDeath.CharacterRole == PartyCharacterRole.Player1)
            playerMovement.DisableControls();

        /*  * Wife:  * disable only CompanionFollower2D temporarily.  *  * Wife PlayerMovement stays disabled exactly as configured.  */
        if (companionFollower != null)
        {
            companionWasEnabledBeforeTransformation = companionFollower.enabled;
            companionFollower.enabled = false;
        }
    }


    // RESTORE CONTROL IF TRANSFORMATION IS CANCELLED

    private void RestoreCharacterControlAfterCancelledTransformation()
    {
        if (playerDeath != null && playerDeath.CharacterRole == PartyCharacterRole.Player1)
            playerMovement.EnableControls();

        if (companionFollower != null)
            companionFollower.enabled = companionWasEnabledBeforeTransformation;
    }


    // PHYSICS

    private void FreezeCharacterPhysics()
    {
        if (!freezePhysicsDuringTransformation || playerRigidbody == null || physicsFrozenByExposure)
            return;

        previousBodyType = playerRigidbody.bodyType;
        playerRigidbody.linearVelocity = Vector2.zero;
        playerRigidbody.angularVelocity = 0f;
        playerRigidbody.bodyType = RigidbodyType2D.Static;
        physicsFrozenByExposure = true;
    }


    private void RestoreCharacterPhysics()
    {
        if (!physicsFrozenByExposure || playerRigidbody == null)
            return;

        playerRigidbody.bodyType = previousBodyType;
        playerRigidbody.linearVelocity = Vector2.zero;
        playerRigidbody.angularVelocity = 0f;
        physicsFrozenByExposure = false;
    }


    // EXTERNAL DEATH

    private void CancelExposureForExternalDeath()
    {
        waitingForRespawn = true;

        activeLightZones.Clear();

        exposureTime = 0f;

        if (fallbackDeathRoutine != null)
        {
            StopCoroutine(fallbackDeathRoutine);
            fallbackDeathRoutine = null;
        }

        RestoreCharacterPhysics();

        transformingToStone = false;
    }


    // RESET AFTER RESPAWN

    private void ResetAfterRespawn()
    {
        waitingForRespawn = false;

        transformingToStone = false;

        exposureTime = 0f;

        activeLightZones.Clear();

        if (fallbackDeathRoutine != null)
        {
            StopCoroutine(fallbackDeathRoutine);
            fallbackDeathRoutine = null;
        }

        RestoreCharacterPhysics();

        if (playerAnimator != null)
        {
            playerAnimator.ResetTrigger(stoneTriggerHash);

            if (!string.IsNullOrWhiteSpace(idleStateName))
                playerAnimator.Play(idleStateName, 0, 0f);
        }

        if (playerDeath != null && playerDeath.CharacterRole == PartyCharacterRole.Player1 && playerMovement != null)
            playerMovement.EnableControls();

    }


    // REFERENCES

    private void ResolveReferences()
    {
        if (playerMovement == null)
            playerMovement = GetComponent<PlayerMovement>();

        if (companionFollower == null)
            companionFollower = GetComponent<CompanionFollower2D>();

        if (playerDeath == null)
            playerDeath = GetComponent<PlayerDeath>();

        if (playerRigidbody == null)
            playerRigidbody = GetComponent<Rigidbody2D>();

        if (playerAnimator == null)
            playerAnimator = GetComponent<Animator>();
    }


    // ANIMATOR

    private bool HasAnimatorParameter(string parameterName, AnimatorControllerParameterType type)
    {
        if (playerAnimator == null || string.IsNullOrWhiteSpace(parameterName))
            return false;

        AnimatorControllerParameter[] parameters = playerAnimator.parameters;

        foreach (AnimatorControllerParameter parameter in parameters)
            if (parameter.type == type && parameter.name == parameterName)
                return true;

        return false;
    }


    private void RebuildAnimatorHash()
    {
        if (string.IsNullOrWhiteSpace(stoneTriggerName))
            stoneTriggerName = "TurnToStone";

        stoneTriggerHash = Animator.StringToHash(stoneTriggerName);
    }


    // VALIDATE

    private void OnValidate()
    {
        exposureDuration = Mathf.Max(0.1f, exposureDuration);

        animationEventFallbackDelay = Mathf.Max(0f, animationEventFallbackDelay);

        if (string.IsNullOrWhiteSpace(stoneTriggerName))
            stoneTriggerName = "TurnToStone";

        RebuildAnimatorHash();
    }
}
