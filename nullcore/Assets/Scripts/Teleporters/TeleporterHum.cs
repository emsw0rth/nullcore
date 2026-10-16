using UnityEngine;
using Nullcore.Core;
using Nullcore.Player;

namespace Nullcore.Teleporters
{
    [RequireComponent(typeof(AudioSource))]
    public class TeleporterHum : MonoBehaviour
    {
        [SerializeField] private TeleporterController teleporter;
        [SerializeField] private AudioClip hum;
        [SerializeField] private float maxVolume = 1f;
        [SerializeField] private float minDistance = 2f;
        [SerializeField] private float maxDistance = 10f;

        private AudioSource audioSource;
        private Transform player;

        private void Awake()
        {
            audioSource = GetComponent<AudioSource>();
            audioSource.clip = hum;
            audioSource.loop = true;
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f;
            audioSource.volume = 0f;

            if (teleporter == null)
            {
                teleporter = GetComponentInParent<TeleporterController>();
            }
        }

        private void Update()
        {
            if (teleporter == null || hum == null)
            {
                return;
            }

            if (!teleporter.On)
            {
                if (audioSource.isPlaying)
                {
                    audioSource.Stop();
                }
                return;
            }

            if (player == null)
            {
                var playerController = FindFirstObjectByType<PlayerController>();
                if (playerController != null)
                {
                    player = playerController.transform;
                }
            }

            if (!audioSource.isPlaying)
            {
                audioSource.Play();
            }

            if (player == null)
            {
                audioSource.volume = 0f;
                return;
            }

            float distance = Vector2.Distance(transform.position, player.position);
            float t = Mathf.InverseLerp(maxDistance, minDistance, distance);
            audioSource.volume = Mathf.Clamp01(t) * maxVolume * GameAudioSettings.SfxVolume;
        }
    }
}
