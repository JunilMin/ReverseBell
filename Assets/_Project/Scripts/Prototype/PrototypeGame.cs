using System.Collections.Generic;
using ReverseBell.Core;
using UnityEngine;

namespace ReverseBell.Prototype
{
    public sealed class PrototypeGame : MonoBehaviour
    {
        private static readonly Color BackgroundColor = new Color(0.035f, 0.045f, 0.08f);
        private static readonly Color GroundColor = new Color(0.13f, 0.15f, 0.22f);
        private static readonly Color PlatformColor = new Color(0.25f, 0.28f, 0.38f);
        private static readonly Color AccentColor = new Color(0.55f, 0.42f, 0.2f);

        private const int PlatformCount = 50;
        private const int PlayerMaximumHealth = 10;
        private const float PlatformStartY = -1.6f;
        private const float PlatformSpacing = 2.35f;
        private const float TowerTop =
            PlatformStartY + ((PlatformCount - 1) * PlatformSpacing) + 1.9f;
        private const float EnemyStandingOffset = 0.89f;

        private readonly float[] platformXPattern = { -5f, 0f, 5f, 0f };

        private PlayerAvatar player;
        private readonly List<HumanoidMeleeEnemy> enemies = new List<HumanoidMeleeEnemy>();
        private RhythmClock rhythmClock;
        private Transform towerBell;
        private GUIStyle titleStyle;
        private GUIStyle bodyStyle;
        private GUIStyle centerStyle;
        private int checkpointLevel;
        private bool towerCleared;
        private float feedbackUntil;
        private WorldHealthBar playerHealthBar;

        public int Health { get; private set; } = PlayerMaximumHealth;
        public int MaxHealth => PlayerMaximumHealth;

        public int Score { get; private set; }

        public int Combo { get; private set; }

        public int ResonancePoints { get; private set; }

        public int ResonanceLevel => Mathf.Clamp(ResonancePoints / 3, 0, 4);

        public int EnemiesRemaining => enemies.Count;
        public PlayerAvatar PlayerCharacter => player;

        public string Feedback { get; private set; } = "Wait for the enemy attack";

        private void Awake()
        {
            Camera mainCamera = ConfigureCamera();
            CreateStage();
            player.AttackRequested += HandlePlayerAttackRequested;
            player.SpecialRequested += HandleSpecialRequested;
            mainCamera.GetComponent<VerticalCameraFollow>().Initialize(player.transform);

            rhythmClock = FindFirstObjectByType<RhythmClock>();
            foreach (HumanoidMeleeEnemy enemy in enemies)
            {
                enemy.Initialize(
                    this,
                    player,
                    rhythmClock,
                    enemy.WeaponType,
                    enemy.transform.position.x - 1.8f,
                    enemy.transform.position.x + 1.8f);
            }

            var hazardDirector = gameObject.AddComponent<TowerHazardDirector>();
            hazardDirector.Initialize(this, player);
        }

        private void OnDestroy()
        {
            if (player == null)
            {
                return;
            }

            player.AttackRequested -= HandlePlayerAttackRequested;
            player.SpecialRequested -= HandleSpecialRequested;
        }

        private void HandlePlayerAttackRequested(float facingDirection)
        {
            PerformPlayerAttack(facingDirection);
        }

        private void HandleSpecialRequested(float facingDirection)
        {
            TryPerformSpecial(facingDirection);
        }

        private void Update()
        {
            if (player.transform.position.y < GetFallLimit())
            {
                player.ResetPosition();
                TakeDamage(2);
            }

            UpdateCheckpoint();
            UpdateTowerGoal();

            if (!towerCleared &&
                Time.time >= feedbackUntil &&
                Feedback != "Wait for the enemy attack")
            {
                Feedback = "Wait for the enemy attack";
            }
        }

        public void RegisterParry(ParryGrade grade)
        {
            Combo++;

            bool isPerfect = grade == ParryGrade.Perfect;
            Score += isPerfect ? 150 + (Combo * 10) : 75 + (Combo * 5);
            ResonancePoints = Mathf.Min(12, ResonancePoints + (isPerfect ? 2 : 1));
            Feedback = isPerfect ? "PERFECT PARRY!" : "GOOD PARRY";
            feedbackUntil = Time.time + 0.8f;

        }

        public void RegisterHazardParry()
        {
            Combo++;
            Score += 100 + (Combo * 5);
            ResonancePoints = Mathf.Min(12, ResonancePoints + 1);
            Feedback = "TRAP PARRIED!";
            feedbackUntil = Time.time + 0.65f;
        }

