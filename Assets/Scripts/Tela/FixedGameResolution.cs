
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class FixedGameResolution : MonoBehaviour
{
    [Header("Resolução fixa")]
    public int targetWidth = 1080;
    public int targetHeight = 720;

    [Tooltip("Windowed = janela fixa. FullScreenWindow = tela cheia sem borda.")]
    public FullScreenMode screenMode = FullScreenMode.Windowed;

    [Tooltip("Se ligado, tenta forçar novamente caso a janela seja redimensionada.")]
    public bool keepResolutionLocked = true;

    [Tooltip("Intervalo em segundos para verificar se a resolução mudou.")]
    public float checkInterval = 0.25f;

    [Header("UI / Canvas")]
    [Tooltip("Ajusta todos os CanvasScaler da cena para a resolução fixa.")]
    public bool configureCanvasScalers = true;

    public CanvasScaler.ScaleMode uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
    public CanvasScaler.ScreenMatchMode screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;

    [Range(0f, 1f)]
    public float matchWidthOrHeight = 0.5f;

    [Header("Persistência")]
    public bool dontDestroyOnLoad = true;

    [Header("Debug")]
    public bool showDebugLogs = false;

    private float nextCheckTime;

    void Awake()
    {
        if (dontDestroyOnLoad)
            DontDestroyOnLoad(gameObject);

        ApplyResolution();
        ConfigureCanvasScalers();
    }

    void Start()
    {
        ApplyResolution();
        ConfigureCanvasScalers();
    }

    void Update()
    {
        if (!keepResolutionLocked)
            return;

        if (Time.unscaledTime < nextCheckTime)
            return;

        nextCheckTime = Time.unscaledTime + Mathf.Max(0.05f, checkInterval);

        if (Screen.width != targetWidth || Screen.height != targetHeight || Screen.fullScreenMode != screenMode)
        {
            ApplyResolution();
            ConfigureCanvasScalers();
        }
    }

    [ContextMenu("Apply Fixed Resolution")]
    public void ApplyResolution()
    {
        targetWidth = Mathf.Max(320, targetWidth);
        targetHeight = Mathf.Max(240, targetHeight);

        Screen.SetResolution(targetWidth, targetHeight, screenMode);

        if (showDebugLogs)
        {
            Debug.Log(
                "[FixedGameResolution] Resolução aplicada: " +
                targetWidth + "x" + targetHeight + " | Mode: " + screenMode,
                this
            );
        }
    }

    [ContextMenu("Configure Canvas Scalers")]
    public void ConfigureCanvasScalers()
    {
        if (!configureCanvasScalers)
            return;

        CanvasScaler[] scalers = FindObjectsOfType<CanvasScaler>(true);

        foreach (CanvasScaler scaler in scalers)
        {
            if (scaler == null)
                continue;

            scaler.uiScaleMode = uiScaleMode;
            scaler.referenceResolution = new Vector2(targetWidth, targetHeight);
            scaler.screenMatchMode = screenMatchMode;
            scaler.matchWidthOrHeight = matchWidthOrHeight;
        }

        if (showDebugLogs)
            Debug.Log("[FixedGameResolution] CanvasScalers configurados: " + scalers.Length, this);
    }

    [ContextMenu("Set 1080x720 Windowed")]
    public void Set1080x720Windowed()
    {
        targetWidth = 1080;
        targetHeight = 720;
        screenMode = FullScreenMode.Windowed;
        ApplyResolution();
        ConfigureCanvasScalers();
    }

    [ContextMenu("Set 1080x720 FullScreen Window")]
    public void Set1080x720FullscreenWindow()
    {
        targetWidth = 1080;
        targetHeight = 720;
        screenMode = FullScreenMode.FullScreenWindow;
        ApplyResolution();
        ConfigureCanvasScalers();
    }
}
