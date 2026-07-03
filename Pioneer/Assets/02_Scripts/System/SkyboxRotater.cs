using UnityEngine;

public class SkyboxRotater : MonoBehaviour
{
    [SerializeField] private Material material;
    [SerializeField] private float degreesPerSecond = 1f;
    [SerializeField] private bool cloneMaterialAtRuntime = true;

    private static readonly int RotationID = Shader.PropertyToID("_Rotation");
    private static SkyboxRotater instance;

    private Material sourceMaterial;
    private Material runtimeMaterial;
    private Material activeMaterial;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (FindObjectOfType<SkyboxRotater>() != null)
            return;

        GameObject go = new GameObject(nameof(SkyboxRotater));
        DontDestroyOnLoad(go);
        go.AddComponent<SkyboxRotater>();
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
    }

    private void Update()
    {
        if (!TryUpdateActiveMaterial())
            return;

        float rotation = activeMaterial.GetFloat(RotationID);
        rotation = Mathf.Repeat(rotation + degreesPerSecond * Time.deltaTime, 360f);
        activeMaterial.SetFloat(RotationID, rotation);
    }

    private bool TryUpdateActiveMaterial()
    {
        if (material == null && runtimeMaterial != null && RenderSettings.skybox == runtimeMaterial)
            return activeMaterial != null && activeMaterial.HasProperty(RotationID);

        Material targetMaterial = material != null ? material : RenderSettings.skybox;

        if (targetMaterial == null || !targetMaterial.HasProperty(RotationID))
            return false;

        if (targetMaterial == sourceMaterial && activeMaterial != null)
        {
            if (material == null && runtimeMaterial != null && RenderSettings.skybox == sourceMaterial)
                RenderSettings.skybox = runtimeMaterial;

            return true;
        }

        ReleaseRuntimeMaterial();

        sourceMaterial = targetMaterial;
        activeMaterial = sourceMaterial;

        if (Application.isPlaying && cloneMaterialAtRuntime)
        {
            runtimeMaterial = new Material(sourceMaterial)
            {
                name = $"{sourceMaterial.name} (Runtime)"
            };

            activeMaterial = runtimeMaterial;

            if (material == null || RenderSettings.skybox == sourceMaterial)
                RenderSettings.skybox = runtimeMaterial;
        }

        return true;
    }

    private void OnDestroy()
    {
        if (instance == this)
            instance = null;

        if (runtimeMaterial != null && RenderSettings.skybox == runtimeMaterial)
            RenderSettings.skybox = sourceMaterial;

        ReleaseRuntimeMaterial();
    }

    private void ReleaseRuntimeMaterial()
    {
        if (runtimeMaterial == null)
            return;

        Destroy(runtimeMaterial);
        runtimeMaterial = null;
    }
}
