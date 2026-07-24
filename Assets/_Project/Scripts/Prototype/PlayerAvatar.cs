using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ReverseBell.Prototype
{
    [RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D), typeof(SpriteRenderer))]
    public sealed class PlayerAvatar : MonoBehaviour
    {
        private const float MoveSpeed = 6f;
        private const float JumpSpeed = 12.5f;
        private const float ParryDuration = 0.18f;
        private const float AttackDuration = 0.28f;
        private const float SpecialDuration = 0.45f;
        private const float AttackCooldown = 0.32f;
        private const float SpecialCooldown = 0.7f;

        private Rigidbody2D body;
        private SpriteRenderer parryRenderer;
        private HeroSwordsmanVisual characterVisual;
        private Vector2 checkpoint = new Vector2(-4.8f, -2.8f);
        private float parryStartedAt = float.NegativeInfinity;
        private float parryUntil;
        private float attackUntil;
        private float specialUntil;
        private float nextAttackAt;
        private float nextSpecialAt;
        private float facingDirection = 1f;

        public bool IsParrying => Time.time <= parryUntil;
        public float FacingDirection => facingDirection;
        public bool IsSwordAction =>
            IsParrying || Time.time <= attackUntil || Time.time <= specialUntil;

        public event Action<float> AttackRequested;
        public event Action<float> SpecialRequested;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            body.freezeRotation = true;
            body.gravityScale = 3f;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            characterVisual = gameObject.AddComponent<HeroSwordsmanVisual>();

            var parryObject = new GameObject("Parry Flash");
            parryObject.transform.SetParent(transform, false);
            parryObject.transform.localScale = new Vector3(2.3f, 1.6f, 1f);
            parryRenderer = parryObject.AddComponent<SpriteRenderer>();
            parryRenderer.sprite = PrototypeVisuals.CircleSprite;
            parryRenderer.color = new Color(0.35f, 0.9f, 1f, 0.22f);
            parryRenderer.sortingOrder = -1;
            parryRenderer.enabled = false;
        }

        private void Update()
        {
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
                AttackRequested?.Invoke(facingDirection);
            }

            if (WasParryPressed())
            {
                parryStartedAt = Time.time;
                parryUntil = Time.time + ParryDuration;
            }

            if (WasSpecialPressed() && Time.time >= nextSpecialAt)
            {
                specialUntil = Time.time + SpecialDuration;
                nextSpecialAt = Time.time + SpecialCooldown;
                SpecialRequested?.Invoke(facingDirection);
            }

            parryRenderer.enabled = IsParrying;
            characterVisual.SetPose(movement, body.linearVelocity.y, IsGrounded(), IsSwordAction);
        }

        public void ResetPosition()
        {
            transform.position = new Vector3(checkpoint.x, checkpoint.y, 0f);
            body.linearVelocity = Vector2.zero;
        }

        public void SetCheckpoint(Vector2 position)
        {
            checkpoint = position;
        }

        public ParryGrade EvaluateParry()
        {
            if (!IsParrying)
            {
                return ParryGrade.None;
            }

            float elapsed = Time.time - parryStartedAt;
            return elapsed <= 0.085f ? ParryGrade.Perfect : ParryGrade.Good;
        }

        private bool IsGrounded()
        {
            Vector2 origin = (Vector2)transform.position + new Vector2(0f, -0.76f);
            return Physics2D.Raycast(origin, Vector2.down, 0.12f).collider != null;
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
