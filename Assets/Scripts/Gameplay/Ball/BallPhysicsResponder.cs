// ─────────────────────────────────────────────────────────────────────────────
//  BallPhysicsResponder.cs
//  Assembly : EchoLine.Gameplay
//  Location : Assets/Scripts/Gameplay/BallPhysicsResponder.cs
//
//  Responsibilities (SRP):
//    1. Enforce a maximum ball speed each FixedUpdate to prevent energy
//       accumulation across successive bounces causing tunnelling.
//    2. On collision, delegate to IBallContactHandler (hazards, basket) or
//       raise the appropriate GameEvent directly (bounce off Wall/PlayerLine).
//    3. Gate all collision responses behind _isLive — set exclusively by
//       BallLauncher via SetLive() to ensure events only fire while in play.
//
//  DOES NOT
//  ─────────
//  • Write to Rigidbody2D, CircleCollider2D, or PhysicsMaterial2D — that
//    authority belongs entirely to BallLauncher (see BallLauncher.cs).
//  • Know anything about VFX, trails, sonar pulses, or UI.
//  • Hold direct references to sibling ball scripts.
//
//  SETUP
//  ─────
//  Attach to the Ball prefab root alongside BallLauncher, BallTrailRenderer,
//  BallSonarEmitter, and BallDeathHandler.
//  Wire all [SerializeField] references in the Inspector.
//  Assign this component to BallLauncher._physicsResponder in the Inspector.
// ─────────────────────────────────────────────────────────────────────────────

using UnityEngine;
using EchoLine.Core;

namespace EchoLine.Gameplay
{
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(CircleCollider2D))]
    public sealed class BallPhysicsResponder : MonoBehaviour
    {
        // ─────────────────────────────────────────────────────────────
        //  Inspector wiring
        // ─────────────────────────────────────────────────────────────

        [Header("Config")]
        [Tooltip("ScriptableObject containing all physics tuning values. " +
                 "Used here exclusively for maxBallSpeed.")]
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

        // Cached only for EnforceMaxSpeed — BallLauncher owns all write
        // authority over this Rigidbody2D; this script reads velocity only.
        [SerializeField] private Rigidbody2D _rb;

        // ─────────────────────────────────────────────────────────────
        //  State
        // ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Driven by BallLauncher via SetLive().
        /// All collision responses and speed capping are gated behind this flag.
        /// </summary>
        private bool _isLive;

        // ─────────────────────────────────────────────────────────────
        //  Unity lifecycle
        // ─────────────────────────────────────────────────────────────

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();

            ValidateConfig();
            CacheLayerIndices();
        }

        private void FixedUpdate()
        {
            if (!_isLive) return;
            //EnforceMaxSpeed();
        }

        // ─────────────────────────────────────────────────────────────
        //  Public API  —  called by BallLauncher
        // ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Called by BallLauncher immediately before unfreezing the Rigidbody2D
        /// on launch, and immediately before clearing velocity on reset.
        /// Enables or disables collision response and speed capping.
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
            // Guards against energy accumulation across rapid successive bounces
            // which can cause the ball to tunnel through thin geometry.
            float max = _config.MaxBallSpeed;
            if (_rb.linearVelocity.sqrMagnitude > max * max)
                _rb.linearVelocity = _rb.linearVelocity.normalized * max;
        }

        // ─────────────────────────────────────────────────────────────
        //  Startup helpers
        // ─────────────────────────────────────────────────────────────

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
    }
}
