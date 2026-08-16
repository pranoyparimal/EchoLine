using UnityEngine;
using EchoLine.Core;

// This file lives in Assets/Editor/ under the EchoLine.Editor asmdef.
// It is stripped from all builds automatically — zero runtime cost.
namespace EchoLine.Editor
{
    /// <summary>
    /// Attach to any GameObject in your test scene to verify that
    /// InputReader is firing correctly before LineDrawer is wired up.
    ///
    /// What it tests:
    ///   ✓ Single-finger touch start  → logs world position
    ///   ✓ Single-finger drag         → logs world position each move event
    ///   ✓ Single-finger lift         → logs draw end
    ///   ✓ Two-finger tap             → logs erase-last trigger
    ///
    /// Setup:
    ///   1. Add this component to the Managers GameObject.
    ///   2. Assign the same InputReader asset used by InputReaderEnabler.
    ///   3. Enter Play mode — open Console and interact with the Game view
    ///      (use the Input Debugger or a real device / Unity Remote).
    ///   4. Remove this component before shipping.
    ///
    /// Assembly: EchoLine.Editor (Editor-only, never included in builds)
    /// </summary>
    public sealed class TouchInputTester : MonoBehaviour
    {
        [Tooltip("The same InputReader asset assigned to InputReaderEnabler.")]
        [SerializeField] private InputReader _inputReader;

        // Running count of move events so the Console doesn't flood
        private int _moveEventCount;

        private void OnEnable()
        {
            if (_inputReader == null)
            {
                Debug.LogError("[TouchInputTester] InputReader not assigned.", this);
                return;
            }

            _inputReader.OnDrawStart += OnDrawStart;
            _inputReader.OnDrawMove  += OnDrawMove;
            _inputReader.OnDrawEnd   += OnDrawEnd;
            _inputReader.OnEraseLast += OnEraseLast;

            Debug.Log("[TouchInputTester] Subscribed to InputReader. Ready for input.");
        }

        private void OnDisable()
        {
            if (_inputReader == null) return;

            _inputReader.OnDrawStart -= OnDrawStart;
            _inputReader.OnDrawMove  -= OnDrawMove;
            _inputReader.OnDrawEnd   -= OnDrawEnd;
            _inputReader.OnEraseLast -= OnEraseLast;
        }

        // ─── Handlers ────────────────────────────────────────────────

        private void OnDrawStart(Vector2 worldPos)
        {
            _moveEventCount = 0;
            Debug.Log($"[TouchInputTester] ✦ DRAW START  — world: {worldPos:F3}");
        }

        private void OnDrawMove(Vector2 worldPos)
        {
            _moveEventCount++;
            // Log every 10th move event to avoid Console spam on fast drags
            if (_moveEventCount % 10 == 0)
                Debug.Log($"[TouchInputTester]   DRAW MOVE   — world: {worldPos:F3}  " +
                          $"(event #{_moveEventCount})");
        }

        private void OnDrawEnd()
        {
            Debug.Log($"[TouchInputTester] ✦ DRAW END    — {_moveEventCount} move events recorded.");
        }

        private void OnEraseLast()
        {
            Debug.Log("[TouchInputTester] ✦ ERASE LAST  — two-finger tap detected.");
        }
    }
}
