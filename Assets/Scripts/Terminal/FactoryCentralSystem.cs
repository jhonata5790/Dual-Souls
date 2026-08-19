
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
public class FactoryCentralSystem : MonoBehaviour
{
    [Header("Estado inicial da fábrica")]
    public bool mainPowerOn = true;
    public bool interiorLightsOn = true;
    public bool cargoGateOpen = false;
    public bool securityModeActive = false;

    [Header("Referências principais")]
    [Tooltip("Raiz da fábrica. Normalmente é o objeto Fabrica.")]
    public Transform factoryRoot;

    [Tooltip("Objeto que tem o script do portão grande. Pode ser o próprio portão ou o controller dele.")]
    public GameObject cargoGateControllerObject;

    [Tooltip("Componente do FactoryLightingTuner. Pode arrastar o objeto/componente aqui.")]
    public MonoBehaviour lightingTuner;

    [Tooltip("Opcional: luzes específicas da fábrica. Se vazio, o sistema tenta achar sozinho.")]
    public List<Light> factoryLights = new List<Light>();

    [Header("Regras")]
    [Tooltip("Se ligado, o portão grande só abre com energia geral ligada.")]
    public bool gateRequiresPower = true;

    [Tooltip("Se ligado, o portão grande não abre em modo segurança.")]
    public bool gateBlockedBySecurity = true;

    [Tooltip("Se ligado, as luzes só podem ligar com energia geral ligada.")]
    public bool lightsRequirePower = true;

    [Tooltip("Se ligado, desligar a energia geral também apaga as luzes.")]
    public bool powerOffTurnsLightsOff = true;

    [Header("Mensagens do terminal")]
    [TextArea(2, 4)]
    public string lastSystemMessage = "Terminal aguardando comando.";

    [Header("Eventos")]
    public UnityEvent onStatusChanged;
    public UnityEvent onPowerTurnedOn;
    public UnityEvent onPowerTurnedOff;
    public UnityEvent onLightsTurnedOn;
    public UnityEvent onLightsTurnedOff;
    public UnityEvent onGateOpened;
    public UnityEvent onGateClosed;
    public UnityEvent onSecurityEnabled;
    public UnityEvent onSecurityDisabled;

    [Header("Debug")]
    public bool showDebugLogs = false;

    private readonly Dictionary<Light, bool> savedLightStates = new Dictionary<Light, bool>();

    void Reset()
    {
        TryAutoFindReferences();
    }

    void Awake()
    {
        TryAutoFindReferences();
        CacheInitialLightStates();
        ApplyAllStates(false);
    }

    void Start()
    {
        ApplyAllStates(false);
        NotifyStatusChanged();
    }

    [ContextMenu("Auto Find References")]
    public void TryAutoFindReferences()
    {
        if (factoryRoot == null)
        {
            GameObject f = GameObject.Find("Fabrica");
            if (f == null) f = GameObject.Find("Factory");
            if (f != null) factoryRoot = f.transform;
        }

        if (lightingTuner == null)
        {
            MonoBehaviour[] behaviours = FindObjectsOfType<MonoBehaviour>(true);
            foreach (MonoBehaviour b in behaviours)
            {
                if (b == null) continue;

                string typeName = b.GetType().Name;
                if (typeName == "FactoryLightingTuner")
                {
                    lightingTuner = b;
                    break;
                }
            }
        }

        if (cargoGateControllerObject == null)
        {
            MonoBehaviour[] behaviours = FindObjectsOfType<MonoBehaviour>(true);
            foreach (MonoBehaviour b in behaviours)
            {
                if (b == null) continue;

                string typeName = b.GetType().Name.ToLowerInvariant();
                string objName = b.gameObject.name.ToLowerInvariant();

                if (typeName.Contains("loadinggate") || typeName.Contains("cargogate") ||
                    objName.Contains("loading") || objName.Contains("cargo") || objName.Contains("gate") || objName.Contains("portao") || objName.Contains("portão"))
                {
                    cargoGateControllerObject = b.gameObject;
                    break;
                }
            }
        }

        if (factoryLights.Count == 0)
            FindFactoryLights();
    }

    [ContextMenu("Find Factory Lights")]
    public void FindFactoryLights()
    {
        factoryLights.Clear();

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

            if (n.Contains("sun") || n.Contains("moon"))
                continue;

            if (n.Contains("terminal") || n.Contains("monitor") || n.Contains("pc"))
                continue;

            if (!factoryLights.Contains(light))
                factoryLights.Add(light);
        }

        CacheInitialLightStates();

