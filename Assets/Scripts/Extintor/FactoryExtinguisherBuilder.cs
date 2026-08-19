
using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
[DisallowMultipleComponent]
public class FactoryExtinguisherBuilder : MonoBehaviour
{
    [Header("Construção")]
    public bool buildOnStart = false;
    public bool clearBeforeBuild = true;

    [Header("Tamanho")]
    public float bodyHeight = 0.95f;
    public float bodyRadius = 0.16f;
    public float baseY = 0f;

    [Header("Cores")]
    public Color redColor = new Color(0.85f, 0.02f, 0.02f, 1f);
    public Color darkRedColor = new Color(0.45f, 0.01f, 0.01f, 1f);
    public Color metalColor = new Color(0.55f, 0.55f, 0.55f, 1f);
    public Color blackColor = new Color(0.01f, 0.01f, 0.01f, 1f);
    public Color whiteColor = new Color(0.92f, 0.92f, 0.86f, 1f);

    [Header("Extras")]
    public bool createInteractionCollider = true;
    public bool createHoldPoints = true;
    public bool createWallBracket = true;

    [Header("Debug")]
    public bool showDebugLogs = false;

    private const string GENERATED_ROOT_NAME = "Extinguisher_Visual_Generated";

    void Start()
    {
        if (Application.isPlaying && buildOnStart)
            BuildExtinguisher();
    }

    [ContextMenu("Build Extinguisher")]
    public void BuildExtinguisher()
    {
        if (clearBeforeBuild)
            ClearGenerated();

        Material red = MakeMaterial("Extinguisher_Red_Mat", redColor, 0f);
        Material darkRed = MakeMaterial("Extinguisher_DarkRed_Mat", darkRedColor, 0f);
        Material metal = MakeMaterial("Extinguisher_Metal_Mat", metalColor, 0.45f);
        Material black = MakeMaterial("Extinguisher_Black_Mat", blackColor, 0f);
        Material white = MakeMaterial("Extinguisher_White_Label_Mat", whiteColor, 0f);

        GameObject visualRoot = new GameObject(GENERATED_ROOT_NAME);
        visualRoot.transform.SetParent(transform, false);

        float bodyCenterY = baseY + bodyHeight * 0.5f;

        CreatePrimitive("Body_Red_Cylinder", PrimitiveType.Cylinder, visualRoot.transform,
            new Vector3(0f, bodyCenterY, 0f), Quaternion.identity,
            new Vector3(bodyRadius, bodyHeight * 0.5f, bodyRadius), red);

        CreatePrimitive("Bottom_DarkRed_Cap", PrimitiveType.Cylinder, visualRoot.transform,
            new Vector3(0f, baseY + 0.035f, 0f), Quaternion.identity,
            new Vector3(bodyRadius * 1.04f, 0.035f, bodyRadius * 1.04f), darkRed);

        CreatePrimitive("Top_Metal_Cap", PrimitiveType.Cylinder, visualRoot.transform,
            new Vector3(0f, baseY + bodyHeight + 0.035f, 0f), Quaternion.identity,
            new Vector3(bodyRadius * 0.78f, 0.045f, bodyRadius * 0.78f), metal);

        CreatePrimitive("Neck_Metal", PrimitiveType.Cylinder, visualRoot.transform,
            new Vector3(0f, baseY + bodyHeight + 0.12f, 0f), Quaternion.identity,
            new Vector3(bodyRadius * 0.38f, 0.075f, bodyRadius * 0.38f), metal);

        CreatePrimitive("Handle_Back_Metal", PrimitiveType.Cube, visualRoot.transform,
            new Vector3(0f, baseY + bodyHeight + 0.22f, 0.045f), Quaternion.identity,
            new Vector3(0.36f, 0.045f, 0.06f), metal);

        CreatePrimitive("Handle_Front_Black", PrimitiveType.Cube, visualRoot.transform,
            new Vector3(0f, baseY + bodyHeight + 0.22f, -0.065f), Quaternion.identity,
            new Vector3(0.34f, 0.04f, 0.05f), black);

        CreatePrimitive("Safety_Pin_Metal", PrimitiveType.Cylinder, visualRoot.transform,
            new Vector3(-0.17f, baseY + bodyHeight + 0.13f, -0.025f), Quaternion.Euler(0f, 0f, 90f),
            new Vector3(0.018f, 0.09f, 0.018f), metal);

        CreatePrimitive("White_Label", PrimitiveType.Cube, visualRoot.transform,
            new Vector3(0f, baseY + bodyHeight * 0.55f, -bodyRadius - 0.006f), Quaternion.identity,
            new Vector3(bodyRadius * 1.25f, bodyHeight * 0.32f, 0.012f), white);

        CreatePrimitive("Label_Red_Stripe", PrimitiveType.Cube, visualRoot.transform,
            new Vector3(0f, baseY + bodyHeight * 0.66f, -bodyRadius - 0.014f), Quaternion.identity,
            new Vector3(bodyRadius * 1.05f, bodyHeight * 0.035f, 0.014f), red);

        CreateHose(visualRoot.transform, black, metal);

        if (createWallBracket)
            CreateWallBracket(visualRoot.transform, metal, darkRed);

        if (createInteractionCollider)
            CreateOrUpdateInteractionCollider();

        if (createHoldPoints)
            CreateOrUpdateHoldPoints();

        AddMarkerIfMissing();

        if (showDebugLogs)
            Debug.Log("[FactoryExtinguisherBuilder] Extintor construído: " + name, this);
    }

