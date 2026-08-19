using System.Collections;
using UnityEngine;

/// <summary>
/// Portao de carga com abertura falsa tipo porta de enrolar:
/// ele nao sobe atravessando a parede; ele diminui verticalmente mantendo o topo preso,
/// dando a impressao de que esta abrindo para cima.
///
/// Como usar:
/// 1. Coloque este script no objeto Loading_Gate.
/// 2. Arraste o Player_FPS para o campo Player Transform, ou deixe o player com a tag "Player".
/// 3. Rode o jogo, aproxime-se do portao e pressione F.
/// </summary>
[DisallowMultipleComponent]
public class FactoryLoadingGateController : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("O objeto visual do portao. Se ficar vazio, usa o proprio objeto deste script.")]
    public Transform gateVisual;

    [Tooltip("Player usado para medir distancia. Se vazio, tenta encontrar pela tag Player, depois por nome contendo 'Player'.")]
    public Transform playerTransform;

    [Tooltip("Ponto usado para medir interacao. Se vazio, usa o centro do portao.")]
    public Transform interactionPoint;

    [Header("Interacao")]
    public KeyCode interactionKey = KeyCode.F;
    public float interactionDistance = 4f;
    public string openText = "Pressione F para abrir";
    public string closeText = "Pressione F para fechar";

    [Header("Animacao do portao")]
    [Tooltip("Tempo da animacao em segundos.")]
    public float animationDuration = 1.15f;

    [Tooltip("Escala vertical quando aberto. 0.08 deixa uma pequena borda no topo.")]
    [Range(0.01f, 0.35f)]
    public float openedVerticalScale = 0.08f;

    [Tooltip("Curva da animacao. Cria aceleracao e desaceleracao para nao ficar duro.")]
    public AnimationCurve movementCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Tooltip("Se ligado, desativa o collider do portao quando ele estiver aberto.")]
    public bool disableSolidColliderWhenOpen = true;

    [Tooltip("Tempo normalizado a partir do qual o collider desliga durante a abertura.")]
    [Range(0f, 1f)]
    public float colliderDisableAfter = 0.82f;

    [Header("Texto na tela")]
    public bool drawInteractionText = true;
    public int fontSize = 24;
    public Vector2 textOffsetFromCenter = new Vector2(0f, 160f);

    [Header("Som opcional")]
    public AudioSource audioSource;
    public AudioClip openSound;
    public AudioClip closeSound;

    private Vector3 closedLocalScale;
    private float closedWorldTopY;
    private bool isOpen;
    private bool isAnimating;
    private Coroutine animationRoutine;
    private Collider[] gateColliders;
    private GUIStyle guiStyle;

    private void Awake()
    {
        if (gateVisual == null)
            gateVisual = transform;

        CacheClosedState();
        CacheColliders();
        TryFindPlayer();
    }

    private void Reset()
    {
        gateVisual = transform;
        interactionDistance = 4f;
        animationDuration = 1.15f;
        openedVerticalScale = 0.08f;
    }

    private void OnValidate()
    {
        animationDuration = Mathf.Max(0.05f, animationDuration);
        interactionDistance = Mathf.Max(0.2f, interactionDistance);
        colliderDisableAfter = Mathf.Clamp01(colliderDisableAfter);
    }

    private void Update()
    {
        if (gateVisual == null)
            gateVisual = transform;

        if (playerTransform == null)
            TryFindPlayer();

        if (!IsPlayerNear())
            return;

        if (Input.GetKeyDown(interactionKey))
            ToggleGate();
    }

    [ContextMenu("Toggle Gate")]
    public void ToggleGate()
    {
        if (isAnimating)
            return;

        SetOpen(!isOpen);
    }

    public void SetOpen(bool open)
    {
        if (animationRoutine != null)
            StopCoroutine(animationRoutine);

        animationRoutine = StartCoroutine(AnimateGate(open));
    }

    [ContextMenu("Force Open")]
    public void ForceOpen()
    {
        if (animationRoutine != null)
            StopCoroutine(animationRoutine);

        isOpen = true;
        isAnimating = false;
        ApplyVisualState(1f);
        SetGateColliders(false);
    }

    [ContextMenu("Force Closed")]
    public void ForceClosed()
    {
        if (animationRoutine != null)
            StopCoroutine(animationRoutine);

        isOpen = false;
        isAnimating = false;
        ApplyVisualState(0f);
        SetGateColliders(true);
    }

    [ContextMenu("Recalculate Closed State")]
    public void RecalculateClosedState()
    {
        if (gateVisual == null)
            gateVisual = transform;

        CacheClosedState();
        CacheColliders();
        Debug.Log($"[FactoryLoadingGateController] Estado fechado recalculado para {gateVisual.name}.", this);
    }

    private IEnumerator AnimateGate(bool opening)
    {
        isAnimating = true;

        PlayGateSound(opening);

        float start = opening ? 0f : 1f;
        float end = opening ? 1f : 0f;
        float timer = 0f;

        // Se vai fechar, o collider volta logo no inicio para bloquear passagem.
        if (!opening && disableSolidColliderWhenOpen)
            SetGateColliders(true);

        while (timer < animationDuration)
        {
            timer += Time.deltaTime;
            float rawT = Mathf.Clamp01(timer / animationDuration);
            float curvedT = movementCurve != null ? movementCurve.Evaluate(rawT) : Mathf.SmoothStep(0f, 1f, rawT);
            float state = Mathf.Lerp(start, end, curvedT);

            ApplyVisualState(state);

            if (opening && disableSolidColliderWhenOpen && state >= colliderDisableAfter)
                SetGateColliders(false);

            yield return null;
        }

        ApplyVisualState(end);
        isOpen = opening;
        isAnimating = false;
        animationRoutine = null;

        if (disableSolidColliderWhenOpen)
            SetGateColliders(!isOpen);
    }

    /// <summary>
    /// state = 0 fechado, state = 1 aberto.
    /// A escala vertical diminui, mas o topo do portao fica preso na mesma altura.
    /// </summary>
    private void ApplyVisualState(float state)
    {
        if (gateVisual == null)
            return;

        float verticalMultiplier = Mathf.Lerp(1f, openedVerticalScale, Mathf.Clamp01(state));

        Vector3 newScale = closedLocalScale;
        newScale.y = closedLocalScale.y * verticalMultiplier;
        gateVisual.localScale = newScale;

        // Corrige a posicao depois da escala para manter o topo ancorado.
        Bounds bounds = GetCombinedRendererBounds(gateVisual);
        float deltaY = closedWorldTopY - bounds.max.y;
        gateVisual.position += Vector3.up * deltaY;
    }

    private void CacheClosedState()
    {
        if (gateVisual == null)
            gateVisual = transform;

        closedLocalScale = gateVisual.localScale;
        Bounds bounds = GetCombinedRendererBounds(gateVisual);
        closedWorldTopY = bounds.max.y;
    }

    private void CacheColliders()
    {
        if (gateVisual == null)
            gateVisual = transform;

        gateColliders = gateVisual.GetComponentsInChildren<Collider>(true);
    }

    private void SetGateColliders(bool enabled)
    {
        if (gateColliders == null || gateColliders.Length == 0)
            CacheColliders();

        foreach (Collider col in gateColliders)
        {
            if (col == null)
                continue;

            // Nao mexe em trigger, caso voce coloque um trigger proprio de interacao depois.
            if (col.isTrigger)
                continue;

            col.enabled = enabled;
        }
    }

    private bool IsPlayerNear()
    {
        if (playerTransform == null)
            return false;

        Vector3 checkPoint = interactionPoint != null ? interactionPoint.position : GetInteractionCenter();
        float distance = Vector3.Distance(playerTransform.position, checkPoint);
        return distance <= interactionDistance;
    }

    private Vector3 GetInteractionCenter()
    {
        Bounds bounds = GetCombinedRendererBounds(gateVisual != null ? gateVisual : transform);
        return bounds.center;
    }

    private void TryFindPlayer()
    {
        GameObject taggedPlayer = GameObject.FindGameObjectWithTag("Player");
        if (taggedPlayer != null)
        {
            playerTransform = taggedPlayer.transform;
            return;
        }

        GameObject[] allObjects = FindObjectsOfType<GameObject>();
        foreach (GameObject obj in allObjects)
        {
            if (obj.name.ToLower().Contains("player"))
            {
                playerTransform = obj.transform;
                return;
            }
        }

        if (Camera.main != null)
            playerTransform = Camera.main.transform;
    }

    private Bounds GetCombinedRendererBounds(Transform target)
    {
        Renderer[] renderers = target.GetComponentsInChildren<Renderer>(true);

        if (renderers.Length == 0)
            return new Bounds(target.position, Vector3.one);

        Bounds combined = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            combined.Encapsulate(renderers[i].bounds);

        return combined;
    }

    private void PlayGateSound(bool opening)
    {
        if (audioSource == null)
            return;

        AudioClip clip = opening ? openSound : closeSound;
        if (clip != null)
            audioSource.PlayOneShot(clip);
    }

    private void OnGUI()
    {
        if (!drawInteractionText || !IsPlayerNear())
            return;

        if (guiStyle == null)
        {
            guiStyle = new GUIStyle(GUI.skin.label);
            guiStyle.alignment = TextAnchor.MiddleCenter;
            guiStyle.fontSize = fontSize;
            guiStyle.normal.textColor = Color.white;
        }

        guiStyle.fontSize = fontSize;

        string message = isOpen ? closeText : openText;
        Rect rect = new Rect(
            (Screen.width * 0.5f) - 250f + textOffsetFromCenter.x,
            (Screen.height * 0.5f) + textOffsetFromCenter.y,
            500f,
            40f
        );

        // Sombra simples para melhorar leitura.
        Rect shadowRect = rect;
        shadowRect.x += 2f;
        shadowRect.y += 2f;

        Color oldColor = GUI.color;
        GUI.color = Color.black;
        GUI.Label(shadowRect, message, guiStyle);
        GUI.color = Color.white;
        GUI.Label(rect, message, guiStyle);
        GUI.color = oldColor;
    }

    private void OnDrawGizmosSelected()
    {
        Transform source = interactionPoint != null ? interactionPoint : transform;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(source.position, interactionDistance);

        if (gateVisual != null)
        {
            Bounds bounds = GetCombinedRendererBounds(gateVisual);
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(bounds.center, bounds.size);
        }
    }
}
