using UnityEngine;

namespace ReverseBell.Prototype
{
    public sealed class WallTrapProjectile : MonoBehaviour
    {
        private PrototypeGame game;
        private PlayerAvatar player;
        private Vector2 direction;
        private float speed;
        private int damage;
        private float expiresAt;
        private bool resolved;

        public void Initialize(
            PrototypeGame owner,
            PlayerAvatar target,
            Vector2 travelDirection,
            float travelSpeed,
            int hitDamage,
            bool isRock)
        {
            game = owner;
            player = target;
            direction = travelDirection.normalized;
            speed = travelSpeed;
            damage = hitDamage;
            expiresAt = Time.time + 5f;

            var renderer = gameObject.AddComponent<SpriteRenderer>();
            renderer.sprite = isRock
                ? PrototypeVisuals.CircleSprite
                : PrototypeVisuals.WhiteSprite;
            renderer.color = isRock
                ? new Color(0.48f, 0.42f, 0.36f)
                : new Color(0.82f, 0.86f, 0.92f);
            renderer.sortingOrder = 10;

            transform.localScale = isRock
                ? new Vector3(0.58f, 0.58f, 1f)
                : new Vector3(1.05f, 0.16f, 1f);
            if (!isRock)
            {
                float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                transform.rotation = Quaternion.Euler(0f, 0f, angle);
            }
        }

        private void Update()
        {
            if (resolved)
            {
                return;
            }

            transform.position += (Vector3)(direction * speed * Time.deltaTime);

            if (Vector2.Distance(transform.position, player.transform.position) <= 0.62f)
            {
                ResolveImpact();
                return;
            }

            if (Time.time >= expiresAt)
            {
                Destroy(gameObject);
            }
        }

        public void ResolveImpact()
        {
            if (resolved)
            {
                return;
            }

            resolved = true;
            if (player.ConsumeParry() != ParryGrade.None)
            {
                game.RegisterHazardParry();
            }
            else
            {
                game.TakeDamage(damage);
            }

            Destroy(gameObject);
        }
    }
}
