using TMPro;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace SwiftRunner
{
    public static class SwiftRunnerVisualFactory
    {
        private static Sprite sharedSprite;

        private static Sprite SharedSprite
        {
            get
            {
                if (sharedSprite != null)
                {
                    return sharedSprite;
                }

                sharedSprite = Sprite.Create(
                    Texture2D.whiteTexture,
                    new Rect(0f, 0f, Texture2D.whiteTexture.width, Texture2D.whiteTexture.height),
                    new Vector2(0.5f, 0.5f),
                    100f);
                return sharedSprite;
            }
        }

        public static GameObject CreateBlock(string objectName, Transform parent, Vector3 position, Vector2 size, Color color, int sortingOrder)
        {
            var block = new GameObject(objectName);
            block.transform.SetParent(parent, false);
            block.transform.localPosition = position;

            var renderer = block.AddComponent<SpriteRenderer>();
            renderer.sprite = SharedSprite;
            renderer.color = color;
            renderer.sortingOrder = sortingOrder;

            block.transform.localScale = new Vector3(size.x, size.y, 1f);
            return block;
        }

        public static GameObject CreateTexturedBlock(
            string objectName,
            Transform parent,
            Vector3 position,
            Vector2 size,
            string spriteAssetPath,
            Color color,
            int sortingOrder,
            Color fallbackColor)
        {
            var sprite = LoadSprite(spriteAssetPath);
            if (sprite == null)
            {
                return CreateBlock(objectName, parent, position, size, fallbackColor, sortingOrder);
            }

            return CreateSpriteObject(objectName, parent, position, size, spriteAssetPath, color, sortingOrder);
        }

        public static GameObject CreateSpriteObject(
            string objectName,
            Transform parent,
            Vector3 position,
            Vector2 size,
            string spriteAssetPath,
            Color color,
            int sortingOrder,
            bool fallbackToBlock = true)
        {
            var sprite = LoadSprite(spriteAssetPath);
            if (sprite == null)
            {
                return fallbackToBlock
                    ? CreateBlock(objectName, parent, position, size, color, sortingOrder)
                    : CreateEmptySpriteHolder(objectName, parent, position, sortingOrder);
            }

            var spriteObject = new GameObject(objectName);
            spriteObject.transform.SetParent(parent, false);
            spriteObject.transform.localPosition = position;

            var renderer = spriteObject.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            renderer.sortingOrder = sortingOrder;

            var spriteSize = sprite.bounds.size;
            if (spriteSize.x <= 0.0001f || spriteSize.y <= 0.0001f)
            {
                spriteObject.transform.localScale = Vector3.one;
                return spriteObject;
            }

            spriteObject.transform.localScale = new Vector3(
                size.x / spriteSize.x,
                size.y / spriteSize.y,
                1f);
            return spriteObject;
        }

        public static GameObject CreateEnemyVisual(string objectName, Transform parent, Vector3 position, string spriteAssetPath, Color tint)
        {
            var enemy = new GameObject(objectName);
            enemy.transform.SetParent(parent, false);
            enemy.transform.localPosition = position;

            var outline = CreateBlock("Outline", enemy.transform, new Vector3(0f, -0.02f, 0f), new Vector2(1.12f, 1.86f), new Color(0.24f, 0.03f, 0.04f, 0.92f), 18);
            outline.transform.localScale = Vector3.one;

            var body = CreateTexturedBlock(
                "Body",
                enemy.transform,
                new Vector3(0f, 0.02f, 0f),
                new Vector2(1.02f, 1.76f),
                spriteAssetPath,
                tint,
                19,
                new Color(0.93f, 0.2f, 0.16f, 1f));
            body.transform.localScale = Vector3.one;

            var helm = CreateTexturedBlock(
                "Helm",
                enemy.transform,
                new Vector3(0f, 0.86f, 0f),
                new Vector2(0.42f, 0.24f),
                spriteAssetPath,
                new Color(1f, 0.4f, 0.2f, 0.4f),
                20,
                new Color(1f, 0.52f, 0.4f, 1f));
            helm.transform.localScale = Vector3.one;

            return enemy;
        }

        public static void CreateWorldLabel(string objectName, Transform parent, Vector3 position, string text, Color color, int sortingOrder)
        {
            var labelObject = new GameObject(objectName);
            labelObject.transform.SetParent(parent, false);
            labelObject.transform.localPosition = position;

            var textMesh = labelObject.AddComponent<TextMeshPro>();
            textMesh.text = text;
            textMesh.fontSize = 4f;
            textMesh.alignment = TextAlignmentOptions.Center;
            textMesh.color = color;
            textMesh.sortingOrder = sortingOrder;
            textMesh.font = TMP_Settings.defaultFontAsset;
        }

        public static Sprite LoadSprite(string spriteAssetPath)
        {
#if UNITY_EDITOR
            if (string.IsNullOrWhiteSpace(spriteAssetPath))
            {
                return null;
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(spriteAssetPath);
#else
            return null;
#endif
        }

        private static GameObject CreateEmptySpriteHolder(string objectName, Transform parent, Vector3 position, int sortingOrder)
        {
            var spriteObject = new GameObject(objectName);
            spriteObject.transform.SetParent(parent, false);
            spriteObject.transform.localPosition = position;

            var renderer = spriteObject.AddComponent<SpriteRenderer>();
            renderer.sortingOrder = sortingOrder;
            return spriteObject;
        }
    }
}
