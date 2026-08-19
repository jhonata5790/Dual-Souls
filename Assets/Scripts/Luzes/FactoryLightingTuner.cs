
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class FactoryLightingTuner : MonoBehaviour
{
    [Header("Referências")]
    [Tooltip("Objeto raiz da fábrica. Normalmente é 'Fabrica'.")]
    public Transform factoryRoot;

    [Tooltip("Pai onde as luzes extras geradas serão criadas.")]
    public Transform generatedLightsParent;

    [Header("Comportamento")]
    [Tooltip("Se ligado, procura a fábrica e luzes automaticamente.")]
    public bool autoFindOnStart = true;

    [Tooltip("Se ligado, aplica os ajustes no Start.")]
    public bool applyOnStart = false;

    [Tooltip("Preserva se cada luz estava ligada/desligada. Recomendado para o terminal continuar controlando as luzes.")]
    public bool preserveEnabledState = true;

    [Tooltip("Cria luzes de preenchimento internas para evitar que a fábrica vire um breu.")]
    public bool createInteriorFillLights = true;

    [Tooltip("Cria luzes para doca/entrada externa.")]
    public bool createDockLights = true;

    [Tooltip("Cria luzes de apoio para sala de controle e escada.")]
    public bool createControlRoomSupportLights = true;

    [Header("Luzes principais do teto")]
    public float ceilingLightIntensity = 3.2f;
    public float ceilingLightRange = 16f;
    public LightShadows ceilingLightShadows = LightShadows.None;

    [Tooltip("Se quiser algumas sombras no teto, coloca 1, 2 ou 3. Para performance, 0 é melhor.")]
    public int maxCeilingLightsWithShadows = 0;

    [Header("Luzes de preenchimento internas")]
    public float fillLightIntensity = 0.9f;
    public float fillLightRange = 22f;
    public Color fillLightColor = new Color(0.72f, 0.82f, 1.0f, 1f);

    [Header("Doca / área externa frontal")]
    public float dockLightIntensity = 2.8f;
    public float dockLightRange = 18f;
    public Color dockLightColor = new Color(1.0f, 0.88f, 0.68f, 1f);

    [Header("Sala de controle / escada")]
    public float controlRoomLightIntensity = 1.8f;
    public float controlRoomLightRange = 10f;
    public Color controlRoomLightColor = new Color(0.72f, 0.86f, 1.0f, 1f);

    [Header("Luzes de status / placas")]
    [Tooltip("Luzes decorativas/status ficam mais fracas para não estourar a cena.")]
    public bool tuneStatusAndSignLights = true;
    public float statusLightIntensity = 0.7f;
    public float statusLightRange = 3.5f;

    [Header("Lista encontrada")]
    public List<Light> allFactoryLights = new List<Light>();
    public List<Light> ceilingLights = new List<Light>();
    public List<Light> statusLights = new List<Light>();
    public List<Light> generatedSupportLights = new List<Light>();

    [Header("Debug")]
    public bool showDebugLogs = false;

    private readonly Dictionary<Light, bool> originalEnabledState = new Dictionary<Light, bool>();

    private const string GENERATED_PARENT_NAME = "Factory_Lighting_Tuned_Generated";

    void Reset()
    {
        TryFindFactoryRoot();
    }

    void Awake()
    {
        if (autoFindOnStart)
            FindFactoryLights();

        if (applyOnStart)
            ApplyLightingTuning();
    }

    void Start()
    {
        if (autoFindOnStart && allFactoryLights.Count == 0)
            FindFactoryLights();

        if (applyOnStart)
            ApplyLightingTuning();
    }

    [ContextMenu("Find Factory Lights")]
    public void FindFactoryLights()
    {
        TryFindFactoryRoot();
        EnsureGeneratedParent();

        allFactoryLights.Clear();
        ceilingLights.Clear();
        statusLights.Clear();
        generatedSupportLights.Clear();
        originalEnabledState.Clear();

        Light[] lights;

        if (factoryRoot != null)
            lights = factoryRoot.GetComponentsInChildren<Light>(true);
        else
            lights = FindObjectsOfType<Light>(true);

        foreach (Light light in lights)
        {
            if (light == null)
                continue;

            string n = light.name.ToLowerInvariant();

            if (ShouldIgnoreLight(light))
                continue;

            allFactoryLights.Add(light);
            originalEnabledState[light] = light.enabled;

            if (IsGeneratedSupportLight(light))
            {
                generatedSupportLights.Add(light);
                continue;
            }

            if (IsCeilingLight(light))
                ceilingLights.Add(light);

            if (IsStatusOrSignLight(light))
                statusLights.Add(light);
        }

        if (showDebugLogs)
        {
            Debug.Log(
                "[FactoryLightingTuner] Luzes encontradas: " + allFactoryLights.Count +
                " | Teto: " + ceilingLights.Count +
                " | Status/placas: " + statusLights.Count,
                this
            );
        }
    }

    bool ShouldIgnoreLight(Light light)
    {
        string n = light.name.ToLowerInvariant();

        if (n.Contains("sun") || n.Contains("moon"))
            return true;

        if (n.Contains("security_mode"))
            return true;

        if (n.Contains("terminal") || n.Contains("monitor") || n.Contains("pc"))
            return true;

        return false;
    }

    bool IsGeneratedSupportLight(Light light)
    {
        string n = light.name.ToLowerInvariant();
        return n.Contains("factory_fill_light") ||
               n.Contains("factory_dock_light") ||
               n.Contains("factory_control_support_light");
    }

    bool IsCeilingLight(Light light)
    {
        string n = light.name.ToLowerInvariant();

        if (n.StartsWith("light_"))
            return true;

        if (n.Contains("luminaria") || n.Contains("luminária") || n.Contains("lamp"))
            return true;

        if (light.transform.position.y >= 5.5f && light.type == LightType.Point)
            return true;

        return false;
    }

    bool IsStatusOrSignLight(Light light)
    {
        string n = light.name.ToLowerInvariant();

        return n.Contains("status") ||
               n.Contains("exit") ||
               n.Contains("sign") ||
               n.Contains("red") ||
               n.Contains("green") ||
               n.Contains("yellow");
    }

    [ContextMenu("Apply Factory Lighting Tuning")]
    public void ApplyLightingTuning()
    {
        if (allFactoryLights.Count == 0)
            FindFactoryLights();

        EnsureGeneratedParent();

        if (createInteriorFillLights)
            CreateOrUpdateInteriorFillLights();

        if (createDockLights)
            CreateOrUpdateDockLights();

        if (createControlRoomSupportLights)
            CreateOrUpdateControlRoomLights();

        TuneExistingFactoryLights();

        if (preserveEnabledState)
            RestoreOriginalEnabledStates();

        if (showDebugLogs)
            Debug.Log("[FactoryLightingTuner] Ajustes de iluminação aplicados.", this);
    }

    void TuneExistingFactoryLights()
    {
        int shadowedCeilingCount = 0;

        foreach (Light light in ceilingLights)
        {
            if (light == null)
                continue;

            light.type = LightType.Point;
            light.intensity = ceilingLightIntensity;
            light.range = ceilingLightRange;
            light.color = new Color(0.82f, 0.9f, 1.0f, 1f);

            if (shadowedCeilingCount < maxCeilingLightsWithShadows)
            {
                light.shadows = LightShadows.Soft;
                shadowedCeilingCount++;
            }
            else
            {
                light.shadows = ceilingLightShadows;
            }
        }

        if (tuneStatusAndSignLights)
        {
            foreach (Light light in statusLights)
            {
                if (light == null)
                    continue;

                light.intensity = statusLightIntensity;
                light.range = statusLightRange;
                light.shadows = LightShadows.None;
            }
        }

        foreach (Light light in allFactoryLights)
        {
            if (light == null)
                continue;

            if (ceilingLights.Contains(light) || statusLights.Contains(light))
                continue;

            if (IsGeneratedSupportLight(light))
                continue;

            string n = light.name.ToLowerInvariant();

            if (n.Contains("control") || n.Contains("room") || n.Contains("stairs"))
            {
                light.intensity = controlRoomLightIntensity;
                light.range = controlRoomLightRange;
                light.color = controlRoomLightColor;
                light.shadows = LightShadows.None;
            }
            else
            {
                light.intensity = Mathf.Max(light.intensity, 1.2f);
                light.range = Mathf.Max(light.range, 9f);
                light.shadows = LightShadows.None;
            }
        }
    }

    void CreateOrUpdateInteriorFillLights()
    {
        EnsureGeneratedParent();

        CreateOrUpdatePointLight(
            "Factory_Fill_Light_Center",
            new Vector3(0f, 0f, 5.4f),
            fillLightColor,
            fillLightIntensity,
            fillLightRange
        );

        CreateOrUpdatePointLight(
            "Factory_Fill_Light_Back",
            new Vector3(0f, 13f, 5.2f),
            fillLightColor,
            fillLightIntensity * 0.85f,
            fillLightRange
        );

        CreateOrUpdatePointLight(
            "Factory_Fill_Light_Front",
            new Vector3(0f, -13f, 5.2f),
            fillLightColor,
            fillLightIntensity * 0.85f,
            fillLightRange
        );

        CreateOrUpdatePointLight(
            "Factory_Fill_Light_Left",
            new Vector3(-9f, 0f, 4.7f),
            fillLightColor,
            fillLightIntensity * 0.65f,
            fillLightRange * 0.75f
        );

        CreateOrUpdatePointLight(
            "Factory_Fill_Light_Right",
            new Vector3(9f, 0f, 4.7f),
            fillLightColor,
            fillLightIntensity * 0.65f,
            fillLightRange * 0.75f
        );
    }

    void CreateOrUpdateDockLights()
    {
        EnsureGeneratedParent();

        CreateOrUpdatePointLight(
            "Factory_Dock_Light_Left",
            new Vector3(-5.5f, -18.8f, 4.2f),
            dockLightColor,
            dockLightIntensity,
            dockLightRange
        );

        CreateOrUpdatePointLight(
            "Factory_Dock_Light_Right",
            new Vector3(5.5f, -18.8f, 4.2f),
            dockLightColor,
            dockLightIntensity,
            dockLightRange
        );

        CreateOrUpdatePointLight(
            "Factory_Dock_Light_Exterior",
            new Vector3(0f, -25f, 4.5f),
            dockLightColor,
            dockLightIntensity * 0.75f,
            dockLightRange
        );
    }

    void CreateOrUpdateControlRoomLights()
    {
        EnsureGeneratedParent();

        CreateOrUpdatePointLight(
            "Factory_Control_Support_Light_Room",
            new Vector3(0f, 16.4f, 6.1f),
            controlRoomLightColor,
            controlRoomLightIntensity,
            controlRoomLightRange
        );

        CreateOrUpdatePointLight(
            "Factory_Control_Support_Light_Stairs",
            new Vector3(4.3f, 14.2f, 3.0f),
            controlRoomLightColor,
            controlRoomLightIntensity * 0.75f,
            controlRoomLightRange * 0.9f
        );

        CreateOrUpdatePointLight(
            "Factory_Control_Support_Light_Platform",
            new Vector3(3.5f, 14.6f, 5.0f),
            controlRoomLightColor,
            controlRoomLightIntensity * 0.65f,
            controlRoomLightRange * 0.8f
        );
    }

    Light CreateOrUpdatePointLight(string lightName, Vector3 localPosition, Color color, float intensity, float range)
    {
        EnsureGeneratedParent();

        Transform existing = generatedLightsParent.Find(lightName);
        GameObject obj;

        if (existing == null)
        {
            obj = new GameObject(lightName);
            obj.transform.SetParent(generatedLightsParent, false);
        }
        else
        {
            obj = existing.gameObject;
        }

        obj.transform.localPosition = localPosition;

        Light light = obj.GetComponent<Light>();
        if (light == null)
            light = obj.AddComponent<Light>();

        light.type = LightType.Point;
        light.color = color;
        light.intensity = intensity;
        light.range = range;
        light.shadows = LightShadows.None;
        light.renderMode = LightRenderMode.Auto;

        if (!generatedSupportLights.Contains(light))
            generatedSupportLights.Add(light);

        if (!allFactoryLights.Contains(light))
            allFactoryLights.Add(light);

        if (!originalEnabledState.ContainsKey(light))
            originalEnabledState[light] = light.enabled;

        return light;
    }

    void RestoreOriginalEnabledStates()
    {
        foreach (var pair in originalEnabledState)
        {
            if (pair.Key != null)
                pair.Key.enabled = pair.Value;
        }
    }

    [ContextMenu("Turn Tuned Factory Lights ON")]
    public void TurnTunedFactoryLightsOn()
    {
        if (allFactoryLights.Count == 0)
            FindFactoryLights();

        foreach (Light light in allFactoryLights)
        {
            if (light != null && !ShouldIgnoreLight(light))
                light.enabled = true;
        }
    }

    [ContextMenu("Turn Tuned Factory Lights OFF")]
    public void TurnTunedFactoryLightsOff()
    {
        if (allFactoryLights.Count == 0)
            FindFactoryLights();

        foreach (Light light in allFactoryLights)
        {
            if (light != null && !ShouldIgnoreLight(light))
                light.enabled = false;
        }
    }

    [ContextMenu("Delete Generated Support Lights")]
    public void DeleteGeneratedSupportLights()
    {
        EnsureGeneratedParent();

        if (generatedLightsParent == null)
            return;

        List<GameObject> toDelete = new List<GameObject>();

        for (int i = 0; i < generatedLightsParent.childCount; i++)
        {
            Transform child = generatedLightsParent.GetChild(i);
            toDelete.Add(child.gameObject);
        }

        foreach (GameObject obj in toDelete)
        {
            if (Application.isPlaying)
                Destroy(obj);
            else
                DestroyImmediate(obj);
        }

        generatedSupportLights.Clear();
        allFactoryLights.RemoveAll(l => l == null || IsGeneratedSupportLight(l));
    }

    void TryFindFactoryRoot()
    {
        if (factoryRoot != null)
            return;

        GameObject factory = GameObject.Find("Fabrica");

        if (factory == null)
            factory = GameObject.Find("Factory");

        if (factory != null)
            factoryRoot = factory.transform;
    }

    void EnsureGeneratedParent()
    {
        TryFindFactoryRoot();

        if (generatedLightsParent != null)
            return;

        Transform parent = factoryRoot != null ? factoryRoot : transform;

        Transform existing = parent.Find(GENERATED_PARENT_NAME);

        if (existing == null)
        {
            GameObject obj = new GameObject(GENERATED_PARENT_NAME);
            obj.transform.SetParent(parent, false);
            generatedLightsParent = obj.transform;
        }
        else
        {
            generatedLightsParent = existing;
        }
    }
}
