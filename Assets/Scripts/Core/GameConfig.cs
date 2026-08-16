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
        menuName  = "EchoLine/Game Config",
        order     = 0)]
    public sealed class GameConfig : ScriptableObject
    {
        // ─────────────────────────────────────────────────────────────
        //  Physics — Ball
        // ─────────────────────────────────────────────────────────────

        [Header("Physics — Ball")]

        [Space(5)]
        [Tooltip("Applied to the ball's Rigidbody2D.gravityScale. " +
                 "Works with Physics2D.gravity (0, -18) set in Project Settings.")]
        [Range(0.5f, 3f)]
        public float gravityScale = 1.2f;

        [Space(5)]
        [Tooltip("Bounciness (restitution) of the ball's PhysicsMaterial2D. " +
                 "0 = no bounce, 1 = perfectly elastic.")]
        [Range(0f, 1f)]
        public float ballBounciness = 0.35f;

        [Space(5)]
        [Tooltip("Linear damping applied to the ball's Rigidbody2D. " +
                 "Higher values slow horizontal drift after bounces.")]
        [Range(0f, 2f)]
        public float ballDrag = 0.3f;

        [Space(5)]
        [Tooltip("Angular damping applied to the ball's Rigidbody2D. " +
                 "Prevents endless spin after glancing collisions.")]
        [Range(0f, 2f)]
        public float ballAngularDrag = 0.5f;

        [Space(5)]
        [Tooltip("World-space radius of the ball's CircleCollider2D. " +
                 "Must match the sprite's visual radius.")]
        [Range(0.1f, 1f)]
        public float ballRadius = 0.25f;

        // ─────────────────────────────────────────────────────────────
        //  Sonar — Timing
        // ─────────────────────────────────────────────────────────────

        [Space(10)]
        [Header("Sonar — Timing")]
        [Space(5)]
        [Tooltip("How long a hazard stays visible after the sonar ring passes through it. " +
                 "GDD target: 0.5 s.")]
        [Range(0.1f, 2f)]
        public float sonarFlashDuration = 0.5f;

        [Space(5)]
        [Tooltip("Time for the sonar ring to expand from the launcher to the screen edge. " +
                 "GDD target: 2 s pulse interval — set expand time to match.")]
        [Range(0.5f, 5f)]
        public float sonarExpandTime = 2f;

        // ─────────────────────────────────────────────────────────────
        //  Validation
        // ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Returns true when all values are within sensible ranges.
        /// Call from unit tests or editor tooling before entering Play mode.
        /// </summary>
        public bool IsValid()
        {
            return gravityScale    > 0f
                && ballBounciness  >= 0f && ballBounciness <= 1f
                && ballDrag        >= 0f
                && ballAngularDrag >= 0f
                && ballRadius      > 0f
                && sonarFlashDuration > 0f
                && sonarExpandTime    > 0f;
        }

        // ─────────────────────────────────────────────────────────────
        //  Editor defaults
        // ─────────────────────────────────────────────────────────────

        // Reset() is called by Unity when the asset is first created via
        // the right-click menu, populating it with the tuned target values
        // rather than zero/default. No action needed from you.
        private void Reset()
        {
            gravityScale      = 1.2f;
            ballBounciness    = 0.35f;
            ballDrag          = 0.3f;
            ballAngularDrag   = 0.5f;
            ballRadius        = 0.25f;
            sonarFlashDuration = 0.5f;
            sonarExpandTime   = 2f;
        }
    }
}
