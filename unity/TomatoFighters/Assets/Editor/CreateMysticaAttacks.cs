using TomatoFighters.Shared.Data;
using TomatoFighters.Shared.Enums;
using UnityEditor;
using UnityEngine;

namespace TomatoFighters.Editor
{
    /// <summary>
    /// Programmatically creates Mystica's 4 AttackData assets.
    /// Run once via menu: <b>Tools &gt; TomatoFighters &gt; Create Mystica Attacks</b>.
    /// </summary>
    public static class CreateMysticaAttacks
    {
        private const string FOLDER = "Assets/ScriptableObjects/Attacks/Mystica";

        [MenuItem("Tools/TomatoFighters/Create Mystica Attacks")]
        public static void Execute()
        {
            EnsureFolderExists(FOLDER);

            CreateAttack(new AttackParams
            {
                fileName    = "MysticaStrike1",
                attackId    = "mystica_strike_1",
                attackName  = "Magic Burst 1",
                damageMultiplier   = 0.6f,
                knockbackForce     = new Vector2(1.5f, 0f),
                launchForce        = Vector2.zero,
                hitboxStartFrame   = 3,
                hitboxActiveFrames = 4,
                totalFrames        = 16,
                animationSpeed     = 1.0f,
                telegraphType      = TelegraphType.Normal,
                causesWallBounce   = false,
                causesLaunch       = false,
                isOTGCapable       = false,
                isAirAttack        = false
            });

            CreateAttack(new AttackParams
            {
                fileName    = "MysticaStrike2",
                attackId    = "mystica_strike_2",
                attackName  = "Magic Burst 2",
                damageMultiplier   = 0.8f,
                knockbackForce     = new Vector2(2.0f, 0.3f),
                launchForce        = Vector2.zero,
                hitboxStartFrame   = 3,
                hitboxActiveFrames = 4,
                totalFrames        = 18,
                animationSpeed     = 1.0f,
                telegraphType      = TelegraphType.Normal,
                causesWallBounce   = false,
                causesLaunch       = false,
                isOTGCapable       = false,
                isAirAttack        = false
            });

            CreateAttack(new AttackParams
            {
                fileName    = "MysticaStrike3",
                attackId    = "mystica_strike_3",
                attackName  = "Magic Burst 3",
                damageMultiplier   = 1.0f,
                knockbackForce     = new Vector2(3.0f, 0.5f),
                launchForce        = Vector2.zero,
                hitboxStartFrame   = 4,
                hitboxActiveFrames = 5,
                totalFrames        = 22,
                animationSpeed     = 1.0f,
                telegraphType      = TelegraphType.Normal,
                causesWallBounce   = false,
                causesLaunch       = false,
                isOTGCapable       = false,
                isAirAttack        = false
            });

            CreateAttack(new AttackParams
            {
                fileName    = "MysticaArcaneBolt",
                attackId    = "mystica_arcane_bolt",
                attackName  = "Arcane Bolt",
                damageMultiplier   = 1.4f,
                knockbackForce     = new Vector2(2.0f, 1.0f),
                launchForce        = Vector2.zero,
                hitboxStartFrame   = 6,
                hitboxActiveFrames = 6,
                totalFrames        = 30,
                animationSpeed     = 1.0f,
                telegraphType      = TelegraphType.Normal,
                causesWallBounce   = false,
                causesLaunch       = false,
                isOTGCapable       = false,
                isAirAttack        = false
            });

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[CreateMysticaAttacks] Created 4 Mystica attack assets in " + FOLDER);
        }

        private struct AttackParams
        {
            public string fileName;
            public string attackId;
            public string attackName;
            public float damageMultiplier;
            public Vector2 knockbackForce;
            public Vector2 launchForce;
            public int hitboxStartFrame;
            public int hitboxActiveFrames;
            public int totalFrames;
            public float animationSpeed;
            public TelegraphType telegraphType;
            public bool causesWallBounce;
            public bool causesLaunch;
            public bool isOTGCapable;
            public bool isAirAttack;
        }

        private static void CreateAttack(AttackParams p)
        {
            string path = $"{FOLDER}/{p.fileName}.asset";

            var existing = AssetDatabase.LoadAssetAtPath<AttackData>(path);
            if (existing != null)
            {
                Debug.Log($"[CreateMysticaAttacks] {p.fileName} already exists, skipping.");
                return;
            }

            var attack = ScriptableObject.CreateInstance<AttackData>();

            attack.attackId           = p.attackId;
            attack.attackName         = p.attackName;
            attack.damageMultiplier   = p.damageMultiplier;
            attack.knockbackForce     = p.knockbackForce;
            attack.launchForce        = p.launchForce;
            attack.hitboxStartFrame   = p.hitboxStartFrame;
            attack.hitboxActiveFrames = p.hitboxActiveFrames;
            attack.totalFrames        = p.totalFrames;
            attack.animationSpeed     = p.animationSpeed;
            attack.telegraphType      = p.telegraphType;
            attack.causesWallBounce   = p.causesWallBounce;
            attack.causesLaunch       = p.causesLaunch;
            attack.isOTGCapable       = p.isOTGCapable;
            attack.isAirAttack        = p.isAirAttack;

            AssetDatabase.CreateAsset(attack, path);
            Debug.Log($"[CreateMysticaAttacks] Created {p.fileName} at {path}");
        }

        private static void EnsureFolderExists(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;

            var parts = path.Split('/');
            var current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                var next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }
    }
}
