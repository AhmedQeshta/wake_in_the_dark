using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Cinemachine;

public class LevelCameraDirector : MonoBehaviour
{
    // ==================================================
    // CAMERA
    // ==================================================

    [Header("Level Intro Camera")]

    [SerializeField]
    private CinemachineCamera introCamera;


    // ==================================================
    // PLAYER CAMERA
    // ==================================================

    [Header("Player Camera")]

    [SerializeField]
    private PlayerCameraBinder playerCameraBinder;


    // ==================================================
    // INTRO SETTINGS
    // ==================================================

    [Header("Intro Settings")]

    [Tooltip(
        "Play the wide establishing shot when entering this level."
    )]
    [SerializeField]
    private bool playOnLevelEnter = true;


    [Tooltip(
        "Play the establishing shot again after Reset."
    )]
    [SerializeField]
    private bool playOnReload = false;


    [Tooltip(
        "How long the wide view remains visible."
    )]
    [SerializeField, Min(0f)]
    private float introHoldDuration = 0.6f;


    [Tooltip(
        "Should match the Default Blend time " +
        "on the Cinemachine Brain."
    )]
    [SerializeField, Min(0f)]
    private float blendDuration = 0.8f;


    // ==================================================
    // PLAYER CONTROL
    // ==================================================

    [Header("Player Control")]

    [SerializeField]
    private bool freezePlayerDuringIntro = true;


    // ==================================================
    // STATE
    // ==================================================

    private bool prepared;

    private bool playing;

    private bool previousMovementEnabled = true;


    // ==================================================
    // PUBLIC
    // ==================================================

    public bool PlayOnLevelEnter =>
        playOnLevelEnter;


    public bool PlayOnReload =>
        playOnReload;


    public bool IsPrepared =>
        prepared;


    public bool IsPlaying =>
        playing;


    // ==================================================
    // PREPARE INTRO
    // ==================================================

    public void PrepareIntro()
    {
        ResolveReferences();


        if (!CanPlayIntro())
        {
            SkipToGameplayImmediate();

            return;
        }


        CinemachineCamera gameplayCamera =
            playerCameraBinder.GameplayCamera;


        PlayerMovement movement =
            playerCameraBinder.PlayerMovement;


        // ----------------------------------------------
        // PLAYER
        // ----------------------------------------------

        if (movement != null)
        {
            previousMovementEnabled =
                movement.enabled;


            if (freezePlayerDuringIntro)
            {
                movement.enabled =
                    false;
            }
        }


        // ----------------------------------------------
        // IMPORTANT ORDER
        // ----------------------------------------------
        //
        // Turn Intro ON first,
        // then Gameplay OFF.
        //
        // This prevents a frame with no
        // available CinemachineCamera.
        // ----------------------------------------------

        introCamera.gameObject.SetActive(
            true
        );


        gameplayCamera.gameObject.SetActive(
            false
        );


        prepared =
            true;


        playing =
            false;


        Debug.Log(
            "Level intro camera prepared: " +
            gameObject.scene.name,
            this
        );
    }


    // ==================================================
    // PLAY INTRO
    // ==================================================

    public IEnumerator PlayIntroRoutine()
    {
        if (playing)
            yield break;


        if (!prepared)
        {
            PrepareIntro();
        }


        if (!CanPlayIntro())
        {
            SkipToGameplayImmediate();

            yield break;
        }


        playing =
            true;


        CinemachineCamera gameplayCamera =
            playerCameraBinder.GameplayCamera;


        // ==================================================
        // SHOW WIDE SHOT
        // ==================================================

        if (introHoldDuration > 0f)
        {
            yield return new WaitForSecondsRealtime(
                introHoldDuration
            );
        }


        // ==================================================
        // ACTIVATE PLAYER CAMERA
        // ==================================================

        /*
         * Turn GameplayCamera on first.
         */
        gameplayCamera.gameObject.SetActive(
            true
        );


        /*
         * Give Cinemachine one frame to register
         * the new camera.
         */
        yield return null;


        // ==================================================
        // RELEASE INTRO CAMERA
        // ==================================================

        /*
         * Cinemachine Brain will now switch/blend
         * to GameplayCamera according to the
         * Brain Default Blend.
         */
        introCamera.gameObject.SetActive(
            false
        );


        // ==================================================
        // WAIT FOR BLEND
        // ==================================================

        if (blendDuration > 0f)
        {
            yield return new WaitForSecondsRealtime(
                blendDuration
            );
        }


        // ==================================================
        // ENABLE PLAYER
        // ==================================================

        RestorePlayerMovement();


        prepared =
            false;


        playing =
            false;


        Debug.Log(
            "Level intro complete: " +
            gameObject.scene.name,
            this
        );
    }


    // ==================================================
    // SKIP
    // ==================================================

    public void SkipToGameplayImmediate()
    {
        ResolveReferences();


        if (playerCameraBinder != null &&
            playerCameraBinder.GameplayCamera != null)
        {
            playerCameraBinder
                .GameplayCamera
                .gameObject
                .SetActive(
                    true
                );
        }


        if (introCamera != null)
        {
            introCamera.gameObject.SetActive(
                false
            );
        }


        RestorePlayerMovement();


        prepared =
            false;


        playing =
            false;
    }


    // ==================================================
    // RESTORE PLAYER
    // ==================================================

    private void RestorePlayerMovement()
    {
        if (playerCameraBinder == null)
            return;


        PlayerMovement movement =
            playerCameraBinder.PlayerMovement;


        if (movement == null)
            return;


        if (freezePlayerDuringIntro)
        {
            movement.enabled =
                previousMovementEnabled;
        }
    }


    // ==================================================
    // CHECK
    // ==================================================

    private bool CanPlayIntro()
    {
        return
            introCamera != null &&
            playerCameraBinder != null &&
            playerCameraBinder.GameplayCamera != null &&
            introCamera !=
            playerCameraBinder.GameplayCamera;
    }


    // ==================================================
    // REFERENCES
    // ==================================================

    private void ResolveReferences()
    {
        if (playerCameraBinder == null)
        {
            playerCameraBinder =
                FindComponentInMyScene<PlayerCameraBinder>();
        }


        /*
         * Best setup:
         *
         * CameraSetup
         * ├ CameraBounds
         * └ IntroCamera
         *
         * so this fallback can find IntroCamera.
         */
        if (introCamera == null)
        {
            CinemachineCamera[] cameras =
                GetComponentsInChildren<CinemachineCamera>(
                    true
                );


            foreach (
                CinemachineCamera camera
                in cameras)
            {
                if (camera == null)
                    continue;


                if (playerCameraBinder != null &&
                    camera ==
                    playerCameraBinder.GameplayCamera)
                {
                    continue;
                }


                introCamera =
                    camera;

                break;
            }
        }
    }


    // ==================================================
    // SCENE SEARCH
    // ==================================================

    private T FindComponentInMyScene<T>()
        where T : Component
    {
        Scene scene =
            gameObject.scene;


        if (!scene.IsValid() ||
            !scene.isLoaded)
        {
            return null;
        }


        GameObject[] roots =
            scene.GetRootGameObjects();


        foreach (GameObject root in roots)
        {
            if (root == null)
                continue;


            T result =
                root.GetComponentInChildren<T>(
                    true
                );


            if (result != null)
            {
                return result;
            }
        }


        return null;
    }


    // ==================================================
    // VALIDATION
    // ==================================================

    private void OnValidate()
    {
        introHoldDuration =
            Mathf.Max(
                0f,
                introHoldDuration
            );


        blendDuration =
            Mathf.Max(
                0f,
                blendDuration
            );
    }
}