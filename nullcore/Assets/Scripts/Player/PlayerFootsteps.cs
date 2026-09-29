using UnityEngine;

namespace Nullcore.Player
{
    [RequireComponent(typeof(AudioSource))]
    public class PlayerFootsteps : MonoBehaviour
    {
        [SerializeField] private PlayerController playerController;
        [SerializeField] private AudioClip[] walkClips = new AudioClip[2];
        [SerializeField] private AudioClip[] runClips = new AudioClip[2];
        [SerializeField] private float walkStepInterval = 0.35f;
        [SerializeField] private float runStepInterval = 0.2f;

        private AudioSource audioSource;
        private float timer;
        private int stepIndex;
        private bool wasActive;

        private void Awake()
        {
            audioSource = GetComponent<AudioSource>();
            if (playerController == null)
            {
                playerController = GetComponent<PlayerController>();
            }
        }

        private void Update()
        {
            bool active = playerController != null && playerController.IsGrounded && playerController.IsMoving;

            if (!active)
            {
                wasActive = false;
                timer = 0f;
                return;
            }

            if (!wasActive)
            {
                // Just started moving on the ground - play the first step immediately.
                wasActive = true;
                timer = 0f;
            }

            timer -= Time.deltaTime;
            if (timer <= 0f)
            {
                PlayStep();
                timer = playerController.IsRunning ? runStepInterval : walkStepInterval;
            }
        }

        private void PlayStep()
        {
            AudioClip[] clips = playerController.IsRunning ? runClips : walkClips;
            if (clips == null || clips.Length == 0)
            {
                return;
            }

            AudioClip clip = clips[stepIndex % clips.Length];
            stepIndex++;
            if (clip != null)
            {
                audioSource.PlayOneShot(clip);
            }
        }
    }
}
