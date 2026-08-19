using System.Collections;
using UnityEngine;

/// <summary>
/// Controla o portão principal da fábrica exportada do Blender/FBX.
/// Funciona mesmo quando o portão não tem pivô bom: o script cria um pivô falso no topo
/// e encolhe o portão de baixo para cima, dando a ilusão de que ele sobe/enrola.
///
/// Como usar:
/// 1. Coloque este script em Assets/Scripts.
/// 2. Adicione este componente no objeto raiz da fábrica OU diretamente no Loading_Gate.
/// 3. Se possível, arraste o Player_FPS para Player Transform.
/// 4. Dê Play, chegue perto do portão e pressione F.
/// </summary>
[DisallowMultipleComponent]
public class FactoryLoadingGateController_V2 : MonoBehaviour
{
    [Header("Referências")]
    [Tooltip("Objeto do jogador. Se ficar vazio, o script tenta achar um objeto com tag Player ou nome Player_FPS.")]
    public Transform playerTransform;

    [Tooltip("Objeto visual do portão. Se ficar vazio, o script tenta achar um filho chamado Loading_Gate.")]
    public Transform gateTransform;

    [Tooltip("Nome usado para procurar o portão dentro do FBX.")]
    public string gateObjectName = "Loading_Gate";

    [Header("Interação")]
    public KeyCode interactKey = KeyCode.F;
    public float interactionDistance = 4.0f;
    public string openPrompt = "Pressione F para abrir";
    public string closePrompt = "Pressione F para fechar";

    [Header("Animação")]
    [Tooltip("Tempo da animação de abrir/fechar.")]
    public float animationDuration = 1.25f;

    [Tooltip("Altura visual restante quando o portão estiver aberto. 0.05 = sobra 5% do portão no topo.")]
    [Range(0.01f, 0.25f)]
    public float openedHeightPercent = 0.05f;

    [Tooltip("Curva de suavização da animação.")]
    public AnimationCurve animationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Colisão")]
    [Tooltip("Desativa os colliders do portão quando aberto, para o jogador passar.")]
    public bool disableCollidersWhenOpen = true;

    [Tooltip("Reativa os colliders logo no começo do fechamento.")]
    public bool enableCollidersAtCloseStart = true;

    [Header("Debug")]
    public bool showDebugLogs = false;
    public bool drawInteractionGizmo = true;

    private Transform gatePivot;
    private Renderer[] gateRenderers;
    private Collider[] gateColliders;

    private Vector3 pivotClosedScale;
    private bool isOpen;
    private bool isMoving;
    private bool isReady;
    private float gateClosedHeight;
    private Vector3 lastKnownGateCenter;

    private void Awake()
    {
        PrepareGate();
    }

    private void Reset()
    {
        animationDuration = 1.25f;
        interactionDistance = 4.0f;
        openedHeightPercent = 0.05f;
    }

    [ContextMenu("Prepare / Rebuild Gate Pivot")]
    public void PrepareGate()
    {
        AutoFindPlayer();
        AutoFindGate();

        if (gateTransform == null)
        {
            Debug.LogWarning("[FactoryLoadingGateController_V2] Não encontrei o portão. Arraste o objeto Loading_Gate para Gate Transform.", this);
            isReady = false;
            return;
        }

        // Se o setup anterior marcou a fábrica inteira como Static, animação visual pode não atualizar direito.
        // O portão precisa ser dinâmico.
        SetStaticRecursive(gateTransform.gameObject, false);

        gateRenderers = gateTransform.GetComponentsInChildren<Renderer>(true);
        gateColliders = gateTransform.GetComponentsInChildren<Collider>(true);

        Bounds gateBounds;
        if (!TryGetBounds(gateRenderers, out gateBounds))
        {
            Debug.LogWarning("[FactoryLoadingGateController_V2] O portão não tem Renderer. O script precisa de uma malha visível para calcular o topo.", this);
            isReady = false;
            return;
        }

        gateClosedHeight = Mathf.Max(0.01f, gateBounds.size.y);
        lastKnownGateCenter = gateBounds.center;

        // Evita criar pivôs infinitos se o método for chamado mais de uma vez.
        if (gateTransform.parent != null && gateTransform.parent.name == gateObjectName + "_AnimatedPivot")
        {
            gatePivot = gateTransform.parent;
            pivotClosedScale = gatePivot.localScale;
            isReady = true;
            DebugLog("Pivô animado já existia. Reutilizando.");
            return;
        }

        Transform oldParent = gateTransform.parent;

        // O pivô fica exatamente no topo visual do portão.
        Vector3 topCenter = new Vector3(gateBounds.center.x, gateBounds.max.y, gateBounds.center.z);

        GameObject pivotObject = new GameObject(gateObjectName + "_AnimatedPivot");
        gatePivot = pivotObject.transform;
        gatePivot.position = topCenter;
        gatePivot.rotation = Quaternion.identity;
        gatePivot.localScale = Vector3.one;
        gatePivot.SetParent(oldParent, true);

        gateTransform.SetParent(gatePivot, true);

        SetStaticRecursive(gatePivot.gameObject, false);

        pivotClosedScale = gatePivot.localScale;
        isOpen = false;
        isMoving = false;
        isReady = true;

        DebugLog("Portão preparado. Altura calculada: " + gateClosedHeight.ToString("0.00") + "m. Pivô criado em: " + gatePivot.position);
    }

