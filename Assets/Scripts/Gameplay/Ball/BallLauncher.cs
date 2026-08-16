// ─────────────────────────────────────────────────────────────────────────────
//  BallLauncher.cs
//  Assembly : EchoLine.Gameplay
//  Location : Assets/Scripts/Gameplay/BallLauncher.cs
//
//  Responsibilities (SRP):
//    1. Apply all physics configuration values from GameConfig to the
//       Rigidbody2D and PhysicsMaterial2D once on Awake and again on every reset.
//    2. Hold the ball frozen at its start position until Launch() is called.
//    3. Execute the launch (unfreeze constraints, let gravity take over).
//    4. Reset the ball to its start position and frozen state when requested.
//
//  This is the ONLY script that writes to the Rigidbody2D or PhysicsMaterial2D.
//  No other ball script should touch physics values directly.
//
//  External callers:
//    • LevelManager   — calls Launch() when the player taps the Drop button.
//    • LevelManager   — calls ResetBall() after a death or manual reset.
//    • InputReader    — OnEraseLast is handled upstream; this script does NOT
//                       subscribe to input directly.
// ─────────────────────────────────────────────────────────────────────────────

using UnityEngine;
using EchoLine.Core;                     // GameConfig

namespace EchoLine.Gameplay
{
    /// <summary>
    /// Manages the ball's lifecycle: physics initialisation, launch, and reset.
    /// Reads all tunable physics values exclusively from <see cref="GameConfig"/>.
    /// </summary>
    public sealed class BallLauncher : MonoBehaviour
    {
        // ─────────────────────────────────────────────────────────────────────
        //  Inspector fields
        // ─────────────────────────────────────────────────────────────────────

        [Header("Physics References")]

        [Tooltip("The Rigidbody2D on the Ball GameObject. Assign in Inspector.")]
        [SerializeField] private Rigidbody2D _rb;

        [Tooltip(
            "The PhysicsMaterial2D assigned to the Ball's CircleCollider2D. " +
            "Must be a dedicated asset used ONLY by the ball — not shared with " +
            "walls or hazards — so runtime modifications stay isolated.")]
        [SerializeField] private PhysicsMaterial2D _ballPhysicsMaterial;

        [Header("Configuration")]

        [Tooltip("ScriptableObject containing all physics tuning values.")]
        [SerializeField] private GameConfig _config;

        [Header("Launch State")]

        [Tooltip(
            "World-space position the ball returns to on reset. " +
            "Populated automatically from the GameObject's position at Awake. " +
            "Override in Inspector only if the launch position differs from the " +
            "GameObject's initial placement.")]
        [SerializeField] private Vector3 _startPosition;

        // ─────────────────────────────────────────────────────────────────────
        //  Properties
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// True from the moment Launch() is called until ResetBall() completes.
        /// BallPhysicsResponder reads this to guard against registering contacts
        /// before the ball is actually in play.
        /// </summary>
        public bool IsLaunched { get; private set; }

        /// <summary>
        /// The world-space position the ball launches from and resets to.
        /// Read by BallSonarEmitter to set the initial sonar origin on reset.
        /// </summary>
        public Vector3 StartPosition => _startPosition;

        // ─────────────────────────────────────────────────────────────────────
        //  Unity lifecycle
        // ─────────────────────────────────────────────────────────────────────

        private void Awake()
        {
            ValidateReferences();

            // Record start position from the GameObject's initial world placement.
            // This means the level designer positions the Ball prefab in the scene
            // and this script captures it automatically — no manual data entry needed.
            if (_startPosition == Vector3.zero)
                _startPosition = transform.position;

            ApplyPhysicsConfig();
            FreezeRigidbody();
        }

#if UNITY_EDITOR
        // Visualise the start position and launch-ready state in the Scene view.
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = IsLaunched ? Color.green : Color.cyan;
            Gizmos.DrawWireSphere(_startPosition, 0.15f);
            UnityEditor.Handles.Label(
                _startPosition + Vector3.up * 0.25f,
                IsLaunched ? "LAUNCHED" : "READY");
        }
#endif

