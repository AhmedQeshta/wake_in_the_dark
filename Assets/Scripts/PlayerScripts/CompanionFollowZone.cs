using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class CompanionFollowZone : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CompanionFollower2D companionFollower;
    [SerializeField] private bool autoFindCompanion = true;

    [Header("Player")]
    [SerializeField] private string playerTag = "Player";

    [Header("Zone Behavior")]
    [SerializeField] private bool enableFollowingOnEnter = true;
    [SerializeField] private bool disableFollowingOnExit = true;
    [SerializeField] private bool triggerOnlyOnce = false;

    private Collider2D zoneCollider;
    private bool used;

    private void Awake()
    {
        zoneCollider = GetComponent<Collider2D>();
        zoneCollider.isTrigger = true;
        ResolveCompanion();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (used && triggerOnlyOnce)
            return;

        if (!IsMainPlayer(other))
            return;

        ResolveCompanion();

        if (companionFollower == null)
        {
            Debug.LogWarning(
                "CompanionFollowZone: CompanionFollower2D was not found.",
                this
            );

            return;
        }

        if (enableFollowingOnEnter)
            companionFollower.EnableFollowing();

        if (triggerOnlyOnce)
            used = true;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!disableFollowingOnExit)
            return;

        if (!IsMainPlayer(other))
            return;

        ResolveCompanion();

        if (companionFollower != null)
            companionFollower.DisableFollowing();
    }

    private bool IsMainPlayer(Collider2D other)
    {
        if (other == null)
            return false;

        PlayerMovement movement = other.GetComponentInParent<PlayerMovement>();

        if (movement != null && movement.CompareTag(playerTag))
            return true;

        if (other.attachedRigidbody != null)
        {
            GameObject bodyObject = other.attachedRigidbody.gameObject;

            if (bodyObject != null && bodyObject.CompareTag(playerTag))
                return true;
        }

        return other.CompareTag(playerTag);
    }

    private void ResolveCompanion()
    {
        if (companionFollower != null || !autoFindCompanion)
            return;

        companionFollower = FindAnyObjectByType<CompanionFollower2D>();
    }

    private void OnValidate()
    {
        if (string.IsNullOrWhiteSpace(playerTag))
            playerTag = "Player";

        Collider2D col = GetComponent<Collider2D>();

        if (col != null)
            col.isTrigger = true;
    }
}
