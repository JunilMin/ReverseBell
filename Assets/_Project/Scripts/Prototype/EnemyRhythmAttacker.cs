using ReverseBell.Core;
using UnityEngine;

namespace ReverseBell.Prototype
{
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class EnemyRhythmAttacker : MonoBehaviour
    {
        private const float ProjectileSpeed = 5.5f;
        private const float ContactDistance = 0.75f;

        private readonly Color idleColor = new Color(0.95f, 0.2f, 0.28f);
        private readonly Color warningColor = new Color(1f, 0.75f, 0.15f);

        private PrototypeGame game;
        private PlayerAvatar player;
        private RhythmClock clock;
        private SpriteRenderer enemyRenderer;
        private Transform projectile;
        private SpriteRenderer projectileRenderer;
        private bool reflected;
        private bool defeated;
        private float respawnAt;
        private ParryGrade reflectedGrade;
        private int wave = 1;

        public int Health { get; private set; } = 3;

        public int MaxHealth { get; private set; } = 3;

        public void Initialize(PrototypeGame owner, PlayerAvatar target, RhythmClock rhythmClock)
        {
            game = owner;
            player = target;
            clock = rhythmClock;
            enemyRenderer = GetComponent<SpriteRenderer>();

            CreateProjectile();

            if (clock != null)
            {
                clock.Beat += OnBeat;
            }
        }

        private void OnDestroy()
        {
            if (clock != null)
            {
                clock.Beat -= OnBeat;
            }
        }

        private void Update()
        {
            if (defeated)
            {
                if (Time.time >= respawnAt)
                {
                    Respawn();
                }

                return;
            }

            if (!projectile.gameObject.activeSelf)
            {
                return;
            }

            Vector3 target = reflected ? transform.position : player.transform.position;
            projectile.position = Vector3.MoveTowards(
                projectile.position,
                target,
                ProjectileSpeed * Time.deltaTime);

            if (Vector2.Distance(projectile.position, target) > ContactDistance)
            {
                return;
            }

            if (reflected)
            {
                int damage = game.RegisterParry(reflectedGrade);
                ApplyDamage(damage);
                enemyRenderer.color = Color.white;
                Invoke(nameof(RestoreEnemyColor), 0.12f);
                HideProjectile();
                return;
            }

            ParryGrade grade = player.EvaluateParry();
            if (grade != ParryGrade.None)
            {
                reflected = true;
                reflectedGrade = grade;
                projectileRenderer.color = new Color(0.3f, 0.9f, 1f);
                return;
            }

            game.TakeDamage();
            HideProjectile();
        }

        private void OnBeat(long beat)
        {
            if (defeated)
            {
                return;
            }

            long beatInBar = beat % 4;

            if (beatInBar == 1 && !projectile.gameObject.activeSelf)
            {
                enemyRenderer.color = warningColor;
            }
            else if (beatInBar == 2 && !projectile.gameObject.activeSelf)
            {
                enemyRenderer.color = idleColor;
                LaunchProjectile();
            }
        }

        private void CreateProjectile()
        {
            var projectileObject = new GameObject("Rhythm Projectile");
            projectile = projectileObject.transform;
            projectile.localScale = Vector3.one * 0.42f;
            projectileRenderer = projectileObject.AddComponent<SpriteRenderer>();
            projectileRenderer.sprite = PrototypeVisuals.WhiteSprite;
            projectileRenderer.color = new Color(1f, 0.65f, 0.12f);
            projectileRenderer.sortingOrder = 2;
            projectileObject.SetActive(false);
        }

        private void LaunchProjectile()
        {
            reflected = false;
            reflectedGrade = ParryGrade.None;
            projectile.position = transform.position + Vector3.left * 0.8f;
            projectileRenderer.color = new Color(1f, 0.65f, 0.12f);
            float scale = 0.42f + (game.ResonanceLevel * 0.025f);
            projectile.localScale = Vector3.one * scale;
            projectile.gameObject.SetActive(true);
        }

        private void HideProjectile()
        {
            projectile.gameObject.SetActive(false);
            reflected = false;
        }

        private void RestoreEnemyColor()
        {
            if (!defeated)
            {
                enemyRenderer.color = idleColor;
            }
        }

        private void ApplyDamage(int damage)
        {
            Health = Mathf.Max(0, Health - damage);
            if (Health > 0)
            {
                return;
            }

            defeated = true;
            enemyRenderer.enabled = false;
            GetComponent<Collider2D>().enabled = false;
            respawnAt = Time.time + 1.5f;
            game.RegisterEnemyDefeat(wave);
        }

        private void Respawn()
        {
            wave++;
            MaxHealth = 3 + Mathf.Min(wave - 1, 3);
            Health = MaxHealth;
            defeated = false;
            enemyRenderer.enabled = true;
            enemyRenderer.color = idleColor;
            GetComponent<Collider2D>().enabled = true;
        }
    }
}
