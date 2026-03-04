using TomatoFighters.Shared.Enums;
using UnityEngine;

namespace TomatoFighters.Characters
{
    /// <summary>
    /// Temporary debug component for switching characters at runtime via number keys.
    /// Attach to the same GameObject as <see cref="CharacterSpawner"/>.
    /// 1=Brutor, 2=Slasher, 3=Mystica, 4=Viper.
    /// </summary>
    [RequireComponent(typeof(CharacterSpawner))]
    public class CharacterSwitchDebugUI : MonoBehaviour
    {
        private CharacterSpawner spawner;

        private void Awake()
        {
            spawner = GetComponent<CharacterSpawner>();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Alpha1))
                spawner.SwitchCharacter(CharacterType.Brutor);
            else if (Input.GetKeyDown(KeyCode.Alpha2))
                spawner.SwitchCharacter(CharacterType.Slasher);
            else if (Input.GetKeyDown(KeyCode.Alpha3))
                spawner.SwitchCharacter(CharacterType.Mystica);
            else if (Input.GetKeyDown(KeyCode.Alpha4))
                spawner.SwitchCharacter(CharacterType.Viper);
        }

        private void OnGUI()
        {
            var style = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold
            };

            string current = spawner.SelectedCharacter.ToString();
            GUI.Label(new Rect(10, 10, 300, 25), $"Character: {current}", style);
            GUI.Label(new Rect(10, 30, 400, 25), "Press 1-4 to switch", GUI.skin.label);
        }
    }
}
