using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.Tilemaps;

[RequireComponent(typeof(Collider2D))]
public class AnimatedTileTrigger : MonoBehaviour
{
    // ==================================================
    // TILEMAP
    // ==================================================

    [Header("Tilemap")]

    [SerializeField]
    private Tilemap tilemap;


    // ==================================================
    // ANIMATED PARTS
    // ==================================================

    [Header("Animated Parts")]

    [Tooltip(
        "One entry for each tile part. " +
        "For the 2x2 statue use 4 entries: " +
        "top-left, top-right, bottom-left, bottom-right."
    )]
    [SerializeField]
    private AnimatedPart[] animatedParts;


    // ==================================================
    // PLAYBACK
    // ==================================================

    [Header("Playback")]

    [Tooltip(
        "Duration of one complete animation cycle. " +
        "Example: 4 frames at 4 FPS = 1 second."
    )]
    [SerializeField, Min(0.01f)]
    private float animationDuration = 1f;


    [Tooltip(
        "What happens after one complete animation cycle."
    )]
    [SerializeField]
    private AnimationEndBehavior endBehavior =
        AnimationEndBehavior.FreezeOnLastFrame;


    // ==================================================
    // TRIGGER OPTIONS
    // ==================================================

    [Header("Trigger Options")]

    [FormerlySerializedAs("playOnce")]
    [Tooltip(
        "If enabled, this trigger can activate only once."
    )]
    [SerializeField]
    private bool triggerOnlyOnce = true;


    [Tooltip("Delay before the animation starts.")]
    [SerializeField, Min(0f)]
    private float startDelay = 0f;


    [Tooltip(
        "Disable the trigger collider after activation."
    )]
    [SerializeField]
    private bool disableTriggerAfterActivation = true;


    // ==================================================
    // STATE
    // ==================================================

    private Collider2D triggerCollider;

    private TileBase[] originalTiles;

    private bool activated;

    private Coroutine animationRoutine;


    // ==================================================
    // AWAKE
    // ==================================================

    private void Awake()
    {
        triggerCollider =
            GetComponent<Collider2D>();


        if (triggerCollider != null)
        {
            triggerCollider.isTrigger =
                true;
        }
    }


    // ==================================================
    // START
    // ==================================================

    private void Start()
    {
        CacheOriginalTiles();
    }


    // ==================================================
    // CACHE ORIGINAL TILES
    // ==================================================

    private void CacheOriginalTiles()
    {
        if (tilemap == null ||
            animatedParts == null)
        {
            return;
        }


        originalTiles =
            new TileBase[
                animatedParts.Length
            ];


        for (int i = 0;
             i < animatedParts.Length;
             i++)
        {
            AnimatedPart part =
                animatedParts[i];


            if (part == null)
                continue;


            originalTiles[i] =
                tilemap.GetTile(
                    part.cell
                );
        }
    }


    // ==================================================
    // PLAYER ENTERS TRIGGER
    // ==================================================

    private void OnTriggerEnter2D(
        Collider2D other)
    {
        if (activated &&
            triggerOnlyOnce)
        {
            return;
        }


        PlayerMovement player =
            other.GetComponentInParent
                <PlayerMovement>();


        if (player == null)
            return;


        StartAnimation();
    }


    // ==================================================
    // START ANIMATION
    // ==================================================

    public void StartAnimation()
    {
        if (activated &&
            triggerOnlyOnce)
        {
            return;
        }


        activated =
            true;


        if (animationRoutine != null)
        {
            StopCoroutine(
                animationRoutine
            );
        }


        animationRoutine =
            StartCoroutine(
                StartAnimationRoutine()
            );
    }


    // ==================================================
    // ANIMATION ROUTINE
    // ==================================================

    private IEnumerator
        StartAnimationRoutine()
    {
        if (startDelay > 0f)
        {
            yield return
                new WaitForSeconds(
                    startDelay
                );
        }


        ApplyAnimatedTiles();


        if (disableTriggerAfterActivation &&
            triggerCollider != null)
        {
            triggerCollider.enabled =
                false;
        }


        if (endBehavior ==
            AnimationEndBehavior.KeepAnimating)
        {
            animationRoutine =
                null;

            yield break;
        }


        yield return
            new WaitForSeconds(
                animationDuration
            );


        ApplyEndBehavior();


        animationRoutine =
            null;
    }


