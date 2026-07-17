using System;
using System.Collections.Generic;
using Agrovator.PitchSimulator.GuidedPitch;
using UnityEngine;
using UnityEngine.UI;

namespace Agrovator.PitchSimulator.UI
{
    [Serializable]
    public sealed class PitchFeedbackRow
    {
        [SerializeField] private GameObject root;
        [SerializeField] private Text labelText;
        [SerializeField] private Text valueText;

        public PitchFeedbackRow(GameObject root, Text labelText, Text valueText)
        {
            this.root = root;
            this.labelText = labelText;
            this.valueText = valueText;
        }

        public GameObject Root => root;
        public Text LabelText => labelText;
        public Text ValueText => valueText;
        internal bool IsValid => root != null && labelText != null && valueText != null;

        internal void Render(string label, string value)
        {
            labelText.text = label ?? string.Empty;
            valueText.text = value ?? string.Empty;
            root.SetActive(true);
        }

        internal void Clear()
        {
            if (labelText != null) labelText.text = string.Empty;
            if (valueText != null) valueText.text = string.Empty;
            if (root != null) root.SetActive(false);
        }
    }

    public sealed class PitchFeedbackView : MonoBehaviour
    {
        private const int RequiredRowCount = 3;

        [SerializeField] private PitchFeedbackRow[] rows = new PitchFeedbackRow[RequiredRowCount];

        public IReadOnlyList<PitchFeedbackRow> Rows => rows;

        public void Configure(PitchFeedbackRow[] feedbackRows)
        {
            rows = feedbackRows == null ? null : (PitchFeedbackRow[])feedbackRows.Clone();
            ValidateRows();
        }

        public void Render(GuidedPitchFeedback feedback, Func<string, string> localize)
        {
            if (feedback == null) throw new ArgumentNullException(nameof(feedback));
            if (localize == null) throw new ArgumentNullException(nameof(localize));
            ValidateRows();

            rows[0].Render(localize("guided.feedback.worked"), localize(feedback.WorkedKey));
            rows[1].Render(localize("guided.feedback.missing"), localize(feedback.MissingKey));
            rows[2].Render(localize("guided.feedback.improve"), localize(feedback.ImproveKey));
        }

        public void Clear()
        {
            if (rows == null) return;
            foreach (var row in rows) row?.Clear();
        }

        private void ValidateRows()
        {
            if (rows == null || rows.Length != RequiredRowCount)
            {
                throw new InvalidOperationException("Pitch feedback requires exactly three prebuilt rows.");
            }

            var roots = new HashSet<GameObject>();
            foreach (var row in rows)
            {
                if (row == null || !row.IsValid || !roots.Add(row.Root))
                {
                    throw new InvalidOperationException("Pitch feedback rows must be valid and distinct.");
                }
            }
        }
    }
}
