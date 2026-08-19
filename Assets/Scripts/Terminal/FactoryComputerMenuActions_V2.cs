using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Ações dos botões do Canvas do terminal da fábrica.
/// Coloque este script no Canvas do menu ou deixe o Builder criar automaticamente.
/// </summary>
public class FactoryComputerMenuActions_V2 : MonoBehaviour
{
    [Header("Referências")]
    public FactoryComputerTerminalFocus terminal;
    public GameObject loadingGateObject;

    [Header("Status UI")]
    public Text powerStatusText;
    public Text lightsStatusText;
    public Text gateStatusText;
    public Text securityStatusText;
    public Text messageText;

    [Header("Estados temporários")]
    public bool factoryPowerOn = false;
    public bool internalLightsOn = true;
    public bool loadingGateOpen = false;
    public bool securityModeOn = false;

    [Header("Luzes opcionais")]
    public Light[] internalLights;

    private void Reset()
    {
        AutoAssign();
    }

    private void Awake()
    {
        AutoAssign();
        RefreshUI();
    }

    private void OnEnable()
    {
        RefreshUI();
    }

    [ContextMenu("Auto Assign")]
    public void AutoAssign()
    {
        if (terminal == null)
            terminal = FindObjectOfType<FactoryComputerTerminalFocus>(true);

        if (loadingGateObject == null)
        {
            GameObject gate = GameObject.Find("Loading_Gate");
            if (gate != null)
                loadingGateObject = gate;
        }
    }

    public void ShutdownPC()
    {
        if (terminal == null)
            AutoAssign();

        SetMessage("Encerrando terminal...");

        if (terminal != null)
            terminal.ShutdownPC();
        else
            gameObject.SetActive(false);
    }

    public void ToggleFactoryPower()
    {
        factoryPowerOn = !factoryPowerOn;
        SetMessage(factoryPowerOn ? "Energia geral ligada." : "Energia geral desligada.");
        RefreshUI();
    }

    public void ToggleInternalLights()
    {
        internalLightsOn = !internalLightsOn;

        if (internalLights != null && internalLights.Length > 0)
        {
            foreach (Light l in internalLights)
            {
                if (l != null)
                    l.enabled = internalLightsOn;
            }
        }

        SetMessage(internalLightsOn ? "Luzes internas ligadas." : "Luzes internas desligadas.");
        RefreshUI();
    }

    public void ToggleLoadingGate()
    {
        loadingGateOpen = !loadingGateOpen;

        if (loadingGateObject == null)
            AutoAssign();

        if (loadingGateObject != null)
        {
            // Funciona com o script FactoryLoadingGateController_V2.
            loadingGateObject.SendMessage("ToggleGate", SendMessageOptions.DontRequireReceiver);
        }

        SetMessage(loadingGateOpen ? "Comando enviado: abrir portão." : "Comando enviado: fechar portão.");
        RefreshUI();
    }

    public void ToggleSecurityMode()
    {
        securityModeOn = !securityModeOn;
        SetMessage(securityModeOn ? "Modo segurança ativado." : "Modo segurança desativado.");
        RefreshUI();
    }

    public void RefreshUI()
    {
        SetStatus(powerStatusText, "ENERGIA", factoryPowerOn ? "LIGADA" : "DESLIGADA");
        SetStatus(lightsStatusText, "LUZES", internalLightsOn ? "LIGADAS" : "DESLIGADAS");
        SetStatus(gateStatusText, "PORTÃO", loadingGateOpen ? "ABERTO" : "FECHADO");
        SetStatus(securityStatusText, "SEGURANÇA", securityModeOn ? "ATIVA" : "INATIVA");
    }

    private void SetStatus(Text text, string label, string value)
    {
        if (text != null)
            text.text = label + ": " + value;
    }

    private void SetMessage(string value)
    {
        if (messageText != null)
            messageText.text = value;
    }
}