    private void Update()
    {
        if (!isReady)
        {
            return;
        }

        if (playerTransform == null)
        {
            AutoFindPlayer();
            if (playerTransform == null)
            {
                return;
            }
        }

        bool nearGate = IsPlayerNearGate();

        if (nearGate && Input.GetKeyDown(interactKey) && !isMoving)
        {
            ToggleGate();
        }
    }

    [ContextMenu("Toggle Gate Now")]
    public void ToggleGate()
    {
        if (!isReady || gatePivot == null)
        {
            PrepareGate();
        }

        if (!isReady || gatePivot == null)
        {
            return;
        }

        StopAllCoroutines();
        StartCoroutine(AnimateGate(!isOpen));
    }

    [ContextMenu("Open Gate Now")]
    public void OpenGateNow()
    {
        if (!isReady) PrepareGate();
        if (!isReady) return;

        StopAllCoroutines();
        SetGateOpenAmount(1f);
        isOpen = true;
        isMoving = false;
        SetGateColliders(false);
    }

    [ContextMenu("Close Gate Now")]
    public void CloseGateNow()
    {
        if (!isReady) PrepareGate();
        if (!isReady) return;

        StopAllCoroutines();
        SetGateOpenAmount(0f);
        isOpen = false;
        isMoving = false;
        SetGateColliders(true);
    }

    private IEnumerator AnimateGate(bool open)
    {
        isMoving = true;

        if (!open && enableCollidersAtCloseStart)
        {
            SetGateColliders(true);
        }

        float startAmount = GetCurrentOpenAmount();
        float targetAmount = open ? 1f : 0f;
        float timer = 0f;

        DebugLog(open ? "Abrindo portão..." : "Fechando portão...");

        while (timer < animationDuration)
        {
            timer += Time.deltaTime;
            float normalized = animationDuration <= 0.01f ? 1f : Mathf.Clamp01(timer / animationDuration);
            float curved = animationCurve != null ? animationCurve.Evaluate(normalized) : normalized;
            float amount = Mathf.Lerp(startAmount, targetAmount, curved);

            SetGateOpenAmount(amount);
            yield return null;
        }

        SetGateOpenAmount(targetAmount);
        isOpen = open;
        isMoving = false;

        if (open && disableCollidersWhenOpen)
        {
            SetGateColliders(false);
        }
        else if (!open)
        {
            SetGateColliders(true);
        }

        DebugLog(open ? "Portão aberto." : "Portão fechado.");
    }

    /// <summary>
    /// amount 0 = fechado. amount 1 = aberto.
    /// O truque: escala o pivô no Y. Como o pivô está no topo, o portão encolhe de baixo para cima.
    /// </summary>
    private void SetGateOpenAmount(float amount)
    {
        amount = Mathf.Clamp01(amount);

        float closedY = pivotClosedScale.y;
        float openedY = pivotClosedScale.y * Mathf.Clamp(openedHeightPercent, 0.01f, 1f);
        float newY = Mathf.Lerp(closedY, openedY, amount);

        Vector3 scale = gatePivot.localScale;
        scale.y = newY;
        gatePivot.localScale = scale;
    }

    private float GetCurrentOpenAmount()
    {
        if (gatePivot == null || Mathf.Abs(pivotClosedScale.y) < 0.0001f)
        {
            return isOpen ? 1f : 0f;
        }

        float closedY = pivotClosedScale.y;
        float openedY = pivotClosedScale.y * Mathf.Clamp(openedHeightPercent, 0.01f, 1f);
        float currentY = gatePivot.localScale.y;

        return Mathf.InverseLerp(closedY, openedY, currentY);
    }

