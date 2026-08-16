using System.Collections.Generic;
using UnityEngine;
using EchoLine.Core;

namespace EchoLine.Gameplay
{
    /// <summary>
    /// Listens to InputReader events and renders player-drawn lines using
    /// Unity's LineRenderer component.
    ///
    /// Responsibilities:
    ///   • OnDrawStart  → begin a new LineRenderer segment
    ///   • OnDrawMove   → append world-space points to the active segment
    ///   • OnDrawEnd    → finalise and store the completed line
    ///   • OnEraseLast  → destroy the most recently completed line
    ///
    /// Assembly: EchoLine.Gameplay (references EchoLine.Core — already wired)
    /// Scene setup: attach to the "Managers" GameObject alongside InputReaderEnabler.
    /// </summary>
    public sealed class LineDrawer : MonoBehaviour
    {
        // ─────────────────────────────────────────────────────────────
        //  Inspector fields
        // ─────────────────────────────────────────────────────────────

        [Header("Dependencies")]
        [Tooltip("The shared InputReader ScriptableObject asset.")]
        [SerializeField] private InputReader _inputReader;

        [Header("Line Appearance")]
        [Tooltip("Material applied to every drawn line (use the Neon Cyan URP/Unlit material).")]
        [SerializeField] private Material _lineMaterial;

        [Tooltip("World-space width of the drawn line.")]
        [SerializeField] private float _lineWidth = 0.08f;

        [Tooltip("Minimum distance (world units) between recorded points. " +
                 "Prevents point spam on slow drags.")]
        [SerializeField] private float _minPointDistance = 0.05f;

        [Header("Limits")]
        [Tooltip("Maximum number of lines the player can draw at once.")]
        [SerializeField] private int _maxLines = 5;

        // ─────────────────────────────────────────────────────────────
        //  Private state
        // ─────────────────────────────────────────────────────────────

        // All completed line GameObjects, oldest first
        private readonly List<GameObject> _completedLines = new List<GameObject>();

        // The line currently being drawn (null when not drawing)
        private LineRenderer _activeLine;
        private List<Vector3> _activePoints;

        // ─────────────────────────────────────────────────────────────
        //  Unity lifecycle
        // ─────────────────────────────────────────────────────────────

        private void OnEnable()
        {
            if (_inputReader == null)
            {
                Debug.LogError("[LineDrawer] InputReader asset is not assigned.", this);
                return;
            }

            _inputReader.OnDrawStart  += HandleDrawStart;
            _inputReader.OnDrawMove   += HandleDrawMove;
            _inputReader.OnDrawEnd    += HandleDrawEnd;
            _inputReader.OnEraseLast  += HandleEraseLast;
        }

        private void OnDisable()
        {
            if (_inputReader == null) return;

            _inputReader.OnDrawStart  -= HandleDrawStart;
            _inputReader.OnDrawMove   -= HandleDrawMove;
            _inputReader.OnDrawEnd    -= HandleDrawEnd;
            _inputReader.OnEraseLast  -= HandleEraseLast;
        }

        // ─────────────────────────────────────────────────────────────
        //  Input handlers
        // ─────────────────────────────────────────────────────────────

        private void HandleDrawStart(Vector2 worldPos)
        {
            // Enforce max-line cap — erase the oldest line silently if needed
            if (_completedLines.Count >= _maxLines)
                DestroyLine(0);

            _activePoints = new List<Vector3> { worldPos };
            _activeLine   = CreateLineRenderer();
            ApplyPoint(worldPos);
        }

        private void HandleDrawMove(Vector2 worldPos)
        {
            if (_activeLine == null) return;

            Vector3 pos = worldPos;
            Vector3 last = _activePoints[_activePoints.Count - 1];

            // Deduplicate: skip points that are too close together
            if (Vector3.Distance(pos, last) < _minPointDistance) return;

            _activePoints.Add(pos);
            ApplyPoint(pos);
        }

        private void HandleDrawEnd()
        {
            if (_activeLine == null) return;

            // Discard single-point lines (accidental taps)
            if (_activePoints.Count < 2)
            {
                Destroy(_activeLine.gameObject);
            }
            else
            {
                _completedLines.Add(_activeLine.gameObject);
            }

            _activeLine  = null;
            _activePoints = null;
        }

        private void HandleEraseLast()
        {
            // Cancel an in-progress draw first
            if (_activeLine != null)
            {
                Destroy(_activeLine.gameObject);
                _activeLine   = null;
                _activePoints = null;
                return;
            }

            // Otherwise erase the most recently completed line
            if (_completedLines.Count > 0)
                DestroyLine(_completedLines.Count - 1);
        }

        // ─────────────────────────────────────────────────────────────
        //  Public API (called by future GameManager / reset logic)
        // ─────────────────────────────────────────────────────────────

        /// <summary>Destroys all drawn lines. Called on level reset.</summary>
        public void ClearAllLines()
        {
            // Cancel active draw
            if (_activeLine != null)
            {
                Destroy(_activeLine.gameObject);
                _activeLine   = null;
                _activePoints = null;
            }

            for (int i = _completedLines.Count - 1; i >= 0; i--)
                DestroyLine(i);
        }

        // ─────────────────────────────────────────────────────────────
        //  Helpers
        // ─────────────────────────────────────────────────────────────

        private LineRenderer CreateLineRenderer()
        {
            var go = new GameObject("PlayerLine");
            go.transform.SetParent(transform);

            var lr = go.AddComponent<LineRenderer>();
            lr.material          = _lineMaterial;
            lr.startWidth        = _lineWidth;
            lr.endWidth          = _lineWidth;
            lr.positionCount     = 0;
            lr.useWorldSpace     = true;
            lr.sortingLayerName  = "Gameplay";   // set this layer in your project
            lr.sortingOrder      = 1;

            return lr;
        }

        private void ApplyPoint(Vector2 worldPos)
        {
            _activeLine.positionCount++;
            _activeLine.SetPosition(_activeLine.positionCount - 1,
                new Vector3(worldPos.x, worldPos.y, 0f));
        }

        private void DestroyLine(int index)
        {
            if (index < 0 || index >= _completedLines.Count) return;
            Destroy(_completedLines[index]);
            _completedLines.RemoveAt(index);
        }
    }
}
