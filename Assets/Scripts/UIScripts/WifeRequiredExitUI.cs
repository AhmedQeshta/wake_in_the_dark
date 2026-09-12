using System.Collections;
using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(CanvasGroup))]
public class WifeRequiredExitUI : MonoBehaviour
{
  // INSTANCE
  public static WifeRequiredExitUI Instance { get; private set; }


  // REFERENCES
  [Header("References")]

  [Tooltip("Fullscreen RectTransform of WifeRequiredWarning.")]
  [SerializeField] private RectTransform warningRoot;


  [Tooltip("Arabic/English warning text.")]
  [SerializeField] private TMP_Text messageText;


  [Tooltip("Arrow UI RectTransform. At rotation 0 the arrow sprite " + "should preferably point RIGHT.")]
  [SerializeField] private RectTransform directionArrow;


  [Tooltip("Canvas that contains WifeRequiredWarning. " + "Leave empty to auto-find the parent Canvas.")]
  [SerializeField] private Canvas parentCanvas;


  [Tooltip("World camera used to locate Wife. " + "Leave empty to use Camera.main automatically.")]
  [SerializeField] private Camera worldCamera;



  // ARROW
  [Header("Direction Arrow")]
  [Tooltip("Distance from the edges of the HUD in UI pixels.")]
  [SerializeField, Min(0f)] private float screenEdgePadding = 70f;


  [Tooltip("Extra rotation applied to the arrow. " + "Use 0 if the source sprite points RIGHT. " + "Use -90 if the source sprite points UP.")]
  [SerializeField] private float arrowRotationOffset = 0f;


  [Tooltip("If Wife cannot be found, hide only the arrow but keep the text visible.")]
  [SerializeField] private bool hideArrowWhenTargetMissing = true;


  [Tooltip("Horizontal distance between the arrow and Wife's screen position. " + "Wife on right -> arrow sits just to her left. " + "Wife on left -> arrow sits just to her right.")]
  [SerializeField, Min(0f)] private float arrowDistanceFromWife = 80f;


  [Tooltip("Keep the arrow inside the HUD even when Wife is off-screen.")]
  [SerializeField] private bool clampArrowToScreen = true;


  // FADE
  [Header("Fade")]
  [SerializeField, Min(0f)] private float fadeInDuration = 0.15f;

  [SerializeField, Min(0f)] private float fadeOutDuration = 0.25f;


  // TARGET
  [Header("Target")]

  [Tooltip("Tag used to recover Wife automatically if the exit did not pass a Transform.")]
  [SerializeField] private string wifeTag = "wife";


  // STATE
  private CanvasGroup canvasGroup;
  private Transform wifeTarget;
  private Coroutine fadeRoutine;
  private bool visibleRequested;


  // AWAKE
  private void Awake()
  {
    if (Instance != null && Instance != this)
    {
      Destroy(gameObject);
      return;
    }

    Instance = this;


    canvasGroup = GetComponent<CanvasGroup>();


    ResolveReferences();

    canvasGroup.alpha = 0f;
    canvasGroup.interactable = false;
    canvasGroup.blocksRaycasts = false;

    if (directionArrow != null)
      directionArrow.gameObject.SetActive(false);

    if (messageText != null)
      messageText.gameObject.SetActive(false);
  }


  // UPDATE

  private void LateUpdate()
  {
    if (!visibleRequested)
      return;


    if (wifeTarget == null)
      wifeTarget = FindWifeTarget();


    UpdateDirectionArrow();
  }


  // SHOW

  public void ShowWarning(Transform target)
  {
    ResolveReferences();

    if (target != null)
      wifeTarget = target;
    else if (wifeTarget == null)
      wifeTarget = FindWifeTarget();


    if (messageText != null)
      messageText.gameObject.SetActive(true);


    visibleRequested = true;

    UpdateDirectionArrow();


    StartFade(1f, fadeInDuration);
  }


  // HIDE

  public void HideWarning()
  {
    visibleRequested = false;


    StartFade(0f, fadeOutDuration);
  }


  // ARROW

  private void UpdateDirectionArrow()
  {
    if (directionArrow == null || warningRoot == null)
      return;


    if (wifeTarget == null)
    {
      if (hideArrowWhenTargetMissing)
        directionArrow.gameObject.SetActive(false);
      return;
    }


    Camera cameraToUse = ResolveWorldCamera();


    if (cameraToUse == null)
    {
      directionArrow.gameObject.SetActive(false);
      return;
    }


    Vector3 screenPoint3D = cameraToUse.WorldToScreenPoint(wifeTarget.position);

    Vector2 screenPoint = new Vector2(screenPoint3D.x, screenPoint3D.y);


    /*
     * If target is behind the camera, mirror the point through the screen center so the direction remains meaningful.
     */
    if (screenPoint3D.z < 0f)
    {
      Vector2 screenCenter = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
      screenPoint = screenCenter - (screenPoint - screenCenter);
    }


    Camera canvasCamera = parentCanvas != null && parentCanvas.renderMode != RenderMode.ScreenSpaceOverlay ? parentCanvas.worldCamera : null;


    if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(warningRoot, screenPoint, canvasCamera, out Vector2 wifeLocalPosition))
    {
      directionArrow.gameObject.SetActive(false);
      return;
    }


