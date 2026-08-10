using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class TrapHazard : MonoBehaviour
{
    // ==================================================
    // PLAYER
    // ==================================================

    [Header("Player Detection")]
    [SerializeField]
    private string playerTag = "Player";


    // ==================================================
    // AUDIO
    // ==================================================

    [Header("Trap Audio")]
    [SerializeField]
    private AudioClip trapHitSound;

    [SerializeField, Range(0f, 1f)]
    private float trapHitVolume = 1f;

    [SerializeField]
    private bool randomizePitch = true;

    [SerializeField, Range(0.5f, 1.5f)]
    private float minimumPitch = 0.95f;

    [SerializeField, Range(0.5f, 1.5f)]
    private float maximumPitch = 1.05f;


    // ==================================================
    // COMPONENTS
    // ==================================================

    private AudioSource audioSource;


    // ==================================================
    // STATE
    // ==================================================

    private bool triggered;


    // ==================================================
    // AWAKE
    // ==================================================

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();

        audioSource.playOnAwake = false;
        audioSource.loop = false;

        /*
         * Trap sound is normal gameplay audio.
         */
        audioSource.ignoreListenerPause = false;
    }


    // ==================================================
    // PLAYER TOUCH
    // ==================================================

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (triggered)
            return;


        GameObject playerObject =
            other.attachedRigidbody != null
                ? other.attachedRigidbody.gameObject
                : other.gameObject;


        if (!playerObject.CompareTag(playerTag))
            return;


        PlayerDeath playerDeath =
            playerObject.GetComponent<PlayerDeath>();


        if (playerDeath == null)
        {
            playerDeath =
                other.GetComponentInParent<PlayerDeath>();
        }


        if (playerDeath == null)
        {
            Debug.LogWarning(
                "Trap touched Player, but PlayerDeath was not found.",
                this
            );

            return;
        }


        if (playerDeath.IsDead)
            return;


        triggered = true;


        // ----------------------------------------------
        // PLAY SOUND IMMEDIATELY
        // ----------------------------------------------

        float soundDuration =
            PlayTrapSound();


        // ----------------------------------------------
        // KILL PLAYER
        // ----------------------------------------------

        playerDeath.KillPlayer(
            soundDuration
        );
    }


    // ==================================================
    // SOUND
    // ==================================================

    private float PlayTrapSound()
    {
        if (audioSource == null ||
            trapHitSound == null)
        {
            return 0f;
        }


        if (randomizePitch)
        {
            audioSource.pitch =
                Random.Range(
                    minimumPitch,
                    maximumPitch
                );
        }
        else
        {
            audioSource.pitch = 1f;
        }


        audioSource.PlayOneShot(
            trapHitSound,
            trapHitVolume
        );


        /*
         * Pitch changes playback duration.
         *
         * Faster pitch = shorter sound.
         * Slower pitch = longer sound.
         */
        float realDuration =
            trapHitSound.length /
            Mathf.Max(
                Mathf.Abs(audioSource.pitch),
                0.01f
            );


        return realDuration;
    }


    // ==================================================
    // VALIDATION
    // ==================================================

    private void OnValidate()
    {
        if (string.IsNullOrWhiteSpace(playerTag))
        {
            playerTag = "Player";
        }


        trapHitVolume =
            Mathf.Clamp01(
                trapHitVolume
            );


        minimumPitch =
            Mathf.Clamp(
                minimumPitch,
                0.5f,
                1.5f
            );


        maximumPitch =
            Mathf.Clamp(
                maximumPitch,
                0.5f,
                1.5f
            );


        if (minimumPitch > maximumPitch)
        {
            float temp =
                minimumPitch;

            minimumPitch =
                maximumPitch;

            maximumPitch =
                temp;
        }
    }
}