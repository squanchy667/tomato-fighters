using TomatoFighters.Editor.Prefabs;
using TomatoFighters.Shared.Enums;
using UnityEditor;

namespace TomatoFighters.Editor.Characters
{
    /// <summary>
    /// Creates a movement test scene pre-wired with the Slasher prefab.
    /// Run via menu: <b>TomatoFighters > Characters > Create Slasher Movement Scene</b>.
    /// </summary>
    public static class SlasherMovementTestSceneCreator
    {
        private const string PREFAB_PATH = "Assets/Prefabs/Player/Slasher.prefab";
        private const string SCENE_PATH = "Assets/Scenes/SlasherMovementTest.unity";

        [MenuItem("TomatoFighters/Characters/Create Slasher Movement Scene")]
        public static void CreateScene()
        {
            MovementTestSceneCreator.CreateTestScene(PREFAB_PATH, SCENE_PATH, CharacterType.Slasher);
        }
    }
}
