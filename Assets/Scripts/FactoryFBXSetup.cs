using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Attach this script to the ROOT GameObject of the factory FBX instance in the scene.
/// Then use the component menu: Apply Factory FBX Setup.
///
/// It assigns readable Unity materials and adds practical colliders based on object names
/// exported from the Blender factory generator.
/// </summary>
[ExecuteAlways]
public class FactoryFBXSetup : MonoBehaviour
{
    [Header("Setup")]
    [Tooltip("If enabled, removes existing colliders on factory children before adding new ones.")]
    public bool clearOldCollidersBeforeApplying = false;

    [Tooltip("Marks all children as static. Good for baked lighting, batching and static MeshColliders.")]
    public bool markObjectsAsStatic = true;

    [Tooltip("Adds Unity Point Lights to pendant lamps, status lights and exit signs.")]
    public bool addFunctionalUnityLights = true;

    [Tooltip("Optional. Leave empty to keep the current layer. If the layer exists, all factory objects will be moved to it.")]
    public string optionalLayerName = "";

    [Header("Collision")]
    [Tooltip("Uses exact MeshColliders for ramps and stair-like meshes. Most other objects receive BoxColliders.")]
    public bool useMeshCollidersForRampsAndStairs = true;

    [Tooltip("Decorative thin elements such as painted safety lines, parking stripes, windows, skylights and signs will not receive colliders.")]
    public bool skipDecorativeColliders = true;

    [Header("Light Settings")]
    public float pendantLightIntensity = 450f;
    public float pendantLightRange = 9f;
    public float statusLightIntensity = 2.5f;
    public float statusLightRange = 2.5f;
    public float exitSignLightIntensity = 1.8f;
    public float exitSignLightRange = 3f;

    private readonly Dictionary<string, Material> materials = new Dictionary<string, Material>();

    [ContextMenu("Apply Factory FBX Setup")]
    public void ApplyFactorySetup()
    {
        BuildMaterials();

        int materialCount = 0;
        int colliderCount = 0;
        int lightCount = 0;
        int layer = GetOptionalLayer();

        Transform[] allChildren = GetComponentsInChildren<Transform>(true);

        foreach (Transform child in allChildren)
        {
            GameObject obj = child.gameObject;

            if (markObjectsAsStatic)
                obj.isStatic = true;

            if (layer >= 0)
                obj.layer = layer;

            Renderer renderer = obj.GetComponent<Renderer>();
            if (renderer != null)
            {
                AssignMaterialByName(obj, renderer);
                materialCount++;
            }

            if (clearOldCollidersBeforeApplying)
                RemoveColliders(obj);

            if (ShouldReceiveCollider(obj))
            {
                if (!HasCollider(obj))
                {
                    AddBestCollider(obj);
                    colliderCount++;
                }
            }

            if (addFunctionalUnityLights)
            {
                if (AddOrUpdateLight(obj))
                    lightCount++;
            }
        }

        Debug.Log($"Factory FBX setup applied to '{name}'. Materials assigned: {materialCount}. Colliders added: {colliderCount}. Lights added/updated: {lightCount}.", this);
    }

    [ContextMenu("Remove Generated Colliders From Children")]
    public void RemoveGeneratedCollidersFromChildren()
    {
        int removed = 0;
        Transform[] allChildren = GetComponentsInChildren<Transform>(true);

        foreach (Transform child in allChildren)
        {
            Collider[] colliders = child.GetComponents<Collider>();
            foreach (Collider col in colliders)
            {
                SafeDestroy(col);
                removed++;
            }
        }

        Debug.Log($"Removed {removed} colliders from '{name}'.", this);
    }

    [ContextMenu("Remove Generated Lights From Children")]
    public void RemoveGeneratedLightsFromChildren()
    {
        int removed = 0;
        Transform[] allChildren = GetComponentsInChildren<Transform>(true);

        foreach (Transform child in allChildren)
        {
            Light lightComponent = child.GetComponent<Light>();
            if (lightComponent != null)
            {
                SafeDestroy(lightComponent);
                removed++;
            }
        }

        Debug.Log($"Removed {removed} lights from '{name}'.", this);
    }

