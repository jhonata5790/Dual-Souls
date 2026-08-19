using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// Cria do zero um Canvas de terminal para o PC da sala de controle.
/// Use pelo menu de contexto: Build/Rebuild Terminal Canvas.
/// O Canvas nasce desativado, e o FactoryComputerTerminalFocus ativa ele depois da animação da câmera.
/// </summary>
[DisallowMultipleComponent]
public class FactoryComputerTerminalCanvasBuilder : MonoBehaviour
{
    [Header("Referências")]
    public FactoryComputerTerminalFocus terminalFocus;
    public Transform canvasParent;

    [Header("Configuração do Canvas")]
    public string canvasName = "Factory_PC_Menu_Canvas";
    public int sortingOrder = 20000;
    public Vector2 referenceResolution = new Vector2(1920, 1080);

    [Header("Visual")]
    public Color backdropColor = new Color(0.01f, 0.035f, 0.045f, 0.72f);
    public Color mainPanelColor = new Color(0.02f, 0.08f, 0.105f, 0.94f);
    public Color headerColor = new Color(0.02f, 0.18f, 0.26f, 1f);
    public Color buttonColor = new Color(0.05f, 0.22f, 0.30f, 1f);
    public Color buttonHoverColor = new Color(0.09f, 0.34f, 0.46f, 1f);
    public Color shutdownButtonColor = new Color(0.36f, 0.08f, 0.07f, 1f);
    public Color textColor = new Color(0.86f, 0.96f, 1f, 1f);
    public Color accentColor = new Color(0.0f, 0.75f, 1f, 1f);

    [Header("Textos")]
    public string terminalTitle = "TERMINAL DA FÁBRICA";
    public string subtitle = "Sistema central de supervisão";
    public string footerText = "Clique em DESLIGAR PC para sair do terminal.";

    [Header("Comportamento")]
    public bool assignCanvasToTerminal = true;
    public bool keepCanvasDisabledAfterBuild = true;
    public bool createEventSystemIfMissing = true;

    private Font cachedFont;

    private void Reset()
    {
        AutoAssign();
    }

    [ContextMenu("Auto Assign")]
    public void AutoAssign()
    {
        if (terminalFocus == null)
            terminalFocus = FindObjectOfType<FactoryComputerTerminalFocus>(true);

        if (canvasParent == null)
            canvasParent = transform;
    }

    [ContextMenu("Build/Rebuild Terminal Canvas")]
    public void BuildOrRebuildTerminalCanvas()
    {
        AutoAssign();

        if (createEventSystemIfMissing)
            EnsureEventSystemExists();

        Transform parent = canvasParent != null ? canvasParent : transform;
        Transform old = parent.Find(canvasName);
        if (old != null)
            SafeDestroy(old.gameObject);

        GameObject canvasGO = CreateUIObject(canvasName, parent);
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortingOrder;

        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = referenceResolution;
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        canvasGO.AddComponent<GraphicRaycaster>();

        RectTransform canvasRT = canvasGO.GetComponent<RectTransform>();
        StretchFull(canvasRT);

        FactoryComputerMenuActions_V2 actions = canvasGO.AddComponent<FactoryComputerMenuActions_V2>();
        actions.terminal = terminalFocus;

        BuildTerminalLayout(canvasGO.transform, actions);

        if (assignCanvasToTerminal && terminalFocus != null)
            terminalFocus.terminalMenuCanvas = canvasGO;

        if (keepCanvasDisabledAfterBuild)
            canvasGO.SetActive(false);

        Debug.Log("[FactoryComputerTerminalCanvasBuilder] Canvas do terminal criado: " + canvasName, this);
    }

    [ContextMenu("Delete Terminal Canvas")]
    public void DeleteTerminalCanvas()
    {
        Transform parent = canvasParent != null ? canvasParent : transform;
        Transform old = parent.Find(canvasName);
        if (old != null)
            SafeDestroy(old.gameObject);
    }

