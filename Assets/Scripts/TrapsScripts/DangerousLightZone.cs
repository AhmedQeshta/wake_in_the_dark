using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class DangerousLightZone : MonoBehaviour
{
    // SETTINGS
    [Header("Settings")]

    [Tooltip("Automatically keep this Collider2D configured as a Trigger.")]
    [SerializeField] private bool forceIsTrigger = true;


    [Tooltip("How long a character can stay inside THIS dangerous light before death.")]
    [SerializeField, Min(0.1f)] private float exposureDuration = 3f;


    public float ExposureDuration => exposureDuration;

    // CHARACTER FILTER
    [Header("Character Filter")]
    [Tooltip("Dangerous light affects Player 1.")]
    [SerializeField] private bool affectPlayer1 = true;

    [Tooltip("Dangerous light affects Wife / Player 2.")]
    [SerializeField] private bool affectWife = true;


    // STATE
    /*
     * A character may have several Collider2D components.
     *
     * Count each collider so exposure only ends when the
     * LAST collider for that character leaves this light.
     */
    private readonly Dictionary<PlayerLightExposure, int> exposureColliderCounts = new Dictionary<PlayerLightExposure, int>();

    /*
     * Only used to avoid repeating the same setup warning
     * every physics frame.
     */
    private readonly HashSet<PlayerDeath> warnedMissingExposure = new HashSet<PlayerDeath>();

    private Collider2D zoneCollider;


    // AWAKE

    private void Awake()
    {
        zoneCollider = GetComponent<Collider2D>();
        ConfigureCollider();
    }


    // ENTER

    private void OnTriggerEnter2D(Collider2D other)
    {
        PlayerLightExposure exposure = FindExposure(other);

        if (exposure == null)
        {
            WarnIfCharacterIsMissingExposure(other);
            return;
        }

        PlayerDeath deathTarget = exposure.GetComponent<PlayerDeath>();

        if (deathTarget != null && !CanAffectCharacter(deathTarget))


            if (exposureColliderCounts.TryGetValue(exposure, out int currentCount)) { exposureColliderCounts[exposure] = currentCount + 1; }
            else
            {
                exposureColliderCounts.Add(exposure, 1);
                exposure.EnterDangerousLight(this);
            }
    }


    // EXIT

    private void OnTriggerExit2D(Collider2D other)
    {
        PlayerLightExposure exposure = FindExposure(other);

        if (exposure == null || !exposureColliderCounts.TryGetValue(exposure, out int currentCount)) return;

        currentCount--;

        if (currentCount <= 0)
        {
            exposureColliderCounts.Remove(exposure);
            exposure.ExitDangerousLight(this);
        }
        else
        {
            exposureColliderCounts[exposure] = currentCount;
        }
    }


    // FIND EXPOSURE COMPONENT

    private PlayerLightExposure FindExposure(Collider2D other)
    {
        if (other == null)
            return null;

        PlayerLightExposure exposure = other.GetComponentInParent<PlayerLightExposure>();

        if (exposure != null)
            return exposure;

        if (other.attachedRigidbody != null)
        {
            exposure = other.attachedRigidbody.GetComponent<PlayerLightExposure>();

            if (exposure != null)
                return exposure;

            exposure = other.attachedRigidbody.GetComponentInParent<PlayerLightExposure>();
        }

        return exposure;
    }


    // CHARACTER FILTER

    private bool CanAffectCharacter(PlayerDeath deathTarget)
    {
        if (deathTarget == null) return true;

        switch (deathTarget.CharacterRole)
        {
            case PartyCharacterRole.Wife:
                return affectWife;

            case PartyCharacterRole.Player1:
            default:
                return affectPlayer1;
        }
    }


    // MISSING EXPOSURE WARNING

    private void WarnIfCharacterIsMissingExposure(Collider2D other)
    {
        PlayerDeath deathTarget = FindDeathTarget(other);

        if (deathTarget == null || !CanAffectCharacter(deathTarget) || warnedMissingExposure.Contains(deathTarget))
            return;

        warnedMissingExposure.Add(deathTarget);
    }


    private PlayerDeath FindDeathTarget(Collider2D other)
    {
        if (other == null)
            return null;

        PlayerDeath death = other.GetComponentInParent<PlayerDeath>();
        if (death != null)
            return death;

        if (other.attachedRigidbody != null)
        {
            death = other.attachedRigidbody.GetComponent<PlayerDeath>();
            if (death != null)
                return death;

            death = other.attachedRigidbody.GetComponentInParent<PlayerDeath>();
        }

        return death;
    }


    // DISABLE

    private void OnDisable()
    {
        if (exposureColliderCounts.Count > 0)
        {
            List<PlayerLightExposure> characters = new List<PlayerLightExposure>(exposureColliderCounts.Keys);

            foreach (PlayerLightExposure exposure in characters)
                if (exposure != null) exposure.ExitDangerousLight(this);

            exposureColliderCounts.Clear();
        }

        warnedMissingExposure.Clear();
    }


    // COLLIDER

    private void ConfigureCollider()
    {
        if (zoneCollider == null)
            return;

        if (forceIsTrigger)
            zoneCollider.isTrigger = true;
    }


    // RESET / VALIDATE

    private void Reset()
    {
        zoneCollider = GetComponent<Collider2D>();
        ConfigureCollider();
    }


    private void OnValidate()
    {
        exposureDuration = Mathf.Max(0.1f, exposureDuration);

        zoneCollider = GetComponent<Collider2D>();
        ConfigureCollider();
    }
}
