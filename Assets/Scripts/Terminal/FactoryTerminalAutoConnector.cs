
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class FactoryTerminalAutoConnector : MonoBehaviour
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

    [Tooltip("Raiz do menu do terminal. Se vazio, usa este objeto.")]
    public Transform terminalRoot;

    [Header("Botões")]
    public Button cargoGateButton;
    public Button lightsButton;
    public Button powerButton;
    public Button securityButton;
    public Button shutdownButton;

    [Header("Textos de status")]
    public TextOutput energyStatusText = new TextOutput();
    public TextOutput cargoGateStatusText = new TextOutput();
    public TextOutput securityStatusText = new TextOutput();
    public TextOutput lightsStatusText = new TextOutput();
    public TextOutput terminalMessageText = new TextOutput();

    [Header("Labels dos botões")]
    public TextOutput cargoGateButtonLabel = new TextOutput();
    public TextOutput lightsButtonLabel = new TextOutput();
    public TextOutput powerButtonLabel = new TextOutput();
    public TextOutput securityButtonLabel = new TextOutput();

    [Header("Fechar terminal")]
    public GameObject terminalFocusObject;
    public bool shutdownButtonClosesTerminal = true;

    [Header("Auto setup")]
    public bool autoFindFactorySystem = true;
    public bool autoFindTerminalFocus = true;
    public bool autoBindButtons = true;
    public bool autoBindTexts = true;

    [Tooltip("Ligado = limpa OnClick antigo dos botões e coloca só os eventos corretos.")]
    public bool clearOldButtonEvents = true;

    [Tooltip("Atualiza os textos mesmo sem clicar, para manter o terminal sincronizado.")]
    public bool refreshDisplayEveryFrame = false;

    public float refreshInterval = 0.15f;

    [Header("Debug")]
    public bool showDebugLogs = true;

    private float nextRefreshTime;

    void Reset()
    {
        terminalRoot = transform;
        AutoSetup();
    }

    void Awake()
    {
        AutoSetup();
    }

    void Start()
    {
        AutoSetup();
        UpdateTerminalDisplay();
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
        if (!refreshDisplayEveryFrame)
            return;

        if (Time.unscaledTime < nextRefreshTime)
            return;

        nextRefreshTime = Time.unscaledTime + Mathf.Max(0.03f, refreshInterval);
        UpdateTerminalDisplay();
    }

    [ContextMenu("AUTO SETUP NOW")]
    public void AutoSetup()
    {
        if (terminalRoot == null)
            terminalRoot = transform;

        if (autoFindFactorySystem && factorySystem == null)
            factorySystem = FindObjectOfType<FactoryCentralSystem>(true);

        if (autoFindTerminalFocus && terminalFocusObject == null)
            FindTerminalFocusObject();

        if (autoBindButtons)
            AutoBindButtons();

        if (autoBindTexts)
            AutoBindTexts();

        WireButtons();

        if (showDebugLogs)
            LogSetupResult();
    }

    void FindTerminalFocusObject()
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
                return;
            }
        }

        GameObject pc = GameObject.Find("PC");
        if (pc != null)
            terminalFocusObject = pc;
    }

    void AutoBindButtons()
    {
        Button[] buttons = terminalRoot.GetComponentsInChildren<Button>(true);

        foreach (Button button in buttons)
        {
            if (button == null)
                continue;

            string text = GetButtonSearchText(button);

            if (cargoGateButton == null && (text.Contains("PORTAO") || text.Contains("PORTÃO") || text.Contains("CARGA") || text.Contains("GATE")))
            {
                cargoGateButton = button;
                BindButtonLabel(button, cargoGateButtonLabel);
                continue;
            }

            if (lightsButton == null && (text.Contains("LUZES") || text.Contains("LUZ") || text.Contains("LIGHT")))
            {
                lightsButton = button;
                BindButtonLabel(button, lightsButtonLabel);
                continue;
            }

            if (powerButton == null && (text.Contains("ENERGIA") || text.Contains("POWER")))
            {
                powerButton = button;
                BindButtonLabel(button, powerButtonLabel);
                continue;
            }

            if (securityButton == null && (text.Contains("SEGURANCA") || text.Contains("SEGURANÇA") || text.Contains("SECURITY")))
            {
                securityButton = button;
                BindButtonLabel(button, securityButtonLabel);
                continue;
            }

            if (shutdownButton == null && (text.Contains("DESLIGAR") || text.Contains("SAIR") || text.Contains("SHUTDOWN") || text.Contains("EXIT")))
            {
                shutdownButton = button;
                continue;
            }
        }
    }

    string GetButtonSearchText(Button button)
    {
        string value = button.name;

        TMP_Text tmp = button.GetComponentInChildren<TMP_Text>(true);
        if (tmp != null)
            value += " " + tmp.text + " " + tmp.name;

        Text ui = button.GetComponentInChildren<Text>(true);
        if (ui != null)
            value += " " + ui.text + " " + ui.name;

        return Normalize(value);
    }

    void BindButtonLabel(Button button, TextOutput output)
    {
        if (button == null || output == null || output.IsAssigned())
            return;

        TMP_Text tmp = button.GetComponentInChildren<TMP_Text>(true);
        if (tmp != null)
        {
            output.tmpText = tmp;
            return;
        }

        Text ui = button.GetComponentInChildren<Text>(true);
        if (ui != null)
            output.uiText = ui;
    }

    void AutoBindTexts()
    {
        TMP_Text[] tmpTexts = terminalRoot.GetComponentsInChildren<TMP_Text>(true);

        foreach (TMP_Text t in tmpTexts)
        {
            if (t == null)
                continue;

            TryBindTMPStatus(t);
        }

        Text[] uiTexts = terminalRoot.GetComponentsInChildren<Text>(true);

        foreach (Text t in uiTexts)
        {
            if (t == null)
                continue;

            TryBindUIStatus(t);
        }
    }

    void TryBindTMPStatus(TMP_Text text)
    {
        string value = Normalize(text.text + " " + text.name);

        if (IsButtonLabel(text.transform))
            return;

        if (!energyStatusText.IsAssigned() && value.Contains("ENERGIA"))
            energyStatusText.tmpText = text;
        else if (!cargoGateStatusText.IsAssigned() && (value.Contains("PORTAO") || value.Contains("PORTÃO")))
            cargoGateStatusText.tmpText = text;
        else if (!securityStatusText.IsAssigned() && (value.Contains("SEGURANCA") || value.Contains("SEGURANÇA")))
            securityStatusText.tmpText = text;
        else if (!lightsStatusText.IsAssigned() && (value.Contains("LUZES") || value.Contains("LUZ")))
            lightsStatusText.tmpText = text;
        else if (!terminalMessageText.IsAssigned() && (value.Contains("AGUARDANDO") || value.Contains("COMANDO") || value.Contains("MENSAGEM") || value.Contains("MESSAGE")))
            terminalMessageText.tmpText = text;
    }

    void TryBindUIStatus(Text text)
    {
        string value = Normalize(text.text + " " + text.name);

        if (IsButtonLabel(text.transform))
            return;

        if (!energyStatusText.IsAssigned() && value.Contains("ENERGIA"))
            energyStatusText.uiText = text;
        else if (!cargoGateStatusText.IsAssigned() && (value.Contains("PORTAO") || value.Contains("PORTÃO")))
            cargoGateStatusText.uiText = text;
        else if (!securityStatusText.IsAssigned() && (value.Contains("SEGURANCA") || value.Contains("SEGURANÇA")))
            securityStatusText.uiText = text;
        else if (!lightsStatusText.IsAssigned() && (value.Contains("LUZES") || value.Contains("LUZ")))
            lightsStatusText.uiText = text;
        else if (!terminalMessageText.IsAssigned() && (value.Contains("AGUARDANDO") || value.Contains("COMANDO") || value.Contains("MENSAGEM") || value.Contains("MESSAGE")))
            terminalMessageText.uiText = text;
    }

    bool IsButtonLabel(Transform t)
    {
        if (t == null)
            return false;

        return t.GetComponentInParent<Button>() != null;
    }

    void WireButtons()
    {
        WireButton(cargoGateButton, UI_ToggleCargoGate);
        WireButton(lightsButton, UI_ToggleInteriorLights);
        WireButton(powerButton, UI_ToggleMainPower);
        WireButton(securityButton, UI_ToggleSecurityMode);
        WireButton(shutdownButton, UI_ShutdownPC);
    }

    void WireButton(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button == null)
            return;

        if (clearOldButtonEvents)
            button.onClick.RemoveAllListeners();

        button.onClick.AddListener(action);
        button.interactable = true;
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

        cargoGateButtonLabel.Set(factorySystem.cargoGateOpen ? "FECHAR PORTÃO" : "ABRIR PORTÃO");
        lightsButtonLabel.Set(factorySystem.interiorLightsOn ? "DESLIGAR LUZES" : "LIGAR LUZES");
        powerButtonLabel.Set(factorySystem.mainPowerOn ? "DESLIGAR ENERGIA" : "LIGAR ENERGIA");
        securityButtonLabel.Set(factorySystem.securityModeActive ? "DESATIVAR SEGURANÇA" : "ATIVAR SEGURANÇA");
    }

    public void UI_ToggleCargoGate()
    {
        if (!CheckSystem()) return;

        if (showDebugLogs)
            Debug.Log("[FactoryTerminalAutoConnector] Clique: Portão de carga", this);

        factorySystem.ToggleCargoGate();
        UpdateTerminalDisplay();
    }

    public void UI_ToggleInteriorLights()
    {
        if (!CheckSystem()) return;

        if (showDebugLogs)
            Debug.Log("[FactoryTerminalAutoConnector] Clique: Luzes internas", this);

        factorySystem.ToggleInteriorLights();
        UpdateTerminalDisplay();
    }

    public void UI_ToggleMainPower()
    {
        if (!CheckSystem()) return;

        if (showDebugLogs)
            Debug.Log("[FactoryTerminalAutoConnector] Clique: Energia geral", this);

        factorySystem.ToggleMainPower();
        UpdateTerminalDisplay();
    }

    public void UI_ToggleSecurityMode()
    {
        if (!CheckSystem()) return;

        if (showDebugLogs)
            Debug.Log("[FactoryTerminalAutoConnector] Clique: Modo segurança", this);

        factorySystem.ToggleSecurityMode();
        UpdateTerminalDisplay();
    }

    public void UI_ShutdownPC()
    {
        if (showDebugLogs)
            Debug.Log("[FactoryTerminalAutoConnector] Clique: Desligar PC", this);

        if (factorySystem != null)
        {
            factorySystem.lastSystemMessage = "Terminal encerrado.";
            factorySystem.onStatusChanged?.Invoke();
        }

        UpdateTerminalDisplay();

        if (shutdownButtonClosesTerminal)
            TryCloseTerminal();
    }

    bool CheckSystem()
    {
        if (factorySystem != null)
            return true;

        factorySystem = FindObjectOfType<FactoryCentralSystem>(true);

        if (factorySystem != null)
            return true;

        Debug.LogError("[FactoryTerminalAutoConnector] Factory System está vazio. Arraste o Factory_System_Manager no campo Factory System.", this);
        return false;
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
                    return;
                }
            }
        }
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

    void LogSetupResult()
    {
        Debug.Log(
            "[FactoryTerminalAutoConnector] Setup:\n" +
            "FactorySystem: " + (factorySystem != null ? factorySystem.name : "NULO") + "\n" +
            "TerminalRoot: " + (terminalRoot != null ? terminalRoot.name : "NULO") + "\n" +
            "Botao Portao: " + (cargoGateButton != null ? cargoGateButton.name : "NULO") + "\n" +
            "Botao Luzes: " + (lightsButton != null ? lightsButton.name : "NULO") + "\n" +
            "Botao Energia: " + (powerButton != null ? powerButton.name : "NULO") + "\n" +
            "Botao Seguranca: " + (securityButton != null ? securityButton.name : "NULO") + "\n" +
            "Botao Desligar: " + (shutdownButton != null ? shutdownButton.name : "NULO"),
            this
        );
    }
}
