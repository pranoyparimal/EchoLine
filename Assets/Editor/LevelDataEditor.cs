// ─────────────────────────────────────────────────────────────────────────────
//  LevelDataEditor.cs
//  Assembly : EchoLine.Editor   (Assets/Editor/EchoLine.Editor.asmdef)
//
//  Custom Inspector for LevelData. Adds:
//    • A live validation banner (errors shown in red before you save)
//    • An obstacle count mismatch warning with a one-click fix button
//    • A "Normalize Scales" button (clamps each axis to > 0)
//    • A read-only summary line at the bottom
//
//  This file MUST live in Assets/Editor/ — the EchoLine.Editor asmdef
//  includes only the Editor platform, so it is stripped from all builds.
// ─────────────────────────────────────────────────────────────────────────────

using UnityEditor;
using UnityEngine;
using EchoLine.Core;

namespace EchoLine.Editor
{
    [CustomEditor(typeof(LevelData))]
    public sealed class LevelDataEditor : UnityEditor.Editor
    {
        // ── Serialized property handles (cached in OnEnable) ──────────────────

        SerializedProperty _levelNumber;
        SerializedProperty _lineLimit;
        SerializedProperty _minimumLinesForThreeStar;
        SerializedProperty _sonarInterval;
        SerializedProperty _launcherPosition;
        SerializedProperty _goalPosition;
        SerializedProperty _obstaclePositions;
        SerializedProperty _obstacleScales;

        // ── GUIStyles (built once, lazily) ────────────────────────────────────

        GUIStyle _errorBox;
        GUIStyle _summaryLabel;

        void OnEnable()
        {
            _levelNumber              = serializedObject.FindProperty("levelNumber");
            _lineLimit                = serializedObject.FindProperty("lineLimit");
            _minimumLinesForThreeStar = serializedObject.FindProperty("minimumLinesForThreeStar");
            _sonarInterval            = serializedObject.FindProperty("sonarInterval");
            _launcherPosition         = serializedObject.FindProperty("launcherPosition");
            _goalPosition             = serializedObject.FindProperty("goalPosition");
            _obstaclePositions        = serializedObject.FindProperty("obstaclePositions");
            _obstacleScales           = serializedObject.FindProperty("obstacleScales");
        }

        public override void OnInspectorGUI()
        {
            // Lazy style init (can't use new GUIStyle in OnEnable — GUI not ready yet)
            _errorBox     ??= new GUIStyle(EditorStyles.helpBox)   { richText = true };
            _summaryLabel ??= new GUIStyle(EditorStyles.miniLabel) { richText = true };

            serializedObject.Update();

            // ── Validation banner ─────────────────────────────────────────────
            var data = (LevelData)target;
            if (!data.IsValid(out string validationError))
            {
                EditorGUILayout.Space(4);
                EditorGUILayout.LabelField(
                    $"<color=red>⚠ {validationError}</color>",
                    _errorBox);
                EditorGUILayout.Space(4);
            }

            // ── Identity ──────────────────────────────────────────────────────
            EditorGUILayout.LabelField("Identity", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_levelNumber);
            EditorGUILayout.Space(6);

            // ── Line Rules ────────────────────────────────────────────────────
            EditorGUILayout.LabelField("Line Rules", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_lineLimit);
            EditorGUILayout.PropertyField(_minimumLinesForThreeStar);
            EditorGUILayout.Space(6);

            // ── Sonar ─────────────────────────────────────────────────────────
            EditorGUILayout.LabelField("Sonar", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_sonarInterval);
            EditorGUILayout.Space(6);

            // ── Layout ────────────────────────────────────────────────────────
            EditorGUILayout.LabelField("Layout", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_launcherPosition);
            EditorGUILayout.PropertyField(_goalPosition);
            EditorGUILayout.Space(6);

            // ── Obstacles ─────────────────────────────────────────────────────
            EditorGUILayout.LabelField("Obstacles", EditorStyles.boldLabel);

            int posCount   = _obstaclePositions.arraySize;
            int scaleCount = _obstacleScales.arraySize;

            // Mismatch fix button — shown only when lists are out of sync
            if (posCount != scaleCount)
            {
                EditorGUILayout.HelpBox(
                    $"List length mismatch: Positions={posCount}, Scales={scaleCount}.",
                    MessageType.Warning);

                if (GUILayout.Button("Fix: match Scales length to Positions"))
                {
                    // Grow or shrink _obstacleScales to match _obstaclePositions
                    while (_obstacleScales.arraySize < _obstaclePositions.arraySize)
                    {
                        _obstacleScales.InsertArrayElementAtIndex(_obstacleScales.arraySize);
                        var elem = _obstacleScales.GetArrayElementAtIndex(
                                       _obstacleScales.arraySize - 1);
                        elem.vector2Value = Vector2.one;
                    }
                    while (_obstacleScales.arraySize > _obstaclePositions.arraySize)
                        _obstacleScales.DeleteArrayElementAtIndex(_obstacleScales.arraySize - 1);
                }
            }

            EditorGUILayout.PropertyField(_obstaclePositions, true);
            EditorGUILayout.PropertyField(_obstacleScales, true);

            // Normalize scales button — clamps each axis to a minimum of 0.05
            if (_obstacleScales.arraySize > 0 &&
                GUILayout.Button("Normalize Scales (clamp axes to 0.05 min)"))
            {
                for (int i = 0; i < _obstacleScales.arraySize; i++)
                {
                    var elem = _obstacleScales.GetArrayElementAtIndex(i);
                    var v    = elem.vector2Value;
                    elem.vector2Value = new Vector2(
                        Mathf.Max(v.x, 0.05f),
                        Mathf.Max(v.y, 0.05f));
                }
            }

            EditorGUILayout.Space(8);

            // ── Summary ───────────────────────────────────────────────────────
            EditorGUILayout.LabelField(
                $"<b>Level {_levelNumber.intValue}</b> — " +
                $"{_obstaclePositions.arraySize} obstacles · " +
                $"Line limit: {_lineLimit.intValue} · " +
                $"Sonar: {_sonarInterval.floatValue:F1} s",
                _summaryLabel);

            serializedObject.ApplyModifiedProperties();
        }
    }
}
