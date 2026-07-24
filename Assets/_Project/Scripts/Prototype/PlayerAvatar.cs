using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ReverseBell.Prototype
{
    [RequireComponent(typeof(Rigidbody2D), typeof(CapsuleCollider2D), typeof(SpriteRenderer))]
    public sealed class PlayerAvatar : MonoBehaviour
    {
        private const float MoveSpeed = 6f;
        private const float JumpSpeed = 12.5f;
        private const float ParryDuration = 0.26f;
        private const float PerfectParryDuration = 0.11f;
        private const float ParryCooldown = 0.55f;
        private const float BackAttackDotThreshold = -0.3f;
        private const float AttackDuration = 0.28f;
        private const float SpecialDuration = 0.45f;
        private const float HurtDuration = 0.35f;
        private const float AttackCooldown = 0.32f;
        private const float SpecialCooldown = 5f;

        private Rigidbody2D body;
        private SpriteRenderer parryRenderer;
        private HeroSwordsmanVisual characterVisual;
        private Vector2 checkpoint = new Vector2(-5.5f, -2.8f);
        private float parryStartedAt = float.NegativeInfinity;
        private float parryUntil;
        private float attackUntil;
        private float specialUntil;
        private float nextAttackAt;
        private float nextSpecialAt;
        private float nextParryAt;
        private float facingDirection = 1f;
        private float attackImpactAt;
        private float specialImpactAt;
        private float queuedAttackDirection;
        private float queuedSpecialDirection;
        private bool attackImpactQueued;
        private bool specialImpactQueued;
        private float hurtUntil;

        public bool IsParrying => Time.time <= parryUntil;
        public float FacingDirection => facingDirection;
        public float ParryCooldownRemaining => Mathf.Max(0f, nextParryAt - Time.time);
        public float SpecialCooldownRemaining => Mathf.Max(0f, nextSpecialAt - Time.time);
        public bool IsSwordAction =>
            IsParrying || Time.time <= attackUntil || Time.time <= specialUntil;
        public HeroActionState CurrentAction
        {
            get
            {
                if (Time.time <= hurtUntil)
                {
                    return HeroActionState.Hurt;
                }

                if (Time.time <= specialUntil)
                {
                    return HeroActionState.Special;
                }

                if (IsParrying)
                {
                    return HeroActionState.Parry;
                }

                return Time.time <= attackUntil
                    ? HeroActionState.Attack
                    : HeroActionState.None;
            }
        }

        public event Action<float> AttackRequested;
        public event Action<float> SpecialRequested;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            body.freezeRotation = true;
            body.gravityScale = 3f;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;

            CapsuleCollider2D capsule = GetComponent<CapsuleCollider2D>();
            capsule.size = new Vector2(0.72f, 1.4f);
            capsule.direction = CapsuleDirection2D.Vertical;
            capsule.sharedMaterial = new PhysicsMaterial2D("Player Movement")
            {
                friction = 0f,
                bounciness = 0f
            };

            characterVisual = gameObject.AddComponent<HeroSwordsmanVisual>();

            var parryObject = new GameObject("Parry Flash");
            parryObject.transform.SetParent(transform, false);
            parryObject.transform.localScale = new Vector3(0.48f, 0.92f, 1f);
            parryRenderer = parryObject.AddComponent<SpriteRenderer>();
            parryRenderer.sprite = PrototypeVisuals.CircleSprite;
            parryRenderer.color = new Color(0.35f, 0.9f, 1f, 0.58f);
            parryRenderer.sortingOrder = 5;
            parryRenderer.enabled = false;
        }

        private void Update()
        {
            ResolveQueuedCombatImpacts();

            float movement = ReadMovement();
            if (Mathf.Abs(movement) > 0.05f)
            {
                facingDirection = Mathf.Sign(movement);
            }

            body.linearVelocity = new Vector2(movement * MoveSpeed, body.linearVelocity.y);

            if (WasJumpPressed() && IsGrounded())
            {
                body.linearVelocity = new Vector2(body.linearVelocity.x, JumpSpeed);
            }

            if (WasAttackPressed() && Time.time >= nextAttackAt)
            {
                attackUntil = Time.time + AttackDuration;
                nextAttackAt = Time.time + AttackCooldown;
                attackImpactAt = Time.time + 0.12f;
                queuedAttackDirection = facingDirection;
                attackImpactQueued = true;
            }

            if (WasParryPressed())
            {
                TryStartParry();
            }

            if (WasSpecialPressed())
            {
                TryStartSpecial();
            }

            parryRenderer.enabled = IsParrying;
            parryRenderer.transform.localPosition =
                new Vector3(facingDirection * 0.55f, 0.02f, 0f);
            characterVisual.SetPose(movement, body.linearVelocity.y, IsGrounded(), CurrentAction);
        }

        private void ResolveQueuedCombatImpacts()
        {
            if (attackImpactQueued && Time.time >= attackImpactAt)
            {
                attackImpactQueued = false;
                AttackRequested?.Invoke(queuedAttackDirection);
            }

            if (specialImpactQueued && Time.time >= specialImpactAt)
            {
                specialImpactQueued = false;
                SpecialRequested?.Invoke(queuedSpecialDirection);
            }
        }

        public void ResetPosition()
        {
            RespawnAt(checkpoint);
        }

        public void RespawnAt(Vector2 position)
        {
            transform.position = new Vector3(position.x, position.y, 0f);
            body.linearVelocity = Vector2.zero;
        }

        public void SetCheckpoint(Vector2 position)
        {
            checkpoint = position;
        }

        public void ShowHurt()
        {
            hurtUntil = Time.time + HurtDuration;
        }

        public bool TryStartParry()
        {
            if (Time.time < nextParryAt)
            {
                return false;
            }

            parryStartedAt = Time.time;
            parryUntil = Time.time + ParryDuration;
            nextParryAt = Time.time + ParryCooldown;
            return true;
        }

        public bool TryStartSpecial()
        {
            if (Time.time < nextSpecialAt)
            {
                return false;
            }

            specialUntil = Time.time + SpecialDuration;
            nextSpecialAt = Time.time + SpecialCooldown;
            specialImpactAt = Time.time + 0.2f;
            queuedSpecialDirection = facingDirection;
            specialImpactQueued = true;
            return true;
        }

        public ParryGrade EvaluateParry()
        {
            if (!IsParrying)
            {
                return ParryGrade.None;
            }

            float elapsed = Time.time - parryStartedAt;
            return elapsed <= PerfectParryDuration ? ParryGrade.Perfect : ParryGrade.Good;
        }

        public bool CanGuardFrom(Vector2 sourcePosition)
        {
            Vector2 directionToSource = sourcePosition - (Vector2)transform.position;
            if (directionToSource.sqrMagnitude <= 0.0001f)
            {
                return true;
            }

            directionToSource.Normalize();
            Vector2 facing = Vector2.right * facingDirection;
            return Vector2.Dot(directionToSource, facing) > BackAttackDotThreshold;
        }

        public ParryGrade ConsumeParryFrom(Vector2 sourcePosition)
        {
            if (!CanGuardFrom(sourcePosition))
            {
                return ParryGrade.None;
            }

            ParryGrade grade = EvaluateParry();
            if (grade != ParryGrade.None)
            {
                parryUntil = float.NegativeInfinity;
            }

            return grade;
        }

        public ParryGrade ConsumeParry()
        {
            return ConsumeParryFrom(
                (Vector2)transform.position + (Vector2.right * facingDirection));
        }

        private bool IsGrounded()
        {
            Vector2 feet = (Vector2)transform.position + new Vector2(0f, -0.71f);
            return HasGroundBelow(feet + Vector2.left * 0.22f) ||
                HasGroundBelow(feet) ||
                HasGroundBelow(feet + Vector2.right * 0.22f);
        }

        private bool HasGroundBelow(Vector2 origin)
        {
            RaycastHit2D[] hits = Physics2D.RaycastAll(origin, Vector2.down, 0.14f);
            foreach (RaycastHit2D hit in hits)
            {
                if (hit.collider != null && hit.collider.gameObject != gameObject)
                {
                    return true;
                }
            }

            return false;
        }

        private static float ReadMovement()
        {
            float value = 0f;

            if (Keyboard.current != null)
            {
                if (Keyboard.current.leftArrowKey.isPressed)
                {
                    value -= 1f;
                }

                if (Keyboard.current.rightArrowKey.isPressed)
                {
                    value += 1f;
                }
            }

            return value;
        }

        private static bool WasJumpPressed()
        {
            return Keyboard.current != null &&
                Keyboard.current.upArrowKey.wasPressedThisFrame;
        }

        private static bool WasAttackPressed()
        {
            return Keyboard.current != null &&
                Keyboard.current.zKey.wasPressedThisFrame;
        }

        private static bool WasParryPressed()
        {
            return Keyboard.current != null &&
                Keyboard.current.xKey.wasPressedThisFrame;
        }

        private static bool WasSpecialPressed()
        {
            return Keyboard.current != null &&
                Keyboard.current.cKey.wasPressedThisFrame;
        }
    }
}