        // ─────────────────────────────────────────────────────────────────────
        //  Public API
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Releases the ball into the physics simulation.
        /// Called by LevelManager when the player taps the Drop button.
        /// Safe to call only once per round — guarded by IsLaunched.
        /// </summary>
        public void Launch()
        {
            if (IsLaunched)
            {
                Debug.LogWarning("[BallLauncher] Launch() called while already launched. Ignored.");
                return;
            }

            IsLaunched = true;
            UnfreezeRigidbody();

            Debug.Log("[BallLauncher] Ball launched.");
        }

        /// <summary>
        /// Returns the ball to its start position and re-freezes it, ready for
        /// the next attempt. Called by LevelManager after a death or manual reset.
        /// Re-applies GameConfig values so hot-reloaded SO changes take effect
        /// immediately without requiring a scene reload.
        /// </summary>
        public void ResetBall()
        {
            IsLaunched = false;

            // Stop all motion before repositioning to avoid one-frame collisions
            // at the reset position caused by residual velocity.
            _rb.linearVelocity  = Vector2.zero;
            _rb.angularVelocity = 0f;

            transform.position = _startPosition;
            transform.rotation = Quaternion.identity;

            // Re-apply config: catches any values tweaked in the SO during play mode.
            ApplyPhysicsConfig();
            FreezeRigidbody();

            Debug.Log("[BallLauncher] Ball reset to start position.");
        }

        // ─────────────────────────────────────────────────────────────────────
        //  Physics configuration
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Writes all GameConfig physics values to the Rigidbody2D and
        /// PhysicsMaterial2D. This is the single authoritative point of physics
        /// initialisation for the ball — no other script touches these values.
        /// </summary>
        private void ApplyPhysicsConfig()
        {
            // ── Rigidbody2D ──────────────────────────────────────────────────
            _rb.gravityScale      = _config.GravityScale;      // 1.2
            _rb.linearDamping     = _config.BallDrag;     // 0.3
            _rb.angularDamping    = _config.AngularDrag;    // 0.5

            // Continuous collision detection — prevents tunnelling through thin
            // player-drawn lines at high velocity.
            _rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            // ── Global gravity ───────────────────────────────────────────────
            // Physics2D.gravity is scene-global. Setting it here is appropriate
            // because Echo Line has one physics context and one gravity value.
            // If future levels need variable gravity, move this to LevelManager.
            Physics2D.gravity = _config.Gravity;               // (0, -18)

            // ── PhysicsMaterial2D ────────────────────────────────────────────
            // This material must be assigned exclusively to the ball's collider.
            // Writing to a shared material would affect walls and hazards too.
            _ballPhysicsMaterial.bounciness = _config.Bounciness;   // 0.35
            _ballPhysicsMaterial.friction   = _config.Friction;     // 0.2
        }

        // ─────────────────────────────────────────────────────────────────────
        //  Rigidbody constraint helpers
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Locks all Rigidbody2D axes so the ball is completely stationary
        /// while the player is drawing lines.
        /// </summary>
        private void FreezeRigidbody()
        {
            _rb.constraints = RigidbodyConstraints2D.FreezeAll;
        }

        /// <summary>
        /// Removes all constraints so gravity and collision forces act freely.
        /// Z-rotation is re-locked because rolling rotation on a 2D ball adds
        /// no gameplay value and makes trajectory harder to read visually.
        /// </summary>
        private void UnfreezeRigidbody()
        {
            _rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        }

        // ─────────────────────────────────────────────────────────────────────
        //  Validation
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Fails fast with a clear message if any required reference is missing.
        /// Catches wiring mistakes in the Inspector before they become null-ref
        /// exceptions at runtime.
        /// </summary>
        private void ValidateReferences()
        {
            if (_rb == null)
                Debug.LogError("[BallLauncher] Rigidbody2D is not assigned.", this);

            if (_ballPhysicsMaterial == null)
                Debug.LogError("[BallLauncher] PhysicsMaterial2D is not assigned.", this);

            if (_config == null)
                Debug.LogError("[BallLauncher] GameConfig ScriptableObject is not assigned.", this);
        }
    }
}
