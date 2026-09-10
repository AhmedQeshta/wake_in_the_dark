using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class AudioDistance2D : MonoBehaviour
{
    // DISTANCE
    [Header("2D Hearing Distance")]

    [Tooltip("Inside this distance, the sound reaches Maximum Volume.")]
    [SerializeField, Min(0f)] private float fullVolumeDistance = 2f;


    [Tooltip("At or beyond this distance, the sound fades to silence.")]
    [SerializeField, Min(0.01f)] private float maxHearingDistance = 10f;


    // VOLUME

    [Header("Volume")]

    [Tooltip("Maximum AudioSource volume when the player is close.")]
    [SerializeField, Range(0f, 1f)] private float maximumVolume = 1f;


    [Tooltip("Controls the target volume between Full Volume Distance and Max Hearing Distance.")]
    [SerializeField] private AnimationCurve volumeCurve = AnimationCurve.Linear(0f, 1f, 1f, 0f);


    // SMOOTH FADE

    [Header("Smooth Fade")]

    [Tooltip("How long volume takes to fade from silence to Maximum Volume.")]
    [SerializeField, Min(0f)] private float fadeInDuration = 0.5f;


    [Tooltip("How long volume takes to fade from Maximum Volume to silence.")]
    [SerializeField, Min(0f)] private float fadeOutDuration = 0.75f;


    [Tooltip("If ON, the first volume calculation is applied immediately so the scene does not start with an incorrect loud volume.")]
    [SerializeField] private bool snapVolumeOnStart = true;


    // TARGET

    [Header("Listener / Player Target")]

    [Tooltip("Usually leave empty. The script prefers the active Player transform. If no Player exists, it falls back to AudioListener / Main Camera.")]
    [SerializeField] private Transform listenerTarget;


    [Tooltip("When auto-finding, prefer the GameObject tagged Player. Recommended ON for gameplay hearing regions.")]
    [SerializeField] private bool preferPlayerTarget = true;


    [Tooltip("Tag used by the main player.")]
    [SerializeField] private string playerTag = "Player";


    [Tooltip("Automatically resolve a replacement target after additive level loads/reloads.")]
    [SerializeField] private bool autoFindListener = true;


    // DEBUG / PUBLIC

    [Header("Debug")]
    [SerializeField] private bool drawHearingGizmos = true;


    public float CurrentDistance { get; private set; }
    public float TargetVolume { get; private set; }
    public bool IsInsideHearingRange => listenerTarget != null && CurrentDistance < maxHearingDistance;


    // COMPONENTS
    private AudioSource audioSource;


    // AWAKE
    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();

        /*  * Distance is handled entirely in X/Y by this component.  * Unity's built-in 3D rolloff is therefore disabled.  */
        audioSource.spatialBlend = 0f;

        ResolveListener();

        if (snapVolumeOnStart)
            ApplyVolumeImmediately();
    }


    // UPDATE

    private void Update()
    { /*  * The Player object is recreated after additive level reloads.  * Unity destroyed objects compare equal to null, so this safely  * resolves the new Player when necessary.  */
        if (listenerTarget == null && autoFindListener)
            ResolveListener();

        UpdateVolumeSmoothly();
    }


    // VOLUME

    private void ApplyVolumeImmediately()
    {
        if (audioSource == null)
            return;

        TargetVolume = CalculateTargetVolume();
        audioSource.volume = TargetVolume;
    }


    private void UpdateVolumeSmoothly()
    {
        if (audioSource == null || listenerTarget == null)
            return;

        TargetVolume = CalculateTargetVolume();

        float currentVolume = audioSource.volume;

        if (Mathf.Approximately(currentVolume, TargetVolume))
        {
            audioSource.volume = TargetVolume;
            return;
        }

        bool fadingIn = TargetVolume > currentVolume;

        float duration = fadingIn ? fadeInDuration : fadeOutDuration;

        if (duration <= 0f || maximumVolume <= 0f)
        {
            audioSource.volume = TargetVolume;
            return;
        }

        /*  * Speed is based on a full 0 -> MaximumVolume fade.  * Most smaller distance changes therefore finish proportionally faster.  */
        float volumePerSecond = maximumVolume / duration;

        audioSource.volume = Mathf.MoveTowards(currentVolume, TargetVolume, volumePerSecond * Time.unscaledDeltaTime);

        /*  * IMPORTANT:  *  * This script NEVER calls AudioSource.Stop() or Pause().  *  * When the Player leaves the hearing area, volume fades to 0  * but the clip keeps advancing silently.  *  * If the Player returns before the clip ends, it fades back in  * at the CURRENT playback position instead of restarting.  */
    }


    private float CalculateTargetVolume()
    {
        if (listenerTarget == null)
        {
            CurrentDistance = float.PositiveInfinity;
            return 0f;
        }

        Vector2 sourcePosition = new Vector2(transform.position.x, transform.position.y);
        Vector2 targetPosition = new Vector2(listenerTarget.position.x, listenerTarget.position.y);

        CurrentDistance = Vector2.Distance(sourcePosition, targetPosition);

        //  FULL VOLUME 
        if (CurrentDistance <= fullVolumeDistance)
            return maximumVolume;

        // SILENT
        if (CurrentDistance >= maxHearingDistance)
            return 0f;

        // DISTANCE CURVE
        float normalizedDistance = Mathf.InverseLerp(fullVolumeDistance, maxHearingDistance, CurrentDistance);
        float curveValue = volumeCurve != null && volumeCurve.length > 0 ? volumeCurve.Evaluate(normalizedDistance) : 1f - normalizedDistance;
        return maximumVolume * Mathf.Clamp01(curveValue);
    }


    // FIND PLAYER / LISTENER

    private void ResolveListener()
    {
        if (listenerTarget != null)
            return;

        // MAIN PLAYER FIRST
        if (preferPlayerTarget && !string.IsNullOrWhiteSpace(playerTag))
        {
            GameObject playerObject = null;

            try
            {
                playerObject = GameObject.FindGameObjectWithTag(playerTag);
            }
            catch (UnityException)
            {
                /*  
                * If the tag was accidentally removed from Tag Manager,  
                * fall through to AudioListener / Camera instead of breaking.  
                */
            }

            if (playerObject != null)
            {
                listenerTarget = playerObject.transform;
                return;
            }
        }

        // AUDIO LISTENER FALLBACK
        AudioListener listener = FindAnyObjectByType<AudioListener>();

        if (listener != null)
        {
            listenerTarget = listener.transform;
            return;
        }

        // MAIN CAMERA FALLBACK
        if (Camera.main != null)
            listenerTarget = Camera.main.transform;
    }


    // MANUAL TARGET
    public void SetListenerTarget(Transform newTarget)
    {
        listenerTarget = newTarget;

        if (snapVolumeOnStart)
            ApplyVolumeImmediately();
    }


    public void ClearListenerTarget()
    {
        listenerTarget = null;

        if (autoFindListener)
            ResolveListener();
    }


    // GIZMOS
    private void OnDrawGizmosSelected()
    {
        if (!drawHearingGizmos)
            return;

        Gizmos.DrawWireSphere(transform.position, fullVolumeDistance);
        Gizmos.DrawWireSphere(transform.position, maxHearingDistance);
    }


    // VALIDATION
    private void OnValidate()
    {
        fullVolumeDistance = Mathf.Max(0f, fullVolumeDistance);
        maxHearingDistance = Mathf.Max(fullVolumeDistance + 0.01f, maxHearingDistance);
        maximumVolume = Mathf.Clamp01(maximumVolume);
        fadeInDuration = Mathf.Max(0f, fadeInDuration);
        fadeOutDuration = Mathf.Max(0f, fadeOutDuration);
        if (string.IsNullOrWhiteSpace(playerTag))
            playerTag = "Player";

        if (volumeCurve == null || volumeCurve.length == 0)
            volumeCurve = AnimationCurve.Linear(0f, 1f, 1f, 0f);
    }
}
