
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class FactoryExtinguisherPlayer : MonoBehaviour
{
    [Header("Referências")]
    public Camera playerCamera;

    [Tooltip("Onde o extintor aparece na mão. Se vazio, cria automaticamente como filho da câmera.")]
    public Transform handHolder;

    [Header("Interação")]
    public float pickupDistance = 3f;
    public float extinguishDistance = 5f;

    [Tooltip("Clique usado para pegar e também para usar quando já estiver com extintor.")]
    public KeyCode useKey = KeyCode.Mouse0;

    [Tooltip("Tecla para largar o extintor. Opcional.")]
    public KeyCode dropKey = KeyCode.G;

    [Header("Extintor")]
    public bool carryingExtinguisher = false;
    public FactoryExtinguisherMarker currentExtinguisher;

    [Tooltip("Quanto de fogo apaga por segundo.")]
    public float extinguishPowerPerSecond = 38f;

    [Tooltip("Quanto de carga gasta por segundo.")]
    public float fuelUsePerSecond = 16f;

    [Header("Spray visual")]
    public ParticleSystem sprayParticles;
    public bool autoCreateSprayParticles = true;

    [Header("UI opcional")]
    public TMP_Text tmpPromptText;
    public Text uiPromptText;

    [Header("Debug")]
    public bool showDebugLogs = false;

    private GameObject heldVisualInstance;

    void Reset()
    {
        playerCamera = Camera.main;
    }

    void Awake()
    {
        if (playerCamera == null)
            playerCamera = Camera.main;

        EnsureHandHolder();

        if (autoCreateSprayParticles && sprayParticles == null)
            sprayParticles = CreateSprayParticles();

        SetPrompt("");
    }

    void Update()
    {
        if (playerCamera == null)
            return;

        if (Input.GetKeyDown(dropKey) && carryingExtinguisher)
            DropExtinguisher();

        if (carryingExtinguisher)
            HandleCarrying();
        else
            HandlePickupSearch();
    }

    void HandlePickupSearch()
    {
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, pickupDistance, ~0, QueryTriggerInteraction.Collide))
        {
            FactoryExtinguisherMarker marker = hit.collider.GetComponentInParent<FactoryExtinguisherMarker>();

            if (marker != null && marker.canBePicked && !marker.isPicked)
            {
                SetPrompt(marker.interactionPrompt);

                if (Input.GetKeyDown(useKey))
                    PickupExtinguisher(marker);

                return;
            }
        }

        SetPrompt("");
    }

    void HandleCarrying()
    {
        if (currentExtinguisher == null)
        {
            ClearHeldExtinguisher();
            return;
        }

        bool wantsToSpray = Input.GetKey(useKey) && currentExtinguisher.currentFuel > 0f;

        if (wantsToSpray)
        {
            Spray();
            SetPrompt("Extintor: " + Mathf.CeilToInt(currentExtinguisher.currentFuel) + "%");
        }
        else
        {
            StopSpray();

            if (currentExtinguisher.currentFuel > 0f)
                SetPrompt("Segure clique para usar o extintor");
            else
                SetPrompt("Extintor vazio");
        }
    }

    void PickupExtinguisher(FactoryExtinguisherMarker marker)
    {
        currentExtinguisher = marker;
        carryingExtinguisher = true;

        marker.SetPicked(true);

        CreateHeldVisual(marker);

        if (showDebugLogs)
            Debug.Log("[FactoryExtinguisherPlayer] Extintor pego: " + marker.name, marker);
    }

    void CreateHeldVisual(FactoryExtinguisherMarker marker)
    {
        ClearHeldVisualOnly();

        if (marker.visualRoot != null)
        {
            heldVisualInstance = Instantiate(marker.visualRoot, handHolder);
            heldVisualInstance.name = "Held_Extinguisher_Visual";
            heldVisualInstance.SetActive(true);

            heldVisualInstance.transform.localPosition = Vector3.zero;
            heldVisualInstance.transform.localRotation = Quaternion.identity;
            heldVisualInstance.transform.localScale = Vector3.one * 0.85f;
        }
        else
        {
            heldVisualInstance = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            heldVisualInstance.name = "Held_Extinguisher_Fallback";
            heldVisualInstance.transform.SetParent(handHolder, false);
            heldVisualInstance.transform.localPosition = Vector3.zero;
            heldVisualInstance.transform.localRotation = Quaternion.identity;
            heldVisualInstance.transform.localScale = new Vector3(0.12f, 0.42f, 0.12f);

            Collider col = heldVisualInstance.GetComponent<Collider>();
            if (col != null)
                Destroy(col);
        }
    }

    void Spray()
    {
        if (currentExtinguisher == null)
            return;

        currentExtinguisher.currentFuel -= fuelUsePerSecond * Time.deltaTime;
        currentExtinguisher.currentFuel = Mathf.Max(0f, currentExtinguisher.currentFuel);

        if (sprayParticles != null && !sprayParticles.isPlaying)
            sprayParticles.Play();

        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, extinguishDistance, ~0, QueryTriggerInteraction.Collide))
        {
            FactoryFireHazard hazard = hit.collider.GetComponentInParent<FactoryFireHazard>();

            if (hazard != null && hazard.IsBurning())
                hazard.Extinguish(extinguishPowerPerSecond * Time.deltaTime);
        }

        if (currentExtinguisher.currentFuel <= 0f)
            StopSpray();
    }

    void StopSpray()
    {
        if (sprayParticles != null && sprayParticles.isPlaying)
            sprayParticles.Stop();
    }

    void DropExtinguisher()
    {
        if (currentExtinguisher != null)
        {
            currentExtinguisher.SetPicked(false);
            currentExtinguisher = null;
        }

        ClearHeldExtinguisher();

        if (showDebugLogs)
            Debug.Log("[FactoryExtinguisherPlayer] Extintor largado.", this);
    }

    void ClearHeldExtinguisher()
    {
        carryingExtinguisher = false;
        StopSpray();
        ClearHeldVisualOnly();
        SetPrompt("");
    }

    void ClearHeldVisualOnly()
    {
        if (heldVisualInstance != null)
        {
            Destroy(heldVisualInstance);
            heldVisualInstance = null;
        }
    }

    void EnsureHandHolder()
    {
        if (handHolder != null)
            return;

        if (playerCamera == null)
            return;

        GameObject holder = new GameObject("Extinguisher_Hand_Holder");
        holder.transform.SetParent(playerCamera.transform, false);
        holder.transform.localPosition = new Vector3(0.42f, -0.42f, 0.72f);
        holder.transform.localRotation = Quaternion.Euler(8f, -14f, -8f);
        holder.transform.localScale = Vector3.one;

        handHolder = holder.transform;
    }

    ParticleSystem CreateSprayParticles()
    {
        EnsureHandHolder();

        GameObject obj = new GameObject("Extinguisher_Spray_Particles");
        obj.transform.SetParent(handHolder != null ? handHolder : transform, false);
        obj.transform.localPosition = new Vector3(0f, 0.35f, 0.45f);
        obj.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);

        ParticleSystem ps = obj.AddComponent<ParticleSystem>();

        var main = ps.main;
        main.loop = true;
        main.startLifetime = 0.55f;
        main.startSpeed = 7f;
        main.startSize = 0.12f;
        main.startColor = new Color(0.92f, 0.92f, 0.92f, 0.65f);
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = ps.emission;
        emission.rateOverTime = 95f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 9f;
        shape.radius = 0.06f;

        var color = ps.colorOverLifetime;
        color.enabled = true;

        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[]
            {
                new GradientColorKey(Color.white, 0f),
                new GradientColorKey(new Color(0.75f, 0.75f, 0.75f), 1f)
            },
            new GradientAlphaKey[]
            {
                new GradientAlphaKey(0.0f, 0f),
                new GradientAlphaKey(0.85f, 0.08f),
                new GradientAlphaKey(0.0f, 1f)
            }
        );

        color.color = gradient;

        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        return ps;
    }

    void SetPrompt(string value)
    {
        if (tmpPromptText != null)
            tmpPromptText.text = value;

        if (uiPromptText != null)
            uiPromptText.text = value;
    }
}
