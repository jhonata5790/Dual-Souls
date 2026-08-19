
using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
public class FactoryFireHazard : MonoBehaviour
{
    public enum HazardState
    {
        Inactive,
        Smoking,
        Burning,
        Extinguished
    }

    [Header("Estado")]
    public HazardState currentState = HazardState.Inactive;

    [Header("Partículas")]
    public ParticleSystem smokeParticles;
    public ParticleSystem fireParticles;

    [Tooltip("Cria fumaça e fogo automaticamente por script.")]
    public bool autoCreateParticles = true;

    [Tooltip("Recria as partículas geradas do zero quando usar o menu de contexto.")]
    public bool clearGeneratedParticlesBeforeCreate = true;

    [Header("Collider para mirar")]
    public bool createAimCollider = true;
    public float aimColliderRadius = 1.1f;
    public Vector3 aimColliderCenter = new Vector3(0f, 0.8f, 0f);

    [Header("Tempo")]
    [Tooltip("Tempo de fumaça antes de virar fogo.")]
    public float smokeTimeBeforeFire = 8f;

    [Header("Fogo")]
    public float maxFireHealth = 100f;
    public float fireHealth = 100f;

    [Tooltip("Quanto maior, mais rápido o extintor apaga esse fogo.")]
    public float extinguishMultiplier = 1f;

    [Tooltip("Se ligado, depois de apagado não volta a pegar fogo sozinho.")]
    public bool stayExtinguished = true;

    [Header("Visual da fumaça")]
    public Color smokeColor = new Color(0.32f, 0.34f, 0.34f, 0.5f);
    public float smokeRate = 22f;
    public float smokeSize = 0.75f;
    public float smokeLifetime = 2.6f;
    public float smokeSpeed = 0.75f;

    [Header("Visual do fogo")]
    public Color fireStartColor = new Color(1f, 0.12f, 0.02f, 0.95f);
    public Color fireMidColor = new Color(1f, 0.75f, 0.05f, 0.9f);
    public float fireRate = 55f;
    public float fireSize = 1.05f;
    public float fireLifetime = 0.55f;
    public float fireSpeed = 1.25f;

    [Header("Eventos")]
    public UnityEvent onSmokeStarted;
    public UnityEvent onFireStarted;
    public UnityEvent onExtinguished;

    [Header("Debug")]
    public bool showDebugLogs = false;

    private float smokeTimer;
    private bool hasBeenExtinguished;

    private const string GENERATED_SMOKE_NAME = "Smoke_Particles_Generated";
    private const string GENERATED_FIRE_NAME = "Fire_Particles_Generated";

    void Reset()
    {
        SetupCollider();
    }

    void Awake()
    {
        if (autoCreateParticles)
            EnsureParticles();

        SetupCollider();
        ApplyVisualState();
    }

    void Start()
    {
        if (autoCreateParticles)
            EnsureParticles();

        ApplyVisualState();
    }

    void Update()
    {
        if (currentState == HazardState.Smoking)
        {
            smokeTimer += Time.deltaTime;

            if (smokeTimer >= smokeTimeBeforeFire)
                StartFire();
        }
    }

    [ContextMenu("Rebuild Procedural Particles")]
    public void RebuildProceduralParticles()
    {
        if (clearGeneratedParticlesBeforeCreate)
            ClearGeneratedParticles();

        smokeParticles = CreateSmokeParticles();
        fireParticles = CreateFireParticles();

        ApplyVisualState();
    }

    [ContextMenu("Start Smoke")]
    public void StartSmoke()
    {
        if (stayExtinguished && hasBeenExtinguished)
            return;

        if (autoCreateParticles)
            EnsureParticles();

        currentState = HazardState.Smoking;
        smokeTimer = 0f;
        fireHealth = maxFireHealth;

        ApplyVisualState();
        onSmokeStarted?.Invoke();

        if (showDebugLogs)
            Debug.Log("[FactoryFireHazard] Fumaça iniciada: " + name, this);
    }

    [ContextMenu("Start Fire")]
    public void StartFire()
    {
        if (stayExtinguished && hasBeenExtinguished)
            return;

        if (autoCreateParticles)
            EnsureParticles();

        currentState = HazardState.Burning;
        fireHealth = maxFireHealth;

        ApplyVisualState();
        onFireStarted?.Invoke();

        if (showDebugLogs)
            Debug.Log("[FactoryFireHazard] Fogo iniciado: " + name, this);
    }

    public void Extinguish(float amount)
    {
        if (currentState != HazardState.Burning)
            return;

        fireHealth -= amount * Mathf.Max(0.01f, extinguishMultiplier);
        fireHealth = Mathf.Max(0f, fireHealth);

        UpdateFireParticleStrength();

        if (fireHealth <= 0f)
            ExtinguishCompletely();
    }

    [ContextMenu("Extinguish Completely")]
    public void ExtinguishCompletely()
    {
        currentState = HazardState.Extinguished;
        hasBeenExtinguished = true;
        fireHealth = 0f;

        ApplyVisualState();
        onExtinguished?.Invoke();

        if (showDebugLogs)
            Debug.Log("[FactoryFireHazard] Fogo apagado: " + name, this);
    }

