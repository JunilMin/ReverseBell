using System.Collections.Generic;
using ReverseBell.Core;
using UnityEngine;
using UnityEngine.InputSystem;

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
        private const float PlatformSpacing = 4.6f;
        private const float TowerTop =
            PlatformStartY + ((PlatformCount - 1) * PlatformSpacing) + 1.9f;
        private const float EnemyStandingOffset = 0.89f;
        private const int FloorsBelowPlayerToKeepActive = 5;
        private const int FloorsAbovePlayerToKeepActive = 7;

        private readonly float[] platformXPattern = { -4.8f, -1.8f, 0.2f, -3.2f };

        private PlayerAvatar player;
        private readonly List<HumanoidMeleeEnemy> enemies = new List<HumanoidMeleeEnemy>();
        private readonly Dictionary<int, List<HumanoidMeleeEnemy>> enemiesByFloor =
            new Dictionary<int, List<HumanoidMeleeEnemy>>();
        private readonly Dictionary<int, GameObject> combatGates =
            new Dictionary<int, GameObject>();
        private readonly List<EnemySpawnDefinition> enemySpawnDefinitions =
            new List<EnemySpawnDefinition>();
        private readonly Transform[] floorSections = new Transform[PlatformCount];
        private readonly Dictionary<int, Transform> skillShrines =
            new Dictionary<int, Transform>();
        private readonly HashSet<TowerSkillId> unlockedSkills = new HashSet<TowerSkillId>();
        private RhythmClock rhythmClock;
        private TowerHazardDirector hazardDirector;
        private Transform towerBell;
        private GUIStyle titleStyle;
        private GUIStyle bodyStyle;
        private GUIStyle centerStyle;
        private int checkpointLevel;
        private bool towerCleared;
        private float feedbackUntil;
        private WorldHealthBar playerHealthBar;
        private float damageInvulnerableUntil;
        private Transform finalBossPortal;
        private bool skillMenuOpen;
        private bool skillMenuAllowsUnlock;
        private bool finalBossSpawned;
        private TowerSkillId equippedActiveSkill = TowerSkillId.ResonanceSlash;

        public int Health { get; private set; } = PlayerMaximumHealth;
        public int MaxHealth => PlayerMaximumHealth;
        public int RevivesRemaining { get; private set; } = 3;
        public bool TestModeEnabled { get; private set; }
        public int SkillPoints { get; private set; }
        public bool SkillMenuOpen => skillMenuOpen;
        public TowerSkillId EquippedActiveSkill => equippedActiveSkill;

        public int Score { get; private set; }

        public int Combo { get; private set; }

        public int ResonancePoints { get; private set; }

        public int ResonanceLevel => Mathf.Clamp(ResonancePoints / 3, 0, 4);

        public int EnemiesRemaining => enemies.Count;
        public PlayerAvatar PlayerCharacter => player;
        public int FloorSectionCount => floorSections.Length;
        public int ActiveFloorSectionCount { get; private set; }

        public string Feedback { get; private set; } = "Wait for the enemy attack";

        private void Awake()
        {
            unlockedSkills.Add(TowerSkillId.ResonanceSlash);
            Camera mainCamera = ConfigureCamera();
            CreateStage();
            player.AttackRequested += HandlePlayerAttackRequested;
            player.SpecialRequested += HandleSpecialRequested;
            mainCamera.GetComponent<VerticalCameraFollow>().Initialize(
                player.transform,
                TowerTop + 16f);

            rhythmClock = FindFirstObjectByType<RhythmClock>();
            foreach (HumanoidMeleeEnemy enemy in enemies)
            {
                InitializeEnemy(enemy);
            }

            hazardDirector = gameObject.AddComponent<TowerHazardDirector>();
            hazardDirector.Initialize(this, player);
            UpdateFloorStreaming(true);
        }

        private void OnDestroy()
        {
            Time.timeScale = 1f;
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
            if (skillMenuOpen)
            {
                if (Keyboard.current != null &&
                    (Keyboard.current.eKey.wasPressedThisFrame ||
                     Keyboard.current.escapeKey.wasPressedThisFrame))
                {
                    CloseSkillMenu();
                }
                return;
            }

            if (Keyboard.current != null &&
                Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                OpenSkillMenu(false);
                return;
            }

            if (Keyboard.current != null && Keyboard.current.f4Key.wasPressedThisFrame)
            {
                SetTestMode(!TestModeEnabled);
            }

            if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
            {
                TryInteract();
            }

            if (player.transform.position.y < GetFallLimit())
            {
                player.ResetPosition();
                TakeDamage(2);
            }

            UpdateTowerGoal();
            UpdateFloorStreaming();

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
            if (unlockedSkills.Contains(TowerSkillId.ResonantGuard))
            {
                ResonancePoints = Mathf.Min(12, ResonancePoints + 1);
            }
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

        private void Heal(int amount)
        {
            Health = Mathf.Min(PlayerMaximumHealth, Health + Mathf.Max(0, amount));
            playerHealthBar?.SetHealth(Health, PlayerMaximumHealth);
        }

        public void RegisterEnemyDefeat(HumanoidMeleeEnemy enemy)
        {
            enemies.Remove(enemy);
            if (enemiesByFloor.TryGetValue(enemy.FloorIndex, out List<HumanoidMeleeEnemy> floorEnemies))
            {
                floorEnemies.Remove(enemy);
                if (floorEnemies.TrueForAll(floorEnemy => floorEnemy == null))
                {
                    UnlockCombatGate(enemy.FloorIndex);
                }
            }

            if (enemy.IsBoss)
            {
                SkillPoints++;
                Score += 2500;
                Feedback = "DEMON BOSS DEFEATED - SKILL POINT +1";
                if (finalBossSpawned && enemy.FloorIndex == PlatformCount - 1)
                {
                    towerCleared = true;
                    Score += 10000;
                    Feedback = "DEMON KNIGHT DEFEATED - TOWER COMPLETE!";
                    feedbackUntil = float.PositiveInfinity;
                }
            }
            else
            {
                Score += 500;
                Feedback = enemy.WeaponType.ToString().ToUpperInvariant() + " GUARD DEFEATED";
            }

            if (unlockedSkills.Contains(TowerSkillId.BloodOath))
            {
                Heal(1);
            }
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

            int damage = 3 + (unlockedSkills.Contains(TowerSkillId.SwordMastery) ? 2 : 0);
            target.ReceivePlayerAttack(damage);
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
            if (equippedActiveSkill == TowerSkillId.HealingBell)
            {
                Heal(5);
                Feedback = "HEALING BELL!";
                feedbackUntil = Time.time + 1f;
                return true;
            }

            if (equippedActiveSkill == TowerSkillId.TimeAegis)
            {
                damageInvulnerableUntil = Time.time + 3f;
                Feedback = "TIME AEGIS - 3 SECOND GUARD";
                feedbackUntil = Time.time + 1f;
                return true;
            }

            HumanoidMeleeEnemy[] targets = enemies.ToArray();
            int hitCount = 0;
            float range = equippedActiveSkill == TowerSkillId.DemonBreaker ? 7f : 4f;
            int specialDamage = equippedActiveSkill switch
            {
                TowerSkillId.DemonBreaker => 12,
                TowerSkillId.Execution => 18,
                _ => 8
            };
            if (unlockedSkills.Contains(TowerSkillId.ArcaneEdge))
            {
                specialDamage += 4;
            }
            foreach (HumanoidMeleeEnemy enemy in targets)
            {
                if (enemy == null ||
                    Vector2.Distance(player.transform.position, enemy.transform.position) > range)
                {
                    continue;
                }

                enemy.ReceivePlayerAttack(specialDamage);
                hitCount++;
                if (equippedActiveSkill == TowerSkillId.Execution)
                {
                    break;
                }
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

        public int GetEnemyCountOnFloor(int floorIndex)
        {
            return enemiesByFloor.TryGetValue(
                floorIndex,
                out List<HumanoidMeleeEnemy> floorEnemies)
                ? floorEnemies.FindAll(enemy => enemy != null).Count
                : 0;
        }

        public bool IsFloorGateLocked(int floorIndex)
        {
            return combatGates.TryGetValue(floorIndex, out GameObject gate) &&
                gate != null &&
                gate.activeSelf;
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
            if (TestModeEnabled || Time.time < damageInvulnerableUntil)
            {
                return;
            }

            int reducedDamage = unlockedSkills.Contains(TowerSkillId.IronWill)
                ? Mathf.Max(1, amount - 1)
                : Mathf.Max(1, amount);
            Health = Mathf.Max(0, Health - reducedDamage);
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

            HandlePlayerDeath();
        }

        private void HandlePlayerDeath()
        {
            Vector2 deathPosition = player.transform.position;
            Health = PlayerMaximumHealth;
            Combo = 0;
            ResonancePoints = 0;
            damageInvulnerableUntil = Time.time + 1.25f;
            playerHealthBar.SetHealth(Health, PlayerMaximumHealth);

            if (RevivesRemaining > 0)
            {
                RevivesRemaining--;
                player.RespawnAt(deathPosition);
                Feedback = "REVIVED - " + RevivesRemaining + " LIVES LEFT";
                feedbackUntil = Time.time + 1.2f;
                return;
            }

            RevivesRemaining = 3;
            Score = 0;
            checkpointLevel = 0;
            towerCleared = false;
            Vector2 startPosition = new Vector2(-5.5f, -2.8f);
            player.SetCheckpoint(startPosition);
            player.RespawnAt(startPosition);
            RebuildCombatRun();
            Feedback = "ALL LIVES LOST - RETURN TO FLOOR 1";
            feedbackUntil = Time.time + 1.5f;
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

            CreateFloorSections();
            CreateTowerPlatforms();
            CreateTowerDecorations();

            player = CreatePlayer(new Vector2(-5.5f, -2.8f));
            playerHealthBar = player.gameObject.AddComponent<WorldHealthBar>();
            playerHealthBar.Initialize(1.15f, 1.35f, new Color(0.2f, 0.9f, 0.35f));
            playerHealthBar.SetHealth(Health, PlayerMaximumHealth);
            CreateEnemy("Sword Guard", new Vector2(0.2f, -2.8f), EnemyWeaponType.Sword, 0);
            CreateEnemy(
                "Spear Guard",
                new Vector2(-5f, PlatformY(4) + EnemyStandingOffset),
                EnemyWeaponType.Spear,
                4);
            CreateEnemy(
                "Shield Guard",
                new Vector2(-5f, PlatformY(8) + EnemyStandingOffset),
                EnemyWeaponType.Shield,
                8);
            CreateUpperTowerEnemies();
            CreateDemonBosses();
            CreateCombatGates();
            CreateSkillShrines();
            CreateFinalBossArena();

            towerBell = CreateVisualBlock(
                "Final Boss Portal",
                new Vector2(0f, TowerTop + 1.2f),
                new Vector2(1.4f, 1.8f),
                new Color(1f, 0.72f, 0.16f)).transform;
            finalBossPortal = towerBell;
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

        private HumanoidMeleeEnemy CreateEnemy(
            string enemyName,
            Vector2 position,
            EnemyWeaponType weaponType,
            int floorIndex,
            bool recordSpawn = true,
            int bossTier = 0)
        {
            var enemyObject = new GameObject(enemyName);
            enemyObject.transform.position = position;
            ParentToFloorSection(enemyObject, floorIndex);
            var enemy = enemyObject.AddComponent<HumanoidMeleeEnemy>();
            enemy.SetWeaponType(weaponType);
            enemy.SetFloorIndex(floorIndex);
            enemy.SetBossTier(bossTier);
            enemies.Add(enemy);

            if (!enemiesByFloor.TryGetValue(floorIndex, out List<HumanoidMeleeEnemy> floorEnemies))
            {
                floorEnemies = new List<HumanoidMeleeEnemy>();
                enemiesByFloor.Add(floorIndex, floorEnemies);
            }
            floorEnemies.Add(enemy);

            if (recordSpawn)
            {
                enemySpawnDefinitions.Add(
                    new EnemySpawnDefinition(
                        enemyName,
                        position,
                        weaponType,
                        floorIndex,
                        bossTier));
            }

            return enemy;
        }

        private void InitializeEnemy(HumanoidMeleeEnemy enemy)
        {
            enemy.Initialize(
                this,
                player,
                rhythmClock,
                enemy.WeaponType,
                enemy.transform.position.x - 1.8f,
                enemy.transform.position.x + 1.8f);
        }

        private void RebuildCombatRun()
        {
            HumanoidMeleeEnemy[] existingEnemies = enemies.ToArray();
            foreach (HumanoidMeleeEnemy enemy in existingEnemies)
            {
                if (enemy == null)
                {
                    continue;
                }

                enemy.gameObject.SetActive(false);
                Destroy(enemy.gameObject);
            }

            foreach (GameObject gate in combatGates.Values)
            {
                if (gate != null)
                {
                    Destroy(gate);
                }
            }

            WallTrapProjectile[] projectiles =
                FindObjectsByType<WallTrapProjectile>(FindObjectsSortMode.None);
            foreach (WallTrapProjectile projectile in projectiles)
            {
                Destroy(projectile.gameObject);
            }

            enemies.Clear();
            enemiesByFloor.Clear();
            combatGates.Clear();
            finalBossSpawned = false;

            foreach (EnemySpawnDefinition spawn in enemySpawnDefinitions)
            {
                HumanoidMeleeEnemy enemy = CreateEnemy(
                    spawn.Name,
                    spawn.Position,
                    spawn.WeaponType,
                    spawn.FloorIndex,
                    false,
                    spawn.BossTier);
                InitializeEnemy(enemy);
            }

            CreateCombatGates();
            hazardDirector?.ResetDirector();
            UpdateFloorStreaming(true);

            if (towerBell != null)
            {
                towerBell.GetComponent<SpriteRenderer>().color = new Color(1f, 0.72f, 0.16f);
            }
        }

        private void CreateUpperTowerEnemies()
        {
            int[] bossFloors = { 9, 19, 29, 39 };
            for (int floorIndex = 0; floorIndex < PlatformCount; floorIndex++)
            {
                int targetEnemyCount = Mathf.Clamp(1 + (floorIndex / 12), 1, 5);
                bool isBossFloor = System.Array.IndexOf(bossFloors, floorIndex) >= 0;
                if (isBossFloor)
                {
                    targetEnemyCount = Mathf.Max(0, targetEnemyCount - 1);
                }

                int existingCount = enemiesByFloor.TryGetValue(
                    floorIndex,
                    out List<HumanoidMeleeEnemy> existingEnemies)
                    ? existingEnemies.Count
                    : 0;
                float platformX = platformXPattern[floorIndex % platformXPattern.Length];

                for (int enemyIndex = existingCount;
                    enemyIndex < targetEnemyCount;
                    enemyIndex++)
                {
                    EnemyWeaponType type = ((floorIndex / 4 + enemyIndex) % 3) switch
                    {
                        0 => EnemyWeaponType.Sword,
                        1 => EnemyWeaponType.Spear,
                        _ => EnemyWeaponType.Shield
                    };
                    float offset =
                        (enemyIndex - ((targetEnemyCount - 1) * 0.5f)) * 0.75f;
                    CreateEnemy(
                        type + " Guard Floor " + (floorIndex + 1) + " #" + (enemyIndex + 1),
                        new Vector2(
                            platformX + offset,
                            PlatformY(floorIndex) + EnemyStandingOffset),
                        type,
                        floorIndex);
                }
            }
        }

        private void CreateDemonBosses()
        {
            int[] bossFloors = { 9, 19, 29, 39 };
            for (int index = 0; index < bossFloors.Length; index++)
            {
                int floorIndex = bossFloors[index];
                float platformX = platformXPattern[floorIndex % platformXPattern.Length];
                CreateEnemy(
                    "Demon Warden Floor " + (floorIndex + 1),
                    new Vector2(platformX, PlatformY(floorIndex) + EnemyStandingOffset),
                    EnemyWeaponType.Shield,
                    floorIndex,
                    true,
                    index + 1);
            }
        }

        private void CreateSkillShrines()
        {
            int[] shrineFloors = { 9, 19, 29, 39 };
            foreach (int floorIndex in shrineFloors)
            {
                float x = platformXPattern[floorIndex % platformXPattern.Length];
                GameObject shrine = CreateVisualBlock(
                    "Skill Shrine Floor " + (floorIndex + 1),
                    new Vector2(x + 1.8f, PlatformY(floorIndex) + 0.72f),
                    new Vector2(0.65f, 1.25f),
                    new Color(0.62f, 0.22f, 0.9f));
                shrine.GetComponent<SpriteRenderer>().sortingOrder = 2;
                ParentToFloorSection(shrine, floorIndex);
                skillShrines.Add(floorIndex, shrine.transform);
            }
        }

        private void CreateFinalBossArena()
        {
            float arenaY = TowerTop + 11f;
            GameObject floor = CreateBlock(
                "Final Boss Arena Floor",
                new Vector2(0f, arenaY - 2f),
                new Vector2(15f, 0.65f),
                GroundColor,
                true);
            GameObject leftWall = CreateBlock(
                "Final Boss Arena Left Wall",
                new Vector2(-7.4f, arenaY + 1f),
                new Vector2(0.6f, 6.5f),
                GroundColor,
                true);
            GameObject rightWall = CreateBlock(
                "Final Boss Arena Right Wall",
                new Vector2(7.4f, arenaY + 1f),
                new Vector2(0.6f, 6.5f),
                GroundColor,
                true);
            ParentToFloorSection(floor, PlatformCount - 1);
            ParentToFloorSection(leftWall, PlatformCount - 1);
            ParentToFloorSection(rightWall, PlatformCount - 1);
        }

        private void TryInteract()
        {
            foreach (KeyValuePair<int, Transform> shrine in skillShrines)
            {
                if (shrine.Value == null ||
                    Vector2.Distance(player.transform.position, shrine.Value.position) > 1.8f)
                {
                    continue;
                }

                if (GetEnemyCountOnFloor(shrine.Key) > 0 && !TestModeEnabled)
                {
                    Feedback = "DEFEAT THE DEMON BOSS FIRST";
                    feedbackUntil = Time.time + 1f;
                    return;
                }

                ActivateCheckpoint(shrine.Key);
                OpenSkillMenu(true);
                return;
            }

            if (finalBossPortal != null &&
                Vector2.Distance(player.transform.position, finalBossPortal.position) <= 2f)
            {
                EnterFinalBossRoom();
                return;
            }

            Feedback = "NOTHING TO INTERACT WITH";
            feedbackUntil = Time.time + 0.5f;
        }

        private void OpenSkillMenu(bool allowUnlock)
        {
            skillMenuOpen = true;
            skillMenuAllowsUnlock = allowUnlock;
            Time.timeScale = 0f;
            Feedback = allowUnlock ? "SKILL ALTAR" : "SKILL LOADOUT";
        }

        private void CloseSkillMenu()
        {
            skillMenuOpen = false;
            Time.timeScale = 1f;
            Feedback = "SKILL MENU CLOSED";
            feedbackUntil = Time.time + 0.7f;
        }

        private void EnterFinalBossRoom()
        {
            float arenaY = TowerTop + 11f;
            player.RespawnAt(new Vector2(-4.5f, arenaY - 0.95f));
            if (!finalBossSpawned)
            {
                finalBossSpawned = true;
                HumanoidMeleeEnemy boss = CreateEnemy(
                    "Demon Knight Overlord",
                    new Vector2(3.5f, arenaY - 0.95f),
                    EnemyWeaponType.Shield,
                    PlatformCount - 1,
                    false,
                    5);
                InitializeEnemy(boss);
            }

            Feedback = "FINAL DUEL - DEMON KNIGHT OVERLORD";
            feedbackUntil = Time.time + 2f;
        }

        public bool UnlockOrEquipSkill(TowerSkillId skill)
        {
            bool isActive = skill >= TowerSkillId.ResonanceSlash;
            if (!unlockedSkills.Contains(skill))
            {
                if (SkillPoints <= 0)
                {
                    return false;
                }

                SkillPoints--;
                unlockedSkills.Add(skill);
            }

            if (isActive)
            {
                equippedActiveSkill = skill;
            }

            return true;
        }

        public bool IsSkillUnlocked(TowerSkillId skill)
        {
            return unlockedSkills.Contains(skill);
        }

        private void CreateCombatGates()
        {
            foreach (int floorIndex in enemiesByFloor.Keys)
            {
                CreateCombatGate(floorIndex);
            }
        }

        private void CreateCombatGate(int floorIndex)
        {
            var gate = new GameObject("Combat Gate Floor " + (floorIndex + 1));
            gate.transform.position = new Vector3(
                1.72f,
                PlatformY(floorIndex) + 0.62f,
                0f);
            gate.transform.localScale = new Vector3(0.18f, 0.72f, 1f);
            ParentToFloorSection(gate, floorIndex);

            var renderer = gate.AddComponent<SpriteRenderer>();
            renderer.sprite = PrototypeVisuals.WhiteSprite;
            renderer.color = new Color(1f, 0.55f, 0.12f, 0.85f);
            renderer.sortingOrder = 12;
            gate.SetActive(!TestModeEnabled);
            combatGates[floorIndex] = gate;
        }

        public void SetTestMode(bool enabled)
        {
            TestModeEnabled = enabled;
            foreach (GameObject gate in combatGates.Values)
            {
                if (gate != null)
                {
                    gate.SetActive(!enabled);
                }
            }

            Feedback = enabled
                ? "TEST MODE ON - INVINCIBLE / GATES DISABLED"
                : "TEST MODE OFF";
            feedbackUntil = Time.time + 1.5f;
        }

        private void UnlockCombatGate(int floorIndex)
        {
            if (!combatGates.TryGetValue(floorIndex, out GameObject gate))
            {
                return;
            }

            combatGates.Remove(floorIndex);
            Destroy(gate);
            Feedback = "FLOOR " + (floorIndex + 1) + " CLEARED - PATH OPEN";
            feedbackUntil = Time.time + 1.2f;
        }

        private void CreateTowerPlatforms()
        {
            for (int index = 0; index < PlatformCount; index++)
            {
                float y = PlatformY(index);
                float x = platformXPattern[index % platformXPattern.Length];

                CreateCastleChamber(index, y);

                GameObject platform = CreateBlock(
                    "Floor " + (index + 1) + " Walkway",
                    new Vector2(-2.4f, y),
                    new Vector2(9f, 0.48f),
                    PlatformColor,
                    true);
                ParentToFloorSection(platform, index);

                GameObject stairLanding = CreateOneWayLanding(
                    "Floor " + (index + 1) + " Stair Landing",
                    new Vector2(4.55f, y),
                    new Vector2(5.1f, 0.38f),
                    Color.Lerp(PlatformColor, AccentColor, 0.18f));
                ParentToFloorSection(stairLanding, index);

                if (index < PlatformCount - 1)
                {
                    CreateStairFlight(
                        index,
                        y + 0.24f,
                        PlatformY(index + 1) + 0.24f);
                }

                if (index % 3 == 1)
                {
                    float side = index % 2 == 0 ? 1f : -1f;
                    GameObject brazier = CreateVisualBlock(
                        "Tower Brazier " + (index + 1),
                        new Vector2(x - (side * 1.8f), y + 0.55f),
                        new Vector2(0.35f, 0.7f),
                        new Color(1f, 0.35f, 0.08f));
                    ParentToFloorSection(brazier, index);
                }

                if ((index + 1) % 10 == 0)
                {
                    GameObject banner = CreateVisualBlock(
                        "Checkpoint Banner " + (index + 1),
                        new Vector2(x, y + 0.9f),
                        new Vector2(0.22f, 1.4f),
                        new Color(0.18f, 0.7f, 0.75f));
                    ParentToFloorSection(banner, index);
                }
            }

            CreateEntryStairs();

            GameObject bellChamberFloor = CreateBlock(
                "Bell Chamber Floor",
                new Vector2(0f, TowerTop),
                new Vector2(12f, 0.5f),
                AccentColor,
                true);
            ParentToFloorSection(bellChamberFloor, PlatformCount - 1);
        }

        private void CreateCastleChamber(int floorIndex, float floorY)
        {
            Color wallColor = new Color(0.075f, 0.085f, 0.13f);
            Color masonryColor = new Color(0.16f, 0.17f, 0.23f);

            GameObject backdrop = CreateVisualBlock(
                "Floor " + (floorIndex + 1) + " Stone Chamber",
                new Vector2(0f, floorY + (PlatformSpacing * 0.5f)),
                new Vector2(13.6f, PlatformSpacing - 0.12f),
                wallColor);
            backdrop.GetComponent<SpriteRenderer>().sortingOrder = -8;
            ParentToFloorSection(backdrop, floorIndex);

            GameObject leftPillar = CreateVisualBlock(
                "Floor " + (floorIndex + 1) + " Left Pillar",
                new Vector2(-6.55f, floorY + 2.25f),
                new Vector2(0.42f, 4.45f),
                masonryColor);
            GameObject stairPillar = CreateVisualBlock(
                "Floor " + (floorIndex + 1) + " Stairwell Pillar",
                new Vector2(1.82f, floorY + 2.55f),
                new Vector2(0.32f, 3.45f),
                masonryColor);
            GameObject ceilingBeam = CreateVisualBlock(
                "Floor " + (floorIndex + 1) + " Ceiling Beam",
                new Vector2(0f, floorY + PlatformSpacing - 0.18f),
                new Vector2(13.6f, 0.36f),
                masonryColor);
            leftPillar.GetComponent<SpriteRenderer>().sortingOrder = -2;
            stairPillar.GetComponent<SpriteRenderer>().sortingOrder = -2;
            ceilingBeam.GetComponent<SpriteRenderer>().sortingOrder = -2;
            ParentToFloorSection(leftPillar, floorIndex);
            ParentToFloorSection(stairPillar, floorIndex);
            ParentToFloorSection(ceilingBeam, floorIndex);

            for (int brick = 0; brick < 5; brick++)
            {
                float brickX = -5.3f + (brick * 1.55f);
                GameObject stone = CreateVisualBlock(
                    "Floor " + (floorIndex + 1) + " Wall Stone " + (brick + 1),
                    new Vector2(brickX, floorY + 3.65f),
                    new Vector2(1.18f, 0.18f),
                    new Color(0.22f, 0.23f, 0.29f));
                stone.GetComponent<SpriteRenderer>().sortingOrder = -3;
                ParentToFloorSection(stone, floorIndex);
            }
        }

        private void CreateEntryStairs()
        {
            CreateStairFlight(
                0,
                -3.5f,
                PlatformY(0) + 0.24f,
                "Tower Entrance");
        }

        private void CreateStairFlight(
            int floorIndex,
            float startY,
            float endY,
            string customName = null)
        {
            const int StepCount = 11;
            const float StartX = 2.15f;
            const float EndX = 6.65f;
            string stairName = customName ?? "Stairs " + (floorIndex + 1) + " to " + (floorIndex + 2);

            var ramp = new GameObject(stairName + " Ramp");
            ramp.transform.position = Vector3.zero;
            var rampCollider = ramp.AddComponent<PolygonCollider2D>();
            rampCollider.isTrigger = true;
            rampCollider.points = new[]
            {
                new Vector2(StartX, startY - 0.18f),
                new Vector2(EndX, startY - 0.18f),
                new Vector2(EndX, endY)
            };
            ParentToFloorSection(ramp, floorIndex);

            float angle = Mathf.Atan2(endY - startY, EndX - StartX) * Mathf.Rad2Deg;
            float length = Vector2.Distance(
                new Vector2(StartX, startY),
                new Vector2(EndX, endY));
            var climbZone = new GameObject(stairName + " Climb Zone");
            climbZone.transform.position = new Vector3(
                (StartX + EndX) * 0.5f,
                (startY + endY) * 0.5f + 0.35f,
                0f);
            climbZone.transform.rotation = Quaternion.Euler(0f, 0f, angle);
            var climbTrigger = climbZone.AddComponent<BoxCollider2D>();
            climbTrigger.size = new Vector2(length, 1.25f);
            climbTrigger.isTrigger = true;
            StairClimbZone stairZone = climbZone.AddComponent<StairClimbZone>();
            stairZone.Configure(
                new Vector2(StartX, startY),
                new Vector2(EndX, endY));
            ParentToFloorSection(climbZone, floorIndex);

            Color stairColor = Color.Lerp(PlatformColor, AccentColor, 0.28f);
            for (int stepIndex = 0; stepIndex < StepCount; stepIndex++)
            {
                float t = (stepIndex + 0.5f) / StepCount;
                float x = Mathf.Lerp(StartX, EndX, t);
                float y = Mathf.Lerp(startY, endY, t) - 0.11f;
                GameObject step = CreateVisualBlock(
                    stairName + " Step " + (stepIndex + 1),
                    new Vector2(x, y),
                    new Vector2(1.35f, 0.22f),
                    stairColor);
                step.GetComponent<SpriteRenderer>().sortingOrder = 0;
                ParentToFloorSection(step, floorIndex);
            }

            GameObject lowerPost = CreateVisualBlock(
                stairName + " Lower Post",
                new Vector2(StartX, startY + 0.48f),
                new Vector2(0.16f, 0.95f),
                AccentColor);
            GameObject upperPost = CreateVisualBlock(
                stairName + " Upper Post",
                new Vector2(EndX, endY + 0.48f),
                new Vector2(0.16f, 0.95f),
                AccentColor);
            ParentToFloorSection(lowerPost, floorIndex);
            ParentToFloorSection(upperPost, floorIndex);
        }

        private void CreateFloorSections()
        {
            for (int index = 0; index < floorSections.Length; index++)
            {
                var section = new GameObject("Floor Section " + (index + 1).ToString("00"));
                section.transform.SetParent(transform, false);
                floorSections[index] = section.transform;
            }
        }

        private void ParentToFloorSection(GameObject target, int floorIndex)
        {
            if (target == null ||
                floorIndex < 0 ||
                floorIndex >= floorSections.Length ||
                floorSections[floorIndex] == null)
            {
                return;
            }

            target.transform.SetParent(floorSections[floorIndex], true);
        }

        private void UpdateFloorStreaming(bool force = false)
        {
            if (player == null)
            {
                return;
            }

            int playerFloor = Mathf.Clamp(
                Mathf.FloorToInt(
                    (player.transform.position.y - PlatformStartY) / PlatformSpacing),
                0,
                PlatformCount - 1);
            int firstActiveFloor = Mathf.Max(0, playerFloor - FloorsBelowPlayerToKeepActive);
            int lastActiveFloor = Mathf.Min(
                PlatformCount - 1,
                playerFloor + FloorsAbovePlayerToKeepActive);
            int activeCount = 0;

            for (int index = 0; index < floorSections.Length; index++)
            {
                Transform section = floorSections[index];
                if (section == null)
                {
                    continue;
                }

                bool shouldBeActive = index >= firstActiveFloor && index <= lastActiveFloor;
                if (force || section.gameObject.activeSelf != shouldBeActive)
                {
                    section.gameObject.SetActive(shouldBeActive);
                }

                if (shouldBeActive)
                {
                    activeCount++;
                }
            }

            ActiveFloorSectionCount = activeCount;
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

        private static GameObject CreateOneWayLanding(
            string objectName,
            Vector2 position,
            Vector2 scale,
            Color color)
        {
            var landing = new GameObject(objectName);
            landing.transform.position = position;
            landing.transform.localScale = scale;

            var renderer = landing.AddComponent<SpriteRenderer>();
            renderer.sprite = PrototypeVisuals.WhiteSprite;
            renderer.color = color;

            var collider = landing.AddComponent<BoxCollider2D>();
            collider.usedByEffector = true;
            var effector = landing.AddComponent<PlatformEffector2D>();
            effector.surfaceArc = 160f;

            var body = landing.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Static;
            return landing;
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

        private void ActivateCheckpoint(int floorIndex)
        {
            int reachedCheckpoint = (floorIndex + 1) / 10;

            if (reachedCheckpoint <= checkpointLevel)
            {
                return;
            }

            checkpointLevel = reachedCheckpoint;
            float checkpointX = platformXPattern[floorIndex % platformXPattern.Length];
            float checkpointY = PlatformY(floorIndex) + 1.1f;
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

            Feedback = "PRESS E TO ENTER THE FINAL BOSS ROOM";
            feedbackUntil = Time.time + 0.2f;
        }

        private float GetFallLimit()
        {
            return checkpointLevel == 0
                ? -7f
                : (checkpointLevel * PlatformSpacing * 10f) - 8f;
        }

        private void OnGUI()
        {
            InitializeGuiStyles();
            if (skillMenuOpen)
            {
                DrawSkillMenu();
                return;
            }

            GUI.Label(
                new Rect(24f, 18f, 600f, 40f),
                "REVERSE BELL - VERTICAL TOWER",
                titleStyle);
            GUI.Label(
                new Rect(26f, 62f, 650f, 90f),
                "MOVE  LEFT/RIGHT    JUMP  UP    ATTACK  Z    PARRY  X    SPECIAL  C    INTERACT  E\n" +
                "CLIMB TO THE BELL AT THE TOP",
                bodyStyle);

            string beatText = rhythmClock == null ? "-" : (rhythmClock.CurrentBeat + 1).ToString();
            int floor = Mathf.Clamp(
                Mathf.FloorToInt((player.transform.position.y + 4f) / PlatformSpacing) + 1,
                1,
                PlatformCount);
            GUI.Label(
                new Rect(Screen.width - 350f, 24f, 320f, 230f),
                "HP  " + Health + " / " + PlayerMaximumHealth +
                "\nSCORE  " + Score +
                "\nLIVES  " + RevivesRemaining +
                "\nCOMBO  x" + Combo +
                "\nRESONANCE  " + ResonanceLevel + " / 4" +
                (ResonanceLevel >= 4 && player.SpecialCooldownRemaining <= 0f
                    ? "  SPECIAL READY"
                    : string.Empty) +
                "\nSPECIAL CD  " + player.SpecialCooldownRemaining.ToString("0.0") + "s" +
                (TestModeEnabled ? "\nTEST MODE  ON (F4)" : "\nTEST MODE  OFF (F4)") +
                "\nENEMIES  " + EnemiesRemaining +
                "\nFLOOR  " + floor + " / " + PlatformCount +
                "\nBEAT  " + beatText,
                bodyStyle);

            GUI.Label(
                new Rect((Screen.width * 0.5f) - 250f, 24f, 500f, 60f),
                Feedback,
                centerStyle);
        }

        private void DrawSkillMenu()
        {
            GUI.Box(new Rect(0f, 0f, Screen.width, Screen.height), GUIContent.none);
            GUI.Label(
                new Rect((Screen.width * 0.5f) - 260f, 24f, 520f, 42f),
                skillMenuAllowsUnlock
                    ? "BELL ALTAR - UNLOCK AND EQUIP SKILLS"
                    : "PAUSED - SKILL LOADOUT",
                centerStyle);
            GUI.Label(
                new Rect((Screen.width * 0.5f) - 260f, 65f, 520f, 32f),
                "SKILL POINTS: " + SkillPoints +
                (skillMenuAllowsUnlock
                    ? "    E / ESC: CLOSE"
                    : "    ESC: RESUME    (UNLOCK AT 10F ALTARS)"),
                centerStyle);

            TowerSkillId[] passiveSkills =
            {
                TowerSkillId.SwordMastery,
                TowerSkillId.IronWill,
                TowerSkillId.ResonantGuard,
                TowerSkillId.ArcaneEdge,
                TowerSkillId.BloodOath
            };
            string[] passiveNames =
            {
                "Sword Mastery  (+2 sword damage)",
                "Iron Will  (-1 damage taken)",
                "Resonant Guard  (+1 parry resonance)",
                "Arcane Edge  (+4 active damage)",
                "Blood Oath  (heal 1 on kill)"
            };

            TowerSkillId[] activeSkills =
            {
                TowerSkillId.ResonanceSlash,
                TowerSkillId.HealingBell,
                TowerSkillId.DemonBreaker,
                TowerSkillId.TimeAegis,
                TowerSkillId.Execution
            };
            string[] activeNames =
            {
                "Resonance Slash  (area slash)",
                "Healing Bell  (heal 5 HP)",
                "Demon Breaker  (wide heavy strike)",
                "Time Aegis  (3 sec invincibility)",
                "Execution  (focused massive hit)"
            };

            DrawSkillColumn(
                new Rect((Screen.width * 0.5f) - 470f, 110f, 440f, 390f),
                "PASSIVE SKILLS",
                passiveSkills,
                passiveNames,
                false,
                skillMenuAllowsUnlock);
            DrawSkillColumn(
                new Rect((Screen.width * 0.5f) + 30f, 110f, 440f, 390f),
                "ACTIVE SKILLS - ONLY ONE EQUIPPED TO C",
                activeSkills,
                activeNames,
                true,
                skillMenuAllowsUnlock);
        }

        private void DrawSkillColumn(
            Rect area,
            string heading,
            TowerSkillId[] skills,
            string[] names,
            bool active,
            bool allowUnlock)
        {
            GUI.Box(area, GUIContent.none);
            GUI.Label(new Rect(area.x + 15f, area.y + 12f, area.width - 30f, 30f), heading, titleStyle);
            for (int index = 0; index < skills.Length; index++)
            {
                TowerSkillId skill = skills[index];
                bool unlocked = IsSkillUnlocked(skill);
                bool equipped = active && equippedActiveSkill == skill;
                string state = equipped
                    ? " [EQUIPPED]"
                    : unlocked
                        ? active ? " [SELECT]" : " [UNLOCKED]"
                        : allowUnlock ? " [1 POINT]" : " [LOCKED - ALTAR]";
                Rect buttonRect = new Rect(
                    area.x + 18f,
                    area.y + 58f + (index * 58f),
                    area.width - 36f,
                    44f);
                if (GUI.Button(buttonRect, names[index] + state))
                {
                    if (unlocked || allowUnlock)
                    {
                        UnlockOrEquipSkill(skill);
                    }
                }
            }
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

        private sealed class EnemySpawnDefinition
        {
            public EnemySpawnDefinition(
                string name,
                Vector2 position,
                EnemyWeaponType weaponType,
                int floorIndex,
                int bossTier)
            {
                Name = name;
                Position = position;
                WeaponType = weaponType;
                FloorIndex = floorIndex;
                BossTier = bossTier;
            }

            public string Name { get; }
            public Vector2 Position { get; }
            public EnemyWeaponType WeaponType { get; }
            public int FloorIndex { get; }
            public int BossTier { get; }
        }
    }
}
