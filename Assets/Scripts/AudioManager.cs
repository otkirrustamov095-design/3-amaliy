using System;
using UnityEngine;

/// <summary>
/// Tovush tizimi: BGM (fon musiqasi) va SFX (tovush effektlari).
/// Audio fayllar bo'lmasa, tovushlar kod orqali avtomatik yaratiladi,
/// shuning uchun loyiha hech qanday tashqi fayllarsiz ishlaydi.
/// Xohlasangiz, o'zingizning .mp3/.wav fayllaringizni Inspector'dagi maydonlarga qo'yishingiz mumkin.
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Ixtiyoriy: o'z audio fayllaringizni shu yerga sudrab qo'ying")]
    public AudioClip bgmClip;    // fon musiqasi
    public AudioClip jumpClip;   // sakrash SFX
    public AudioClip coinClip;   // bonus SFX
    public AudioClip clickClip;  // tugma bosish SFX

    public float MusicVolume { get; private set; }
    public float SfxVolume { get; private set; }

    const int SampleRate = 22050;
    const string MusicKey = "music_volume";
    const string SfxKey = "sfx_volume";

    AudioSource bgmSource;
    AudioSource sfxSource;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        bgmSource = gameObject.AddComponent<AudioSource>();
        bgmSource.loop = true;
        bgmSource.playOnAwake = false;

        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.loop = false;
        sfxSource.playOnAwake = false;

        if (bgmClip == null) bgmClip = GenerateMusic();
        if (jumpClip == null) jumpClip = GenerateTone("Jump", 0.25f, t => 300f + 1800f * t, 0.5f);
        if (coinClip == null) coinClip = GenerateTone("Coin", 0.35f, t => t < 0.1f ? 988f : 1319f, 0.5f);
        if (clickClip == null) clickClip = GenerateTone("Click", 0.08f, t => 900f, 0.5f);

        MusicVolume = PlayerPrefs.GetFloat(MusicKey, 0.5f);
        SfxVolume = PlayerPrefs.GetFloat(SfxKey, 0.8f);
        ApplyVolumes();

        bgmSource.clip = bgmClip;
        bgmSource.Play();
    }

    // ---------- Umumiy metodlar ----------

    public void PlayJump()  { PlaySfx(jumpClip); }
    public void PlayCoin()  { PlaySfx(coinClip); }
    public void PlayClick() { PlaySfx(clickClip); }

    public void SetMusicVolume(float value)
    {
        MusicVolume = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat(MusicKey, MusicVolume);
        ApplyVolumes();
    }

    public void SetSfxVolume(float value)
    {
        SfxVolume = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat(SfxKey, SfxVolume);
        ApplyVolumes();
    }

    void PlaySfx(AudioClip clip)
    {
        if (clip != null && sfxSource != null) sfxSource.PlayOneShot(clip);
    }

    void ApplyVolumes()
    {
        if (bgmSource != null) bgmSource.volume = MusicVolume;
        if (sfxSource != null) sfxSource.volume = SfxVolume;
    }

    // ---------- Tovushlarni kod orqali yaratish ----------

    static AudioClip MakeClip(string name, float[] data)
    {
        AudioClip clip = AudioClip.Create(name, data.Length, 1, SampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    /// <summary>Qisqa sinus tovush (chastotasi vaqt bo'yicha o'zgaradi) - SFX uchun</summary>
    static AudioClip GenerateTone(string name, float duration, Func<float, float> frequencyAt, float volume)
    {
        int samples = Mathf.CeilToInt(duration * SampleRate);
        float[] data = new float[samples];
        float phase = 0f;
        for (int i = 0; i < samples; i++)
        {
            float t = i / (float)SampleRate;
            phase += frequencyAt(t) / SampleRate;
            float envelope = Mathf.Exp(-4f * t / duration) * Mathf.Min(1f, t * 400f);
            data[i] = Mathf.Sin(2f * Mathf.PI * phase) * envelope * volume;
        }
        return MakeClip(name, data);
    }

    /// <summary>8 soniyalik takrorlanadigan sodda fon musiqasi (melodiya + bas)</summary>
    static AudioClip GenerateMusic()
    {
        // C mazhor pentatonika: C5 D5 E5 G5 A5
        float[] notes = { 523.25f, 587.33f, 659.25f, 783.99f, 880f };
        int[] melody =
        {
            0, 2, 4, 2,  3, 2, 1, -1,
            0, 2, 3, 4,  3, 2, 1, -1,
            2, 3, 4, 3,  2, 1, 0, -1,
            1, 2, 3, 2,  1, 0, -1, -1
        };
        float[] bass = { 130.81f, 110f, 87.31f, 98f }; // C3 A2 F2 G2

        const float step = 0.25f;
        float length = melody.Length * step; // 8 soniya
        int samples = Mathf.CeilToInt(length * SampleRate);
        float[] data = new float[samples];

        for (int i = 0; i < samples; i++)
        {
            float t = i / (float)SampleRate;
            int stepIndex = Mathf.Min((int)(t / step), melody.Length - 1);
            float local = t - stepIndex * step;

            float value = 0f;

            // Melodiya (uchburchak to'lqin)
            int note = melody[stepIndex];
            if (note >= 0)
            {
                float phase = (t * notes[note]) % 1f;
                float triangle = 4f * Mathf.Abs(phase - 0.5f) - 1f;
                float env = Mathf.Exp(-6f * local) * Mathf.Min(1f, local * 100f);
                value += triangle * env * 0.22f;
            }

            // Bas (sinus to'lqin), har 2 soniyada o'zgaradi
            int bar = Mathf.Min((int)(t / 2f), bass.Length - 1);
            float barTime = t - bar * 2f;
            float bassEnv = Mathf.Exp(-1.2f * barTime) * Mathf.Min(1f, barTime * 50f);
            value += Mathf.Sin(2f * Mathf.PI * bass[bar] * t) * bassEnv * 0.25f;

            data[i] = value;
        }
        return MakeClip("BGM", data);
    }
}
