using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Controla a mecânica física do computador/terminal da sala de controle.
///
/// Este script NÃO cria PC, NÃO cria menu e NÃO abre nada automaticamente.
/// Ele apenas:
/// - recebe uma chamada de interação;
/// - trava o controle do jogador;
/// - anima a câmera do player até o Terminal_Camera_Target;
/// - ativa o Canvas/menu já criado por você;
/// - libera o cursor;
/// - fecha pelo botão "Desligar PC";
/// - anima a câmera de volta e devolve o controle.
///
/// Coloque este script no objeto PC ou Monitor.
/// </summary>
[DisallowMultipleComponent]
public class FactoryComputerTerminalFocus : MonoBehaviour
{
    [Header("Referências principais")]
    [Tooltip("Raiz do player. Exemplo: Player_FPS.")]
    public Transform playerRoot;

    [Tooltip("Câmera real do jogador.")]
    public Camera playerCamera;

    [Tooltip("Objeto usado pelo raycast. No seu caso: Monitor.")]
    public Transform monitorRaycastTarget;

    [Tooltip("Objeto vazio na frente da tela. A câmera do player anima até aqui.")]
    public Transform terminalCameraTarget;

    [Tooltip("Canvas/menu feito por você. Deve começar desativado.")]
    public GameObject terminalMenuCanvas;

    [Header("Comportamento")]
    public bool forceClosedOnStart = true;
    public bool openOnStart = false;
    public bool closeWithEscape = true;

    [Tooltip("Se verdadeiro, tenta achar referências automaticamente por nome.")]
    public bool autoFindReferences = true;

    [Tooltip("Se verdadeiro, o cursor aparece e destrava quando o menu abre.")]
    public bool unlockCursorWhenOpen = true;

    [Tooltip("Se verdadeiro, esconde a mira enquanto usa o terminal.")]
    public bool hideCrosshairWhileOpen = true;

