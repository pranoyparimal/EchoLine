// ─────────────────────────────────────────────────────────────────────────────
//  LevelData.cs
//  Assembly : EchoLine.Core   (Assets/Scripts/Core/EchoLine.Core.asmdef)
//  Save .asset files to : Assets/Levels/World1/ or Assets/Levels/World2/
//
//  ScriptableObject holding every design parameter for one Echo Line level.
//  Both EchoLine.Gameplay and EchoLine.UI reference EchoLine.Core, so this
//  asset is readable from any assembly in the project without circular deps.
// ─────────────────────────────────────────────────────────────────────────────

using System.Collections.Generic;
using UnityEngine;

namespace EchoLine.Core
{
    [CreateAssetMenu(
        fileName = "LevelData_W1_L01",
        menuName  = "EchoLine/Level Data",
        order     = 0)]
    public sealed class LevelData : ScriptableObject
    {
        // ── Identity ──────────────────────────────────────────────────────────

        [Header("Identity")]

        [Tooltip("Global level index (1-based). Used for save keys and unlock checks.")]
        [Min(1)]
        public int levelNumber = 1;

        // ── Line rules ────────────────────────────────────────────────────────

        [Header("Line Rules")]

        [Tooltip("Maximum number of lines the player may draw before releasing the ball. " +
                 "0 = unlimited (not recommended in shipped levels).")]
        [Min(0)]
        public int lineLimit = 5;

        [Tooltip("Minimum lines drawn to earn 3 stars. " +
                 "Must be ≤ lineLimit. Set to 1 for levels where efficiency is the challenge.")]
        [Min(1)]
        public int minimumLinesForThreeStar = 1;

        // ── Sonar ─────────────────────────────────────────────────────────────

        [Header("Sonar")]

        [Tooltip("Seconds between automatic sonar pulses from the launcher. " +
                 "GDD default is 2 s. Lower values make hazards easier to read.")]
        [Min(0.5f)]
        public float sonarInterval = 2f;

        // ── Layout ────────────────────────────────────────────────────────────

        [Header("Layout (world-space positions)")]

        [Tooltip("World-space position of the ball launcher / start point.")]
        public Vector2 launcherPosition = new Vector2(0f, 4f);

        [Tooltip("World-space position of the centre of the goal basket.")]
        public Vector2 goalPosition = new Vector2(0f, -4f);

        // ── Obstacles ─────────────────────────────────────────────────────────

        [Header("Obstacles")]

        [Tooltip("World-space centre position for each obstacle. " +
                 "Index must match obstacleScales (same list length).")]
        public List<Vector2> obstaclePositions = new List<Vector2>();

        [Tooltip("Local scale (width, height) for each obstacle. " +
                 "Index must match obstaclePositions (same list length).")]
        public List<Vector2> obstacleScales = new List<Vector2>();

        // ── Validation ────────────────────────────────────────────────────────

        /// <summary>
        /// Returns true when the asset's data is internally consistent.
        /// Called by the custom Editor inspector and can be called at runtime
        /// (e.g. in LevelManager.LoadLevel) as a quick sanity check.
        /// </summary>
        public bool IsValid(out string error)
        {
            if (obstaclePositions.Count != obstacleScales.Count)
            {
                error = $"[LevelData] Level {levelNumber}: " +
                        $"obstaclePositions has {obstaclePositions.Count} entries but " +
                        $"obstacleScales has {obstacleScales.Count}. Lists must be the same length.";
                return false;
            }

            if (minimumLinesForThreeStar > lineLimit && lineLimit > 0)
            {
                error = $"[LevelData] Level {levelNumber}: " +
                        $"minimumLinesForThreeStar ({minimumLinesForThreeStar}) " +
                        $"exceeds lineLimit ({lineLimit}).";
                return false;
            }

            error = string.Empty;
            return true;
        }

#if UNITY_EDITOR
        // Runs in the Editor whenever the asset is saved or a field is changed
        // in the Inspector. Surfaces authoring errors immediately.
        private void OnValidate()
        {
            if (!IsValid(out string error))
                Debug.LogWarning(error, this);
        }
#endif
    }
}
