using System.Collections;
using NUnit.Framework;
using ReverseBell.Prototype;
using UnityEngine;
using UnityEngine.TestTools;

namespace ReverseBell.Tests
{
    public sealed class PlayablePrototypeTests
    {
        [UnityTest]
        public IEnumerator PrototypeGame_CreatesPlayableStage()
        {
            var root = new GameObject("Prototype Test Root");
            PrototypeGame game = root.AddComponent<PrototypeGame>();

            yield return null;

            Assert.That(GameObject.Find("Player"), Is.Not.Null);
            Assert.That(GameObject.Find("Sword Guard"), Is.Not.Null);
            Assert.That(GameObject.Find("Spear Guard"), Is.Not.Null);
            Assert.That(game.GetEnemy(EnemyWeaponType.Shield), Is.Not.Null);
            Assert.That(GameObject.Find("Ground"), Is.Not.Null);
            Assert.That(game.FloorSectionCount, Is.EqualTo(50));
            Assert.That(game.ActiveFloorSectionCount, Is.EqualTo(8));
            Assert.That(GameObject.Find("Tower Bell"), Is.Not.Null);
            Assert.That(Object.FindFirstObjectByType<PlayerAvatar>(), Is.Not.Null);
            Assert.That(Object.FindFirstObjectByType<HeroSwordsmanVisual>(), Is.Not.Null);
            Assert.That(Object.FindFirstObjectByType<HumanoidMeleeEnemy>(), Is.Not.Null);
            Assert.That(Object.FindFirstObjectByType<HumanoidEnemyVisual>(), Is.Not.Null);
            Assert.That(HumanoidEnemyVisual.CachedAnimationCount, Is.EqualTo(14));
            Assert.That(HumanoidEnemyVisual.CachedSpriteCount, Is.LessThan(1000));
            VerticalCameraFollow cameraFollow =
                Object.FindFirstObjectByType<VerticalCameraFollow>();
            Assert.That(cameraFollow, Is.Not.Null);
            Assert.That(cameraFollow.MaximumY, Is.GreaterThan(100f));
            Assert.That(GameObject.Find("Rhythm Projectile"), Is.Null);

            HeroSwordsmanVisual hero = Object.FindFirstObjectByType<HeroSwordsmanVisual>();
            hero.SetPose(1f, 0f, true, HeroActionState.None);
            Assert.That(hero.FacesRight, Is.True);
            hero.SetPose(-1f, 0f, true, HeroActionState.None);
            Assert.That(hero.FacesRight, Is.False);

            var heroRenderer = hero.GetComponent<SpriteRenderer>();
            hero.SetPose(0f, 0f, true, HeroActionState.Attack);
            Sprite attackSprite = heroRenderer.sprite;
            hero.SetPose(0f, 0f, true, HeroActionState.Parry);
            Sprite parrySprite = heroRenderer.sprite;
            hero.SetPose(0f, 0f, true, HeroActionState.Special);
            Sprite specialSprite = heroRenderer.sprite;
            Assert.That(parrySprite, Is.Not.SameAs(attackSprite));
            Assert.That(specialSprite, Is.Not.SameAs(attackSprite));
            Assert.That(specialSprite, Is.Not.SameAs(parrySprite));
            Assert.That(GameObject.Find("Player").GetComponent<WorldHealthBar>(), Is.Not.Null);

            Object.Destroy(root);
        }

        [UnityTest]
        public IEnumerator DefeatedEnemy_IsDestroyedAndDoesNotRespawn()
        {
            var root = new GameObject("Enemy Death Test Root");
            root.AddComponent<PrototypeGame>();

            yield return null;

            HumanoidMeleeEnemy[] enemies =
                Object.FindObjectsByType<HumanoidMeleeEnemy>(FindObjectsSortMode.None);
            HumanoidMeleeEnemy target = null;

            foreach (HumanoidMeleeEnemy enemy in enemies)
            {
                if (enemy.WeaponType == EnemyWeaponType.Sword)
                {
                    target = enemy;
                    break;
                }
            }

            Assert.That(target, Is.Not.Null);

            while (target != null && target.Health > 0)
            {
                target.ReceivePlayerAttack(target.MaxHealth);
            }

            yield return null;
            Assert.That(target == null, Is.False);

            yield return new WaitForSeconds(4f);
            Assert.That(target == null, Is.True);

            Object.Destroy(root);
        }

        [UnityTest]
        public IEnumerator CombatProgress_ParriesBuildComboAndResonance()
        {
            var root = new GameObject("Combat Progress Test Root");
            var game = root.AddComponent<PrototypeGame>();

            yield return null;

            game.RegisterParry(ParryGrade.Good);
            game.RegisterParry(ParryGrade.Perfect);

            Assert.That(game.Combo, Is.EqualTo(2));
            Assert.That(game.ResonancePoints, Is.EqualTo(3));
            Assert.That(game.ResonanceLevel, Is.EqualTo(1));
            Assert.That(game.Score, Is.GreaterThan(0));

            game.TakeDamage();

            Assert.That(game.Combo, Is.Zero);
            Assert.That(game.ResonancePoints, Is.Zero);

            Object.Destroy(root);
        }

        [UnityTest]
        public IEnumerator SwordAttack_HitsOnlyEnemyInFacingDirection()
        {
            var root = new GameObject("Sword Attack Test Root");
            var game = root.AddComponent<PrototypeGame>();
            yield return null;

            HumanoidMeleeEnemy shield = game.GetEnemy(EnemyWeaponType.Shield);
            shield.transform.position =
                game.PlayerCharacter.transform.position + Vector3.right;
            int startingHealth = shield.Health;

            Assert.That(game.PerformPlayerAttack(1f), Is.True);
            Assert.That(shield.Health, Is.EqualTo(startingHealth - 3));
            Assert.That(game.PerformPlayerAttack(-1f), Is.False);

            Object.Destroy(root);
        }

