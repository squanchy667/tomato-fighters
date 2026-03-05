using TomatoFighters.Combat;
using TomatoFighters.Shared.Data;
using TomatoFighters.World;
using UnityEngine;

namespace TomatoFighters.Editor.Prefabs
{
    /// <summary>
    /// Data class holding everything <see cref="EnemyPrefabCreator"/> needs
    /// to build an enemy prefab. Per-enemy creator scripts populate this
    /// and pass it to <see cref="EnemyPrefabCreator.CreateEnemyPrefab"/>.
    /// Parallel to <see cref="CharacterPrefabConfig"/> for players.
    /// </summary>
    public class EnemyPrefabConfig
    {
        public string prefabPath;
        public string enemyType;
        public EnemyData enemyDataAsset;
        public AttackData[] attackDatas;
        public DefenseConfig defenseConfig;
        public RuntimeAnimatorController animatorController;
        public Vector2 bodySize = new Vector2(0.8f, 1.2f);
        public Vector2 bodyOffset = new Vector2(0f, 0.1f);
        public HitboxDefinition[] hitboxDefinitions;
        public Color spriteColor = new Color(1f, 0.6f, 0.15f);
        public Sprite bodySprite;
    }
}
