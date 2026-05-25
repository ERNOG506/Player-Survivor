using UnityEngine;

public class GameMusicPlayer : MonoBehaviour
{
    public static GameMusicPlayer Instance { get; private set; }

    [Header("Music")]
    public AudioClip[] musicTracks;
    public AudioClip restartMenuTrack;
    public bool loadFromResources = true;
    public bool shuffle = true;
    public float volume = 0.42f;
    public float restartMenuVolume = 0.5f;
    public float fadeInSeconds = 1.2f;

    private AudioSource audioSource;
    private int currentTrackIndex = -1;
    private float targetVolume;
    private bool playingRestartMenu;

    public bool HasTracks => musicTracks != null && musicTracks.Length > 0;

    public static GameMusicPlayer EnsureExists()
    {
        if (Instance != null)
        {
            return Instance;
        }

        GameMusicPlayer existing = FindFirstObjectByType<GameMusicPlayer>();
        if (existing != null)
        {
            Instance = existing;
            existing.Initialize();
            return existing;
        }

        GameObject musicObject = new GameObject("Game Music Player");
        Instance = musicObject.AddComponent<GameMusicPlayer>();
        Instance.Initialize();
        return Instance;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        Initialize();
    }

    private void Update()
    {
        if (audioSource == null || !HasTracks)
        {
            return;
        }

        if (audioSource.volume < targetVolume)
        {
            float fadeSpeed = fadeInSeconds <= 0f ? targetVolume : targetVolume / fadeInSeconds;
            audioSource.volume = Mathf.Min(targetVolume, audioSource.volume + fadeSpeed * Time.unscaledDeltaTime);
        }

        if (!playingRestartMenu && !audioSource.isPlaying)
        {
            PlayNextTrack();
        }
    }

    public void ReloadTracks()
    {
        if (loadFromResources)
        {
            AudioClip[] loadedTracks = Resources.LoadAll<AudioClip>("Music");
            if (loadedTracks != null && loadedTracks.Length > 0)
            {
                musicTracks = PreferLoopTracks(loadedTracks);
            }

            AudioClip loadedRestartMenu = Resources.Load<AudioClip>("SFX/RestartMenu_Alt_Loop");
            if (loadedRestartMenu != null)
            {
                restartMenuTrack = loadedRestartMenu;
            }
        }
    }

    public void PlayGameplayMusic()
    {
        if (audioSource == null)
        {
            Initialize();
        }

        playingRestartMenu = false;
        if (audioSource == null || !HasTracks || !Application.isPlaying)
        {
            return;
        }

        if (!audioSource.isPlaying || audioSource.clip == restartMenuTrack)
        {
            PlayNextTrack();
        }
    }

    public void PlayRestartMenuMusic()
    {
        if (audioSource == null)
        {
            Initialize();
        }

        if (audioSource == null || restartMenuTrack == null || !Application.isPlaying)
        {
            return;
        }

        playingRestartMenu = true;
        targetVolume = Mathf.Clamp01(restartMenuVolume);
        audioSource.loop = true;
        if (audioSource.clip != restartMenuTrack)
        {
            audioSource.clip = restartMenuTrack;
            audioSource.volume = fadeInSeconds > 0f ? 0f : targetVolume;
            audioSource.Play();
        }
    }

    public void PlayNextTrack()
    {
        if (!HasTracks || audioSource == null)
        {
            return;
        }

        playingRestartMenu = false;
        int nextIndex = GetNextTrackIndex();
        AudioClip nextClip = musicTracks[nextIndex];
        if (nextClip == null)
        {
            return;
        }

        currentTrackIndex = nextIndex;
        targetVolume = Mathf.Clamp01(volume);
        audioSource.loop = nextClip.name.Contains("_Loop");
        audioSource.clip = nextClip;
        audioSource.volume = fadeInSeconds > 0f ? 0f : targetVolume;
        audioSource.Play();
    }

    public void SetVolume(float newVolume)
    {
        volume = Mathf.Clamp01(newVolume);
        targetVolume = volume;
        if (audioSource != null)
        {
            audioSource.volume = Mathf.Min(audioSource.volume, targetVolume);
        }
    }

    private void Initialize()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
        }

        if (Application.isPlaying)
        {
            DontDestroyOnLoad(gameObject);
        }

        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.spatialBlend = 0f;
        audioSource.ignoreListenerPause = true;

        ReloadTracks();
        if (Application.isPlaying && HasTracks && !audioSource.isPlaying)
        {
            PlayGameplayMusic();
        }
    }

    private int GetNextTrackIndex()
    {
        if (musicTracks.Length == 1)
        {
            return 0;
        }

        if (!shuffle)
        {
            return (currentTrackIndex + 1) % musicTracks.Length;
        }

        int nextIndex = Random.Range(0, musicTracks.Length);
        if (nextIndex == currentTrackIndex)
        {
            nextIndex = (nextIndex + 1) % musicTracks.Length;
        }

        return nextIndex;
    }

    private static AudioClip[] PreferLoopTracks(AudioClip[] loadedTracks)
    {
        int loopCount = 0;
        for (int i = 0; i < loadedTracks.Length; i++)
        {
            if (loadedTracks[i] != null && loadedTracks[i].name.Contains("_Loop"))
            {
                loopCount++;
            }
        }

        if (loopCount == 0)
        {
            return loadedTracks;
        }

        AudioClip[] loopTracks = new AudioClip[loopCount];
        int index = 0;
        for (int i = 0; i < loadedTracks.Length; i++)
        {
            if (loadedTracks[i] != null && loadedTracks[i].name.Contains("_Loop"))
            {
                loopTracks[index] = loadedTracks[i];
                index++;
            }
        }

        return loopTracks;
    }
}
