using ReverseBell.Core;
using UnityEngine;

namespace ReverseBell.Prototype
{
    [RequireComponent(typeof(BoxCollider2D))]
    public sealed class HumanoidMeleeEnemy : MonoBehaviour
    {
        private PrototypeGame game;
        private PlayerAvatar player;
        private RhythmClock clock;
        private HumanoidEnemyVisual visual;
        private EnemyWeaponType weaponType;
        private float leftBound;
        private float rightBound;
        private float attackRange;
        private float moveSpeed;
        private bool defeated;
        private float stunnedUntil;
        private int attackDamage;
        private WorldHealthBar healthBar;

        public int Health { get; private set; }

        public int MaxHealth { get; private set; }

        public EnemyWeaponType WeaponType => weaponType;
        public bool IsStunned => Time.time < stunnedUntil;

        public void SetWeaponType(EnemyWeaponType type)
        {
            weaponType = type;
        }

        public void Initialize(
            PrototypeGame owner,
            PlayerAvatar target,
            RhythmClock rhythmClock,
            EnemyWeaponType type,
            float patrolLeft,
            float patrolRight)
        {
            game = owner;
            player = target;
            clock = rhythmClock;
            weaponType = type;
            leftBound = patrolLeft;
            rightBound = patrolRight;

            MaxHealth = type switch
            {
                EnemyWeaponType.Shield => 20,
                EnemyWeaponType.Spear => 15,
                _ => 12
            };
            Health = MaxHealth;
            attackRange = type == EnemyWeaponType.Spear ? 1.9f : 1.25f;
            moveSpeed = type == EnemyWeaponType.Shield ? 1.6f : 2.2f;
            attackDamage = type switch
            {
                EnemyWeaponType.Shield => 4,
                EnemyWeaponType.Spear => 3,
                _ => 2
            };

            GetComponent<BoxCollider2D>().size = new Vector2(0.8f, 1.4f);
            GetComponent<BoxCollider2D>().isTrigger = true;

            visual = gameObject.AddComponent<HumanoidEnemyVisual>();
            visual.Build(type);
            healthBar = gameObject.AddComponent<WorldHealthBar>();
            healthBar.Initialize(1.35f, 1.25f, new Color(0.9f, 0.16f, 0.14f));
            healthBar.SetHealth(Health, MaxHealth);

            if (clock != null)
            {
                clock.Beat += OnBeat;
            }
        }

        private void Update()
        {
            if (defeated || IsStunned)
            {
                visual.SetMoving(false);
                return;
            }

            float horizontal = player.transform.position.x - transform.position.x;
            float vertical = Mathf.Abs(player.transform.position.y - transform.position.y);
            visual.SetFacing(horizontal);

            if (vertical > 1.3f || Mathf.Abs(horizontal) > 5f)
            {
                visual.SetMoving(false);
                return;
            }

            if (Mathf.Abs(horizontal) > attackRange * 0.85f)
            {
                float targetX = Mathf.Clamp(player.transform.position.x, leftBound, rightBound);
                float newX = Mathf.MoveTowards(transform.position.x, targetX, moveSpeed * Time.deltaTime);
                transform.position = new Vector3(newX, transform.position.y, 0f);
                visual.SetMoving(true);
            }
            else
            {
                visual.SetMoving(false);
            }
        }

        private void OnDestroy()
        {
            if (clock != null)
            {
                clock.Beat -= OnBeat;
            }
        }

        private void OnBeat(long beat)
        {
            if (defeated || IsStunned || !PlayerIsInCombatLane())
            {
                return;
            }

            long beatInBar = beat % 4;
            if (beatInBar == 1)
            {
                visual.SetTelegraph(true);
            }
            else if (beatInBar == 2)
            {
                PerformAttack();
            }
        }

        private bool PlayerIsInCombatLane()
        {
            Vector2 difference = player.transform.position - transform.position;
            return Mathf.Abs(difference.y) <= 1.3f && Mathf.Abs(difference.x) <= 5f;
        }

        private void PerformAttack()
        {
            visual.PlayAttack();

            if (Vector2.Distance(player.transform.position, transform.position) > attackRange)
            {
                return;
            }

            ParryGrade grade = player.EvaluateParry();
            if (grade == ParryGrade.None)
            {
                game.TakeDamage(attackDamage);
                return;
            }

            ReceiveParry(grade);
        }

        public void ReceiveParry(ParryGrade grade)
        {
            if (defeated || grade == ParryGrade.None)
            {
                return;
            }

            game.RegisterParry(grade);
            float direction = Mathf.Sign(transform.position.x - player.transform.position.x);
            if (Mathf.Approximately(direction, 0f))
            {
                direction = -player.FacingDirection;
            }

            float pushedX = Mathf.Clamp(
                transform.position.x + (direction * 0.7f),
                leftBound,
                rightBound);
            transform.position = new Vector3(pushedX, transform.position.y, 0f);
            stunnedUntil = Time.time + 1f;
            visual.PlayStun(1f);
        }

        public void ReceivePlayerAttack(int damage)
        {
            if (defeated || damage <= 0)
            {
                return;
            }

            ApplyDamage(damage);
        }

        private void ApplyDamage(int damage)
        {
            Health = Mathf.Max(0, Health - damage);
            healthBar.SetHealth(Health, MaxHealth);

            if (Health > 0)
            {
                visual.PlayHit();
                return;
            }

            defeated = true;
            GetComponent<BoxCollider2D>().enabled = false;
            game.RegisterEnemyDefeat(this);
            visual.PlayDeath();
            Destroy(gameObject, visual.DeathDuration);
        }
    }
}
