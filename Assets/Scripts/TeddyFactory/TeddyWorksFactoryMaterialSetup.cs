
using UnityEngine;

[ExecuteAlways]
[DisallowMultipleComponent]
public class TeddyWorksFactoryMaterialSetup : MonoBehaviour
{
    [Header("Raiz da fábrica")]
    public Transform factoryRoot;

    [Header("Aplicação")]
    public bool applyOnStart = false;
    public bool includeInactiveObjects = true;
    public bool overwriteExistingMaterials = true;

    [Header("Cores principais")]
    public Color wallCyan = new Color(0.42f, 0.86f, 0.92f, 1f);
    public Color floorDarkConcrete = new Color(0.18f, 0.20f, 0.21f, 1f);
    public Color roofDarkMetal = new Color(0.08f, 0.09f, 0.10f, 1f);
    public Color darkMetal = new Color(0.05f, 0.055f, 0.06f, 1f);
    public Color lightMetal = new Color(0.55f, 0.58f, 0.60f, 1f);
    public Color safetyYellow = new Color(1.0f, 0.72f, 0.05f, 1f);
    public Color rubberBlack = new Color(0.01f, 0.01f, 0.012f, 1f);
    public Color glassBlue = new Color(0.08f, 0.20f, 0.32f, 0.45f);

    [Header("Cores dos setores / energias")]
    public Color electricBlue = new Color(0.08f, 0.45f, 1.0f, 1f);
    public Color chemicalGreen = new Color(0.12f, 0.85f, 0.28f, 1f);
    public Color pneumaticBlue = new Color(0.14f, 0.72f, 1.0f, 1f);
    public Color hydraulicYellow = new Color(1.0f, 0.78f, 0.04f, 1f);
    public Color thermalOrange = new Color(1.0f, 0.32f, 0.04f, 1f);
    public Color mechanicalRed = new Color(0.9f, 0.05f, 0.05f, 1f);

    [Header("Cores TeddyWorks")]
    public Color teddyBrown = new Color(0.48f, 0.28f, 0.12f, 1f);
    public Color fabricRed = new Color(0.85f, 0.04f, 0.08f, 1f);
    public Color fabricBlue = new Color(0.08f, 0.32f, 0.9f, 1f);
    public Color fabricPink = new Color(1.0f, 0.35f, 0.72f, 1f);
    public Color fabricYellow = new Color(1.0f, 0.86f, 0.15f, 1f);
    public Color cardboard = new Color(0.55f, 0.34f, 0.16f, 1f);
    public Color cottonWhite = new Color(0.92f, 0.92f, 0.86f, 1f);
    public Color monitorBlue = new Color(0.02f, 0.65f, 1f, 1f);
    public Color signWhite = new Color(0.92f, 0.95f, 1f, 1f);

    [Header("Materiais gerados")]
    public Material wallCyanMat, floorMat, roofMat, darkMetalMat, lightMetalMat, safetyYellowMat, rubberBlackMat, glassBlueMat;
    public Material electricBlueMat, chemicalGreenMat, chemicalSpillMat, pneumaticBlueMat, hydraulicYellowMat, thermalOrangeMat, mechanicalRedMat;
    public Material teddyBrownMat, fabricRedMat, fabricBlueMat, fabricPinkMat, fabricYellowMat, cardboardMat, cottonWhiteMat, monitorBlueMat, signWhiteMat;

    [Header("Debug")]
    public bool showDebugLogs = true;

    void Reset()
    {
        factoryRoot = transform;
    }

    void Awake()
    {
        EnsureMaterials();

        if (applyOnStart && Application.isPlaying)
            ApplyMaterials();
    }

    void Start()
    {
        if (applyOnStart && Application.isPlaying)
            ApplyMaterials();
    }

