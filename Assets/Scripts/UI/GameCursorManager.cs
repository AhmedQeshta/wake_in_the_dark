using UnityEngine;

public class GameCursorManager : MonoBehaviour
{
    // ==================================================
    // INSTANCE
    // ==================================================

    public static GameCursorManager Instance
    {
        get;
        private set;
    }


    // ==================================================
    // CURSOR
    // ==================================================

    [Header("Custom Cursor")]

    [Tooltip(
        "Transparent PNG cursor texture. " +
        "32x32 is recommended for this pixel-art game."
    )]
    [SerializeField]
    private Texture2D cursorTexture;


    [Tooltip(
        "Click point inside the cursor texture, measured from its top-left corner. " +
        "For an arrow whose tip is top-left, leave this at (0, 0)."
    )]
    [SerializeField]
    private Vector2 hotSpot =
        Vector2.zero;


    [Tooltip(
        "Auto normally uses the hardware cursor when supported."
    )]
    [SerializeField]
    private CursorMode cursorMode =
        CursorMode.Auto;


    // ==================================================
    // START STATE
    // ==================================================

    [Header("Start State")]

    [Tooltip(
        "Bootstrap starts on the main menu, so the cursor should normally be visible."
    )]
    [SerializeField]
    private bool showCursorOnAwake =
        true;


    // ==================================================
    // STATE
    // ==================================================

    private bool cursorShouldBeVisible;


    // ==================================================
    // AWAKE
    // ==================================================

    private void Awake()
    {
        if (Instance != null &&
            Instance != this)
        {
            Debug.LogWarning(
                "Duplicate GameCursorManager found. Removing duplicate.",
                this
            );

            Destroy(gameObject);

            return;
        }


        Instance =
            this;


        /*
         * Bootstrap already stays loaded for the whole game,
         * so DontDestroyOnLoad is not required.
         */
        if (showCursorOnAwake)
        {
            ShowMenuCursor();
        }
        else
        {
            HideGameplayCursor();
        }
    }


    // ==================================================
    // SHOW MENU CURSOR
    // ==================================================

    public void ShowMenuCursor()
    {
        cursorShouldBeVisible =
            true;


        /*
         * This is a 2D platformer, so we do not need
         * FPS-style mouse locking.
         */
        Cursor.lockState =
            CursorLockMode.None;


        if (cursorTexture != null)
        {
            Cursor.SetCursor(
                cursorTexture,
                ClampHotSpot(hotSpot),
                cursorMode
            );
        }
        else
        {
            /*
             * No custom texture assigned:
             * use the operating system cursor.
             */
            Cursor.SetCursor(
                null,
                Vector2.zero,
                CursorMode.Auto
            );
        }


        Cursor.visible =
            true;
    }


    // ==================================================
    // HIDE DURING GAMEPLAY
    // ==================================================

    public void HideGameplayCursor()
    {
        cursorShouldBeVisible =
            false;


        /*
         * Do not lock it to the center.
         * We only hide it.
         *
         * That is cleaner for your 2D platformer,
         * and it does not interfere with UI when Pause opens.
         */
        Cursor.lockState =
            CursorLockMode.None;


        Cursor.visible =
            false;
    }


    // ==================================================
    // PUBLIC GENERIC STATE
    // ==================================================

    public void SetCursorVisible(
        bool visible)
    {
        if (visible)
        {
            ShowMenuCursor();
        }
        else
        {
            HideGameplayCursor();
        }
    }


    // ==================================================
    // RE-APPLY AFTER ALT-TAB / WINDOW FOCUS
    // ==================================================

    private void OnApplicationFocus(
        bool hasFocus)
    {
        if (!hasFocus)
            return;


        /*
         * Some desktop platforms can change cursor state
         * after the game loses focus.
         */
        if (cursorShouldBeVisible)
        {
            ShowMenuCursor();
        }
        else
        {
            HideGameplayCursor();
        }
    }


    // ==================================================
    // HOTSPOT VALIDATION
    // ==================================================

    private Vector2 ClampHotSpot(
        Vector2 value)
    {
        if (cursorTexture == null)
        {
            return Vector2.zero;
        }


        float maxX =
            Mathf.Max(
                0f,
                cursorTexture.width - 1f
            );


        float maxY =
            Mathf.Max(
                0f,
                cursorTexture.height - 1f
            );


        return new Vector2(
            Mathf.Clamp(
                value.x,
                0f,
                maxX
            ),
            Mathf.Clamp(
                value.y,
                0f,
                maxY
            )
        );
    }


    // ==================================================
    // CLEANUP
    // ==================================================

    private void OnDestroy()
    {
        if (Instance != this)
            return;


        Instance =
            null;


        /*
         * Restore a normal cursor when Bootstrap is destroyed
         * or when Play Mode stops.
         */
        Cursor.lockState =
            CursorLockMode.None;


        Cursor.SetCursor(
            null,
            Vector2.zero,
            CursorMode.Auto
        );


        Cursor.visible =
            true;
    }
}