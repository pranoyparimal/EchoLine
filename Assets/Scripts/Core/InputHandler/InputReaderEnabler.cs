using UnityEngine;

namespace EchoLine.Core
{
    /// <summary>
    /// Thin MonoBehaviour whose only job is to call Enable/Disable on the
    /// InputReader ScriptableObject at the right Unity lifecycle moments.
    ///
    /// Why a separate MonoBehaviour?
    /// ScriptableObjects have no OnEnable/OnDisable tied to scene load/unload.
    /// This component bridges that gap without putting any game logic here.
    ///
    /// Scene setup: attach to a dedicated "Managers" GameObject in Game.unity.
    /// Drag the InputReader asset into the Inspector field.
    /// </summary>
    public sealed class InputReaderEnabler : MonoBehaviour
    {
        [Tooltip("The InputReader ScriptableObject asset from Assets/Scripts/Core/")]
        [SerializeField] private InputReader _inputReader;

        private void OnEnable()
        {
            if (_inputReader == null)
            {
                Debug.LogError("[InputReaderEnabler] InputReader asset is not assigned.", this);
                return;
            }

            _inputReader.Enable();
        }

        private void OnDisable()
        {
            _inputReader?.Disable();
        }
    }
}
