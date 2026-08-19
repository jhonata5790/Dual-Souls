
using UnityEngine;
using UnityEngine.Rendering;

[DisallowMultipleComponent]
public class DayNightLightOnly : MonoBehaviour
{
    [Header("Tempo")]
    [Range(0f, 24f)] public float currentHour = 12f;
    [Tooltip("24 = 24 horas do jogo em 24 minutos reais.")]
    public float fullDayDurationMinutes = 24f;
    public bool timeRunning = true;

    [Header("Luzes principais")]
    public Light sunLight;
    public Light moonLight;
    public bool createMoonLightIfMissing = true;

    [Header("Horários")]
    [Range(0f, 24f)] public float sunriseStart = 5f;
    [Range(0f, 24f)] public float dayStart = 7f;
    [Range(0f, 24f)] public float sunsetStart = 17.5f;
    [Range(0f, 24f)] public float nightStart = 19f;

    [Header("Sol")]
    public float sunDayIntensity = 1.15f;
    public float sunNightIntensity = 0f;
    public Color sunDayColor = new Color(1f, 0.95f, 0.82f);
    public Color sunSunriseColor = new Color(1f, 0.58f, 0.32f);
    public Color sunSunsetColor = new Color(1f, 0.42f, 0.22f);

    [Header("Lua")]
    public float moonNightIntensity = 0.18f;
    public float moonDayIntensity = 0f;
    public Color moonColor = new Color(0.42f, 0.52f, 0.85f);

    [Header("Ambiente")]
    public bool controlAmbientLight = true;
    public Color ambientDayColor = new Color(0.58f, 0.60f, 0.64f);
    public Color ambientSunriseColor = new Color(0.38f, 0.28f, 0.23f);
    public Color ambientNightColor = new Color(0.018f, 0.024f, 0.045f);

    [Header("Reflexos")]
    public bool controlReflectionIntensity = true;
    [Range(0f, 1f)] public float reflectionDayIntensity = 0.55f;
    [Range(0f, 1f)] public float reflectionNightIntensity = 0.03f;

    [Header("Fog opcional")]
    public bool controlFog = true;
    public bool fogEnabled = true;
    public Color fogDayColor = new Color(0.62f, 0.72f, 0.86f);
    public Color fogSunriseColor = new Color(0.58f, 0.35f, 0.24f);
    public Color fogNightColor = new Color(0.01f, 0.014f, 0.035f);
    public float fogDayDensity = 0.004f;
    public float fogNightDensity = 0.018f;

    [Header("Sombras")]
    public LightShadows sunShadows = LightShadows.Soft;
    public LightShadows moonShadows = LightShadows.Soft;

    [Header("Debug")]
    public bool applyInEditMode = false;
    public bool showDebugLogs = false;

    public float DayFactor { get; private set; }
    public float NightFactor { get; private set; }
    public bool IsDay { get { return DayFactor > 0.65f; } }
    public bool IsNight { get { return DayFactor < 0.15f; } }

    void Reset()
    {
        sunLight = RenderSettings.sun;

        if (sunLight == null)
        {
            Light[] lights = FindObjectsOfType<Light>();
            foreach (Light light in lights)
            {
                if (light != null && light.type == LightType.Directional)
                {
                    sunLight = light;
                    break;
                }
            }
        }
    }

    void Awake()
    {
        SetupReferences();
        ApplyLighting();
    }

    void Start()
    {
        SetupReferences();
        ApplyLighting();
    }

    void Update()
    {
        if (!Application.isPlaying && !applyInEditMode)
            return;

        if (Application.isPlaying && timeRunning)
        {
            float daySeconds = Mathf.Max(1f, fullDayDurationMinutes * 60f);
            currentHour += (24f / daySeconds) * Time.deltaTime;

            if (currentHour >= 24f)
                currentHour -= 24f;
        }

        ApplyLighting();
    }

    [ContextMenu("Setup References")]
    public void SetupReferences()
    {
        if (sunLight == null)
            sunLight = RenderSettings.sun;

        if (sunLight == null)
        {
            Light[] lights = FindObjectsOfType<Light>();
            foreach (Light light in lights)
            {
                if (light != null && light.type == LightType.Directional)
                {
                    sunLight = light;
                    break;
                }
            }
        }

        if (sunLight != null)
        {
            sunLight.type = LightType.Directional;
            sunLight.shadows = sunShadows;
            RenderSettings.sun = sunLight;
        }

        if (moonLight == null && createMoonLightIfMissing)
            CreateMoonLight();

        if (moonLight != null)
        {
            moonLight.type = LightType.Directional;
            moonLight.color = moonColor;
            moonLight.shadows = moonShadows;
        }
    }

    void CreateMoonLight()
    {
        GameObject moonObj = new GameObject("Moon_Light_Generated");
        moonObj.transform.SetParent(transform, false);

        moonLight = moonObj.AddComponent<Light>();
        moonLight.type = LightType.Directional;
        moonLight.color = moonColor;
        moonLight.intensity = moonNightIntensity;
        moonLight.shadows = moonShadows;
    }

    [ContextMenu("Apply Lighting Now")]
    public void ApplyLighting()
    {
        currentHour = NormalizeHour(currentHour);

        DayFactor = CalculateDayFactor(currentHour);
        NightFactor = 1f - DayFactor;

        float sunriseFactor = CalculateSunriseFactor(currentHour);
        float sunsetFactor = CalculateSunsetFactor(currentHour);

        ApplySun(sunriseFactor, sunsetFactor);
        ApplyMoon();
        ApplyAmbient(sunriseFactor, sunsetFactor);
        ApplyReflection();
        ApplyFog(sunriseFactor, sunsetFactor);

        if (showDebugLogs)
            Debug.Log("[DayNightLightOnly] Hora: " + currentHour.ToString("00.00") + " | DayFactor: " + DayFactor.ToString("0.00"), this);
    }

