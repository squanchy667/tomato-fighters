using System;
using TomatoFighters.Characters.Passives;
using TomatoFighters.Combat;
using TomatoFighters.Shared.Enums;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TomatoFighters.Editor.Prefabs
{
    public enum HitboxShape { Circle, Box }

    [Serializable]
    public struct HitboxDefinition
    {
        public string hitboxId;
        public HitboxShape shape;
        public float circleRadius;
        public Vector2 boxSize;
        public Vector2 offset;
    }

    /// <summary>
    /// Data class holding everything <see cref="PlayerPrefabCreator"/> needs
    /// to build a character prefab. Character-specific creators populate this
    /// and pass it to <see cref="PlayerPrefabCreator.CreatePlayerPrefab"/>.
    /// </summary>
    public class CharacterPrefabConfig
    {
        public string prefabPath;
        public CharacterType characterType;
        public MovementConfig movementConfig;
        public ComboDefinition comboDefinition;
        public RuntimeAnimatorController animatorController;
        public InputActionAsset inputActions;
        public HitboxDefinition[] hitboxes;
        public DefenseConfig defenseConfig;
        public float baseAttack = 10f;
        public bool useTimerFallback = true;
        public float fallbackActiveDuration = 0.3f;
        public PassiveConfig passiveConfig;
    }
}
