
using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

public class FactoryInteractionSystem_V8 : MonoBehaviour
{
    [Header("Referências")]
    public Camera playerCamera;
    public Transform factoryRoot;

    [Header("Raycast")]
    public float interactionDistance = 4f;
    public LayerMask interactionMask = ~0;
    public bool ignoreTriggers = false;

    [Header("Input")]
    public int mouseButton = 0;

    [Header("UI")]
    public Text promptText;
    public string defaultPrompt = "Clique esquerdo para interagir";

    [Header("Debug")]
    public bool showDebugLogs = false;
    public Transform currentHitTransform;
    public GameObject currentInteractableObject;

    public bool HasTarget { get; private set; }
    public string CurrentPrompt { get; private set; }

    object currentTarget;
    MethodInfo currentInteractMethod;

    void Reset()
    {
        playerCamera = Camera.main;
    }

    void Awake()
    {
        if (playerCamera == null)
            playerCamera = GetComponentInChildren<Camera>();

        if (playerCamera == null)
            playerCamera = Camera.main;

        if (promptText != null)
            promptText.gameObject.SetActive(false);
    }

    void Update()
    {
        ScanForInteractable();

        if (HasTarget && Input.GetMouseButtonDown(mouseButton))
        {
            InteractWithCurrentTarget();
        }
    }

    void ScanForInteractable()
    {
        ClearTarget();

        if (playerCamera == null)
            return;

        QueryTriggerInteraction triggerMode = ignoreTriggers
            ? QueryTriggerInteraction.Ignore
            : QueryTriggerInteraction.Collide;

        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);

        if (!Physics.Raycast(ray, out RaycastHit hit, interactionDistance, interactionMask, triggerMode))
        {
            HidePrompt();
            return;
        }

        currentHitTransform = hit.transform;

        if (showDebugLogs)
            Debug.Log("[FactoryInteractionSystem_V8] Raycast hit: " + hit.transform.name, hit.transform);

        object target = FindInteractableFromHit(hit.transform);

        if (target == null)
        {
            HidePrompt();
            return;
        }

        currentTarget = target;
        currentInteractableObject = GetGameObjectFromTarget(target);
        currentInteractMethod = FindInteractMethod(target);

        if (currentInteractMethod == null)
        {
            if (showDebugLogs)
                Debug.LogWarning("[FactoryInteractionSystem_V8] Target found, but no interaction method was found: " + target);
            HidePrompt();
            ClearTarget();
            return;
        }

