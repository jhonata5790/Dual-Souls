
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class FactoryTerminalFunctionManager : MonoBehaviour
{
    [Header("Referências principais")]
    [Tooltip("Raiz da fábrica. Normalmente é o objeto 'Fabrica'.")]
    public Transform factoryRoot;

    [Tooltip("Script do terminal/câmera do PC. Pode ser o objeto PC com FactoryComputerTerminalFocus.")]
    public MonoBehaviour terminalFocus;

    [Tooltip("Canvas do menu do terminal. Normalmente é Factory_PC_Menu_Canvas.")]
    public Canvas terminalCanvas;

    [Tooltip("Script do portão de carga. Pode ser o Loading_Gate com FactoryLoadingGateController_V2.")]
    public MonoBehaviour loadingGateController;

    [Header("Auto configuração")]
    public bool autoFindReferences = true;
    public bool autoBindButtonsOnStart = true;
    public bool autoFindLightsOnStart = true;

    [Header("Estado inicial")]
    public bool mainPowerOn = true;
    public bool factoryLightsOn = true;
    public bool securityModeOn = false;

    [Header("Luzes da fábrica")]
    public bool excludeSunLight = true;
    public bool excludeTerminalAndMonitorLights = true;
    public bool excludeStatusLights = true;
    public float securityModeLightDimMultiplier = 0.35f;

    [Tooltip("Luzes internas controladas pelo botão de luzes. Se vazio, o script procura automaticamente.")]
    public List<Light> factoryLights = new List<Light>();

    [Header("Modo segurança")]
    public bool createSecurityLightsIfMissing = true;
    public bool blinkSecurityLights = true;
    public float securityBlinkSpeed = 3.5f;
    public float securityLightIntensity = 2.8f;
    public float securityLightRange = 9f;
    public Color securityLightColor = new Color(1f, 0.05f, 0.03f, 1f);

    [Tooltip("Luzes vermelhas do modo segurança. Podem ser criadas automaticamente.")]
    public List<Light> securityLights = new List<Light>();

    [Header("Textos de status opcionais")]
    public Text powerStatusText;
    public Text lightsStatusText;
    public Text gateStatusText;
    public Text securityStatusText;
    public Text feedbackText;

    [Header("Debug")]
    public bool showDebugLogs = false;

    private readonly Dictionary<Light, float> originalIntensity = new Dictionary<Light, float>();
    private readonly Dictionary<Light, bool> originalEnabled = new Dictionary<Light, bool>();

    private bool cachedGateOpen = false;

    void Reset()
    {
        terminalCanvas = GetComponent<Canvas>();
        factoryRoot = FindByName("Fabrica");
    }

    void Awake()
    {
        if (autoFindReferences)
            AutoFindReferences();

        if (autoFindLightsOnStart)
            RegisterFactoryLights();

        if (createSecurityLightsIfMissing)
            CreateSecurityLightsIfNeeded();

        ApplyAllStatesInstant();
        UpdateStatusTexts();
    }

    void Start()
    {
        if (autoBindButtonsOnStart)
            AutoBindButtons();
    }

    void Update()
    {
        if (securityModeOn && blinkSecurityLights)
            AnimateSecurityLights();
    }

    [ContextMenu("Auto Find References")]
    public void AutoFindReferences()
    {
        if (factoryRoot == null)
            factoryRoot = FindByName("Fabrica");

        if (terminalCanvas == null)
            terminalCanvas = GetComponent<Canvas>();

        if (terminalCanvas == null)
            terminalCanvas = GetComponentInChildren<Canvas>(true);

        if (terminalCanvas == null && factoryRoot != null)
        {
            Canvas[] canvases = factoryRoot.GetComponentsInChildren<Canvas>(true);
            foreach (Canvas c in canvases)
            {
                if (c.name.ToLowerInvariant().Contains("pc") || c.name.ToLowerInvariant().Contains("terminal"))
                {
                    terminalCanvas = c;
                    break;
                }
            }
        }

        if (terminalFocus == null)
        {
            MonoBehaviour[] allBehaviours = FindObjectsOfType<MonoBehaviour>(true);
            foreach (MonoBehaviour mb in allBehaviours)
            {
                if (mb == null) continue;

                string typeName = mb.GetType().Name.ToLowerInvariant();
                string objName = mb.name.ToLowerInvariant();

                if (typeName.Contains("terminalfocus") || typeName.Contains("computerterminal"))
                {
                    terminalFocus = mb;
                    break;
                }

                if (objName.Contains("pc") && typeName.Contains("terminal"))
                {
                    terminalFocus = mb;
                    break;
                }
            }
        }

        if (loadingGateController == null)
        {
            MonoBehaviour[] allBehaviours = FindObjectsOfType<MonoBehaviour>(true);
            foreach (MonoBehaviour mb in allBehaviours)
            {
                if (mb == null) continue;

                string typeName = mb.GetType().Name.ToLowerInvariant();
                string objName = mb.name.ToLowerInvariant();

                if (typeName.Contains("loadinggate") || objName.Contains("loading_gate"))
                {
                    loadingGateController = mb;
                    break;
                }
            }
        }

        AutoFindStatusTexts();
    }

    [ContextMenu("Register Factory Lights")]
    public void RegisterFactoryLights()
    {
        factoryLights.Clear();
        originalIntensity.Clear();
        originalEnabled.Clear();

        if (factoryRoot == null)
            factoryRoot = FindByName("Fabrica");

        Light[] lights = factoryRoot != null
            ? factoryRoot.GetComponentsInChildren<Light>(true)
            : FindObjectsOfType<Light>(true);

        foreach (Light light in lights)
        {
            if (light == null)
                continue;

            if (ShouldExcludeLight(light))
                continue;

            if (!factoryLights.Contains(light))
                factoryLights.Add(light);

            if (!originalIntensity.ContainsKey(light))
                originalIntensity.Add(light, light.intensity);

            if (!originalEnabled.ContainsKey(light))
                originalEnabled.Add(light, light.enabled);
        }

        if (showDebugLogs)
            Debug.Log("[FactoryTerminalFunctionManager] Luzes registradas: " + factoryLights.Count, this);
    }

    bool ShouldExcludeLight(Light light)
    {
        string n = light.name.ToLowerInvariant();

        if (n.Contains("security_mode"))
            return true;

        if (excludeSunLight && n.Contains("sun"))
            return true;

        if (excludeTerminalAndMonitorLights)
        {
            if (n.Contains("terminal") || n.Contains("pc") || n.Contains("monitor") || n.Contains("screen"))
                return true;
        }

        if (excludeStatusLights)
        {
            if (n.Contains("status") || n.Contains("exit") || n.Contains("sign"))
                return true;
        }

        return false;
    }

    [ContextMenu("Create Security Lights If Needed")]
    public void CreateSecurityLightsIfNeeded()
    {
        securityLights.RemoveAll(l => l == null);

        if (securityLights.Count > 0)
            return;

        if (factoryRoot == null)
            factoryRoot = FindByName("Fabrica");

        Transform parent = factoryRoot != null ? factoryRoot : transform;

        Vector3[] localPositions =
        {
            new Vector3(-12f, -16f, 3.2f),
            new Vector3(12f, -16f, 3.2f),
            new Vector3(-12f, 16f, 3.2f),
            new Vector3(12f, 16f, 3.2f),
            new Vector3(0f, 14f, 6.2f)
        };

        for (int i = 0; i < localPositions.Length; i++)
        {
            GameObject obj = new GameObject("Security_Mode_Red_Light_" + (i + 1));
            obj.transform.SetParent(parent, false);
            obj.transform.localPosition = localPositions[i];

            Light light = obj.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = securityLightColor;
            light.intensity = securityLightIntensity;
            light.range = securityLightRange;
            light.shadows = LightShadows.None;
            light.enabled = false;

            securityLights.Add(light);
        }
    }

    [ContextMenu("Auto Bind Terminal Buttons")]
    public void AutoBindButtons()
    {
        if (terminalCanvas == null)
            terminalCanvas = GetComponent<Canvas>();

        if (terminalCanvas == null)
            terminalCanvas = GetComponentInChildren<Canvas>(true);

        if (terminalCanvas == null)
        {
            Debug.LogWarning("[FactoryTerminalFunctionManager] Nenhum Canvas encontrado para vincular botões.", this);
            return;
        }

        Button[] buttons = terminalCanvas.GetComponentsInChildren<Button>(true);

        foreach (Button button in buttons)
        {
            if (button == null)
                continue;

            string key = GetButtonSearchText(button);

            if (ContainsAny(key, "desligar", "shutdown", "sair", "exit", "fechar pc", "close pc"))
            {
                button.onClick.RemoveListener(ShutdownPC);
                button.onClick.AddListener(ShutdownPC);
                RenameButtonIfUseful(button, "DESLIGAR PC");
            }
            else if (ContainsAny(key, "energia", "power", "geral"))
            {
                button.onClick.RemoveListener(ToggleMainPower);
                button.onClick.AddListener(ToggleMainPower);
                RenameButtonIfUseful(button, "ENERGIA GERAL");
            }
            else if (ContainsAny(key, "luz", "luzes", "lights", "iluminacao", "iluminação"))
            {
                button.onClick.RemoveListener(ToggleFactoryLights);
                button.onClick.AddListener(ToggleFactoryLights);
                RenameButtonIfUseful(button, "LUZES INTERNAS");
            }
            else if (ContainsAny(key, "portao", "portão", "gate", "carga"))
            {
                button.onClick.RemoveListener(ToggleLoadingGateFromTerminal);
                button.onClick.AddListener(ToggleLoadingGateFromTerminal);
                RenameButtonIfUseful(button, "PORTÃO DE CARGA");
            }
            else if (ContainsAny(key, "seguranca", "segurança", "security", "emergencia", "emergência"))
            {
                button.onClick.RemoveListener(ToggleSecurityMode);
                button.onClick.AddListener(ToggleSecurityMode);
                RenameButtonIfUseful(button, "MODO SEGURANÇA");
            }
        }

        UpdateStatusTexts();

        if (showDebugLogs)
            Debug.Log("[FactoryTerminalFunctionManager] Botões vinculados automaticamente.", this);
    }

    string GetButtonSearchText(Button button)
    {
        string s = button.name.ToLowerInvariant();

        Text[] texts = button.GetComponentsInChildren<Text>(true);
        foreach (Text text in texts)
        {
            if (text != null)
                s += " " + text.text.ToLowerInvariant();
        }

        return s;
    }

    bool ContainsAny(string text, params string[] words)
    {
        foreach (string w in words)
        {
            if (text.Contains(w.ToLowerInvariant()))
                return true;
        }

        return false;
    }

    void RenameButtonIfUseful(Button button, string label)
    {
        Text[] texts = button.GetComponentsInChildren<Text>(true);
        if (texts.Length > 0 && string.IsNullOrWhiteSpace(texts[0].text))
            texts[0].text = label;
    }

    void AutoFindStatusTexts()
    {
        if (terminalCanvas == null)
            return;

        Text[] texts = terminalCanvas.GetComponentsInChildren<Text>(true);

        foreach (Text t in texts)
        {
            if (t == null)
                continue;

            string n = t.name.ToLowerInvariant();
            string content = t.text.ToLowerInvariant();
            string combined = n + " " + content;

            if (powerStatusText == null && ContainsAny(combined, "energia", "power"))
                powerStatusText = t;
            else if (lightsStatusText == null && ContainsAny(combined, "luz", "luzes", "lights", "iluminacao", "iluminação"))
                lightsStatusText = t;
            else if (gateStatusText == null && ContainsAny(combined, "portao", "portão", "gate", "carga"))
                gateStatusText = t;
            else if (securityStatusText == null && ContainsAny(combined, "seguranca", "segurança", "security", "emergencia", "emergência"))
                securityStatusText = t;
            else if (feedbackText == null && ContainsAny(combined, "feedback", "mensagem", "message", "log"))
                feedbackText = t;
        }
    }

    [ContextMenu("Apply Current States")]
    public void ApplyAllStatesInstant()
    {
        ApplyFactoryLightState();
        ApplySecurityModeState();
        UpdateStatusTexts();
    }

    public void ShutdownPC()
    {
        SetFeedback("Encerrando terminal...");

        bool called = InvokeFirstMethodFound(
            terminalFocus,
            "ShutdownPC",
            "CloseTerminal",
            "Close",
            "ExitTerminal",
            "LeaveTerminal",
            "CloseComputer",
            "MoveBackToPlayer",
            "ReturnToPlayer"
        );

        if (!called)
        {
            if (terminalCanvas != null)
                terminalCanvas.gameObject.SetActive(false);

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        if (showDebugLogs)
            Debug.Log("[FactoryTerminalFunctionManager] ShutdownPC chamado.", this);
    }

    public void ToggleMainPower()
    {
        mainPowerOn = !mainPowerOn;

        if (!mainPowerOn)
        {
            factoryLightsOn = false;
            securityModeOn = false;
            SetFeedback("Energia geral desligada.");
        }
        else
        {
            factoryLightsOn = true;
            SetFeedback("Energia geral ligada.");
        }

        ApplyAllStatesInstant();
    }

    public void ToggleFactoryLights()
    {
        if (!mainPowerOn)
        {
            SetFeedback("Não é possível ligar luzes sem energia geral.");
            UpdateStatusTexts();
            return;
        }

        factoryLightsOn = !factoryLightsOn;
        SetFeedback(factoryLightsOn ? "Luzes internas ligadas." : "Luzes internas desligadas.");

        ApplyFactoryLightState();
        UpdateStatusTexts();
    }

    public void ToggleLoadingGateFromTerminal()
    {
        if (!mainPowerOn)
        {
            SetFeedback("Portão sem energia.");
            UpdateStatusTexts();
            return;
        }

        bool called = InvokeFirstMethodFound(
            loadingGateController,
            "ToggleGate",
            "Toggle",
            "Interact",
            "Use",
            "OpenGate",
            "Open"
        );

        if (called)
        {
            cachedGateOpen = !cachedGateOpen;
            SetFeedback(cachedGateOpen ? "Portão de carga abrindo/aberto." : "Portão de carga fechando/fechado.");
        }
        else
        {
            SetFeedback("Nenhum controlador de portão encontrado.");
        }

        UpdateStatusTexts();
    }

    public void ToggleSecurityMode()
    {
        if (!mainPowerOn)
        {
            SetFeedback("Modo segurança indisponível sem energia.");
            UpdateStatusTexts();
            return;
        }

        securityModeOn = !securityModeOn;
        SetFeedback(securityModeOn ? "Modo segurança ativado." : "Modo segurança desativado.");

        ApplyFactoryLightState();
        ApplySecurityModeState();
        UpdateStatusTexts();
    }

    void ApplyFactoryLightState()
    {
        for (int i = factoryLights.Count - 1; i >= 0; i--)
        {
            Light light = factoryLights[i];

            if (light == null)
            {
                factoryLights.RemoveAt(i);
                continue;
            }

            if (!originalIntensity.ContainsKey(light))
                originalIntensity[light] = light.intensity;

            if (!originalEnabled.ContainsKey(light))
                originalEnabled[light] = light.enabled;

            bool shouldBeOn = mainPowerOn && factoryLightsOn;

            light.enabled = shouldBeOn;

            if (shouldBeOn)
            {
                float baseIntensity = originalIntensity[light];

                if (securityModeOn)
                    light.intensity = baseIntensity * securityModeLightDimMultiplier;
                else
                    light.intensity = baseIntensity;
            }
        }
    }

    void ApplySecurityModeState()
    {
        if (createSecurityLightsIfMissing)
            CreateSecurityLightsIfNeeded();

        foreach (Light light in securityLights)
        {
            if (light == null)
                continue;

            light.enabled = mainPowerOn && securityModeOn;
            light.color = securityLightColor;
            light.range = securityLightRange;
            light.intensity = securityLightIntensity;
        }
    }

    void AnimateSecurityLights()
    {
        if (!mainPowerOn)
            return;

        float pulse = Mathf.Lerp(0.35f, 1f, Mathf.Abs(Mathf.Sin(Time.time * securityBlinkSpeed)));

        foreach (Light light in securityLights)
        {
            if (light == null || !light.enabled)
                continue;

            light.intensity = securityLightIntensity * pulse;
        }
    }

    bool InvokeFirstMethodFound(MonoBehaviour target, params string[] methodNames)
    {
        if (target == null)
            return false;

        Type type = target.GetType();

        foreach (string methodName in methodNames)
        {
            MethodInfo method = type.GetMethod(
                methodName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                null,
                Type.EmptyTypes,
                null
            );

            if (method != null)
            {
                method.Invoke(target, null);
                return true;
            }
        }

        return false;
    }

    void UpdateStatusTexts()
    {
        SetStatusText(powerStatusText, "ENERGIA", mainPowerOn ? "ONLINE" : "OFFLINE");
        SetStatusText(lightsStatusText, "LUZES", factoryLightsOn && mainPowerOn ? "LIGADAS" : "DESLIGADAS");
        SetStatusText(gateStatusText, "PORTÃO", cachedGateOpen ? "ABERTO" : "FECHADO");
        SetStatusText(securityStatusText, "SEGURANÇA", securityModeOn && mainPowerOn ? "ATIVA" : "NORMAL");
    }

    void SetStatusText(Text text, string label, string value)
    {
        if (text == null)
            return;

        text.text = label + ": " + value;
    }

    void SetFeedback(string message)
    {
        if (feedbackText != null)
            feedbackText.text = message;

        if (showDebugLogs)
            Debug.Log("[FactoryTerminalFunctionManager] " + message, this);
    }

    Transform FindByName(string objectName)
    {
        GameObject obj = GameObject.Find(objectName);
        return obj != null ? obj.transform : null;
    }
}
