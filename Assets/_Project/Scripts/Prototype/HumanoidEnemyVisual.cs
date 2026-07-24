using UnityEngine;

namespace ReverseBell.Prototype
{
    public sealed class HumanoidEnemyVisual : MonoBehaviour
    {
        private static readonly Color BodyColor = new Color(0.72f, 0.18f, 0.2f);
        private static readonly Color WarningColor = new Color(1f, 0.65f, 0.12f);
        private static readonly Color SteelColor = new Color(0.78f, 0.84f, 0.9f);
        private static readonly Color WoodColor = new Color(0.36f, 0.2f, 0.1f);

        private Transform visualRoot;
        private Transform weaponRoot;
        private SpriteRenderer[] bodyRenderers;
        private float facing = -1f;

        public void Build(EnemyWeaponType weaponType)
        {
            visualRoot = new GameObject("Humanoid Visual").transform;
            visualRoot.SetParent(transform, false);

            CreatePart("Head", new Vector2(0f, 0.48f), new Vector2(0.44f, 0.44f), PrototypeVisuals.CircleSprite, BodyColor, 3);
            CreatePart("Torso", new Vector2(0f, 0.02f), new Vector2(0.3f, 0.68f), PrototypeVisuals.WhiteSprite, BodyColor, 2);
            CreatePart("Rear Arm", new Vector2(-0.18f, 0.05f), new Vector2(0.17f, 0.58f), PrototypeVisuals.WhiteSprite, BodyColor, 1);
            CreatePart("Front Arm", new Vector2(0.2f, 0.08f), new Vector2(0.17f, 0.58f), PrototypeVisuals.WhiteSprite, BodyColor, 4);
            CreatePart("Left Leg", new Vector2(-0.13f, -0.5f), new Vector2(0.19f, 0.6f), PrototypeVisuals.WhiteSprite, BodyColor, 1);
            CreatePart("Right Leg", new Vector2(0.13f, -0.5f), new Vector2(0.19f, 0.6f), PrototypeVisuals.WhiteSprite, BodyColor, 1);

            weaponRoot = new GameObject(weaponType + " Weapon").transform;
            weaponRoot.SetParent(visualRoot, false);
            weaponRoot.localPosition = new Vector3(0.36f, 0.1f, 0f);

            switch (weaponType)
            {
                case EnemyWeaponType.Spear:
                    BuildSpear();
                    break;
                case EnemyWeaponType.Shield:
                    BuildShield();
                    break;
                default:
                    BuildSword();
                    break;
            }

            bodyRenderers = visualRoot.GetComponentsInChildren<SpriteRenderer>();
            SetFacing(-1f);
        }

        public void SetFacing(float direction)
        {
            if (Mathf.Abs(direction) > 0.01f)
            {
                facing = Mathf.Sign(direction);
            }

            visualRoot.localScale = new Vector3(facing, 1f, 1f);
        }

        public void SetTelegraph(bool active)
        {
            foreach (SpriteRenderer renderer in bodyRenderers)
            {
                if (renderer.transform.IsChildOf(weaponRoot))
                {
                    continue;
                }

                renderer.color = active ? WarningColor : BodyColor;
            }

            weaponRoot.localRotation = Quaternion.Euler(0f, 0f, active ? 42f : 12f);
        }

        public void PlayAttack()
        {
            weaponRoot.localRotation = Quaternion.Euler(0f, 0f, -72f);
        }

        public void ResetAttack()
        {
            SetTelegraph(false);
            weaponRoot.localRotation = Quaternion.Euler(0f, 0f, 12f);
        }

        private void BuildSword()
        {
            CreateWeaponPart("Sword Blade", new Vector2(0.38f, 0.22f), new Vector2(0.12f, 1.05f), SteelColor, 5);
            CreateWeaponPart("Sword Hilt", new Vector2(0.18f, -0.18f), new Vector2(0.42f, 0.12f), WoodColor, 6);
        }

        private void BuildSpear()
        {
            CreateWeaponPart("Spear Shaft", new Vector2(0.45f, 0.05f), new Vector2(0.1f, 1.7f), WoodColor, 5);
            GameObject tip = CreateWeaponPart("Spear Tip", new Vector2(0.86f, 0.72f), new Vector2(0.2f, 0.38f), SteelColor, 6);
            tip.transform.localRotation = Quaternion.Euler(0f, 0f, -35f);
        }

        private void BuildShield()
        {
            CreateWeaponPart("Short Sword", new Vector2(0.36f, 0.2f), new Vector2(0.11f, 0.8f), SteelColor, 4);
            GameObject shield = CreateWeaponPart(
                "Round Shield",
                new Vector2(-0.2f, 0.02f),
                new Vector2(0.7f, 0.82f),
                new Color(0.35f, 0.42f, 0.52f),
                7,
                PrototypeVisuals.CircleSprite);
            CreatePartOn(
                shield.transform,
                "Shield Boss",
                Vector2.zero,
                new Vector2(0.22f, 0.22f),
                PrototypeVisuals.CircleSprite,
                SteelColor,
                8);
        }

        private GameObject CreateWeaponPart(
            string partName,
            Vector2 localPosition,
            Vector2 localScale,
            Color color,
            int sortingOrder,
            Sprite sprite = null)
        {
            return CreatePartOn(
                weaponRoot,
                partName,
                localPosition,
                localScale,
                sprite ?? PrototypeVisuals.WhiteSprite,
                color,
                sortingOrder);
        }

        private GameObject CreatePart(
            string partName,
            Vector2 localPosition,
            Vector2 localScale,
            Sprite sprite,
            Color color,
            int sortingOrder)
        {
            return CreatePartOn(visualRoot, partName, localPosition, localScale, sprite, color, sortingOrder);
        }

        private static GameObject CreatePartOn(
            Transform parent,
            string partName,
            Vector2 localPosition,
            Vector2 localScale,
            Sprite sprite,
            Color color,
            int sortingOrder)
        {
            var part = new GameObject(partName);
            part.transform.SetParent(parent, false);
            part.transform.localPosition = localPosition;
            part.transform.localScale = localScale;

            var renderer = part.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            renderer.sortingOrder = sortingOrder;
            return part;
        }
    }
}