        if (showDebugLogs)
            Debug.Log("[FactoryCentralSystem] Luzes da fábrica encontradas: " + factoryLights.Count, this);
    }

    void CacheInitialLightStates()
    {
        savedLightStates.Clear();

        foreach (Light light in factoryLights)
        {
            if (light == null)
                continue;

            savedLightStates[light] = light.enabled;
        }
    }

    [ContextMenu("Apply All States")]
    public void ApplyAllStatesFromInspector()
    {
        ApplyAllStates(true);
    }

    void ApplyAllStates(bool sendEvents)
    {
        ApplyPowerState(sendEvents);
        ApplyLightsState(sendEvents);
        ApplySecurityState(sendEvents);
    }

    public bool CanOperateCargoGate()
    {
        if (gateRequiresPower && !mainPowerOn)
            return false;

        if (gateBlockedBySecurity && securityModeActive)
            return false;

        return true;
    }

    public bool CanOperateLights()
    {
        if (lightsRequirePower && !mainPowerOn)
            return false;

        return true;
    }

    public void ToggleMainPower()
    {
        SetMainPower(!mainPowerOn);
    }

    public void SetMainPower(bool enabled)
    {
        if (mainPowerOn == enabled)
        {
            lastSystemMessage = enabled ? "Energia geral já está ligada." : "Energia geral já está desligada.";
            NotifyStatusChanged();
            return;
        }

        mainPowerOn = enabled;

        if (!mainPowerOn && powerOffTurnsLightsOff)
            interiorLightsOn = false;

        ApplyPowerState(true);
        ApplyLightsState(true);

        lastSystemMessage = mainPowerOn ? "Energia geral ligada." : "Energia geral desligada.";

        if (showDebugLogs)
            Debug.Log("[FactoryCentralSystem] Energia geral: " + mainPowerOn, this);

        NotifyStatusChanged();
    }

    void ApplyPowerState(bool sendEvents)
    {
        if (sendEvents)
        {
            if (mainPowerOn)
                onPowerTurnedOn?.Invoke();
            else
                onPowerTurnedOff?.Invoke();
        }
    }

    public void ToggleInteriorLights()
    {
        SetInteriorLights(!interiorLightsOn);
    }

    public void SetInteriorLights(bool enabled)
    {
        if (enabled && !CanOperateLights())
        {
            lastSystemMessage = "Luzes bloqueadas: energia geral desligada.";
            NotifyStatusChanged();
            return;
        }

        if (interiorLightsOn == enabled)
        {
            lastSystemMessage = enabled ? "Luzes internas já estão ligadas." : "Luzes internas já estão desligadas.";
            NotifyStatusChanged();
            return;
        }

        interiorLightsOn = enabled;
        ApplyLightsState(true);

        lastSystemMessage = interiorLightsOn ? "Luzes internas ligadas." : "Luzes internas desligadas.";

        if (showDebugLogs)
            Debug.Log("[FactoryCentralSystem] Luzes internas: " + interiorLightsOn, this);

        NotifyStatusChanged();
    }

    void ApplyLightsState(bool sendEvents)
    {
        bool shouldBeOn = mainPowerOn && interiorLightsOn;

        bool tunerWorked = false;

        if (lightingTuner != null)
        {
            if (shouldBeOn)
                tunerWorked = TryInvokeAnyMethod(lightingTuner.gameObject, false, "TurnTunedFactoryLightsOn", "TurnFactoryLightsOn", "LightsOn");
            else
                tunerWorked = TryInvokeAnyMethod(lightingTuner.gameObject, false, "TurnTunedFactoryLightsOff", "TurnFactoryLightsOff", "LightsOff");
        }

        if (!tunerWorked)
        {
            foreach (Light light in factoryLights)
            {
                if (light == null)
                    continue;

                light.enabled = shouldBeOn;
            }
        }

        if (sendEvents)
        {
            if (shouldBeOn)
                onLightsTurnedOn?.Invoke();
            else
                onLightsTurnedOff?.Invoke();
        }
    }

    public void ToggleCargoGate()
    {
        SetCargoGateOpen(!cargoGateOpen);
    }

    public void SetCargoGateOpen(bool open)
    {
        if (open && !CanOperateCargoGate())
        {
            if (!mainPowerOn)
                lastSystemMessage = "Portão bloqueado: energia geral desligada.";
            else if (securityModeActive)
                lastSystemMessage = "Portão bloqueado: modo segurança ativo.";
            else
                lastSystemMessage = "Portão bloqueado.";

            NotifyStatusChanged();
            return;
        }

        if (cargoGateOpen == open)
        {
            lastSystemMessage = open ? "Portão de carga já está aberto." : "Portão de carga já está fechado.";
            NotifyStatusChanged();
            return;
        }

        bool gateCommandWorked = TryControlCargoGate(open);

        cargoGateOpen = open;

        if (open)
        {
            lastSystemMessage = gateCommandWorked ? "Portão de carga abrindo." : "Portão marcado como aberto, mas nenhum controller respondeu.";
            onGateOpened?.Invoke();
        }
        else
        {
            lastSystemMessage = gateCommandWorked ? "Portão de carga fechando." : "Portão marcado como fechado, mas nenhum controller respondeu.";
            onGateClosed?.Invoke();
        }

        if (showDebugLogs)
            Debug.Log("[FactoryCentralSystem] Portão de carga aberto: " + cargoGateOpen, this);

        NotifyStatusChanged();
    }

    bool TryControlCargoGate(bool open)
    {
        if (cargoGateControllerObject == null)
            return false;

        if (TryInvokeBoolMethod(cargoGateControllerObject, open, "SetOpen", "SetGateOpen", "SetOpened", "ForceOpen"))
            return true;

        if (open)
        {
            if (TryInvokeAnyMethod(cargoGateControllerObject, true, "OpenGateNow", "OpenGate", "Open", "OpenDoor", "OpenCargoGate"))
                return true;
        }
        else
        {
            if (TryInvokeAnyMethod(cargoGateControllerObject, true, "CloseGateNow", "CloseGate", "Close", "CloseDoor", "CloseCargoGate"))
                return true;
        }

        // Fallback: scripts antigos normalmente têm Toggle/Interact.
        return TryInvokeAnyMethod(cargoGateControllerObject, true, "ToggleGate", "Toggle", "Interact", "Use");
    }


    [ContextMenu("TEST Open Cargo Gate")]
    public void TestOpenCargoGateFromInspector()
    {
        SetCargoGateOpen(true);
    }

    [ContextMenu("TEST Close Cargo Gate")]
    public void TestCloseCargoGateFromInspector()
    {
        SetCargoGateOpen(false);
    }

    public void ToggleSecurityMode()
    {
        SetSecurityMode(!securityModeActive);
    }

    public void SetSecurityMode(bool active)
    {
        if (active && !mainPowerOn)
        {
            lastSystemMessage = "Modo segurança bloqueado: energia geral desligada.";
            NotifyStatusChanged();
            return;
        }

        if (securityModeActive == active)
        {
            lastSystemMessage = active ? "Modo segurança já está ativo." : "Modo segurança já está inativo.";
            NotifyStatusChanged();
            return;
        }

        securityModeActive = active;
        ApplySecurityState(true);

        lastSystemMessage = securityModeActive ? "Modo segurança ativado." : "Modo segurança desativado.";

        if (showDebugLogs)
            Debug.Log("[FactoryCentralSystem] Modo segurança: " + securityModeActive, this);

        NotifyStatusChanged();
    }

    void ApplySecurityState(bool sendEvents)
    {
        if (sendEvents)
        {
            if (securityModeActive)
                onSecurityEnabled?.Invoke();
            else
                onSecurityDisabled?.Invoke();
        }
    }

    bool TryInvokeBoolMethod(GameObject target, bool value, params string[] methodNames)
    {
        if (target == null)
            return false;

        MonoBehaviour[] behaviours = target.GetComponentsInChildren<MonoBehaviour>(true);

        foreach (MonoBehaviour behaviour in behaviours)
        {
            if (behaviour == null)
                continue;

            Type type = behaviour.GetType();

            foreach (string methodName in methodNames)
            {
                MethodInfo method = type.GetMethod(
                    methodName,
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
                );

                if (method == null)
                    continue;

                ParameterInfo[] parameters = method.GetParameters();

                if (parameters.Length == 1 && parameters[0].ParameterType == typeof(bool))
                {
                    method.Invoke(behaviour, new object[] { value });
                    return true;
                }
            }
        }

        return false;
    }

    bool TryInvokeAnyMethod(GameObject target, bool logFailure, params string[] methodNames)
    {
        if (target == null)
            return false;

        MonoBehaviour[] behaviours = target.GetComponentsInChildren<MonoBehaviour>(true);

        foreach (MonoBehaviour behaviour in behaviours)
        {
            if (behaviour == null)
                continue;

            Type type = behaviour.GetType();

            foreach (string methodName in methodNames)
            {
                MethodInfo method = type.GetMethod(
                    methodName,
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
                );

                if (method == null)
                    continue;

                if (method.GetParameters().Length == 0)
                {
                    method.Invoke(behaviour, null);
                    return true;
                }
            }
        }

        if (logFailure && showDebugLogs)
            Debug.LogWarning("[FactoryCentralSystem] Nenhum método compatível encontrado em " + target.name, target);

        return false;
    }

    public string GetEnergyStatusText()
    {
        return mainPowerOn ? "ENERGIA: LIGADA" : "ENERGIA: DESLIGADA";
    }

    public string GetCargoGateStatusText()
    {
        if (!mainPowerOn && gateRequiresPower)
            return "PORTÃO: SEM ENERGIA";

        if (securityModeActive && gateBlockedBySecurity)
            return "PORTÃO: BLOQUEADO";

        return cargoGateOpen ? "PORTÃO: ABERTO" : "PORTÃO: FECHADO";
    }

    public string GetSecurityStatusText()
    {
        return securityModeActive ? "SEGURANÇA: ATIVA" : "SEGURANÇA: INATIVA";
    }

    public string GetLightsStatusText()
    {
        if (!mainPowerOn && lightsRequirePower)
            return "LUZES: SEM ENERGIA";

        return interiorLightsOn ? "LUZES: LIGADAS" : "LUZES: DESLIGADAS";
    }

    public string GetSystemSummary()
    {
        return GetEnergyStatusText() + "\n" +
               GetCargoGateStatusText() + "\n" +
               GetSecurityStatusText() + "\n" +
               GetLightsStatusText();
    }

    void NotifyStatusChanged()
    {
        onStatusChanged?.Invoke();
    }
}
