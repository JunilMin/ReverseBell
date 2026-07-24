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
        private bool attackResetPending;
        private float attackResetAt;

        public int Health { get; private set; }

        public int MaxHealth { get; private set; }

        public EnemyWeaponType WeaponType => weaponType;

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

            MaxHealth = type == EnemyWeaponType.Shield ? 4 : 3;
            Health = MaxHealth;
            attackRange = type == EnemyWeaponType.Spear ? 1.9f : 1.25f;
            moveSpeed = type == EnemyWeaponType.Shield ? 1.6f : 2.2f;

            GetComponent<BoxCollider2D>().size = new Vector2(0.8f, 1.4f);
            GetComponent<BoxCollider2D>().isTrigger = true;

            visual = gameObject.AddComponent<HumanoidEnemyVisual>();
            visual.Build(type);

            if (clock != null)
            {
                clock.Beat += OnBeat;
            }
        }

        private void Update()
        {
            if (defeated)
            {
                return;
            }

            float horizontal = player.transform.position.x - transform.position.x;
            float vertical = Mathf.Abs(player.transform.position.y - transform.position.y);
            visual.SetFacing(horizontal);

            if (attackResetPending && Time.time >= attackResetAt)
            {
                attackResetPending = false;
                visual.ResetAttack();
            }

            if (vertical > 1.3f || Mathf.Abs(horizontal) > 5f)
            {
                return;
            }

            if (Mathf.Abs(horizontal) > attackRange * 0.85f)
            {
                float targetX = Mathf.Clamp(player.transform.position.x, leftBound, rightBound);
                float newX = Mathf.MoveTowards(transform.position.x, targetX, moveSpeed * Time.deltaTime);
                transform.position = new Vector3(newX, transform.position.y, 0f);
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
            if (defeated || !PlayerIsInCombatLane())
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
            attackResetPending = true;
            attackResetAt = Time.time + 0.38f;

            if (Vector2.Distance(player.transform.position, transform.position) > attackRange)
            {
                return;
            }

            ParryGrade grade = player.EvaluateParry();
            if (grade == ParryGrade.None)
            {
                game.TakeDamage();
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

            int damage = game.RegisterParry(grade);
            ApplyDamage(damage);
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

            if (Health > 0)
            {
                return;
            }

            defeated = true;
            game.RegisterEnemyDefeat(this);
            Destroy(gameObject);
        }
    }
}
