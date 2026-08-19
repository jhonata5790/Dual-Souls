
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class FactoryTerminalConnector : MonoBehaviour
{
    [Serializable]
    public class TextOutput
    {
        public TMP_Text tmpText;
        public Text uiText;

        public void Set(string value)
        {
            if (tmpText != null)
                tmpText.text = value;

            if (uiText != null)
                uiText.text = value;
        }

        public bool IsAssigned()
        {
            return tmpText != null || uiText != null;
        }
    }

    [Header("Sistema central")]
    public FactoryCentralSystem factorySystem;

    [Header("Textos de status")]
    public TextOutput energyStatusText = new TextOutput();
    public TextOutput cargoGateStatusText = new TextOutput();
    public TextOutput securityStatusText = new TextOutput();
    public TextOutput lightsStatusText = new TextOutput();
    public TextOutput terminalMessageText = new TextOutput();

    [Header("Labels dos botões - opcional")]
    public TextOutput cargoGateButtonLabel = new TextOutput();
    public TextOutput lightsButtonLabel = new TextOutput();
    public TextOutput powerButtonLabel = new TextOutput();
    public TextOutput securityButtonLabel = new TextOutput();

    [Header("Fechar terminal")]
    [Tooltip("Objeto que tem o script de foco do terminal. Normalmente é o PC.")]
    public GameObject terminalFocusObject;

    [Tooltip("Se ligado, tenta fechar o terminal ao apertar o botão Desligar PC.")]
    public bool shutdownButtonClosesTerminal = true;

    [Header("Atualização")]
    public bool autoFindOnStart = true;
    public bool autoBindTextsOnStart = false;
    public float refreshInterval = 0.15f;

    [Header("Debug")]
    public bool showDebugLogs = false;

    private float nextRefreshTime;

    void Reset()
    {
        AutoFindReferences();
    }

    void Awake()
    {
        if (autoFindOnStart)
            AutoFindReferences();

        if (autoBindTextsOnStart)
            AutoBindTextsFromChildren();
    }

    void OnEnable()
    {
        if (factorySystem != null)
            factorySystem.onStatusChanged.AddListener(UpdateTerminalDisplay);

        UpdateTerminalDisplay();
    }

    void OnDisable()
    {
        if (factorySystem != null)
            factorySystem.onStatusChanged.RemoveListener(UpdateTerminalDisplay);
    }

    void Update()
    {
        if (Time.unscaledTime < nextRefreshTime)
            return;

        nextRefreshTime = Time.unscaledTime + Mathf.Max(0.03f, refreshInterval);
        UpdateTerminalDisplay();
    }

    [ContextMenu("Auto Find References")]
    public void AutoFindReferences()
    {
        if (factorySystem == null)
            factorySystem = FindObjectOfType<FactoryCentralSystem>(true);

        if (terminalFocusObject == null)
        {
            MonoBehaviour[] behaviours = FindObjectsOfType<MonoBehaviour>(true);

            foreach (MonoBehaviour b in behaviours)
            {
                if (b == null)
                    continue;

                string typeName = b.GetType().Name.ToLowerInvariant();

                if (typeName.Contains("computerterminalfocus") || typeName.Contains("terminalfocus"))
                {
                    terminalFocusObject = b.gameObject;
                    break;
                }
            }
        }
    }

    [ContextMenu("Auto Bind Texts From Children")]
    public void AutoBindTextsFromChildren()
    {
        TMP_Text[] tmpTexts = GetComponentsInChildren<TMP_Text>(true);
        Text[] uiTexts = GetComponentsInChildren<Text>(true);

        foreach (TMP_Text t in tmpTexts)
        {
            if (t == null)
                continue;

            TryBindTMP(t);
        }

        foreach (Text t in uiTexts)
        {
            if (t == null)
                continue;

            TryBindUIText(t);
        }

        UpdateTerminalDisplay();

        if (showDebugLogs)
            Debug.Log("[FactoryTerminalConnector] Auto bind de textos concluído.", this);
    }

    void TryBindTMP(TMP_Text text)
    {
        string value = Normalize(text.text + " " + text.name);

        if (!energyStatusText.IsAssigned() && value.Contains("ENERGIA"))
            energyStatusText.tmpText = text;
        else if (!cargoGateStatusText.IsAssigned() && (value.Contains("PORTAO") || value.Contains("PORTÃO")))
            cargoGateStatusText.tmpText = text;
        else if (!securityStatusText.IsAssigned() && (value.Contains("SEGURANCA") || value.Contains("SEGURANÇA")))
            securityStatusText.tmpText = text;
        else if (!lightsStatusText.IsAssigned() && (value.Contains("LUZES") || value.Contains("LUZ")))
            lightsStatusText.tmpText = text;
        else if (!terminalMessageText.IsAssigned() && (value.Contains("AGUARDANDO") || value.Contains("COMANDO") || value.Contains("STATUS")))
            terminalMessageText.tmpText = text;
    }

    void TryBindUIText(Text text)
    {
        string value = Normalize(text.text + " " + text.name);

        if (!energyStatusText.IsAssigned() && value.Contains("ENERGIA"))
            energyStatusText.uiText = text;
        else if (!cargoGateStatusText.IsAssigned() && (value.Contains("PORTAO") || value.Contains("PORTÃO")))
            cargoGateStatusText.uiText = text;
        else if (!securityStatusText.IsAssigned() && (value.Contains("SEGURANCA") || value.Contains("SEGURANÇA")))
            securityStatusText.uiText = text;
        else if (!lightsStatusText.IsAssigned() && (value.Contains("LUZES") || value.Contains("LUZ")))
            lightsStatusText.uiText = text;
        else if (!terminalMessageText.IsAssigned() && (value.Contains("AGUARDANDO") || value.Contains("COMANDO") || value.Contains("STATUS")))
            terminalMessageText.uiText = text;
    }

    string Normalize(string input)
    {
        if (string.IsNullOrEmpty(input))
            return "";

        return input.ToUpperInvariant()
            .Replace("Ã", "A")
            .Replace("Á", "A")
            .Replace("À", "A")
            .Replace("Â", "A")
            .Replace("É", "E")
            .Replace("Ê", "E")
            .Replace("Í", "I")
            .Replace("Ó", "O")
            .Replace("Ô", "O")
            .Replace("Õ", "O")
            .Replace("Ú", "U")
            .Replace("Ç", "C");
    }

    [ContextMenu("Update Terminal Display")]
    public void UpdateTerminalDisplay()
    {
        if (factorySystem == null)
            return;

        energyStatusText.Set(factorySystem.GetEnergyStatusText());
        cargoGateStatusText.Set(factorySystem.GetCargoGateStatusText());
        securityStatusText.Set(factorySystem.GetSecurityStatusText());
        lightsStatusText.Set(factorySystem.GetLightsStatusText());
        terminalMessageText.Set(factorySystem.lastSystemMessage);

        UpdateButtonLabels();
    }

    void UpdateButtonLabels()
    {
        if (factorySystem == null)
            return;

        cargoGateButtonLabel.Set(factorySystem.cargoGateOpen ? "FECHAR PORTÃO" : "ABRIR PORTÃO");
        lightsButtonLabel.Set(factorySystem.interiorLightsOn ? "DESLIGAR LUZES" : "LIGAR LUZES");
        powerButtonLabel.Set(factorySystem.mainPowerOn ? "DESLIGAR ENERGIA" : "LIGAR ENERGIA");
        securityButtonLabel.Set(factorySystem.securityModeActive ? "DESATIVAR SEGURANÇA" : "ATIVAR SEGURANÇA");
    }

    public void UI_ToggleCargoGate()
    {
        if (factorySystem == null)
            return;

        factorySystem.ToggleCargoGate();
        UpdateTerminalDisplay();
    }

    public void UI_ToggleInteriorLights()
    {
        if (factorySystem == null)
            return;

        factorySystem.ToggleInteriorLights();
        UpdateTerminalDisplay();
    }

    public void UI_ToggleMainPower()
    {
        if (factorySystem == null)
            return;

        factorySystem.ToggleMainPower();
        UpdateTerminalDisplay();
    }

    public void UI_ToggleSecurityMode()
    {
        if (factorySystem == null)
            return;

        factorySystem.ToggleSecurityMode();
        UpdateTerminalDisplay();
    }

    public void UI_ShutdownPC()
    {
        if (factorySystem != null)
        {
            factorySystem.lastSystemMessage = "Terminal encerrado.";
            factorySystem.onStatusChanged?.Invoke();
        }

        if (shutdownButtonClosesTerminal)
            TryCloseTerminal();
    }

    void TryCloseTerminal()
    {
        if (terminalFocusObject == null)
            return;

        MonoBehaviour[] behaviours = terminalFocusObject.GetComponentsInChildren<MonoBehaviour>(true);

        string[] methodNames =
        {
            "CloseTerminal",
            "Close",
            "ExitTerminal",
            "Exit",
            "CloseComputer",
            "Shutdown",
            "StopFocus",
            "ReleaseFocus"
        };

        foreach (MonoBehaviour behaviour in behaviours)
        {
            if (behaviour == null)
                continue;

            Type type = behaviour.GetType();

            foreach (string methodName in methodNames)
            {
                System.Reflection.MethodInfo method = type.GetMethod(
                    methodName,
                    System.Reflection.BindingFlags.Instance |
                    System.Reflection.BindingFlags.Public |
                    System.Reflection.BindingFlags.NonPublic
                );

                if (method == null)
                    continue;

                if (method.GetParameters().Length == 0)
                {
                    method.Invoke(behaviour, null);

                    if (showDebugLogs)
                        Debug.Log("[FactoryTerminalConnector] Terminal fechado por " + methodName, this);

                    return;
                }
            }
        }

        if (showDebugLogs)
            Debug.LogWarning("[FactoryTerminalConnector] Não encontrou método de fechar terminal.", this);
    }
}
