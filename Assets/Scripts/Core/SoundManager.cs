using UnityEngine;
using System.Collections;

/// <summary>
/// Quản lý âm thanh toàn cục (BGM và SFX) sử dụng mô hình Singleton.
/// </summary>
public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    [Header("Audio Sources")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource sfxSource;

    [Header("Volume Settings")]
    [Range(0f, 1f)]
    [SerializeField] private float musicVolume = 0.8f;
    [Range(0f, 1f)]
    [SerializeField] private float sfxVolume = 0.8f;

    [Header("Default UI Sounds")]
    [SerializeField] private AudioClip defaultClickSound;

    [Header("BGM Clips")]
    public AudioClip bgmMenu;
    public AudioClip[] bgmStoryPlaylist;
    public AudioClip bgmBattle;
    public AudioClip bgmVictory;
    public AudioClip bgmDefeat;

    private Coroutine playlistCoroutine;

    // Properties để get/set âm lượng từ code khác (ví dụ: UI Settings slider)
    public float MusicVolume
    {
        get => musicVolume;
        set
        {
            musicVolume = Mathf.Clamp01(value);
            if (musicSource != null) musicSource.volume = musicVolume;
        }
    }

    public float SFXVolume
    {
        get => sfxVolume;
        set
        {
            sfxVolume = Mathf.Clamp01(value);
            if (sfxSource != null) sfxSource.volume = sfxVolume;
        }
    }

    private void Awake()
    {
        // Đảm bảo chỉ có một SoundManager duy nhất trong game
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeAudioSources();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnValidate()
    {
        // Cập nhật âm lượng ngay trong Editor khi kéo thanh trượt
        if (musicSource != null) musicSource.volume = musicVolume;
        if (sfxSource != null) sfxSource.volume = sfxVolume;
    }

    private void InitializeAudioSources()
    {
        // Tự động tạo AudioSource nếu chưa được gán
        if (musicSource == null)
        {
            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.loop = true;
            musicSource.playOnAwake = false;
        }
        musicSource.volume = musicVolume;

        if (sfxSource == null)
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.loop = false;
            sfxSource.playOnAwake = false;
        }
        sfxSource.volume = sfxVolume;
    }

    /// <summary>
    /// Phát một âm thanh hiệu ứng (SFX) tùy chọn.
    /// </summary>
    public void PlaySFX(AudioClip clip)
    {
        if (clip != null && sfxSource != null)
        {
            sfxSource.PlayOneShot(clip);
        }
    }

    /// <summary>
    /// Phát âm thanh click chuột mặc định cho các nút.
    /// </summary>
    public void PlayDefaultClickSound()
    {
        if (defaultClickSound != null)
        {
            PlaySFX(defaultClickSound);
        }
        else
        {
            Debug.LogWarning("[SoundManager] Default click sound is not assigned.");
        }
    }

    /// <summary>
    /// Phát nhạc nền (BGM).
    /// </summary>
    public void PlayMusic(AudioClip musicClip)
    {
        if (musicSource == null || musicClip == null) return;

        if (playlistCoroutine != null)
        {
            StopCoroutine(playlistCoroutine);
            playlistCoroutine = null;
        }

        if (musicSource.clip == musicClip && musicSource.isPlaying) return;

        musicSource.loop = true;
        musicSource.clip = musicClip;
        musicSource.Play();
    }

    /// <summary>
    /// Phát một danh sách nhạc nền luân phiên.
    /// </summary>
    public void PlayMusicPlaylist(AudioClip[] playlist)
    {
        if (playlist == null || playlist.Length == 0) return;

        if (playlistCoroutine != null)
        {
            StopCoroutine(playlistCoroutine);
        }

        playlistCoroutine = StartCoroutine(PlaylistRoutine(playlist));
    }

    private IEnumerator PlaylistRoutine(AudioClip[] playlist)
    {
        int currentIndex = 0;
        
        if (musicSource == null) yield break;

        musicSource.loop = false; // Tắt loop để chuyển bài
        
        while (true)
        {
            musicSource.clip = playlist[currentIndex];
            musicSource.Play();

            // Chờ bài nhạc kết thúc
            while (musicSource.isPlaying)
            {
                yield return null;
            }

            currentIndex++;
            if (currentIndex >= playlist.Length)
            {
                currentIndex = 0;
            }
        }
    }

    /// <summary>
    /// Dừng nhạc nền.
    /// </summary>
    public void StopMusic()
    {
        if (playlistCoroutine != null)
        {
            StopCoroutine(playlistCoroutine);
            playlistCoroutine = null;
        }

        if (musicSource != null)
        {
            musicSource.Stop();
        }
    }
}
