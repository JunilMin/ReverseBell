using ReverseBell.Core;
using UnityEngine;

namespace ReverseBell.Prototype
{
    public sealed class PrototypeGame : MonoBehaviour
    {
        private static readonly Color BackgroundColor = new Color(0.035f, 0.045f, 0.08f);
        private static readonly Color GroundColor = new Color(0.13f, 0.15f, 0.22f);

        private PlayerAvatar player;
        private EnemyRhythmAttacker enemy;
        private RhythmClock rhythmClock;
        private GUIStyle titleStyle;
        private GUIStyle bodyStyle;
        private GUIStyle centerStyle;

        public int Health { get; private set; } = 3;

        public int Score { get; private set; }

        public string Feedback { get; private set; } = "적의 공격을 기다리세요";

        private float feedbackUntil;

        private void Awake()
        {
            ConfigureCamera();
            CreateStage();

            rhythmClock = FindFirstObjectByType<RhythmClock>();
            enemy.Initialize(this, player, rhythmClock);
        }

        private void Update()
        {
            if (player.transform.position.y < -7f)
            {
                player.ResetPosition();
                TakeDamage();
            }

            if (Time.time >= feedbackUntil && Feedback != "적의 공격을 기다리세요")
            {
                Feedback = "적의 공격을 기다리세요";
            }
        }

        public void RegisterParry()
        {
            Score++;
            Feedback = "PERFECT PARRY!";
            feedbackUntil = Time.time + 0.8f;
        }

        public void TakeDamage()
        {
            Health--;
            Feedback = "공격을 놓쳤습니다";
            feedbackUntil = Time.time + 0.8f;

            if (Health > 0)
            {
                return;
            }

            Health = 3;
            Score = 0;
            Feedback = "다시 시작!";
            feedbackUntil = Time.time + 1f;
        }

        private void ConfigureCamera()
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
        }

        private void CreateStage()
        {
            CreateBlock("Ground", new Vector2(0f, -4f), new Vector2(18f, 1f), GroundColor, true);
            CreateBlock("Left Tower", new Vector2(-7.5f, -1.5f), new Vector2(1f, 4f), GroundColor, true);
            CreateBlock("Right Tower", new Vector2(7.5f, -1.5f), new Vector2(1f, 4f), GroundColor, true);

            GameObject playerObject = CreateBlock(
                "Player",
                new Vector2(-4.8f, -2.8f),
                new Vector2(0.8f, 1.4f),
                new Color(0.2f, 0.75f, 1f),
                false);
            player = playerObject.AddComponent<PlayerAvatar>();

            GameObject enemyObject = CreateBlock(
                "Enemy",
                new Vector2(4.8f, -2.8f),
                new Vector2(0.9f, 1.5f),
                new Color(0.95f, 0.2f, 0.28f),
                true);
            enemy = enemyObject.AddComponent<EnemyRhythmAttacker>();
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

            var collider = block.AddComponent<BoxCollider2D>();

            if (isStatic)
            {
                var body = block.AddComponent<Rigidbody2D>();
                body.bodyType = RigidbodyType2D.Static;
            }

            return block;
        }

        private void OnGUI()
        {
            InitializeGuiStyles();

            GUI.Label(new Rect(24f, 18f, 500f, 40f), "REVERSE BELL — PLAYABLE PROTOTYPE", titleStyle);
            GUI.Label(new Rect(26f, 62f, 600f, 90f),
                "이동  A/D 또는 ←/→    점프  Space    패링  J\n" +
                "패드  Left Stick       점프  A        패링  X",
                bodyStyle);

            string beatText = rhythmClock == null ? "-" : (rhythmClock.CurrentBeat + 1).ToString();
            GUI.Label(
                new Rect(Screen.width - 330f, 24f, 300f, 100f),
                "HP  " + new string('●', Health) + "\nPARRY  " + Score + "\nBEAT  " + beatText,
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

