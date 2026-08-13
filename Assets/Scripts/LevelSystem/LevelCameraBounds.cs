using UnityEngine;

[DisallowMultipleComponent]
public class LevelCameraBounds : MonoBehaviour
{
    // ==================================================
    // COLLIDER
    // ==================================================

    [Header("Camera Bounds")]

    [SerializeField]
    private Collider2D boundingCollider;


    // ==================================================
    // PUBLIC
    // ==================================================

    public Collider2D BoundingCollider =>
        boundingCollider;


    // ==================================================
    // AWAKE
    // ==================================================

    private void Awake()
    {
        ResolveCollider();
    }


    // ==================================================
    // RESOLVE
    // ==================================================

    private void ResolveCollider()
    {
        if (boundingCollider != null)
            return;


        boundingCollider =
            GetComponent<Collider2D>();


        if (boundingCollider == null)
        {
            Debug.LogError(
                "LevelCameraBounds: No Collider2D found.",
                this
            );
        }
    }


    // ==================================================
    // VALIDATION
    // ==================================================

    private void OnValidate()
    {
        if (boundingCollider == null)
        {
            boundingCollider =
                GetComponent<Collider2D>();
        }
    }
}