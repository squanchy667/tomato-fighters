using System.Collections.Generic;
using TomatoFighters.Editor.VFX;
using TomatoFighters.Shared.Data;
using UnityEditor;
using UnityEngine;

namespace TomatoFighters.Editor
{
    /// <summary>
    /// Creates 12 T1 ability VFX prefabs and the AbilityVfxLookup SO asset.
    /// Run via <b>TomatoFighters → Create Ability VFX</b>.
    /// Re-running overwrites existing assets.
    /// </summary>
    public static class AbilityVfxCreator
    {
        private const string VFX_ROOT = "Assets/Prefabs/Effects/Abilities";
        private const string SO_ROOT = "Assets/ScriptableObjects/VFX";
        private const string LOOKUP_PATH = SO_ROOT + "/AbilityVfxLookup.asset";

        // Character color palette
        private static readonly ColorRGBA Orange = new() { r = 1f, g = 0.549f, b = 0f, a = 1f };        // #FF8C00
        private static readonly ColorRGBA Crimson = new() { r = 0.863f, g = 0.078f, b = 0.235f, a = 1f }; // #DC143C
        private static readonly ColorRGBA Emerald = new() { r = 0.314f, g = 0.784f, b = 0.471f, a = 1f }; // #50C878
        private static readonly ColorRGBA Gold = new() { r = 1f, g = 0.843f, b = 0f, a = 1f };            // #FFD700
        private static readonly ColorRGBA Purple = new() { r = 0.6f, g = 0.2f, b = 0.8f, a = 1f };
        private static readonly ColorRGBA Cyan = new() { r = 0f, g = 0.8f, b = 0.8f, a = 1f };
        private static readonly ColorRGBA BlueOrange = new() { r = 0.4f, g = 0.6f, b = 1f, a = 1f };
        private static readonly ColorRGBA Transparent = new() { r = 1f, g = 1f, b = 1f, a = 0f };

        [MenuItem("TomatoFighters/Create Ability VFX")]
        public static void CreateAll()
        {
            EnsureFolder(VFX_ROOT);
            EnsureFolder(SO_ROOT);

            var entries = new List<AbilityVfxLookup.AbilityVfxEntry>();

            // ── Brutor (Orange) ────────────────────────────────────────
            entries.Add(CreateVfx("Warden_Provoke", BuildProvoke()));
            entries.Add(CreateVfx("Bulwark_IronGuard", BuildIronGuard()));
            entries.Add(CreateVfx("Guardian_ShieldLink", BuildShieldLink()));

            // ── Slasher (Crimson Red) ──────────────────────────────────
            entries.Add(CreateVfx("Executioner_MarkForDeath", BuildMarkForDeath()));
            entries.Add(CreateVfx("Reaper_CleavingStrikes", BuildCleavingStrikes()));
            entries.Add(CreateVfx("Shadow_PhaseDash", BuildPhaseDash()));

            // ── Mystica (Emerald Green) ────────────────────────────────
            entries.Add(CreateVfx("Sage_MendingAura", BuildMendingAura()));
            entries.Add(CreateVfx("Enchanter_Empower", BuildEmpower()));
            entries.Add(CreateVfx("Conjurer_SummonSproutling", BuildSummonSproutling()));

            // ── Viper (Gold Yellow) ────────────────────────────────────
            entries.Add(CreateVfx("Marksman_PiercingShots", BuildPiercingShots()));
            entries.Add(CreateVfx("Trapper_HarpoonShot", BuildHarpoonShot()));
            entries.Add(CreateVfx("Arcanist_ManaCharge", BuildManaCharge()));

            // ── Create AbilityVfxLookup SO ─────────────────────────────
            CreateLookupAsset(entries.ToArray());

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[T034] Created {entries.Count} VFX prefabs + AbilityVfxLookup at {LOOKUP_PATH}");
        }

        // ── Brutor VFX ──────────────────────────────────────────────────

        /// <summary>Orange radial pulse — burst AoE, 0.5s, 30 particles.</summary>
        private static ParticleConfig BuildProvoke()
        {
            return new ParticleConfig
            {
                main = new MainModuleConfig
                {
                    duration = 0.5f, looping = false, startLifetime = 0.4f,
                    startSpeed = 6f, startSize = 0.3f, startColor = Orange,
                    simulationSpace = "World", maxParticles = 30
                },
                emission = new EmissionConfig
                {
                    rateOverTime = 0f,
                    bursts = new[] { new BurstConfig { time = 0f, count = 30 } }
                },
                shape = new ShapeConfig
                {
                    shapeType = "Circle", radius = 0.5f, arc = 360f, randomDirection = false
                },
                colorOverLifetime = new ColorOverLifetimeConfig
                {
                    gradient = new[]
                    {
                        new GradientStop { time = 0f, color = Orange },
                        new GradientStop { time = 1f, color = new ColorRGBA { r = 1f, g = 0.549f, b = 0f, a = 0f } }
                    }
                },
                sizeOverLifetime = new SizeOverLifetimeConfig
                {
                    curve = new[] { new CurvePoint { time = 0f, value = 1f }, new CurvePoint { time = 1f, value = 0.2f } }
                }
            };
        }

