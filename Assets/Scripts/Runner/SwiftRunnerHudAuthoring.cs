using TMPro;
using UnityEngine;

namespace SwiftRunner
{
    [DisallowMultipleComponent]
    public sealed class SwiftRunnerHudAuthoring : MonoBehaviour
    {
        [SerializeField] private RectTransform topLeftPanel;
        [SerializeField] private RectTransform topCenterPanel;
        [SerializeField] private RectTransform bottomHintPanel;
        [SerializeField] private TextMeshProUGUI comboLabel;
        [SerializeField] private TextMeshProUGUI phaseLabel;
        [SerializeField] private TextMeshProUGUI statusLabel;
        [SerializeField] private TextMeshProUGUI hintLabel;

        public void Configure(
            RectTransform topLeftPanel,
            RectTransform topCenterPanel,
            RectTransform bottomHintPanel,
            TextMeshProUGUI comboLabel,
            TextMeshProUGUI phaseLabel,
            TextMeshProUGUI statusLabel,
            TextMeshProUGUI hintLabel)
        {
            this.topLeftPanel = topLeftPanel;
            this.topCenterPanel = topCenterPanel;
            this.bottomHintPanel = bottomHintPanel;
            this.comboLabel = comboLabel;
            this.phaseLabel = phaseLabel;
            this.statusLabel = statusLabel;
            this.hintLabel = hintLabel;
        }
    }
}