        [UnityTest]
        public IEnumerator Special_RequiresAndConsumesFullResonance()
        {
            var root = new GameObject("Special Attack Test Root");
            var game = root.AddComponent<PrototypeGame>();
            yield return null;

            Assert.That(game.TryPerformSpecial(1f), Is.False);

            for (int index = 0; index < 6; index++)
            {
                game.RegisterParry(ParryGrade.Perfect);
            }

            HumanoidMeleeEnemy shield = game.GetEnemy(EnemyWeaponType.Shield);
            shield.transform.position =
                game.PlayerCharacter.transform.position + Vector3.right;
            int startingHealth = shield.Health;

            Assert.That(game.TryPerformSpecial(1f), Is.True);
            Assert.That(game.ResonancePoints, Is.Zero);
            Assert.That(shield.Health, Is.EqualTo(startingHealth - 8));

            Object.Destroy(root);
        }

        [UnityTest]
        public IEnumerator Parry_StunsAndPushesEnemyWithoutHealthDamage()
        {
            var root = new GameObject("Parry Stun Test Root");
            var game = root.AddComponent<PrototypeGame>();
            yield return null;

            HumanoidMeleeEnemy enemy = game.GetEnemy(EnemyWeaponType.Sword);
            int startingHealth = enemy.Health;
            float startingX = enemy.transform.position.x;

            enemy.ReceiveParry(ParryGrade.Perfect);

            Assert.That(enemy.Health, Is.EqualTo(startingHealth));
            Assert.That(enemy.IsStunned, Is.True);
            Assert.That(enemy.transform.position.x, Is.Not.EqualTo(startingX));

            yield return new WaitForSeconds(1.1f);
            Assert.That(enemy.IsStunned, Is.False);

            Object.Destroy(root);
        }

        [UnityTest]
        public IEnumerator WallTrap_DamagesPlayerWhenNotParried()
        {
            var root = new GameObject("Wall Trap Test Root");
            var game = root.AddComponent<PrototypeGame>();
            yield return null;

            int startingHealth = game.Health;
            var trapObject = new GameObject("Test Wall Arrow");
            var trap = trapObject.AddComponent<WallTrapProjectile>();
            trap.Initialize(game, game.PlayerCharacter, Vector2.right, 0f, 2, false);
            trap.ResolveImpact();

            Assert.That(game.Health, Is.EqualTo(startingHealth - 2));

            Object.Destroy(root);
        }

        [UnityTest]
        public IEnumerator Parry_CannotBeMaintainedByRepeatedInput()
        {
            var root = new GameObject("Parry Cooldown Test Root");
            var game = root.AddComponent<PrototypeGame>();
            yield return null;

            PlayerAvatar player = game.PlayerCharacter;
            Assert.That(player.TryStartParry(), Is.True);
            Assert.That(player.TryStartParry(), Is.False);
            Assert.That(player.ParryCooldownRemaining, Is.GreaterThan(0.6f));

            yield return new WaitForSeconds(0.14f);
            Assert.That(player.EvaluateParry(), Is.EqualTo(ParryGrade.None));
            Assert.That(player.TryStartParry(), Is.False);

            Object.Destroy(root);
        }

        [UnityTest]
        public IEnumerator Special_HasFiveSecondCooldown()
        {
            var root = new GameObject("Special Cooldown Test Root");
            var game = root.AddComponent<PrototypeGame>();
            yield return null;

            PlayerAvatar player = game.PlayerCharacter;
            Assert.That(player.TryStartSpecial(), Is.True);
            Assert.That(player.TryStartSpecial(), Is.False);
            Assert.That(player.SpecialCooldownRemaining, Is.GreaterThan(4.8f));

            Object.Destroy(root);
        }

        [UnityTest]
        public IEnumerator CombatFloors_LockUntilAllEnemiesAreDefeated()
        {
            var root = new GameObject("Combat Gate Test Root");
            var game = root.AddComponent<PrototypeGame>();
            yield return null;

            Assert.That(game.IsFloorGateLocked(0), Is.True);
            Assert.That(game.GetEnemyCountOnFloor(48), Is.EqualTo(5));

            HumanoidMeleeEnemy groundEnemy = game.GetEnemy(EnemyWeaponType.Sword);
            groundEnemy.ReceivePlayerAttack(groundEnemy.MaxHealth);

            Assert.That(game.GetEnemyCountOnFloor(0), Is.Zero);
            Assert.That(game.IsFloorGateLocked(0), Is.False);

            Object.Destroy(root);
        }

        [UnityTest]
        public IEnumerator ThreeRevivesThenFourthDeathReturnsToFloorOne()
        {
            var root = new GameObject("Lives Test Root");
            var game = root.AddComponent<PrototypeGame>();
            yield return null;

            game.PlayerCharacter.transform.position = new Vector3(0f, 50f, 0f);
            game.TakeDamage(game.MaxHealth);
            Assert.That(game.RevivesRemaining, Is.EqualTo(2));
            Assert.That(game.Health, Is.EqualTo(game.MaxHealth));
            Assert.That(game.PlayerCharacter.transform.position.y, Is.EqualTo(50f).Within(0.01f));

            for (int death = 0; death < 3; death++)
            {
                yield return new WaitForSeconds(1.3f);
                game.TakeDamage(game.MaxHealth);
            }

            Assert.That(game.RevivesRemaining, Is.EqualTo(3));
            Assert.That(game.PlayerCharacter.transform.position.y, Is.EqualTo(-2.8f).Within(0.01f));
            Assert.That(game.IsFloorGateLocked(0), Is.True);

            Object.Destroy(root);
        }
    }
}