    void CreateHose(Transform parent, Material black, Material metal)
    {
        GameObject hoseObj = new GameObject("Black_Hose_Line");
        hoseObj.transform.SetParent(parent, false);

        LineRenderer line = hoseObj.AddComponent<LineRenderer>();
        line.useWorldSpace = false;
        line.positionCount = 6;
        line.widthMultiplier = 0.035f;
        line.material = black;
        line.numCapVertices = 6;
        line.numCornerVertices = 6;

        float y = baseY + bodyHeight + 0.1f;
        line.SetPosition(0, new Vector3(bodyRadius * 0.55f, y, 0f));
        line.SetPosition(1, new Vector3(bodyRadius * 1.15f, y - 0.08f, -0.03f));
        line.SetPosition(2, new Vector3(bodyRadius * 1.35f, y - 0.22f, -0.02f));
        line.SetPosition(3, new Vector3(bodyRadius * 1.05f, y - 0.36f, -0.03f));
        line.SetPosition(4, new Vector3(bodyRadius * 0.75f, y - 0.44f, -0.02f));
        line.SetPosition(5, new Vector3(bodyRadius * 0.55f, y - 0.49f, -0.02f));

        CreatePrimitive("Nozzle_Black", PrimitiveType.Cube, parent,
            new Vector3(bodyRadius * 1.42f, y - 0.20f, -0.03f), Quaternion.Euler(0f, 0f, -25f),
            new Vector3(0.16f, 0.035f, 0.045f), black);

        CreatePrimitive("Nozzle_Tip_Metal", PrimitiveType.Cylinder, parent,
            new Vector3(bodyRadius * 1.52f, y - 0.24f, -0.03f), Quaternion.Euler(0f, 0f, 65f),
            new Vector3(0.025f, 0.06f, 0.025f), metal);
    }

    void CreateWallBracket(Transform parent, Material metal, Material darkRed)
    {
        float backZ = bodyRadius + 0.055f;

        CreatePrimitive("Wall_Bracket_Backplate", PrimitiveType.Cube, parent,
            new Vector3(0f, baseY + bodyHeight * 0.48f, backZ), Quaternion.identity,
            new Vector3(0.42f, 0.9f, 0.045f), metal);

        CreatePrimitive("Wall_Bracket_Lower_Hook", PrimitiveType.Cube, parent,
            new Vector3(0f, baseY + 0.16f, bodyRadius * 0.65f), Quaternion.identity,
            new Vector3(0.34f, 0.06f, 0.12f), darkRed);

        CreatePrimitive("Wall_Bracket_Upper_Strap", PrimitiveType.Cube, parent,
            new Vector3(0f, baseY + bodyHeight * 0.62f, -0.005f), Quaternion.identity,
            new Vector3(0.39f, 0.065f, 0.055f), metal);
    }

