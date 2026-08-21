using System.Collections.Generic;
using UnityEngine;

namespace EchoLine.Core
{
    // ─────────────────────────────────────────────────────────────────────────
    //  GameEventSO  —  void signal
    //
    //  A ScriptableObject event channel that carries no payload.
    //  Use for signals where the fact that something happened is enough:
    //  OnBallDied, OnBallEnteredBasket, OnLevelReset, etc.
    //
    //  Place this file in: Assets/Scripts/Core/
    //  Create assets via: Assets > Create > EchoLine > Events > Game Event
    //
    //  USAGE — RAISER
    //      [SerializeField] GameEventSO onBallDied;
    //      onBallDied.Raise();
    //
    //  USAGE — LISTENER  (in OnEnable / OnDisable)
    //      onBallDied.AddListener(HandleBallDied);
    //      onBallDied.RemoveListener(HandleBallDied);
    // ─────────────────────────────────────────────────────────────────────────

    [CreateAssetMenu(
        fileName = "GameEvent",
        menuName  = "EchoLine/Events/Game Event",
        order     = 10)]
    public sealed class GameEventSO : ScriptableObject
    {
        // Listeners stored as a list so we can iterate safely even if a
        // listener removes itself during the broadcast.
        private readonly List<System.Action> _listeners = new();

        public void Raise()
        {
            // Iterate a copy — a listener may call RemoveListener mid-loop.
            for (int i = _listeners.Count - 1; i >= 0; i--)
                _listeners[i]?.Invoke();
        }

        public void AddListener(System.Action listener)
        {
            if (!_listeners.Contains(listener))
                _listeners.Add(listener);
        }

        public void RemoveListener(System.Action listener)
        {
            _listeners.Remove(listener);
        }
    }


    // ─────────────────────────────────────────────────────────────────────────
    //  GameEventSO<T>  —  typed payload
    //
    //  Generic base for events that carry data.
    //  Not directly instantiable in the Editor — derive a concrete sealed
    //  class per type (see Vector2GameEventSO below) so Unity's asset menu
    //  and Inspector work correctly with no extra tooling.
    // ─────────────────────────────────────────────────────────────────────────

    public abstract class GameEventSO<T> : ScriptableObject
    {
        private readonly List<System.Action<T>> _listeners = new();

        public void Raise(T value)
        {
            for (int i = _listeners.Count - 1; i >= 0; i--)
                _listeners[i]?.Invoke(value);
        }

        public void AddListener(System.Action<T> listener)
        {
            if (!_listeners.Contains(listener))
                _listeners.Add(listener);
        }

        public void RemoveListener(System.Action<T> listener)
        {
            _listeners.Remove(listener);
        }
    }


    // ─────────────────────────────────────────────────────────────────────────
    //  Vector2GameEventSO
    //
    //  Concrete typed event carrying a world-space position.
    //  Used by BallPhysicsResponder to broadcast the contact point on a
    //  bounce so BallSonarEmitter can spawn the dynamic ripple at the
    //  correct location.
    //
    //  Create assets via: Assets > Create > EchoLine > Events > Vector2 Event
    // ─────────────────────────────────────────────────────────────────────────

    /*[CreateAssetMenu(
        fileName = "Vector2GameEvent",
        menuName  = "EchoLine/Events/Vector2 Event",
        order     = 11)]
    public sealed class Vector2GameEventSO : GameEventSO<Vector2> { }*/
}
