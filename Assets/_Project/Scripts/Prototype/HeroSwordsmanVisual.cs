using UnityEngine;

namespace ReverseBell.Prototype
{
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class HeroSwordsmanVisual : MonoBehaviour
    {
        private const float IdleFramesPerSecond = 3f;
        private const float RunFramesPerSecond = 10f;
        private const float AttackFramesPerSecond = 22f;

        private SpriteRenderer characterRenderer;
        private Sprite[] idleFrames;
        private Sprite[] runFrames;
        private Sprite[] jumpFrames;
        private Sprite[] attackFrames;
        private float facing = 1f;
        private float parryAnimationStartedAt;
        private bool wasParrying;

        private void Awake()
        {
            characterRenderer = GetComponent<SpriteRenderer>();
            characterRenderer.color = Color.white;
            characterRenderer.sortingOrder = 3;

            idleFrames = CreateFrames(
                Resources.Load<Texture2D>("HeroSwordsman/Idle"),
                1,
                2,
                2);
            runFrames = CreateFrames(
                Resources.Load<Texture2D>("HeroSwordsman/Run"),
                3,
                3,
                8);
            jumpFrames = CreateFrames(
                Resources.Load<Texture2D>("HeroSwordsman/JumpAndFall"),
                2,
                2,
                4);
            attackFrames = CreateFrames(
                Resources.Load<Texture2D>("HeroSwordsman/Attack"),
                6,
                1,
                6);

            characterRenderer.sprite = idleFrames[0];
        }

        public void SetPose(float movement, float verticalVelocity, bool isGrounded, bool isParrying)
        {
            if (Mathf.Abs(movement) > 0.05f)
            {
                facing = Mathf.Sign(movement);
            }

            characterRenderer.flipX = facing > 0f;

            if (isParrying)
            {
                if (!wasParrying)
                {
                    parryAnimationStartedAt = Time.time;
                }

                float elapsed = Time.time - parryAnimationStartedAt;
                int attackIndex = Mathf.Min(
                    Mathf.FloorToInt(elapsed * AttackFramesPerSecond),
                    attackFrames.Length - 1);
                characterRenderer.sprite = attackFrames[attackIndex];
                wasParrying = true;
                return;
            }

            wasParrying = false;

            if (!isGrounded)
            {
                characterRenderer.sprite = verticalVelocity >= 0f
                    ? jumpFrames[1]
                    : jumpFrames[3];
                return;
            }

            if (Mathf.Abs(movement) > 0.05f)
            {
                characterRenderer.sprite = GetLoopFrame(runFrames, RunFramesPerSecond);
                return;
            }

            characterRenderer.sprite = GetLoopFrame(idleFrames, IdleFramesPerSecond);
        }

        private static Sprite GetLoopFrame(Sprite[] frames, float framesPerSecond)
        {
            int index = Mathf.FloorToInt(Time.time * framesPerSecond) % frames.Length;
            return frames[index];
        }

        private static Sprite[] CreateFrames(
            Texture2D texture,
            int columns,
            int rows,
            int frameCount)
        {
            if (texture == null)
            {
                throw new MissingReferenceException("Hero Swordsman texture was not found in Resources.");
            }

            const float frameSize = 64f;
            var frames = new Sprite[frameCount];

            for (int index = 0; index < frameCount; index++)
            {
                int column = index % columns;
                int topRow = index / columns;
                int rowFromBottom = rows - 1 - topRow;
                var frameRect = new Rect(
                    column * frameSize,
                    rowFromBottom * frameSize,
                    frameSize,
                    frameSize);

                frames[index] = Sprite.Create(
                    texture,
                    frameRect,
                    new Vector2(0.5f, 0.12f),
                    64f);
                frames[index].name = texture.name + " Frame " + index;
            }

            return frames;
        }
    }
}

