using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class DangerousLightZone : MonoBehaviour
{
    // ==================================================
    // LIGHT EXPOSURE
    // ==================================================

    [Header("Light Exposure")]

    [Tooltip("How long the player can stay inside THIS light before transforming into stone.")]
    [SerializeField, Min(0.1f)] private float exposureDuration = 3f;


    // ==================================================
    // COLLIDER
    // ==================================================

    [Header("Collider")]

    [Tooltip("Automatically keep this Collider2D configured as a Trigger.")]
    [SerializeField] private bool forceIsTrigger = true;


    // ==================================================
    // PUBLIC VALUES
    // ==================================================

    public float ExposureDuration => exposureDuration;


    // ==================================================
    // STATE
    // ==================================================

    /*
     * Player may have multiple Collider2D components.
     *
     * Count them so one collider exiting does not stop
     * exposure while another player collider is still inside.
     */
    private readonly Dictionary<PlayerLightExposure, int> playerColliderCounts = new Dictionary<PlayerLightExposure, int>();


    private Collider2D zoneCollider;


    // ==================================================
    // AWAKE
    // ==================================================

    private void Awake()
    {
        zoneCollider = GetComponent<Collider2D>();
        ConfigureCollider();
    }


    // ==================================================
    // ENTER
    // ==================================================

    private void OnTriggerEnter2D(Collider2D other)
    {
        PlayerLightExposure exposure = other.GetComponentInParent<PlayerLightExposure>();


        if (exposure == null)
            return;


        if (playerColliderCounts.TryGetValue(exposure, out int currentCount))
        {
            playerColliderCounts[exposure] = currentCount + 1;
        }
        else
        {
            playerColliderCounts.Add(exposure, 1);

            exposure.EnterDangerousLight(this);
        }
    }


    // ==================================================
    // EXIT
    // ==================================================

    private void OnTriggerExit2D(Collider2D other)
    {
        PlayerLightExposure exposure = other.GetComponentInParent<PlayerLightExposure>();


        if (exposure == null)
            return;


        if (!playerColliderCounts.TryGetValue(exposure, out int currentCount))
        {
            return;
        }


        currentCount--;


        if (currentCount <= 0)
        {
            playerColliderCounts.Remove(exposure);


            exposure.ExitDangerousLight(this);
        }
        else
        {
            playerColliderCounts[exposure] = currentCount;
        }
    }


    // ==================================================
    // DISABLE CLEANUP
    // ==================================================

    private void OnDisable()
    {
        if (playerColliderCounts.Count == 0)
            return;


        List<PlayerLightExposure> players = new List<PlayerLightExposure>(playerColliderCounts.Keys);


        foreach (PlayerLightExposure exposure in players)
        {
            if (exposure != null)
            {
                exposure.ExitDangerousLight(this);
            }
        }


        playerColliderCounts.Clear();
    }


    // ==================================================
    // COLLIDER SETUP
    // ==================================================

    private void ConfigureCollider()
    {
        if (zoneCollider == null)
            return;


        if (forceIsTrigger)
        {
            zoneCollider.isTrigger = true;
        }
    }


    // ==================================================
    // RESET
    // ==================================================

    private void Reset()
    {
        zoneCollider = GetComponent<Collider2D>();


        ConfigureCollider();
    }


    // ==================================================
    // VALIDATE
    // ==================================================

    private void OnValidate()
    {
        exposureDuration = Mathf.Max(0.1f, exposureDuration);


        zoneCollider = GetComponent<Collider2D>();


        ConfigureCollider();
    }
}
