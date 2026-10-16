using UnityEngine;

namespace Nullcore.Core
{
    // The player's volume preferences: one slider for sound effects, one for music. Both are 0..1
    // multipliers on top of whatever volume the individual sounds are authored at, and are saved
    // between sessions. Sound effects should play through PlaySfx (or multiply by SfxVolume) and
    // music sources should multiply by MusicVolume and listen to Changed.
    public static class GameAudioSettings
    {
        private const string SfxKey = "audio.sfxVolume";
        private const string MusicKey = "audio.musicVolume";

        public static float SfxVolume { get; private set; } = 1f;
        public static float MusicVolume { get; private set; } = 1f;

        public static event System.Action Changed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Load()
        {
            SfxVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(SfxKey, 1f));
            MusicVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(MusicKey, 1f));
        }

        public static void SetSfxVolume(float value)
        {
            SfxVolume = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(SfxKey, SfxVolume);
            PlayerPrefs.Save();
            Changed?.Invoke();
        }

        public static void SetMusicVolume(float value)
        {
            MusicVolume = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(MusicKey, MusicVolume);
            PlayerPrefs.Save();
            Changed?.Invoke();
        }

        public static void PlaySfx(AudioSource source, AudioClip clip)
        {
            if (source != null && clip != null)
            {
                source.PlayOneShot(clip, SfxVolume);
            }
        }
    }
}
