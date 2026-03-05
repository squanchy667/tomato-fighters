using TomatoFighters.Editor.Prefabs;
using TomatoFighters.Shared.Enums;
using UnityEditor;

namespace TomatoFighters.Editor.Characters
{
    /// <summary>
    /// Creates a movement test scene pre-wired with the Brutor prefab.
    /// Run via menu: <b>TomatoFighters > Characters > Create Brutor Movement Scene</b>.
    /// </summary>
    public static class BrutorMovementTestSceneCreator
    {
        private const string PREFAB_PATH = "Assets/Prefabs/Player/Brutor.prefab";
        private const string SCENE_PATH = "Assets/Scenes/BrutorMovementTest.unity";

        [MenuItem("TomatoFighters/Characters/Create Brutor Movement Scene")]
        public static void CreateScene()
        {
            MovementTestSceneCreator.CreateTestScene(PREFAB_PATH, SCENE_PATH, CharacterType.Brutor);
        }
    }
}
