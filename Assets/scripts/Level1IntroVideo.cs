using UnityEngine;
using UnityEngine.Video;
using UnityEngine.Playables;

public class Level1IntroVideo : MonoBehaviour
{
    [Header("Intro Video")]
    [SerializeField] private VideoPlayer videoPlayer;
    [SerializeField] private GameObject videoCanvas;

    [Header("Level Timeline")]
    [SerializeField] private PlayableDirector levelTimeline;

    private bool introEnded = false;

    private void Start()
    {
        // Make sure Timeline does not run before the intro ends
        if (levelTimeline != null)
        {
            levelTimeline.Stop();
            levelTimeline.time = 0;
        }

        // Show video UI
        if (videoCanvas != null)
        {
            videoCanvas.SetActive(true);
        }

        // Listen for natural end of the video
        videoPlayer.loopPointReached += OnVideoFinished;

        // Start intro video
        videoPlayer.Play();
    }

    private void Update()
    {
        if (introEnded)
            return;

        // Enter skips the intro video
        if (Input.GetKeyDown(KeyCode.Return) ||
            Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            EndIntro();
        }
    }

    private void OnVideoFinished(VideoPlayer vp)
    {
        EndIntro();
    }

    private void EndIntro()
    {
        if (introEnded)
            return;

        introEnded = true;

        // Stop video
        if (videoPlayer != null)
        {
            videoPlayer.Stop();
        }

        // Hide video UI
        if (videoCanvas != null)
        {
            videoCanvas.SetActive(false);
        }

        // Start your existing Timeline from the beginning
        if (levelTimeline != null)
        {
            levelTimeline.time = 0;
            levelTimeline.Play();
        }
    }

    private void OnDestroy()
    {
        if (videoPlayer != null)
        {
            videoPlayer.loopPointReached -= OnVideoFinished;
        }
    }
}