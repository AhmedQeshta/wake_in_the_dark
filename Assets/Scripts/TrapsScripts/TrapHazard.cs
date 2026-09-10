using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class TrapHazard : MonoBehaviour
{
    // CHARACTER FILTER
    [Header("Character Filter")]

    [Tooltip("Allow this trap to kill Player 1.")]
    [SerializeField] private bool killPlayer1 = true;

    [Tooltip("Allow this trap to kill Wife / Player 2.")]
    [SerializeField] private bool killWife = true;


    // AUDIO

    [Header("Trap Audio")]
    [SerializeField] private AudioClip trapHitSound;
    [SerializeField, Range(0f, 1f)] private float trapHitVolume = 1f;
    [SerializeField] private bool randomizePitch = true;
    [SerializeField, Range(0.5f, 1.5f)] private float minimumPitch = 0.95f;
    [SerializeField, Range(0.5f, 1.5f)] private float maximumPitch = 1.05f;


    // COMPONENTS
    private AudioSource audioSource;

    // AWAKE
    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.loop = false;

        /*
         * Trap sound is gameplay audio.
         * Pause it with normal gameplay audio.
         */
        audioSource.ignoreListenerPause = false;
    }


    // TRIGGER
    private void OnTriggerEnter2D(Collider2D other)
    {
        PlayerDeath deathTarget = FindDeathTarget(other);

        if (deathTarget == null || !CanKillCharacter(deathTarget) || deathTarget.IsDead)
            return;

        // SOUND
        float soundDuration = PlayTrapSound();

        // KILL THE ACTUAL CHARACTER THAT TOUCHED THE TRAP
        deathTarget.KillPlayer(soundDuration);
    }


    // FIND DEATH TARGET
    private PlayerDeath FindDeathTarget(Collider2D other)
    {
        if (other == null)
            return null;

        /*
         * First try the collider hierarchy.
         *
         * This supports:
         * Player
         * Wife
         * child colliders
         */
        PlayerDeath death = other.GetComponentInParent<PlayerDeath>();

        if (death != null)
            return death;


        /*
         * Then try the Rigidbody2D root.
         */
        if (other.attachedRigidbody != null)
        {
            death = other.attachedRigidbody.GetComponent<PlayerDeath>();
            if (death != null)
                return death;

            death = other.attachedRigidbody.GetComponentInParent<PlayerDeath>();
        }


        return death;
    }


    // CHARACTER FILTER
    private bool CanKillCharacter(PlayerDeath deathTarget)
    {
        if (deathTarget == null)
            return false;


        switch (deathTarget.CharacterRole)
        {
            case PartyCharacterRole.Wife:
                return killWife;

            case PartyCharacterRole.Player1:
            default:
                return killPlayer1;
        }
    }


    // SOUND
    private float PlayTrapSound()
    {
        if (audioSource == null || trapHitSound == null)
            return 0f;


        if (randomizePitch)
            audioSource.pitch = Random.Range(minimumPitch, maximumPitch);
        else
            audioSource.pitch = 1f;


        audioSource.PlayOneShot(trapHitSound, trapHitVolume);


        /*
         * Actual duration after pitch modification.
         */
        return trapHitSound.length / Mathf.Max(Mathf.Abs(audioSource.pitch), 0.01f);
    }


    // VALIDATE

    private void OnValidate()
    {
        trapHitVolume = Mathf.Clamp01(trapHitVolume);
        minimumPitch = Mathf.Clamp(minimumPitch, 0.5f, 1.5f);
        maximumPitch = Mathf.Clamp(maximumPitch, 0.5f, 1.5f);

        if (minimumPitch > maximumPitch)
        {
            float temp = minimumPitch;
            minimumPitch = maximumPitch;
            maximumPitch = temp;
        }
    }
}
