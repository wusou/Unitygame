using UnityEngine;

namespace SwiftRunner
{
    [DisallowMultipleComponent]
    public sealed class SwiftRunnerFinishGateAuthoring : MonoBehaviour
    {
        [SerializeField] private Transform gateFrame;
        [SerializeField] private Transform finishBanner;
        [SerializeField] private Transform triggerVolume;
        [SerializeField] private float finishLineX;
        [SerializeField] private Vector2 gateSize;
        [SerializeField] private Vector2 bannerSize;
        [SerializeField] private Vector2 triggerSize;

        public void Configure(
            Transform gateFrame,
            Transform finishBanner,
            Transform triggerVolume,
            float finishLineX,
            Vector2 gateSize,
            Vector2 bannerSize,
            Vector2 triggerSize)
        {
            this.gateFrame = gateFrame;
            this.finishBanner = finishBanner;
            this.triggerVolume = triggerVolume;
            this.finishLineX = finishLineX;
            this.gateSize = gateSize;
            this.bannerSize = bannerSize;
            this.triggerSize = triggerSize;
        }
    }
}
