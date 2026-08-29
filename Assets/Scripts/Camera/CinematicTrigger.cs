using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Collider2D))]
public class CinematicTrigger : MonoBehaviour
{
    [Header("References")]
    [Tooltip("LevelCameraDirector for THIS level. If left empty, the script searches only inside this scene.")]
    [SerializeField] private LevelCameraDirector levelCameraDirector;

    [Header("Settings")]
    [Tooltip("If enabled, this trigger can start its cinematic only once until the scene is reloaded.")]
    [SerializeField] private bool playOnlyOnce = true;

    [Tooltip("Tag used by the Player root GameObject.")]
    [SerializeField] private string playerTag = "Player";

    [Tooltip("After an additive scene load, wait a few physics frames and check whether the Player was spawned already overlapping this trigger.")]
    [SerializeField] private bool checkInitialOverlap = true;

    [SerializeField, Min(1)]
    private int initialOverlapCheckFrames = 3;

    private Collider2D triggerCollider;
    private bool hasTriggered;
    private bool triggeredDuringCurrentOverlap;

    private readonly HashSet<Collider2D> playerCollidersInside =
        new HashSet<Collider2D>();

    private Coroutine initialOverlapRoutine;

    private void Awake()
    {
        triggerCollider = GetComponent<Collider2D>();
        ConfigureCollider();
        ResolveDirector();
    }

    private void Start()
    {
        if (checkInitialOverlap)
        {
            initialOverlapRoutine =
                StartCoroutine(InitialOverlapCheckRoutine());
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryRegisterAndTrigger(other, "OnTriggerEnter2D");
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        TryRegisterAndTrigger(other, "OnTriggerStay2D");
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!IsPlayerCollider(other))
            return;

        playerCollidersInside.Remove(other);

        if (playerCollidersInside.Count == 0)
        {
            triggeredDuringCurrentOverlap = false;
        }
    }

    private void TryRegisterAndTrigger(
        Collider2D other,
        string source)
    {
        if (!IsPlayerCollider(other))
            return;

        playerCollidersInside.Add(other);

        TryPlayCinematic(source);
    }

    private void TryPlayCinematic(string source)
    {
        if (playOnlyOnce && hasTriggered)
            return;

        if (triggeredDuringCurrentOverlap)
            return;


        if (LevelLoader.Instance != null &&
            LevelLoader.Instance.IsLoading)
        {
            return;
        }

        ResolveDirector();

        if (levelCameraDirector == null)
            return;

        triggeredDuringCurrentOverlap = true;

        if (playOnlyOnce)
            hasTriggered = true;

        levelCameraDirector.PlayIntroTimeline();
    }

    private IEnumerator InitialOverlapCheckRoutine()
    {
        int frames =
            Mathf.Max(
                1,
                initialOverlapCheckFrames
            );

        for (int i = 0; i < frames; i++)
        {
            yield return new WaitForFixedUpdate();

            if (playOnlyOnce && hasTriggered)
                yield break;

            CheckForPlayerAlreadyInside();
        }

        initialOverlapRoutine = null;
    }

    private void CheckForPlayerAlreadyInside()
    {
        if (triggerCollider == null ||
            !triggerCollider.enabled ||
            !triggerCollider.gameObject.activeInHierarchy)
        {
            return;
        }

        PlayerMovement player = FindPlayerInMyScene();

        if (player == null)
            return;

        Collider2D[] playerColliders = player.GetComponentsInChildren<Collider2D>(true);

        foreach (Collider2D playerCollider in playerColliders)
        {
            if (playerCollider == null ||
                !playerCollider.enabled ||
                playerCollider == triggerCollider)
            {
                continue;
            }

            ColliderDistance2D distance =
                triggerCollider.Distance(playerCollider);

            if (!distance.isOverlapped)
                continue;

            playerCollidersInside.Add(playerCollider);

            TryPlayCinematic("InitialOverlapCheck");

            return;
        }
    }

    private bool IsPlayerCollider(
        Collider2D other)
    {
        if (other == null)
            return false;

        PlayerMovement movement =
            other.GetComponentInParent<PlayerMovement>();

        if (movement != null)
        {
            return
                string.IsNullOrWhiteSpace(playerTag) ||
                movement.CompareTag(playerTag);
        }

        if (other.attachedRigidbody != null)
        {
            GameObject bodyObject =
                other.attachedRigidbody.gameObject;

            if (bodyObject != null &&
                (
                    string.IsNullOrWhiteSpace(playerTag) ||
                    bodyObject.CompareTag(playerTag)
                ))
            {
                return true;
            }
        }

        return
            !string.IsNullOrWhiteSpace(playerTag) &&
            other.CompareTag(playerTag);
    }

    private PlayerMovement FindPlayerInMyScene()
    {
        Scene scene =
            gameObject.scene;

        if (!scene.IsValid() ||
            !scene.isLoaded)
        {
            return null;
        }

        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root == null)
                continue;

            PlayerMovement player =
                root.GetComponentInChildren<PlayerMovement>(true);

            if (player != null)
                return player;
        }

        return null;
    }

    private void ResolveDirector()
    {
        if (levelCameraDirector != null &&
            levelCameraDirector.gameObject.scene ==
            gameObject.scene)
        {
            return;
        }

        levelCameraDirector = null;

        Scene scene =
            gameObject.scene;

        if (!scene.IsValid() ||
            !scene.isLoaded)
        {
            return;
        }

        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root == null)
                continue;

            LevelCameraDirector director =
                root.GetComponentInChildren<LevelCameraDirector>(true);

            if (director != null)
            {
                levelCameraDirector = director;
                return;
            }
        }
    }

    public void ResetTrigger()
    {
        hasTriggered = false;
        triggeredDuringCurrentOverlap = false;
        playerCollidersInside.Clear();
    }

    private void ConfigureCollider()
    {
        if (triggerCollider == null)
        {
            triggerCollider =
                GetComponent<Collider2D>();
        }

        if (triggerCollider != null)
        {
            triggerCollider.isTrigger = true;
        }
    }

    private void Reset()
    {
        triggerCollider =
            GetComponent<Collider2D>();

        ConfigureCollider();
        ResolveDirector();
    }

    private void OnValidate()
    {
        initialOverlapCheckFrames =
            Mathf.Max(
                1,
                initialOverlapCheckFrames
            );

        if (string.IsNullOrWhiteSpace(playerTag))
        {
            playerTag = "Player";
        }

        triggerCollider =
            GetComponent<Collider2D>();

        ConfigureCollider();
    }

    private void OnDisable()
    {
        if (initialOverlapRoutine != null)
        {
            StopCoroutine(initialOverlapRoutine);
            initialOverlapRoutine = null;
        }

        playerCollidersInside.Clear();
        triggeredDuringCurrentOverlap = false;
    }
}
