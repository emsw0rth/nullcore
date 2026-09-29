using UnityEngine;
using UnityEngine.SceneManagement;
using Nullcore.Core;
using Nullcore.Player;

namespace Nullcore.Teleporters
{
    [RequireComponent(typeof(BoxCollider2D))]
    public class TeleporterExitZone : MonoBehaviour
    {
        private const string LevelCompletedSceneName = "LevelCompleted";

        private TeleporterController teleporter;

        private void Awake()
        {
            teleporter = GetComponentInParent<TeleporterController>();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (teleporter == null || !teleporter.On)
            {
                return;
            }
            if (other.GetComponent<PlayerController>() == null)
            {
                return;
            }

            GameSession.StopTimer();
            GameSession.SetNextLevel(teleporter.NextLevelSceneName);
            SceneManager.LoadScene(LevelCompletedSceneName);
        }

        private void OnDrawGizmos()
        {
            var col = GetComponent<BoxCollider2D>();
            if (col == null)
            {
                return;
            }

            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = new Color(0f, 1f, 1f, 0.35f);
            Gizmos.DrawCube(col.offset, col.size);
            Gizmos.color = new Color(0f, 1f, 1f, 1f);
            Gizmos.DrawWireCube(col.offset, col.size);
        }
    }
}