    void ApplySun(float sunriseFactor, float sunsetFactor)
    {
        if (sunLight == null)
            return;

        sunLight.intensity = Mathf.Lerp(sunNightIntensity, sunDayIntensity, DayFactor);
        sunLight.enabled = sunLight.intensity > 0.01f;

        Color targetColor = sunDayColor;

        if (sunriseFactor > sunsetFactor)
            targetColor = Color.Lerp(sunDayColor, sunSunriseColor, sunriseFactor);
        else
            targetColor = Color.Lerp(sunDayColor, sunSunsetColor, sunsetFactor);

        sunLight.color = targetColor;
        sunLight.transform.rotation = Quaternion.Euler(GetCelestialAngle(currentHour), 170f, 0f);
        sunLight.shadows = sunShadows;
    }

    void ApplyMoon()
    {
        if (moonLight == null)
            return;

        moonLight.intensity = Mathf.Lerp(moonDayIntensity, moonNightIntensity, NightFactor);
        moonLight.enabled = moonLight.intensity > 0.005f;
        moonLight.color = moonColor;
        moonLight.transform.rotation = Quaternion.Euler(GetCelestialAngle(currentHour + 12f), 170f, 0f);
        moonLight.shadows = moonShadows;
    }

    void ApplyAmbient(float sunriseFactor, float sunsetFactor)
    {
        if (!controlAmbientLight)
            return;

        RenderSettings.ambientMode = AmbientMode.Flat;

        Color ambient = Color.Lerp(ambientNightColor, ambientDayColor, DayFactor);

        float transitionFactor = Mathf.Max(sunriseFactor, sunsetFactor);
        ambient = Color.Lerp(ambient, ambientSunriseColor, transitionFactor * 0.75f);

        RenderSettings.ambientLight = ambient;
    }

    void ApplyReflection()
    {
        if (!controlReflectionIntensity)
            return;

        RenderSettings.reflectionIntensity = Mathf.Lerp(reflectionNightIntensity, reflectionDayIntensity, DayFactor);
    }

    void ApplyFog(float sunriseFactor, float sunsetFactor)
    {
        if (!controlFog)
            return;

        RenderSettings.fog = fogEnabled;
        RenderSettings.fogMode = FogMode.ExponentialSquared;

        Color fogColor = Color.Lerp(fogNightColor, fogDayColor, DayFactor);

        float transitionFactor = Mathf.Max(sunriseFactor, sunsetFactor);
        fogColor = Color.Lerp(fogColor, fogSunriseColor, transitionFactor * 0.7f);

        RenderSettings.fogColor = fogColor;
        RenderSettings.fogDensity = Mathf.Lerp(fogNightDensity, fogDayDensity, DayFactor);
    }

    float CalculateDayFactor(float hour)
    {
        hour = NormalizeHour(hour);

        if (hour >= dayStart && hour <= sunsetStart)
            return 1f;

        if (hour >= sunriseStart && hour < dayStart)
            return Smooth01(Mathf.InverseLerp(sunriseStart, dayStart, hour));

        if (hour > sunsetStart && hour < nightStart)
            return 1f - Smooth01(Mathf.InverseLerp(sunsetStart, nightStart, hour));

        return 0f;
    }

    float CalculateSunriseFactor(float hour)
    {
        if (hour < sunriseStart || hour > dayStart)
            return 0f;

        float mid = (sunriseStart + dayStart) * 0.5f;
        float distance = Mathf.Abs(hour - mid);
        float halfRange = Mathf.Max(0.01f, (dayStart - sunriseStart) * 0.5f);

        return 1f - Mathf.Clamp01(distance / halfRange);
    }

    float CalculateSunsetFactor(float hour)
    {
        if (hour < sunsetStart || hour > nightStart)
            return 0f;

        float mid = (sunsetStart + nightStart) * 0.5f;
        float distance = Mathf.Abs(hour - mid);
        float halfRange = Mathf.Max(0.01f, (nightStart - sunsetStart) * 0.5f);

        return 1f - Mathf.Clamp01(distance / halfRange);
    }

    float GetCelestialAngle(float hour)
    {
        hour = NormalizeHour(hour);
        return (hour / 24f) * 360f - 90f;
    }

    float NormalizeHour(float hour)
    {
        while (hour < 0f)
            hour += 24f;

        while (hour >= 24f)
            hour -= 24f;

        return hour;
    }

    float Smooth01(float t)
    {
        t = Mathf.Clamp01(t);
        return t * t * (3f - 2f * t);
    }

    [ContextMenu("Set Time 00:00 Night")]
    public void SetNight()
    {
        currentHour = 0f;
        ApplyLighting();
    }

    [ContextMenu("Set Time 05:00 Sunrise")]
    public void SetSunrise()
    {
        currentHour = 5f;
        ApplyLighting();
    }

    [ContextMenu("Set Time 12:00 Noon")]
    public void SetNoon()
    {
        currentHour = 12f;
        ApplyLighting();
    }

    [ContextMenu("Set Time 18:00 Sunset")]
    public void SetSunset()
    {
        currentHour = 18f;
        ApplyLighting();
    }

    [ContextMenu("Set Time 21:00 Night")]
    public void SetEveningNight()
    {
        currentHour = 21f;
        ApplyLighting();
    }
}