    [ContextMenu("Reset Hazard")]
    public void ResetHazard()
    {
        currentState = HazardState.Inactive;
        smokeTimer = 0f;
        fireHealth = maxFireHealth;
        hasBeenExtinguished = false;

        ApplyVisualState();
    }

    public bool IsBurning()
    {
        return currentState == HazardState.Burning;
    }

    public bool IsSmoking()
    {
        return currentState == HazardState.Smoking;
    }

    public bool IsExtinguished()
    {
        return currentState == HazardState.Extinguished;
    }

    public bool IsSolved()
    {
        return currentState == HazardState.Extinguished || currentState == HazardState.Inactive;
    }

    void ApplyVisualState()
    {
        if (smokeParticles != null)
        {
            if (currentState == HazardState.Smoking || currentState == HazardState.Burning)
            {
                if (!smokeParticles.isPlaying)
                    smokeParticles.Play();
            }
            else
            {
                smokeParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }

        if (fireParticles != null)
        {
            if (currentState == HazardState.Burning)
            {
                if (!fireParticles.isPlaying)
                    fireParticles.Play();
            }
            else
            {
                fireParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }

        UpdateFireParticleStrength();
    }

    void UpdateFireParticleStrength()
    {
        if (fireParticles == null)
            return;

        float t = Mathf.Clamp01(fireHealth / Mathf.Max(1f, maxFireHealth));

        ParticleSystem.MainModule main = fireParticles.main;
        main.startSize = Mathf.Lerp(0.18f, fireSize, t);
        main.startLifetime = Mathf.Lerp(0.12f, fireLifetime, t);
        main.startSpeed = Mathf.Lerp(0.35f, fireSpeed, t);

        ParticleSystem.EmissionModule emission = fireParticles.emission;
        emission.rateOverTime = Mathf.Lerp(6f, fireRate, t);
    }

    void SetupCollider()
    {
        if (!createAimCollider)
            return;

        SphereCollider sphere = GetComponent<SphereCollider>();

        if (sphere == null)
            sphere = gameObject.AddComponent<SphereCollider>();

        sphere.isTrigger = true;
        sphere.radius = aimColliderRadius;
        sphere.center = aimColliderCenter;
    }

    void EnsureParticles()
    {
        if (smokeParticles == null)
        {
            Transform existingSmoke = transform.Find(GENERATED_SMOKE_NAME);
            if (existingSmoke != null)
                smokeParticles = existingSmoke.GetComponent<ParticleSystem>();

            if (smokeParticles == null)
                smokeParticles = CreateSmokeParticles();
        }

        if (fireParticles == null)
        {
            Transform existingFire = transform.Find(GENERATED_FIRE_NAME);
            if (existingFire != null)
                fireParticles = existingFire.GetComponent<ParticleSystem>();

            if (fireParticles == null)
                fireParticles = CreateFireParticles();
        }
    }

    ParticleSystem CreateSmokeParticles()
    {
        GameObject obj = new GameObject(GENERATED_SMOKE_NAME);
        obj.transform.SetParent(transform, false);
        obj.transform.localPosition = Vector3.up * 0.35f;
        obj.transform.localRotation = Quaternion.identity;
        obj.transform.localScale = Vector3.one;

        ParticleSystem ps = obj.AddComponent<ParticleSystem>();

        ParticleSystem.MainModule main = ps.main;
        main.loop = true;
        main.playOnAwake = false;
        main.startLifetime = smokeLifetime;
        main.startSpeed = smokeSpeed;
        main.startSize = smokeSize;
        main.startColor = smokeColor;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = 0f;

        ParticleSystem.EmissionModule emission = ps.emission;
        emission.enabled = true;
        emission.rateOverTime = smokeRate;

        ParticleSystem.ShapeModule shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 16f;
        shape.radius = 0.22f;

        ParticleSystem.ColorOverLifetimeModule color = ps.colorOverLifetime;
        color.enabled = true;
        color.color = BuildSmokeGradient();

        ParticleSystem.SizeOverLifetimeModule size = ps.sizeOverLifetime;
        size.enabled = true;
        AnimationCurve smokeSizeCurve = new AnimationCurve();
        smokeSizeCurve.AddKey(0f, 0.25f);
        smokeSizeCurve.AddKey(0.35f, 0.85f);
        smokeSizeCurve.AddKey(1f, 1.6f);
        size.size = new ParticleSystem.MinMaxCurve(1f, smokeSizeCurve);

        ParticleSystem.NoiseModule noise = ps.noise;
        noise.enabled = true;
        noise.strength = 0.35f;
        noise.frequency = 0.45f;
        noise.scrollSpeed = 0.25f;

        ParticleSystemRenderer renderer = ps.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.material = CreateParticleMaterial("Smoke_Procedural_Mat", new Color(0.55f, 0.55f, 0.55f, 0.45f));
        renderer.sortingFudge = 0.1f;

        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        return ps;
    }

    ParticleSystem CreateFireParticles()
    {
        GameObject obj = new GameObject(GENERATED_FIRE_NAME);
        obj.transform.SetParent(transform, false);
        obj.transform.localPosition = Vector3.up * 0.15f;
        obj.transform.localRotation = Quaternion.identity;
        obj.transform.localScale = Vector3.one;

        ParticleSystem ps = obj.AddComponent<ParticleSystem>();

        ParticleSystem.MainModule main = ps.main;
        main.loop = true;
        main.playOnAwake = false;
        main.startLifetime = fireLifetime;
        main.startSpeed = fireSpeed;
        main.startSize = fireSize;
        main.startColor = fireStartColor;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = -0.05f;

        ParticleSystem.EmissionModule emission = ps.emission;
        emission.enabled = true;
        emission.rateOverTime = fireRate;

        ParticleSystem.ShapeModule shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 22f;
        shape.radius = 0.24f;

        ParticleSystem.ColorOverLifetimeModule color = ps.colorOverLifetime;
        color.enabled = true;
        color.color = BuildFireGradient();

        ParticleSystem.SizeOverLifetimeModule size = ps.sizeOverLifetime;
        size.enabled = true;
        AnimationCurve fireSizeCurve = new AnimationCurve();
        fireSizeCurve.AddKey(0f, 0.1f);
        fireSizeCurve.AddKey(0.2f, 1f);
        fireSizeCurve.AddKey(1f, 0f);
        size.size = new ParticleSystem.MinMaxCurve(1f, fireSizeCurve);

        ParticleSystem.NoiseModule noise = ps.noise;
        noise.enabled = true;
        noise.strength = 0.18f;
        noise.frequency = 1.25f;
        noise.scrollSpeed = 0.65f;

        ParticleSystemRenderer renderer = ps.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.material = CreateParticleMaterial("Fire_Procedural_Mat", new Color(1f, 0.25f, 0.02f, 0.85f));
        renderer.sortingFudge = 0.2f;

        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        return ps;
    }

    ParticleSystem.MinMaxGradient BuildSmokeGradient()
    {
        Gradient gradient = new Gradient();

        gradient.SetKeys(
            new GradientColorKey[]
            {
                new GradientColorKey(new Color(0.18f, 0.18f, 0.18f), 0f),
                new GradientColorKey(new Color(0.45f, 0.45f, 0.45f), 0.55f),
                new GradientColorKey(new Color(0.62f, 0.62f, 0.62f), 1f)
            },
            new GradientAlphaKey[]
            {
                new GradientAlphaKey(0.0f, 0f),
                new GradientAlphaKey(0.55f, 0.18f),
                new GradientAlphaKey(0.25f, 0.65f),
                new GradientAlphaKey(0.0f, 1f)
            }
        );

        return new ParticleSystem.MinMaxGradient(gradient);
    }

    ParticleSystem.MinMaxGradient BuildFireGradient()
    {
        Gradient gradient = new Gradient();

        gradient.SetKeys(
            new GradientColorKey[]
            {
                new GradientColorKey(fireStartColor, 0f),
                new GradientColorKey(fireMidColor, 0.42f),
                new GradientColorKey(new Color(0.18f, 0.02f, 0.0f), 1f)
            },
            new GradientAlphaKey[]
            {
                new GradientAlphaKey(0.0f, 0f),
                new GradientAlphaKey(1.0f, 0.08f),
                new GradientAlphaKey(0.9f, 0.45f),
                new GradientAlphaKey(0.0f, 1f)
            }
        );

        return new ParticleSystem.MinMaxGradient(gradient);
    }

    Material CreateParticleMaterial(string matName, Color tint)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");

        if (shader == null)
            shader = Shader.Find("Particles/Standard Unlit");

        if (shader == null)
            shader = Shader.Find("Sprites/Default");

        if (shader == null)
            shader = Shader.Find("Unlit/Transparent");

        Material mat = new Material(shader);
        mat.name = matName;

        if (mat.HasProperty("_BaseColor"))
            mat.SetColor("_BaseColor", tint);

        if (mat.HasProperty("_Color"))
            mat.SetColor("_Color", tint);

        if (mat.HasProperty("_Surface"))
            mat.SetFloat("_Surface", 1f);

        if (mat.HasProperty("_Blend"))
            mat.SetFloat("_Blend", 0f);

        mat.renderQueue = 3000;

        return mat;
    }

    [ContextMenu("Clear Generated Particles")]
    public void ClearGeneratedParticles()
    {
        DeleteChildByName(GENERATED_SMOKE_NAME);
        DeleteChildByName(GENERATED_FIRE_NAME);

        smokeParticles = null;
        fireParticles = null;
    }

    void DeleteChildByName(string childName)
    {
        Transform child = transform.Find(childName);

        if (child == null)
            return;

        if (Application.isPlaying)
            Destroy(child.gameObject);
        else
            DestroyImmediate(child.gameObject);
    }
}
