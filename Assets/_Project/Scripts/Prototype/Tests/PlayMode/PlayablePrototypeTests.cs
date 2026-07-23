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
            Assert.That(Object.FindFirstObjectByType<PlayerAvatar>(), Is.Not.Null);
            Assert.That(Object.FindFirstObjectByType<EnemyRhythmAttacker>(), Is.Not.Null);

            Object.Destroy(root);
        }
    }
}

