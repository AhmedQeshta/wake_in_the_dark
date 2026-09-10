using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Collider2D))]
public class CinematicTrigger : MonoBehaviour
{
    // SESSION HISTORY
    /*
     * This survives a scene unload/reload because it is static.
     *
     * Example:
     * Level_Final plays intro once
     * -> player dies
     * -> Level_Final reloads
     * -> the new CinematicTrigger sees that Level_Final already played
     * -> intro does NOT play again.
     *
     * It resets automatically when the game/app is started again.
     */
    private static readonly HashSet<string> playedScenes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);


    // REFERENCES

    [Header("References")]

    [Tooltip("LevelCameraDirector for THIS level.If left empty, the script searches only inside this scene.")]
    [SerializeField] private LevelCameraDirector levelCameraDirector;


    // SETTINGS

    [Header("Settings")]

    [Tooltip("If enabled, this level's intro cinematic plays only once for the current game session. Reloading the same level will not replay it.")]
    [SerializeField] private bool playOnlyOnce = true;


    [Tooltip("Tag used by the Player root GameObject.")]
    [SerializeField] private string playerTag = "Player";


    [Tooltip("After an additive scene load, wait a few physics frames and check whether the Player was spawned already overlapping this trigger.")]
    [SerializeField] private bool checkInitialOverlap = true;


    [SerializeField, Min(1)] private int initialOverlapCheckFrames = 3;


    // STATE

    private Collider2D triggerCollider;
    private bool hasTriggered;
    private bool triggeredDuringCurrentOverlap;
    private readonly HashSet<Collider2D> playerCollidersInside = new HashSet<Collider2D>();
    private Coroutine initialOverlapRoutine;


    // AWAKE
    private void Awake()
    {
        triggerCollider = GetComponent<Collider2D>();
        ConfigureCollider();
        ResolveDirector();
        /*  * IMPORTANT:  * A normal bool would reset to false every time this scene reloads.  *  * Read the persistent session history instead.  */
        hasTriggered = playOnlyOnce && HasPlayedThisScene();
    }


    // START

    private void Start()
    {
        if (checkInitialOverlap && !(playOnlyOnce && hasTriggered))
            initialOverlapRoutine = StartCoroutine(InitialOverlapCheckRoutine());
    }


    // TRIGGER EVENTS

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryRegisterAndTrigger(other);
    }


    private void OnTriggerStay2D(Collider2D other)
    {
        TryRegisterAndTrigger(other);
    }


    private void OnTriggerExit2D(Collider2D other)
    {
        if (!IsPlayerCollider(other))
            return;

        playerCollidersInside.Remove(other);

        if (playerCollidersInside.Count == 0)
            triggeredDuringCurrentOverlap = false;
    }


    // REGISTER PLAYER

    private void TryRegisterAndTrigger(Collider2D other)
    {
        if (!IsPlayerCollider(other))
            return;

        playerCollidersInside.Add(other);

        TryPlayCinematic();
    }


    // PLAY CINEMATIC

    private void TryPlayCinematic()
    { /*  * If this scene already played its intro during this game session,  * do nothing even if the level has just been reloaded.  */
        if (playOnlyOnce && (hasTriggered || HasPlayedThisScene()))
        {
            hasTriggered = true;
            return;
        }

        if (triggeredDuringCurrentOverlap || (LevelLoader.Instance != null && LevelLoader.Instance.IsLoading))
            return;

        ResolveDirector();

        if (levelCameraDirector == null)
            return;

        triggeredDuringCurrentOverlap = true;

        if (playOnlyOnce)
        {
            hasTriggered = true;
            MarkThisSceneAsPlayed();
        }

        levelCameraDirector.PlayIntroTimeline();
    }


    // INITIAL OVERLAP

    private IEnumerator InitialOverlapCheckRoutine()
    {
        int frames = Mathf.Max(1, initialOverlapCheckFrames);

        for (int i = 0; i < frames; i++)
        {
            yield return new WaitForFixedUpdate();

            if (playOnlyOnce && (hasTriggered || HasPlayedThisScene()))
            {
                hasTriggered = true;
                yield break;
            }

            CheckForPlayerAlreadyInside();
        }

        initialOverlapRoutine = null;
    }


    private void CheckForPlayerAlreadyInside()
    {
        if (triggerCollider == null || !triggerCollider.enabled || !triggerCollider.gameObject.activeInHierarchy)
            return;

        PlayerMovement player = FindPlayerInMyScene();

        if (player == null)
            return;

        Collider2D[] playerColliders = player.GetComponentsInChildren<Collider2D>(true);

        foreach (Collider2D playerCollider in playerColliders)
        {
            if (playerCollider == null || !playerCollider.enabled || playerCollider == triggerCollider)
                continue;

            ColliderDistance2D distance = triggerCollider.Distance(playerCollider);

            if (!distance.isOverlapped)
                continue;

            playerCollidersInside.Add(playerCollider);

            TryPlayCinematic();
            return;
        }
    }


    // PLAYER DETECTION
    private bool IsPlayerCollider(Collider2D other)
    {
        if (other == null)
            return false;

        PlayerMovement movement = other.GetComponentInParent<PlayerMovement>();

        if (movement != null)
            return string.IsNullOrWhiteSpace(playerTag) || movement.CompareTag(playerTag);

        if (other.attachedRigidbody != null)
        {
            GameObject bodyObject = other.attachedRigidbody.gameObject;

            if (bodyObject != null && (string.IsNullOrWhiteSpace(playerTag) || bodyObject.CompareTag(playerTag)))
                return true;
        }

        return !string.IsNullOrWhiteSpace(playerTag) && other.CompareTag(playerTag);
    }


    // FIND PLAYER IN THIS SCENE
    private PlayerMovement FindPlayerInMyScene()
    {
        Scene scene = gameObject.scene;

        if (!scene.IsValid() || !scene.isLoaded)
            return null;

        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root == null)
                continue;

            PlayerMovement player = root.GetComponentInChildren<PlayerMovement>(true);

            if (player != null)
                return player;
        }

        return null;
    }


    // RESOLVE DIRECTOR

    private void ResolveDirector()
    {
        if (levelCameraDirector != null && levelCameraDirector.gameObject.scene == gameObject.scene)
            return;

        levelCameraDirector = null;

        Scene scene = gameObject.scene;

        if (!scene.IsValid() || !scene.isLoaded)
            return;

        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root == null)
                continue;

            LevelCameraDirector director = root.GetComponentInChildren<LevelCameraDirector>(true);

            if (director != null)
            {
                levelCameraDirector = director;
                return;
            }
        }
    }


    // SESSION HISTORY

    private string GetSceneKey()
    {
        Scene scene = gameObject.scene;

        if (!scene.IsValid()) return string.Empty;

        /*  * Scene path is safer than only the scene name.  * Fall back to name if path is unavailable.  */
        if (!string.IsNullOrWhiteSpace(scene.path))
            return scene.path;

        return scene.name;
    }


    private bool HasPlayedThisScene()
    {
        string sceneKey = GetSceneKey();

        if (string.IsNullOrWhiteSpace(sceneKey))
            return false;

        return playedScenes.Contains(sceneKey);
    }


    private void MarkThisSceneAsPlayed()
    {
        string sceneKey = GetSceneKey();

        if (string.IsNullOrWhiteSpace(sceneKey))
            return;

        playedScenes.Add(sceneKey);
    }


    // RESET THIS TRIGGER

    /*
     * Optional utility.
     *
     * Calling this allows THIS scene intro to play again
     * during the current game session.
     */
    public void ResetTrigger()
    {
        hasTriggered = false;
        triggeredDuringCurrentOverlap = false;
        playerCollidersInside.Clear();

        string sceneKey = GetSceneKey();

        if (!string.IsNullOrWhiteSpace(sceneKey))
            playedScenes.Remove(sceneKey);
    }


    // RESET ALL SESSION CINEMATICS
    /*
     * Call this when starting a completely NEW GAME if the player
     * can start a second playthrough without closing the application.
     */
    public static void ResetAllSessionTriggers()
    {
        playedScenes.Clear();
    }


    // COLLIDER

    private void ConfigureCollider()
    {
        if (triggerCollider == null)
            triggerCollider = GetComponent<Collider2D>();

        if (triggerCollider != null)
            triggerCollider.isTrigger = true;
    }


    // RESET

    private void Reset()
    {
        triggerCollider = GetComponent<Collider2D>();

        ConfigureCollider();
        ResolveDirector();
    }


    // VALIDATE

    private void OnValidate()
    {
        initialOverlapCheckFrames = Mathf.Max(1, initialOverlapCheckFrames);

        if (string.IsNullOrWhiteSpace(playerTag))
            playerTag = "Player";

        triggerCollider = GetComponent<Collider2D>();

        ConfigureCollider();
    }


    // DISABLE

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
