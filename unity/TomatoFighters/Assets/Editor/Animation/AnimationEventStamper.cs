using System.Collections.Generic;
using System.Linq;
using TomatoFighters.Combat;
using TomatoFighters.Shared.Data;
using UnityEditor;
using UnityEngine;

namespace TomatoFighters.Editor.Animation
{
    /// <summary>
    /// <b>Animation Pipeline — Step 3 (Editor).</b>
    /// Stamps Animation Events onto attack <c>.anim</c> clips at frame-accurate times
    /// derived from <see cref="AttackData"/> ScriptableObjects.
    ///
    /// <para><b>Events stamped per attack clip:</b></para>
    /// <list type="number">
    ///   <item><c>ActivateHitbox()</c> at <c>hitboxStartFrame</c> — calls HitboxManager</item>
    ///   <item><c>DeactivateHitbox()</c> at <c>hitboxStartFrame + hitboxActiveFrames</c> — calls HitboxManager</item>
    ///   <item><c>OnComboWindowOpen()</c> after hitbox ends — calls ComboController</item>
    ///   <item><c>OnFinisherEnd()</c> at last frame of finisher clips — calls ComboController</item>
    /// </list>
    ///
    /// <para><b>Separate from AnimationBuilder (DD-4):</b> Events depend on AttackData SOs
    /// (a different data source than metadata.json). Re-stamping when timing changes
    /// doesn't require rebuilding all clips.</para>
    ///
    /// <para><b>Usage:</b> <c>TomatoFighters &gt; Stamp Animation Events</c></para>
    /// </summary>
    /// <seealso cref="AnimationBuilder"/>
    /// <seealso cref="AnimationForgeMetadata"/>
    public static class AnimationEventStamper
    {
        private const string ATTACKS_ROOT = "Assets/ScriptableObjects/Attacks";

