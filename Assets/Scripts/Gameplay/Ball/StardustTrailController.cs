// ─────────────────────────────────────────────────────────────────────────────
//  StardustTrailController.cs
//  Assembly : EchoLine.Gameplay
//  Location : Assets/Scripts/Gameplay/Ball/StardustTrailController.cs
//
//  Responsibilities (SRP):
//    1. Dynamically adjust the stardust trail particle emission rate based on
//       the ball's current velocity magnitude.
//    2. Emit particles only when the ball is in motion (above a small
//       velocity threshold), producing a sparse and elegant trailing effect.
//
//  This script is placed on a child GameObject of the Ball prefab that holds
//  a ParticleSystem configured for world-space simulation. It does NOT modify
//  any physics values — it is purely visual.
//
//  External callers:
//    • None — this component is self-contained and reads velocity passively.
// ─────────────────────────────────────────────────────────────────────────────

using UnityEngine;

namespace EchoLine.Gameplay
{
    /// <summary>
    /// Controls the stardust trail particle emission rate based on the parent
    /// ball's velocity. Produces a sparse, elegant trail of white sparkle
    /// particles that lingers behind the ball as it moves.
    /// </summary>
    public sealed class StardustTrailController : MonoBehaviour
    {
        // ─────────────────────────────────────────────────────────────────────
        //  Configuration
        // ─────────────────────────────────────────────────────────────────────

        [Header("Velocity Thresholds")]

        [Tooltip(
            "Minimum speed the ball must have before any particles are emitted. " +
            "Below this threshold the trail is completely silent.")]
        [SerializeField] private float _velocityThreshold = 0.5f;

        [Tooltip(
            "Speed at which particle emission reaches its maximum rate. " +
            "Speeds above this value are clamped.")]
        [SerializeField] private float _maxVelocity = 12f;

        [Header("Emission")]

        [Tooltip("Maximum particles emitted per second at full speed.")]
        [SerializeField] private float _maxEmissionRate = 25f;

        [Tooltip(
            "Minimum particles emitted per second once the velocity threshold " +
            "is exceeded. Provides a gentle baseline even at low speeds.")]
        [SerializeField] private float _minEmissionRate = 5f;

        // ─────────────────────────────────────────────────────────────────────
        //  Cached references
        // ─────────────────────────────────────────────────────────────────────

        private Rigidbody2D      _rb;
        private ParticleSystem   _ps;
        private ParticleSystem.EmissionModule _emission;

        // ─────────────────────────────────────────────────────────────────────
        //  Unity lifecycle
        // ─────────────────────────────────────────────────────────────────────

        private void Awake()
        {
            _rb = GetComponentInParent<Rigidbody2D>();
            _ps = GetComponent<ParticleSystem>();

            if (_rb == null)
                Debug.LogError("[StardustTrailController] No Rigidbody2D found on parent.", this);

            if (_ps == null)
                Debug.LogError("[StardustTrailController] No ParticleSystem found on this GameObject.", this);

            _emission = _ps.emission;

            // Start with emission disabled — trail activates once ball moves.
            _emission.rateOverTime = 0f;
        }

        private void Update()
        {
            if (_rb == null || _ps == null) return;

            float speed = _rb.linearVelocity.magnitude;

            if (speed < _velocityThreshold)
            {
                // Below threshold: no emission.
                _emission.rateOverTime = 0f;
                return;
            }

            // Normalise speed into the [0, 1] range between threshold and max.
            float t = Mathf.InverseLerp(_velocityThreshold, _maxVelocity, speed);

            // Lerp emission rate for a smooth, elegant ramp-up.
            float rate = Mathf.Lerp(_minEmissionRate, _maxEmissionRate, t);
            _emission.rateOverTime = rate;
        }
    }
}
