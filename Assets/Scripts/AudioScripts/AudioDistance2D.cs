using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class AudioDistance2D : MonoBehaviour
{
    // ==================================================
    // DISTANCE
    // ==================================================

    [Header("2D Hearing Distance")]

    [Tooltip("Inside this distance, the sound plays at full volume.")]
    [SerializeField, Min(0f)] private float fullVolumeDistance = 2f;


    [Tooltip("At or beyond this distance, the sound is silent.")]
    [SerializeField, Min(0.01f)] private float maxHearingDistance = 10f;


    // ==================================================
    // VOLUME
    // ==================================================

    [Header("Volume")]

    [Tooltip("Maximum AudioSource volume when the listener is close.")]
    [SerializeField, Range(0f, 1f)] private float maximumVolume = 1f;


    [Tooltip("Controls how volume fades between Full Volume Distance and Max Hearing Distance.")]
    [SerializeField] private AnimationCurve volumeCurve = AnimationCurve.Linear(0f, 1f, 1f, 0f);


    // ==================================================
    // LISTENER
    // ==================================================

    [Header("Listener")]

    [Tooltip("Usually leave empty. The script automatically uses the active AudioListener / Main Camera.")]
    [SerializeField] private Transform listenerTarget;


    [SerializeField] private bool autoFindListener = true;


    // ==================================================
    // COMPONENTS
    // ==================================================

    private AudioSource audioSource;


    // ==================================================
    // AWAKE
    // ==================================================

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();


        /*
         * We handle distance ourselves in X/Y,
         * so Unity's normal 3D spatial attenuation is disabled.
         */
        audioSource.spatialBlend = 0f;


        ResolveListener();

        UpdateVolume();
    }


    // ==================================================
    // UPDATE
    // ==================================================

    private void Update()
    {
        if (listenerTarget == null && autoFindListener)
            ResolveListener();


        UpdateVolume();
    }


    // ==================================================
    // VOLUME
    // ==================================================

    private void UpdateVolume()
    {
        if (audioSource == null || listenerTarget == null)
            return;

        Vector2 sourcePosition = new Vector2(transform.position.x, transform.position.y);
        Vector2 listenerPosition = new Vector2(listenerTarget.position.x, listenerTarget.position.y);

        float distance = Vector2.Distance(sourcePosition, listenerPosition);


        // ----------------------------------------------
        // FULL VOLUME
        // ----------------------------------------------

        if (distance <= fullVolumeDistance)
        {
            audioSource.volume = maximumVolume;
            return;
        }


        // ----------------------------------------------
        // SILENT
        // ----------------------------------------------

        if (distance >= maxHearingDistance)
        {
            audioSource.volume = 0f;
            return;
        }


        // ----------------------------------------------
        // FADE
        // ----------------------------------------------

        float normalizedDistance = Mathf.InverseLerp(fullVolumeDistance, maxHearingDistance, distance);
        float curveValue = volumeCurve != null && volumeCurve.length > 0 ? volumeCurve.Evaluate(normalizedDistance) : 1f - normalizedDistance;

        audioSource.volume = maximumVolume * Mathf.Clamp01(curveValue);
    }


    // ==================================================
    // FIND LISTENER
    // ==================================================

    private void ResolveListener()
    {
        if (listenerTarget != null)
            return;


        AudioListener listener = FindAnyObjectByType<AudioListener>();


        if (listener != null)
        {
            listenerTarget = listener.transform;
            return;
        }


        if (Camera.main != null)
            listenerTarget = Camera.main.transform;
    }


    // ==================================================
    // VALIDATION
    // ==================================================

    private void OnValidate()
    {
        fullVolumeDistance = Mathf.Max(0f, fullVolumeDistance);


        maxHearingDistance = Mathf.Max(fullVolumeDistance + 0.01f, maxHearingDistance);


        maximumVolume = Mathf.Clamp01(maximumVolume);


        if (volumeCurve == null || volumeCurve.length == 0)
            volumeCurve = AnimationCurve.Linear(0f, 1f, 1f, 0f);
    }
}