        public void RegisterEnemyDefeat(HumanoidMeleeEnemy enemy)
        {
            enemies.Remove(enemy);
            Score += 500;
            Feedback = enemy.WeaponType.ToString().ToUpperInvariant() + " GUARD DEFEATED";
            feedbackUntil = Time.time + 1.2f;
        }

        public bool PerformPlayerAttack(float facingDirection)
        {
            HumanoidMeleeEnemy target = FindClosestEnemy(
                facingDirection,
                1.65f,
                1.3f,
                true);
            if (target == null)
            {
                Feedback = "SWORD MISS";
                feedbackUntil = Time.time + 0.35f;
                return false;
            }

            target.ReceivePlayerAttack(3);
            Score += 50;
            Feedback = "SWORD HIT";
            feedbackUntil = Time.time + 0.45f;
            return true;
        }

        public bool TryPerformSpecial(float facingDirection)
        {
            if (ResonanceLevel < 4)
            {
                Feedback = "SPECIAL NOT READY";
                feedbackUntil = Time.time + 0.7f;
                return false;
            }

            ResonancePoints = 0;
            HumanoidMeleeEnemy[] targets = enemies.ToArray();
            int hitCount = 0;
            foreach (HumanoidMeleeEnemy enemy in targets)
            {
                if (enemy == null ||
                    Vector2.Distance(player.transform.position, enemy.transform.position) > 4f)
                {
                    continue;
                }

                enemy.ReceivePlayerAttack(8);
                hitCount++;
            }

            Score += hitCount * 150;
            Feedback = hitCount > 0 ? "RESONANCE SLASH!" : "RESONANCE RELEASED";
            feedbackUntil = Time.time + 1f;
            return true;
        }

        public HumanoidMeleeEnemy GetEnemy(EnemyWeaponType type)
        {
            return enemies.Find(enemy => enemy != null && enemy.WeaponType == type);
        }

        private HumanoidMeleeEnemy FindClosestEnemy(
            float facingDirection,
            float horizontalRange,
            float verticalRange,
            bool requireFacing)
        {
            HumanoidMeleeEnemy closest = null;
            float closestDistance = float.PositiveInfinity;

            foreach (HumanoidMeleeEnemy enemy in enemies)
            {
                if (enemy == null)
                {
                    continue;
                }

                Vector2 difference = enemy.transform.position - player.transform.position;
                if (Mathf.Abs(difference.x) > horizontalRange ||
                    Mathf.Abs(difference.y) > verticalRange ||
                    (requireFacing && difference.x * facingDirection <= 0f))
                {
                    continue;
                }

                float distance = difference.sqrMagnitude;
                if (distance < closestDistance)
                {
                    closest = enemy;
                    closestDistance = distance;
                }
            }

            return closest;
        }

        public void TakeDamage(int amount = 1)
        {
            Health = Mathf.Max(0, Health - Mathf.Max(1, amount));
            Combo = 0;
            ResonancePoints = Mathf.Max(0, ResonancePoints - 3);
            Feedback = "DAMAGE TAKEN";
            feedbackUntil = Time.time + 0.8f;
            player.ShowHurt();
            playerHealthBar.SetHealth(Health, PlayerMaximumHealth);

            if (Health > 0)
            {
                return;
            }

            Health = PlayerMaximumHealth;
            Score = 0;
            Combo = 0;
            ResonancePoints = 0;
            Feedback = "RESTART";
            feedbackUntil = Time.time + 1f;
            player.ResetPosition();
            playerHealthBar.SetHealth(Health, PlayerMaximumHealth);
        }

        private static Camera ConfigureCamera()
        {
            Camera mainCamera = Camera.main;
            if (mainCamera == null)
            {
                var cameraObject = new GameObject("Main Camera");
                mainCamera = cameraObject.AddComponent<Camera>();
                cameraObject.tag = "MainCamera";
            }

            mainCamera.orthographic = true;
            mainCamera.orthographicSize = 6.1f;
            mainCamera.transform.position = new Vector3(0f, 0f, -10f);
            mainCamera.backgroundColor = BackgroundColor;

            VerticalCameraFollow cameraFollow =
                mainCamera.GetComponent<VerticalCameraFollow>() ??
                mainCamera.gameObject.AddComponent<VerticalCameraFollow>();
            return mainCamera;
        }