    // ==================================================
    // APPLY ANIMATED TILES
    // ==================================================

    private void ApplyAnimatedTiles()
    {
        if (tilemap == null ||
            animatedParts == null)
        {
            return;
        }


        foreach (
            AnimatedPart part
            in animatedParts
        )
        {
            if (part == null ||
                part.animatedTile == null)
            {
                continue;
            }


            tilemap.SetTile(
                part.cell,
                part.animatedTile
            );


            tilemap.RefreshTile(
                part.cell
            );
        }
    }


    // ==================================================
    // END BEHAVIOR
    // ==================================================

    private void ApplyEndBehavior()
    {
        switch (endBehavior)
        {
            case AnimationEndBehavior
                .FreezeOnFirstFrame:

                RestoreOriginalTiles();

                break;


            case AnimationEndBehavior
                .FreezeOnLastFrame:

                ApplyLastFrameTiles();

                break;


            case AnimationEndBehavior
                .Disappear:

                ClearAnimatedCells();

                break;


            case AnimationEndBehavior
                .KeepAnimating:

            default:

                break;
        }
    }


    // ==================================================
    // LAST FRAME
    // ==================================================

    private void ApplyLastFrameTiles()
    {
        if (tilemap == null ||
            animatedParts == null)
        {
            return;
        }


        for (int i = 0;
             i < animatedParts.Length;
             i++)
        {
            AnimatedPart part =
                animatedParts[i];


            if (part == null)
                continue;


            if (part.lastFrameTile == null)
            {
                Debug.LogWarning(
                    "AnimatedTileTrigger: Last Frame Tile " +
                    "is missing for Animated Part " +
                    i +
                    ".",
                    this
                );


                continue;
            }


            tilemap.SetTile(
                part.cell,
                part.lastFrameTile
            );


            tilemap.RefreshTile(
                part.cell
            );
        }
    }


    // ==================================================
    // DISAPPEAR
    // ==================================================

    private void ClearAnimatedCells()
    {
        if (tilemap == null ||
            animatedParts == null)
        {
            return;
        }


        foreach (
            AnimatedPart part
            in animatedParts
        )
        {
            if (part == null)
                continue;


            tilemap.SetTile(
                part.cell,
                null
            );


            tilemap.RefreshTile(
                part.cell
            );
        }
    }


    // ==================================================
    // RESET
    // ==================================================

    public void ResetAnimation()
    {
        if (animationRoutine != null)
        {
            StopCoroutine(
                animationRoutine
            );


            animationRoutine =
                null;
        }


        activated =
            false;


        RestoreOriginalTiles();


        if (triggerCollider != null)
        {
            triggerCollider.enabled =
                true;
        }
    }


    // ==================================================
    // RESTORE ORIGINAL / FIRST FRAME
    // ==================================================

    private void RestoreOriginalTiles()
    {
        if (tilemap == null ||
            animatedParts == null ||
            originalTiles == null)
        {
            return;
        }


        int count =
            Mathf.Min(
                animatedParts.Length,
                originalTiles.Length
            );


        for (int i = 0;
             i < count;
             i++)
        {
            AnimatedPart part =
                animatedParts[i];


            if (part == null)
                continue;


            tilemap.SetTile(
                part.cell,
                originalTiles[i]
            );


            tilemap.RefreshTile(
                part.cell
            );
        }
    }


    // ==================================================
    // VALIDATION
    // ==================================================

    private void OnValidate()
    {
        startDelay =
            Mathf.Max(
                0f,
                startDelay
            );


        animationDuration =
            Mathf.Max(
                0.01f,
                animationDuration
            );


        Collider2D col =
            GetComponent<Collider2D>();


        if (col != null)
        {
            col.isTrigger =
                true;
        }
    }
}