    [ContextMenu("Create/Refresh Materials")]
    public void EnsureMaterials()
    {
        wallCyanMat = MakeMaterial("TW_Wall_Cyan", wallCyan, 0f, 0.35f, false);
        floorMat = MakeMaterial("TW_Floor_DarkConcrete", floorDarkConcrete, 0f, 0.25f, false);
        roofMat = MakeMaterial("TW_Roof_DarkMetal", roofDarkMetal, 0.25f, 0.35f, false);
        darkMetalMat = MakeMaterial("TW_Metal_Dark", darkMetal, 0.45f, 0.35f, false);
        lightMetalMat = MakeMaterial("TW_Metal_Light", lightMetal, 0.35f, 0.45f, false);
        safetyYellowMat = MakeMaterial("TW_Safety_Yellow", safetyYellow, 0f, 0.35f, false);
        rubberBlackMat = MakeMaterial("TW_Rubber_Black", rubberBlack, 0f, 0.18f, false);
        glassBlueMat = MakeMaterial("TW_Glass_DarkBlue", glassBlue, 0f, 0.05f, true);

        electricBlueMat = MakeMaterial("TW_Energy_Electric_Blue", electricBlue, 0f, 0.45f, false);
        chemicalGreenMat = MakeMaterial("TW_Energy_Chemical_Green", chemicalGreen, 0f, 0.35f, false);
        chemicalSpillMat = MakeMaterial("TW_Chemical_Spill_Transparent", new Color(chemicalGreen.r, chemicalGreen.g, chemicalGreen.b, 0.45f), 0f, 0.05f, true);
        pneumaticBlueMat = MakeMaterial("TW_Energy_Pneumatic_Blue", pneumaticBlue, 0f, 0.35f, false);
        hydraulicYellowMat = MakeMaterial("TW_Energy_Hydraulic_Yellow", hydraulicYellow, 0f, 0.35f, false);
        thermalOrangeMat = MakeMaterial("TW_Energy_Thermal_Orange", thermalOrange, 0f, 0.35f, false);
        mechanicalRedMat = MakeMaterial("TW_Energy_Mechanical_Red", mechanicalRed, 0f, 0.35f, false);

        teddyBrownMat = MakeMaterial("TW_Teddy_Brown", teddyBrown, 0f, 0.5f, false);
        fabricRedMat = MakeMaterial("TW_Fabric_Red", fabricRed, 0f, 0.65f, false);
        fabricBlueMat = MakeMaterial("TW_Fabric_Blue", fabricBlue, 0f, 0.65f, false);
        fabricPinkMat = MakeMaterial("TW_Fabric_Pink", fabricPink, 0f, 0.65f, false);
        fabricYellowMat = MakeMaterial("TW_Fabric_Yellow", fabricYellow, 0f, 0.65f, false);
        cardboardMat = MakeMaterial("TW_Cardboard", cardboard, 0f, 0.45f, false);
        cottonWhiteMat = MakeMaterial("TW_Cotton_White", cottonWhite, 0f, 0.85f, false);
        monitorBlueMat = MakeEmissionMaterial("TW_Monitor_Blue_Emission", monitorBlue, 1.5f);
        signWhiteMat = MakeMaterial("TW_Sign_White", signWhite, 0f, 0.45f, false);
    }

    [ContextMenu("Apply TeddyWorks Materials")]
    public void ApplyMaterials()
    {
        if (factoryRoot == null)
            factoryRoot = transform;

        EnsureMaterials();

        Renderer[] renderers = factoryRoot.GetComponentsInChildren<Renderer>(includeInactiveObjects);
        int applied = 0;

        foreach (Renderer renderer in renderers)
        {
            if (renderer == null)
                continue;

            if (!overwriteExistingMaterials && renderer.sharedMaterial != null && !renderer.sharedMaterial.name.Contains("Default"))
                continue;

            Material mat = ChooseMaterial(renderer.gameObject);

            if (mat != null)
            {
                renderer.sharedMaterial = mat;
                applied++;
            }
        }

        if (showDebugLogs)
            Debug.Log("[TeddyWorksFactoryMaterialSetup] Materiais aplicados em " + applied + " objetos.", this);
    }

    [ContextMenu("Apply Only Cyan Walls")]
    public void ApplyOnlyCyanWalls()
    {
        if (factoryRoot == null)
            factoryRoot = transform;

        EnsureMaterials();

        Renderer[] renderers = factoryRoot.GetComponentsInChildren<Renderer>(includeInactiveObjects);
        int applied = 0;

        foreach (Renderer renderer in renderers)
        {
            if (renderer == null)
                continue;

            string n = FullName(renderer.gameObject);

            if (HasAny(n, "wall", "parede", "divisoria", "divisória", "partition"))
            {
                renderer.sharedMaterial = wallCyanMat;
                applied++;
            }
        }

        if (showDebugLogs)
            Debug.Log("[TeddyWorksFactoryMaterialSetup] Paredes ciano aplicadas em " + applied + " objetos.", this);
    }

