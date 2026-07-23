using UnityEngine;

namespace ReverseBell.Prototype
{
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class ThickStickmanVisual : MonoBehaviour
    {
        private static readonly Color BodyColor = new Color(0.18f, 0.72f, 0.95f);
        private static readonly Color ParryColor = new Color(0.55f, 0.96f, 1f);

        private Transform visualRoot;
        private Transform leftArm;
        private Transform rightArm;
        private Transform leftLeg;
        private Transform rightLeg;
        private SpriteRenderer[] renderers;
        private float facing = 1f;

        private void Awake()
        {
            GetComponent<SpriteRenderer>().enabled = false;

            visualRoot = new GameObject("Stickman Visual").transform;
            visualRoot.SetParent(transform, false);

            CreatePart(
                "Stickman Head",
                new Vector2(0f, 0.48f),
                new Vector2(0.46f, 0.46f),
                PrototypeVisuals.CircleSprite,
                3);
            CreatePart(
                "Stickman Torso",
                new Vector2(0f, 0.03f),
                new Vector2(0.24f, 0.68f),
                PrototypeVisuals.WhiteSprite,
                2);

            leftArm = CreatePart(
                "Stickman Left Arm",
                new Vector2(-0.2f, 0.05f),
                new Vector2(0.17f, 0.62f),
                PrototypeVisuals.WhiteSprite,
                1).transform;
            rightArm = CreatePart(
                "Stickman Right Arm",
                new Vector2(0.2f, 0.05f),
                new Vector2(0.17f, 0.62f),
                PrototypeVisuals.WhiteSprite,
                1).transform;
            leftLeg = CreatePart(
                "Stickman Left Leg",
                new Vector2(-0.14f, -0.52f),
                new Vector2(0.18f, 0.62f),
                PrototypeVisuals.WhiteSprite,
                1).transform;
            rightLeg = CreatePart(
                "Stickman Right Leg",
                new Vector2(0.14f, -0.52f),
                new Vector2(0.18f, 0.62f),
                PrototypeVisuals.WhiteSprite,
                1).transform;

            renderers = visualRoot.GetComponentsInChildren<SpriteRenderer>();
            SetColor(BodyColor);
        }

        public void SetPose(float movement, float verticalVelocity, bool isGrounded, bool isParrying)
        {
            if (Mathf.Abs(movement) > 0.05f)
            {
                facing = Mathf.Sign(movement);
            }

            visualRoot.localScale = new Vector3(facing, 1f, 1f);
            SetColor(isParrying ? ParryColor : BodyColor);

            if (isParrying)
            {
                SetRotation(leftArm, -78f);
                SetRotation(rightArm, -58f);
                SetRotation(leftLeg, 8f);
                SetRotation(rightLeg, -8f);
                visualRoot.localPosition = new Vector3(0.08f, 0f, 0f);
                return;
            }

            visualRoot.localPosition = Vector3.zero;

            if (!isGrounded)
            {
                float lift = verticalVelocity >= 0f ? 1f : -1f;
                SetRotation(leftArm, -35f * lift);
                SetRotation(rightArm, 35f * lift);
                SetRotation(leftLeg, 18f);
                SetRotation(rightLeg, -18f);
                return;
            }

            float intensity = Mathf.Clamp01(Mathf.Abs(movement));
            float stride = Mathf.Sin(Time.time * 12f) * 28f * intensity;
            SetRotation(leftArm, -stride);
            SetRotation(rightArm, stride);
            SetRotation(leftLeg, stride);
            SetRotation(rightLeg, -stride);
        }

        private GameObject CreatePart(
            string partName,
            Vector2 localPosition,
            Vector2 localScale,
            Sprite sprite,
            int sortingOrder)
        {
            var part = new GameObject(partName);
            part.transform.SetParent(visualRoot, false);
            part.transform.localPosition = localPosition;
            part.transform.localScale = localScale;

            var renderer = part.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = sortingOrder;
            return part;
        }

        private void SetColor(Color color)
        {
            if (renderers == null)
            {
                return;
            }

            foreach (SpriteRenderer renderer in renderers)
            {
                renderer.color = color;
            }
        }

        private static void SetRotation(Transform part, float angle)
        {
            part.localRotation = Quaternion.Euler(0f, 0f, angle);
        }
    }
}

