using UnityEngine;

public class PersistentSuccessMusic : MonoBehaviour
{
    public static PersistentSuccessMusic Instance { get; private set; }

    [SerializeField] private AudioSource musicSource;

    public static bool IsPlaying
    {
        get
        {
            return Instance != null &&
                   Instance.musicSource != null &&
                   Instance.musicSource.isPlaying;
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        DontDestroyOnLoad(gameObject);

        if (musicSource == null)
        {
            musicSource = GetComponent<AudioSource>();
        }
    }

    public void StartMusic()
    {
        if (musicSource == null)
            return;

        musicSource.loop = true;

        if (!musicSource.isPlaying)
        {
            musicSource.time = 0f;
            musicSource.Play();
        }
    }
}