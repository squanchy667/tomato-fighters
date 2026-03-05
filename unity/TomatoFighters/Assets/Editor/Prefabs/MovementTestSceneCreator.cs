using TomatoFighters.Characters;
using TomatoFighters.Combat;
using TomatoFighters.Shared.Components;
using TomatoFighters.Shared.Data;
using TomatoFighters.Shared.Enums;
using TomatoFighters.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TomatoFighters.Editor.Prefabs
{
    /// <summary>
    /// Creates a test scene for verifying movement, combo system, animations, and combat.
    /// Run via menu: <b>TomatoFighters &gt; Create Movement Test Scene</b>.
    ///
    /// <para><b>Scene contents:</b></para>
    /// <list type="bullet">
    ///   <item>Orthographic camera (size 7, dark background)</item>
    ///   <item>20x10 arena with 4 invisible wall colliders</item>
    ///   <item>Dark green ground plane with grid lines for depth perception</item>
    ///   <item>Player — from prefab, with <see cref="PlayerDamageable"/> added for bidirectional damage</item>
    ///   <item>TestDummy enemy — from prefab, positioned right of center for combat testing</item>
    ///   <item>Controls hint text at top of arena</item>
    /// </list>
    ///
    /// <para><b>Input wiring:</b> InputActionReferences don't survive prefab serialization,
    /// so this script re-wires them on the scene instance after instantiation:
    /// Move=WASD, Jump=Space, Dash=L-Shift, Light=LMB, Heavy=C, Run=L-Ctrl.</para>
    ///
    /// <para><b>Fallback:</b> If the prefab doesn't exist, builds the player inline
    /// (without Animator or CharacterAnimationBridge). Run <c>Create Player Prefab</c> first.</para>
    /// </summary>
    public static class MovementTestSceneCreator
    {
        private const string SCENE_FOLDER = "Assets/Scenes";
        private const string SCENE_PATH = SCENE_FOLDER + "/MovementTest.unity";
        private const string PREFAB_PATH = "Assets/Prefabs/Player/Mystica.prefab";
        private const string DUMMY_PREFAB_PATH = "Assets/Prefabs/Enemies/TestDummy.prefab";
        private const string INPUT_ACTIONS_PATH = "Assets/InputSystem_Actions.inputactions";

        private const float ARENA_WIDTH = 20f;
        private const float ARENA_HEIGHT = 10f;
        private const float WALL_THICKNESS = 1f;

        /// <summary>
        /// Creates a movement test scene for a specific character prefab.
        /// Called by per-character scene creators.
        /// </summary>
        public static void CreateTestScene(string prefabPath, string scenePath, CharacterType characterType)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            SetupLayerCollisionMatrix();
            SetupCamera();
            CreateArenaBackground();
            CreateArenaWalls();
            CreatePlayerFromPrefab(prefabPath, characterType);
            CreateTestDummies();
            CreateDebugCanvas();

            PlayerPrefabCreator.EnsureFolderExists(SCENE_FOLDER);
            EditorSceneManager.SaveScene(scene, scenePath);

            Debug.Log($"[MovementTestScene] Scene created at {scenePath} for {characterType}");
            Debug.Log("[MovementTestScene] Controls: WASD = move, Space = jump, L-Shift = dash, LMB = light, C = heavy, L-Ctrl = run");
        }

        private static void SetupLayerCollisionMatrix()
        {
            int playerHitbox = LayerMask.NameToLayer("PlayerHitbox");
            int playerHurtbox = LayerMask.NameToLayer("PlayerHurtbox");
            int enemyHitbox = LayerMask.NameToLayer("EnemyHitbox");
            int enemyHurtbox = LayerMask.NameToLayer("EnemyHurtbox");

            if (playerHitbox < 0 || playerHurtbox < 0 || enemyHitbox < 0 || enemyHurtbox < 0)
            {
                Debug.LogError(
                    "[MovementTestScene] Missing physics layers. " +
                    "Add PlayerHitbox, PlayerHurtbox, EnemyHitbox, EnemyHurtbox in Tags and Layers.");
                return;
            }

            // Enable cross-team collisions
            Physics2D.IgnoreLayerCollision(playerHitbox, enemyHurtbox, false);
            Physics2D.IgnoreLayerCollision(enemyHitbox, playerHurtbox, false);

            // Disable same-team and self collisions
            Physics2D.IgnoreLayerCollision(playerHitbox, playerHurtbox, true);
            Physics2D.IgnoreLayerCollision(enemyHitbox, enemyHurtbox, true);
            Physics2D.IgnoreLayerCollision(playerHitbox, playerHitbox, true);
            Physics2D.IgnoreLayerCollision(enemyHitbox, enemyHitbox, true);
            Physics2D.IgnoreLayerCollision(playerHurtbox, playerHurtbox, true);
            Physics2D.IgnoreLayerCollision(enemyHurtbox, enemyHurtbox, true);

            Debug.Log("[MovementTestScene] Layer collision matrix configured.");
        }

        private static void SetupCamera()
        {
            var camGO = new GameObject("Main Camera");
            camGO.tag = "MainCamera";
            var cam = camGO.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 7f;
            cam.backgroundColor = new Color(0.15f, 0.15f, 0.2f);
            cam.clearFlags = CameraClearFlags.SolidColor;
            camGO.transform.position = new Vector3(0f, 0f, -10f);

            // Runtime diagnostic — validates damage pipeline on Play
            var diagType = System.Type.GetType("DamagePipelineDiagnostic, Assembly-CSharp");
            if (diagType != null)
                camGO.AddComponent(diagType);
            else
                Debug.LogWarning("[MovementTestScene] DamagePipelineDiagnostic not found — skipping.");
        }

        private const string ART_FOLDER = "Assets/Art/Environment/TestArena";

        private static void CreateArenaBackground()
        {
            var environment = new GameObject("Environment");

            // Background layers — full arena, stacked by sorting order
            CreateArtLayer(environment.transform, $"{ART_FOLDER}/bg_forest_distant.png",
                "BG_Distant", -100, Vector3.zero, ARENA_WIDTH, ARENA_HEIGHT);
            CreateArtLayer(environment.transform, $"{ART_FOLDER}/bg_forest_midground.png",
                "BG_Midground", -90, Vector3.zero, ARENA_WIDTH, ARENA_HEIGHT);
            CreateArtLayer(environment.transform, $"{ART_FOLDER}/bg_forest_foreground.png",
                "BG_Foreground", -80, Vector3.zero, ARENA_WIDTH, ARENA_HEIGHT);

            // Ground floor — anchored at bottom of arena
            var floorSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{ART_FOLDER}/ground_forest_floor.png");
            if (floorSprite != null)
            {
                float floorScaleY = ARENA_HEIGHT * 0.2f / floorSprite.bounds.size.y;
                float floorHeight = floorSprite.bounds.size.y * floorScaleY;
                CreateArtLayer(environment.transform, $"{ART_FOLDER}/ground_forest_floor.png",
                    "Ground_Floor", -50,
                    new Vector3(0f, -ARENA_HEIGHT / 2f + floorHeight / 2f, 0f),
                    ARENA_WIDTH, ARENA_HEIGHT * 0.2f);
            }

            // Stone wall visuals — visual only, no colliders (DD-6)
            var wallLeftSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{ART_FOLDER}/wall_left_stone.png");
            if (wallLeftSprite != null)
            {
                float wallScaleX = ARENA_WIDTH * 0.05f / wallLeftSprite.bounds.size.x;
                float wallWidth = wallLeftSprite.bounds.size.x * wallScaleX;
                CreateArtLayer(environment.transform, $"{ART_FOLDER}/wall_left_stone.png",
                    "Wall_Left_Visual", -40,
                    new Vector3(-ARENA_WIDTH / 2f + wallWidth / 2f, 0f, 0f),
                    ARENA_WIDTH * 0.05f, ARENA_HEIGHT);
            }

            var wallRightSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{ART_FOLDER}/wall_right_stone.png");
            if (wallRightSprite != null)
            {
                float wallScaleX = ARENA_WIDTH * 0.05f / wallRightSprite.bounds.size.x;
                float wallWidth = wallRightSprite.bounds.size.x * wallScaleX;
                CreateArtLayer(environment.transform, $"{ART_FOLDER}/wall_right_stone.png",
                    "Wall_Right_Visual", -40,
                    new Vector3(ARENA_WIDTH / 2f - wallWidth / 2f, 0f, 0f),
                    ARENA_WIDTH * 0.05f, ARENA_HEIGHT);
            }

            Debug.Log("[MovementTestScene] Art layer background created (6 sprites).");
        }

        /// <summary>
        /// Creates a SpriteRenderer child scaled to fit the target dimensions using sprite.bounds.size (DD-7).
        /// </summary>
        private static void CreateArtLayer(Transform parent, string spritePath, string goName,
            int sortingOrder, Vector3 position, float targetWidth, float targetHeight)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
            if (sprite == null)
            {
                Debug.LogWarning($"[MovementTestScene] Sprite not found: {spritePath} — using fallback color.");
                var fallback = new GameObject(goName);
                fallback.transform.SetParent(parent);
                fallback.transform.position = position;
                var fallbackSR = fallback.AddComponent<SpriteRenderer>();
                fallbackSR.sprite = CreateRectSprite();
                fallbackSR.color = new Color(0.2f, 0.25f, 0.15f);
                fallback.transform.localScale = new Vector3(targetWidth, targetHeight, 1f);
                fallbackSR.sortingOrder = sortingOrder;
                return;
            }

            var go = new GameObject(goName);
            go.transform.SetParent(parent);
            go.transform.position = position;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = sortingOrder;

            // Scale sprite to fit target dimensions (DD-7)
            float scaleX = targetWidth / sprite.bounds.size.x;
            float scaleY = targetHeight / sprite.bounds.size.y;
            go.transform.localScale = new Vector3(scaleX, scaleY, 1f);
        }

        private static void CreateArenaWalls()
        {
            var walls = new GameObject("Walls");

            CreateWall("Wall_Left", walls.transform,
                new Vector3(-ARENA_WIDTH / 2f - WALL_THICKNESS / 2f, 0f, 0f),
                new Vector2(WALL_THICKNESS, ARENA_HEIGHT + WALL_THICKNESS * 2));

            CreateWall("Wall_Right", walls.transform,
                new Vector3(ARENA_WIDTH / 2f + WALL_THICKNESS / 2f, 0f, 0f),
                new Vector2(WALL_THICKNESS, ARENA_HEIGHT + WALL_THICKNESS * 2));

            CreateWall("Wall_Top", walls.transform,
                new Vector3(0f, ARENA_HEIGHT / 2f + WALL_THICKNESS / 2f, 0f),
                new Vector2(ARENA_WIDTH + WALL_THICKNESS * 2, WALL_THICKNESS));

            CreateWall("Wall_Bottom", walls.transform,
                new Vector3(0f, -ARENA_HEIGHT / 2f - WALL_THICKNESS / 2f, 0f),
                new Vector2(ARENA_WIDTH + WALL_THICKNESS * 2, WALL_THICKNESS));
        }

        private static void CreateWall(string name, Transform parent, Vector3 position, Vector2 size)
        {
            var wall = new GameObject(name);
            wall.transform.SetParent(parent);
            wall.transform.position = position;
            var col = wall.AddComponent<BoxCollider2D>();
            col.size = size;
        }

        private static void CreatePlayerFromPrefab(string prefabPath, CharacterType characterType)
        {
            // Force reimport to ensure the Library cache is current
            AssetDatabase.ImportAsset(prefabPath, ImportAssetOptions.ForceUpdate);

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                Debug.LogWarning("[MovementTestScene] Player prefab not found. Run 'Create Player Prefab' first. Building inline fallback.");
                BuildInlineFallbackPlayer();
                return;
            }

            var player = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            player.transform.position = new Vector3(-3f, 0f, 0f);

            // InputActionReferences created via InputActionReference.Create() don't survive
            // prefab serialization — wire them on the scene instance directly.
            var inputActions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(INPUT_ACTIONS_PATH);
            var inputHandler = player.GetComponent<CharacterInputHandler>();
            if (inputActions != null && inputHandler != null)
            {
                var inputSO = new SerializedObject(inputHandler);
                WireAction(inputActions, inputSO, "moveAction", "Player/Move");
                WireAction(inputActions, inputSO, "jumpAction", "Player/Jump");
                WireAction(inputActions, inputSO, "dashAction", "Player/Sprint");
                WireAction(inputActions, inputSO, "lightAttackAction", "Player/Attack");
                WireAction(inputActions, inputSO, "heavyAttackAction", "Player/Crouch");
                WireAction(inputActions, inputSO, "runAction", "Player/Run");
                inputSO.ApplyModifiedPropertiesWithoutUndo();
            }

            // Add PlayerDamageable so enemy attacks can hit the player
            if (player.GetComponent<PlayerDamageable>() == null)
                player.AddComponent<PlayerDamageable>();

            // Temp debug HP bar (replaced by T025 HUD)
            if (player.GetComponent<DebugHealthBar>() == null)
            {
                var hpBar = player.AddComponent<DebugHealthBar>();
                var hbSO = new SerializedObject(hpBar);
                var fillColorProp = hbSO.FindProperty("fillColor");
                if (fillColorProp != null)
                    fillColorProp.colorValue = new Color(0.2f, 0.8f, 0.3f); // Green for player
                hbSO.ApplyModifiedPropertiesWithoutUndo();
            }

            // Defense debug UI — floating text on deflect/clash/dodge
            if (player.GetComponent<DefenseDebugUI>() == null)
            {
                var debugUI = player.AddComponent<DefenseDebugUI>();
                var defenseSystem = player.GetComponent<DefenseSystem>();
                if (defenseSystem != null)
                {
                    var debugSO = new SerializedObject(debugUI);
                    debugSO.FindProperty("defenseSystem").objectReferenceValue = defenseSystem;
                    debugSO.ApplyModifiedPropertiesWithoutUndo();
                }
            }

            Debug.Log("[MovementTestScene] Player instantiated with input actions + PlayerDamageable + DebugHealthBar + DefenseDebugUI.");
        }

        // ── 5 Tiered Dummies ─────────────────────────────────────────────

        private struct DummyTierConfig
        {
            public string name;
            public string label;
            public string attackAsset;
            public string attackId;
            public string attackName;
            public float damageMultiplier;
            public Vector2 knockbackForce;
            public Vector2 launchForce;
            public TelegraphType telegraphType;
            public float telegraphDuration;
            public float attackActiveDuration;
            public float attackInterval;
            public Color spriteColor;
            public Vector2 bodyScale;
            public Vector2 hitboxSize;
            public Vector2 hitboxOffset;
            public Vector3 position;
        }

        private static DummyTierConfig[] GetDummyTiers()
        {
            return new[]
            {
                // Tier 1 — Bruiser (strongest, attacks BOTH sides)
                new DummyTierConfig
                {
                    name = "Dummy_Bruiser",
                    label = "BRUISER [Both Sides]",
                    attackAsset = "DummyBruiser",
                    attackId = "dummy_bruiser",
                    attackName = "Bruiser Slam",
                    damageMultiplier = 2.0f,
                    knockbackForce = new Vector2(8f, 2f),
                    launchForce = new Vector2(0f, 3f),
                    telegraphType = TelegraphType.Unstoppable,
                    telegraphDuration = 1.5f,
                    attackActiveDuration = 2.0f,
                    attackInterval = 1.0f,
                    spriteColor = new Color(0.7f, 0.1f, 0.1f),   // Dark crimson
                    bodyScale = new Vector2(1.0f, 1.5f),
                    hitboxSize = new Vector2(2.4f, 1.0f),
                    hitboxOffset = new Vector2(0f, 0.1f),         // Centered — hits both sides
                    position = new Vector3(0f, 2.5f, 0f)
                },
                // Tier 2 — Heavy
                new DummyTierConfig
                {
                    name = "Dummy_Heavy",
                    label = "HEAVY",
                    attackAsset = "DummyHeavy",
                    attackId = "dummy_heavy",
                    attackName = "Heavy Swing",
                    damageMultiplier = 1.5f,
                    knockbackForce = new Vector2(5f, 1f),
                    launchForce = Vector2.zero,
                    telegraphType = TelegraphType.Unstoppable,
                    telegraphDuration = 1.2f,
                    attackActiveDuration = 1.5f,
                    attackInterval = 2.0f,
                    spriteColor = new Color(0.9f, 0.2f, 0.15f),  // Red
                    bodyScale = new Vector2(0.9f, 1.35f),
                    hitboxSize = new Vector2(1.0f, 0.8f),
                    hitboxOffset = new Vector2(-0.7f, 0.1f),
                    position = new Vector3(5f, -1.5f, 0f)
                },
                // Tier 3 — Fighter
                new DummyTierConfig
                {
                    name = "Dummy_Fighter",
                    label = "FIGHTER",
                    attackAsset = "DummyFighter",
                    attackId = "dummy_fighter",
                    attackName = "Fighter Jab",
                    damageMultiplier = 1.0f,
                    knockbackForce = new Vector2(3f, 0f),
                    launchForce = Vector2.zero,
                    telegraphType = TelegraphType.Normal,
                    telegraphDuration = 1.0f,
                    attackActiveDuration = 1.0f,
                    attackInterval = 3.0f,
                    spriteColor = new Color(1f, 0.6f, 0.15f),    // Orange (original)
                    bodyScale = new Vector2(0.8f, 1.2f),
                    hitboxSize = new Vector2(0.8f, 0.6f),
                    hitboxOffset = new Vector2(-0.6f, 0.1f),
                    position = new Vector3(3f, 1f, 0f)
                },
                // Tier 4 — Scrapper
                new DummyTierConfig
                {
                    name = "Dummy_Scrapper",
                    label = "SCRAPPER",
                    attackAsset = "DummyScrapper",
                    attackId = "dummy_scrapper",
                    attackName = "Scrapper Poke",
                    damageMultiplier = 0.6f,
                    knockbackForce = new Vector2(2f, 0f),
                    launchForce = Vector2.zero,
                    telegraphType = TelegraphType.Normal,
                    telegraphDuration = 0.7f,
                    attackActiveDuration = 0.7f,
                    attackInterval = 4.0f,
                    spriteColor = new Color(1f, 0.75f, 0.2f),    // Yellow-orange
                    bodyScale = new Vector2(0.7f, 1.1f),
                    hitboxSize = new Vector2(0.6f, 0.5f),
                    hitboxOffset = new Vector2(-0.5f, 0.1f),
                    position = new Vector3(7f, 1.5f, 0f)
                },
                // Tier 5 — Weakling (weakest)
                new DummyTierConfig
                {
                    name = "Dummy_Weakling",
                    label = "WEAKLING",
                    attackAsset = "DummyWeakling",
                    attackId = "dummy_weakling",
                    attackName = "Weakling Slap",
                    damageMultiplier = 0.3f,
                    knockbackForce = new Vector2(1f, 0f),
                    launchForce = Vector2.zero,
                    telegraphType = TelegraphType.Normal,
                    telegraphDuration = 0.5f,
                    attackActiveDuration = 0.5f,
                    attackInterval = 5.0f,
                    spriteColor = new Color(1f, 0.9f, 0.4f),     // Pale yellow
                    bodyScale = new Vector2(0.65f, 1.0f),
                    hitboxSize = new Vector2(0.5f, 0.4f),
                    hitboxOffset = new Vector2(-0.4f, 0.1f),
                    position = new Vector3(7f, -2.5f, 0f)
                }
            };
        }

        private static void CreateTestDummies()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(DUMMY_PREFAB_PATH);
            if (prefab == null)
            {
                Debug.LogWarning(
                    "[MovementTestScene] TestDummy prefab not found. " +
                    "Run 'TomatoFighters > Create TestDummy Prefab' first. Skipping enemy placement.");
                return;
            }

            var tiers = GetDummyTiers();
            var dummiesRoot = new GameObject("Dummies");

            for (int i = 0; i < tiers.Length; i++)
            {
                var tier = tiers[i];

                // Create or load the tier-specific AttackData SO
                string attackPath = $"Assets/ScriptableObjects/Attacks/Enemy/{tier.attackAsset}.asset";
                var attackData = TestDummyPrefabCreator.CreateOrLoadAttackData(
                    attackPath, tier.attackId, tier.attackName,
                    tier.damageMultiplier, tier.knockbackForce, tier.launchForce,
                    tier.telegraphType);

                PlaceDummyInstance(prefab, dummiesRoot.transform, tier, attackData);
            }

            Debug.Log($"[MovementTestScene] Placed {tiers.Length} tiered dummies (Bruiser → Weakling).");
        }

        private static void PlaceDummyInstance(GameObject prefab, Transform parent,
            DummyTierConfig tier, AttackData attackData)
        {
            var dummy = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            dummy.name = tier.name;
            dummy.transform.SetParent(parent);
            dummy.transform.position = tier.position;

            // Override sprite color and body scale
            var spriteChild = dummy.transform.Find("Sprite");
            if (spriteChild != null)
            {
                var sr = spriteChild.GetComponent<SpriteRenderer>();
                if (sr != null) sr.color = tier.spriteColor;
                spriteChild.localScale = new Vector3(tier.bodyScale.x, tier.bodyScale.y, 1f);
            }

            // Override attack settings via SerializedObject
            var dummyEnemy = dummy.GetComponent<TestDummyEnemy>();
            if (dummyEnemy != null)
            {
                var so = new SerializedObject(dummyEnemy);
                so.FindProperty("attackData").objectReferenceValue = attackData;
                so.FindProperty("attackInterval").floatValue = tier.attackInterval;
                so.FindProperty("attackActiveDuration").floatValue = tier.attackActiveDuration;
                so.FindProperty("telegraphDuration").floatValue = tier.telegraphDuration;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            // Override hitbox collider size/offset for this tier
            var hitboxChild = dummy.transform.Find("Hitbox_Punch");
            if (hitboxChild != null)
            {
                var col = hitboxChild.GetComponent<BoxCollider2D>();
                if (col != null)
                {
                    col.size = tier.hitboxSize;
                    col.offset = tier.hitboxOffset;
                }

                // Update debug visual to match new collider
                var debugVisual = hitboxChild.Find("DebugVisual");
                if (debugVisual != null)
                {
                    debugVisual.localPosition = new Vector3(tier.hitboxOffset.x, tier.hitboxOffset.y, 0f);
                    debugVisual.localScale = new Vector3(tier.hitboxSize.x, tier.hitboxSize.y, 1f);
                }
            }

            // Ensure debug HP bar
            if (dummy.GetComponent<DebugHealthBar>() == null)
                dummy.AddComponent<DebugHealthBar>();

            // Defense debug UI
            if (dummy.GetComponent<DefenseDebugUI>() == null)
            {
                var debugUI = dummy.AddComponent<DefenseDebugUI>();
                var defenseSystem = dummy.GetComponent<DefenseSystem>();
                if (defenseSystem != null)
                {
                    var debugSO = new SerializedObject(debugUI);
                    debugSO.FindProperty("defenseSystem").objectReferenceValue = defenseSystem;
                    debugSO.ApplyModifiedPropertiesWithoutUndo();
                }
            }

            // Floating label above the dummy
            var labelGO = new GameObject("Label");
            labelGO.transform.SetParent(dummy.transform);
            float labelY = tier.bodyScale.y + 0.3f;
            labelGO.transform.localPosition = new Vector3(0f, labelY, 0f);
            var tm = labelGO.AddComponent<TextMesh>();
            tm.text = tier.label;
            tm.fontSize = 24;
            tm.characterSize = 0.1f;
            tm.anchor = TextAnchor.LowerCenter;
            tm.alignment = TextAlignment.Center;
            tm.color = tier.spriteColor;
        }

        private static void BuildInlineFallbackPlayer()
        {
            var inputActions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(INPUT_ACTIONS_PATH);

            var root = new GameObject("Player");
            root.transform.position = new Vector3(-3f, 0f, 0f);

            var rb = root.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;

            var col = root.AddComponent<BoxCollider2D>();
            col.size = new Vector2(0.8f, 0.6f);
            col.offset = Vector2.zero;

            var spriteChild = new GameObject("Sprite");
            spriteChild.transform.SetParent(root.transform);
            spriteChild.transform.localPosition = Vector3.zero;
            var sr = spriteChild.AddComponent<SpriteRenderer>();
            sr.sprite = CreateRectSprite();
            sr.color = new Color(0.85f, 0.2f, 0.2f);
            spriteChild.transform.localScale = new Vector3(0.8f, 1.2f, 1f);
            sr.sortingOrder = 1;

            var shadowChild = new GameObject("Shadow");
            shadowChild.transform.SetParent(root.transform);
            shadowChild.transform.localPosition = new Vector3(0f, -0.1f, 0f);
            var shadowSR = shadowChild.AddComponent<SpriteRenderer>();
            shadowSR.sprite = CreateRectSprite();
            shadowSR.color = new Color(0f, 0f, 0f, 0.3f);
            shadowChild.transform.localScale = new Vector3(0.9f, 0.2f, 1f);
            shadowSR.sortingOrder = 0;

            // Load Mystica config, fall back to any available config
            var config = AssetDatabase.LoadAssetAtPath<MovementConfig>("Assets/ScriptableObjects/MovementConfigs/Mystica_MovementConfig.asset")
                      ?? AssetDatabase.LoadAssetAtPath<MovementConfig>("Assets/ScriptableObjects/MovementConfigs/Brutor_MovementConfig.asset");

            var comboDef = AssetDatabase.LoadAssetAtPath<ComboDefinition>("Assets/ScriptableObjects/ComboDefinitions/Mystica_ComboDefinition.asset")
                        ?? AssetDatabase.LoadAssetAtPath<ComboDefinition>("Assets/ScriptableObjects/ComboDefinitions/Brutor_ComboDefinition.asset");

            var motor = root.AddComponent<CharacterMotor>();
            var motorSO = new SerializedObject(motor);
            motorSO.FindProperty("config").objectReferenceValue = config;
            motorSO.FindProperty("characterType").enumValueIndex = (int)CharacterType.Mystica;
            motorSO.FindProperty("spriteTransform").objectReferenceValue = spriteChild.transform;
            motorSO.ApplyModifiedPropertiesWithoutUndo();

            var comboController = root.AddComponent<ComboController>();
            var comboSO = new SerializedObject(comboController);
            comboSO.FindProperty("comboDefinition").objectReferenceValue = comboDef;
            comboSO.FindProperty("characterType").enumValueIndex = (int)CharacterType.Mystica;
            comboSO.FindProperty("motor").objectReferenceValue = motor;
            comboSO.ApplyModifiedPropertiesWithoutUndo();

            root.AddComponent<ComboDebugUI>();

            var inputHandler = root.AddComponent<CharacterInputHandler>();
            var inputSO = new SerializedObject(inputHandler);
            inputSO.FindProperty("motor").objectReferenceValue = motor;
            inputSO.FindProperty("comboController").objectReferenceValue = comboController;

            if (inputActions != null)
            {
                WireAction(inputActions, inputSO, "moveAction", "Player/Move");
                WireAction(inputActions, inputSO, "jumpAction", "Player/Jump");
                WireAction(inputActions, inputSO, "dashAction", "Player/Sprint");
                WireAction(inputActions, inputSO, "lightAttackAction", "Player/Attack");
                WireAction(inputActions, inputSO, "heavyAttackAction", "Player/Crouch");

                if (inputActions.FindAction("Player/Run") != null)
                    WireAction(inputActions, inputSO, "runAction", "Player/Run");
            }

            inputSO.ApplyModifiedPropertiesWithoutUndo();

            root.AddComponent<PlayerDamageable>();

            // Temp debug HP bar
            var hpBar = root.AddComponent<DebugHealthBar>();
            var hpBarSO = new SerializedObject(hpBar);
            var fcProp = hpBarSO.FindProperty("fillColor");
            if (fcProp != null)
                fcProp.colorValue = new Color(0.2f, 0.8f, 0.3f);
            hpBarSO.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void WireAction(InputActionAsset asset, SerializedObject so,
            string propertyName, string actionPath)
        {
            var action = asset.FindAction(actionPath);
            if (action == null) return;
            so.FindProperty(propertyName).objectReferenceValue = InputActionReference.Create(action);
        }

        private static void CreateDebugCanvas()
        {
            var textGO = new GameObject("ControlsHint");
            var tm = textGO.AddComponent<TextMesh>();
            tm.text = "WASD: Move | Space: Jump | L-Shift: Dash | LMB: Light | C: Heavy | L-Ctrl: Run";
            tm.fontSize = 24;
            tm.characterSize = 0.15f;
            tm.anchor = TextAnchor.UpperCenter;
            tm.alignment = TextAlignment.Center;
            tm.color = new Color(1f, 1f, 1f, 0.5f);
            textGO.transform.position = new Vector3(0f, ARENA_HEIGHT / 2f - 0.5f, 0f);
        }

        private static Sprite CreateRectSprite()
        {
            var tex = new Texture2D(4, 4);
            var pixels = new Color[16];
            for (int i = 0; i < 16; i++) pixels[i] = Color.white;
            tex.SetPixels(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4);
        }
    }
}
