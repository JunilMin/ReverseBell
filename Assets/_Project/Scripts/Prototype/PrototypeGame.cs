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

        private const float TowerTop = 30.8f;
        private const int PlatformCount = 16;

        private readonly float[] platformXPattern = { -4.2f, 0f, 4.2f, 0f };

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

        public int Health { get; private set; } = 3;

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
            player.AttackRequested += PerformPlayerAttack;
            player.SpecialRequested += TryPerformSpecial;
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
        }

        private void Update()
        {
            if (player.transform.position.y < GetFallLimit())
            {
                player.ResetPosition();
                TakeDamage();
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

        public int RegisterParry(ParryGrade grade)
        {
            Combo++;

            bool isPerfect = grade == ParryGrade.Perfect;
            Score += isPerfect ? 150 + (Combo * 10) : 75 + (Combo * 5);
            ResonancePoints = Mathf.Min(12, ResonancePoints + (isPerfect ? 2 : 1));
            Feedback = isPerfect ? "PERFECT PARRY!" : "GOOD PARRY";
            feedbackUntil = Time.time + 0.8f;

            return isPerfect && ResonanceLevel >= 4 ? 2 : 1;
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

            target.ReceivePlayerAttack(1);
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

                enemy.ReceivePlayerAttack(3);
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

        public void TakeDamage()
        {
            Health--;
            Combo = 0;
            ResonancePoints = Mathf.Max(0, ResonancePoints - 3);
            Feedback = "DAMAGE TAKEN";
            feedbackUntil = Time.time + 0.8f;

            if (Health > 0)
            {
                return;
            }

            Health = 3;
            Score = 0;
            Combo = 0;
            ResonancePoints = 0;
            Feedback = "RESTART";
            feedbackUntil = Time.time + 1f;
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
            mainCamera.orthographicSize = 5.4f;
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

            CreateBlock("Ground", new Vector2(0f, -4f), new Vector2(14f, 1f), GroundColor, true);
            CreateBlock(
                "Left Tower Wall",
                new Vector2(-7.2f, towerCenter),
                new Vector2(0.8f, towerHeight),
                GroundColor,
                true);
            CreateBlock(
                "Right Tower Wall",
                new Vector2(7.2f, towerCenter),
                new Vector2(0.8f, towerHeight),
                GroundColor,
                true);

            CreateTowerPlatforms();
            CreateTowerDecorations();

            player = CreatePlayer(new Vector2(-4.8f, -2.8f));
            CreateEnemy("Sword Guard", new Vector2(4.2f, -2.8f), EnemyWeaponType.Sword);
            CreateEnemy("Spear Guard", new Vector2(-4.2f, 7.4f), EnemyWeaponType.Spear);
            CreateEnemy("Shield Guard", new Vector2(-4.2f, 15.6f), EnemyWeaponType.Shield);

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

            var collider = playerObject.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(0.8f, 1.4f);

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

        private void CreateTowerPlatforms()
        {
            for (int index = 0; index < PlatformCount; index++)
            {
                float y = -1.7f + (index * 2.05f);
                float x = platformXPattern[index % platformXPattern.Length];
                float width = index % 4 == 3 ? 4.8f : 4.2f;

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
                new Vector2(10f, 0.5f),
                AccentColor,
                true);
        }

        private static void CreateTowerDecorations()
        {
            for (int floor = 0; floor < 10; floor++)
            {
                float y = -1f + (floor * 3.2f);
                CreateVisualBlock(
                    "Left Window " + floor,
                    new Vector2(-6.65f, y),
                    new Vector2(0.35f, 1.2f),
                    new Color(0.12f, 0.3f, 0.46f));
                CreateVisualBlock(
                    "Right Window " + floor,
                    new Vector2(6.65f, y + 1.6f),
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
                Mathf.FloorToInt((player.transform.position.y + 1.7f) / 2.05f),
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
            float checkpointY = -1.7f + (platformIndex * 2.05f) + 1.1f;
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
            return checkpointLevel == 0 ? -7f : (checkpointLevel * 10.25f) - 8f;
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
                Mathf.FloorToInt((player.transform.position.y + 4f) / 2.05f) + 1,
                1,
                PlatformCount + 1);
            GUI.Label(
                new Rect(Screen.width - 350f, 24f, 320f, 180f),
                "HP  " + Health +
                "\nSCORE  " + Score +
                "\nCOMBO  x" + Combo +
                "\nRESONANCE  " + ResonanceLevel + " / 4" +
                (ResonanceLevel >= 4 ? "  SPECIAL READY" : string.Empty) +
                "\nENEMIES  " + EnemiesRemaining +
                "\nFLOOR  " + floor + " / " + (PlatformCount + 1) +
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