        private void CreateStage()
        {
            float towerCenter = (TowerTop - 4f) * 0.5f;
            float towerHeight = TowerTop + 9f;

            CreateBlock("Ground", new Vector2(0f, -4f), new Vector2(17f, 1f), GroundColor, true);
            CreateBlock(
                "Left Tower Wall",
                new Vector2(-8.7f, towerCenter),
                new Vector2(0.8f, towerHeight),
                GroundColor,
                true);
            CreateBlock(
                "Right Tower Wall",
                new Vector2(8.7f, towerCenter),
                new Vector2(0.8f, towerHeight),
                GroundColor,
                true);

            CreateTowerPlatforms();
            CreateTowerDecorations();

            player = CreatePlayer(new Vector2(-5.5f, -2.8f));
            playerHealthBar = player.gameObject.AddComponent<WorldHealthBar>();
            playerHealthBar.Initialize(1.15f, 1.35f, new Color(0.2f, 0.9f, 0.35f));
            playerHealthBar.SetHealth(Health, PlayerMaximumHealth);
            CreateEnemy("Sword Guard", new Vector2(5f, -2.8f), EnemyWeaponType.Sword);
            CreateEnemy(
                "Spear Guard",
                new Vector2(-5f, PlatformY(4) + EnemyStandingOffset),
                EnemyWeaponType.Spear);
            CreateEnemy(
                "Shield Guard",
                new Vector2(-5f, PlatformY(8) + EnemyStandingOffset),
                EnemyWeaponType.Shield);
            CreateUpperTowerEnemies();

            towerBell = CreateVisualBlock(
                "Tower Bell",
                new Vector2(0f, TowerTop + 1.2f),
                new Vector2(1.4f, 1.8f),
                new Color(1f, 0.72f, 0.16f)).transform;
        }

        private static PlayerAvatar CreatePlayer(Vector2 position)
        {
            var playerObject = new GameObject("Player");
            playerObject.transform.position = position;
            playerObject.AddComponent<SpriteRenderer>();

            var collider = playerObject.AddComponent<CapsuleCollider2D>();
            collider.size = new Vector2(0.72f, 1.4f);
            collider.direction = CapsuleDirection2D.Vertical;

            return playerObject.AddComponent<PlayerAvatar>();
        }

        private void CreateEnemy(string enemyName, Vector2 position, EnemyWeaponType weaponType)
        {
            var enemyObject = new GameObject(enemyName);
            enemyObject.transform.position = position;
            var enemy = enemyObject.AddComponent<HumanoidMeleeEnemy>();
            enemy.SetWeaponType(weaponType);
            enemies.Add(enemy);
        }

        private void CreateUpperTowerEnemies()
        {
            for (int floorIndex = 12; floorIndex < PlatformCount; floorIndex += 4)
            {
                EnemyWeaponType type = ((floorIndex / 4) % 3) switch
                {
                    0 => EnemyWeaponType.Sword,
                    1 => EnemyWeaponType.Spear,
                    _ => EnemyWeaponType.Shield
                };
                float x = platformXPattern[floorIndex % platformXPattern.Length];
                CreateEnemy(
                    type + " Guard Floor " + (floorIndex + 1),
                    new Vector2(x, PlatformY(floorIndex) + EnemyStandingOffset),
                    type);
            }
        }

        private void CreateTowerPlatforms()
        {
            for (int index = 0; index < PlatformCount; index++)
            {
                float y = PlatformY(index);
                float x = platformXPattern[index % platformXPattern.Length];
                float width = index % 4 == 3 ? 6.2f : 5.4f;

                CreateBlock(
                    "Tower Platform " + (index + 1),
                    new Vector2(x, y),
                    new Vector2(width, 0.38f),
                    PlatformColor,
                    true);

                if ((index + 1) % 5 == 0)
                {
                    CreateVisualBlock(
                        "Checkpoint Banner " + (index + 1),
                        new Vector2(x, y + 0.9f),
                        new Vector2(0.22f, 1.4f),
                        new Color(0.18f, 0.7f, 0.75f));
                }
            }

            CreateBlock(
                "Bell Chamber Floor",
                new Vector2(0f, TowerTop),
                new Vector2(12f, 0.5f),
                AccentColor,
                true);
        }

        private static float PlatformY(int index)
        {
            return PlatformStartY + (index * PlatformSpacing);
        }

        private static void CreateTowerDecorations()
        {
            int decorationCount = Mathf.CeilToInt(TowerTop / 3.55f) + 1;
            for (int floor = 0; floor < decorationCount; floor++)
            {
                float y = -1f + (floor * 3.55f);
                CreateVisualBlock(
                    "Left Window " + floor,
                    new Vector2(-8.15f, y),
                    new Vector2(0.35f, 1.2f),
                    new Color(0.12f, 0.3f, 0.46f));
                CreateVisualBlock(
                    "Right Window " + floor,
                    new Vector2(8.15f, y + 1.78f),
                    new Vector2(0.35f, 1.2f),
                    new Color(0.12f, 0.3f, 0.46f));
            }
        }

