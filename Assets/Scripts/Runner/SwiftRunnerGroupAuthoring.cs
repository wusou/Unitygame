using UnityEngine;

namespace SwiftRunner
{
    [DisallowMultipleComponent]
    public sealed class SwiftRunnerGroupAuthoring : MonoBehaviour
    {
        [SerializeField] private string displayName;
        [SerializeField] private string role;
        [SerializeField] private float worldXMin = float.NaN;
        [SerializeField] private float worldXMax = float.NaN;
        [SerializeField] [TextArea(2, 4)] private string notes;

        public void Configure(string displayName, string role, float worldXMin = float.NaN, float worldXMax = float.NaN, string notes = null)
        {
            this.displayName = displayName;
            this.role = role;
            this.worldXMin = worldXMin;
            this.worldXMax = worldXMax;
            this.notes = notes ?? string.Empty;
        }
    }
}
