
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class FactoryTerminalCRTEffectBuilder : MonoBehaviour
{
    [Header("Referência")]
    [Tooltip("Canvas do terminal. Se vazio, o script tenta pegar o Canvas no próprio objeto.")]
    public Canvas targetCanvas;

    [Tooltip("Onde o overlay será criado. Se vazio, usa o RectTransform do Canvas.")]
    public RectTransform overlayParent;

    [Header("Criação")]
    public bool buildOnEnable = true;
    public bool rebuildIfMissing = true;
    public bool keepOverlayOnTop = true;

    [Header("Intensidade geral")]
    [Range(0f, 1f)] public float masterAlpha = 0.85f;

    [Header("Ruído procedural")]
    public bool enableNoise = true;
    [Range(32, 512)] public int noiseTextureWidth = 192;
    [Range(32, 512)] public int noiseTextureHeight = 108;
    [Range(0f, 1f)] public float noiseAlpha = 0.22f;
    [Range(0f, 1f)] public float noiseContrast = 0.65f;
    public float noiseRefreshRate = 26f;
    public Vector2 noiseScrollSpeed = new Vector2(0.00f, -0.35f);
    public float noiseJitter = 0.018f;

    [Header("Scanlines")]
    public bool enableScanlines = true;
    [Range(2, 16)] public int scanlineTextureHeight = 6;
    [Range(0f, 1f)] public float scanlineAlpha = 0.26f;
    public float scanlineScrollSpeed = 0.08f;

    [Header("Vinheta")]
    public bool enableVignette = true;
    [Range(64, 1024)] public int vignetteTextureSize = 512;
    [Range(0f, 1f)] public float vignetteAlpha = 0.65f;
    [Range(0.1f, 3f)] public float vignettePower = 1.65f;

    [Header("Flicker")]
    public bool enableFlicker = true;
    [Range(0f, 1f)] public float minFlicker = 0.78f;
    [Range(0f, 1f)] public float maxFlicker = 1.0f;
    public float flickerSpeed = 11f;

    [Header("Linha de varredura")]
    public bool enableSweepLine = true;
    [Range(2f, 80f)] public float sweepLineHeight = 22f;
    [Range(0f, 1f)] public float sweepLineAlpha = 0.22f;
    public float sweepDuration = 2.2f;

    [Header("Nomes dos objetos gerados")]
    public string rootName = "CRT_Effect_Overlay_Generated";
    public string noiseName = "CRT_Noise_Procedural";
    public string scanlinesName = "CRT_Scanlines_Procedural";
    public string vignetteName = "CRT_Vignette_Procedural";
    public string sweepLineName = "CRT_SweepLine_Procedural";

    private RectTransform root;
    private CanvasGroup rootCanvasGroup;

    private RawImage noiseImage;
    private RawImage scanlinesImage;
    private RawImage vignetteImage;
    private Image sweepLineImage;

    private Texture2D noiseTexture;
    private Texture2D scanlineTexture;
    private Texture2D vignetteTexture;

    private float nextNoiseRefreshTime;
    private float flickerSeed;
    private Vector2 noiseOffset;
    private float scanlineOffset;

    void Reset()
    {
        targetCanvas = GetComponent<Canvas>();
        if (targetCanvas == null)
            targetCanvas = GetComponentInParent<Canvas>();
    }

    void Awake()
    {
        AutoFindReferences();
        flickerSeed = Random.Range(0f, 999f);
    }

    void OnEnable()
    {
        AutoFindReferences();

        if (buildOnEnable && (rebuildIfMissing || root == null))
            BuildIfNeeded();

        flickerSeed = Random.Range(0f, 999f);
    }

    void Update()
    {
        if (!Application.isPlaying)
            return;

        if (root == null)
        {
            if (buildOnEnable && rebuildIfMissing)
                BuildIfNeeded();

            if (root == null)
                return;
        }

        if (keepOverlayOnTop)
            root.SetAsLastSibling();

        AnimateEffect();
    }

    void AutoFindReferences()
    {
        if (targetCanvas == null)
            targetCanvas = GetComponent<Canvas>();

        if (targetCanvas == null)
            targetCanvas = GetComponentInParent<Canvas>(true);

        if (overlayParent == null && targetCanvas != null)
            overlayParent = targetCanvas.transform as RectTransform;
    }

    [ContextMenu("Build/Rebuild CRT Effect Overlay")]
    public void BuildOrRebuild()
    {
        AutoFindReferences();
        DeleteGeneratedOverlay();
        BuildIfNeeded();
    }

    [ContextMenu("Delete CRT Effect Overlay")]
    public void DeleteGeneratedOverlay()
    {
        Transform old = transform.Find(rootName);

        if (old == null && overlayParent != null)
            old = overlayParent.Find(rootName);

        if (old != null)
        {
            if (Application.isPlaying)
                Destroy(old.gameObject);
            else
                DestroyImmediate(old.gameObject);
        }

        root = null;
        rootCanvasGroup = null;
        noiseImage = null;
        scanlinesImage = null;
        vignetteImage = null;
        sweepLineImage = null;
    }

    public void BuildIfNeeded()
    {
        AutoFindReferences();

        if (overlayParent == null)
        {
            Debug.LogWarning("[FactoryTerminalCRTEffectBuilder] Nenhum Canvas/RectTransform encontrado para criar o efeito CRT.", this);
            return;
        }

        Transform existing = overlayParent.Find(rootName);
        if (existing != null)
        {
            root = existing as RectTransform;
            rootCanvasGroup = root.GetComponent<CanvasGroup>();
            if (rootCanvasGroup == null)
                rootCanvasGroup = root.gameObject.AddComponent<CanvasGroup>();

            CacheGeneratedChildren();
            return;
        }

        GameObject rootObj = new GameObject(rootName, typeof(RectTransform), typeof(CanvasGroup));
        rootObj.transform.SetParent(overlayParent, false);

        root = rootObj.GetComponent<RectTransform>();
        StretchFull(root);

        rootCanvasGroup = rootObj.GetComponent<CanvasGroup>();
        rootCanvasGroup.interactable = false;
        rootCanvasGroup.blocksRaycasts = false;
        rootCanvasGroup.alpha = masterAlpha;

        if (enableNoise)
            noiseImage = CreateRawImage(noiseName, root, CreateNoiseTexture(), new Color(0.70f, 0.95f, 1f, noiseAlpha));

        if (enableScanlines)
            scanlinesImage = CreateRawImage(scanlinesName, root, CreateScanlineTexture(), new Color(0f, 0f, 0f, scanlineAlpha));

        if (enableVignette)
            vignetteImage = CreateRawImage(vignetteName, root, CreateVignetteTexture(), new Color(0f, 0f, 0f, vignetteAlpha));

        if (enableSweepLine)
            sweepLineImage = CreateSweepLine(sweepLineName, root);

        root.SetAsLastSibling();
    }

    void CacheGeneratedChildren()
    {
        if (root == null)
            return;

        Transform noise = root.Find(noiseName);
        Transform scan = root.Find(scanlinesName);
        Transform vignette = root.Find(vignetteName);
        Transform sweep = root.Find(sweepLineName);

        if (noise != null)
            noiseImage = noise.GetComponent<RawImage>();

        if (scan != null)
            scanlinesImage = scan.GetComponent<RawImage>();

        if (vignette != null)
            vignetteImage = vignette.GetComponent<RawImage>();

        if (sweep != null)
            sweepLineImage = sweep.GetComponent<Image>();
    }

    RawImage CreateRawImage(string objName, RectTransform parent, Texture2D texture, Color color)
    {
        GameObject obj = new GameObject(objName, typeof(RectTransform), typeof(RawImage));
        obj.transform.SetParent(parent, false);

        RectTransform rt = obj.GetComponent<RectTransform>();
        StretchFull(rt);

        RawImage raw = obj.GetComponent<RawImage>();
        raw.texture = texture;
        raw.color = color;
        raw.raycastTarget = false;

        return raw;
    }

    Image CreateSweepLine(string objName, RectTransform parent)
    {
        GameObject obj = new GameObject(objName, typeof(RectTransform), typeof(Image));
        obj.transform.SetParent(parent, false);

        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 0.5f);
        rt.anchorMax = new Vector2(1f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = new Vector2(0f, -sweepLineHeight * 0.5f);
        rt.offsetMax = new Vector2(0f, sweepLineHeight * 0.5f);

        Image img = obj.GetComponent<Image>();
        img.color = new Color(0.65f, 0.95f, 1f, sweepLineAlpha);
        img.raycastTarget = false;

        return img;
    }

    void StretchFull(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        rt.anchoredPosition = Vector2.zero;
        rt.localScale = Vector3.one;
    }

    Texture2D CreateNoiseTexture()
    {
        noiseTexture = new Texture2D(noiseTextureWidth, noiseTextureHeight, TextureFormat.RGBA32, false);
        noiseTexture.name = "CRT_Noise_Texture_Procedural";
        noiseTexture.wrapMode = TextureWrapMode.Repeat;
        noiseTexture.filterMode = FilterMode.Point;

        FillNoiseTexture();
        return noiseTexture;
    }

    void FillNoiseTexture()
    {
        if (noiseTexture == null)
            return;

        Color32[] pixels = new Color32[noiseTexture.width * noiseTexture.height];

        for (int i = 0; i < pixels.Length; i++)
        {
            float r = Random.value;
            r = Mathf.Lerp(0.5f, r, noiseContrast);

            byte v = (byte)Mathf.Clamp(Mathf.RoundToInt(r * 255f), 0, 255);
            pixels[i] = new Color32(v, v, v, 255);
        }

        noiseTexture.SetPixels32(pixels);
        noiseTexture.Apply(false, false);
    }

    Texture2D CreateScanlineTexture()
    {
        scanlineTexture = new Texture2D(1, scanlineTextureHeight, TextureFormat.RGBA32, false);
        scanlineTexture.name = "CRT_Scanline_Texture_Procedural";
        scanlineTexture.wrapMode = TextureWrapMode.Repeat;
        scanlineTexture.filterMode = FilterMode.Point;

        Color32[] pixels = new Color32[scanlineTextureHeight];

        for (int y = 0; y < scanlineTextureHeight; y++)
        {
            byte alpha = 0;

            if (y == 0)
                alpha = 255;
            else if (y == 1)
                alpha = 130;
            else
                alpha = 0;

            pixels[y] = new Color32(255, 255, 255, alpha);
        }

        scanlineTexture.SetPixels32(pixels);
        scanlineTexture.Apply(false, false);

        return scanlineTexture;
    }

    Texture2D CreateVignetteTexture()
    {
        vignetteTexture = new Texture2D(vignetteTextureSize, vignetteTextureSize, TextureFormat.RGBA32, false);
        vignetteTexture.name = "CRT_Vignette_Texture_Procedural";
        vignetteTexture.wrapMode = TextureWrapMode.Clamp;
        vignetteTexture.filterMode = FilterMode.Bilinear;

        Color32[] pixels = new Color32[vignetteTextureSize * vignetteTextureSize];

        float center = (vignetteTextureSize - 1) * 0.5f;

        for (int y = 0; y < vignetteTextureSize; y++)
        {
            for (int x = 0; x < vignetteTextureSize; x++)
            {
                float dx = (x - center) / center;
                float dy = (y - center) / center;

                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                float alpha = Mathf.Clamp01(Mathf.Pow(Mathf.InverseLerp(0.35f, 1.05f, dist), vignettePower));

                byte a = (byte)Mathf.RoundToInt(alpha * 255f);
                pixels[y * vignetteTextureSize + x] = new Color32(255, 255, 255, a);
            }
        }

        vignetteTexture.SetPixels32(pixels);
        vignetteTexture.Apply(false, false);

        return vignetteTexture;
    }

    void AnimateEffect()
    {
        float dt = Time.unscaledDeltaTime;
        float t = Time.unscaledTime;

        if (rootCanvasGroup != null)
        {
            float flickerValue = 1f;

            if (enableFlicker)
                flickerValue = Mathf.Lerp(minFlicker, maxFlicker, Mathf.PerlinNoise(t * flickerSpeed, flickerSeed));

            rootCanvasGroup.alpha = masterAlpha * flickerValue;
        }

        if (enableNoise && noiseImage != null)
        {
            if (noiseRefreshRate > 0f && t >= nextNoiseRefreshTime)
            {
                FillNoiseTexture();
                nextNoiseRefreshTime = t + (1f / noiseRefreshRate);
            }

            noiseOffset += noiseScrollSpeed * dt;

            float jitterX = Random.Range(-noiseJitter, noiseJitter);
            float jitterY = Random.Range(-noiseJitter, noiseJitter);

            noiseImage.uvRect = new Rect(noiseOffset.x + jitterX, noiseOffset.y + jitterY, 1.05f, 1.05f);
            noiseImage.color = new Color(0.70f, 0.95f, 1f, noiseAlpha);
        }

        if (enableScanlines && scanlinesImage != null)
        {
            scanlineOffset += scanlineScrollSpeed * dt;

            float repeatY = Mathf.Max(1f, Screen.height / Mathf.Max(1f, scanlineTextureHeight));
            scanlinesImage.uvRect = new Rect(0f, scanlineOffset, 1f, repeatY);
            scanlinesImage.color = new Color(0f, 0f, 0f, scanlineAlpha);
        }

        if (enableVignette && vignetteImage != null)
        {
            vignetteImage.color = new Color(0f, 0f, 0f, vignetteAlpha);
        }

        if (enableSweepLine && sweepLineImage != null)
        {
            RectTransform rt = sweepLineImage.rectTransform;
            RectTransform parent = root;

            float height = parent.rect.height;
            if (height <= 1f)
                height = Screen.height;

            float phase = Mathf.Repeat(t / Mathf.Max(0.01f, sweepDuration), 1f);
            float y = Mathf.Lerp(-height * 0.55f, height * 0.55f, phase);

            rt.anchoredPosition = new Vector2(0f, y);

            float fade = Mathf.Sin(phase * Mathf.PI);
            sweepLineImage.color = new Color(0.65f, 0.95f, 1f, sweepLineAlpha * fade);
        }
    }

    public void SetEffectVisible(bool visible)
    {
        if (root != null)
            root.gameObject.SetActive(visible);
    }

    public void SetMasterAlpha(float alpha)
    {
        masterAlpha = Mathf.Clamp01(alpha);
    }
}
