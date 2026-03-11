using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraFlow : MonoBehaviour
{
    [Header("目标")]
    [SerializeField] private Transform target;

    [Header("偏移与平滑")]
    [SerializeField] private Vector3 offset = new Vector3(0f, 1f, -10f);
    [SerializeField] private float smoothTime = 0.2f;

    [Header("边界限制")]
    [SerializeField] private bool useBounds = false;
    [SerializeField] private Vector2 minBounds;
    [SerializeField] private Vector2 maxBounds;

    private Vector3 velocity;
    private Camera cachedCamera;

    private void Awake()
    {
        cachedCamera = GetComponent<Camera>();
    }

    private void LateUpdate()
    {
        if (target == null)
        {
            return;
        }

        var desiredPos = target.position + offset;

        if (useBounds)
        {
            ClampToBounds(ref desiredPos);
        }

        transform.position = Vector3.SmoothDamp(transform.position, desiredPos, ref velocity, smoothTime);
    }

    private void ClampToBounds(ref Vector3 desiredPos)
    {
        var cam = cachedCamera != null ? cachedCamera : GetComponent<Camera>();
        if (cam == null || !cam.orthographic)
        {
            desiredPos.x = Mathf.Clamp(desiredPos.x, minBounds.x, maxBounds.x);
            desiredPos.y = Mathf.Clamp(desiredPos.y, minBounds.y, maxBounds.y);
            return;
        }

        var halfHeight = Mathf.Max(0f, cam.orthographicSize);
        var halfWidth = halfHeight * Mathf.Max(0.01f, cam.aspect);

        var clampedMinX = minBounds.x + halfWidth;
        var clampedMaxX = maxBounds.x - halfWidth;
        var clampedMinY = minBounds.y + halfHeight;
        var clampedMaxY = maxBounds.y - halfHeight;

        desiredPos.x = clampedMinX <= clampedMaxX
            ? Mathf.Clamp(desiredPos.x, clampedMinX, clampedMaxX)
            : (minBounds.x + maxBounds.x) * 0.5f;

        desiredPos.y = clampedMinY <= clampedMaxY
            ? Mathf.Clamp(desiredPos.y, clampedMinY, clampedMaxY)
            : (minBounds.y + maxBounds.y) * 0.5f;
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }
}