    private void BuildMaterials()
    {
        materials.Clear();

        materials["Concrete"] = CreateMaterial("Factory_Concrete", new Color(0.48f, 0.48f, 0.45f), 0.0f, 0.35f);
        materials["ConcreteLight"] = CreateMaterial("Factory_Concrete_Light", new Color(0.70f, 0.72f, 0.70f), 0.0f, 0.4f);
        materials["IndustrialFloor"] = CreateMaterial("Factory_Industrial_Floor_Grey", new Color(0.30f, 0.31f, 0.32f), 0.0f, 0.25f);
        materials["Asphalt"] = CreateMaterial("Factory_Asphalt_Dark", new Color(0.11f, 0.11f, 0.105f), 0.0f, 0.12f);

        materials["LightMetal"] = CreateMaterial("Factory_Light_Metal", new Color(0.66f, 0.68f, 0.68f), 0.65f, 0.35f);
        materials["DarkMetal"] = CreateMaterial("Factory_Dark_Metal", new Color(0.13f, 0.14f, 0.15f), 0.75f, 0.28f);
        materials["RoofMetal"] = CreateMaterial("Factory_Roof_Metal", new Color(0.35f, 0.38f, 0.39f), 0.55f, 0.26f);

        materials["Glass"] = CreateMaterial("Factory_Glass", new Color(0.40f, 0.72f, 0.90f, 0.35f), 0.0f, 0.85f, true);
        materials["DarkPlastic"] = CreateMaterial("Factory_Dark_Plastic", new Color(0.045f, 0.047f, 0.052f), 0.0f, 0.45f);
        materials["PlasticGrey"] = CreateMaterial("Factory_Plastic_Grey", new Color(0.42f, 0.45f, 0.48f), 0.0f, 0.35f);
        materials["PlasticBlue"] = CreateMaterial("Factory_Plastic_Blue", new Color(0.10f, 0.25f, 0.55f), 0.0f, 0.4f);
        materials["Rubber"] = CreateMaterial("Factory_Rubber", new Color(0.015f, 0.015f, 0.014f), 0.0f, 0.18f);

        materials["SafetyYellow"] = CreateMaterial("Factory_Safety_Yellow", new Color(1.0f, 0.76f, 0.04f), 0.0f, 0.3f);
        materials["RedPaint"] = CreateMaterial("Factory_Red_Paint", new Color(0.82f, 0.05f, 0.035f), 0.0f, 0.38f);
        materials["ConveyorBelt"] = CreateMaterial("Factory_Conveyor_Black_Belt", new Color(0.025f, 0.024f, 0.022f), 0.0f, 0.20f);
        materials["WoodPallet"] = CreateMaterial("Factory_Wood_Pallet", new Color(0.54f, 0.36f, 0.18f), 0.0f, 0.25f);
        materials["Cardboard"] = CreateMaterial("Factory_Cardboard", new Color(0.65f, 0.45f, 0.25f), 0.0f, 0.32f);

        materials["WhiteEmission"] = CreateMaterial("Factory_White_Emission", new Color(1f, 1f, 1f), 0.0f, 0.0f, false, new Color(1.0f, 0.96f, 0.86f) * 2.5f);
        materials["RedEmission"] = CreateMaterial("Factory_Red_Emission", new Color(1f, 0.05f, 0.035f), 0.0f, 0.0f, false, new Color(1f, 0.05f, 0.035f) * 2.8f);
        materials["GreenEmission"] = CreateMaterial("Factory_Green_Emission", new Color(0.05f, 1.0f, 0.25f), 0.0f, 0.0f, false, new Color(0.05f, 1.0f, 0.25f) * 2.4f);
        materials["YellowEmission"] = CreateMaterial("Factory_Yellow_Emission", new Color(1.0f, 0.78f, 0.05f), 0.0f, 0.0f, false, new Color(1.0f, 0.78f, 0.05f) * 2.4f);
    }

    private Material CreateMaterial(string materialName, Color color, float metallic, float smoothness, bool transparent = false, Color? emission = null)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
            shader = Shader.Find("Standard");

        Material mat = new Material(shader);
        mat.name = materialName;

        SetColor(mat, color);
        SetFloatIfExists(mat, "_Metallic", metallic);
        SetFloatIfExists(mat, "_Smoothness", smoothness);

        if (transparent)
            ConfigureTransparent(mat);

        if (emission.HasValue)
            ConfigureEmission(mat, emission.Value);