        HasTarget = true;
        CurrentPrompt = GetPromptFromTarget(target);
        ShowPrompt(CurrentPrompt);
    }

    object FindInteractableFromHit(Transform hit)
    {
        if (hit == null)
            return null;

        // 1. Terminal do PC: procurar no próprio objeto, nos pais e filhos.
        object terminal = FindComponentByTypeNameAround(hit, "FactoryComputerTerminalFocus");
        if (terminal != null)
            return terminal;

        object terminalV6 = FindComponentByTypeNameAround(hit, "FactoryComputerTerminal_V6");
        if (terminalV6 != null)
            return terminalV6;

        // 2. Portas e portões já criados em versões anteriores.
        string[] knownTypeNames =
        {
            "FactoryLoadingGateController_V2",
            "FactoryLoadingGateController",
            "FactoryEmployeeDoorController",
            "FactoryControlRoomBuilder_V2",
            "FactoryControlRoomDoorController"
        };

        foreach (string typeName in knownTypeNames)
        {
            object found = FindComponentByTypeNameAround(hit, typeName);
            if (found != null)
                return found;
        }

        // 3. Fallback por nome, caso o script esteja no objeto atingido ou pai.
        Transform t = hit;
        while (t != null)
        {
            string n = t.name.ToLowerInvariant();

            if (n.Contains("monitor") || n.Contains("pc") || n.Contains("terminal"))
            {
                object foundTerminal = FindComponentByTypeNameAround(t, "FactoryComputerTerminalFocus");
                if (foundTerminal != null)
                    return foundTerminal;
            }

            if (n.Contains("loading_gate") || n.Contains("employee_door") || n.Contains("control_room_door"))
            {
                MonoBehaviour[] behaviours = t.GetComponentsInParent<MonoBehaviour>(true);
                foreach (MonoBehaviour mb in behaviours)
                {
                    if (mb == null)
                        continue;

                    MethodInfo method = FindInteractMethod(mb);
                    if (method != null)
                        return mb;
                }
            }

            t = t.parent;
        }

        return null;
    }

    object FindComponentByTypeNameAround(Transform hit, string typeName)
    {
        if (hit == null)
            return null;

        MonoBehaviour[] onSelfAndParents = hit.GetComponentsInParent<MonoBehaviour>(true);
        foreach (MonoBehaviour mb in onSelfAndParents)
        {
            if (mb == null)
                continue;

            if (mb.GetType().Name == typeName)
                return mb;
        }

        MonoBehaviour[] onChildren = hit.GetComponentsInChildren<MonoBehaviour>(true);
        foreach (MonoBehaviour mb in onChildren)
        {
            if (mb == null)
                continue;

            if (mb.GetType().Name == typeName)
                return mb;
        }

        return null;
    }

    MethodInfo FindInteractMethod(object target)
    {
        if (target == null)
            return null;

        Type type = target.GetType();

        string[] methodNames =
        {
            "Interact",
            "Use",
            "UseTerminal",
            "OpenTerminal",
            "ToggleTerminal",
            "Toggle",
            "ToggleDoor",
            "ToggleGate",
            "Open",
            "OpenDoor",
            "OpenGate"
        };

        foreach (string methodName in methodNames)
        {
            MethodInfo method = type.GetMethod(
                methodName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                null,
                Type.EmptyTypes,
                null
            );

            if (method != null)
                return method;
        }

        return null;
    }

    void InteractWithCurrentTarget()
    {
        if (currentTarget == null || currentInteractMethod == null)
            return;

        if (showDebugLogs)
            Debug.Log("[FactoryInteractionSystem_V8] Interacting with: " + currentTarget.GetType().Name);

        currentInteractMethod.Invoke(currentTarget, null);
    }

    string GetPromptFromTarget(object target)
    {
        if (target == null)
            return defaultPrompt;

        Type type = target.GetType();
        string typeName = type.Name.ToLowerInvariant();

        if (typeName.Contains("computer") || typeName.Contains("terminal"))
            return "Clique esquerdo para usar computador";

        bool isOpen = TryGetBool(target, "IsOpen", "isOpen", "Open", "opened");

        if (typeName.Contains("gate"))
            return isOpen ? "Clique esquerdo para fechar portão" : "Clique esquerdo para abrir portão";

        if (typeName.Contains("door") || typeName.Contains("builder"))
            return isOpen ? "Clique esquerdo para fechar porta" : "Clique esquerdo para abrir porta";

        return defaultPrompt;
    }

    bool TryGetBool(object target, params string[] names)
    {
        if (target == null)
            return false;

        Type type = target.GetType();

        foreach (string name in names)
        {
            FieldInfo field = type.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (field != null && field.FieldType == typeof(bool))
                return (bool)field.GetValue(target);

            PropertyInfo prop = type.GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (prop != null && prop.PropertyType == typeof(bool) && prop.CanRead)
                return (bool)prop.GetValue(target);
        }

        return false;
    }

    GameObject GetGameObjectFromTarget(object target)
    {
        if (target is Component c)
            return c.gameObject;

        return null;
    }

    void ShowPrompt(string message)
    {
        if (promptText == null)
            return;

        promptText.text = message;
        promptText.gameObject.SetActive(true);
    }

    void HidePrompt()
    {
        if (promptText == null)
            return;

        promptText.gameObject.SetActive(false);
    }

    void ClearTarget()
    {
        HasTarget = false;
        CurrentPrompt = "";
        currentTarget = null;
        currentInteractMethod = null;
        currentInteractableObject = null;
    }
}