        /// <summary>Blue-orange shield glow — sustained aura, looping, 20 particles.</summary>
        private static ParticleConfig BuildIronGuard()
        {
            return new ParticleConfig
            {
                main = new MainModuleConfig
                {
                    duration = 1f, looping = true, startLifetime = 0.8f,
                    startSpeed = 0.5f, startSize = 0.4f, startColor = BlueOrange,
                    simulationSpace = "Local", maxParticles = 20
                },
                emission = new EmissionConfig { rateOverTime = 15f },
                shape = new ShapeConfig
                {
                    shapeType = "Circle", radius = 0.6f, arc = 360f, randomDirection = true
                },
                colorOverLifetime = new ColorOverLifetimeConfig
                {
                    gradient = new[]
                    {
                        new GradientStop { time = 0f, color = BlueOrange },
                        new GradientStop { time = 0.5f, color = Orange },
                        new GradientStop { time = 1f, color = new ColorRGBA { r = 0.4f, g = 0.6f, b = 1f, a = 0f } }
                    }
                },
                sizeOverLifetime = new SizeOverLifetimeConfig
                {
                    curve = new[] { new CurvePoint { time = 0f, value = 0.8f }, new CurvePoint { time = 1f, value = 0.1f } }
                },
                noise = new NoiseConfig { strength = 0.3f, frequency = 1f, octaves = 1 }
            };
        }

        /// <summary>Gold beam/tether — sustained, looping, 15 particles.</summary>
        private static ParticleConfig BuildShieldLink()
        {
            return new ParticleConfig
            {
                main = new MainModuleConfig
                {
                    duration = 1f, looping = true, startLifetime = 0.6f,
                    startSpeed = 0.3f, startSize = 0.3f, startColor = Gold,
                    simulationSpace = "Local", maxParticles = 15
                },
                emission = new EmissionConfig { rateOverTime = 12f },
                shape = new ShapeConfig
                {
                    shapeType = "Circle", radius = 0.4f, arc = 360f, randomDirection = true
                },
                colorOverLifetime = new ColorOverLifetimeConfig
                {
                    gradient = new[]
                    {
                        new GradientStop { time = 0f, color = Gold },
                        new GradientStop { time = 1f, color = new ColorRGBA { r = 1f, g = 0.843f, b = 0f, a = 0f } }
                    }
                },
                sizeOverLifetime = new SizeOverLifetimeConfig
                {
                    curve = new[] { new CurvePoint { time = 0f, value = 1f }, new CurvePoint { time = 1f, value = 0.3f } }
                }
            };
        }

        // ── Slasher VFX ─────────────────────────────────────────────────

        /// <summary>Red pulsing marker — target-placed, 6s (managed by ability), 10 particles.</summary>
        private static ParticleConfig BuildMarkForDeath()
        {
            return new ParticleConfig
            {
                main = new MainModuleConfig
                {
                    duration = 6f, looping = true, startLifetime = 1f,
                    startSpeed = 0.2f, startSize = 0.5f, startColor = Crimson,
                    simulationSpace = "Local", maxParticles = 10
                },
                emission = new EmissionConfig { rateOverTime = 5f },
                shape = new ShapeConfig
                {
                    shapeType = "Circle", radius = 0.2f, arc = 360f, randomDirection = true
                },
                colorOverLifetime = new ColorOverLifetimeConfig
                {
                    gradient = new[]
                    {
                        new GradientStop { time = 0f, color = Crimson },
                        new GradientStop { time = 0.5f, color = new ColorRGBA { r = 1f, g = 0.3f, b = 0.3f, a = 0.8f } },
                        new GradientStop { time = 1f, color = new ColorRGBA { r = 0.863f, g = 0.078f, b = 0.235f, a = 0f } }
                    }
                },
                sizeOverLifetime = new SizeOverLifetimeConfig
                {
                    curve = new[]
                    {
                        new CurvePoint { time = 0f, value = 0.8f },
                        new CurvePoint { time = 0.5f, value = 1.2f },
                        new CurvePoint { time = 1f, value = 0.8f }
                    }
                }
            };
        }

