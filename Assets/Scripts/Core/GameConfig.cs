using UnityEngine;

namespace EchoLine.Core
{
    /// <summary>
    /// Single source of truth for all physics and sonar timing constants.
    /// Create via: Assets > Create > EchoLine > Game Config
    /// Place the resulting asset at: Assets/Scripts/Core/GameConfig.asset
    ///
    /// Reference this asset from any MonoBehaviour that needs physics or sonar
    /// values — never hard-code these numbers in scripts.
    /// </summary>
    [CreateAssetMenu(
        fileName = "GameConfig",
        menuName = "EchoLine/Game Config",
        order = 0)]
    public sealed class GameConfig : ScriptableObject
    {
        // ─────────────────────────────────────────────────────────────
        //  Physics — Ball
        // ─────────────────────────────────────────────────────────────

        [Header("Physics — Ball")]

        // ── ADDED ─────────────────────────────────────────────────────
        [Space(5)]
        [Tooltip("World-space gravity vector applied via Physics2D.gravity. " +
                 "GDD target: (0, -18) for a dense marble feel.")]
        [SerializeField] private Vector2 gravity = new Vector2(0f, -18f);
        // ──────────────────────────────────────────────────────────────

        [Space(5)]
        [Tooltip("Applied to the ball's Rigidbody2D.gravityScale. " +
                 "Works with Physics2D.gravity (0, -18) set in Project Settings.")]
        [Range(0.5f, 3f)]
        [SerializeField] private float gravityScale = 1.2f;

        [Space(5)]
        [Tooltip("Bounciness (restitution) of the ball's PhysicsMaterial2D. " +
                 "0 = no bounce, 1 = perfectly elastic.")]
        [Range(0f, 1f)]
        [SerializeField] private float ballBounciness = 0.35f;

        // ── ADDED ─────────────────────────────────────────────────────
        [Space(5)]
        [Tooltip("Friction of the ball's PhysicsMaterial2D. " +
                 "Lower values = icier feel. Tuned alongside bounciness. " +
                 "GDD target: 0.2")]
        [Range(0f, 1f)]
        [SerializeField] private float ballFriction = 0.2f;
        // ──────────────────────────────────────────────────────────────

        [Space(5)]
        [Tooltip("Linear damping applied to the ball's Rigidbody2D. " +
                 "Higher values slow horizontal drift after bounces.")]
        [Range(0f, 2f)]
        [SerializeField] private float ballDrag = 0.3f;

        [Space(5)]
        [Tooltip("Angular damping applied to the ball's Rigidbody2D. " +
                 "Prevents endless spin after glancing collisions.")]
        [Range(0f, 2f)]
        [SerializeField] private float ballAngularDrag = 0.5f;

        [Space(5)]
        [Tooltip("World-space radius of the ball's CircleCollider2D. " +
                 "Must match the sprite's visual radius.")]
        [Range(0.1f, 1f)]
        [SerializeField] private float ballRadius = 0.25f;

        // ─────────────────────────────────────────────────────────────
        //  Sonar — Timing
        // ─────────────────────────────────────────────────────────────

        [Space(10)]
        [Header("Sonar — Timing")]

        [Space(5)]
        [Tooltip("How long a hazard stays visible after the sonar ring passes through it. " +
                 "GDD target: 0.5 s.")]
        [Range(0.1f, 2f)]
        [SerializeField] private float sonarFlashDuration = 0.5f;

        [Space(5)]
        [Tooltip("Time for the sonar ring to expand from the launcher to the screen edge. " +
                 "GDD target: 2 s pulse interval — set expand time to match.")]
        [Range(0.5f, 5f)]
        [SerializeField] private float sonarExpandTime = 2f;

        // ─────────────────────────────────────────────────────────────
        //  Properties — consumed by BallLauncher.cs
        // ─────────────────────────────────────────────────────────────

        /// <summary>World-space gravity vector. Applied via Physics2D.gravity in BallLauncher.</summary>
        public Vector2 Gravity => gravity;

        /// <summary>Rigidbody2D.gravityScale for the ball.</summary>
        public float GravityScale => gravityScale;

        /// <summary>PhysicsMaterial2D.bounciness for the ball.</summary>
        public float Bounciness => ballBounciness;

        /// <summary>PhysicsMaterial2D.friction for the ball.</summary>
        public float Friction => ballFriction;

        /// <summary>Rigidbody2D.linearDamping for the ball.</summary>
        public float BallDrag => ballDrag;

        /// <summary>Rigidbody2D.angularDamping for the ball.</summary>
        public float AngularDrag => ballAngularDrag;

        // ─────────────────────────────────────────────────────────────
        //  Validation
        // ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Returns true when all values are within sensible ranges.
        /// Call from unit tests or editor tooling before entering Play mode.
        /// </summary>
        public bool IsValid()
        {
            return gravity.y < 0f                          // gravity must pull downward
                && gravityScale > 0f
                && ballBounciness >= 0f && ballBounciness <= 1f
                && ballFriction >= 0f && ballFriction <= 1f // ── ADDED
                && ballDrag >= 0f
                && ballAngularDrag >= 0f
                && ballRadius > 0f
                && sonarFlashDuration > 0f
                && sonarExpandTime > 0f;
        }

        // ─────────────────────────────────────────────────────────────
        //  Editor defaults
        // ─────────────────────────────────────────────────────────────

        // Reset() is called by Unity when the asset is first created via
        // the right-click menu, populating it with the tuned target values
        // rather than zero/default. No action needed from you.
        private void Reset()
        {
            gravity = new Vector2(0f, -18f); // ── ADDED
            gravityScale = 1.2f;
            ballBounciness = 0.35f;
            ballFriction = 0.2f;                  // ── ADDED
            ballDrag = 0.3f;
            ballAngularDrag = 0.5f;
            ballRadius = 0.25f;
            sonarFlashDuration = 0.5f;
            sonarExpandTime = 2f;
        }
    }
}
