using UnityEngine;

namespace Nullcore.Core
{
    [RequireComponent(typeof(AudioSource))]
    public class LevelInfo : MonoBehaviour
    {
        [SerializeField] private string levelName;
        [SerializeField] private AudioClip levelMusic;
        [SerializeField, Range(0f, 1f)] private float musicVolume = 0.5f;

        public string LevelName => levelName;

        private void Awake()
        {
            GameSession.StartLevel(levelName);
            PlayMusic();
        }

        private void PlayMusic()
        {
            if (levelMusic == null)
            {
                return;
            }

            var audioSource = GetComponent<AudioSource>();
            audioSource.clip = levelMusic;
            audioSource.loop = true;
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f;
            audioSource.volume = musicVolume;
            audioSource.Play();
        }
    }
}
