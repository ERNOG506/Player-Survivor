using UnityEngine;

public class GameSfxPlayer : MonoBehaviour
{
    public static GameSfxPlayer Instance { get; private set; }

    private AudioSource audioSource;
    private AudioClip enemyExplosion;
    private AudioClip playerExplosion;
    private AudioClip powerUpClick;

    public static GameSfxPlayer EnsureExists()
    {
        if (Instance != null)
        {
            return Instance;
        }

        GameSfxPlayer existing = FindFirstObjectByType<GameSfxPlayer>();
        if (existing != null)
        {
            Instance = existing;
            existing.Initialize();
            return existing;
        }

        GameObject sfxObject = new GameObject("Game SFX Player");
        Instance = sfxObject.AddComponent<GameSfxPlayer>();
        Instance.Initialize();
        return Instance;
    }

    public static void PlayEnemyExplosion(Vector3 position)
    {
        EnsureExists().PlayAt(position, EnsureExists().enemyExplosion, 0.72f);
    }

    public static void PlayPlayerExplosion(Vector3 position)
    {
        EnsureExists().PlayAt(position, EnsureExists().playerExplosion, 0.95f);
    }

    public static void PlayPowerUpClick()
    {
        EnsureExists().PlayAt(Vector3.zero, EnsureExists().powerUpClick, 0.65f);
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
        audioSource.spatialBlend = 0f;
        audioSource.ignoreListenerPause = true;

        enemyExplosion = Resources.Load<AudioClip>("SFX/EnemyExplosion_Alt");
        playerExplosion = Resources.Load<AudioClip>("SFX/PlayerExplosion_Alt");
        powerUpClick = Resources.Load<AudioClip>("SFX/PowerUpClick_Alt");
    }

    private void PlayAt(Vector3 position, AudioClip clip, float volume)
    {
        if (!Application.isPlaying || clip == null)
        {
            return;
        }

        audioSource.transform.position = position;
        audioSource.PlayOneShot(clip, Mathf.Clamp01(volume));
    }
}
