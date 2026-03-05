using TomatoFighters.Editor.Prefabs;
using TomatoFighters.Shared.Enums;
using UnityEditor;

namespace TomatoFighters.Editor.Characters
{
    /// <summary>
    /// Creates a movement test scene pre-wired with the Viper prefab.
    /// Run via menu: <b>TomatoFighters > Characters > Create Viper Movement Scene</b>.
    /// </summary>
    public static class ViperMovementTestSceneCreator
    {
        private const string PREFAB_PATH = "Assets/Prefabs/Player/Viper.prefab";
        private const string SCENE_PATH = "Assets/Scenes/ViperMovementTest.unity";

        [MenuItem("TomatoFighters/Characters/Create Viper Movement Scene")]
        public static void CreateScene()
        {
            MovementTestSceneCreator.CreateTestScene(PREFAB_PATH, SCENE_PATH, CharacterType.Viper);
        }
    }
}