    private bool IsPlayerNearGate()
    {
        Vector3 gateCenter = GetGateCenter();
        float distance = Vector3.Distance(playerTransform.position, gateCenter);
        return distance <= interactionDistance;
    }

    private Vector3 GetGateCenter()
    {
        if (gateRenderers != null && TryGetBounds(gateRenderers, out Bounds bounds))
        {
            lastKnownGateCenter = bounds.center;
            return bounds.center;
        }

        if (gatePivot != null)
        {
            return gatePivot.position + Vector3.down * (gateClosedHeight * 0.5f);
        }

        return lastKnownGateCenter;
    }

    private void SetGateColliders(bool enabled)
    {
        if (!disableCollidersWhenOpen && !enabled)
        {
            return;
        }

        if (gateColliders == null)
        {
            return;
        }

        foreach (Collider gateCollider in gateColliders)
        {
            if (gateCollider != null)
            {
                gateCollider.enabled = enabled;
            }
        }
    }

    private void AutoFindPlayer()
    {
        if (playerTransform != null)
        {
            return;
        }

        GameObject taggedPlayer = GameObject.FindGameObjectWithTag("Player");
        if (taggedPlayer != null)
        {
            playerTransform = taggedPlayer.transform;
            return;
        }

        GameObject namedPlayer = GameObject.Find("Player_FPS");
        if (namedPlayer != null)
        {
            playerTransform = namedPlayer.transform;
        }
    }

    private void AutoFindGate()
    {
        if (gateTransform != null)
        {
            return;
        }

        if (gameObject.name == gateObjectName || gameObject.name.Contains(gateObjectName))
        {
            gateTransform = transform;
            return;
        }

        Transform foundChild = FindDeepChild(transform, gateObjectName);
        if (foundChild != null)
        {
            gateTransform = foundChild;
            return;
        }

        GameObject foundInScene = GameObject.Find(gateObjectName);
        if (foundInScene != null)
        {
            gateTransform = foundInScene.transform;
        }
    }

    private Transform FindDeepChild(Transform parent, string childName)
    {
        foreach (Transform child in parent)
        {
            if (child.name == childName || child.name.Contains(childName))
            {
                return child;
            }

            Transform result = FindDeepChild(child, childName);
            if (result != null)
            {
                return result;
            }
        }

        return null;
    }

    private bool TryGetBounds(Renderer[] renderers, out Bounds bounds)
    {
        bounds = new Bounds();
        bool hasBounds = false;

        if (renderers == null)
        {
            return false;
        }

        foreach (Renderer renderer in renderers)
        {
            if (renderer == null || !renderer.enabled)
            {
                continue;
            }

            if (!hasBounds)
            {
                bounds = renderer.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(renderer.bounds);
            }
        }

        return hasBounds;
    }

    private void SetStaticRecursive(GameObject target, bool value)
    {
        if (target == null)
        {
            return;
        }

        target.isStatic = value;

        foreach (Transform child in target.transform)
        {
            SetStaticRecursive(child.gameObject, value);
        }
    }

    private void OnGUI()
    {
        if (!Application.isPlaying || !isReady || playerTransform == null)
        {
            return;
        }

        if (!IsPlayerNearGate())
        {
            return;
        }

        string text = isOpen ? closePrompt : openPrompt;

        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.fontSize = 24;
        style.alignment = TextAnchor.MiddleCenter;
        style.normal.textColor = Color.white;

        Rect shadowRect = new Rect(2, Screen.height - 118, Screen.width, 60);
        GUIStyle shadowStyle = new GUIStyle(style);
        shadowStyle.normal.textColor = Color.black;
        GUI.Label(shadowRect, text, shadowStyle);

        Rect rect = new Rect(0, Screen.height - 120, Screen.width, 60);
        GUI.Label(rect, text, style);
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawInteractionGizmo)
        {
            return;
        }

        Gizmos.color = Color.yellow;

        Vector3 center = transform.position;
        if (Application.isPlaying && isReady)
        {
            center = GetGateCenter();
        }
        else if (gateTransform != null)
        {
            Renderer[] renderers = gateTransform.GetComponentsInChildren<Renderer>(true);
            if (TryGetBounds(renderers, out Bounds bounds))
            {
                center = bounds.center;
            }
            else
            {
                center = gateTransform.position;
            }
        }

        Gizmos.DrawWireSphere(center, interactionDistance);
    }

    private void DebugLog(string message)
    {
        if (showDebugLogs)
        {
            Debug.Log("[FactoryLoadingGateController_V2] " + message, this);
        }
    }
}
