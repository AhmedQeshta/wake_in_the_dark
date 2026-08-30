using System;
using UnityEngine;
using UnityEngine.Video;

[Serializable]
public class LevelVideoEntry
{
  [Tooltip("Exact Unity scene name, for example Level_02.")]
  public string sceneName;

  [Tooltip("Video shown before entering this level.")]
  public VideoClip videoClip;
}
