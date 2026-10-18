using UnityEngine;

namespace Nullcore.Robots
{
    // Shared sensing helpers for the patrolling robots.
    internal static class RobotSensors
    {
        private static readonly RaycastHit2D[] Hits = new RaycastHit2D[8];

        // True if anything solid on `mask` is directly in front of the robot's body - at ANY height, not just
        // at one point on the body. A single wall ray at body-centre height lets low obstacles (a pipe, a crate)
        // slip underneath it: the robot's lower body is blocked but the sensor sees nothing, so the robot
        // pushes against the obstacle forever instead of turning around.
        //
        // This sweeps a box the size of the body forward by `distance`. The bottom of the box is lifted by
        // `skin` so the floor the robot is standing on never counts as something in front of it.
        public static bool BodyBlocked(Collider2D body, float direction, float distance, LayerMask mask, float skin = 0.05f)
        {
            Bounds b = body.bounds;
            Vector2 size = new Vector2(b.size.x, Mathf.Max(0.01f, b.size.y - skin));
            Vector2 origin = new Vector2(b.center.x, b.center.y + skin * 0.5f);

            var filter = new ContactFilter2D();
            filter.SetLayerMask(mask);
            filter.useTriggers = false;

            int count = body.gameObject.scene.GetPhysicsScene2D().BoxCast(
                origin, size, 0f, Vector2.right * Mathf.Sign(direction), distance, filter, Hits);

            Transform self = body.attachedRigidbody != null ? body.attachedRigidbody.transform : body.transform;
            for (int i = 0; i < count; i++)
            {
                Collider2D hit = Hits[i].collider;
                if (hit != null && !hit.transform.IsChildOf(self))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
