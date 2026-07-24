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
        private Sprite[] parryFrames;
        private Sprite[] specialFrames;
        private Sprite[] hurtFrames;
        private float facing = 1f;
        private float actionAnimationStartedAt;
        private HeroActionState previousAction;

        public bool FacesRight => characterRenderer != null && !characterRenderer.flipX;

        private void Awake()
        {
            characterRenderer = GetComponent<SpriteRenderer>();
            characterRenderer.color = Color.white;
            characterRenderer.sortingOrder = 3;

            idleFrames = CreateFrames(
                Resources.Load<Texture2D>("HeroSwordsman/Idle"),
                1,
                2,
                2,
                52f);
            runFrames = CreateFrames(
                Resources.Load<Texture2D>("HeroSwordsman/Run"),
                3,
                3,
                8,
                52f);
            jumpFrames = CreateFrames(
                Resources.Load<Texture2D>("HeroSwordsman/JumpAndFall"),
                2,
                2,
                4,
                52f);
            attackFrames = CreateFrames(
                Resources.Load<Texture2D>("HeroSwordsman/Attack"),
                6,
                1,
                6,
                44f);
            parryFrames = CreateSelectedFrames(
                Resources.Load<Texture2D>("HeroSwordsman/Attack"),
                6,
                1,
                // The hero pack has no dedicated block sheet. These sword frames
                // lift the blade from a low guard into a firm high guard.
                new[] { 4, 3, 0 },
                52f);
            specialFrames = CreateFrames(
                Resources.Load<Texture2D>("HeroSwordsman/JumpAttack"),
                6,
                1,
                6,
                40f);
            hurtFrames = CreateFrames(
                Resources.Load<Texture2D>("HeroSwordsman/HurtAndDie"),
                3,
                4,
                3,
                52f);

            characterRenderer.sprite = idleFrames[0];
        }

        public void SetPose(
            float movement,
            float verticalVelocity,
            bool isGrounded,
            HeroActionState action)
        {
            if (Mathf.Abs(movement) > 0.05f)
            {
                facing = Mathf.Sign(movement);
            }

            characterRenderer.flipX = facing < 0f;

            if (action != HeroActionState.None)
            {
                if (action != previousAction)
                {
                    actionAnimationStartedAt = Time.time;
                }

                Sprite[] frames = FramesForAction(action);
                float speed = action == HeroActionState.Special ? 14f :
                    action == HeroActionState.Parry ? 18f :
                    action == HeroActionState.Hurt ? 12f :
                    AttackFramesPerSecond;
                float elapsed = Time.time - actionAnimationStartedAt;
                int frameIndex = Mathf.Min(
                    Mathf.FloorToInt(elapsed * speed),
                    frames.Length - 1);
                characterRenderer.sprite = frames[frameIndex];
                previousAction = action;
                return;
            }

            previousAction = HeroActionState.None;

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

        private Sprite[] FramesForAction(HeroActionState action)
        {
            return action switch
            {
                HeroActionState.Parry => parryFrames,
                HeroActionState.Special => specialFrames,
                HeroActionState.Hurt => hurtFrames,
                _ => attackFrames
            };
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
            int frameCount,
            float pixelsPerUnit)
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
                    new Vector2(0.5f, 0.57f),
                    pixelsPerUnit);
                frames[index].name = texture.name + " Frame " + index;
            }

            return frames;
        }

        private static Sprite[] CreateSelectedFrames(
            Texture2D texture,
            int columns,
            int rows,
            int[] selectedIndices,
            float pixelsPerUnit)
        {
            Sprite[] allFrames = CreateFrames(
                texture,
                columns,
                rows,
                columns * rows,
                pixelsPerUnit);
            var selectedFrames = new Sprite[selectedIndices.Length];

            for (int index = 0; index < selectedIndices.Length; index++)
            {
                selectedFrames[index] = allFrames[selectedIndices[index]];
            }

            return selectedFrames;
        }
    }
}