    Rect rect = warningRoot.rect;


    /*
     * First get the Wife position that is safe to use inside the HUD.
     *
     * If Wife is visible, this is essentially her screen position.
     * If Wife is off-screen, this becomes the nearest point along
     * the HUD edge.
     */
    Vector2 targetPosition = wifeLocalPosition;


    if (clampArrowToScreen)
    {
      targetPosition.x = Mathf.Clamp(targetPosition.x, rect.xMin + screenEdgePadding, rect.xMax - screenEdgePadding);
      targetPosition.y = Mathf.Clamp(targetPosition.y, rect.yMin + screenEdgePadding, rect.yMax - screenEdgePadding);
    }


    /*
     * REQUIRED BEHAVIOR:
     * Wife on RIGHT side:
     *     Wife     [arrow ->] Wife
     *     Arrow is just to the LEFT of Wife.
     *
     * Wife on LEFT side:
     *     Wife [<- arrow]
     *     Arrow is just to the RIGHT of Wife.
     *
     * This is intentionally NOT the opposite SCREEN edge.
     * The arrow stays close to Wife.
     */
    bool wifeIsOnRight = wifeLocalPosition.x >= rect.center.x;


    Vector2 arrowPosition = targetPosition;


    arrowPosition.x += wifeIsOnRight ? -arrowDistanceFromWife : arrowDistanceFromWife;


    if (clampArrowToScreen)
    {
      arrowPosition.x = Mathf.Clamp(arrowPosition.x, rect.xMin + screenEdgePadding, rect.xMax - screenEdgePadding);

      arrowPosition.y = Mathf.Clamp(arrowPosition.y, rect.yMin + screenEdgePadding, rect.yMax - screenEdgePadding);
    }


    directionArrow.anchoredPosition = arrowPosition;


    /*
     * Rotation is calculated FROM the arrow TO Wife.
     * Your generated arrow points RIGHT at Z = 0, so:
     * Wife on right -> angle about 0 degrees.
     * Wife on left  -> angle about 180 degrees.
     */
    Vector2 pointDirection = targetPosition - arrowPosition;


    if (pointDirection.sqrMagnitude < 0.0001f)
      pointDirection = wifeIsOnRight ? Vector2.right : Vector2.left;


    float angle = Mathf.Atan2(pointDirection.y, pointDirection.x) * Mathf.Rad2Deg;


    directionArrow.localEulerAngles = new Vector3(0f, 0f, angle + arrowRotationOffset);


    if (!directionArrow.gameObject.activeSelf)
      directionArrow.gameObject.SetActive(true);
  }


  // FADE

  private void StartFade(float targetAlpha, float duration)
  {
    if (canvasGroup == null)
      return;


    if (fadeRoutine != null)
      StopCoroutine(fadeRoutine);


    fadeRoutine = StartCoroutine(FadeRoutine(targetAlpha, duration));
  }


  private IEnumerator FadeRoutine(float targetAlpha, float duration)
  {
    float startAlpha = canvasGroup.alpha;


    if (duration <= 0f)
    {
      canvasGroup.alpha = targetAlpha;
      fadeRoutine = null;
      yield break;
    }


    float elapsed = 0f;


    while (elapsed < duration)
    {
      elapsed += Time.unscaledDeltaTime;
      float t = Mathf.Clamp01(elapsed / duration);
      canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
      yield return null;
    }


    canvasGroup.alpha = targetAlpha;


    if (targetAlpha <= 0f && directionArrow != null)
      directionArrow.gameObject.SetActive(false);


    fadeRoutine = null;
  }


  // FIND WIFE

  private Transform FindWifeTarget()
  {
    if (string.IsNullOrWhiteSpace(wifeTag))
      return null;


    try
    {
      GameObject wife = GameObject.FindGameObjectWithTag(wifeTag);

      if (wife != null)
        return wife.transform;
    }
    catch (UnityException)
    {
      // Tag missing: simply leave arrow hidden.
    }


    return null;
  }


  // REFERENCES

  private void ResolveReferences()
  {
    if (warningRoot == null)
      warningRoot = transform as RectTransform;


    if (parentCanvas == null)
      parentCanvas = GetComponentInParent<Canvas>(true);
  }


  private Camera ResolveWorldCamera()
  {
    if (worldCamera != null && worldCamera.isActiveAndEnabled)
      return worldCamera;


    if (Camera.main != null)
    {
      worldCamera = Camera.main;
      return worldCamera;
    }


    worldCamera = FindAnyObjectByType<Camera>();


    return worldCamera;
  }


  // VALIDATE
  private void OnValidate()
  {
    screenEdgePadding = Mathf.Max(0f, screenEdgePadding);
    arrowDistanceFromWife = Mathf.Max(0f, arrowDistanceFromWife);
    fadeInDuration = Mathf.Max(0f, fadeInDuration);
    fadeOutDuration = Mathf.Max(0f, fadeOutDuration);


    if (string.IsNullOrWhiteSpace(wifeTag))
      wifeTag = "wife";


    if (warningRoot == null)
      warningRoot = transform as RectTransform;

    if (messageText != null)
      messageText.gameObject.SetActive(true);
  }


  // DESTROY
  private void OnDestroy()
  {
    if (Instance == this)
      Instance = null;
  }
}
