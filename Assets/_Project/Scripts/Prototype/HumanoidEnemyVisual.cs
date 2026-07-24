using UnityEngine;

namespace ReverseBell.Prototype
{
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class HumanoidEnemyVisual : MonoBehaviour
    {
        private const float FrameSize = 256f;
        private const float PixelsPerUnit = 72f;
        private const float IdleFramesPerSecond = 30f;
        private const float RunFramesPerSecond = 34f;
        private const float AttackFramesPerSecond = 48f;
        private const float ReactionFramesPerSecond = 45f;

        private SpriteRenderer characterRenderer;
        private Sprite[] idleLeft;
        private Sprite[] idleRight;
        private Sprite[] runLeft;
        private Sprite[] runRight;
        private Sprite[] attackLeft;
        private Sprite[] attackRight;
        private Sprite[] hitLeft;
        private Sprite[] hitRight;
        private Sprite[] deathLeft;
        private Sprite[] deathRight;
        private Sprite[] currentFrames;
        private EnemyWeaponType weaponType;
        private float animationStartedAt;
        private float framesPerSecond;
        private float attackFramesPerSecond;
        private float facing = -1f;
        private bool loopAnimation;
        private bool telegraphing;
        private bool attacking;
        private bool reacting;
        private bool dying;

        public float DeathDuration =>
            deathLeft == null ? 0f : deathLeft.Length / ReactionFramesPerSecond;

        public void Build(EnemyWeaponType type)
        {
            weaponType = type;
            characterRenderer = GetComponent<SpriteRenderer>();
            characterRenderer.sortingOrder = 3;
            characterRenderer.color = EnemyTint(type);

            idleLeft = LoadFrames("L_Idle");
            idleRight = LoadFrames("R_Idle");
            runLeft = LoadFrames("L_Run");
            runRight = LoadFrames("R_Run");
            hitLeft = LoadFrames("L_Impact_Body");
            hitRight = LoadFrames("R_Impact_Body");
            deathLeft = LoadFrames("L_Death_Forward");
            deathRight = LoadFrames("R_Death_Forward");

            string attackName = type switch
            {
                EnemyWeaponType.Spear => "Slash3",
                EnemyWeaponType.Shield => "Slash4",
                _ => "Slash1"
            };
            attackLeft = LoadFrames("L_" + attackName);
            attackRight = LoadFrames("R_" + attackName);
            attackFramesPerSecond =
                type == EnemyWeaponType.Shield ? 100f : AttackFramesPerSecond;

            PlayLoop(idleLeft, IdleFramesPerSecond);
        }

        private void Update()
        {
            if (currentFrames == null || currentFrames.Length == 0)
            {
                return;
            }

            float elapsed = Time.time - animationStartedAt;
            int rawIndex = Mathf.FloorToInt(elapsed * framesPerSecond);

            if (!loopAnimation && rawIndex >= currentFrames.Length)
            {
                if (dying)
                {
                    characterRenderer.sprite = currentFrames[^1];
                    return;
                }

                telegraphing = false;
                attacking = false;
                reacting = false;
                characterRenderer.color = EnemyTint(weaponType);
                PlayLoop(FacingFrames(idleLeft, idleRight), IdleFramesPerSecond);
                return;
            }

            int index = loopAnimation
                ? rawIndex % currentFrames.Length
                : Mathf.Min(rawIndex, currentFrames.Length - 1);
            characterRenderer.sprite = currentFrames[index];
        }

        public void SetFacing(float direction)
        {
            if (dying || Mathf.Abs(direction) <= 0.01f)
            {
                return;
            }

            float newFacing = Mathf.Sign(direction);
            if (Mathf.Approximately(newFacing, facing))
            {
                return;
            }

            facing = newFacing;
            if (!telegraphing && !attacking && !reacting)
            {
                PlayLoop(FacingFrames(idleLeft, idleRight), IdleFramesPerSecond);
            }
        }

        public void SetMoving(bool moving)
        {
            if (dying || telegraphing || attacking || reacting)
            {
                return;
            }

            Sprite[] desired = moving
                ? FacingFrames(runLeft, runRight)
                : FacingFrames(idleLeft, idleRight);
            float speed = moving ? RunFramesPerSecond : IdleFramesPerSecond;
            if (currentFrames != desired)
            {
                PlayLoop(desired, speed);
            }
        }

        public void SetTelegraph(bool active)
        {
            if (dying || !active)
            {
                return;
            }

            telegraphing = true;
            attacking = false;
            reacting = false;
            characterRenderer.color = Color.Lerp(EnemyTint(weaponType), Color.white, 0.45f);
            PlayOnce(FacingFrames(attackLeft, attackRight), attackFramesPerSecond);
        }

        public void PlayAttack()
        {
            if (dying)
            {
                return;
            }

            attacking = true;
            telegraphing = false;
            characterRenderer.color = EnemyTint(weaponType);
            if (currentFrames != FacingFrames(attackLeft, attackRight))
            {
                PlayOnce(FacingFrames(attackLeft, attackRight), attackFramesPerSecond);
            }
        }

        public void ResetAttack()
        {
            if (dying)
            {
                return;
            }

            attacking = false;
            telegraphing = false;
            characterRenderer.color = EnemyTint(weaponType);
        }

        public void PlayHit()
        {
            if (dying)
            {
                return;
            }

            reacting = true;
            attacking = false;
            telegraphing = false;
            characterRenderer.color = Color.white;
            PlayOnce(FacingFrames(hitLeft, hitRight), ReactionFramesPerSecond);
        }

        public void PlayDeath()
        {
            dying = true;
            reacting = false;
            attacking = false;
            telegraphing = false;
            characterRenderer.color = EnemyTint(weaponType);
            PlayOnce(FacingFrames(deathLeft, deathRight), ReactionFramesPerSecond);
        }

        private void PlayLoop(Sprite[] frames, float speed)
        {
            currentFrames = frames;
            framesPerSecond = speed;
            animationStartedAt = Time.time;
            loopAnimation = true;
        }

        private void PlayOnce(Sprite[] frames, float speed)
        {
            currentFrames = frames;
            framesPerSecond = speed;
            animationStartedAt = Time.time;
            loopAnimation = false;
        }

        private Sprite[] FacingFrames(Sprite[] left, Sprite[] right)
        {
            return facing < 0f ? left : right;
        }

        private static Sprite[] LoadFrames(string textureName)
        {
            Texture2D texture = Resources.Load<Texture2D>("AnimatedKnight/" + textureName);
            if (texture == null)
            {
                throw new MissingReferenceException(
                    "Animated Knight texture was not found: " + textureName);
            }

            int columns = Mathf.RoundToInt(texture.width / FrameSize);
            int rows = Mathf.RoundToInt(texture.height / FrameSize);
            var frames = new Sprite[columns * rows];
            int index = 0;

            for (int topRow = 0; topRow < rows; topRow++)
            {
                int rowFromBottom = rows - 1 - topRow;
                for (int column = 0; column < columns; column++)
                {
                    frames[index] = Sprite.Create(
                        texture,
                        new Rect(
                            column * FrameSize,
                            rowFromBottom * FrameSize,
                            FrameSize,
                            FrameSize),
                        new Vector2(0.5f, 0.13f),
                        PixelsPerUnit);
                    frames[index].name = textureName + " Frame " + index;
                    index++;
                }
            }

            return frames;
        }

        private static Color EnemyTint(EnemyWeaponType type)
        {
            return type switch
            {
                EnemyWeaponType.Spear => new Color(0.78f, 0.9f, 1f),
                EnemyWeaponType.Shield => new Color(1f, 0.78f, 0.58f),
                _ => Color.white
            };
        }
    }
}
