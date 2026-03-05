using TomatoFighters.Editor.Prefabs;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace TomatoFighters.Editor.Characters
{
    /// <summary>
    /// Creates a Corn Knight enemy prefab with Animator wired to the generated override controller.
    /// Run via menu: <b>TomatoFighters > Create CornKnight Prefab</b>.
    /// </summary>
    public static class CornKnightEnemyCreator
    {
        [MenuItem("TomatoFighters/Create CornKnight Prefab")]
        public static void Create()
        {
            var whiteSquare = TestDummyPrefabCreator.GetOrCreateWhiteSquareSprite();

            string overridePath = "Assets/Animations/Enemies/CornKnight/CornKnight_Override.overrideController";
            var overrideController = AssetDatabase.LoadAssetAtPath<AnimatorOverrideController>(overridePath);
            if (overrideController == null)
            {
                Debug.LogError(
                    $"[CornKnightEnemyCreator] Override controller not found at {overridePath}. " +
                    "Run 'TomatoFighters > Build Animations > All Characters' first.");
                return;
            }

            var config = new EnemyPrefabConfig
            {
                prefabPath = "Assets/Prefabs/Enemies/CornKnight.prefab",
                enemyType = "CornKnight",
                animatorController = overrideController,
                bodySprite = whiteSquare,
                spriteColor = new Color(1f, 0.85f, 0.2f), // Yellow-corn color
                bodySize = new Vector2(0.8f, 1.4f),
                bodyOffset = new Vector2(0f, 0.1f),
            };

            EnemyPrefabCreator.CreateEnemyPrefab(config);
        }
    }
}
