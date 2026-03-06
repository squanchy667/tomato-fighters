using UnityEditor;
using UnityEngine;

namespace TomatoFighters.Editor.VFX
{
    public static class ParticleSystemApplier
    {
        public static GameObject Apply(ParticleConfig config, string name)
        {
            var go = new GameObject(name);
            var ps = go.AddComponent<ParticleSystem>();

            // Main module
            var main = ps.main;
            main.duration = config.main.duration;
            main.loop = config.main.looping;
            main.startLifetime = config.main.startLifetime;
            main.startSpeed = config.main.startSpeed;
            main.startSize = config.main.startSize;
            main.startColor = ToColor(config.main.startColor);
            main.startRotation = config.main.startRotation * Mathf.Deg2Rad;
            main.gravityModifier = config.main.gravityModifier;
            main.simulationSpace = ParseSimulationSpace(config.main.simulationSpace);
            main.maxParticles = config.main.maxParticles;

            // Emission module
            var emission = ps.emission;
            emission.rateOverTime = config.emission.rateOverTime;
            if (config.emission.bursts != null && config.emission.bursts.Length > 0)
            {
                emission.burstCount = config.emission.bursts.Length;
                for (int i = 0; i < config.emission.bursts.Length; i++)
                {
                    var burst = config.emission.bursts[i];
                    emission.SetBurst(i, new ParticleSystem.Burst(burst.time, burst.count));
                }
            }

            // Shape module
            var shape = ps.shape;
            shape.shapeType = ParseShapeType(config.shape.shapeType);
            shape.angle = config.shape.angle;
            shape.radius = config.shape.radius;
            shape.arc = config.shape.arc;
            shape.randomDirectionAmount = config.shape.randomDirection ? 1f : 0f;

            // Color over Lifetime
            if (config.colorOverLifetime?.gradient != null && config.colorOverLifetime.gradient.Length > 0)
            {
                var col = ps.colorOverLifetime;
                col.enabled = true;
                col.color = BuildGradient(config.colorOverLifetime.gradient);
            }

            // Size over Lifetime
            if (config.sizeOverLifetime?.curve != null && config.sizeOverLifetime.curve.Length > 0)
            {
                var sol = ps.sizeOverLifetime;
                sol.enabled = true;
                sol.size = BuildCurve(config.sizeOverLifetime.curve);
            }

            // Velocity over Lifetime
            if (config.velocityOverLifetime != null &&
                (HasPoints(config.velocityOverLifetime.x) ||
                 HasPoints(config.velocityOverLifetime.y) ||
                 HasPoints(config.velocityOverLifetime.z)))
            {
                var vel = ps.velocityOverLifetime;
                vel.enabled = true;
                vel.space = ParseSimulationSpace(config.velocityOverLifetime.space);

                if (HasPoints(config.velocityOverLifetime.x))
                    vel.x = BuildCurve(config.velocityOverLifetime.x);
                if (HasPoints(config.velocityOverLifetime.y))
                    vel.y = BuildCurve(config.velocityOverLifetime.y);
                if (HasPoints(config.velocityOverLifetime.z))
                    vel.z = BuildCurve(config.velocityOverLifetime.z);

                vel.speedModifier = config.velocityOverLifetime.speedModifier;
            }

            // Noise module
            if (config.noise != null && config.noise.strength > 0)
            {
                var noise = ps.noise;
                noise.enabled = true;
                noise.strength = config.noise.strength;
                noise.frequency = config.noise.frequency;
                noise.octaveCount = Mathf.Clamp(config.noise.octaves, 1, 4);
                noise.scrollSpeed = config.noise.scrollSpeed;
                noise.damping = config.noise.damping > 0;
                // Separate axes for richer motion
                noise.separateAxes = true;
                noise.strengthX = config.noise.strength;
                noise.strengthY = config.noise.strength;
                noise.strengthZ = config.noise.strength * 0.5f; // less Z for 2D
            }

            // Rotation over Lifetime
            if (config.rotationOverLifetime != null)
            {
                bool hasCurve = config.rotationOverLifetime.curve != null &&
                                config.rotationOverLifetime.curve.Length > 0;
                bool hasConstant = config.rotationOverLifetime.angularVelocity != 0;

                if (hasCurve || hasConstant)
                {
                    var rot = ps.rotationOverLifetime;
                    rot.enabled = true;
                    if (hasCurve)
                        rot.z = BuildCurve(config.rotationOverLifetime.curve, Mathf.Deg2Rad);
                    else
                        rot.z = config.rotationOverLifetime.angularVelocity * Mathf.Deg2Rad;
                }
            }

            // Trails
            if (config.trails != null && config.trails.lifetime > 0)
            {
                var trails = ps.trails;
                trails.enabled = true;
                trails.lifetime = config.trails.lifetime;
                trails.widthOverTrail = config.trails.widthMultiplier;
                trails.minVertexDistance = config.trails.minimumVertexDistance;
                trails.dieWithParticles = config.trails.dieWithParticle;

                if (config.trails.colorGradient != null && config.trails.colorGradient.Length > 0)
                    trails.colorOverLifetime = BuildGradient(config.trails.colorGradient);

                if (config.trails.widthCurve != null && config.trails.widthCurve.Length > 0)
                    trails.widthOverTrail = BuildCurve(config.trails.widthCurve);
            }

            // Renderer
            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.material = AssetDatabase.GetBuiltinExtraResource<Material>(
                "Default-Particle.mat");

            if (config.renderer != null)
            {
                renderer.renderMode = ParseRenderMode(config.renderer.renderMode);
                renderer.lengthScale = config.renderer.lengthScale;
                renderer.velocityScale = config.renderer.speedScale;
                renderer.sortMode = ParseSortMode(config.renderer.sortMode);
                renderer.normalDirection = config.renderer.normalDirection;
            }

            // If trails are enabled, assign trail material too
            if (config.trails != null && config.trails.lifetime > 0)
            {
                renderer.trailMaterial = AssetDatabase.GetBuiltinExtraResource<Material>(
                    "Default-Particle.mat");
            }

            return go;
        }

