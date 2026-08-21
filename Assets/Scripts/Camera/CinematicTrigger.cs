using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class CinematicTrigger : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Drag the GameObject holding your LevelCameraDirector here.")]
    [SerializeField] private LevelCameraDirector levelCameraDirector;

    [Header("Settings")]
    [Tooltip("Should this cinematic only play the first time the player enters?")]
    [SerializeField] private bool playOnlyOnce = true;

    [Tooltip("The tag assigned to your Player GameObject.")]
    [SerializeField] private string playerTag = "Player";

    private bool hasTriggered = false;

    // Automatically check the 'Is Trigger' box when you add this script
    private void Reset()
    {
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            col.isTrigger = true;
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (playOnlyOnce && hasTriggered) return;

        // Check if the object entering the trigger is the Player
        if (collision.CompareTag(playerTag))
        {
            if (levelCameraDirector != null)
            {
                levelCameraDirector.PlayIntroTimeline();
                hasTriggered = true;

                Debug.Log("Cinematic triggered by: " + collision.gameObject.name, this);
            }
            else
            {
                Debug.LogWarning("CinematicTrigger: LevelCameraDirector is not assigned!", this);
            }
        }
    }
}