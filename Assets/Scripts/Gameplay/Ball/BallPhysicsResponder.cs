using UnityEngine;
using EchoLine.Core;

namespace EchoLine.Gameplay
{
    /// <summary>
    /// Detects and routes all physics-layer collision events for the ball.
    ///
    /// RESPONSIBILITIES
    /// ─────────────────
    /// • Apply GameConfig values to Rigidbody2D / PhysicsMaterial2D at startup.
    /// • Enforce a maximum ball speed each FixedUpdate to prevent tunnelling
    ///   caused by energy accumulation across multiple bounces.
    /// • On collision, delegate to IBallContactHandler (hazards, basket) or
    ///   raise the appropriate GameEvent directly (bounce off Wall/PlayerLine).
    /// • Gate all collision responses behind _isLive — events before launch
    ///   are silently ignored.
    ///
    /// DOES NOT
    /// ─────────
    /// • Touch Rigidbody2D after Awake (write authority belongs to BallLauncher
    ///   while the ball is live — see BallLauncher.cs).
    /// • Know anything about VFX, trails, sonar pulses, or UI.
    /// • Hold direct references to sibling ball scripts.
    ///
    /// SETUP
    /// ─────
    /// Attach to the Ball prefab root alongside BallLauncher, BallTrailRenderer,
    /// BallSonarEmitter, and BallDeathHandler.
    /// Wire all [SerializeField] references in the Inspector.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(CircleCollider2D))]
    public sealed class BallPhysicsResponder : MonoBehaviour
    {
        // ─────────────────────────────────────────────────────────────
        //  Inspector wiring
        // ─────────────────────────────────────────────────────────────

        [Header("Config")]
        [SerializeField] private GameConfig _config;

        [Space(10)]
        [Header("Events — Outbound")]

        [Tooltip("Raised when the ball makes contact with a Wall or PlayerLine. " +
                 "Payload: world-space contact point. " +
                 "Listeners: BallSonarEmitter, BallTrailRenderer.")]
        [SerializeField] private Vector2GameEventSO _onBallBounced;

        // NOTE: OnBallDied and OnBallEnteredBasket are NOT raised here.
        // They are raised by the hazard / basket implementors of IBallContactHandler
        // after OnBallContact() is called. This script only knows "something
        // with a handler was hit" — it does not know which event to raise.

        // ─────────────────────────────────────────────────────────────
        //  Layer cache  (set in Awake from Physics2D layer names)
        // ─────────────────────────────────────────────────────────────

        private int _wallLayer;
        private int _playerLineLayer;

        // ─────────────────────────────────────────────────────────────
        //  Component cache
        // ─────────────────────────────────────────────────────────────

        private Rigidbody2D   _rb;
        private CircleCollider2D _collider;
        private PhysicsMaterial2D _material;

        // ─────────────────────────────────────────────────────────────
        //  State
        // ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Set to true by BallLauncher.Launch() via SetLive().
        /// All collision responses are gated behind this flag.
        /// </summary>
        private bool _isLive;

        // ─────────────────────────────────────────────────────────────
        //  Unity lifecycle
        // ─────────────────────────────────────────────────────────────

        private void Awake()
        {
            // Cache components
            _rb       = GetComponent<Rigidbody2D>();
            _collider = GetComponent<CircleCollider2D>();

            ValidateConfig();
            ApplyConfigToPhysics();
            CacheLayerIndices();
        }

        private void FixedUpdate()
        {
            if (!_isLive) return;
            EnforceMaxSpeed();
        }

        // ─────────────────────────────────────────────────────────────
        //  Public API  —  called by BallLauncher
        // ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Called by BallLauncher immediately before applying the launch impulse.
        /// Enables collision response processing.
        /// </summary>
        public void SetLive(bool live) => _isLive = live;

        // ─────────────────────────────────────────────────────────────
        //  Collision routing
        // ─────────────────────────────────────────────────────────────

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (!_isLive) return;

            int hitLayer = collision.gameObject.layer;

