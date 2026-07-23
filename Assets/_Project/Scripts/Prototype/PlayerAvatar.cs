using UnityEngine;
using UnityEngine.InputSystem;

namespace ReverseBell.Prototype
{
    [RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D), typeof(SpriteRenderer))]
    public sealed class PlayerAvatar : MonoBehaviour
    {
        private const float MoveSpeed = 6f;
        private const float JumpSpeed = 11f;
        private const float ParryDuration = 0.18f;

        private Rigidbody2D body;
        private SpriteRenderer bodyRenderer;
        private SpriteRenderer parryRenderer;
        private float parryUntil;

        public bool IsParrying => Time.time <= parryUntil;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            body.freezeRotation = true;
            body.gravityScale = 3f;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            bodyRenderer = GetComponent<SpriteRenderer>();

            var parryObject = new GameObject("Parry Flash");
            parryObject.transform.SetParent(transform, false);
            parryObject.transform.localScale = new Vector3(2.3f, 1.6f, 1f);
            parryRenderer = parryObject.AddComponent<SpriteRenderer>();
            parryRenderer.sprite = PrototypeVisuals.WhiteSprite;
            parryRenderer.color = new Color(0.35f, 0.9f, 1f, 0.22f);
            parryRenderer.sortingOrder = -1;
            parryRenderer.enabled = false;
        }

        private void Update()
        {
            float movement = ReadMovement();
            body.linearVelocity = new Vector2(movement * MoveSpeed, body.linearVelocity.y);

            if (WasJumpPressed() && IsGrounded())
            {
                body.linearVelocity = new Vector2(body.linearVelocity.x, JumpSpeed);
            }

            if (WasParryPressed())
            {
                parryUntil = Time.time + ParryDuration;
            }

            parryRenderer.enabled = IsParrying;
            bodyRenderer.color = IsParrying
                ? new Color(0.65f, 0.95f, 1f)
                : new Color(0.2f, 0.75f, 1f);
        }

        public void ResetPosition()
        {
            transform.position = new Vector3(-4.8f, -2.8f, 0f);
            body.linearVelocity = Vector2.zero;
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
                if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed)
                {
                    value -= 1f;
                }

                if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed)
                {
                    value += 1f;
                }
            }

            if (Gamepad.current != null)
            {
                float stick = Gamepad.current.leftStick.x.ReadValue();
                if (Mathf.Abs(stick) > Mathf.Abs(value))
                {
                    value = stick;
                }
            }

            return value;
        }

        private static bool WasJumpPressed()
        {
            bool keyboard = Keyboard.current != null &&
                (Keyboard.current.spaceKey.wasPressedThisFrame ||
                 Keyboard.current.wKey.wasPressedThisFrame ||
                 Keyboard.current.upArrowKey.wasPressedThisFrame);
            bool gamepad = Gamepad.current != null &&
                Gamepad.current.buttonSouth.wasPressedThisFrame;
            return keyboard || gamepad;
        }

        private static bool WasParryPressed()
        {
            bool keyboard = Keyboard.current != null &&
                (Keyboard.current.jKey.wasPressedThisFrame ||
                 Keyboard.current.kKey.wasPressedThisFrame);
            bool gamepad = Gamepad.current != null &&
                Gamepad.current.buttonWest.wasPressedThisFrame;
            return keyboard || gamepad;
        }
    }
}
