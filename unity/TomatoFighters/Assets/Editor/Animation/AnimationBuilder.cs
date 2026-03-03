using System.Collections.Generic;
using System.Linq;
using TomatoFighters.Combat;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace TomatoFighters.Editor.Animation
{
    /// <summary>
    /// <b>Animation Pipeline — Step 2 of 2 (Editor).</b>
    /// Creates <c>.anim</c> clips from sliced sprite sheets and builds an <c>AnimatorController</c>.
    /// Fully data-driven — reads all animations from metadata.json with no hardcoded names.
    /// Run via menu: <b>TomatoFighters &gt; Build Animations</b>.
    ///
    /// <para><b>Prerequisites:</b> Run <see cref="SpriteSheetImporter"/> (Step 1) first to
    /// generate the sliced sprite assets this step consumes.</para>
    ///
    /// <para><b>Output:</b> <c>Assets/Animations/TomatoFighter/</c> —
    /// <c>.anim</c> clips + <c>TomatoFighter_Controller.controller</c>.</para>
    ///
    /// <para><b>Animation categories (determined by <c>loop</c> field in metadata):</b></para>
    /// <list type="bullet">
    ///   <item><b>Locomotion</b> (<c>loop: true</c>) — idle, walk, run.
    ///     Wired as states with <c>Speed</c> float transitions.
    ///     idle↔walk at 0.1, walk↔run at 0.9, run→idle direct.
    ///     Driven at runtime by <see cref="TomatoFighters.Combat.CharacterAnimationBridge"/>.</item>
    ///   <item><b>Action</b> (<c>loop: false</c>) — attacks, hurt, death, etc.
    ///     Each gets a trigger parameter (<c>{animName}Trigger</c>) and a state that plays once,
    ///     then transitions back to idle via Exit Time.
    ///     Driven at runtime by <see cref="TomatoFighters.Combat.ComboController"/>.</item>
    /// </list>
    ///
    /// <para><b>Adding new animations:</b> Drop PNG + update metadata.json.
    /// Looping anims auto-become locomotion states; non-looping become action triggers.
    /// Re-run Import Sprite Sheets, then Build Animations.</para>
    ///
    /// <para><b>Binding path:</b> Curves target <c>""</c> (empty) because the Animator
    /// and SpriteRenderer share the same Sprite child GameObject.</para>
    /// </summary>
    /// <seealso cref="AnimationForgeMetadata"/>
    /// <seealso cref="SpriteSheetImporter"/>
    /// <seealso cref="TomatoFighters.Combat.TomatoFighterAnimatorParams"/>
    /// <seealso cref="TomatoFighters.Combat.CharacterAnimationBridge"/>
    public static class AnimationBuilder
    {
        private const string OUTPUT_FOLDER = "Assets/Animations/TomatoFighter";
        private const string CONTROLLER_PATH = OUTPUT_FOLDER + "/TomatoFighter_Controller.controller";

        // Locomotion state layout — order matters for Speed threshold wiring
        private static readonly string[] LOCOMOTION_ORDER = { "idle", "walk", "run" };

        // Speed thresholds: idle < 0.1 < walk < 0.9 < run
        private const float WALK_THRESHOLD = 0.1f;
        private const float RUN_THRESHOLD = 0.9f;
        private const float TRANSITION_DURATION = 0.05f;

        [MenuItem("TomatoFighters/Build Animations")]
        public static void BuildAnimations()
        {
            var metadata = AnimationForgeMetadata.Load();
            if (metadata == null) return;

            EnsureFolderExists(OUTPUT_FOLDER);

            // Create clips for ALL animations
            var clips = new Dictionary<string, AnimationClip>();
            foreach (var kvp in metadata.animations)
            {
                string animName = kvp.Key;
                var entry = kvp.Value;

                string sheetPath = AnimationForgeMetadata.GetSheetPath(metadata.characterName, animName);
                var clip = CreateClip(animName, entry, sheetPath);
                if (clip != null)
                    clips[animName] = clip;
            }

            if (clips.Count == 0)
            {
                Debug.LogError("[AnimationBuilder] No clips created. Ensure sprite sheets are imported and sliced.");
                return;
            }

            // Split into locomotion (loop=true) and action (loop=false)
            var locomotion = new Dictionary<string, AnimationClip>();
            var actions = new Dictionary<string, AnimationClip>();

            foreach (var kvp in clips)
            {
                if (metadata.animations[kvp.Key].loop)
                    locomotion[kvp.Key] = kvp.Value;
                else
                    actions[kvp.Key] = kvp.Value;
            }

            // Build the controller
            BuildController(locomotion, actions);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[AnimationBuilder] Created {clips.Count} clips ({locomotion.Count} locomotion, {actions.Count} action) at {OUTPUT_FOLDER}");
        }

        /// <summary>Creates a single .anim clip from a sliced sprite sheet.</summary>
        private static AnimationClip CreateClip(string animName, AnimationForgeMetadata.AnimationEntry entry, string sheetPath)
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
            clip.name = $"tomato_fighter_{animName}";
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

            string clipPath = $"{OUTPUT_FOLDER}/tomato_fighter_{animName}.anim";

            // Overwrite existing clip asset if present
            var existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
            if (existing != null)
                AssetDatabase.DeleteAsset(clipPath);

            AssetDatabase.CreateAsset(clip, clipPath);
            Debug.Log($"[AnimationBuilder] {animName}: {sprites.Length} frames, {entry.fps}fps, loop={entry.loop}");
            return clip;
        }

        /// <summary>
        /// Builds the AnimatorController with locomotion states (Speed-driven)
        /// and action states (trigger-driven, return to idle on exit).
        /// </summary>
        private static void BuildController(
            Dictionary<string, AnimationClip> locomotion,
            Dictionary<string, AnimationClip> actions)
        {
            // Delete existing controller to start clean
            if (AssetDatabase.LoadAssetAtPath<AnimatorController>(CONTROLLER_PATH) != null)
                AssetDatabase.DeleteAsset(CONTROLLER_PATH);

            var controller = AnimatorController.CreateAnimatorControllerAtPath(CONTROLLER_PATH);
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

            // --- Locomotion states ---
            // Place them in the order defined by LOCOMOTION_ORDER for consistent layout
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

            // Add any locomotion anims not in the predefined order (future-proofing)
            foreach (var kvp in locomotion)
            {
                if (locoStates.ContainsKey(kvp.Key)) continue;

                var state = rootSM.AddState(kvp.Key, new Vector3(300, yOffset, 0));
                state.motion = kvp.Value;
                locoStates[kvp.Key] = state;
                yOffset += 80f;
            }

            // Set idle as default if it exists
            if (locoStates.ContainsKey("idle"))
                rootSM.defaultState = locoStates["idle"];

            // Wire locomotion transitions: idle ↔ walk ↔ run
            WireLocomotionTransitions(locoStates);

            // --- Action states ---
            // Each gets a trigger parameter and a state that returns to idle on exit
            var idleState = locoStates.ContainsKey("idle") ? locoStates["idle"] : null;
            float actionY = 0f;

            foreach (var kvp in actions)
            {
                string triggerName = kvp.Key + "Trigger";
                controller.AddParameter(triggerName, AnimatorControllerParameterType.Trigger);

                var state = rootSM.AddState(kvp.Key, new Vector3(600, actionY, 0));
                state.motion = kvp.Value;
                actionY += 80f;

                // Any state → action state (via trigger)
                var triggerTransition = rootSM.AddAnyStateTransition(state);
                triggerTransition.hasExitTime = false;
                triggerTransition.duration = 0f;
                triggerTransition.AddCondition(AnimatorConditionMode.If, 0, triggerName);

                // Action state → idle (via exit time, plays clip once)
                if (idleState != null)
                {
                    var exitTransition = state.AddTransition(idleState);
                    exitTransition.hasExitTime = true;
                    exitTransition.exitTime = 1f;
                    exitTransition.duration = 0.05f;
                }
            }

            EditorUtility.SetDirty(controller);

            int locoCount = locoStates.Count;
            int actionCount = actions.Count;
            Debug.Log($"[AnimationBuilder] Controller built: {locoCount} locomotion states, {actionCount} action states.");
        }

        /// <summary>
        /// Wires Speed-based transitions between known locomotion states.
        /// idle ↔ walk at WALK_THRESHOLD, walk ↔ run at RUN_THRESHOLD, run → idle direct.
        /// </summary>
        private static void WireLocomotionTransitions(Dictionary<string, AnimatorState> states)
        {
            string speedParam = TomatoFighterAnimatorParams.SPEED;

            // idle ↔ walk
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

            // walk ↔ run
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

            // run → idle (direct path for when run resets: stop, attack, dash, jump)
            if (states.ContainsKey("run") && states.ContainsKey("idle"))
            {
                var t = states["run"].AddTransition(states["idle"]);
                t.hasExitTime = false;
                t.duration = TRANSITION_DURATION;
                t.AddCondition(AnimatorConditionMode.Less, WALK_THRESHOLD, speedParam);
            }
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
