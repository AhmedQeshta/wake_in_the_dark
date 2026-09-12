using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class MenuButtonSoundManager : MonoBehaviour
{
  [Header("Shared Button Sound")]
  [SerializeField] private AudioSource audioSource;
  [SerializeField] private AudioClip clickSound;
  [SerializeField, Range(0f, 1f)] private float clickVolume = 0.7f;

  [Header("Button Discovery")]
  [SerializeField] private bool autoFindChildButtons = true;
  [SerializeField] private Button[] buttons;

  [Header("Options")]
  [SerializeField] private bool requireInteractableButton = true;
  [SerializeField] private bool randomizePitch = false;
  [SerializeField, Range(0.5f, 1.5f)] private float minimumPitch = 0.98f;
  [SerializeField, Range(0.5f, 1.5f)] private float maximumPitch = 1.02f;

  private Button[] registeredButtons = System.Array.Empty<Button>();
  private UnityAction[] registeredActions = System.Array.Empty<UnityAction>();

  private void Awake()
  {
    ResolveAudioSource();
    RegisterButtons();
  }

  public void RegisterButtons()
  {
    UnregisterButtons();

    if (autoFindChildButtons)
      buttons = GetComponentsInChildren<Button>(true);

    if (buttons == null || buttons.Length == 0)
      return;

    registeredButtons = new Button[buttons.Length];
    registeredActions = new UnityAction[buttons.Length];

    for (int i = 0; i < buttons.Length; i++)
    {
      Button button = buttons[i];
      registeredButtons[i] = button;

      if (button == null)
        continue;

      UnityAction action = () => PlayClickSound(button);
      registeredActions[i] = action;
      button.onClick.AddListener(action);
    }
  }

  private void UnregisterButtons()
  {
    if (registeredButtons == null)
      return;

    for (int i = 0; i < registeredButtons.Length; i++)
    {
      Button button = registeredButtons[i];

      if (button == null)
        continue;

      if (registeredActions != null &&
          i < registeredActions.Length &&
          registeredActions[i] != null)
      {
        button.onClick.RemoveListener(registeredActions[i]);
      }
    }

    registeredButtons = System.Array.Empty<Button>();
    registeredActions = System.Array.Empty<UnityAction>();
  }

  private void PlayClickSound(Button button)
  {
    if (button == null)
      return;

    if (requireInteractableButton && !button.interactable)
      return;

    if (audioSource == null || clickSound == null)
      return;

    audioSource.pitch = randomizePitch
        ? Random.Range(minimumPitch, maximumPitch)
        : 1f;

    audioSource.PlayOneShot(clickSound, clickVolume);
  }

  private void ResolveAudioSource()
  {
    if (audioSource != null)
      return;

    audioSource = GetComponent<AudioSource>();

    if (audioSource == null)
    {
      Debug.LogWarning(
          "MenuButtonSoundManager: Assign the UI_Manager AudioSource in the Inspector.",
          this
      );
    }
  }

  public void RefreshButtons()
  {
    RegisterButtons();
  }

  public void SetClickSound(AudioClip newClickSound)
  {
    clickSound = newClickSound;
  }

  private void OnDestroy()
  {
    UnregisterButtons();
  }

  private void OnValidate()
  {
    clickVolume = Mathf.Clamp01(clickVolume);
    minimumPitch = Mathf.Clamp(minimumPitch, 0.5f, 1.5f);
    maximumPitch = Mathf.Clamp(maximumPitch, 0.5f, 1.5f);

    if (minimumPitch > maximumPitch)
    {
      float temp = minimumPitch;
      minimumPitch = maximumPitch;
      maximumPitch = temp;
    }

    if (audioSource == null)
      audioSource = GetComponent<AudioSource>();
  }
}
