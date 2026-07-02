using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Camera))]
[DefaultExecutionOrder(1000)]
public class PlayerCircleCutoutController : MonoBehaviour
{
    private static readonly int CutoutParamsId = Shader.PropertyToID("_PlayerCutoutParams");
    private static readonly int CutoutShapeId = Shader.PropertyToID("_PlayerCutoutShape");

    [SerializeField] private Transform target;
    [SerializeField] private Vector3 targetOffset = new Vector3(0f, 1f, 0f);
    [SerializeField, Range(0.01f, 0.3f)] private float radius = 0.09f;
    [SerializeField, Range(0f, 0.15f)] private float edgeSoftness = 0.025f;
    [SerializeField, Min(0f)] private float depthPadding = 0.5f;

    private Camera targetCamera;

    private void Awake()
    {
        targetCamera = GetComponent<Camera>();
    }

    private void LateUpdate()
    {
        if (targetCamera == null)
            targetCamera = GetComponent<Camera>();

        if (target == null)
            ResolveTarget();

        if (targetCamera == null || target == null)
        {
            DisableCutout();
            return;
        }

        Vector3 viewportPosition = targetCamera.WorldToViewportPoint(target.position + targetOffset);
        bool visible = viewportPosition.z > 0f
            && viewportPosition.x > -radius
            && viewportPosition.x < 1f + radius
            && viewportPosition.y > -radius
            && viewportPosition.y < 1f + radius;

        if (!visible)
        {
            DisableCutout();
            return;
        }

        Shader.SetGlobalVector(CutoutParamsId,
            new Vector4(viewportPosition.x, viewportPosition.y, viewportPosition.z, 1f));
        Shader.SetGlobalVector(CutoutShapeId,
            new Vector4(radius, edgeSoftness, depthPadding, 0f));
    }

    private void OnDisable()
    {
        DisableCutout();
    }

    private void ResolveTarget()
    {
        if (PlayerCore.Instance != null)
        {
            target = PlayerCore.Instance.transform;
            return;
        }

        PlayerCore player = FindObjectOfType<PlayerCore>();
        if (player != null)
            target = player.transform;
    }

    private static void DisableCutout()
    {
        Shader.SetGlobalVector(CutoutParamsId, Vector4.zero);
    }
}
