using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

namespace EchoLine.Core
{
    /// <summary>
    /// Listens to the New Input System and re-broadcasts touch events as plain
    /// C# Actions. Scripts in Gameplay and UI subscribe to these — they never
    /// talk to the Input System directly.
    ///
    /// Placed in EchoLine.Core so both EchoLine.Gameplay and EchoLine.UI can
    /// subscribe without any circular assembly dependency.
    ///
    /// Asset location: Assets/Scripts/Core/InputReader.asset
    /// Create via: Assets > Create > EchoLine > Input Reader
    /// </summary>
    [CreateAssetMenu(
        fileName = "InputReader",
        menuName  = "EchoLine/Input Reader",
        order     = 1)]
    public sealed class InputReader : ScriptableObject
    {
        // ─────────────────────────────────────────────────────────────
        //  Events — subscribe in Awake, unsubscribe in OnDestroy
        // ─────────────────────────────────────────────────────────────

        /// <summary>Finger touched the screen. Provides world-space start position.</summary>
        public event Action<Vector2> OnDrawStart;

        /// <summary>Finger is moving across the screen. Provides current world-space position.</summary>
        public event Action<Vector2> OnDrawMove;

        /// <summary>Finger lifted. Line segment is complete.</summary>
        public event Action OnDrawEnd;

        /// <summary>Two fingers tapped simultaneously. Erase the last drawn line.</summary>
        public event Action OnEraseLast;

        // ─────────────────────────────────────────────────────────────
        //  Lifecycle — called by InputReaderEnabler (MonoBehaviour)
        // ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Call this from a MonoBehaviour's OnEnable.
        /// Enables the EnhancedTouch API and hooks callbacks.
        /// </summary>
        public void Enable()
        {
            EnhancedTouchSupport.Enable();
            Touch.onFingerDown += HandleFingerDown;
            Touch.onFingerMove += HandleFingerMove;
            Touch.onFingerUp   += HandleFingerUp;
        }

        /// <summary>
        /// Call this from a MonoBehaviour's OnDisable.
        /// </summary>
        public void Disable()
        {
            Touch.onFingerDown -= HandleFingerDown;
            Touch.onFingerMove -= HandleFingerMove;
            Touch.onFingerUp   -= HandleFingerUp;
            EnhancedTouchSupport.Disable();
        }

        // ─────────────────────────────────────────────────────────────
        //  Internal handlers
        // ─────────────────────────────────────────────────────────────

        private void HandleFingerDown(Finger finger)
        {
            // Two fingers down simultaneously → erase last line
            if (Touch.activeTouches.Count == 2)
            {
                OnEraseLast?.Invoke();
                return;
            }

            // Single finger → begin drawing
            if (finger.index == 0)
                OnDrawStart?.Invoke(ScreenToWorld(finger.screenPosition));
        }

        private void HandleFingerMove(Finger finger)
        {
            // Only track the primary finger for drawing
            if (finger.index == 0 && Touch.activeTouches.Count == 1)
                OnDrawMove?.Invoke(ScreenToWorld(finger.screenPosition));
        }

        private void HandleFingerUp(Finger finger)
        {
            // Primary finger lifted — end the current line segment
            if (finger.index == 0)
                OnDrawEnd?.Invoke();
        }

        // ─────────────────────────────────────────────────────────────
        //  Helpers
        // ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Converts a screen-space pixel position to world space using the
        /// main camera. Camera.main is cached internally by Unity after the
        /// first call per frame, so this is safe to call every finger-move event.
        /// </summary>
        private static Vector2 ScreenToWorld(Vector2 screenPos)
        {
            return Camera.main.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, 0f));
        }
    }
}