        /// <summary>Red arc/slash trail — on-hit burst, 0.3s, 25 particles.</summary>
        private static ParticleConfig BuildCleavingStrikes()
        {
            return new ParticleConfig
            {
                main = new MainModuleConfig
                {
                    duration = 0.3f, looping = false, startLifetime = 0.25f,
                    startSpeed = 4f, startSize = 0.25f, startColor = Crimson,
                    simulationSpace = "World", maxParticles = 25
                },
                emission = new EmissionConfig
                {
                    rateOverTime = 0f,
                    bursts = new[] { new BurstConfig { time = 0f, count = 25 } }
                },
                shape = new ShapeConfig
                {
                    shapeType = "Circle", radius = 0.8f, arc = 120f, randomDirection = false
                },
                colorOverLifetime = new ColorOverLifetimeConfig
                {
                    gradient = new[]
                    {
                        new GradientStop { time = 0f, color = Crimson },
                        new GradientStop { time = 1f, color = new ColorRGBA { r = 0.863f, g = 0.078f, b = 0.235f, a = 0f } }
                    }
                },
                sizeOverLifetime = new SizeOverLifetimeConfig
                {
                    curve = new[] { new CurvePoint { time = 0f, value = 1f }, new CurvePoint { time = 1f, value = 0.1f } }
                },
                renderer = new RendererConfig { renderMode = "StretchedBillboard", lengthScale = 2f, speedScale = 0.3f }
            };
        }

        /// <summary>Purple afterimage particles — trail effect, 0.6s, 40 particles.</summary>
        private static ParticleConfig BuildPhaseDash()
        {
            return new ParticleConfig
            {
                main = new MainModuleConfig
                {
                    duration = 0.6f, looping = false, startLifetime = 0.5f,
                    startSpeed = 1f, startSize = 0.5f, startColor = Purple,
                    simulationSpace = "World", maxParticles = 40
                },
                emission = new EmissionConfig
                {
                    rateOverTime = 0f,
                    bursts = new[] { new BurstConfig { time = 0f, count = 40 } }
                },
                shape = new ShapeConfig
                {
                    shapeType = "Sphere", radius = 0.3f, randomDirection = true
                },
                colorOverLifetime = new ColorOverLifetimeConfig
                {
                    gradient = new[]
                    {
                        new GradientStop { time = 0f, color = Purple },
                        new GradientStop { time = 1f, color = new ColorRGBA { r = 0.6f, g = 0.2f, b = 0.8f, a = 0f } }
                    }
                },
                sizeOverLifetime = new SizeOverLifetimeConfig
                {
                    curve = new[]
                    {
                        new CurvePoint { time = 0f, value = 1f },
                        new CurvePoint { time = 0.3f, value = 0.6f },
                        new CurvePoint { time = 1f, value = 0f }
                    }
                },
                noise = new NoiseConfig { strength = 0.5f, frequency = 2f, octaves = 2 }
            };
        }

        // ── Mystica VFX ─────────────────────────────────────────────────

        /// <summary>Green heal particles — sustained aura, looping, 20 particles.</summary>
        private static ParticleConfig BuildMendingAura()
        {
            return new ParticleConfig
            {
                main = new MainModuleConfig
                {
                    duration = 1f, looping = true, startLifetime = 1f,
                    startSpeed = 0.8f, startSize = 0.2f, startColor = Emerald,
                    simulationSpace = "Local", maxParticles = 20, gravityModifier = -0.3f
                },
                emission = new EmissionConfig { rateOverTime = 12f },
                shape = new ShapeConfig
                {
                    shapeType = "Circle", radius = 0.5f, arc = 360f, randomDirection = true
                },
                colorOverLifetime = new ColorOverLifetimeConfig
                {
                    gradient = new[]
                    {
                        new GradientStop { time = 0f, color = Emerald },
                        new GradientStop { time = 0.7f, color = new ColorRGBA { r = 0.5f, g = 1f, b = 0.5f, a = 0.8f } },
                        new GradientStop { time = 1f, color = new ColorRGBA { r = 0.314f, g = 0.784f, b = 0.471f, a = 0f } }
                    }
                },
                sizeOverLifetime = new SizeOverLifetimeConfig
                {
                    curve = new[] { new CurvePoint { time = 0f, value = 0.5f }, new CurvePoint { time = 1f, value = 0f } }
                },
                noise = new NoiseConfig { strength = 0.2f, frequency = 0.8f, octaves = 1 }
            };
        }