    private void BuildTerminalLayout(Transform root, FactoryComputerMenuActions_V2 actions)
    {
        GameObject backdrop = CreatePanel("Terminal_Backdrop", root, backdropColor);
        StretchFull(backdrop.GetComponent<RectTransform>());

        GameObject main = CreatePanel("Terminal_Main_Panel", root, mainPanelColor);
        RectTransform mainRT = main.GetComponent<RectTransform>();
        SetCentered(mainRT, new Vector2(980f, 610f), Vector2.zero);

        GameObject header = CreatePanel("Terminal_Header", main.transform, headerColor);
        RectTransform headerRT = header.GetComponent<RectTransform>();
        SetTopStretch(headerRT, 0f, 92f, new Vector2(0f, -46f));

        Text title = CreateText("Terminal_Title", header.transform, terminalTitle, 26, FontStyle.Bold, TextAnchor.MiddleCenter);
        SetCentered(title.rectTransform, new Vector2(700f, 34f), new Vector2(0f, 16f));

        Text sub = CreateText("Terminal_Subtitle", header.transform, subtitle, 16, FontStyle.Normal, TextAnchor.MiddleCenter);
        sub.color = new Color(textColor.r, textColor.g, textColor.b, 0.75f);
        SetCentered(sub.rectTransform, new Vector2(700f, 26f), new Vector2(0f, -18f));

        GameObject statusPanel = CreatePanel("Terminal_Status_Panel", main.transform, new Color(0f, 0f, 0f, 0.16f));
        RectTransform statusRT = statusPanel.GetComponent<RectTransform>();
        SetRect(statusRT, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -145f), new Vector2(-90f, 90f));

        Text statusTitle = CreateText("Status_Title", statusPanel.transform, "STATUS DO SISTEMA", 18, FontStyle.Bold, TextAnchor.MiddleLeft);
        statusTitle.color = accentColor;
        SetAnchored(statusTitle.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(20f, -18f), new Vector2(-40f, 28f));

        actions.powerStatusText = CreateText("Status_Power", statusPanel.transform, "ENERGIA: DESLIGADA", 17, FontStyle.Normal, TextAnchor.MiddleLeft);
        actions.lightsStatusText = CreateText("Status_Lights", statusPanel.transform, "LUZES: LIGADAS", 17, FontStyle.Normal, TextAnchor.MiddleLeft);
        actions.gateStatusText = CreateText("Status_Gate", statusPanel.transform, "PORTÃO: FECHADO", 17, FontStyle.Normal, TextAnchor.MiddleLeft);
        actions.securityStatusText = CreateText("Status_Security", statusPanel.transform, "SEGURANÇA: INATIVA", 17, FontStyle.Normal, TextAnchor.MiddleLeft);

        SetAnchored(actions.powerStatusText.rectTransform, new Vector2(0f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, 1f), new Vector2(20f, -52f), new Vector2(-30f, 26f));
        SetAnchored(actions.lightsStatusText.rectTransform, new Vector2(0.5f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(10f, -52f), new Vector2(-30f, 26f));
        SetAnchored(actions.gateStatusText.rectTransform, new Vector2(0f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, 1f), new Vector2(20f, -78f), new Vector2(-30f, 26f));
        SetAnchored(actions.securityStatusText.rectTransform, new Vector2(0.5f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(10f, -78f), new Vector2(-30f, 26f));

        GameObject commandPanel = CreatePanel("Terminal_Command_Panel", main.transform, new Color(0f, 0f, 0f, 0.10f));
        RectTransform commandRT = commandPanel.GetComponent<RectTransform>();
        SetRect(commandRT, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, -55f), new Vector2(-90f, -260f));

        Text commandTitle = CreateText("Commands_Title", commandPanel.transform, "COMANDOS", 18, FontStyle.Bold, TextAnchor.MiddleLeft);
        commandTitle.color = accentColor;
        SetAnchored(commandTitle.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(20f, -22f), new Vector2(-40f, 32f));

        Button powerButton = CreateButton("Button_Toggle_Power", commandPanel.transform, "ENERGIA GERAL", new Vector2(-245f, 45f), new Vector2(230f, 56f), buttonColor);
        powerButton.onClick.AddListener(actions.ToggleFactoryPower);

        Button lightsButton = CreateButton("Button_Toggle_Lights", commandPanel.transform, "LUZES INTERNAS", new Vector2(0f, 45f), new Vector2(230f, 56f), buttonColor);
        lightsButton.onClick.AddListener(actions.ToggleInternalLights);

        Button gateButton = CreateButton("Button_Toggle_Gate", commandPanel.transform, "PORTÃO DE CARGA", new Vector2(245f, 45f), new Vector2(230f, 56f), buttonColor);
        gateButton.onClick.AddListener(actions.ToggleLoadingGate);

        Button securityButton = CreateButton("Button_Toggle_Security", commandPanel.transform, "MODO SEGURANÇA", new Vector2(-125f, -35f), new Vector2(260f, 56f), buttonColor);
        securityButton.onClick.AddListener(actions.ToggleSecurityMode);

        Button shutdownButton = CreateButton("Button_Shutdown_PC", commandPanel.transform, "DESLIGAR PC", new Vector2(170f, -35f), new Vector2(260f, 56f), shutdownButtonColor);
        shutdownButton.onClick.AddListener(actions.ShutdownPC);

        Text message = CreateText("Terminal_Message", main.transform, "Terminal aguardando comando.", 16, FontStyle.Normal, TextAnchor.MiddleLeft);
        message.color = new Color(textColor.r, textColor.g, textColor.b, 0.82f);
        SetAnchored(message.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 0f), new Vector2(45f, 55f), new Vector2(-90f, 34f));
        actions.messageText = message;

