
using UnityEngine;
using UnityEngine.Rendering;

[ExecuteAlways]
[DisallowMultipleComponent]
public class MoonDiscController : MonoBehaviour
{
    [Header("Referências")]
    [Tooltip("Câmera do jogador. Se vazio, usa Camera.main.")]
    public Camera targetCamera;

    [Tooltip("Directional Light da lua. O disco visual será colocado na direção oposta ao forward dessa luz.")]
    public Light moonLight;

    [Tooltip("Renderer do Quad/Plane da lua.")]
    public Renderer moonRenderer;

    [Header("Posicionamento")]
    [Tooltip("Distância visual da lua em relação à câmera. Não precisa ser física, é só visual.")]
    public float distanceFromCamera = 450f;

    [Tooltip("Tamanho visual da lua.")]
    public float visualSize = 45f;

    [Tooltip("Mantém a lua sempre acompanhando a posição da câmera, como um objeto de céu.")]
    public bool followCameraPosition = true;

    [Tooltip("Faz o quad da lua olhar para a câmera.")]
    public bool faceCamera = true;

    [Header("Material / Visibilidade")]
    [Tooltip("Força material Unlit/Transparent para a lua continuar visível mesmo no escuro.")]
    public bool forceUnlitTransparentMaterial = true;

    [Tooltip("Textura PNG da lua. Opcional. Se vazio, usa a textura já colocada no material.")]
    public Texture2D moonTexture;

    public Color moonTint = new Color(0.72f, 0.82f, 1f, 1f);

    [Range(0f, 1f)]
    public float minAlpha = 0.05f;

    [Range(0f, 1f)]
    public float maxAlpha = 1f;

    [Tooltip("Se ligado, a lua aparece mais forte quando a Moon Light está forte e some de dia.")]
    public bool fadeWithMoonLight = true;

    [Tooltip("Use o mesmo valor aproximado do Moon Night Intensity do seu DayNightLightOnly. Exemplo: 0.18.")]
    public float referenceMoonIntensity = 0.18f;

    [Tooltip("Desativa o renderer se a lua estiver praticamente invisível.")]
    public bool disableRendererWhenInvisible = true;

    [Header("Debug")]
    public bool showDebugLogs = false;

    private Material runtimeMaterial;

    void Reset()
    {
        targetCamera = Camera.main;
        moonRenderer = GetComponent<Renderer>();

        GameObject moonObj = GameObject.Find("Moon Light");
        if (moonObj == null)
            moonObj = GameObject.Find("Moon_Light");
        if (moonObj == null)
            moonObj = GameObject.Find("Moon_Light_Generated");

        if (moonObj != null)
            moonLight = moonObj.GetComponent<Light>();
    }

    void Awake()
    {
        Setup();
    }

    void OnEnable()
    {
        Setup();
        ApplyNow();
    }

    void LateUpdate()
    {
        ApplyNow();
    }

    void OnValidate()
    {
        if (!gameObject.activeInHierarchy)
            return;

        Setup();
        ApplyNow();
    }

    [ContextMenu("Setup Moon Disc")]
    public void Setup()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;

        if (moonRenderer == null)
            moonRenderer = GetComponent<Renderer>();

        if (moonLight == null)
        {
            GameObject moonObj = GameObject.Find("Moon Light");
            if (moonObj == null)
                moonObj = GameObject.Find("Moon_Light");
            if (moonObj == null)
                moonObj = GameObject.Find("Moon_Light_Generated");

            if (moonObj != null)
                moonLight = moonObj.GetComponent<Light>();
        }

        if (moonRenderer != null)
        {
            moonRenderer.shadowCastingMode = ShadowCastingMode.Off;
            moonRenderer.receiveShadows = false;
        }

        SetupMaterial();
    }

    void SetupMaterial()
    {
        if (moonRenderer == null)
            return;

        Material mat = Application.isPlaying ? moonRenderer.material : moonRenderer.sharedMaterial;

        if (mat == null)
        {
            Shader shader = FindBestTransparentUnlitShader();
            mat = new Material(shader != null ? shader : Shader.Find("Sprites/Default"));
            mat.name = "Moon_Disc_Unlit_Transparent_Generated";

            if (Application.isPlaying)
                moonRenderer.material = mat;
            else
                moonRenderer.sharedMaterial = mat;
        }

        if (forceUnlitTransparentMaterial)
        {
            Shader shader = FindBestTransparentUnlitShader();

            if (shader != null && mat.shader != shader)
                mat.shader = shader;

            ConfigureTransparentMaterial(mat);
        }

        if (moonTexture != null)
        {
            if (mat.HasProperty("_BaseMap"))
                mat.SetTexture("_BaseMap", moonTexture);

            if (mat.HasProperty("_MainTex"))
                mat.SetTexture("_MainTex", moonTexture);
        }

        runtimeMaterial = mat;
    }

    Shader FindBestTransparentUnlitShader()
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");

        if (shader == null)
            shader = Shader.Find("Unlit/Transparent");

        if (shader == null)
            shader = Shader.Find("Sprites/Default");

        return shader;
    }

    void ConfigureTransparentMaterial(Material mat)
    {
        if (mat == null)
            return;

        if (mat.HasProperty("_Surface"))
            mat.SetFloat("_Surface", 1f);

        if (mat.HasProperty("_Blend"))
            mat.SetFloat("_Blend", 0f);

        if (mat.HasProperty("_AlphaClip"))
            mat.SetFloat("_AlphaClip", 0f);

        if (mat.HasProperty("_Cull"))
            mat.SetFloat("_Cull", 0f);

        mat.renderQueue = (int)RenderQueue.Transparent;

        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        mat.DisableKeyword("_ALPHATEST_ON");
        mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
    }

    [ContextMenu("Apply Moon Disc Now")]
    public void ApplyNow()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;

        if (moonRenderer == null)
            moonRenderer = GetComponent<Renderer>();

        if (moonLight == null || targetCamera == null)
            return;

        Vector3 moonSourceDirection = -moonLight.transform.forward.normalized;

        if (moonSourceDirection.sqrMagnitude < 0.001f)
            moonSourceDirection = Vector3.up;

        if (followCameraPosition)
            transform.position = targetCamera.transform.position + moonSourceDirection * distanceFromCamera;

        if (faceCamera)
        {
            Vector3 toCamera = targetCamera.transform.position - transform.position;

            if (toCamera.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.LookRotation(toCamera.normalized, Vector3.up);
        }

        transform.localScale = Vector3.one * visualSize;

        ApplyVisibility();

        if (showDebugLogs)
            Debug.Log("[MoonDiscController] Moon Disc alinhado com a Moon Light.", this);
    }

    void ApplyVisibility()
    {
        if (moonRenderer == null)
            return;

        Material mat = runtimeMaterial;

        if (mat == null)
            mat = Application.isPlaying ? moonRenderer.material : moonRenderer.sharedMaterial;

        float intensityFactor = 1f;

        if (fadeWithMoonLight)
        {
            float refIntensity = Mathf.Max(0.001f, referenceMoonIntensity);
            intensityFactor = moonLight != null ? Mathf.Clamp01(moonLight.intensity / refIntensity) : 1f;
        }

        float alpha = Mathf.Lerp(minAlpha, maxAlpha, intensityFactor);
        Color finalColor = moonTint;
        finalColor.a = alpha;

        if (mat != null)
        {
            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", finalColor);

            if (mat.HasProperty("_Color"))
                mat.SetColor("_Color", finalColor);
        }

        if (disableRendererWhenInvisible)
            moonRenderer.enabled = alpha > 0.025f;
        else
            moonRenderer.enabled = true;
    }
}
