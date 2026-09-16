using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Playables;

public class IntroSceneManager : MonoBehaviour
{
    [Header("Timeline")]
    public PlayableDirector timelineDirector;

    [Header("Settings")]
    public float levelLoadDelay = 2f;

    private bool isEndingIntro = false;

    void Start()
    {
        if (timelineDirector != null)
        {
            timelineDirector.stopped += OnTimelineFinished;
        }
    }

    void Update()
    {
        // Press ENTER at any time to skip intro
        if (Input.GetKeyDown(KeyCode.Return))
        {
            SkipIntro();
        }
    }

    void SkipIntro()
    {
        if (isEndingIntro)
            return;

        isEndingIntro = true;

        if (timelineDirector != null)
        {
            timelineDirector.Stop();
        }

        StartCoroutine(LoadLevelAfterDelay());
    }

    void OnTimelineFinished(PlayableDirector director)
    {
        // If Timeline finishes normally
        if (!isEndingIntro)
        {
            isEndingIntro = true;
            StartCoroutine(LoadLevelAfterDelay());
        }
    }

    IEnumerator LoadLevelAfterDelay()
    {
        yield return new WaitForSeconds(levelLoadDelay);

        SceneManager.LoadScene("SampleScene");
    }

    void OnDestroy()
    {
        if (timelineDirector != null)
        {
            timelineDirector.stopped -= OnTimelineFinished;
        }
    }
}