        Text footer = CreateText("Terminal_Footer", main.transform, footerText, 14, FontStyle.Normal, TextAnchor.MiddleCenter);
        footer.color = new Color(textColor.r, textColor.g, textColor.b, 0.55f);
        SetAnchored(footer.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 22f), new Vector2(-80f, 28f));

        actions.RefreshUI();
    }

    private GameObject CreatePanel(string name, Transform parent, Color color)
    {
        GameObject go = CreateUIObject(name, parent);
        Image img = go.AddComponent<Image>();
        img.color = color;
        return go;
    }

    private Button CreateButton(string name, Transform parent, string label, Vector2 anchoredPosition, Vector2 size, Color normalColor)
    {
        GameObject go = CreateUIObject(name, parent);
        RectTransform rt = go.GetComponent<RectTransform>();
        SetCentered(rt, size, anchoredPosition);

        Image img = go.AddComponent<Image>();
        img.color = normalColor;

        Button button = go.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = normalColor;
        colors.highlightedColor = buttonHoverColor;
        colors.pressedColor = new Color(normalColor.r * 0.7f, normalColor.g * 0.7f, normalColor.b * 0.7f, normalColor.a);
        colors.selectedColor = buttonHoverColor;
        colors.disabledColor = new Color(0.15f, 0.15f, 0.15f, 0.7f);
        button.colors = colors;

        Text text = CreateText("Label", go.transform, label, 16, FontStyle.Bold, TextAnchor.MiddleCenter);
        StretchFull(text.rectTransform);
        text.raycastTarget = false;

        return button;
    }

    private Text CreateText(string name, Transform parent, string value, int size, FontStyle style, TextAnchor anchor)
    {
        GameObject go = CreateUIObject(name, parent);
        Text text = go.AddComponent<Text>();
        text.text = value;
        text.font = GetFont();
        text.fontSize = size;
        text.fontStyle = style;
        text.alignment = anchor;
        text.color = textColor;
        text.raycastTarget = false;
        return text;
    }

    private Font GetFont()
    {
        if (cachedFont != null)
            return cachedFont;

        try
        {
            cachedFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }
        catch
        {
            cachedFont = null;
        }

        if (cachedFont == null)
        {
            try
            {
                cachedFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
            }
            catch
            {
                cachedFont = null;
            }
        }

        return cachedFont;
    }

    private GameObject CreateUIObject(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go;
    }

    private void EnsureEventSystemExists()
    {
        EventSystem existing = FindObjectOfType<EventSystem>(true);
        if (existing != null)
            return;

        GameObject go = new GameObject("EventSystem");
        go.AddComponent<EventSystem>();
        go.AddComponent<StandaloneInputModule>();
    }

    private void StretchFull(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = Vector2.zero;
    }

    private void SetCentered(RectTransform rt, Vector2 size, Vector2 anchoredPosition)
    {
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchoredPosition;
        rt.sizeDelta = size;
    }

    private void SetTopStretch(RectTransform rt, float horizontalPadding, float height, Vector2 anchoredPosition)
    {
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = anchoredPosition;
        rt.sizeDelta = new Vector2(-horizontalPadding * 2f, height);
    }

    private void SetAnchored(RectTransform rt, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition, Vector2 sizeDelta)
    {
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = pivot;
        rt.anchoredPosition = anchoredPosition;
        rt.sizeDelta = sizeDelta;
    }

    private void SetRect(RectTransform rt, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition, Vector2 sizeDelta)
    {
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = pivot;
        rt.anchoredPosition = anchoredPosition;
        rt.sizeDelta = sizeDelta;
    }

    private void SafeDestroy(GameObject go)
    {
        if (go == null)
            return;

        if (Application.isPlaying)
            Destroy(go);
        else
            DestroyImmediate(go);
    }
}