        /// <summary>Cyan/teal buff glow — sustained aura, looping, 20 particles.</summary>
        private static ParticleConfig BuildEmpower()
        {
            return new ParticleConfig
            {
                main = new MainModuleConfig
                {
                    duration = 1f, looping = true, startLifetime = 0.7f,
                    startSpeed = 1f, startSize = 0.3f, startColor = Cyan,
                    simulationSpace = "Local", maxParticles = 20, gravityModifier = -0.5f
                },
                emission = new EmissionConfig { rateOverTime = 15f },
                shape = new ShapeConfig
                {
                    shapeType = "Circle", radius = 0.4f, arc = 360f, randomDirection = true
                },
                colorOverLifetime = new ColorOverLifetimeConfig
                {
                    gradient = new[]
                    {
                        new GradientStop { time = 0f, color = Cyan },
                        new GradientStop { time = 1f, color = new ColorRGBA { r = 0f, g = 0.8f, b = 0.8f, a = 0f } }
                    }
                },
                sizeOverLifetime = new SizeOverLifetimeConfig
                {
                    curve = new[] { new CurvePoint { time = 0f, value = 1f }, new CurvePoint { time = 1f, value = 0.2f } }
                }
            };
        }

        /// <summary>Green leafy burst — spawn burst, 0.5s, 30 particles.</summary>
        private static ParticleConfig BuildSummonSproutling()
        {
            return new ParticleConfig
            {
                main = new MainModuleConfig
                {
                    duration = 0.5f, looping = false, startLifetime = 0.4f,
                    startSpeed = 3f, startSize = 0.25f, startColor = Emerald,
                    simulationSpace = "World", maxParticles = 30, gravityModifier = 0.5f
                },
                emission = new EmissionConfig
                {
                    rateOverTime = 0f,
                    bursts = new[] { new BurstConfig { time = 0f, count = 30 } }
                },
                shape = new ShapeConfig
                {
                    shapeType = "Cone", angle = 40f, radius = 0.3f
                },
                colorOverLifetime = new ColorOverLifetimeConfig
                {
                    gradient = new[]
                    {
                        new GradientStop { time = 0f, color = Emerald },
                        new GradientStop { time = 0.5f, color = new ColorRGBA { r = 0.2f, g = 0.6f, b = 0.2f, a = 0.8f } },
                        new GradientStop { time = 1f, color = new ColorRGBA { r = 0.314f, g = 0.784f, b = 0.471f, a = 0f } }
                    }
                },
                sizeOverLifetime = new SizeOverLifetimeConfig
                {
                    curve = new[] { new CurvePoint { time = 0f, value = 1f }, new CurvePoint { time = 1f, value = 0.3f } }
                },
                rotationOverLifetime = new RotationOverLifetimeConfig { angularVelocity = 90f }
            };
        }

        // ── Viper VFX ───────────────────────────────────────────────────

        /// <summary>Yellow arrow trail — sustained passive indicator, looping, 15 particles.</summary>
        private static ParticleConfig BuildPiercingShots()
        {
            return new ParticleConfig
            {
                main = new MainModuleConfig
                {
                    duration = 1f, looping = true, startLifetime = 0.5f,
                    startSpeed = 2f, startSize = 0.15f, startColor = Gold,
                    simulationSpace = "Local", maxParticles = 15
                },
                emission = new EmissionConfig { rateOverTime = 10f },
                shape = new ShapeConfig
                {
                    shapeType = "Edge", radius = 0.3f
                },
                colorOverLifetime = new ColorOverLifetimeConfig
                {
                    gradient = new[]
                    {
                        new GradientStop { time = 0f, color = Gold },
                        new GradientStop { time = 1f, color = new ColorRGBA { r = 1f, g = 0.843f, b = 0f, a = 0f } }
                    }
                },
                sizeOverLifetime = new SizeOverLifetimeConfig
                {
                    curve = new[] { new CurvePoint { time = 0f, value = 1f }, new CurvePoint { time = 1f, value = 0.3f } }
                },
                renderer = new RendererConfig { renderMode = "StretchedBillboard", lengthScale = 1.5f, speedScale = 0.2f }
            };
        }