        // --- Helpers ---

        private static bool HasPoints(CurvePoint[] points)
        {
            return points != null && points.Length > 0;
        }

        private static Color ToColor(ColorRGBA c)
        {
            if (c == null) return Color.white;
            return new Color(c.r, c.g, c.b, c.a);
        }

        private static ParticleSystemSimulationSpace ParseSimulationSpace(string space)
        {
            if (string.IsNullOrEmpty(space)) return ParticleSystemSimulationSpace.Local;
            switch (space.ToLower())
            {
                case "world": return ParticleSystemSimulationSpace.World;
                case "custom": return ParticleSystemSimulationSpace.Custom;
                default: return ParticleSystemSimulationSpace.Local;
            }
        }

        private static ParticleSystemShapeType ParseShapeType(string shapeType)
        {
            if (string.IsNullOrEmpty(shapeType)) return ParticleSystemShapeType.Cone;
            switch (shapeType.ToLower())
            {
                case "sphere": return ParticleSystemShapeType.Sphere;
                case "circle": return ParticleSystemShapeType.Circle;
                case "edge": return ParticleSystemShapeType.SingleSidedEdge;
                case "box": return ParticleSystemShapeType.Box;
                case "hemisphere": return ParticleSystemShapeType.Hemisphere;
                case "donut": return ParticleSystemShapeType.Donut;
                default: return ParticleSystemShapeType.Cone;
            }
        }

        private static ParticleSystemRenderMode ParseRenderMode(string mode)
        {
            if (string.IsNullOrEmpty(mode)) return ParticleSystemRenderMode.Billboard;
            switch (mode.ToLower())
            {
                case "stretchedbillboard": return ParticleSystemRenderMode.Stretch;
                case "stretch": return ParticleSystemRenderMode.Stretch;
                case "horizontalbillboard": return ParticleSystemRenderMode.HorizontalBillboard;
                case "verticalbillboard": return ParticleSystemRenderMode.VerticalBillboard;
                case "mesh": return ParticleSystemRenderMode.Mesh;
                default: return ParticleSystemRenderMode.Billboard;
            }
        }

        private static ParticleSystemSortMode ParseSortMode(string mode)
        {
            if (string.IsNullOrEmpty(mode)) return ParticleSystemSortMode.None;
            switch (mode.ToLower())
            {
                case "distance": return ParticleSystemSortMode.Distance;
                case "oldestinfront": return ParticleSystemSortMode.OldestInFront;
                case "youngestinfront": return ParticleSystemSortMode.YoungestInFront;
                default: return ParticleSystemSortMode.None;
            }
        }

        private static ParticleSystem.MinMaxGradient BuildGradient(GradientStop[] stops)
        {
            var gradient = new Gradient();
            var colorKeys = new GradientColorKey[stops.Length];
            var alphaKeys = new GradientAlphaKey[stops.Length];

            for (int i = 0; i < stops.Length; i++)
            {
                var c = ToColor(stops[i].color);
                colorKeys[i] = new GradientColorKey(c, stops[i].time);
                alphaKeys[i] = new GradientAlphaKey(c.a, stops[i].time);
            }

            gradient.SetKeys(colorKeys, alphaKeys);
            return new ParticleSystem.MinMaxGradient(gradient);
        }

        private static ParticleSystem.MinMaxCurve BuildCurve(CurvePoint[] points, float scale = 1f)
        {
            var keyframes = new Keyframe[points.Length];
            for (int i = 0; i < points.Length; i++)
            {
                keyframes[i] = new Keyframe(points[i].time, points[i].value * scale);
            }
            return new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(keyframes));
        }
    }
}
