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
            root.AddComponent<PrototypeGame>();

            yield return null;

            Assert.That(GameObject.Find("Player"), Is.Not.Null);
            Assert.That(GameObject.Find("Sword Guard"), Is.Not.Null);
            Assert.That(GameObject.Find("Spear Guard"), Is.Not.Null);
            Assert.That(GameObject.Find("Shield Guard"), Is.Not.Null);
            Assert.That(GameObject.Find("Ground"), Is.Not.Null);
            Assert.That(GameObject.Find("Tower Platform 16"), Is.Not.Null);
            Assert.That(GameObject.Find("Bell Chamber Floor"), Is.Not.Null);
            Assert.That(GameObject.Find("Tower Bell"), Is.Not.Null);
            Assert.That(Object.FindFirstObjectByType<PlayerAvatar>(), Is.Not.Null);
            Assert.That(Object.FindFirstObjectByType<HeroSwordsmanVisual>(), Is.Not.Null);
            Assert.That(Object.FindFirstObjectByType<HumanoidMeleeEnemy>(), Is.Not.Null);
            Assert.That(Object.FindFirstObjectByType<HumanoidEnemyVisual>(), Is.Not.Null);
            Assert.That(Object.FindFirstObjectByType<VerticalCameraFollow>(), Is.Not.Null);
            Assert.That(GameObject.Find("Rhythm Projectile"), Is.Null);

            HeroSwordsmanVisual hero = Object.FindFirstObjectByType<HeroSwordsmanVisual>();
            hero.SetPose(1f, 0f, true, false);
            Assert.That(hero.FacesRight, Is.True);
            hero.SetPose(-1f, 0f, true, false);
            Assert.That(hero.FacesRight, Is.False);

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
                target.ReceiveParry(ParryGrade.Perfect);
            }

            yield return null;
            Assert.That(target == null, Is.True);

            yield return new WaitForSeconds(2f);
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
            Assert.That(shield.Health, Is.EqualTo(startingHealth - 1));
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
            Assert.That(shield.Health, Is.EqualTo(startingHealth - 3));

            Object.Destroy(root);
        }
    }
}