            // ── Path A: object implements IBallContactHandler
            //    (Hazard types, GoalBasket — they own their GameEvent raises)
            var handler = collision.gameObject.GetComponent<IBallContactHandler>();
            if (handler != null)
            {
                handler.OnBallContact(collision);
                return; // handler takes full responsibility from here
            }

            // ── Path B: Wall or PlayerLine bounce
            //    Raise OnBallBounced with the contact point so
            //    BallSonarEmitter and BallTrailRenderer can respond.
            if (hitLayer == _wallLayer || hitLayer == _playerLineLayer)
            {
                Vector2 contactPoint = collision.GetContact(0).point;
                _onBallBounced?.Raise(contactPoint);
            }

            // Any other layer (e.g. Default objects without a handler) is
            // intentionally silenced — no fallback needed.
        }

        // ─────────────────────────────────────────────────────────────
        //  Speed cap
        // ─────────────────────────────────────────────────────────────

        private void EnforceMaxSpeed()
        {
            // [ADD TO GameConfig] maxBallSpeed field (suggested default: 18f)
            // Guards against energy accumulation across rapid successive bounces
            // which can cause the ball to tunnel through thin geometry.
            float max = _config.maxBallSpeed;
            if (_rb.linearVelocity.sqrMagnitude > max * max)
                _rb.linearVelocity = _rb.linearVelocity.normalized * max;
        }

        // ─────────────────────────────────────────────────────────────
        //  Startup helpers
        // ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Writes GameConfig values onto the Rigidbody2D and its PhysicsMaterial2D.
        /// BallLauncher owns the Rigidbody2D after launch — this is the only
        /// place BallPhysicsResponder touches it.
        /// </summary>
        private void ApplyConfigToPhysics()
        {
            _rb.gravityScale    = _config.GravityScale;
            _rb.linearDamping   = _config.BallDrag;
            _rb.angularDamping  = _config.AngularDrag;

            // PhysicsMaterial2D drives bounciness. Create one at runtime if the
            // collider doesn't already have a shared material assigned.
            if (_collider.sharedMaterial == null)
            {
                _material = new PhysicsMaterial2D("Ball_Runtime")
                {
                    bounciness = _config.Bounciness,
                    friction   = 0f   // friction handled by wall/line materials
                };
                _collider.sharedMaterial = _material;
            }
            else
            {
                // Mutate the existing material so the asset stays the source of truth
                _material = _collider.sharedMaterial;
                _material.bounciness = _config.Bounciness;
            }

            _collider.radius = _config.BallRadius;
        }

        private void CacheLayerIndices()
        {
            // LayerMask.NameToLayer returns -1 if the layer doesn't exist.
            // The assertions below will surface missing layer setup during
            // development before any collision logic silently fails.
            _wallLayer       = LayerMask.NameToLayer("Wall");
            _playerLineLayer = LayerMask.NameToLayer("PlayerLine");

#if UNITY_EDITOR
            if (_wallLayer       == -1) Debug.LogError("[BallPhysicsResponder] Layer 'Wall' not found. Add it in Project Settings > Tags and Layers.", this);
            if (_playerLineLayer == -1) Debug.LogError("[BallPhysicsResponder] Layer 'PlayerLine' not found. Add it in Project Settings > Tags and Layers.", this);
#endif
        }

        private void ValidateConfig()
        {
#if UNITY_EDITOR
            if (_config == null)
            {
                Debug.LogError("[BallPhysicsResponder] GameConfig is not assigned. " +
                               "Wire the asset in the Inspector.", this);
                return;
            }

            if (!_config.IsValid())
                Debug.LogWarning("[BallPhysicsResponder] GameConfig contains out-of-range values. " +
                                 "Check the asset in the Inspector.", this);
#endif
        }

        // ─────────────────────────────────────────────────────────────
        //  Cleanup
        // ─────────────────────────────────────────────────────────────

        private void OnDestroy()
        {
            // Destroy the runtime material if we created it — avoids a
            // memory leak in long play sessions or frequent level reloads.
            if (_material != null && _material.name == "Ball_Runtime")
                Destroy(_material);
        }
    }
}
