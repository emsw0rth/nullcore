using UnityEngine;

namespace Nullcore.Core
{
    [RequireComponent(typeof(AudioSource))]
    public class LevelInfo : MonoBehaviour
    {
        [SerializeField] private string levelName;
        [SerializeField] private AudioClip levelMusic;
        [SerializeField, Range(0f, 1f)] private float musicVolume = 0.5f;

        private AudioSource audioSource;

        public string LevelName => levelName;

        private void Awake()
        {
            GameSession.StartLevel(levelName);
            PlayMusic();
        }

        private void OnEnable()
        {
            GameAudioSettings.Changed += ApplyVolume;
        }

        private void OnDisable()
        {
            GameAudioSettings.Changed -= ApplyVolume;
        }

        private void PlayMusic()
        {
            if (levelMusic == null)
            {
                return;
            }

            audioSource = GetComponent<AudioSource>();
            audioSource.clip = levelMusic;
            audioSource.loop = true;
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f;
            ApplyVolume();
            audioSource.Play();
        }

        // The level's own music volume scaled by the player's music slider.
        private void ApplyVolume()
        {
            if (audioSource != null)
            {
                audioSource.volume = musicVolume * GameAudioSettings.MusicVolume;
            }
        }
    }
}
