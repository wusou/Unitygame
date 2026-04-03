using UnityEngine;

namespace SwiftRunner
{
    public sealed class SwiftRunnerObstacle : MonoBehaviour
    {
        private SwiftRunnerGameController controller;
        [SerializeField] private float forwardX;
        [SerializeField] private int laneIndex;
        [SerializeField] private float halfWidth;
        [SerializeField] private float clearanceHeight;
        [SerializeField] private float penalty;
        [SerializeField] private SwiftRunnerObstacleType obstacleType;
        [SerializeField] private Collider2D[] contactColliders;
        private bool consumed;

        public SwiftRunnerObstacleType ObstacleType => obstacleType;

        public void Configure(SwiftRunnerGameController gameController, SwiftRunnerObstacleType type, float obstacleX, int laneIndex, float halfWidth, float clearanceHeight, float penalty)
        {
            BindController(gameController);
            obstacleType = type;
            forwardX = obstacleX;
            this.laneIndex = laneIndex;
            this.halfWidth = halfWidth;
            this.clearanceHeight = clearanceHeight;
            this.penalty = penalty;
            consumed = false;
        }

        public void BindController(SwiftRunnerGameController gameController)
        {
            controller = gameController;
            consumed = false;
        }

        public void SetContactColliders(params Collider2D[] colliders)
        {
            contactColliders = colliders;
        }

        public void Resolve(SwiftRunnerPlayerController player)
        {
            if (player == null || consumed)
            {
                return;
            }

            if (!player.IsLaneCompatible(laneIndex) || !IsPlayerTouching(player))
            {
                return;
            }

            switch (obstacleType)
            {
                case SwiftRunnerObstacleType.Slowdown:
                    if (!player.ClearsHeight(clearanceHeight))
                    {
                        consumed = true;
                        controller.ApplySlowdown(penalty, "撞上障碍，节奏断了。");
                    }
                    break;

                case SwiftRunnerObstacleType.LowBarrier:
                    if (!player.IsSliding && !player.ClearsHeight(clearanceHeight))
                    {
                        controller.KillRunner("没滑过去，直接撞上低栏杆。");
                    }
                    break;

                case SwiftRunnerObstacleType.KillWall:
                    if (!player.ClearsHeight(clearanceHeight))
                    {
                        controller.KillRunner("你一头撞上了城墙。");
                    }
                    break;

                case SwiftRunnerObstacleType.Water:
                    if (!player.ClearsHeight(clearanceHeight))
                    {
                        controller.KillRunner("掉进了水里。");
                    }
                    break;

                case SwiftRunnerObstacleType.FinishGate:
                    controller.CompleteRun();
                    break;
            }
        }

        private bool IsInsideWaterCore(SwiftRunnerPlayerController player)
        {
            if (player == null)
            {
                return false;
            }

            var forgivingHalfWidth = Mathf.Max(0.1f, halfWidth - 0.42f);
            return Mathf.Abs(player.ForwardX - forwardX) <= forgivingHalfWidth;
        }

        private bool IsPlayerTouching(SwiftRunnerPlayerController player)
        {
            if (contactColliders != null && contactColliders.Length > 0)
            {
                for (var index = 0; index < contactColliders.Length; index++)
                {
                    if (player.IsOverlappingCollider(contactColliders[index]))
                    {
                        return obstacleType != SwiftRunnerObstacleType.Water || IsInsideWaterCore(player);
                    }
                }

                return false;
            }

            return player.IsOverlappingX(forwardX, halfWidth);
        }
    }
}
