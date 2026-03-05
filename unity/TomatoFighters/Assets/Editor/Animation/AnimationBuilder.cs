using System.Collections.Generic;
using System.Linq;
using TomatoFighters.Combat;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace TomatoFighters.Editor.Animation
{
    /// <summary>
    /// <b>Animation Pipeline — Step 2 (Editor).</b>
    /// Creates <c>.anim</c> clips from sliced sprite sheets, builds a shared
    /// <c>BaseCharacter_Controller</c>, and generates per-character
    /// <c>AnimatorOverrideController</c>s with character-specific clip mappings.
    ///
    /// <para><b>Architecture (DD-1):</b> All 4 characters share the same state machine
    /// (same states, same transitions, same parameters). Only animation clips differ.
    /// The base controller uses Mystica's clips as the template (DD-5).
    /// Override controllers replace clips per character.</para>
    ///
    /// <para><b>Canonical states:</b> The base controller has locomotion (idle/walk/run),
    /// airborne (jump/land), action (dash), 10 attack slots (attack_1–attack_10),
    /// defense (block/guard), and reaction (hurt/death) states.</para>
    ///
    /// <para><b>Placeholder clips (DD-2):</b> When a canonical state has no matching
    /// animation in metadata.json, a single-frame placeholder clip is generated from
    /// the character's idle sprite. ERROR is logged for unmapped metadata animations;
    /// WARNING for canonical states with no matching animation.</para>
    ///
    /// <para><b>Pipeline usage:</b></para>
    /// <code>
    /// TomatoFighters > Import Sprite Sheets > All Characters    // Step 1
    /// TomatoFighters > Build Animations > All Characters         // Step 2 (this)
    /// TomatoFighters > Stamp Animation Events                    // Step 3
    /// </code>
    /// </summary>
    /// <seealso cref="AnimationForgeMetadata"/>
    /// <seealso cref="SpriteSheetImporter"/>
    /// <seealso cref="TomatoFighterAnimatorParams"/>
    public static class AnimationBuilder
    {
        private const string BASE_OUTPUT_FOLDER = "Assets/Animations/Base";

        // Locomotion state layout — order matters for Speed threshold wiring
        private static readonly string[] LOCOMOTION_ORDER = { "idle", "walk", "run" };

        // Speed thresholds: idle < 0.1 < walk < 0.9 < run
        private const float WALK_THRESHOLD = 0.1f;
        private const float RUN_THRESHOLD = 0.9f;
        private const float TRANSITION_DURATION = 0.05f;

        // Trigger name mapping for special states (non-standard naming)
        private static readonly Dictionary<string, string> TRIGGER_OVERRIDES = new Dictionary<string, string>
        {
            ["hurt"] = TomatoFighterAnimatorParams.HURTTRIGGER,
            ["death"] = TomatoFighterAnimatorParams.DEATHTRIGGER,
            ["block"] = TomatoFighterAnimatorParams.BLOCKTRIGGER,
            ["guard"] = TomatoFighterAnimatorParams.GUARDTRIGGER,
            ["stun"] = TomatoFighterAnimatorParams.STUNTRIGGER,
            ["knockback"] = TomatoFighterAnimatorParams.KNOCKBACKTRIGGER,
        };

        // ── Menu Items ──

        [MenuItem("TomatoFighters/Build Animations/All Characters")]
        public static void BuildAllAnimations()
        {
            // Step 1: Build clips for all characters
            var allCharacterClips = new Dictionary<string, Dictionary<string, AnimationClip>>();

            foreach (var kvp in AnimationForgeMetadata.Characters)
            {
                string charName = kvp.Key;
                Debug.Log($"[AnimationBuilder] Building clips for {charName}...");
                var clips = BuildCharacterClips(kvp.Value.sourceFolder, kvp.Value.outputFolder);
                if (clips != null && clips.Count > 0)
                    allCharacterClips[charName] = clips;
            }

            // Step 2: Build base controller using Mystica's clips (DD-5)
            Dictionary<string, AnimationClip> mysticaClips = null;
            allCharacterClips.TryGetValue("Mystica", out mysticaClips);

            string mysticaSourceFolder = AnimationForgeMetadata.Characters["Mystica"].sourceFolder;
            var mysticaMetadata = AnimationForgeMetadata.Load(mysticaSourceFolder);

            BuildBaseController(mysticaClips, mysticaMetadata);

            // Step 3: Build override controllers for each character
            foreach (var kvp in AnimationForgeMetadata.Characters)
            {
                string charName = kvp.Key;
                Dictionary<string, AnimationClip> clips = null;
                allCharacterClips.TryGetValue(charName, out clips);

                var metadata = AnimationForgeMetadata.Load(kvp.Value.sourceFolder);
                BuildOverrideController(charName, kvp.Value.outputFolder, clips, metadata);
            }

            // ── Enemy pipeline ──
            // Step 4: Build clips for all enemies
            var allEnemyClips = new Dictionary<string, Dictionary<string, AnimationClip>>();

            foreach (var kvp in AnimationForgeMetadata.EnemyCharacters)
            {
                if (string.IsNullOrEmpty(kvp.Value.sourceFolder)) continue; // TestDummy
                Debug.Log($"[AnimationBuilder] Building enemy clips for {kvp.Key}...");
                var clips = BuildEnemyClips(kvp.Key, kvp.Value.sourceFolder, kvp.Value.outputFolder);
                if (clips != null && clips.Count > 0)
                    allEnemyClips[kvp.Key] = clips;
            }

            // Step 5: Build base enemy controller using first available enemy's clips
            string baseEnemyKey = null;
            Dictionary<string, AnimationClip> baseEnemyClips = null;
            foreach (var kvp in allEnemyClips)
            {
                baseEnemyKey = kvp.Key;
                baseEnemyClips = kvp.Value;
                break;
            }
            BuildBaseEnemyController(baseEnemyClips, baseEnemyKey);

            // Step 6: Build enemy override controllers
            foreach (var kvp in AnimationForgeMetadata.EnemyCharacters)
            {
                Dictionary<string, AnimationClip> clips = null;
                allEnemyClips.TryGetValue(kvp.Key, out clips);
                BuildEnemyOverrideController(kvp.Key, kvp.Value.outputFolder, clips);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            int enemyCount = AnimationForgeMetadata.EnemyCharacters.Count;
            Debug.Log($"[AnimationBuilder] Done — base controller + 4 player overrides + base enemy controller + {enemyCount} enemy overrides built.");
        }

        [MenuItem("TomatoFighters/Build Animations/Mystica")]
        public static void BuildMysticaAnimations() => BuildSingleCharacter("Mystica");

        [MenuItem("TomatoFighters/Build Animations/Slasher")]
        public static void BuildSlasherAnimations() => BuildSingleCharacter("Slasher");

        [MenuItem("TomatoFighters/Build Animations/Brutor")]
        public static void BuildBrutorAnimations() => BuildSingleCharacter("Brutor");

        [MenuItem("TomatoFighters/Build Animations/Viper")]
        public static void BuildViperAnimations() => BuildSingleCharacter("Viper");

        /// <summary>Builds clips for a single character and rebuilds base + all overrides.</summary>
        private static void BuildSingleCharacter(string characterName)
        {
            // Rebuild everything to keep base + overrides in sync
            BuildAllAnimations();
        }

        // ── Clip Building ──

        /// <summary>
        /// Builds animation clips for a single character from their metadata.json.
        /// Returns a dictionary of animation name → clip. Also generates placeholder clips
        /// for any canonical states not found in metadata.
        /// </summary>
        public static Dictionary<string, AnimationClip> BuildCharacterClips(string sourceFolder, string outputFolder)
        {
            var metadata = AnimationForgeMetadata.Load(sourceFolder);
            if (metadata == null) return null;

            EnsureFolderExists(outputFolder);

            string spritesFolder = $"{sourceFolder}/Sprites";
            var clips = new Dictionary<string, AnimationClip>();

            // Create clips for ALL animations in metadata
            foreach (var kvp in metadata.animations)
            {
                string animName = kvp.Key;
                var entry = kvp.Value;

                string sheetPath = AnimationForgeMetadata.GetSheetPath(spritesFolder, metadata.characterName, animName);
                var clip = CreateClip(animName, entry, sheetPath, metadata.characterName, outputFolder);
                if (clip != null)
                    clips[animName] = clip;
            }

            if (clips.Count == 0)
            {
                Debug.LogError($"[AnimationBuilder] No clips created for {metadata.characterName}. Ensure sprite sheets are imported.");
                return null;
            }

            // Generate placeholder clips for canonical states without real art
            GeneratePlaceholderClips(metadata.characterName, clips, spritesFolder, outputFolder, metadata);

            Debug.Log($"[AnimationBuilder] {metadata.characterName}: {clips.Count} total clips built.");
            return clips;
        }

        /// <summary>
        /// Builds animation clips and a standalone controller (legacy path).
        /// Kept for backward compatibility with per-character builds.
        /// </summary>
        public static void BuildAnimations(string sourceFolder, string outputFolder)
        {
            // Delegate to the new pipeline
            BuildAllAnimations();
        }

        /// <summary>Creates a single .anim clip from a sliced sprite sheet.</summary>
        private static AnimationClip CreateClip(
            string animName, AnimationForgeMetadata.AnimationEntry entry,
            string sheetPath, string characterName, string outputFolder)
        {
            var sprites = AssetDatabase.LoadAllAssetsAtPath(sheetPath)
                .OfType<Sprite>()
                .OrderBy(s => s.name)
                .ToArray();

            if (sprites.Length == 0)
            {
                Debug.LogError($"[AnimationBuilder] No sliced sprites at {sheetPath}. Run 'Import Sprite Sheets' first.");
                return null;
            }

            if (sprites.Length != entry.frameCount)
                Debug.LogWarning($"[AnimationBuilder] {animName}: expected {entry.frameCount} frames, found {sprites.Length}.");

            var clip = new AnimationClip();
            clip.name = $"{characterName}_{animName}";
            clip.frameRate = entry.fps;

            // Animator and SpriteRenderer are on the same GameObject → empty binding path
            var keyframes = new ObjectReferenceKeyframe[sprites.Length];
            float frameDuration = 1f / entry.fps;

            for (int i = 0; i < sprites.Length; i++)
            {
                keyframes[i] = new ObjectReferenceKeyframe
                {
                    time = i * frameDuration,
                    value = sprites[i]
                };
            }

            var binding = EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite");
            AnimationUtility.SetObjectReferenceCurve(clip, binding, keyframes);

            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = entry.loop;
            AnimationUtility.SetAnimationClipSettings(clip, settings);

            string clipPath = $"{outputFolder}/{characterName}_{animName}.anim";

            // Overwrite existing clip asset if present
            var existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
            if (existing != null)
                AssetDatabase.DeleteAsset(clipPath);

            AssetDatabase.CreateAsset(clip, clipPath);
            Debug.Log($"[AnimationBuilder] {animName}: {sprites.Length} frames, {entry.fps}fps, loop={entry.loop}");
            return clip;
        }

        /// <summary>
        /// Generates single-frame placeholder clips for canonical states that lack real art.
        /// Uses the character's idle sprite sheet first frame as the placeholder image.
        /// </summary>
        private static void GeneratePlaceholderClips(
            string characterName,
            Dictionary<string, AnimationClip> existingClips,
            string spritesFolder,
            string outputFolder,
            AnimationForgeMetadata.MetadataRoot metadata)
        {
            // Find the character display name for this internal name
            string charKey = AnimationForgeMetadata.Characters
                .FirstOrDefault(c => c.Value.outputFolder == outputFolder).Key;

            // Determine which canonical states need placeholders
            var neededStates = new List<string>();

            // Attack slots for this character
            if (charKey != null && AnimationForgeMetadata.AttackSlotMappings.ContainsKey(charKey))
            {
                foreach (var slot in AnimationForgeMetadata.AttackSlotMappings[charKey].Keys)
                {
                    if (!existingClips.ContainsKey(slot))
                        neededStates.Add(slot);
                }
            }

            // Defense and reaction states
            foreach (var state in AnimationForgeMetadata.DefenseStates)
            {
                if (!existingClips.ContainsKey(state))
                    neededStates.Add(state);
            }
            foreach (var state in AnimationForgeMetadata.ReactionStates)
            {
                if (!existingClips.ContainsKey(state))
                    neededStates.Add(state);
            }
            // Dash
            if (!existingClips.ContainsKey("dash"))
                neededStates.Add("dash");

            if (neededStates.Count == 0) return;

            // Load the first idle sprite as the placeholder image
            string idleSheetPath = AnimationForgeMetadata.GetSheetPath(spritesFolder, characterName, "idle");
            var idleSprites = AssetDatabase.LoadAllAssetsAtPath(idleSheetPath)
                .OfType<Sprite>()
                .OrderBy(s => s.name)
                .ToArray();

            if (idleSprites.Length == 0)
            {
                Debug.LogError($"[AnimationBuilder] Cannot create placeholders for {characterName}: no idle sprites at {idleSheetPath}");
                return;
            }

            Sprite placeholderSprite = idleSprites[0];
            int fps = metadata.animations.ContainsKey("idle") ? metadata.animations["idle"].fps : 12;

            foreach (string stateName in neededStates)
            {
                bool isGuard = stateName == "guard";
                var clip = CreatePlaceholderClip(characterName, stateName, placeholderSprite, fps, isGuard);
                if (clip != null)
                {
                    string clipPath = $"{outputFolder}/{characterName}_{stateName}_placeholder.anim";
                    var existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
                    if (existing != null)
                        AssetDatabase.DeleteAsset(clipPath);

                    AssetDatabase.CreateAsset(clip, clipPath);
                    existingClips[stateName] = clip;
                }
            }

            Debug.Log($"[AnimationBuilder] {characterName}: generated {neededStates.Count} placeholder clips.");
        }

        /// <summary>Creates a single-frame placeholder animation clip.</summary>
        private static AnimationClip CreatePlaceholderClip(
            string characterName, string stateName, Sprite sprite, int fps, bool loop)
        {
            var clip = new AnimationClip();
            clip.name = $"{characterName}_{stateName}_placeholder";
            clip.frameRate = fps;

            var keyframes = new ObjectReferenceKeyframe[]
            {
                new ObjectReferenceKeyframe { time = 0f, value = sprite }
            };

            var binding = EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite");
            AnimationUtility.SetObjectReferenceCurve(clip, binding, keyframes);

            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = loop;
            AnimationUtility.SetAnimationClipSettings(clip, settings);

            return clip;
        }

        // ── Base Controller ──

        /// <summary>
        /// Builds the shared base AnimatorController with all canonical states.
        /// Uses Mystica's clips as the template (DD-5). States with no Mystica clip
        /// get a placeholder. All 4 characters share this state machine via overrides.
        /// </summary>
        private static void BuildBaseController(
            Dictionary<string, AnimationClip> mysticaClips,
            AnimationForgeMetadata.MetadataRoot mysticaMetadata)
        {
            EnsureFolderExists(BASE_OUTPUT_FOLDER);
            string controllerPath = $"{BASE_OUTPUT_FOLDER}/BaseCharacter_Controller.controller";

            // Validate Mystica's metadata
            if (mysticaMetadata != null)
            {
                string mysticaKey = "Mystica";
                AnimationForgeMetadata.ValidateMetadata(mysticaKey, mysticaMetadata);
            }

            // Delete existing controller to start clean
            if (AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath) != null)
                AssetDatabase.DeleteAsset(controllerPath);

            var controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
            var rootSM = controller.layers[0].stateMachine;

            // --- Parameters ---
            controller.AddParameter(TomatoFighterAnimatorParams.SPEED, AnimatorControllerParameterType.Float);
            controller.AddParameter(TomatoFighterAnimatorParams.ISGROUNDED, AnimatorControllerParameterType.Bool);

            // Set IsGrounded default to true
            var parameters = controller.parameters;
            for (int i = 0; i < parameters.Length; i++)
            {
                if (parameters[i].name == TomatoFighterAnimatorParams.ISGROUNDED)
                {
                    parameters[i].defaultBool = true;
                    break;
                }
            }
            controller.parameters = parameters;

            // Classify Mystica clips
            var locomotion = new Dictionary<string, AnimationClip>();
            var airborne = new Dictionary<string, AnimationClip>();
            var actionClips = new Dictionary<string, AnimationClip>();

            if (mysticaClips != null)
            {
                foreach (var kvp in mysticaClips)
                {
                    if (AnimationForgeMetadata.LocomotionStates.Contains(kvp.Key))
                        locomotion[kvp.Key] = kvp.Value;
                    else if (AnimationForgeMetadata.AirborneStates.Contains(kvp.Key))
                        airborne[kvp.Key] = kvp.Value;
                    else
                        actionClips[kvp.Key] = kvp.Value;
                }
            }

            // --- Locomotion states ---
            var locoStates = new Dictionary<string, AnimatorState>();
            float yOffset = 0f;

            foreach (string name in LOCOMOTION_ORDER)
            {
                if (!locomotion.ContainsKey(name)) continue;

                var state = rootSM.AddState(name, new Vector3(300, yOffset, 0));
                state.motion = locomotion[name];
                locoStates[name] = state;
                yOffset += 80f;
            }

            if (locoStates.ContainsKey("idle"))
                rootSM.defaultState = locoStates["idle"];

            WireLocomotionTransitions(locoStates);

            // --- Airborne states ---
            WireAirborneStates(rootSM, airborne, locoStates);

            // --- Action states (all trigger-driven) ---
            var idleState = locoStates.ContainsKey("idle") ? locoStates["idle"] : null;
            float actionY = 0f;

            // Collect all action state names: dash + attack slots + defense + reaction
            var allActionStates = new List<string>();
            allActionStates.Add("dash");
            allActionStates.AddRange(AnimationForgeMetadata.AttackSlots);
            allActionStates.AddRange(AnimationForgeMetadata.DefenseStates);
            allActionStates.AddRange(AnimationForgeMetadata.ReactionStates);

            foreach (string stateName in allActionStates)
            {
                // Determine trigger name
                string triggerName;
                if (TRIGGER_OVERRIDES.ContainsKey(stateName))
                    triggerName = TRIGGER_OVERRIDES[stateName];
                else
                    triggerName = stateName + "Trigger";

                controller.AddParameter(triggerName, AnimatorControllerParameterType.Trigger);

                var state = rootSM.AddState(stateName, new Vector3(600, actionY, 0));
                actionY += 60f;

                // Assign clip from Mystica (real or placeholder)
                if (actionClips.ContainsKey(stateName))
                    state.motion = actionClips[stateName];

                // AnyState → action (via trigger)
                var triggerTransition = rootSM.AddAnyStateTransition(state);
                triggerTransition.hasExitTime = false;
                triggerTransition.duration = 0f;
                triggerTransition.AddCondition(AnimatorConditionMode.If, 0, triggerName);

                // Guard loops until interrupted; all others → idle via exit time
                bool isLooping = stateName == "guard";
                if (!isLooping && idleState != null)
                {
                    var exitTransition = state.AddTransition(idleState);
                    exitTransition.hasExitTime = true;
                    exitTransition.exitTime = 1f;
                    exitTransition.duration = 0.05f;
                }
            }

            EditorUtility.SetDirty(controller);
            Debug.Log($"[AnimationBuilder] Base controller built at {controllerPath}: " +
                      $"{locoStates.Count} locomotion, {airborne.Count} airborne, " +
                      $"{allActionStates.Count} action states.");
        }

        // ── Override Controllers ──

        /// <summary>
        /// Builds an AnimatorOverrideController for a single character.
        /// Maps base controller clips to character-specific clips where available.
        /// </summary>
        private static void BuildOverrideController(
            string characterName,
            string outputFolder,
            Dictionary<string, AnimationClip> characterClips,
            AnimationForgeMetadata.MetadataRoot metadata)
        {
            EnsureFolderExists(outputFolder);

            // Validate metadata
            if (metadata != null)
                AnimationForgeMetadata.ValidateMetadata(characterName, metadata);

            string baseControllerPath = $"{BASE_OUTPUT_FOLDER}/BaseCharacter_Controller.controller";
            var baseController = AssetDatabase.LoadAssetAtPath<AnimatorController>(baseControllerPath);
            if (baseController == null)
            {
                Debug.LogError($"[AnimationBuilder] Base controller not found at {baseControllerPath}. Build all animations first.");
                return;
            }

            string overridePath = $"{outputFolder}/{characterName}_Override.overrideController";

            // Delete existing override to start clean
            if (AssetDatabase.LoadAssetAtPath<AnimatorOverrideController>(overridePath) != null)
                AssetDatabase.DeleteAsset(overridePath);

            var overrideController = new AnimatorOverrideController(baseController);
            overrideController.name = $"{characterName}_Override";

            if (characterClips != null && characterClips.Count > 0)
            {
                // Get all clip overrides from the base controller
                var overrides = new List<KeyValuePair<AnimationClip, AnimationClip>>();
                overrideController.GetOverrides(overrides);

                // Build a lookup from clip name suffix to character clip
                // Base clips are named like "purple_mage_{stateName}" or "purple_mage_{stateName}_placeholder"
                // Character clips are named like "{charInternalName}_{stateName}" or "{charInternalName}_{stateName}_placeholder"
                var charClipsByState = new Dictionary<string, AnimationClip>();
                foreach (var kvp in characterClips)
                {
                    charClipsByState[kvp.Key] = kvp.Value;
                }

                // Replace base clips with character clips where states match
                for (int i = 0; i < overrides.Count; i++)
                {
                    var baseClip = overrides[i].Key;
                    if (baseClip == null) continue;

                    // Extract state name from base clip name
                    string stateName = ExtractStateName(baseClip.name);
                    if (stateName != null && charClipsByState.ContainsKey(stateName))
                    {
                        overrides[i] = new KeyValuePair<AnimationClip, AnimationClip>(
                            baseClip, charClipsByState[stateName]);
                    }
                }

                overrideController.ApplyOverrides(overrides);
            }

            AssetDatabase.CreateAsset(overrideController, overridePath);
            EditorUtility.SetDirty(overrideController);

            Debug.Log($"[AnimationBuilder] Override controller built for {characterName} at {overridePath}.");
        }

        /// <summary>
        /// Extracts the canonical state name from a clip asset name.
        /// E.g., "purple_mage_idle" → "idle", "purple_mage_attack_1_placeholder" → "attack_1"
        /// </summary>
        private static string ExtractStateName(string clipName)
        {
            // Remove "_placeholder" suffix if present
            string name = clipName;
            if (name.EndsWith("_placeholder"))
                name = name.Substring(0, name.Length - "_placeholder".Length);

            // Try matching against all canonical states (longest match first to handle attack_10 before attack_1)
            var sortedStates = AnimationForgeMetadata.AllCanonicalStates
                .OrderByDescending(s => s.Length)
                .ToList();

            foreach (string state in sortedStates)
            {
                if (name.EndsWith("_" + state))
                    return state;
            }

            return null;
        }

        // ── Transition Wiring ──

        /// <summary>
        /// Wires IsGrounded-based transitions for jump and land states.
        /// </summary>
        private static void WireAirborneStates(
            AnimatorStateMachine rootSM,
            Dictionary<string, AnimationClip> airborne,
            Dictionary<string, AnimatorState> locoStates)
        {
            if (!airborne.ContainsKey("jump")) return;

            string groundedParam = TomatoFighterAnimatorParams.ISGROUNDED;

            var jumpState = rootSM.AddState("jump", new Vector3(450, -100, 0));
            jumpState.motion = airborne["jump"];

            foreach (var kvp in locoStates)
            {
                var t = kvp.Value.AddTransition(jumpState);
                t.hasExitTime = false;
                t.duration = TRANSITION_DURATION;
                t.AddCondition(AnimatorConditionMode.IfNot, 0, groundedParam);
            }

            if (airborne.ContainsKey("land"))
            {
                var landState = rootSM.AddState("land", new Vector3(450, -20, 0));
                landState.motion = airborne["land"];

                var jumpToLand = jumpState.AddTransition(landState);
                jumpToLand.hasExitTime = false;
                jumpToLand.duration = TRANSITION_DURATION;
                jumpToLand.AddCondition(AnimatorConditionMode.If, 0, groundedParam);

                if (locoStates.ContainsKey("idle"))
                {
                    var landToIdle = landState.AddTransition(locoStates["idle"]);
                    landToIdle.hasExitTime = true;
                    landToIdle.exitTime = 1f;
                    landToIdle.duration = TRANSITION_DURATION;
                }
            }
            else
            {
                if (locoStates.ContainsKey("idle"))
                {
                    var jumpToIdle = jumpState.AddTransition(locoStates["idle"]);
                    jumpToIdle.hasExitTime = false;
                    jumpToIdle.duration = TRANSITION_DURATION;
                    jumpToIdle.AddCondition(AnimatorConditionMode.If, 0, groundedParam);
                }
            }
        }

        /// <summary>
        /// Wires Speed-based transitions between locomotion states.
        /// </summary>
        private static void WireLocomotionTransitions(Dictionary<string, AnimatorState> states)
        {
            string speedParam = TomatoFighterAnimatorParams.SPEED;

            if (states.ContainsKey("idle") && states.ContainsKey("walk"))
            {
                var t = states["idle"].AddTransition(states["walk"]);
                t.hasExitTime = false;
                t.duration = TRANSITION_DURATION;
                t.AddCondition(AnimatorConditionMode.Greater, WALK_THRESHOLD, speedParam);

                t = states["walk"].AddTransition(states["idle"]);
                t.hasExitTime = false;
                t.duration = TRANSITION_DURATION;
                t.AddCondition(AnimatorConditionMode.Less, WALK_THRESHOLD, speedParam);
            }

            if (states.ContainsKey("walk") && states.ContainsKey("run"))
            {
                var t = states["walk"].AddTransition(states["run"]);
                t.hasExitTime = false;
                t.duration = TRANSITION_DURATION;
                t.AddCondition(AnimatorConditionMode.Greater, RUN_THRESHOLD, speedParam);

                t = states["run"].AddTransition(states["walk"]);
                t.hasExitTime = false;
                t.duration = TRANSITION_DURATION;
                t.AddCondition(AnimatorConditionMode.Less, RUN_THRESHOLD, speedParam);
            }

            if (states.ContainsKey("run") && states.ContainsKey("idle"))
            {
                var t = states["run"].AddTransition(states["idle"]);
                t.hasExitTime = false;
                t.duration = TRANSITION_DURATION;
                t.AddCondition(AnimatorConditionMode.Less, WALK_THRESHOLD, speedParam);
            }
        }

        // ── Enemy Pipeline ──

        private const string ENEMY_BASE_OUTPUT_FOLDER = "Assets/Animations/Enemies/Base";

        // Enemy locomotion: idle + walk only (no run)
        private static readonly string[] ENEMY_LOCOMOTION_ORDER = { "idle", "walk" };

        // Enemy attack slots: 5 max (DD-2 from T024B)
        private static readonly string[] ENEMY_ATTACK_SLOTS = { "attack_1", "attack_2", "attack_3", "attack_4", "attack_5" };

        // Enemy reaction states
        private static readonly HashSet<string> ENEMY_REACTION_STATES = new HashSet<string> { "hurt", "death", "stun", "knockback" };

        // Enemy trigger name overrides (DD-3 from T024B: shorter names to match EnemyBase.cs)
        private static readonly Dictionary<string, string> ENEMY_TRIGGER_OVERRIDES = new Dictionary<string, string>
        {
            ["hurt"] = "Hurt",
            ["death"] = "Death",
            ["stun"] = "StunTrigger",
            ["knockback"] = "KnockbackTrigger",
            ["guard"] = "guardTrigger",
        };

        /// <summary>
        /// Builds animation clips for an enemy from their metadata.json.
        /// Applies EnemyAnimNameAliases to remap non-canonical names (e.g., "attack" → "attack_1").
        /// </summary>
        private static Dictionary<string, AnimationClip> BuildEnemyClips(string enemyKey, string sourceFolder, string outputFolder)
        {
            var metadata = AnimationForgeMetadata.Load(sourceFolder);
            if (metadata == null) return null;

            EnsureFolderExists(outputFolder);

            string spritesFolder = $"{sourceFolder}/Sprites";
            var clips = new Dictionary<string, AnimationClip>();

            // Get alias mappings for this enemy
            Dictionary<string, string> aliases = null;
            AnimationForgeMetadata.EnemyAnimNameAliases.TryGetValue(enemyKey, out aliases);

            foreach (var kvp in metadata.animations)
            {
                string animName = kvp.Key;
                var entry = kvp.Value;

                // Apply alias remapping (e.g., EggplantWizard "attack" → "attack_1")
                string canonicalName = animName;
                if (aliases != null && aliases.ContainsKey(animName) && aliases[animName] != animName)
                    canonicalName = aliases[animName];

                string sheetPath = AnimationForgeMetadata.GetSheetPath(spritesFolder, metadata.characterName, animName);
                var clip = CreateClip(canonicalName, entry, sheetPath, metadata.characterName, outputFolder);
                if (clip != null)
                    clips[canonicalName] = clip;
            }

            if (clips.Count == 0)
            {
                Debug.LogError($"[AnimationBuilder] No clips created for enemy {enemyKey}. Ensure sprite sheets are imported.");
                return null;
            }

            // Generate placeholder clips for missing enemy canonical states
            GenerateEnemyPlaceholderClips(enemyKey, metadata.characterName, clips, spritesFolder, outputFolder, metadata);

            Debug.Log($"[AnimationBuilder] Enemy {enemyKey}: {clips.Count} total clips built.");
            return clips;
        }

        /// <summary>
        /// Generates placeholder clips for enemy canonical states that lack real art.
        /// </summary>
        private static void GenerateEnemyPlaceholderClips(
            string enemyKey,
            string characterName,
            Dictionary<string, AnimationClip> existingClips,
            string spritesFolder,
            string outputFolder,
            AnimationForgeMetadata.MetadataRoot metadata)
        {
            var neededStates = new List<string>();

            // Attack slots for this enemy
            if (AnimationForgeMetadata.EnemyAttackSlotMappings.ContainsKey(enemyKey))
            {
                foreach (var slot in AnimationForgeMetadata.EnemyAttackSlotMappings[enemyKey].Keys)
                {
                    if (!existingClips.ContainsKey(slot))
                        neededStates.Add(slot);
                }
            }

            // Reaction states
            foreach (var state in ENEMY_REACTION_STATES)
            {
                if (!existingClips.ContainsKey(state))
                    neededStates.Add(state);
            }

            // Guard (defense)
            if (!existingClips.ContainsKey("guard"))
                neededStates.Add("guard");

            if (neededStates.Count == 0) return;

            string idleSheetPath = AnimationForgeMetadata.GetSheetPath(spritesFolder, characterName, "idle");
            var idleSprites = AssetDatabase.LoadAllAssetsAtPath(idleSheetPath)
                .OfType<Sprite>()
                .OrderBy(s => s.name)
                .ToArray();

            if (idleSprites.Length == 0)
            {
                Debug.LogError($"[AnimationBuilder] Cannot create placeholders for {enemyKey}: no idle sprites at {idleSheetPath}");
                return;
            }

            Sprite placeholderSprite = idleSprites[0];
            int fps = metadata.animations.ContainsKey("idle") ? metadata.animations["idle"].fps : 12;

            foreach (string stateName in neededStates)
            {
                bool isLooping = stateName == "guard" || stateName == "stun";
                var clip = CreatePlaceholderClip(characterName, stateName, placeholderSprite, fps, isLooping);
                if (clip != null)
                {
                    string clipPath = $"{outputFolder}/{characterName}_{stateName}_placeholder.anim";
                    var existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
                    if (existing != null)
                        AssetDatabase.DeleteAsset(clipPath);

                    AssetDatabase.CreateAsset(clip, clipPath);
                    existingClips[stateName] = clip;
                }
            }

            Debug.Log($"[AnimationBuilder] Enemy {enemyKey}: generated {neededStates.Count} placeholder clips.");
        }

        /// <summary>
        /// Builds the base enemy AnimatorController with enemy canonical states.
        /// Uses the first available enemy's clips as the template.
        /// </summary>
        private static void BuildBaseEnemyController(
            Dictionary<string, AnimationClip> templateClips,
            string templateKey)
        {
            EnsureFolderExists(ENEMY_BASE_OUTPUT_FOLDER);
            string controllerPath = $"{ENEMY_BASE_OUTPUT_FOLDER}/BaseEnemy_Controller.controller";

            if (AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath) != null)
                AssetDatabase.DeleteAsset(controllerPath);

            var controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
            var rootSM = controller.layers[0].stateMachine;

            // Parameters
            controller.AddParameter(TomatoFighterAnimatorParams.SPEED, AnimatorControllerParameterType.Float);

            // Classify template clips
            var locomotion = new Dictionary<string, AnimationClip>();
            var actionClips = new Dictionary<string, AnimationClip>();

            if (templateClips != null)
            {
                foreach (var kvp in templateClips)
                {
                    if (kvp.Key == "idle" || kvp.Key == "walk")
                        locomotion[kvp.Key] = kvp.Value;
                    else
                        actionClips[kvp.Key] = kvp.Value;
                }
            }

            // Locomotion states (idle + walk only)
            var locoStates = new Dictionary<string, AnimatorState>();
            float yOffset = 0f;

            foreach (string name in ENEMY_LOCOMOTION_ORDER)
            {
                AnimationClip clip = null;
                locomotion.TryGetValue(name, out clip);
                if (clip == null) continue;

                var state = rootSM.AddState(name, new Vector3(300, yOffset, 0));
                state.motion = clip;
                locoStates[name] = state;
                yOffset += 80f;
            }

            if (locoStates.ContainsKey("idle"))
                rootSM.defaultState = locoStates["idle"];

            // Wire locomotion: idle <-> walk at threshold 0.1
            WireEnemyLocomotionTransitions(locoStates);

            // Action states: 5 attack slots + guard + reactions
            var idleState = locoStates.ContainsKey("idle") ? locoStates["idle"] : null;
            float actionY = 0f;

            var allEnemyActionStates = new List<string>();
            allEnemyActionStates.AddRange(ENEMY_ATTACK_SLOTS);
            allEnemyActionStates.Add("guard");
            allEnemyActionStates.AddRange(ENEMY_REACTION_STATES);

            foreach (string stateName in allEnemyActionStates)
            {
                string triggerName;
                if (ENEMY_TRIGGER_OVERRIDES.ContainsKey(stateName))
                    triggerName = ENEMY_TRIGGER_OVERRIDES[stateName];
                else
                    triggerName = stateName + "Trigger";

                controller.AddParameter(triggerName, AnimatorControllerParameterType.Trigger);

                var state = rootSM.AddState(stateName, new Vector3(600, actionY, 0));
                actionY += 60f;

                if (actionClips.ContainsKey(stateName))
                    state.motion = actionClips[stateName];

                var triggerTransition = rootSM.AddAnyStateTransition(state);
                triggerTransition.hasExitTime = false;
                triggerTransition.duration = 0f;
                triggerTransition.AddCondition(AnimatorConditionMode.If, 0, triggerName);

                // Guard and stun loop; all others return to idle via exit time
                bool isLooping = stateName == "guard" || stateName == "stun";
                if (!isLooping && idleState != null)
                {
                    var exitTransition = state.AddTransition(idleState);
                    exitTransition.hasExitTime = true;
                    exitTransition.exitTime = 1f;
                    exitTransition.duration = 0.05f;
                }
            }

            EditorUtility.SetDirty(controller);
            Debug.Log($"[AnimationBuilder] Base enemy controller built at {controllerPath}: " +
                      $"{locoStates.Count} locomotion, {allEnemyActionStates.Count} action states " +
                      $"(template: {templateKey ?? "none"}).");
        }

        /// <summary>
        /// Builds an AnimatorOverrideController for a single enemy.
        /// </summary>
        private static void BuildEnemyOverrideController(
            string enemyKey,
            string outputFolder,
            Dictionary<string, AnimationClip> enemyClips)
        {
            EnsureFolderExists(outputFolder);

            string baseControllerPath = $"{ENEMY_BASE_OUTPUT_FOLDER}/BaseEnemy_Controller.controller";
            var baseController = AssetDatabase.LoadAssetAtPath<AnimatorController>(baseControllerPath);
            if (baseController == null)
            {
                Debug.LogError($"[AnimationBuilder] Base enemy controller not found at {baseControllerPath}.");
                return;
            }

            string overridePath = $"{outputFolder}/{enemyKey}_Override.overrideController";

            if (AssetDatabase.LoadAssetAtPath<AnimatorOverrideController>(overridePath) != null)
                AssetDatabase.DeleteAsset(overridePath);

            var overrideController = new AnimatorOverrideController(baseController);
            overrideController.name = $"{enemyKey}_Override";

            if (enemyClips != null && enemyClips.Count > 0)
            {
                var overrides = new List<KeyValuePair<AnimationClip, AnimationClip>>();
                overrideController.GetOverrides(overrides);

                var clipsByState = new Dictionary<string, AnimationClip>();
                foreach (var kvp in enemyClips)
                    clipsByState[kvp.Key] = kvp.Value;

                for (int i = 0; i < overrides.Count; i++)
                {
                    var baseClip = overrides[i].Key;
                    if (baseClip == null) continue;

                    string stateName = ExtractEnemyStateName(baseClip.name);
                    if (stateName != null && clipsByState.ContainsKey(stateName))
                    {
                        overrides[i] = new KeyValuePair<AnimationClip, AnimationClip>(
                            baseClip, clipsByState[stateName]);
                    }
                }

                overrideController.ApplyOverrides(overrides);
            }

            AssetDatabase.CreateAsset(overrideController, overridePath);
            EditorUtility.SetDirty(overrideController);

            Debug.Log($"[AnimationBuilder] Enemy override controller built for {enemyKey} at {overridePath}.");
        }

        /// <summary>
        /// Extracts the canonical state name from an enemy clip asset name.
        /// </summary>
        private static string ExtractEnemyStateName(string clipName)
        {
            string name = clipName;
            if (name.EndsWith("_placeholder"))
                name = name.Substring(0, name.Length - "_placeholder".Length);

            // Enemy canonical states: locomotion + attacks + guard + reactions
            var candidateStates = new List<string>();
            candidateStates.AddRange(ENEMY_LOCOMOTION_ORDER);
            candidateStates.AddRange(ENEMY_ATTACK_SLOTS);
            candidateStates.Add("guard");
            candidateStates.AddRange(ENEMY_REACTION_STATES);

            // Sort by length descending to match attack_5 before attack_1, etc.
            candidateStates.Sort((a, b) => b.Length.CompareTo(a.Length));

            foreach (string state in candidateStates)
            {
                if (name.EndsWith("_" + state))
                    return state;
            }

            return null;
        }

        /// <summary>Wires Speed-based transitions for enemy locomotion (idle + walk).</summary>
        private static void WireEnemyLocomotionTransitions(Dictionary<string, AnimatorState> states)
        {
            if (states.ContainsKey("idle") && states.ContainsKey("walk"))
            {
                string speedParam = TomatoFighterAnimatorParams.SPEED;

                var t = states["idle"].AddTransition(states["walk"]);
                t.hasExitTime = false;
                t.duration = TRANSITION_DURATION;
                t.AddCondition(AnimatorConditionMode.Greater, WALK_THRESHOLD, speedParam);

                t = states["walk"].AddTransition(states["idle"]);
                t.hasExitTime = false;
                t.duration = TRANSITION_DURATION;
                t.AddCondition(AnimatorConditionMode.Less, WALK_THRESHOLD, speedParam);
            }
        }

        // ── Utility ──

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