        private static GameObject CreateBlock(
            string objectName,
            Vector2 position,
            Vector2 scale,
            Color color,
            bool isStatic)
        {
            var block = new GameObject(objectName);
            block.transform.position = position;
            block.transform.localScale = scale;

            var renderer = block.AddComponent<SpriteRenderer>();
            renderer.sprite = PrototypeVisuals.WhiteSprite;
            renderer.color = color;

            block.AddComponent<BoxCollider2D>();

            if (isStatic)
            {
                var body = block.AddComponent<Rigidbody2D>();
                body.bodyType = RigidbodyType2D.Static;
            }

            return block;
        }

        private static GameObject CreateVisualBlock(
            string objectName,
            Vector2 position,
            Vector2 scale,
            Color color)
        {
            var block = new GameObject(objectName);
            block.transform.position = position;
            block.transform.localScale = scale;

            var renderer = block.AddComponent<SpriteRenderer>();
            renderer.sprite = PrototypeVisuals.WhiteSprite;
            renderer.color = color;
            renderer.sortingOrder = -2;
            return block;
        }

        private void UpdateCheckpoint()
        {
            int reachedLevel = Mathf.Clamp(
                Mathf.FloorToInt((player.transform.position.y - PlatformStartY) / PlatformSpacing),
                0,
                PlatformCount);
            int reachedCheckpoint = reachedLevel / 5;

            if (reachedCheckpoint <= checkpointLevel)
            {
                return;
            }

            checkpointLevel = reachedCheckpoint;
            int platformIndex = Mathf.Min((checkpointLevel * 5) - 1, PlatformCount - 1);
            float checkpointX = platformXPattern[platformIndex % platformXPattern.Length];
            float checkpointY = PlatformY(platformIndex) + 1.1f;
            player.SetCheckpoint(new Vector2(checkpointX, checkpointY));
            Feedback = "CHECKPOINT " + checkpointLevel;
            feedbackUntil = Time.time + 1.2f;
        }

        private void UpdateTowerGoal()
        {
            if (towerCleared || towerBell == null)
            {
                return;
            }

            if (Vector2.Distance(player.transform.position, towerBell.position) > 2f)
            {
                return;
            }

            towerCleared = true;
            Score += 5000;
            Feedback = "TOWER ASCENT COMPLETE!";
            feedbackUntil = float.PositiveInfinity;
            towerBell.GetComponent<SpriteRenderer>().color = new Color(0.4f, 1f, 0.75f);
        }

        private float GetFallLimit()
        {
            return checkpointLevel == 0
                ? -7f
                : (checkpointLevel * PlatformSpacing * 5f) - 8f;
        }

        private void OnGUI()
        {
            InitializeGuiStyles();

            GUI.Label(
                new Rect(24f, 18f, 600f, 40f),
                "REVERSE BELL - VERTICAL TOWER",
                titleStyle);
            GUI.Label(
                new Rect(26f, 62f, 650f, 90f),
                "MOVE  LEFT/RIGHT    JUMP  UP    ATTACK  Z    PARRY  X    SPECIAL  C\n" +
                "CLIMB TO THE BELL AT THE TOP",
                bodyStyle);

            string beatText = rhythmClock == null ? "-" : (rhythmClock.CurrentBeat + 1).ToString();
            int floor = Mathf.Clamp(
                Mathf.FloorToInt((player.transform.position.y + 4f) / PlatformSpacing) + 1,
                1,
                PlatformCount);
            GUI.Label(
                new Rect(Screen.width - 350f, 24f, 320f, 180f),
                "HP  " + Health + " / " + PlayerMaximumHealth +
                "\nSCORE  " + Score +
                "\nCOMBO  x" + Combo +
                "\nRESONANCE  " + ResonanceLevel + " / 4" +
                (ResonanceLevel >= 4 ? "  SPECIAL READY" : string.Empty) +
                "\nENEMIES  " + EnemiesRemaining +
                "\nFLOOR  " + floor + " / " + PlatformCount +
                "\nBEAT  " + beatText,
                bodyStyle);

            GUI.Label(
                new Rect((Screen.width * 0.5f) - 250f, 24f, 500f, 60f),
                Feedback,
                centerStyle);
        }

        private void InitializeGuiStyles()
        {
            if (titleStyle != null)
            {
                return;
            }

            titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 22,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.85f, 0.9f, 1f) }
            };

            bodyStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 17,
                normal = { textColor = new Color(0.75f, 0.8f, 0.9f) }
            };

            centerStyle = new GUIStyle(titleStyle)
            {
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(1f, 0.8f, 0.25f) }
            };
        }
    }
}
