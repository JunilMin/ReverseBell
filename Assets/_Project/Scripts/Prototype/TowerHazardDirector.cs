using UnityEngine;

namespace ReverseBell.Prototype
{
    public sealed class TowerHazardDirector : MonoBehaviour
    {
        private const float LeftWallInnerEdge = -8.15f;
        private const float RightWallInnerEdge = 8.15f;

        private PrototypeGame game;
        private PlayerAvatar player;
        private float nextSpawnAt;
        private int spawnCount;

        public void Initialize(PrototypeGame owner, PlayerAvatar target)
        {
            game = owner;
            player = target;
            nextSpawnAt = Time.time + 3f;
        }

        public void ResetDirector()
        {
            spawnCount = 0;
            nextSpawnAt = Time.time + 3f;
        }

        private void Update()
        {
            if (game == null || player == null || Time.time < nextSpawnAt)
            {
                return;
            }

            int floor = Mathf.Max(1, Mathf.FloorToInt((player.transform.position.y + 4f) / 2.35f) + 1);
            if (floor < 6)
            {
                nextSpawnAt = Time.time + 1f;
                return;
            }

            int volleyCount = Mathf.Clamp(1 + (floor / 18), 1, 3);
            for (int volleyIndex = 0; volleyIndex < volleyCount; volleyIndex++)
            {
                SpawnHazard(floor, volleyIndex);
            }
            float difficulty = Mathf.InverseLerp(6f, 50f, floor);
            nextSpawnAt = Time.time + Mathf.Lerp(3.4f, 1.25f, difficulty);
        }

        private void SpawnHazard(int floor, int volleyIndex)
        {
            bool fromLeft = spawnCount % 2 == 0;
            bool isRock = spawnCount % 3 == 2;
            spawnCount++;

            float spawnX = fromLeft ? LeftWallInnerEdge : RightWallInnerEdge;
            float spawnY = player.transform.position.y +
                Random.Range(-0.9f, 1.2f) +
                ((volleyIndex - 1) * 0.35f);
            Vector2 origin = new Vector2(spawnX, spawnY);
            Vector2 target = (Vector2)player.transform.position +
                new Vector2(player.FacingDirection * 0.25f, 0.1f);
            Vector2 direction = (target - origin).normalized;

            var hazardObject = new GameObject(
                (isRock ? "Wall Rock" : "Wall Arrow") + " Floor " + floor);
            hazardObject.transform.position = origin;
            var projectile = hazardObject.AddComponent<WallTrapProjectile>();
            projectile.Initialize(
                game,
                player,
                direction,
                isRock ? 4.5f : 7.5f,
                isRock ? 3 : 2,
                isRock);
        }
    }
}
