// ─────────────────────────────────────────────────────────────────────────────
//  AddStardustTrailToBall.cs
//  Assembly : EchoLine.Editor
//  Location : Assets/Editor/AddStardustTrailToBall.cs
//
//  One-shot editor utility that programmatically creates the StardustTrail
//  child particle system inside the Ball prefab. Run once from the Unity menu:
//
//      EchoLine → Add Stardust Trail to Ball Prefab
//
//  After running, the prefab will contain a new child GameObject named
//  "StardustTrail" with a fully configured ParticleSystem and the
//  StardustTrailController runtime script attached.
// ─────────────────────────────────────────────────────────────────────────────

using UnityEngine;
using UnityEditor;
using EchoLine.Gameplay;

namespace EchoLine.Editor
{
    public static class AddStardustTrailToBall
    {
        private const string BallPrefabPath = "Assets/Prefabs/Gameplay/Ball.prefab";
        private const string ParticleMatPath = "Assets/Materials/ParticleAdditive.mat";

        [MenuItem("EchoLine/Add Stardust Trail to Ball Prefab")]
        public static void Execute()
        {
            // ── Load the prefab for editing ──────────────────────────────────
            string assetPath = BallPrefabPath;
            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(assetPath);

            if (prefabRoot == null)
            {
                Debug.LogError($"[AddStardustTrail] Could not load prefab at '{assetPath}'.");
                return;
            }

            // ── Check if StardustTrail already exists ────────────────────────
            Transform existing = prefabRoot.transform.Find("StardustTrail");
            if (existing != null)
            {
                Debug.LogWarning("[AddStardustTrail] StardustTrail child already exists. Aborting to avoid duplicates.");
                PrefabUtility.UnloadPrefabContents(prefabRoot);
                return;
            }

            // ── Create child GameObject ──────────────────────────────────────
            GameObject trailGO = new GameObject("StardustTrail");
            trailGO.transform.SetParent(prefabRoot.transform, false);
            trailGO.transform.localPosition = Vector3.zero;
            trailGO.layer = prefabRoot.layer; // Match ball's layer (6)

            // ── Add and configure ParticleSystem ─────────────────────────────
            ParticleSystem ps = trailGO.AddComponent<ParticleSystem>();

            // Stop playback while configuring
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            // ── Main Module ──────────────────────────────────────────────────
            var main = ps.main;
            main.duration             = 5f;
            main.loop                 = true;
            main.prewarm              = false;
            main.playOnAwake          = true;
            main.startLifetime        = new ParticleSystem.MinMaxCurve(0.6f, 1.2f);
            main.startSpeed           = new ParticleSystem.MinMaxCurve(0.1f, 0.3f);
            main.startSize            = new ParticleSystem.MinMaxCurve(0.02f, 0.06f);
            main.startColor           = new Color(1f, 1f, 1f, 0.9f); // White
            main.maxParticles         = 100;
            main.simulationSpace      = ParticleSystemSimulationSpace.World;
            main.gravityModifier      = -0.03f; // Slight upward float for magical feel
            main.emitterVelocityMode  = ParticleSystemEmitterVelocityMode.Rigidbody;
            main.scalingMode          = ParticleSystemScalingMode.Hierarchy;

            // ── Emission Module ──────────────────────────────────────────────
            // Base rate set to 0 — StardustTrailController drives it dynamically.
            var emission = ps.emission;
            emission.enabled       = true;
            emission.rateOverTime  = 0f;
            emission.rateOverDistance = 0f;

            // ── Shape Module ─────────────────────────────────────────────────
            // Sphere shape centred on the ball for natural distribution.
            var shape = ps.shape;
            shape.enabled        = true;
            shape.shapeType      = ParticleSystemShapeType.Sphere;
            shape.radius         = 0.12f;
            shape.radiusThickness = 1f; // Emit from entire volume

            // ── Size over Lifetime ───────────────────────────────────────────
            // Particles shrink to nothing — elegant fade-out.
            var sizeOverLifetime = ps.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            AnimationCurve sizeCurve = new AnimationCurve();
            sizeCurve.AddKey(new Keyframe(0f, 1f, 0f, -0.5f));
            sizeCurve.AddKey(new Keyframe(1f, 0f, -0.5f, 0f));
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

            // ── Color over Lifetime ──────────────────────────────────────────
            // White, fading from full alpha to transparent.
            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            Gradient alphaGradient = new Gradient();
            alphaGradient.SetKeys(
                new GradientColorKey[]
                {
                    new GradientColorKey(Color.white, 0f),
                    new GradientColorKey(Color.white, 1f)
                },
                new GradientAlphaKey[]
                {
                    new GradientAlphaKey(0.85f, 0f),
                    new GradientAlphaKey(0.5f,  0.5f),
                    new GradientAlphaKey(0f,    1f)
                }
            );
            colorOverLifetime.color = new ParticleSystem.MinMaxGradient(alphaGradient);

            // ── Noise Module ─────────────────────────────────────────────────
            // Gentle organic shimmer movement for the sparkle particles.
            var noise = ps.noise;
            noise.enabled       = true;
            noise.strength      = 0.15f;
            noise.frequency     = 1.2f;
            noise.scrollSpeed   = 0.3f;
            noise.damping       = true;
            noise.quality       = ParticleSystemNoiseQuality.Medium;
            noise.octaveCount   = 2;

            // ── Velocity over Lifetime ───────────────────────────────────────
            // Disabled — we rely on noise and start speed for movement.
            var velocity = ps.velocityOverLifetime;
            velocity.enabled = false;

            // ── Rotation over Lifetime ───────────────────────────────────────
            // Disabled — billboard particles don't benefit from rotation.
            var rotation = ps.rotationOverLifetime;
            rotation.enabled = false;

            // ── Disable unused modules ───────────────────────────────────────
            var forceModule    = ps.forceOverLifetime;    forceModule.enabled    = false;
            var externalForces = ps.externalForces;       externalForces.enabled = false;
            var subEmitters    = ps.subEmitters;           subEmitters.enabled    = false;
            var textureSheet   = ps.textureSheetAnimation; textureSheet.enabled   = false;
            var trails         = ps.trails;                trails.enabled         = false;
            var collision      = ps.collision;             collision.enabled      = false;
            var trigger        = ps.trigger;               trigger.enabled        = false;
            var lights         = ps.lights;                lights.enabled         = false;
            var customData     = ps.customData;            customData.enabled     = false;
            var inherit        = ps.inheritVelocity;       inherit.enabled        = false;
            var lifetime       = ps.lifetimeByEmitterSpeed; lifetime.enabled      = false;

            // ── Renderer ─────────────────────────────────────────────────────
            var renderer = trailGO.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode   = ParticleSystemRenderMode.Billboard;
            renderer.sortingOrder = 0; // Behind ball (ball sprite is at order 0)
            renderer.minParticleSize = 0f;
            renderer.maxParticleSize = 0.5f;
            renderer.alignment = ParticleSystemRenderSpace.View;

            // Load and assign the existing ParticleAdditive material.
            Material particleMat = AssetDatabase.LoadAssetAtPath<Material>(ParticleMatPath);
            if (particleMat != null)
            {
                renderer.material = particleMat;
            }
            else
            {
                Debug.LogWarning(
                    $"[AddStardustTrail] Could not load material at '{ParticleMatPath}'. " +
                    "Assign the material manually in the Inspector.");
            }

            // ── Add runtime controller ───────────────────────────────────────
            trailGO.AddComponent<StardustTrailController>();

            // ── Save the prefab ──────────────────────────────────────────────
            PrefabUtility.SaveAsPrefabAsset(prefabRoot, assetPath);
            PrefabUtility.UnloadPrefabContents(prefabRoot);

            Debug.Log(
                "[AddStardustTrail] ✓ StardustTrail particle system added to Ball prefab. " +
                "Enter Play mode to verify the effect.");
        }
    }
}
