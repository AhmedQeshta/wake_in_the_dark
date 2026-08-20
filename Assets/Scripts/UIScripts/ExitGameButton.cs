using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

[RequireComponent(typeof(Button))]
public class ExitGameButton : MonoBehaviour
{
    // ==================================================
    // SETTINGS
    // ==================================================

    [Header("Exit Game")]

    [SerializeField, Min(0f)]
    private float exitDelay = 0f;


    // ==================================================
    // COMPONENTS
    // ==================================================

    private Button button;

    private bool exiting;


    // ==================================================
    // AWAKE
    // ==================================================

    private void Awake()
    {
        button =
            GetComponent<Button>();


        button.onClick.AddListener(
            ExitGame
        );
    }


    // ==================================================
    // EXIT GAME
    // ==================================================

    public void ExitGame()
    {
        if (exiting)
            return;


        exiting =
            true;


        if (exitDelay <= 0f)
        {
            QuitNow();

            return;
        }


        StartCoroutine(
            ExitAfterDelay()
        );
    }


    // ==================================================
    // DELAY
    // ==================================================

    private System.Collections.IEnumerator
        ExitAfterDelay()
    {
        yield return
            new WaitForSecondsRealtime(
                exitDelay
            );


        QuitNow();
    }


    // ==================================================
    // QUIT
    // ==================================================

    private void QuitNow()
    {
#if UNITY_EDITOR
        // When testing inside Unity Editor,
        // stop Play Mode.
        EditorApplication.isPlaying =
            false;
#else
                // Windows / Linux / macOS build
                Application.Quit();

#endif
    }


    // ==================================================
    // CLEANUP
    // ==================================================

    private void OnDestroy()
    {
        if (button == null)
            return;


        button.onClick.RemoveListener(
            ExitGame
        );
    }
}