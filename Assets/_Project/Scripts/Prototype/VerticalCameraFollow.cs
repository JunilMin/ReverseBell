using UnityEngine;

namespace ReverseBell.Prototype
{
    public sealed class VerticalCameraFollow : MonoBehaviour
    {
        private const float MinimumY = 0f;

        private Transform target;
        private float velocity;
        private float maximumY = 27f;
        private Camera followCamera;

        public float MaximumY => maximumY;

        public void Initialize(Transform followTarget, float towerMaximumY)
        {
            target = followTarget;
            maximumY = Mathf.Max(MinimumY, towerMaximumY);
            followCamera = GetComponent<Camera>();
        }

        private void LateUpdate()
        {
            if (target == null)
            {
                return;
            }

            float desiredY = Mathf.Clamp(target.position.y + 1.4f, MinimumY, maximumY);
            float smoothY = Mathf.SmoothDamp(
                transform.position.y,
                desiredY,
                ref velocity,
                0.18f);

            if (followCamera != null)
            {
                float safeOffset = followCamera.orthographicSize * 0.62f;
                if (target.position.y > smoothY + safeOffset)
                {
                    smoothY = target.position.y - safeOffset;
                    velocity = 0f;
                }
                else if (target.position.y < smoothY - safeOffset)
                {
                    smoothY = target.position.y + safeOffset;
                    velocity = 0f;
                }
            }

            smoothY = Mathf.Clamp(smoothY, MinimumY, maximumY);
            transform.position = new Vector3(0f, smoothY, -10f);
        }
    }
}