        return mat;
    }

    private void AssignMaterialByName(GameObject obj, Renderer renderer)
    {
        string key = ChooseMaterialKey(obj, renderer);
        if (!materials.TryGetValue(key, out Material material))
            material = materials["Concrete"];

        Material[] shared = renderer.sharedMaterials;
        if (shared == null || shared.Length == 0)
        {
            renderer.sharedMaterial = material;
            return;
        }

        for (int i = 0; i < shared.Length; i++)
            shared[i] = material;

        renderer.sharedMaterials = shared;
    }

    private string ChooseMaterialKey(GameObject obj, Renderer renderer)
    {
        string n = obj.name.ToLowerInvariant();
        string matName = renderer.sharedMaterial != null ? renderer.sharedMaterial.name.ToLowerInvariant() : "";
        string text = n + " " + matName;

        // Emissive / lights first.
        if (text.Contains("input_status_light")) return "GreenEmission";
        if (text.Contains("process_status_light")) return "YellowEmission";
        if (text.Contains("packaging_status_light")) return "RedEmission";
        if (text.Contains("status_light")) return "RedEmission";
        if (text.Contains("exit_sign")) return "GreenEmission";
        if (text.StartsWith("light_") || text.Contains("luminaria") || text.Contains("lamp")) return "WhiteEmission";

        // Glass.
        if (text.Contains("window") || text.Contains("skylight") || text.Contains("glass") || text.Contains("vidro")) return "Glass";

        // Conveyor logic before generic base/floor rules.
        if (text.Contains("conveyor") && text.Contains("belt")) return "ConveyorBelt";
        if (text.Contains("conveyor")) return "LightMetal";

        // Control panels / dark plastic.
        if (text.Contains("panel") || text.Contains("monitor") || text.Contains("chair") || text.Contains("plastic_dark") || text.Contains("plastico_escuro")) return "DarkPlastic";

        // Safety / paint.
        if (text.Contains("safety") || text.Contains("parking_line") || text.Contains("barrier") || text.Contains("handrail") || text.Contains("corrimao")) return "SafetyYellow";
        if (text.Contains("extinguisher") || text.Contains("pallet_truck") || text.Contains("red_paint") || text.Contains("pintura_vermelha")) return "RedPaint";

        // Storage.
        if (text.Contains("hand_pallet_truck")) return "RedPaint";
        if (text.Contains("pallet")) return "WoodPallet";
        if (text.Contains("box") || text.Contains("package") || text.Contains("cardboard") || text.Contains("caixa") || text.Contains("papelao")) return "Cardboard";
        if (text.Contains("container_blue")) return "PlasticBlue";
        if (text.Contains("container") || text.Contains("plastic") || text.Contains("plastico")) return "PlasticGrey";

        // Main structure.
        if (text.Contains("asphalt")) return "Asphalt";
        if (text.Contains("factory_floor") || text.Contains("industrial_floor") || text.Contains("piso_industrial")) return "IndustrialFloor";
        if (text.Contains("roof")) return "RoofMetal";
        if (text.Contains("wall") || text.Contains("control_room") || text.Contains("concrete_light") || text.Contains("concreto_claro")) return "ConcreteLight";
        if (text.Contains("loading_dock") || text.Contains("loading_ramp") || text.Contains("factory_base") || text.Contains("concrete") || text.Contains("concreto")) return "Concrete";

        // Metal objects.
        if (text.Contains("rack") || text.Contains("column") || text.Contains("stairs") || text.Contains("door") || text.Contains("metal_dark") || text.Contains("metal_escuro")) return "DarkMetal";
        if (text.Contains("pipe") || text.Contains("duct") || text.Contains("exhaust") || text.Contains("machine") || text.Contains("gate") || text.Contains("handle") || text.Contains("desk") || text.Contains("metal")) return "LightMetal";

        return "Concrete";
    }

    private bool ShouldReceiveCollider(GameObject obj)
    {
        if (obj == gameObject)
            return false;

        MeshFilter meshFilter = obj.GetComponent<MeshFilter>();
        Renderer renderer = obj.GetComponent<Renderer>();

        if (meshFilter == null || meshFilter.sharedMesh == null || renderer == null)
            return false;

        string n = obj.name.ToLowerInvariant();

        if (skipDecorativeColliders)
        {
            if (n.Contains("window") || n.Contains("skylight") || n.Contains("glass")) return false;
            if (n.Contains("light_") || n.Contains("status_light") || n.Contains("exit_sign")) return false;
            if (n.Contains("safety_line") || n.Contains("parking_line") || n.Contains("safety_path")) return false;
            if (n.Contains("handle")) return false;
        }

        // These should definitely collide.
        if (n.Contains("floor") || n.Contains("base") || n.Contains("wall") || n.Contains("roof")) return true;
        if (n.Contains("gate") || n.Contains("door") || n.Contains("dock") || n.Contains("ramp")) return true;
        if (n.Contains("column") || n.Contains("rack") || n.Contains("stairs") || n.Contains("step")) return true;
        if (n.Contains("conveyor") || n.Contains("machine")) return true;
        if (n.Contains("pallet") || n.Contains("box") || n.Contains("package") || n.Contains("container")) return true;
        if (n.Contains("pipe") || n.Contains("duct") || n.Contains("exhaust")) return true;
        if (n.Contains("barrier") || n.Contains("handrail") || n.Contains("extinguisher")) return true;
        if (n.Contains("desk") || n.Contains("chair") || n.Contains("monitor")) return true;
        if (n.Contains("asphalt")) return true;

        // Fallback: if it has visible mesh, give it a simple collider.
        return true;
    }

    private void AddBestCollider(GameObject obj)
    {
        string n = obj.name.ToLowerInvariant();
        MeshFilter meshFilter = obj.GetComponent<MeshFilter>();

        bool useExactMesh = useMeshCollidersForRampsAndStairs &&
                            (n.Contains("ramp") || n.Contains("stairs") || n.Contains("step"));

        if (useExactMesh && meshFilter != null && meshFilter.sharedMesh != null)
        {
            MeshCollider meshCollider = obj.AddComponent<MeshCollider>();
            meshCollider.sharedMesh = meshFilter.sharedMesh;
            meshCollider.convex = false;
            return;
        }

        BoxCollider box = obj.AddComponent<BoxCollider>();

        if (meshFilter != null && meshFilter.sharedMesh != null)
        {
            Bounds meshBounds = meshFilter.sharedMesh.bounds;
            box.center = meshBounds.center;
            box.size = meshBounds.size;
        }
    }

    private bool AddOrUpdateLight(GameObject obj)
    {
        string n = obj.name.ToLowerInvariant();
        bool isPendantLight = n.StartsWith("light_") || n.Contains("luminaria") || n.Contains("lamp");
        bool isStatusLight = n.Contains("status_light");
        bool isExitSign = n.Contains("exit_sign");

        if (!isPendantLight && !isStatusLight && !isExitSign)
            return false;

        Light lightComponent = obj.GetComponent<Light>();
        bool created = false;

        if (lightComponent == null)
        {
            lightComponent = obj.AddComponent<Light>();
            created = true;
        }

        lightComponent.type = LightType.Point;
        lightComponent.shadows = LightShadows.Soft;

        if (isStatusLight)
        {
            lightComponent.intensity = statusLightIntensity;
            lightComponent.range = statusLightRange;
            lightComponent.color = GetStatusLightColor(n);
        }
        else if (isExitSign)
        {
            lightComponent.intensity = exitSignLightIntensity;
            lightComponent.range = exitSignLightRange;
            lightComponent.color = new Color(0.05f, 1.0f, 0.25f);
        }
        else
        {
            lightComponent.intensity = pendantLightIntensity;
            lightComponent.range = pendantLightRange;
            lightComponent.color = new Color(0.88f, 0.94f, 1.0f);
        }

        return created;
    }

    private Color GetStatusLightColor(string lowerName)
    {
        if (lowerName.Contains("input")) return new Color(0.05f, 1.0f, 0.25f);
        if (lowerName.Contains("process")) return new Color(1.0f, 0.78f, 0.05f);
        if (lowerName.Contains("packaging")) return new Color(1.0f, 0.05f, 0.035f);
        return new Color(1.0f, 0.05f, 0.035f);
    }

    private void RemoveColliders(GameObject obj)
    {
        Collider[] colliders = obj.GetComponents<Collider>();
        foreach (Collider col in colliders)
            SafeDestroy(col);
    }

    private bool HasCollider(GameObject obj)
    {
        return obj.GetComponent<Collider>() != null;
    }

    private int GetOptionalLayer()
    {
        if (string.IsNullOrWhiteSpace(optionalLayerName))
            return -1;

        int layer = LayerMask.NameToLayer(optionalLayerName);
        if (layer < 0)
            Debug.LogWarning($"Layer '{optionalLayerName}' does not exist. Keeping current layers.", this);

        return layer;
    }

    private void SetColor(Material mat, Color color)
    {
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
        if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
    }

    private void SetFloatIfExists(Material mat, string property, float value)
    {
        if (mat.HasProperty(property))
            mat.SetFloat(property, value);
    }

    private void ConfigureTransparent(Material mat)
    {
        // URP Lit.
        if (mat.HasProperty("_Surface")) mat.SetFloat("_Surface", 1f);
        if (mat.HasProperty("_Blend")) mat.SetFloat("_Blend", 0f);
        if (mat.HasProperty("_AlphaClip")) mat.SetFloat("_AlphaClip", 0f);
        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");

        // Built-in Standard fallback.
        if (mat.HasProperty("_Mode")) mat.SetFloat("_Mode", 3f);
        mat.SetOverrideTag("RenderType", "Transparent");
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0);
        mat.DisableKeyword("_ALPHATEST_ON");
        mat.EnableKeyword("_ALPHABLEND_ON");
        mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        mat.renderQueue = 3000;
    }

    private void ConfigureEmission(Material mat, Color emissionColor)
    {
        if (mat.HasProperty("_EmissionColor"))
            mat.SetColor("_EmissionColor", emissionColor);

        mat.EnableKeyword("_EMISSION");
    }

    private void SafeDestroy(Object obj)
    {
        if (obj == null)
            return;

        if (Application.isPlaying)
            Destroy(obj);
        else
            DestroyImmediate(obj);
    }
}