    [Header("Animação da câmera")]
    [Min(0.05f)] public float moveToTerminalDuration = 0.75f;
    [Min(0.05f)] public float moveBackDuration = 0.55f;
    public AnimationCurve cameraCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Tooltip("Se verdadeiro, altera o FOV para encaixar melhor a tela.")]
    public bool animateFOV = true;
    public float terminalFOV = 45f;

    [Header("Bloqueio do player")]
    [Tooltip("Componentes arrastados aqui serão desativados enquanto o terminal estiver aberto.")]
    public Behaviour[] componentsToDisableWhileOpen;

    [Tooltip("Tenta desativar automaticamente scripts de movimento/olhar do player pelo nome.")]
    public bool autoDisablePlayerControlScripts = true;

    [Tooltip("Scripts do player cujo nome contenha estes textos serão desativados durante o terminal.")]
    public string[] autoDisableNameContains =
    {
        "Player_FPS",
        "PlayerFPS",
        "FirstPerson",
        "FPSController",
        "PlayerMovement",
        "Movement",
        "MouseLook",
        "CameraLook",
        "LookController"
    };

    [Header("UI extra")]
    [Tooltip("Objetos de UI que devem sumir enquanto o terminal está aberto. Opcional.")]
    public GameObject[] extraUIToHideWhileOpen;

    [Header("Debug")]
    public bool showDebugLogs;
    public bool isOpen;
    public bool isTransitioning;

    public static bool AnyTerminalOpen { get; private set; }

    private Transform originalCameraParent;
    private Vector3 originalLocalPosition;
    private Quaternion originalLocalRotation;
    private Vector3 originalWorldPosition;
    private Quaternion originalWorldRotation;
    private float originalFOV;

    private CursorLockMode previousLockMode;
    private bool previousCursorVisible;

    private readonly List<Behaviour> disabledBehaviours = new List<Behaviour>();
    private readonly List<GameObject> hiddenCrosshairObjects = new List<GameObject>();
    private readonly List<Behaviour> disabledCrosshairBehaviours = new List<Behaviour>();
    private readonly Dictionary<GameObject, bool> previousExtraUIStates = new Dictionary<GameObject, bool>();

    private Coroutine currentRoutine;

    private void Reset()
    {
        AutoAssignReferences();
    }

    private void Awake()
    {
        if (autoFindReferences)
            AutoAssignReferences();

        if (forceClosedOnStart && !openOnStart)
            ForceCloseImmediate();
    }

    private void Start()
    {
        if (autoFindReferences)
            AutoAssignReferences();

        if (openOnStart)
            OpenTerminal();
        else if (forceClosedOnStart)
            ForceCloseImmediate();
    }

    private void Update()
    {
        if (isOpen && closeWithEscape && Input.GetKeyDown(KeyCode.Escape))
            CloseTerminal();
    }

    /// <summary>
    /// Chamado pelo sistema de interação central quando o player mira no monitor e clica.
    /// </summary>
    [ContextMenu("Open Terminal")]
    public void OpenTerminal()
    {
        if (isOpen || isTransitioning)
            return;

        if (autoFindReferences)
            AutoAssignReferences();

        if (playerCamera == null || terminalCameraTarget == null)
        {
            Debug.LogWarning("[FactoryComputerTerminalFocus] Faltam referências: Player Camera ou Terminal Camera Target.", this);
            return;
        }

        if (currentRoutine != null)
            StopCoroutine(currentRoutine);

        currentRoutine = StartCoroutine(OpenRoutine());
    }

    /// <summary>
    /// Use este método no botão do menu: DESLIGAR PC / SAIR DO SISTEMA.
    /// </summary>
    public void ShutdownPC()
    {
        CloseTerminal();
    }

    /// <summary>
    /// Fecha o menu e volta a câmera para o jogador.
    /// </summary>
    [ContextMenu("Close Terminal")]
    public void CloseTerminal()
    {
        if ((!isOpen && !isTransitioning) || playerCamera == null)
            return;

        if (currentRoutine != null)
            StopCoroutine(currentRoutine);

        currentRoutine = StartCoroutine(CloseRoutine());
    }

    [ContextMenu("Force Close Immediate")]
    public void ForceCloseImmediate()
    {
        isOpen = false;
        isTransitioning = false;
        AnyTerminalOpen = false;

        if (terminalMenuCanvas != null)
            terminalMenuCanvas.SetActive(false);

        RestorePlayerControl();
        RestoreCrosshair();
        RestoreExtraUI();
    }

    [ContextMenu("Auto Assign References")]
    public void AutoAssignReferences()
    {
        if (playerCamera == null)
        {
            if (playerRoot != null)
                playerCamera = playerRoot.GetComponentInChildren<Camera>(true);

            if (playerCamera == null)
                playerCamera = Camera.main;
        }

        if (playerRoot == null && playerCamera != null)
        {
            Transform t = playerCamera.transform;
            while (t.parent != null)
            {
                t = t.parent;
                if (t.name.ToLowerInvariant().Contains("player"))
                {
                    playerRoot = t;
                    break;
                }
            }
        }

        if (monitorRaycastTarget == null)
        {
            Transform found = FindChildRecursive(transform.root, "Monitor");
            if (found != null)
                monitorRaycastTarget = found;
        }

        if (terminalCameraTarget == null)
        {
            Transform found = FindChildRecursive(transform.root, "Terminal_Camera_Target");
            if (found != null)
                terminalCameraTarget = found;
        }
    }

    private IEnumerator OpenRoutine()
    {
        isTransitioning = true;
        AnyTerminalOpen = true;

        SaveCameraState();
        SaveCursorState();
        HideExtraUI();
        HideCrosshair();
        SetPlayerControl(false);

        if (terminalMenuCanvas != null)
            terminalMenuCanvas.SetActive(false);

        yield return AnimateCamera(
            playerCamera.transform.position,
            playerCamera.transform.rotation,
            terminalCameraTarget.position,
            terminalCameraTarget.rotation,
            originalFOV,
            terminalFOV,
            moveToTerminalDuration
        );

        isOpen = true;
        isTransitioning = false;

        if (terminalMenuCanvas != null)
            terminalMenuCanvas.SetActive(true);

        if (unlockCursorWhenOpen)
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }

        if (showDebugLogs)
            Debug.Log("[FactoryComputerTerminalFocus] Terminal aberto.", this);
    }

    private IEnumerator CloseRoutine()
    {
        isTransitioning = true;
        isOpen = false;

        if (terminalMenuCanvas != null)
            terminalMenuCanvas.SetActive(false);

        // Mantém o controle bloqueado até a câmera voltar.
        yield return AnimateCamera(
            playerCamera.transform.position,
            playerCamera.transform.rotation,
            originalWorldPosition,
            originalWorldRotation,
            playerCamera.fieldOfView,
            originalFOV,
            moveBackDuration
        );

        RestoreCameraState();
        RestoreCursorState();
        RestorePlayerControl();
        RestoreCrosshair();
        RestoreExtraUI();

        isTransitioning = false;
        AnyTerminalOpen = false;

        if (showDebugLogs)
            Debug.Log("[FactoryComputerTerminalFocus] Terminal fechado.", this);
    }

    private IEnumerator AnimateCamera(Vector3 fromPos, Quaternion fromRot, Vector3 toPos, Quaternion toRot, float fromFov, float toFov, float duration)
    {
        float elapsed = 0f;
        duration = Mathf.Max(0.05f, duration);

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float rawT = Mathf.Clamp01(elapsed / duration);
            float t = cameraCurve != null ? cameraCurve.Evaluate(rawT) : rawT;

            playerCamera.transform.position = Vector3.Lerp(fromPos, toPos, t);
            playerCamera.transform.rotation = Quaternion.Slerp(fromRot, toRot, t);

            if (animateFOV)
                playerCamera.fieldOfView = Mathf.Lerp(fromFov, toFov, t);

            yield return null;
        }

        playerCamera.transform.position = toPos;
        playerCamera.transform.rotation = toRot;
        if (animateFOV)
            playerCamera.fieldOfView = toFov;
    }

    private void SaveCameraState()
    {
        originalCameraParent = playerCamera.transform.parent;
        originalLocalPosition = playerCamera.transform.localPosition;
        originalLocalRotation = playerCamera.transform.localRotation;
        originalWorldPosition = playerCamera.transform.position;
        originalWorldRotation = playerCamera.transform.rotation;
        originalFOV = playerCamera.fieldOfView;
    }

    private void RestoreCameraState()
    {
        if (playerCamera == null)
            return;

        if (playerCamera.transform.parent == originalCameraParent)
        {
            playerCamera.transform.localPosition = originalLocalPosition;
            playerCamera.transform.localRotation = originalLocalRotation;
        }
        else
        {
            playerCamera.transform.position = originalWorldPosition;
            playerCamera.transform.rotation = originalWorldRotation;
        }

        if (animateFOV)
            playerCamera.fieldOfView = originalFOV;
    }

    private void SaveCursorState()
    {
        previousLockMode = Cursor.lockState;
        previousCursorVisible = Cursor.visible;
    }

    private void RestoreCursorState()
    {
        Cursor.lockState = previousLockMode;
        Cursor.visible = previousCursorVisible;
    }

    private void SetPlayerControl(bool enabled)
    {
        if (enabled)
        {
            RestorePlayerControl();
            return;
        }

        disabledBehaviours.Clear();

        if (componentsToDisableWhileOpen != null)
        {
            foreach (Behaviour b in componentsToDisableWhileOpen)
                DisableBehaviourIfValid(b);
        }

        if (!autoDisablePlayerControlScripts || playerRoot == null)
            return;

        MonoBehaviour[] behaviours = playerRoot.GetComponentsInChildren<MonoBehaviour>(true);
        foreach (MonoBehaviour behaviour in behaviours)
        {
            if (behaviour == null || !behaviour.enabled)
                continue;

            string typeName = behaviour.GetType().Name;

            if (ShouldSkipAutoDisable(typeName))
                continue;

            if (NameMatches(typeName, autoDisableNameContains) || NameMatches(behaviour.name, autoDisableNameContains))
                DisableBehaviourIfValid(behaviour);
        }
    }

    private void DisableBehaviourIfValid(Behaviour b)
    {
        if (b == null || !b.enabled)
            return;

        if (b == this)
            return;

        string typeName = b.GetType().Name;
        if (ShouldSkipAutoDisable(typeName))
            return;

        b.enabled = false;
        if (!disabledBehaviours.Contains(b))
            disabledBehaviours.Add(b);
    }

    private void RestorePlayerControl()
    {
        foreach (Behaviour b in disabledBehaviours)
        {
            if (b != null)
                b.enabled = true;
        }
        disabledBehaviours.Clear();
    }

    private bool ShouldSkipAutoDisable(string typeName)
    {
        if (string.IsNullOrEmpty(typeName))
            return true;

        string n = typeName.ToLowerInvariant();
        if (n.Contains("factorycomputerterminal")) return true;
        if (n.Contains("factoryinteraction")) return true;
        if (n.Contains("playercrosshair")) return true;
        if (n.Contains("eventsystem")) return true;
        return false;
    }

    private void HideCrosshair()
    {
        if (!hideCrosshairWhileOpen)
            return;

        hiddenCrosshairObjects.Clear();
        disabledCrosshairBehaviours.Clear();

        HideByName("Player_Crosshair_Canvas_V7");
        HideByName("Player_Crosshair_Canvas_V6");
        HideByName("Player_Crosshair_Canvas_V5");
        HideByName("Player_Crosshair_Canvas_V4");
        HideByName("Player_Crosshair_Canvas_V3");
        HideByName("Player_Crosshair_Canvas_V2");
        HideByName("Player_Crosshair_Canvas");

        MonoBehaviour[] all = FindObjectsOfType<MonoBehaviour>(true);
        foreach (MonoBehaviour behaviour in all)
        {
            if (behaviour == null || !behaviour.enabled)
                continue;

            string n = behaviour.GetType().Name.ToLowerInvariant();
            if (n.Contains("playercrosshair"))
            {
                behaviour.enabled = false;
                disabledCrosshairBehaviours.Add(behaviour);
            }
        }
    }

    private void RestoreCrosshair()
    {
        foreach (GameObject go in hiddenCrosshairObjects)
        {
            if (go != null)
                go.SetActive(true);
        }
        hiddenCrosshairObjects.Clear();

        foreach (Behaviour b in disabledCrosshairBehaviours)
        {
            if (b != null)
                b.enabled = true;
        }
        disabledCrosshairBehaviours.Clear();
    }

    private void HideByName(string objectName)
    {
        GameObject go = GameObject.Find(objectName);
        if (go == null || !go.activeSelf)
            return;

        go.SetActive(false);
        hiddenCrosshairObjects.Add(go);
    }

    private void HideExtraUI()
    {
        previousExtraUIStates.Clear();
        if (extraUIToHideWhileOpen == null)
            return;

        foreach (GameObject go in extraUIToHideWhileOpen)
        {
            if (go == null)
                continue;

            previousExtraUIStates[go] = go.activeSelf;
            go.SetActive(false);
        }
    }

    private void RestoreExtraUI()
    {
        foreach (KeyValuePair<GameObject, bool> pair in previousExtraUIStates)
        {
            if (pair.Key != null)
                pair.Key.SetActive(pair.Value);
        }
        previousExtraUIStates.Clear();
    }

    private static bool NameMatches(string value, string[] parts)
    {
        if (string.IsNullOrWhiteSpace(value) || parts == null)
            return false;

        string lowered = value.ToLowerInvariant();
        foreach (string part in parts)
        {
            if (string.IsNullOrWhiteSpace(part))
                continue;

            if (lowered.Contains(part.ToLowerInvariant()))
                return true;
        }

        return false;
    }

    private static Transform FindChildRecursive(Transform root, string namePart)
    {
        if (root == null || string.IsNullOrWhiteSpace(namePart))
            return null;

        string target = namePart.ToLowerInvariant();
        foreach (Transform child in root)
        {
            if (child.name.ToLowerInvariant().Contains(target))
                return child;

            Transform found = FindChildRecursive(child, namePart);
            if (found != null)
                return found;
        }

        return null;
    }
}
