using TomatoFighters.Editor.Prefabs;
using TomatoFighters.Shared.Enums;
using UnityEditor;

namespace TomatoFighters.Editor.Characters
{
    /// <summary>
    /// Creates a movement test scene pre-wired with the Mystica prefab.
    /// Run via menu: <b>TomatoFighters > Characters > Create Mystica Movement Scene</b>.
    /// </summary>
    public static class MysticaMovementTestSceneCreator
    {
        private const string PREFAB_PATH = "Assets/Prefabs/Player/Mystica.prefab";
        private const string SCENE_PATH = "Assets/Scenes/MysticaMovementTest.unity";

        [MenuItem("TomatoFighters/Characters/Create Mystica Movement Scene")]
        public static void CreateScene()
        {
            MovementTestSceneCreator.CreateTestScene(PREFAB_PATH, SCENE_PATH, CharacterType.Mystica);
        }
    }
}
