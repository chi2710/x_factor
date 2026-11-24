using System;
using UnityEngine;

// Giá trị cài đặt đang áp dụng, lưu bằng PlayerPrefs.
// Project chưa có AudioMixer: Master gán thẳng vào AudioListener.volume, các giá trị còn lại
// được lưu và báo qua event Changed để hệ thống âm thanh sau này đọc.
public static class GameSettings
{
    public const int DefaultMasterVolume = 60;
    public const bool DefaultMusic = true;
    public const bool DefaultSfx = false;
    public const int DefaultAmbientVolume = 100;

    public static int MasterVolume { get; private set; } = DefaultMasterVolume;   // 0-100
    public static bool Music { get; private set; } = DefaultMusic;
    public static bool Sfx { get; private set; } = DefaultSfx;
    public static int AmbientVolume { get; private set; } = DefaultAmbientVolume; // 0-100

    public static event Action Changed;

    const string MasterKey = "settings.masterVolume";
    const string MusicKey = "settings.music";
    const string SfxKey = "settings.sfx";
    const string AmbientKey = "settings.ambientVolume";

    // Áp dụng cài đặt đã lưu ngay khi vào game, không cần mở Settings.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Init()
    {
        Load();
        Apply();
    }

    public static void Load()
    {
        MasterVolume = PlayerPrefs.GetInt(MasterKey, DefaultMasterVolume);
        Music = PlayerPrefs.GetInt(MusicKey, DefaultMusic ? 1 : 0) == 1;
        Sfx = PlayerPrefs.GetInt(SfxKey, DefaultSfx ? 1 : 0) == 1;
        AmbientVolume = PlayerPrefs.GetInt(AmbientKey, DefaultAmbientVolume);
    }

    // Ghi giá trị mới, lưu xuống PlayerPrefs và áp dụng.
    public static void Set(int masterVolume, bool music, bool sfx, int ambientVolume)
    {
        MasterVolume = Mathf.Clamp(masterVolume, 0, 100);
        Music = music;
        Sfx = sfx;
        AmbientVolume = Mathf.Clamp(ambientVolume, 0, 100);

        PlayerPrefs.SetInt(MasterKey, MasterVolume);
        PlayerPrefs.SetInt(MusicKey, Music ? 1 : 0);
        PlayerPrefs.SetInt(SfxKey, Sfx ? 1 : 0);
        PlayerPrefs.SetInt(AmbientKey, AmbientVolume);
        PlayerPrefs.Save();

        Apply();
    }

    public static void Apply()
    {
        AudioListener.volume = MasterVolume / 100f;
        Changed?.Invoke();
    }
}
