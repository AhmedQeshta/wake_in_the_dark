using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(PlayerMovement))]
[RequireComponent(typeof(PlayerDeath))]
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Animator))]
public class PlayerLightExposure : MonoBehaviour
{
    // ==================================================
    // STONE TRANSFORMATION
    // ==================================================

    [Header("Stone Transformation")]

    [Tooltip("Animator Trigger used by Any State -> Transforming_To_Stone.")]
    [SerializeField] private string stoneTriggerName = "TurnToStone";


    [Tooltip("Freeze the Rigidbody while the stone animation is playing.")]
    [SerializeField] private bool freezePhysicsDuringTransformation = true;


    [Tooltip("Safety fallback. If the Animation Event is missing, death still starts after this delay. Set it slightly longer than the stone animation.")]
    [SerializeField, Min(0f)] private float animationEventFallbackDelay = 1.25f;


    // ==================================================
    // REFERENCES
    // ==================================================

    [Header("References")]

    [SerializeField] private PlayerMovement playerMovement;


    [SerializeField] private PlayerDeath playerDeath;


    [SerializeField] private Rigidbody2D playerRb;


    [SerializeField] private Animator playerAnimator;


    // ==================================================
    // STATE
    // ==================================================

    private readonly List<ActiveLightExposure> activeLights = new List<ActiveLightExposure>();


    private bool transformingToStone;

    private bool waitingForRespawn;

    private Coroutine fallbackDeathRoutine;


    private RigidbodyType2D previousBodyType;

    private bool physicsFrozenByExposure;


    private int stoneTriggerHash;


    // ==================================================
    // PUBLIC VALUES
    // ==================================================

    public bool IsExposed => activeLights.Count > 0;


    public bool IsTransformingToStone => transformingToStone;


    public float HighestExposure01
    {
        get
        {
            float highest = 0f;


            foreach (ActiveLightExposure activeLight in activeLights)
            {
                if (activeLight == null || !activeLight.IsValid)
                    continue;

                if (activeLight.NormalizedExposure > highest)
                {
                    highest = activeLight.NormalizedExposure;
                }
            }


            return highest;
        }
    }


    // ==================================================
    // AWAKE
    // ==================================================

    private void Awake()
    {
        ResolveReferences();

        RebuildAnimatorHash();
    }


    // ==================================================
    // UPDATE
    // ==================================================

    private void Update()
    {
        // ----------------------------------------------
        // WAIT FOR EXISTING PLAYERDEATH RESPAWN
        // ----------------------------------------------

        if (waitingForRespawn)
        {
            if (playerDeath != null && !playerDeath.IsDead)
                ResetAfterRespawn();

            return;
        }


        // ----------------------------------------------
        // PLAYER DIED FROM ANOTHER HAZARD
        // ----------------------------------------------

        if (playerDeath != null && playerDeath.IsDead)
        {
            CancelExposureForExternalDeath();

            return;
        }


        // ----------------------------------------------
        // STONE ANIMATION ALREADY STARTED
        // ----------------------------------------------

        if (transformingToStone)
            return;


        // ----------------------------------------------
        // NOTHING EXPOSING PLAYER
        // ----------------------------------------------

        if (activeLights.Count == 0)
            return;


        DangerousLightZone lightThatKilledPlayer = UpdateActiveLightExposure();


        if (lightThatKilledPlayer != null)
        {
            BeginStoneTransformation(lightThatKilledPlayer);
        }
    }


    // ==================================================
    // UPDATE ACTIVE LIGHTS
    // ==================================================

    private DangerousLightZone UpdateActiveLightExposure()
    {
        for (int i = activeLights.Count - 1; i >= 0; i--)
        {
            ActiveLightExposure activeLight = activeLights[i];


            if (activeLight == null || !activeLight.IsValid)
            {
                activeLights.RemoveAt(i);
                continue;
            }


            activeLight.AddExposure(Time.deltaTime);


            if (activeLight.IsComplete)
            {
                return activeLight.Zone;
            }
        }


        return null;
    }


    // ==================================================
    // ENTER DANGEROUS LIGHT
    // ==================================================

    public void EnterDangerousLight(DangerousLightZone lightZone)
    {
        if ((lightZone == null)
        || (playerDeath != null && playerDeath.IsDead)
        || (transformingToStone || waitingForRespawn)
        || (FindActiveLight(lightZone) != null))
            return;

        activeLights.Add(new ActiveLightExposure(lightZone));
    }


    // ==================================================
    // EXIT DANGEROUS LIGHT
    // ==================================================

    public void ExitDangerousLight(DangerousLightZone lightZone)
    {
        if (lightZone == null)
            return;


        /*
         * Leaving this light removes its runtime entry,
         * so its exposure resets to zero.
         */
        for (int i = activeLights.Count - 1; i >= 0; i--)
        {
            ActiveLightExposure activeLight = activeLights[i];


            if (activeLight == null || activeLight.Zone == lightZone)
                activeLights.RemoveAt(i);
        }
    }


    // ==================================================
    // FIND ACTIVE LIGHT
    // ==================================================

    private ActiveLightExposure FindActiveLight(DangerousLightZone lightZone)
    {
        foreach (ActiveLightExposure activeLight in activeLights)
            if (activeLight != null && activeLight.Zone == lightZone)
                return activeLight;

        return null;
    }


    // ==================================================
    // BEGIN STONE TRANSFORMATION
    // ==================================================

