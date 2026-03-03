using TomatoFighters.Combat;
using TomatoFighters.Shared.Data;
using UnityEditor;
using UnityEngine;

namespace TomatoFighters.Editor
{
    /// <summary>
    /// One-click Mystica setup: creates hitbox children on the Player prefab,
    /// wires HitboxManager, and assigns hitboxId on all Mystica AttackData assets.
    /// Run via menu: <b>Tools > TomatoFighters > Setup Mystica Character</b>.
    /// Select the Player root GameObject in prefab mode first.
    /// </summary>
    public static class SetupMysticaCharacter
    {
        private const string HITBOX_LAYER = "PlayerHitbox";
        private const string ATTACKS_FOLDER = "Assets/ScriptableObjects/Attacks/Mystica";
        private const float BASE_ATTACK = 10f;

        [MenuItem("Tools/TomatoFighters/Setup Mystica Character")]
        public static void Execute()
        {
            var root = Selection.activeGameObject;
            if (root == null)
            {
                Debug.LogError("[SetupMystica] Select the Player root GameObject in prefab mode first.");
                return;
            }

            int layer = LayerMask.NameToLayer(HITBOX_LAYER);
            if (layer < 0)
            {
                Debug.LogError(
                    $"[SetupMystica] Layer '{HITBOX_LAYER}' not found. " +
                    "Add it in Edit > Project Settings > Tags and Layers first.");
                return;
            }

            Debug.Log($"[SetupMystica] Setting up Mystica on '{root.name}'...");

            int hitboxes = CreateHitboxChildren(root, layer);
            WireHitboxManager(root);
            int attacks = AssignHitboxIds();

            EditorUtility.SetDirty(root);
            AssetDatabase.SaveAssets();

            Debug.Log(
                $"[SetupMystica] Done! " +
                $"Created {hitboxes} hitbox children, " +
                $"assigned {attacks} hitboxIds. " +
                "Save the prefab (Ctrl+S).");
        }

        // ── Hitbox Children ──────────────────────────────────────────────

        private static int CreateHitboxChildren(GameObject root, int layer)
        {
            int created = 0;

            // Hitbox_Burst: Magic Burst 1 & 2 — small circle slightly in front
            created += CreateCircle(root, layer, "Hitbox_Burst",
                radius: 0.5f, offsetX: 0.5f, offsetY: 0.1f);

            // Hitbox_BigBurst: Magic Burst 3 (finisher) — wider circle
            created += CreateCircle(root, layer, "Hitbox_BigBurst",
                radius: 0.8f, offsetX: 0.4f, offsetY: 0.1f);

            // Hitbox_Bolt: Arcane Bolt & Empowered Bolt — long narrow box
            created += CreateBox(root, layer, "Hitbox_Bolt",
                sizeX: 1.6f, sizeY: 0.35f, offsetX: 1.1f, offsetY: 0.15f);

            return created;
        }

        private static int CreateCircle(GameObject root, int layer,
            string name, float radius, float offsetX, float offsetY)
        {
            if (root.transform.Find(name) != null)
            {
                Debug.Log($"[SetupMystica] '{name}' already exists, skipping.");
                return 0;
            }

            var go = new GameObject(name);
            go.transform.SetParent(root.transform, false);
            go.transform.localPosition = Vector3.zero;
            go.layer = layer;

            var col = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = radius;
            col.offset = new Vector2(offsetX, offsetY);

            go.AddComponent<HitboxDamage>();
            go.SetActive(false);

            Debug.Log($"[SetupMystica] Created '{name}' — Circle r={radius}, offset=({offsetX}, {offsetY})");
            return 1;
        }

        private static int CreateBox(GameObject root, int layer,
            string name, float sizeX, float sizeY, float offsetX, float offsetY)
        {
            if (root.transform.Find(name) != null)
            {
                Debug.Log($"[SetupMystica] '{name}' already exists, skipping.");
                return 0;
            }

            var go = new GameObject(name);
            go.transform.SetParent(root.transform, false);
            go.transform.localPosition = Vector3.zero;
            go.layer = layer;

            var col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(sizeX, sizeY);
            col.offset = new Vector2(offsetX, offsetY);

            go.AddComponent<HitboxDamage>();
            go.SetActive(false);

            Debug.Log($"[SetupMystica] Created '{name}' — Box ({sizeX}x{sizeY}), offset=({offsetX}, {offsetY})");
            return 1;
        }

        // ── HitboxManager Wiring ─────────────────────────────────────────

        private static void WireHitboxManager(GameObject root)
        {
            var manager = root.GetComponent<HitboxManager>();
            if (manager == null)
                manager = root.AddComponent<HitboxManager>();

            // Wire ComboController reference via SerializedObject
            var so = new SerializedObject(manager);
            var comboProp = so.FindProperty("comboController");
            if (comboProp != null && comboProp.objectReferenceValue == null)
            {
                var combo = root.GetComponent<ComboController>();
                if (combo != null)
                {
                    comboProp.objectReferenceValue = combo;
                    Debug.Log("[SetupMystica] Wired ComboController → HitboxManager.");
                }
                else
                {
                    Debug.LogWarning("[SetupMystica] No ComboController found on root. Wire it manually.");
                }
            }

            var atkProp = so.FindProperty("baseAttack");
            if (atkProp != null)
                atkProp.floatValue = BASE_ATTACK;

            so.ApplyModifiedProperties();

            Debug.Log("[SetupMystica] HitboxManager component ready.");
        }

        // ── AttackData hitboxId Assignment ────────────────────────────────

        private static int AssignHitboxIds()
        {
            int updated = 0;

            updated += SetHitboxId("MysticaStrike1",       "Burst");
            updated += SetHitboxId("MysticaStrike2",       "Burst");
            updated += SetHitboxId("MysticaStrike3",       "BigBurst");
            updated += SetHitboxId("MysticaArcaneBolt",    "Bolt");
            updated += SetHitboxId("MysticaEmpoweredBolt", "Bolt");

            return updated;
        }

        private static int SetHitboxId(string assetName, string hitboxId)
        {
            string path = $"{ATTACKS_FOLDER}/{assetName}.asset";
            var attack = AssetDatabase.LoadAssetAtPath<AttackData>(path);

            if (attack == null)
            {
                Debug.LogWarning($"[SetupMystica] AttackData not found at '{path}'. Run 'Create Mystica Attacks' first.");
                return 0;
            }

            if (attack.hitboxId == hitboxId)
                return 0;

            attack.hitboxId = hitboxId;
            EditorUtility.SetDirty(attack);
            Debug.Log($"[SetupMystica] {assetName} → hitboxId='{hitboxId}'");
            return 1;
        }
    }
}
