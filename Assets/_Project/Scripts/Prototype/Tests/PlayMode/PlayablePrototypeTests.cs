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
            Assert.That(GameObject.Find("Enemy"), Is.Not.Null);
            Assert.That(GameObject.Find("Ground"), Is.Not.Null);
            Assert.That(GameObject.Find("Tower Platform 16"), Is.Not.Null);
            Assert.That(GameObject.Find("Bell Chamber Floor"), Is.Not.Null);
            Assert.That(GameObject.Find("Tower Bell"), Is.Not.Null);
            Assert.That(Object.FindFirstObjectByType<PlayerAvatar>(), Is.Not.Null);
            Assert.That(Object.FindFirstObjectByType<EnemyRhythmAttacker>(), Is.Not.Null);
            Assert.That(Object.FindFirstObjectByType<VerticalCameraFollow>(), Is.Not.Null);

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
    }
}
