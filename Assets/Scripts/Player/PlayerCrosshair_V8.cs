
using UnityEngine;
using UnityEngine.UI;

public class PlayerCrosshair_V8 : MonoBehaviour
{
    [Header("Referências")]
    public FactoryInteractionSystem_V8 interactionSystem;

    [Header("Visual")]
    public Canvas canvas;
    public Image centerDot;
    public Image horizontalLine;
    public Image verticalLine;

    [Header("Tamanho")]
    public float dotSize = 5f;
    public float lineLength = 18f;
    public float lineThickness = 2f;
    public float gap = 8f;

    [Header("Cores")]
    public Color normalColor = Color.white;
    public Color interactColor = new Color(1f, 0.86f, 0.15f, 1f);

    void Awake()
    {
        if (interactionSystem == null)
            interactionSystem = GetComponent<FactoryInteractionSystem_V8>();

        if (interactionSystem == null)
            interactionSystem = GetComponentInParent<FactoryInteractionSystem_V8>();

        if (interactionSystem == null)
            interactionSystem = FindFirstObjectByType<FactoryInteractionSystem_V8>();

        BuildCrosshairIfNeeded();
    }

    void Update()
    {
        Color targetColor = interactionSystem != null && interactionSystem.HasTarget
            ? interactColor
            : normalColor;

        ApplyColor(targetColor);
    }

    void BuildCrosshairIfNeeded()
    {
        if (canvas != null && centerDot != null)
            return;

        GameObject canvasObj = new GameObject("Player_Crosshair_Canvas_V8");
        canvasObj.transform.SetParent(transform, false);

        canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 9999;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        canvasObj.AddComponent<GraphicRaycaster>();

        centerDot = CreateImage("Crosshair_Dot", canvasObj.transform);
        RectTransform dotRect = centerDot.rectTransform;
        dotRect.anchorMin = new Vector2(0.5f, 0.5f);
        dotRect.anchorMax = new Vector2(0.5f, 0.5f);
        dotRect.pivot = new Vector2(0.5f, 0.5f);
        dotRect.anchoredPosition = Vector2.zero;
        dotRect.sizeDelta = new Vector2(dotSize, dotSize);

        horizontalLine = CreateImage("Crosshair_Horizontal", canvasObj.transform);
        RectTransform hRect = horizontalLine.rectTransform;
        hRect.anchorMin = new Vector2(0.5f, 0.5f);
        hRect.anchorMax = new Vector2(0.5f, 0.5f);
        hRect.pivot = new Vector2(0.5f, 0.5f);
        hRect.anchoredPosition = Vector2.zero;
        hRect.sizeDelta = new Vector2(lineLength, lineThickness);

        verticalLine = CreateImage("Crosshair_Vertical", canvasObj.transform);
        RectTransform vRect = verticalLine.rectTransform;
        vRect.anchorMin = new Vector2(0.5f, 0.5f);
        vRect.anchorMax = new Vector2(0.5f, 0.5f);
        vRect.pivot = new Vector2(0.5f, 0.5f);
        vRect.anchoredPosition = Vector2.zero;
        vRect.sizeDelta = new Vector2(lineThickness, lineLength);

        ApplyColor(normalColor);
    }

    Image CreateImage(string name, Transform parent)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);

        Image image = obj.AddComponent<Image>();
        image.raycastTarget = false;
        image.color = normalColor;

        return image;
    }

    void ApplyColor(Color color)
    {
        if (centerDot != null)
            centerDot.color = color;

        if (horizontalLine != null)
            horizontalLine.color = color;

        if (verticalLine != null)
            verticalLine.color = color;
    }

    public void SetVisible(bool visible)
    {
        if (canvas != null)
            canvas.gameObject.SetActive(visible);
    }
}
