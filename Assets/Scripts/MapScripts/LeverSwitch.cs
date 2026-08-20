using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class LeverSwitch : MonoBehaviour
{
    [Header("Interaction")]
    [SerializeField]
    private KeyCode interactionKey = KeyCode.E;

    [SerializeField]
    private string playerTag = "Player";

    [Header("Target")]
    [SerializeField]
    private HideableTilemap targetTilemap;

    [Header("Audio")]
    [SerializeField]
    private AudioClip toggleSound;

    [SerializeField, Range(0f, 1f)]
    private float toggleSoundVolume = 0.8f;

    [SerializeField]
    private bool randomizeTogglePitch = false;

    [SerializeField, Range(0.5f, 1.5f)]
    private float minimumTogglePitch = 0.95f;

    [SerializeField, Range(0.5f, 1.5f)]
    private float maximumTogglePitch = 1.05f;

    [Header("Lever Animation")]
    [SerializeField]
    private Animator leverAnimator;

    [SerializeField]
    private string activatedParameter =
        "IsActivated";

    [Header("Options")]
    [Tooltip("When enabled, pressing the key again restores the door.")]
    [SerializeField]
    private bool canToggle = true;

    private bool playerIsNearby;
    private bool leverIsActivated;

    private int activatedParameterHash;
    private AudioSource audioSource;


    [SerializeField]
    private string leverOffStateName = "LeverOff";

    [SerializeField]
    private string leverOnStateName = "LeverOn";

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();

        activatedParameterHash =
            Animator.StringToHash(
                activatedParameter
            );

        if (audioSource != null)
        {
            audioSource.playOnAwake = false;
            audioSource.loop = false;
        }
    }

    private void Update()
    {
        if (!playerIsNearby)
            return;

        if (Input.GetKeyDown(interactionKey))
        {
            Debug.Log(
                "E pressed near lever.",
                this
            );

            UseLever();
        }
    }

    private void UseLever()
    {
        if (targetTilemap == null)
        {
            Debug.LogError(
                "The lever has no HideableTilemap target.",
                this
            );

            return;
        }

        if (targetTilemap.IsTransitioning)
        {
            Debug.Log(
                "Lever blocked because target Tilemap is transitioning.",
                this
            );

            return;
        }

        bool actionStarted;

        if (canToggle)
        {
            actionStarted =
                targetTilemap.Toggle();

            Debug.Log(
                $"Door toggle requested. Success: {actionStarted}, " +
                $"Currently hidden: {targetTilemap.IsHidden}",
                this
                );

            if (actionStarted)
            {
                leverIsActivated =
                    !leverIsActivated;
            }
        }
        else
        {
            if (leverIsActivated)
                return;

            actionStarted =
                targetTilemap.Hide();

            if (actionStarted)
            {
                leverIsActivated = true;
            }
        }

        if (!actionStarted)
            return;

        if (leverAnimator != null)
        {
            leverAnimator.SetBool(
                activatedParameterHash,
                leverIsActivated
            );
        }

        PlayToggleSound();
    }

    private void PlayToggleSound()
    {
        if (audioSource == null || toggleSound == null)
            return;

        audioSource.pitch = randomizeTogglePitch
            ? Random.Range(minimumTogglePitch, maximumTogglePitch)
            : 1f;

        audioSource.PlayOneShot(
            toggleSound,
            toggleSoundVolume
        );
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log(
         $"Lever trigger entered by: {other.name}",
         this
     );

        if (IsPlayer(other))
        {
            playerIsNearby = true;

            Debug.Log(
                "Player can use lever.",
                this
            );
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (IsPlayer(other))
        {
            playerIsNearby = false;

            Debug.Log(
                "Player left lever.",
                this
            );
        }
    }

    private bool IsPlayer(Collider2D other)
    {
        if (other.CompareTag(playerTag))
            return true;

        Rigidbody2D attachedBody =
            other.attachedRigidbody;

        return attachedBody != null &&
               attachedBody.CompareTag(playerTag);
    }

    private void OnValidate()
    {
        if (string.IsNullOrWhiteSpace(playerTag))
        {
            playerTag = "Player";
        }

        if (string.IsNullOrWhiteSpace(
                activatedParameter))
        {
            activatedParameter =
                "IsActivated";
        }

        toggleSoundVolume = Mathf.Clamp01(toggleSoundVolume);
        minimumTogglePitch = Mathf.Max(0.5f, minimumTogglePitch);
        maximumTogglePitch = Mathf.Min(1.5f, maximumTogglePitch);

        if (minimumTogglePitch > maximumTogglePitch)
        {
            float pitchSwap = minimumTogglePitch;
            minimumTogglePitch = maximumTogglePitch;
            maximumTogglePitch = pitchSwap;
        }
    }


    public void RestoreVisualState()
    {
        if (leverAnimator == null)
            return;

        // Restore the Animator parameter first.
        leverAnimator.SetBool(
            activatedParameterHash,
            leverIsActivated
        );

        // Choose the state the lever was in before disappearing.
        string stateName = leverIsActivated
            ? leverOnStateName
            : leverOffStateName;

        // Show the LAST frame of that animation.
        leverAnimator.Play(
            stateName,
            0,
            1f
        );

        // Force Animator to immediately update visually.
        leverAnimator.Update(0f);
    }
}