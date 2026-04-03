using UnityEngine;

namespace SwiftRunner
{
    public sealed class SwiftRunnerEnemy : MonoBehaviour
    {
        [SerializeField] private int laneIndex;
        [SerializeField] private float forwardX;
        [SerializeField] private Collider2D bodyCollider;
        [SerializeField] private Collider2D stompCollider;

        private SpriteRenderer[] renderers;

        public int LaneIndex => laneIndex;
        public float ForwardX => forwardX;
        public bool IsAlive { get; private set; } = true;

        private void Awake()
        {
            CacheRenderers();
        }

        public void Configure(SwiftRunnerGameController gameController, int laneIndex, float forwardX)
        {
            this.laneIndex = laneIndex;
            this.forwardX = forwardX;
            IsAlive = true;
            CacheRenderers();
        }

        public void SetContactColliders(Collider2D bodyContact, Collider2D stompContact)
        {
            bodyCollider = bodyContact;
            stompCollider = stompContact;
        }

        public bool CanBeStompedBy(SwiftRunnerPlayerController player)
        {
            return IsAlive &&
                   player.IsDescending &&
                   player.IsLaneCompatible(LaneIndex) &&
                   player.IsOverlappingCollider(stompCollider);
        }

        public bool IsThreatening(SwiftRunnerPlayerController player)
        {
            return IsAlive &&
                   player.IsLaneCompatible(LaneIndex) &&
                   player.IsOverlappingCollider(bodyCollider) &&
                   !player.IsAboveColliderTop(bodyCollider, 0.12f);
        }

        public void Kill(SwiftRunnerKillMethod method)
        {
            if (!IsAlive)
            {
                return;
            }

            IsAlive = false;

            if (renderers != null)
            {
                for (var index = 0; index < renderers.Length; index++)
                {
                    if (renderers[index] == null)
                    {
                        continue;
                    }

                    renderers[index].color = method == SwiftRunnerKillMethod.Stomp
                        ? new Color(1f, 0.88f, 0.32f, 1f)
                        : new Color(0.88f, 0.42f, 0.28f, 1f);
                }
            }

            transform.localScale = new Vector3(1.25f, 0.25f, 1f);
            Destroy(gameObject, 0.12f);
        }

        private void CacheRenderers()
        {
            renderers = GetComponentsInChildren<SpriteRenderer>(true);
        }
    }
}