        /// <summary>Yellow chain-link trail — projectile burst, 0.4s, 25 particles.</summary>
        private static ParticleConfig BuildHarpoonShot()
        {
            return new ParticleConfig
            {
                main = new MainModuleConfig
                {
                    duration = 0.4f, looping = false, startLifetime = 0.35f,
                    startSpeed = 8f, startSize = 0.15f, startColor = Gold,
                    simulationSpace = "World", maxParticles = 25
                },
                emission = new EmissionConfig
                {
                    rateOverTime = 0f,
                    bursts = new[] { new BurstConfig { time = 0f, count = 25 } }
                },
                shape = new ShapeConfig
                {
                    shapeType = "Cone", angle = 5f, radius = 0.1f
                },
                colorOverLifetime = new ColorOverLifetimeConfig
                {
                    gradient = new[]
                    {
                        new GradientStop { time = 0f, color = Gold },
                        new GradientStop { time = 1f, color = new ColorRGBA { r = 1f, g = 0.843f, b = 0f, a = 0f } }
                    }
                },
                sizeOverLifetime = new SizeOverLifetimeConfig
                {
                    curve = new[] { new CurvePoint { time = 0f, value = 1f }, new CurvePoint { time = 1f, value = 0.1f } }
                },
                renderer = new RendererConfig { renderMode = "StretchedBillboard", lengthScale = 3f, speedScale = 0.5f }
            };
        }

        /// <summary>Purple energy spiral — sustained charge, looping, 40 particles.</summary>
        private static ParticleConfig BuildManaCharge()
        {
            return new ParticleConfig
            {
                main = new MainModuleConfig
                {
                    duration = 1f, looping = true, startLifetime = 0.6f,
                    startSpeed = 1.5f, startSize = 0.2f, startColor = Purple,
                    simulationSpace = "Local", maxParticles = 40
                },
                emission = new EmissionConfig { rateOverTime = 25f },
                shape = new ShapeConfig
                {
                    shapeType = "Circle", radius = 0.3f, arc = 360f, randomDirection = true
                },
                colorOverLifetime = new ColorOverLifetimeConfig
                {
                    gradient = new[]
                    {
                        new GradientStop { time = 0f, color = Purple },
                        new GradientStop { time = 0.5f, color = new ColorRGBA { r = 0.7f, g = 0.3f, b = 1f, a = 0.9f } },
                        new GradientStop { time = 1f, color = new ColorRGBA { r = 0.6f, g = 0.2f, b = 0.8f, a = 0f } }
                    }
                },
                sizeOverLifetime = new SizeOverLifetimeConfig
                {
                    curve = new[] { new CurvePoint { time = 0f, value = 0.5f }, new CurvePoint { time = 1f, value = 0.1f } }
                },
                noise = new NoiseConfig { strength = 0.4f, frequency = 1.5f, octaves = 2, scrollSpeed = 1f },
                rotationOverLifetime = new RotationOverLifetimeConfig { angularVelocity = 180f }
            };
        }

        // ── Helpers ─────────────────────────────────────────────────────

        /// <summary>
        /// Creates a VFX prefab from the given ParticleConfig and returns an AbilityVfxEntry.
        /// </summary>
        private static AbilityVfxLookup.AbilityVfxEntry CreateVfx(string abilityId, ParticleConfig config)
        {
            string prefabPath = $"{VFX_ROOT}/{abilityId}_VFX.prefab";

            // Build the particle system via the existing applier
            var go = ParticleSystemApplier.Apply(config, $"{abilityId}_VFX");

            // Save as prefab (overwrite if exists)
            var existingPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            GameObject savedPrefab;
            if (existingPrefab != null)
            {
                savedPrefab = PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
                Debug.Log($"[T034] Updated: {prefabPath}");
            }
            else
            {
                savedPrefab = PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
                Debug.Log($"[T034] Created: {prefabPath}");
            }

            // Clean up scene instance
            Object.DestroyImmediate(go);

            return new AbilityVfxLookup.AbilityVfxEntry
            {
                abilityId = abilityId,
                vfxPrefab = savedPrefab
            };
        }

        /// <summary>
        /// Creates or updates the AbilityVfxLookup SO with all VFX entries.
        /// </summary>
        private static void CreateLookupAsset(AbilityVfxLookup.AbilityVfxEntry[] entries)
        {
            var existing = AssetDatabase.LoadAssetAtPath<AbilityVfxLookup>(LOOKUP_PATH);
            if (existing != null)
            {
                existing.SetEntries(entries);
                EditorUtility.SetDirty(existing);
                Debug.Log($"[T034] Updated: {LOOKUP_PATH}");
            }
            else
            {
                var lookup = ScriptableObject.CreateInstance<AbilityVfxLookup>();
                lookup.SetEntries(entries);
                AssetDatabase.CreateAsset(lookup, LOOKUP_PATH);
                Debug.Log($"[T034] Created: {LOOKUP_PATH}");
            }
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;

            string[] parts = path.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }
    }
}
