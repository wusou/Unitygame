using UnityEngine;

namespace SwiftRunner
{
    public sealed class SwiftRunnerCameraFollow : MonoBehaviour
    {
        [SerializeField] private Vector3 offset = new(-2.5f, 0.85f, -10f);
        [SerializeField] private float followSharpness = 10f;

        private SwiftRunnerPlayerController target;

        public void Bind(SwiftRunnerPlayerController player)
        {
            target = player;
            if (target != null)
            {
                transform.position = new Vector3(target.ForwardX + offset.x, offset.y, offset.z);
            }
        }

        private void LateUpdate()
        {
            if (target == null)
            {
                return;
            }

            var desiredPosition = new Vector3(
                target.ForwardX + offset.x,
                target.transform.position.y * 0.14f + offset.y,
                offset.z);

            transform.position = Vector3.Lerp(
                transform.position,
                desiredPosition,
                1f - Mathf.Exp(-followSharpness * Time.deltaTime));
        }
    }
}
