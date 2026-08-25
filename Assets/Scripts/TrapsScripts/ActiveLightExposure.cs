using UnityEngine;

public class ActiveLightExposure
{
  // ==================================================
  // STATE
  // ==================================================

  public DangerousLightZone Zone { get; }

  public float ExposureTime { get; private set; }


  // ==================================================
  // CONSTRUCTOR
  // ==================================================

  public ActiveLightExposure(DangerousLightZone zone)
  {
    Zone = zone;
    ExposureTime = 0f;
  }


  // ==================================================
  // PUBLIC VALUES
  // ==================================================

  public bool IsValid => Zone != null;


  public float RequiredDuration =>
      Zone != null
          ? Mathf.Max(0.1f, Zone.ExposureDuration)
          : 0.1f;


  public float NormalizedExposure => Mathf.Clamp01(ExposureTime / RequiredDuration);


  public bool IsComplete => ExposureTime >= RequiredDuration;


  // ==================================================
  // UPDATE EXPOSURE
  // ==================================================

  public void AddExposure(float deltaTime)
  {
    if (!IsValid)
      return;


    ExposureTime += Mathf.Max(0f, deltaTime);


    ExposureTime = Mathf.Min(ExposureTime, RequiredDuration);
  }


  // ==================================================
  // RESET
  // ==================================================

  public void ResetExposure()
  {
    ExposureTime = 0f;
  }
}