    private void BeginStoneTransformation(DangerousLightZone sourceLight)
    {
        if (transformingToStone || waitingForRespawn)
            return;

        ResolveReferences();


        if (playerAnimator == null)
            return;


        if (playerDeath == null)
        {
            return;
        }


        transformingToStone = true;


        activeLights.Clear();


        // ----------------------------------------------
        // DISABLE PLAYER CONTROLS
        // ----------------------------------------------

        if (playerMovement != null)
            playerMovement.DisableControls();


        // ----------------------------------------------
        // FREEZE PHYSICS
        // ----------------------------------------------

        FreezePlayerPhysics();


        // ----------------------------------------------
        // PLAY STONE ANIMATION
        // ----------------------------------------------

        playerAnimator.ResetTrigger(stoneTriggerHash);


        playerAnimator.SetTrigger(stoneTriggerHash);


        // ----------------------------------------------
        // FALLBACK
        // ----------------------------------------------

        if (fallbackDeathRoutine != null)
            StopCoroutine(fallbackDeathRoutine);


        if (animationEventFallbackDelay > 0f)
            fallbackDeathRoutine = StartCoroutine(AnimationEventFallbackRoutine());



    }


    // ==================================================
    // ANIMATION EVENT
    // ==================================================

    /*
     * Add this Animation Event to the LAST frame of:
     *
     * Transforming_To_Stone
     */
    public void FinishStoneTransformation()
    {
        if (!transformingToStone || waitingForRespawn)
            return;

        if (fallbackDeathRoutine != null)
        {
            StopCoroutine(fallbackDeathRoutine);

            fallbackDeathRoutine = null;
        }


        RestorePlayerPhysics();


        if (playerDeath == null)
        {
            transformingToStone = false;

            if (playerMovement != null)
                playerMovement.EnableControls();

            return;
        }


        /*
         * Existing PlayerDeath starts only after
         * the stone transformation has finished.
         */
        playerDeath.KillPlayer();


        waitingForRespawn = playerDeath.IsDead;


        transformingToStone = false;
    }


    // ==================================================
    // FALLBACK
    // ==================================================

    private IEnumerator AnimationEventFallbackRoutine()
    {
        yield return new WaitForSeconds(animationEventFallbackDelay);

        fallbackDeathRoutine = null;

        if (transformingToStone && !waitingForRespawn)
            FinishStoneTransformation();
    }


    // ==================================================
    // PHYSICS
    // ==================================================

    private void FreezePlayerPhysics()
    {
        if (!freezePhysicsDuringTransformation || playerRb == null || physicsFrozenByExposure)
            return;


        previousBodyType = playerRb.bodyType;


        playerRb.linearVelocity = Vector2.zero;


        playerRb.angularVelocity = 0f;


        playerRb.bodyType = RigidbodyType2D.Static;


        physicsFrozenByExposure = true;
    }


    private void RestorePlayerPhysics()
    {
        if (!physicsFrozenByExposure || playerRb == null)
            return;


        playerRb.bodyType = previousBodyType;


        playerRb.linearVelocity = Vector2.zero;


        playerRb.angularVelocity = 0f;


        physicsFrozenByExposure = false;
    }


    // ==================================================
    // OTHER DEATH TYPES
    // ==================================================

    private void CancelExposureForExternalDeath()
    {
        waitingForRespawn = true;


        activeLights.Clear();


        if (fallbackDeathRoutine != null)
        {
            StopCoroutine(fallbackDeathRoutine);


            fallbackDeathRoutine = null;
        }


        RestorePlayerPhysics();


        transformingToStone = false;
    }


    // ==================================================
    // RESPAWN RESET
    // ==================================================

    private void ResetAfterRespawn()
    {
        waitingForRespawn = false;


        transformingToStone = false;


        activeLights.Clear();


        if (fallbackDeathRoutine != null)
        {
            StopCoroutine(fallbackDeathRoutine);

            fallbackDeathRoutine = null;
        }


        RestorePlayerPhysics();


        if (playerAnimator != null)
        {
            playerAnimator.ResetTrigger(stoneTriggerHash);


            playerAnimator.Play("idle", 0, 0f);
        }


        if (playerMovement != null)
        {
            playerMovement.EnableControls();
        }
    }


    // ==================================================
    // REFERENCES
    // ==================================================

    private void ResolveReferences()
    {
        if (playerMovement == null)
            playerMovement = GetComponent<PlayerMovement>();


        if (playerDeath == null)
            playerDeath = GetComponent<PlayerDeath>();


        if (playerRb == null)
            playerRb = GetComponent<Rigidbody2D>();



        if (playerAnimator == null)
            playerAnimator = GetComponent<Animator>();
    }


    // ==================================================
    // ANIMATOR HASH
    // ==================================================

    private void RebuildAnimatorHash()
    {
        if (string.IsNullOrWhiteSpace(stoneTriggerName))
            stoneTriggerName = "TurnToStone";


        stoneTriggerHash = Animator.StringToHash(stoneTriggerName);
    }


    // ==================================================
    // VALIDATE
    // ==================================================

    private void OnValidate()
    {
        animationEventFallbackDelay = Mathf.Max(0f, animationEventFallbackDelay);

        if (string.IsNullOrWhiteSpace(stoneTriggerName))
            stoneTriggerName = "TurnToStone";

        RebuildAnimatorHash();
    }
}
