using UnityEngine;

namespace EchoLine.Core
{
    /// <summary>
    /// Implemented by any GameObject that should respond to physical contact
    /// with the ball (hazards, goal basket, future interactable types).
    ///
    /// Place this file in: Assets/Scripts/Core/
    ///
    /// CONTRACT
    /// --------
    /// • BallPhysicsResponder calls OnBallContact() on the first frame of
    ///   collision (OnCollisionEnter2D). The implementor is responsible for
    ///   raising whatever GameEvents the rest of the game needs to hear.
    /// • Implementors must NOT reach back into BallPhysicsResponder or any
    ///   other ball script directly — communicate outward via GameEvents only.
    /// • The Collision2D parameter is supplied so implementors can read the
    ///   contact point, normal, or relative velocity if needed (e.g. a hazard
    ///   that behaves differently at low vs high impact speed).
    /// </summary>
    public interface IBallContactHandler
    {
        void OnBallContact(Collision2D collision);
    }
}
