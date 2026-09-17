using System.Collections;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Video;
using TMPro;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

using DegreesOfFreedom.EulerPath;
using RegionDivision;


public class Level3NinthFloorSequence :
    MonoBehaviour,
    IEulerPuzzleKeepRunning
{
    // =========================================================
    // EULER PUZZLE
    // =========================================================

    [Header("Euler Puzzle")]
    [SerializeField]
    private EulerPathPuzzleController eulerPuzzle;


    // =========================================================
    // REGION DIVISION PUZZLE
    // =========================================================

    [Header("Region Division Puzzle")]
    [SerializeField]
    private RegionDivisionPuzzleController regionDivisionPuzzle;


    // =========================================================
    // 9TH FLOOR NOTIFICATION
    // =========================================================

    [Header("9th Floor Notification")]

    [SerializeField]
    private GameObject objectiveNotification;

    [SerializeField]
    private AudioSource notificationAudio;

    [SerializeField]
    private float notificationDuration = 5f;


    // =========================================================
    // EULER CLUE SCREEN
    // =========================================================

    [Header("Euler Clue Screen")]

    [SerializeField]
    private GameObject clueScreen;


    // =========================================================
    // DARK SCREEN
    // =========================================================

    [Header("Dark Screen")]

    [SerializeField]
    private GameObject darkScreen;

    [SerializeField]
    private CanvasGroup darkScreenCanvasGroup;

    [SerializeField]
    private TMP_Text darkScreenText;


    // =========================================================
    // DSW CINEMATIC
    // =========================================================

    [Header("DSW Cinematic")]

    [SerializeField]
    private PlayableDirector dswDirector;

    [SerializeField]
    private GameObject dswCinematicCamera;

    [SerializeField]
    private AudioSource dswCinematicMusic;

    [SerializeField]
    private float cinematicDuration = 9f;

    [SerializeField]
    private float cinematicAudioFadeOutDuration = 2f;

    [SerializeField]
    private float postCinematicDelay = 2f;


    // =========================================================
    // DSW TEXT CONVERSATION
    // =========================================================

    [Header("DSW Text Conversation")]

    [SerializeField]
    private GameObject conversationPanel;

    [SerializeField]
    private CanvasGroup conversationCanvasGroup;

    [SerializeField]
    private TMP_Text conversationSpeakerText;

    [SerializeField]
    private TMP_Text conversationBodyText;

    [Tooltip("Continuous 1-2 second keyboard typing clip.")]
    [SerializeField]
    private AudioSource typingAudio;

    [Tooltip("Seconds between characters.")]
    [SerializeField]
    private float typingCharacterDelay = 0.035f;


    // =========================================================
    // SEQUENCE VIDEOS
    // =========================================================

    [Header("Sequence Videos")]

    [SerializeField]
    private GameObject sequenceVideoScreen;

    [SerializeField]
    private VideoPlayer sequenceVideoPlayer;

    [Tooltip("Detective tries to access protected CCTV/file.")]
    [SerializeField]
    private VideoClip protectedFileAttemptClip;

    [Tooltip("Old final conversation video reference. Kept so your existing Inspector setup is not broken.")]
    [SerializeField]
    private VideoClip finalRafiConversationClip;


    // =========================================================
    // RAFI EVIDENCE
    // =========================================================

    [Header("Rafi Evidence")]

    [SerializeField]
    private GameObject rafiEvidencePanel;


    // =========================================================
    // NEW FINAL ENDING
    // =========================================================

    [Header("Final Ending")]

    [Tooltip("Drag the FinalEndingController GameObject here.")]
    [SerializeField]
    private Level3FinalEndingController finalEndingController;


    // =========================================================
    // REGION DIVISION FAILURE
    // =========================================================

    [Header("Region Division Failure")]

    [Tooltip("Failure image shown after Forfeit.")]
    [SerializeField]
    private GameObject regionDivisionFailurePanel;

    [Tooltip("Failure sound played once when Forfeit is pressed.")]
    [SerializeField]
    private AudioSource regionDivisionFailureAudio;


    // =========================================================
    // PLAYER
    // =========================================================

    [Header("Player Control")]

    [Tooltip("Drag Detective3DMovement here.")]
    [SerializeField]
    private MonoBehaviour playerMovement;


    // =========================================================
    // FADE
    // =========================================================

    [Header("Fade Settings")]

    [SerializeField]
    private float fadeDuration = 0.5f;


    // =========================================================
    // INTERNAL STATE
    // =========================================================

    private bool reachedNinthFloor = false;
    private bool notificationPlayed = false;

    private bool eulerSolved = false;
    private bool regionDivisionSolved = false;

    private bool sequenceRunning = false;

    private bool waitingForEulerClueEnter = false;
    private bool waitingForConversationEnter = false;
    private bool waitingForEvidenceEnter = false;
    private bool waitingForRegionRetryEnter = false;

    private int conversationStep = 0;

    private Coroutine typingCoroutine;
    private bool isTypingConversation = false;
    private string currentConversationFullText = "";

    private bool videoFinished = false;


    // =========================================================
    // AWAKE
    // =========================================================

    private void Awake()
    {
        if (objectiveNotification != null)
            objectiveNotification.SetActive(false);

        if (clueScreen != null)
            clueScreen.SetActive(false);

        if (darkScreen != null)
            darkScreen.SetActive(false);

        if (darkScreenCanvasGroup != null)
            darkScreenCanvasGroup.alpha = 0f;

        if (conversationPanel != null)
            conversationPanel.SetActive(false);

        if (conversationCanvasGroup != null)
            conversationCanvasGroup.alpha = 0f;

        if (sequenceVideoScreen != null)
            sequenceVideoScreen.SetActive(false);

        if (rafiEvidencePanel != null)
            rafiEvidencePanel.SetActive(false);

        if (regionDivisionFailurePanel != null)
            regionDivisionFailurePanel.SetActive(false);

        if (dswCinematicCamera != null)
            dswCinematicCamera.SetActive(false);


        // -------------------------
        // AUDIO INITIAL STATE
        // -------------------------

        if (notificationAudio != null)
            notificationAudio.Stop();

        if (dswCinematicMusic != null)
            dswCinematicMusic.Stop();

        if (typingAudio != null)
        {
            typingAudio.Stop();
            typingAudio.loop = true;
        }

        if (regionDivisionFailureAudio != null)
        {
            regionDivisionFailureAudio.Stop();
            regionDivisionFailureAudio.loop = false;
        }


        // Disable Region Division debug launcher.
        if (regionDivisionPuzzle != null)
            regionDivisionPuzzle.ShowDebugHooks = false;
    }


    // =========================================================
    // EVENTS
    // =========================================================

    private void OnEnable()
    {
        if (eulerPuzzle != null)
        {
            eulerPuzzle.OnPuzzleSolved +=
                HandleEulerSolved;
        }

        if (regionDivisionPuzzle != null)
        {
            regionDivisionPuzzle.OnPuzzleSolved +=
                HandleRegionDivisionSolved;

            regionDivisionPuzzle.OnPuzzleCancelled +=
                HandleRegionDivisionCancelled;
        }
    }


    private void OnDisable()
    {
        StopTypingAudio();

        if (eulerPuzzle != null)
        {
            eulerPuzzle.OnPuzzleSolved -=
                HandleEulerSolved;
        }

        if (regionDivisionPuzzle != null)
        {
            regionDivisionPuzzle.OnPuzzleSolved -=
                HandleRegionDivisionSolved;

            regionDivisionPuzzle.OnPuzzleCancelled -=
                HandleRegionDivisionCancelled;
        }
    }


    // =========================================================
    // UPDATE
    // =========================================================

    private void Update()
    {
        // Euler owns input.
        if (EulerPathPuzzleController.IsMainGameInputLocked)
            return;

        // Region Division owns input.
        if (RegionDivisionInputLock.IsLocked)
            return;


        // =====================================================
        // FAILURE IMAGE -> ENTER TO RETRY REGION DIVISION
        // =====================================================

        if (waitingForRegionRetryEnter &&
            EnterPressed())
        {
            waitingForRegionRetryEnter = false;


            // Stop failure sound if it has not finished yet.
            if (regionDivisionFailureAudio != null)
            {
                regionDivisionFailureAudio.Stop();
            }


            if (regionDivisionFailurePanel != null)
            {
                regionDivisionFailurePanel.SetActive(false);
            }


            if (regionDivisionPuzzle != null)
            {
                regionDivisionPuzzle.StartPuzzle();
            }
            else
            {
                Debug.LogError(
                    "Region Division puzzle is not assigned."
                );
            }

            return;
        }


        // =====================================================
        // EULER CLUE -> ENTER
        // =====================================================

        if (waitingForEulerClueEnter &&
            EnterPressed())
        {
            waitingForEulerClueEnter = false;

            if (!sequenceRunning)
            {
                StartCoroutine(
                    StartDSWSequence()
                );
            }

            return;
        }


        // =====================================================
        // TEXT CONVERSATION -> ENTER
        // =====================================================

        if (waitingForConversationEnter &&
            EnterPressed())
        {
            AdvanceConversation();
            return;
        }


        // =====================================================
        // RAFI EVIDENCE -> ENTER
        //
        // UPDATED:
        // Previously this directly played the final video.
        // Now it opens the Mastermind Question through the
        // Level3FinalEndingController.
        // =====================================================

        if (waitingForEvidenceEnter &&
            EnterPressed())
        {
            waitingForEvidenceEnter = false;


            // Hide the Rafi clue image.
            if (rafiEvidencePanel != null)
            {
                rafiEvidencePanel.SetActive(false);
            }


            // Start the new final accusation sequence.
            if (finalEndingController != null)
            {
                finalEndingController.BeginEnding();
            }
            else
            {
                Debug.LogError(
                    "Level3NinthFloorSequence: " +
                    "Final Ending Controller is not assigned."
                );
            }


            return;
        }
    }


    // =========================================================
    // NINTH FLOOR ARRIVAL
    // =========================================================

    public void NinthFloorReached()
    {
        if (reachedNinthFloor)
            return;

        reachedNinthFloor = true;

        StartCoroutine(
            ShowObjectiveNotification()
        );
    }


    private IEnumerator ShowObjectiveNotification()
    {
        if (objectiveNotification != null)
        {
            objectiveNotification.SetActive(true);
        }

        if (!notificationPlayed)
        {
            notificationPlayed = true;

            if (notificationAudio != null)
            {
                notificationAudio.Stop();
                notificationAudio.Play();
            }
        }

        yield return new WaitForSecondsRealtime(
            notificationDuration
        );

        if (objectiveNotification != null)
        {
            objectiveNotification.SetActive(false);
        }
    }


    // =========================================================
    // EULER
    // =========================================================

    public void TryOpenEulerPuzzle()
    {
        if (!reachedNinthFloor)
            return;

        if (eulerSolved)
            return;

        if (sequenceRunning)
            return;

        if (eulerPuzzle == null)
        {
            Debug.LogError(
                "Euler puzzle is not assigned."
            );

            return;
        }

        if (!eulerPuzzle.IsOpen)
        {
            eulerPuzzle.StartPuzzle();
        }
    }


    private void HandleEulerSolved()
    {
        if (eulerSolved)
            return;

        eulerSolved = true;

        if (playerMovement != null)
        {
            playerMovement.enabled = false;
        }

        if (clueScreen != null)
        {
            clueScreen.SetActive(true);
        }

        waitingForEulerClueEnter = true;
    }


    // =========================================================
    // DSW STORY SEQUENCE
    // =========================================================

    private IEnumerator StartDSWSequence()
    {
        sequenceRunning = true;

        if (clueScreen != null)
        {
            clueScreen.SetActive(false);
        }


        while (
            EulerPathPuzzleController
                .IsMainGameInputLocked
        )
        {
            yield return null;
        }


        if (playerMovement != null)
        {
            playerMovement.enabled = false;
        }


        // =====================================================
        // APPEARANCE TITLE
        // =====================================================

        yield return FadeToBlack(
            "Appearance of DSW sir"
        );

        yield return new WaitForSecondsRealtime(
            3f
        );

        yield return FadeFromBlack();


        // =====================================================
        // DSW CINEMATIC
        // =====================================================

        if (dswCinematicCamera != null)
        {
            dswCinematicCamera.SetActive(true);
        }

        if (dswCinematicMusic != null)
        {
            dswCinematicMusic.Stop();
            dswCinematicMusic.Play();
        }

        if (dswDirector != null)
        {
            dswDirector.time = 0;
            dswDirector.Play();
        }


        float safeFadeDuration =
            Mathf.Clamp(
                cinematicAudioFadeOutDuration,
                0f,
                cinematicDuration
            );


        float normalDuration =
            Mathf.Max(
                0f,
                cinematicDuration -
                safeFadeDuration
            );


        if (normalDuration > 0f)
        {
            yield return new WaitForSecondsRealtime(
                normalDuration
            );
        }


        if (
            safeFadeDuration > 0f &&
            dswCinematicMusic != null
        )
        {
            yield return StartCoroutine(
                FadeOutAudio(
                    dswCinematicMusic,
                    safeFadeDuration
                )
            );
        }
        else if (safeFadeDuration > 0f)
        {
            yield return new WaitForSecondsRealtime(
                safeFadeDuration
            );
        }


        if (dswDirector != null)
        {
            dswDirector.Stop();
        }


        if (
            dswCinematicMusic != null &&
            dswCinematicMusic.isPlaying
        )
        {
            dswCinematicMusic.Stop();
        }


        if (dswCinematicCamera != null)
        {
            dswCinematicCamera.SetActive(false);
        }


        // =====================================================
        // 2 SECOND GAP
        // =====================================================

        yield return new WaitForSecondsRealtime(
            postCinematicDelay
        );


        // =====================================================
        // START TEXT CONVERSATION
        // =====================================================

        yield return StartCoroutine(
            ShowConversationPanel()
        );


        conversationStep = 0;

        ShowConversationLine();

        waitingForConversationEnter = true;
    }


    // =========================================================
    // CONVERSATION
    // =========================================================

    private void ShowConversationLine()
    {
        StopTypingAudio();


        if (
            conversationSpeakerText == null ||
            conversationBodyText == null
        )
        {
            return;
        }


        string speaker = "";
        string line = "";


        switch (conversationStep)
        {
            case 0:

                speaker = "DSW SIR";

                line =
                    "I brought a video clip from the CCTV footage. " +
                    "It shows that no person appeared on the " +
                    "9th floor that night.";

                break;


            case 1:

                speaker = "DETECTIVE";

                line =
                    "It seems that the video clip is broken or modified. " +
                    "May I get access to the bin folder or other files?";

                break;


            case 2:

                speaker = "DSW SIR";

                line =
                    "No, not directly. All other files are protected. " +
                    "You may try.";

                break;
        }


        conversationSpeakerText.text = speaker;


        if (typingCoroutine != null)
        {
            StopCoroutine(
                typingCoroutine
            );

            typingCoroutine = null;
        }


        typingCoroutine =
            StartCoroutine(
                TypeConversationText(
                    line
                )
            );
    }


    private IEnumerator TypeConversationText(
        string fullText)
    {
        isTypingConversation = true;

        currentConversationFullText =
            fullText;

        conversationBodyText.text = "";


        // Start continuous typing audio.
        if (
            typingAudio != null &&
            typingAudio.clip != null
        )
        {
            typingAudio.Stop();

            typingAudio.loop = true;

            typingAudio.time = 0f;

            typingAudio.Play();
        }


        foreach (char character in fullText)
        {
            conversationBodyText.text +=
                character;

            yield return new WaitForSecondsRealtime(
                typingCharacterDelay
            );
        }


        isTypingConversation = false;

        typingCoroutine = null;


        // Stop immediately when sentence finishes.
        StopTypingAudio();
    }


    private void StopTypingAudio()
    {
        if (typingAudio == null)
            return;

        typingAudio.Stop();

        if (typingAudio.clip != null)
        {
            typingAudio.time = 0f;
        }
    }


    private void AdvanceConversation()
    {
        // =====================================================
        // ENTER WHILE TYPING
        // =====================================================

        if (isTypingConversation)
        {
            if (typingCoroutine != null)
            {
                StopCoroutine(
                    typingCoroutine
                );

                typingCoroutine = null;
            }

            if (conversationBodyText != null)
            {
                conversationBodyText.text =
                    currentConversationFullText;
            }

            isTypingConversation = false;

            StopTypingAudio();

            // Same line remains.
            return;
        }


        // =====================================================
        // NEXT SPEAKER
        // =====================================================

        StopTypingAudio();

        conversationStep++;


        if (conversationStep <= 2)
        {
            ShowConversationLine();
            return;
        }


        // =====================================================
        // CONVERSATION COMPLETE
        // =====================================================

        waitingForConversationEnter = false;

        StopTypingAudio();

        StartCoroutine(
            EndConversationAndPlayProtectedVideo()
        );
    }


    // =========================================================
    // CONVERSATION PANEL FADES
    // =========================================================

    private IEnumerator ShowConversationPanel()
    {
        if (
            conversationPanel == null ||
            conversationCanvasGroup == null
        )
        {
            yield break;
        }


        conversationPanel.SetActive(true);

        conversationCanvasGroup.alpha = 0f;


        float elapsed = 0f;


        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;

            conversationCanvasGroup.alpha =
                Mathf.Clamp01(
                    elapsed / fadeDuration
                );

            yield return null;
        }


        conversationCanvasGroup.alpha = 1f;
    }


    private IEnumerator HideConversationPanel()
    {
        StopTypingAudio();


        if (
            conversationPanel == null ||
            conversationCanvasGroup == null
        )
        {
            yield break;
        }


        float elapsed = 0f;


        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;

            conversationCanvasGroup.alpha =
                1f -
                Mathf.Clamp01(
                    elapsed / fadeDuration
                );

            yield return null;
        }


        conversationCanvasGroup.alpha = 0f;

        conversationPanel.SetActive(false);
    }


    // =========================================================
    // PROTECTED FILE VIDEO
    // =========================================================

    private IEnumerator
        EndConversationAndPlayProtectedVideo()
    {
        StopTypingAudio();


        yield return StartCoroutine(
            HideConversationPanel()
        );


        yield return StartCoroutine(
            PlayVideo(
                protectedFileAttemptClip
            )
        );


        // Video finished -> open Region Division.
        if (regionDivisionPuzzle != null)
        {
            regionDivisionPuzzle.StartPuzzle();
        }
        else
        {
            Debug.LogError(
                "Region Division puzzle is not assigned."
            );
        }
    }


    // =========================================================
    // REGION DIVISION SOLVED
    // =========================================================

    private void HandleRegionDivisionSolved()
    {
        if (regionDivisionSolved)
            return;

        regionDivisionSolved = true;

        StartCoroutine(
            ShowRafiEvidenceAfterPuzzle()
        );
    }


    // =========================================================
    // REGION DIVISION FORFEIT
    // =========================================================

    private void HandleRegionDivisionCancelled()
    {
        if (regionDivisionSolved)
            return;

        StartCoroutine(
            ShowRegionDivisionFailure()
        );
    }


    private IEnumerator ShowRegionDivisionFailure()
    {
        // Let Region Division completely close.
        yield return null;


        while (
            RegionDivisionInputLock.IsLocked
        )
        {
            yield return null;
        }


        // =====================================================
        // SHOW FAILURE IMAGE
        // =====================================================

        if (regionDivisionFailurePanel != null)
        {
            regionDivisionFailurePanel.SetActive(true);
        }


        // =====================================================
        // PLAY FAILURE AUDIO ONCE
        // =====================================================

        if (regionDivisionFailureAudio != null)
        {
            regionDivisionFailureAudio.Stop();

            regionDivisionFailureAudio.loop = false;

            regionDivisionFailureAudio.time = 0f;

            regionDivisionFailureAudio.Play();
        }


        Cursor.lockState =
            CursorLockMode.None;

        Cursor.visible = true;


        waitingForRegionRetryEnter = true;
    }


    // =========================================================
    // RAFI EVIDENCE
    // =========================================================

    private IEnumerator ShowRafiEvidenceAfterPuzzle()
    {
        yield return null;


        while (
            RegionDivisionInputLock.IsLocked
        )
        {
            yield return null;
        }


        if (regionDivisionFailureAudio != null)
        {
            regionDivisionFailureAudio.Stop();
        }


        if (regionDivisionFailurePanel != null)
        {
            regionDivisionFailurePanel.SetActive(false);
        }


        if (rafiEvidencePanel != null)
        {
            rafiEvidencePanel.SetActive(true);
        }


        Cursor.lockState =
            CursorLockMode.None;

        Cursor.visible = true;


        waitingForEvidenceEnter = true;
    }


    // =========================================================
    // OLD FINAL CONVERSATION VIDEO
    //
    // NOTE:
    // This method is no longer called by the new ending flow.
    // I am intentionally keeping it here for now so we do not
    // unnecessarily remove working code/references while testing.
    // =========================================================

    private IEnumerator PlayFinalConversationVideo()
    {
        if (rafiEvidencePanel != null)
        {
            rafiEvidencePanel.SetActive(false);
        }


        yield return StartCoroutine(
            PlayVideo(
                finalRafiConversationClip
            )
        );


        sequenceRunning = false;


        if (playerMovement != null)
        {
            playerMovement.enabled = true;
        }


        Cursor.lockState =
            CursorLockMode.Locked;

        Cursor.visible =
            false;
    }


    // =========================================================
    // VIDEO PLAYER
    // =========================================================

    private IEnumerator PlayVideo(
        VideoClip clip)
    {
        if (sequenceVideoPlayer == null)
        {
            Debug.LogError(
                "Sequence VideoPlayer is not assigned."
            );

            yield break;
        }


        if (clip == null)
        {
            Debug.LogError(
                "Video clip is not assigned."
            );

            yield break;
        }


        if (sequenceVideoScreen != null)
        {
            sequenceVideoScreen.SetActive(true);
        }


        Cursor.lockState =
            CursorLockMode.None;

        Cursor.visible =
            true;


        videoFinished =
            false;


        sequenceVideoPlayer.Stop();

        sequenceVideoPlayer.clip =
            clip;


        sequenceVideoPlayer.loopPointReached +=
            HandleVideoFinished;


        sequenceVideoPlayer.Play();


        while (!videoFinished)
        {
            yield return null;
        }


        sequenceVideoPlayer.loopPointReached -=
            HandleVideoFinished;


        sequenceVideoPlayer.Stop();


        if (sequenceVideoScreen != null)
        {
            sequenceVideoScreen.SetActive(false);
        }
    }


    private void HandleVideoFinished(
        VideoPlayer source)
    {
        videoFinished =
            true;
    }


    // =========================================================
    // AUDIO FADE
    // =========================================================

    private IEnumerator FadeOutAudio(
        AudioSource source,
        float duration)
    {
        if (source == null)
            yield break;


        float originalVolume =
            source.volume;


        if (duration <= 0f)
        {
            source.Stop();

            source.volume =
                originalVolume;

            yield break;
        }


        float elapsed =
            0f;


        while (elapsed < duration)
        {
            elapsed +=
                Time.unscaledDeltaTime;


            float progress =
                Mathf.Clamp01(
                    elapsed / duration
                );


            source.volume =
                Mathf.Lerp(
                    originalVolume,
                    0f,
                    progress
                );


            yield return null;
        }


        source.volume =
            0f;


        source.Stop();


        source.volume =
            originalVolume;
    }


    // =========================================================
    // DARK SCREEN
    // =========================================================

    private IEnumerator FadeToBlack(
        string message)
    {
        if (
            darkScreen == null ||
            darkScreenCanvasGroup == null
        )
        {
            yield break;
        }


        darkScreen.SetActive(true);


        if (darkScreenText != null)
        {
            darkScreenText.text =
                message;
        }


        darkScreenCanvasGroup.alpha =
            0f;


        float elapsed =
            0f;


        while (elapsed < fadeDuration)
        {
            elapsed +=
                Time.unscaledDeltaTime;


            darkScreenCanvasGroup.alpha =
                Mathf.Clamp01(
                    elapsed / fadeDuration
                );


            yield return null;
        }


        darkScreenCanvasGroup.alpha =
            1f;
    }


    private IEnumerator FadeFromBlack()
    {
        if (
            darkScreen == null ||
            darkScreenCanvasGroup == null
        )
        {
            yield break;
        }


        float elapsed =
            0f;


        while (elapsed < fadeDuration)
        {
            elapsed +=
                Time.unscaledDeltaTime;


            darkScreenCanvasGroup.alpha =
                1f -
                Mathf.Clamp01(
                    elapsed / fadeDuration
                );


            yield return null;
        }


        darkScreenCanvasGroup.alpha =
            0f;


        darkScreen.SetActive(false);
    }


    // =========================================================
    // ENTER KEY
    // =========================================================

    private bool EnterPressed()
    {
#if ENABLE_INPUT_SYSTEM

        return
            Keyboard.current != null &&
            (
                Keyboard.current.enterKey
                    .wasPressedThisFrame
                ||
                Keyboard.current.numpadEnterKey
                    .wasPressedThisFrame
            );

#else

        return
            Input.GetKeyDown(
                KeyCode.Return
            )
            ||
            Input.GetKeyDown(
                KeyCode.KeypadEnter
            );

#endif
    }
}