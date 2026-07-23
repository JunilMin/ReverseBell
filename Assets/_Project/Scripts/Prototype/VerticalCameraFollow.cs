using UnityEngine;

namespace ReverseBell.Prototype
{
    public sealed class VerticalCameraFollow : MonoBehaviour
    {
        private const float MinimumY = 0f;
        private const float MaximumY = 27f;

        private Transform target;
        private float velocity;

        public void Initialize(Transform followTarget)
        {
            target = followTarget;
        }

        private void LateUpdate()
        {
            if (target == null)
            {
                return;
            }

            float desiredY = Mathf.Clamp(target.position.y + 1.4f, MinimumY, MaximumY);
            float smoothY = Mathf.SmoothDamp(
                transform.position.y,
                desiredY,
                ref velocity,
                0.18f);
            transform.position = new Vector3(0f, smoothY, -10f);
        }
    }
}

