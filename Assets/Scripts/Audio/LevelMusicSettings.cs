using UnityEngine;

[DisallowMultipleComponent]
public class LevelMusicSettings : MonoBehaviour
{
    // ==================================================
    // MUSIC
    // ==================================================

    [Header("Level Music")]

    [SerializeField]
    private AudioClip musicClip;


    [SerializeField, Range(0f, 1f)]
    private float musicVolume = 0.6f;


    [SerializeField]
    private bool loop = true;


    // ==================================================
    // TRANSITION
    // ==================================================

    [Header("Transition")]

    [Tooltip(
        "Time used when changing from another level's music."
    )]
    [SerializeField, Min(0f)]
    private float crossfadeDuration = 0.8f;


    [Tooltip(
        "If enabled, Reset Level starts this music " +
        "again from the beginning."
    )]
    [SerializeField]
    private bool restartOnReload = false;


    // ==================================================
    // PUBLIC
    // ==================================================

    public AudioClip MusicClip =>
        musicClip;


    public float MusicVolume =>
        musicVolume;


    public bool Loop =>
        loop;


    public float CrossfadeDuration =>
        crossfadeDuration;


    public bool RestartOnReload =>
        restartOnReload;


    // ==================================================
    // VALIDATION
    // ==================================================

    private void OnValidate()
    {
        musicVolume =
            Mathf.Clamp01(
                musicVolume
            );


        crossfadeDuration =
            Mathf.Max(
                0f,
                crossfadeDuration
            );
    }
}