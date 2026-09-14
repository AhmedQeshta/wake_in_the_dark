using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

[RequireComponent(typeof(PlayableDirector))]
public class StoryTimelineOnce : MonoBehaviour
{
    // REFERENCES
    [Header("References")]
    [SerializeField] private PlayableDirector director;


    // STORY ID
    [Header("Play Once")]

    [Tooltip("Unique save ID for this story Timeline. For Level_01 use: Level_01_Story")]
    [SerializeField] private string storyId = "Level_01_Story";


    [Tooltip("ON = once the story starts, it stays completed after death/reload and after closing/reopening the game. OFF = once per app session.")]
    [SerializeField] private bool rememberAcrossGameSessions = true;

    [Tooltip("Mark the story as seen as soon as playback starts. Recommended ON so dying during the cinematic does not replay it.")]
    [SerializeField] private bool markSeenWhenPlaybackStarts = true;


    // START CONDITION
    [Header("Start Condition")]

    [Tooltip("Wait until the Level Video Intro/menu flow is finished and gameplay has actually started.")]
    [SerializeField] private bool waitForGameplayReady = true;


    [Tooltip("Extra unscaled delay after gameplay becomes ready.")]
    [SerializeField, Min(0f)] private float startDelay = 0f;


    // DEBUG
    [Header("Debug")]
    [SerializeField] private bool debugLogs = false;


    // SAVE

    private const string StoryKeyPrefix = "WakeInTheDark.StoryTimelineSeen.";


    private static readonly HashSet<string> SessionSeenStories = new HashSet<string>();


    /*
     * Active StoryTimelineOnce objects are tracked so a global reset
     * can re-arm a Story Timeline even when Level_01 is already loaded
     * behind Bootstrap and the component's Unity Start() already ran.
     */
    private static readonly HashSet<StoryTimelineOnce> ActiveInstances = new HashSet<StoryTimelineOnce>();


    private Coroutine storyStartRoutine;
    private bool unityStartHasRun;


    // AWAKE

    private void Awake()
    {
        if (director == null)
            director = GetComponent<PlayableDirector>();


        if (director != null)
            director.playOnAwake = false;
    }


    // ENABLE / START
    private void OnEnable()
    {
        ActiveInstances.Add(this);

        if (unityStartHasRun)
            BeginStoryStartRoutine();
    }


    private void Start()
    {
        unityStartHasRun = true;
        BeginStoryStartRoutine();
    }


    private void OnDisable()
    {
        ActiveInstances.Remove(this);

        if (storyStartRoutine != null)
        {
            StopCoroutine(storyStartRoutine);
            storyStartRoutine = null;
        }
    }


    private void BeginStoryStartRoutine()
    {
        if (!isActiveAndEnabled)
            return;

        if (storyStartRoutine != null)
            StopCoroutine(storyStartRoutine);

        storyStartRoutine = StartCoroutine(StoryStartRoutine());
    }


    private IEnumerator StoryStartRoutine()
    { /*  * Let Bootstrap / additive scene systems initialize first.  */
        yield return null;

        if (HasPlayed())
        {
            Log("Story already played. Timeline will stay stopped.");

            storyStartRoutine = null;
            yield break;
        }

        if (waitForGameplayReady)
        {
            while (!IsGameplayReady())
            {
                yield return null;

                if (!isActiveAndEnabled)
                {
                    storyStartRoutine = null;
                    yield break;
                }
            }
        }

        if (startDelay > 0f)
        {
            float remaining = startDelay;

            while (remaining > 0f)
            {
                remaining -= Time.unscaledDeltaTime;
                yield return null;
            }
        }

        storyStartRoutine = null;

        PlayStory();
    }


    // READY CHECK

    private bool IsGameplayReady()
    {
        /*  
        * Never start behind the Level Video Intro. 
        */
        if (LevelVideoIntroManager.Instance != null && LevelVideoIntroManager.Instance.IsBusy)
            return false;

        /*  
        * AudioManager only enables normal gameplay music when  
        * gameplay is officially active. 
         */
        if (AudioManager.Instance != null)
            return AudioManager.Instance.GameplayMusicEnabled;

        /*  
        * Fallback:  
        * Bootstrap menus normally use Time.timeScale = 0.  
        */
        return Time.timeScale > 0.0001f;
    }


    // PLAY

    public void PlayStory()
    {
        if (director == null || HasPlayed())
            return;

        if (markSeenWhenPlaybackStarts)
            MarkPlayed();

        director.time = 0d;
        director.Evaluate();
        director.Play();

        Log("Story Timeline started.");

        if (!markSeenWhenPlaybackStarts)
            StartCoroutine(MarkWhenFinishedRoutine());
    }


    private IEnumerator MarkWhenFinishedRoutine()
    {
        while (director != null && director.state == PlayState.Playing)
            yield return null;

        MarkPlayed();
    }


    // PLAYED STATE

    public bool HasPlayed()
    {
        string key = GetStoryKey();

        if (rememberAcrossGameSessions)
            return PlayerPrefs.GetInt(key, 0) == 1;

        return SessionSeenStories.Contains(key);
    }


    private void MarkPlayed()
    {
        string key = GetStoryKey();
        SessionSeenStories.Add(key);

        if (!rememberAcrossGameSessions)
            return;

        PlayerPrefs.SetInt(key, 1);
        PlayerPrefs.Save();
    }


    private string GetStoryKey()
    {
        string safeId = string.IsNullOrWhiteSpace(storyId) ? gameObject.scene.name + "." + gameObject.name : storyId.Trim();

        return StoryKeyPrefix + safeId;
    }


    // RESET

    [ContextMenu("DEBUG - Reset This Story")]
    public void ResetStoryPlayed()
    {
        string key = GetStoryKey();

        SessionSeenStories.Remove(key);

        PlayerPrefs.DeleteKey(key);

        PlayerPrefs.Save();

        RearmAfterReset();

        Log("Story played state reset.");
    }



    public static void ResetAllStoryRuntimeState()
    {
        SessionSeenStories.Clear();

        StoryTimelineOnce[] instances = new StoryTimelineOnce[ActiveInstances.Count];

        ActiveInstances.CopyTo(instances);

        foreach (StoryTimelineOnce instance in instances)
        {
            if (instance == null)
                continue;

            instance.RearmAfterReset();
        }
    }


    private void RearmAfterReset()
    {
        /*  
        * Stop any old waiting/finish coroutine and reset the Timeline.  
        */
        StopAllCoroutines();
        storyStartRoutine = null;

        if (director != null)
        {
            director.Stop();
            director.time = 0d;
            director.Evaluate();
        }

        /*  
        * If Unity Start() already happened, start waiting again.  
        * This is essential for Bootstrap because Level_01 can remain  
        * loaded while Reset Progress is pressed. 
         */
        if (unityStartHasRun && isActiveAndEnabled)
            BeginStoryStartRoutine();
    }


    // LOG

    private void Log(string message)
    {
        if (!debugLogs) return;

        Debug.Log("StoryTimelineOnce: " + message, this);
    }


    // DESTROY

    private void OnDestroy()
    {
        ActiveInstances.Remove(this);
    }


    // VALIDATE

    private void OnValidate()
    {
        startDelay = Mathf.Max(0f, startDelay);

        if (director == null)
            director = GetComponent<PlayableDirector>();

        if (string.IsNullOrWhiteSpace(storyId))
            storyId = "Level_01_Story";
    }
}