    Material ChooseMaterial(GameObject obj)
    {
        string n = FullName(obj);

        if (HasAny(n, "glass", "vidro", "window", "janela")) return glassBlueMat;
        if (HasAny(n, "spill", "vazamento", "leak", "mancha")) return chemicalSpillMat;

        if (HasAny(n, "floor", "piso", "ground", "chao", "chão")) return floorMat;
        if (HasAny(n, "wall", "parede", "divisoria", "divisória", "partition")) return wallCyanMat;
        if (HasAny(n, "roof", "telhado", "ceiling", "teto")) return roofMat;
        if (HasAny(n, "pillar", "pilar", "column", "coluna", "beam", "viga", "truss", "trelica", "treliça")) return darkMetalMat;

        if (HasAny(n, "sector_b", "electrical", "eletrica", "elétrica", "electric", "cable", "cabo", "painel_eletric", "panel")) return electricBlueMat;
        if (HasAny(n, "sector_c", "chemical", "quimica", "química", "drum", "tambor", "solvent", "cola", "tinta", "lubricant", "lubrificante")) return chemicalGreenMat;
        if (HasAny(n, "sector_d", "pneumatic", "pneumatica", "pneumática", "compressor", "air", "tank", "valve", "valvula", "válvula")) return pneumaticBlueMat;
        if (HasAny(n, "sector_e", "hydraulic", "hidraulica", "hidráulica", "platform", "plataforma", "dock", "doca", "gate", "portao", "portão")) return hydraulicYellowMat;
        if (HasAny(n, "sector_f", "thermal", "termica", "térmica", "heat", "hot", "caldeira", "boiler", "steam", "vapor", "selagem", "secador")) return thermalOrangeMat;
        if (HasAny(n, "sector_g", "mechanical", "mecanica", "mecânica", "residual", "lever", "alavanca", "hook", "gancho", "chain", "corrente", "arm", "braco", "braço")) return mechanicalRedMat;

        if (HasAny(n, "conveyor", "esteira", "belt")) return rubberBlackMat;
        if (HasAny(n, "machine", "maquina", "máquina", "sewing", "costura", "seladora", "dryer")) return lightMetalMat;
        if (HasAny(n, "rack", "shelf", "shelves", "prateleira", "estante")) return darkMetalMat;
        if (HasAny(n, "rail", "corrimao", "corrimão", "guard", "safety", "yellow", "stair", "stairs", "escada")) return safetyYellowMat;
        if (HasAny(n, "monitor", "screen", "tela", "terminal", "display")) return monitorBlueMat;
        if (HasAny(n, "sign", "placa", "text", "texto")) return signWhiteMat;

        if (HasAny(n, "teddy", "bear", "urso", "pelucia", "pelúcia", "mascot", "mascote")) return teddyBrownMat;
        if (HasAny(n, "fabric_red", "tecido_vermelho", "red_fabric")) return fabricRedMat;
        if (HasAny(n, "fabric_blue", "tecido_azul", "blue_fabric")) return fabricBlueMat;
        if (HasAny(n, "fabric_pink", "tecido_rosa", "pink_fabric")) return fabricPinkMat;
        if (HasAny(n, "fabric_yellow", "tecido_amarelo", "yellow_fabric")) return fabricYellowMat;
        if (HasAny(n, "fabric", "tecido", "roll_fabric", "rolo")) return fabricPinkMat;
        if (HasAny(n, "cotton", "algodao", "algodão", "filling", "enchimento", "saco")) return cottonWhiteMat;
        if (HasAny(n, "box", "caixa", "cardboard", "papelao", "papelão", "pallet", "palete")) return cardboardMat;

        if (HasAny(n, "metal", "pipe", "tubo", "duct", "duto", "vent", "porta", "door")) return darkMetalMat;

        return lightMetalMat;
    }

    string FullName(GameObject obj)
    {
        string value = obj.name;
        Transform current = obj.transform.parent;

        while (current != null)
        {
            value += " " + current.name;
            current = current.parent;
        }

        return Normalize(value);
    }

    bool HasAny(string value, params string[] terms)
    {
        foreach (string term in terms)
        {
            if (value.Contains(Normalize(term)))
                return true;
        }

        return false;
    }

    string Normalize(string input)
    {
        if (string.IsNullOrEmpty(input))
            return "";

        return input.ToLowerInvariant()
            .Replace("á", "a").Replace("à", "a").Replace("ã", "a").Replace("â", "a")
            .Replace("é", "e").Replace("ê", "e")
            .Replace("í", "i")
            .Replace("ó", "o").Replace("ô", "o").Replace("õ", "o")
            .Replace("ú", "u")
            .Replace("ç", "c");
    }

    Material MakeMaterial(string matName, Color color, float metallic, float smoothness, bool transparent)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");

        Material mat = new Material(shader);
        mat.name = matName;

        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
        if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
        if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", metallic);
        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);

        if (transparent)
            ConfigureTransparent(mat);

        return mat;
    }

    Material MakeEmissionMaterial(string matName, Color color, float emissionStrength)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");

        Material mat = new Material(shader);
        mat.name = matName;

        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
        if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);

        if (mat.HasProperty("_EmissionColor"))
        {
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", color * Mathf.Max(0f, emissionStrength));
        }

        return mat;
    }

    void ConfigureTransparent(Material mat)
    {
        if (mat == null) return;

        if (mat.HasProperty("_Surface")) mat.SetFloat("_Surface", 1f);
        if (mat.HasProperty("_Blend")) mat.SetFloat("_Blend", 0f);
        if (mat.HasProperty("_AlphaClip")) mat.SetFloat("_AlphaClip", 0f);

        mat.renderQueue = 3000;
        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        mat.DisableKeyword("_ALPHATEST_ON");
    }
}