    void CreateOrUpdateInteractionCollider()
    {
        BoxCollider box = GetComponent<BoxCollider>();
        if (box == null)
            box = gameObject.AddComponent<BoxCollider>();

        box.isTrigger = true;
        box.center = new Vector3(0f, baseY + bodyHeight * 0.55f, 0f);
        box.size = new Vector3(0.55f, bodyHeight + 0.45f, 0.55f);
    }

    void CreateOrUpdateHoldPoints()
    {
        Transform hold = transform.Find("Hold_Point");
        if (hold == null)
        {
            GameObject obj = new GameObject("Hold_Point");
            hold = obj.transform;
            hold.SetParent(transform, false);
        }

        hold.localPosition = new Vector3(0.22f, baseY + bodyHeight * 0.58f, -0.18f);
        hold.localRotation = Quaternion.Euler(0f, 0f, -12f);

        Transform nozzle = transform.Find("Nozzle_Point");
        if (nozzle == null)
        {
            GameObject obj = new GameObject("Nozzle_Point");
            nozzle = obj.transform;
            nozzle.SetParent(transform, false);
        }

        nozzle.localPosition = new Vector3(0.33f, baseY + bodyHeight * 0.86f, -0.16f);
        nozzle.localRotation = Quaternion.Euler(0f, 0f, -20f);
    }

    void AddMarkerIfMissing()
    {
        FactoryExtinguisherMarker marker = GetComponent<FactoryExtinguisherMarker>();
        if (marker == null)
            marker = gameObject.AddComponent<FactoryExtinguisherMarker>();

        marker.holdPoint = transform.Find("Hold_Point");
        marker.nozzlePoint = transform.Find("Nozzle_Point");

        Transform visual = transform.Find(GENERATED_ROOT_NAME);
        if (visual != null)
            marker.visualRoot = visual.gameObject;
    }

    GameObject CreatePrimitive(string objName, PrimitiveType type, Transform parent, Vector3 localPosition, Quaternion localRotation, Vector3 localScale, Material mat)
    {
        GameObject obj = GameObject.CreatePrimitive(type);
        obj.name = objName;
        obj.transform.SetParent(parent, false);
        obj.transform.localPosition = localPosition;
        obj.transform.localRotation = localRotation;
        obj.transform.localScale = localScale;

        Renderer r = obj.GetComponent<Renderer>();
        if (r != null)
            r.sharedMaterial = mat;

        Collider c = obj.GetComponent<Collider>();
        if (c != null)
        {
            if (Application.isPlaying)
                Destroy(c);
            else
                DestroyImmediate(c);
        }

        return obj;
    }

    Material MakeMaterial(string matName, Color color, float metallic)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
            shader = Shader.Find("Standard");

        Material mat = new Material(shader);
        mat.name = matName;

        if (mat.HasProperty("_BaseColor"))
            mat.SetColor("_BaseColor", color);

        if (mat.HasProperty("_Color"))
            mat.SetColor("_Color", color);

        if (mat.HasProperty("_Metallic"))
            mat.SetFloat("_Metallic", metallic);

        if (mat.HasProperty("_Smoothness"))
            mat.SetFloat("_Smoothness", 0.35f);

        return mat;
    }

    [ContextMenu("Clear Generated")]
    public void ClearGenerated()
    {
        Transform visual = transform.Find(GENERATED_ROOT_NAME);
        if (visual != null)
        {
            if (Application.isPlaying)
                Destroy(visual.gameObject);
            else
                DestroyImmediate(visual.gameObject);
        }
    }
}