        [MenuItem("TomatoFighters/Stamp Animation Events")]
        public static void StampAllEvents()
        {
            int totalStamped = 0;
            int totalSkipped = 0;

            // Build finisher lookup from ComboDefinitions
            var finisherAttacks = BuildFinisherLookup();

            foreach (var kvp in AnimationForgeMetadata.Characters)
            {
                string charName = kvp.Key;
                string outputFolder = kvp.Value.outputFolder;

                if (!AnimationForgeMetadata.AttackSlotMappings.ContainsKey(charName))
                {
                    Debug.LogWarning($"[AnimationEventStamper] No attack slot mapping for {charName}. Skipping.");
                    totalSkipped++;
                    continue;
                }

                var slotMapping = AnimationForgeMetadata.AttackSlotMappings[charName];
                int charStamped = 0;

                foreach (var slot in slotMapping)
                {
                    string slotName = slot.Key;
                    string attackAssetName = slot.Value;

                    // Load AttackData SO
                    var attackData = LoadAttackData(charName, attackAssetName);
                    if (attackData == null)
                    {
                        Debug.LogWarning($"[AnimationEventStamper] {charName}: AttackData '{attackAssetName}' not found. Skipping slot {slotName}.");
                        totalSkipped++;
                        continue;
                    }

                    // Find the animation clip for this slot
                    var clip = FindClipForSlot(charName, slotName, outputFolder);
                    if (clip == null)
                    {
                        Debug.LogWarning($"[AnimationEventStamper] {charName}: no clip found for slot {slotName}. Skipping.");
                        totalSkipped++;
                        continue;
                    }

                    // Skip placeholder clips (single-frame, no meaningful timing)
                    if (clip.name.EndsWith("_placeholder"))
                    {
                        totalSkipped++;
                        continue;
                    }

                    bool isFinisher = finisherAttacks.Contains(attackData);
                    StampEventsOnClip(clip, attackData, isFinisher);
                    charStamped++;
                }

                if (charStamped > 0)
                    Debug.Log($"[AnimationEventStamper] {charName}: stamped events on {charStamped} attack clips.");
                totalStamped += charStamped;
            }

            // ── Enemy clips ──
            foreach (var kvp in AnimationForgeMetadata.EnemyCharacters)
            {
                string enemyKey = kvp.Key;
                string outputFolder = kvp.Value.outputFolder;

                if (!AnimationForgeMetadata.EnemyAttackSlotMappings.ContainsKey(enemyKey))
                    continue;

                var slotMapping = AnimationForgeMetadata.EnemyAttackSlotMappings[enemyKey];
                int enemyStamped = 0;

                foreach (var slot in slotMapping)
                {
                    string slotName = slot.Key;
                    string attackAssetName = slot.Value;

                    // Load from Attacks/Enemy/ subfolder
                    var attackData = LoadEnemyAttackData(enemyKey, attackAssetName);
                    if (attackData == null)
                    {
                        totalSkipped++;
                        continue;
                    }

                    var clip = FindClipForSlot(enemyKey, slotName, outputFolder);
                    if (clip == null || clip.name.EndsWith("_placeholder"))
                    {
                        totalSkipped++;
                        continue;
                    }

                    // Enemies only get ActivateHitbox/DeactivateHitbox — no combo events
                    StampEnemyEventsOnClip(clip, attackData);
                    enemyStamped++;
                }

                if (enemyStamped > 0)
                    Debug.Log($"[AnimationEventStamper] Enemy {enemyKey}: stamped events on {enemyStamped} attack clips.");
                totalStamped += enemyStamped;
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[AnimationEventStamper] Done — {totalStamped} clips stamped, {totalSkipped} skipped.");
        }

        /// <summary>
        /// Stamps hitbox-only events on enemy attack clips (no combo window or finisher events).
        /// </summary>
        private static void StampEnemyEventsOnClip(AnimationClip clip, AttackData attackData)
        {
            float fps = clip.frameRate;
            if (fps <= 0) fps = 12f;

            float frameDuration = 1f / fps;
            var events = new List<AnimationEvent>();

            int startFrame = attackData.hitboxStartFrame;
            events.Add(new AnimationEvent
            {
                time = startFrame * frameDuration,
                functionName = "ActivateHitbox"
            });

            int endFrame = startFrame + attackData.hitboxActiveFrames;
            events.Add(new AnimationEvent
            {
                time = endFrame * frameDuration,
                functionName = "DeactivateHitbox"
            });

            events.Sort((a, b) => a.time.CompareTo(b.time));
            AnimationUtility.SetAnimationEvents(clip, events.ToArray());
            EditorUtility.SetDirty(clip);
        }

        /// <summary>Loads an AttackData SO from the Enemy subfolder.</summary>
        private static AttackData LoadEnemyAttackData(string enemyKey, string assetName)
        {
            // Try Attacks/Enemy/{enemyKey}/{assetName}.asset first
            string path = $"{ATTACKS_ROOT}/Enemy/{enemyKey}/{assetName}.asset";
            var data = AssetDatabase.LoadAssetAtPath<AttackData>(path);
            if (data != null) return data;

            // Fallback: Attacks/Enemy/{assetName}.asset
            path = $"{ATTACKS_ROOT}/Enemy/{assetName}.asset";
            return AssetDatabase.LoadAssetAtPath<AttackData>(path);
        }

        /// <summary>
        /// Stamps animation events onto a single attack clip based on AttackData timing.
        /// Clears any existing events first to allow re-stamping.
        /// </summary>
        private static void StampEventsOnClip(AnimationClip clip, AttackData attackData, bool isFinisher)
        {
            float fps = clip.frameRate;
            if (fps <= 0) fps = 12f;

            float frameDuration = 1f / fps;
            var events = new List<AnimationEvent>();

            // 1. ActivateHitbox at hitboxStartFrame
            int startFrame = attackData.hitboxStartFrame;
            events.Add(new AnimationEvent
            {
                time = startFrame * frameDuration,
                functionName = "ActivateHitbox"
            });

            // 2. DeactivateHitbox at hitboxStartFrame + hitboxActiveFrames
            int endFrame = startFrame + attackData.hitboxActiveFrames;
            events.Add(new AnimationEvent
            {
                time = endFrame * frameDuration,
                functionName = "DeactivateHitbox"
            });

            // 3. OnComboWindowOpen — 1 frame after hitbox deactivates
            int comboWindowFrame = endFrame + 1;
            float comboWindowTime = comboWindowFrame * frameDuration;
            // Clamp to clip length
            if (comboWindowTime < clip.length)
            {
                events.Add(new AnimationEvent
                {
                    time = comboWindowTime,
                    functionName = "OnComboWindowOpen"
                });
            }

            // 4. OnFinisherEnd at last frame (finisher clips only)
            if (isFinisher)
            {
                int totalFrames = attackData.totalFrames > 0 ? attackData.totalFrames : Mathf.RoundToInt(clip.length * fps);
                float lastFrameTime = (totalFrames - 1) * frameDuration;
                // Clamp to just before clip end to avoid exit-time conflicts
                lastFrameTime = Mathf.Min(lastFrameTime, clip.length - 0.001f);

                events.Add(new AnimationEvent
                {
                    time = Mathf.Max(0f, lastFrameTime),
                    functionName = "OnFinisherEnd"
                });
            }

            // Sort by time and apply
            events.Sort((a, b) => a.time.CompareTo(b.time));
            AnimationUtility.SetAnimationEvents(clip, events.ToArray());
            EditorUtility.SetDirty(clip);
        }

        /// <summary>
        /// Builds a set of AttackData SOs that are marked as finishers in any ComboDefinition.
        /// </summary>
        private static HashSet<AttackData> BuildFinisherLookup()
        {
            var finishers = new HashSet<AttackData>();

            // Find all ComboDefinition SOs
            var guids = AssetDatabase.FindAssets("t:ComboDefinition");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var comboDef = AssetDatabase.LoadAssetAtPath<ComboDefinition>(path);
                if (comboDef == null || comboDef.steps == null) continue;

                foreach (var step in comboDef.steps)
                {
                    if (step.isFinisher && step.attackData != null)
                        finishers.Add(step.attackData);
                }
            }

            return finishers;
        }

        /// <summary>Loads an AttackData SO by character folder and asset name.</summary>
        private static AttackData LoadAttackData(string characterName, string assetName)
        {
            string path = $"{ATTACKS_ROOT}/{characterName}/{assetName}.asset";
            return AssetDatabase.LoadAssetAtPath<AttackData>(path);
        }

        /// <summary>
        /// Finds the animation clip for a canonical attack slot.
        /// Searches the character's output folder for clips matching the slot name.
        /// </summary>
        private static AnimationClip FindClipForSlot(string characterName, string slotName, string outputFolder)
        {
            // Look for clips in the character's animation output folder
            var guids = AssetDatabase.FindAssets("t:AnimationClip", new[] { outputFolder });

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
                if (clip == null) continue;

                // Match by state name suffix (e.g., "blue_warrior_attack_1" contains "_attack_1")
                if (clip.name.EndsWith("_" + slotName) || clip.name.EndsWith("_" + slotName + "_placeholder"))
                    return clip;
            }

            return null;
        }
    }
}
