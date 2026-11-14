using UnityEngine;
using System.Collections.Generic;

// Ses yoneticisi. Sesler Assets/Resources/Audio/ klasorunden, asagidaki dosya adlariyla yuklenir
// (.wav / .ogg / .mp3, uzanti yazilmaz). Dosya yoksa o ses sessiz kalir ve Console'da bir kez uyari yazilir.
//
// Efektler: mg_shot, laser_shot, enemy_die, scrap, build, upgrade, sell, repair,
//           gate_open, core_hit, wave_done, game_over, victory
// Dongu:    wind (ruzgar ambiyansi), music_snow (muzik)
//
// Ana ses seviyesi menudeki kaydirici ile AudioListener.volume uzerinden ayarlanir.
public class AegisAudio : MonoBehaviour
{
    public static AegisAudio Instance { get; private set; }

    const int PoolSize = 12;

    static readonly string[] SfxNames =
    {
        "mg_shot", "laser_shot", "enemy_die", "scrap", "build", "upgrade", "sell",
        "repair", "gate_open", "core_hit", "wave_done", "game_over", "victory"
    };

    [Header("Dongu Ses Seviyeleri")]
    public float windVolume = 0.22f;
    public float musicVolume = 0.35f;

    private readonly Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();
    private readonly Dictionary<string, float> lastPlay = new Dictionary<string, float>();
    private AudioSource[] pool;
    private int poolIdx = 0;

    public static void Play(string name, float volume = 1f, bool varyPitch = true)
    {
        if (Instance != null) Instance.PlayInternal(name, volume, varyPitch);
    }

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        pool = new AudioSource[PoolSize];
        for (int i = 0; i < PoolSize; i++)
        {
            pool[i] = gameObject.AddComponent<AudioSource>();
            pool[i].playOnAwake = false;
            pool[i].spatialBlend = 0f;
        }

        LoadClips();
        StartLoop("wind", windVolume);
        StartLoop("music_snow", musicVolume);
    }

    void LoadClips()
    {
        List<string> missing = new List<string>();
        foreach (string n in SfxNames)
        {
            AudioClip c = Resources.Load<AudioClip>("Audio/" + n);
            if (c != null) clips[n] = c;
            else missing.Add(n);
        }

        if (missing.Count > 0)
            Debug.LogWarning("AegisAudio: Assets/Resources/Audio/ icinde bulunamayan sesler (sessiz kalacak): " + string.Join(", ", missing));
    }

    void StartLoop(string name, float volume)
    {
        AudioClip c = Resources.Load<AudioClip>("Audio/" + name);
        if (c == null)
        {
            Debug.LogWarning("AegisAudio: '" + name + "' bulunamadi (Assets/Resources/Audio/" + name + ")");
            return;
        }

        AudioSource s = gameObject.AddComponent<AudioSource>();
        s.clip = c;
        s.loop = true;
        s.volume = volume;
        s.spatialBlend = 0f;
        s.Play();
    }

    // Ayni sesin cok sik tekrarini engelleyen minimum aralik (sn)
    static float MinGap(string name)
    {
        switch (name)
        {
            case "mg_shot": return 0.06f;
            case "laser_shot": return 0.10f;
            case "enemy_die": return 0.05f;
            case "scrap": return 0.04f;
            case "core_hit": return 0.35f;
            default: return 0f;
        }
    }

    void PlayInternal(string name, float vol, bool vary)
    {
        AudioClip c;
        if (!clips.TryGetValue(name, out c) || c == null) return;

        float now = Time.unscaledTime;
        float gap = MinGap(name);
        float last;
        if (gap > 0f && lastPlay.TryGetValue(name, out last) && now - last < gap) return;
        lastPlay[name] = now;

        AudioSource src = pool[poolIdx];
        poolIdx = (poolIdx + 1) % pool.Length;
        src.pitch = vary ? Random.Range(0.94f, 1.06f) : 1f;
        src.volume = vol;
        src.clip = c;
        src.Play();
    }
}