using System.Collections.Generic;
using System.Linq;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Corrige escadas feitas de vários degraus para o jogador subir andando,
/// criando uma rampa invisível de colisão por cima dos degraus e desativando
/// os colliders individuais dos degraus.
///
/// Uso recomendado:
/// 1. Coloque este script no objeto raiz da fábrica, ex: "Fabrica".
/// 2. Clique nos três pontinhos do componente.
/// 3. Use "Apply Stair Walk Fix".
/// 4. Deixe Fix On Start desmarcado se você já aplicou fora do Play Mode.
/// </summary>
[ExecuteAlways]
public class FactoryStairWalkFixer : MonoBehaviour
{
    [Header("Referências")]
    [Tooltip("Raiz da fábrica. Se vazio, usa o próprio GameObject.")]
    public Transform factoryRoot;

    [Tooltip("Opcional. Se for CharacterController, o script ajusta Step Offset e Slope Limit.")]
    public CharacterController playerCharacterController;

    [Header("Busca da escada")]
    [Tooltip("Objetos dos degraus devem conter esse texto no nome.")]
    public string stairStepNameContains = "Control_Stairs_Step";

    [Tooltip("Nome do objeto de rampa invisível gerado.")]
    public string generatedRampName = "Control_Stairs_Walkable_Ramp";

    [Header("Ajustes da rampa")]
    [Tooltip("Largura extra para a rampa cobrir bem a escada.")]
    public float widthPadding = 0.35f;

    [Tooltip("Quanto a rampa começa antes do primeiro degrau.")]
    public float startExtension = 0.45f;

    [Tooltip("Quanto a rampa avança depois do último degrau, para encaixar na plataforma.")]
    public float endExtension = 0.70f;

    [Tooltip("Altura extra acima dos degraus para evitar z-fight/falha de contato.")]
    public float surfaceLift = 0.035f;

    [Tooltip("Espessura física da rampa invisível.")]
    public float rampThickness = 0.25f;

    [Header("Correções")]
    [Tooltip("Desativa colliders dos degraus. Normalmente precisa ficar ligado para o player não bater na frente de cada degrau.")]
    public bool disableStepColliders = true;

    [Tooltip("Ajusta CharacterController do player, se existir.")]
    public bool tuneCharacterController = true;

    [Tooltip("Valor recomendado para subir degraus/rampas sem travar.")]
    public float characterStepOffset = 0.45f;

    [Tooltip("Inclinação máxima que o CharacterController pode subir.")]
    public float characterSlopeLimit = 58f;

    [Tooltip("Gera uma malha visual transparente para você enxergar a rampa no editor. Desligado por padrão.")]
    public bool showDebugRampVisual = false;

    [Header("Execução")]
    [Tooltip("Se ligado, aplica a correção ao iniciar o jogo. Se você já aplicou pelo menu, deixe desligado.")]
    public bool fixOnStart = false;

    private void Reset()
    {
        factoryRoot = transform;
    }

    private void Start()
    {
        if (Application.isPlaying && fixOnStart)
        {
            ApplyStairWalkFix();
        }
    }

    [ContextMenu("Apply Stair Walk Fix")]
    public void ApplyStairWalkFix()
    {
        Transform root = factoryRoot != null ? factoryRoot : transform;

        List<Renderer> stepRenderers = FindStepRenderers(root);
        if (stepRenderers.Count < 2)
        {
            Debug.LogWarning($"[{nameof(FactoryStairWalkFixer)}] Não encontrei degraus suficientes com nome contendo '{stairStepNameContains}'.");
            return;
        }

        RemoveGeneratedRamp(root);

        StairRampData data;
        if (!TryCalculateRampData(stepRenderers, root, out data))
        {
            Debug.LogWarning($"[{nameof(FactoryStairWalkFixer)}] Não consegui calcular a rampa da escada.");
            return;
        }

        GameObject ramp = new GameObject(generatedRampName);
        ramp.transform.SetParent(root, true);
        ramp.transform.position = Vector3.zero;
        ramp.transform.rotation = Quaternion.identity;
        ramp.transform.localScale = Vector3.one;
        ramp.isStatic = true;

        Mesh rampMesh = BuildRampMesh(data);
        rampMesh.name = generatedRampName + "_Mesh";

        MeshCollider meshCollider = ramp.AddComponent<MeshCollider>();
        meshCollider.sharedMesh = rampMesh;
        meshCollider.convex = false;

        if (showDebugRampVisual)
        {
            MeshFilter filter = ramp.AddComponent<MeshFilter>();
            filter.sharedMesh = rampMesh;

            MeshRenderer renderer = ramp.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = CreateDebugMaterial();
        }

        if (disableStepColliders)
        {
            DisableStepColliders(root);
        }

        if (tuneCharacterController)
        {
            TunePlayerCharacterController(root);
        }

        Debug.Log($"[{nameof(FactoryStairWalkFixer)}] Rampa invisível criada para a escada. Degraus encontrados: {stepRenderers.Count}.");
    }

    [ContextMenu("Remove Generated Stair Ramp")]
    public void RemoveGeneratedRampContext()
    {
        Transform root = factoryRoot != null ? factoryRoot : transform;
        RemoveGeneratedRamp(root);
        Debug.Log($"[{nameof(FactoryStairWalkFixer)}] Rampa gerada removida.");
    }

    [ContextMenu("Re-enable Stair Step Colliders")]
    public void ReEnableStepCollidersContext()
    {
        Transform root = factoryRoot != null ? factoryRoot : transform;
        int count = 0;
        foreach (Collider col in root.GetComponentsInChildren<Collider>(true))
        {
            if (col.name.Contains(stairStepNameContains))
            {
                col.enabled = true;
                count++;
            }
        }
        Debug.Log($"[{nameof(FactoryStairWalkFixer)}] Colliders de degraus reativados: {count}.");
    }

    private List<Renderer> FindStepRenderers(Transform root)
    {
        return root.GetComponentsInChildren<Renderer>(true)
            .Where(r => r != null && r.name.Contains(stairStepNameContains))
            .OrderBy(r => r.bounds.center.y)
            .ToList();
    }

    private bool TryCalculateRampData(List<Renderer> stepRenderers, Transform root, out StairRampData data)
    {
        data = new StairRampData();

        Renderer lowest = stepRenderers.OrderBy(r => r.bounds.center.y).First();
        Renderer highest = stepRenderers.OrderByDescending(r => r.bounds.center.y).First();

        Vector3 lowCenter = lowest.bounds.center;
        Vector3 highCenter = highest.bounds.center;

        Vector3 direction = new Vector3(highCenter.x - lowCenter.x, 0f, highCenter.z - lowCenter.z);
        if (direction.sqrMagnitude < 0.0001f)
        {
            // Fallback: usa a maior variação horizontal encontrada entre degraus.
            float bestDistance = 0f;
            Vector3 bestDirection = Vector3.zero;
            for (int i = 0; i < stepRenderers.Count; i++)
            {
                for (int j = i + 1; j < stepRenderers.Count; j++)
                {
                    Vector3 a = stepRenderers[i].bounds.center;
                    Vector3 b = stepRenderers[j].bounds.center;
                    Vector3 d = new Vector3(b.x - a.x, 0f, b.z - a.z);
                    if (d.sqrMagnitude > bestDistance)
                    {
                        bestDistance = d.sqrMagnitude;
                        bestDirection = d;
                    }
                }
            }
            direction = bestDirection;
        }

        if (direction.sqrMagnitude < 0.0001f)
            return false;

        direction.Normalize();
        Vector3 right = Vector3.Cross(Vector3.up, direction).normalized;

        Vector3 reference = lowCenter;
        float minForward = float.PositiveInfinity;
        float maxForward = float.NegativeInfinity;
        float minRight = float.PositiveInfinity;
        float maxRight = float.NegativeInfinity;
        float lowTopY = float.PositiveInfinity;
        float highTopY = float.NegativeInfinity;

        foreach (Renderer renderer in stepRenderers)
        {
            Bounds b = renderer.bounds;
            foreach (Vector3 corner in GetBoundsCorners(b))
            {
                Vector3 flat = new Vector3(corner.x, 0f, corner.z);
                Vector3 flatRef = new Vector3(reference.x, 0f, reference.z);
                Vector3 fromRef = flat - flatRef;

                float f = Vector3.Dot(fromRef, direction);
                float s = Vector3.Dot(fromRef, right);

                minForward = Mathf.Min(minForward, f);
                maxForward = Mathf.Max(maxForward, f);
                minRight = Mathf.Min(minRight, s);
                maxRight = Mathf.Max(maxRight, s);
            }

            if (b.center.y <= lowCenter.y + 0.05f)
                lowTopY = Mathf.Min(lowTopY, b.max.y);

            highTopY = Mathf.Max(highTopY, b.max.y);
        }

        if (float.IsInfinity(lowTopY))
            lowTopY = lowest.bounds.max.y;

        // Tenta encaixar no piso/plataforma superior se existir.
        float platformY = TryFindNearbyPlatformTop(root, highCenter, highTopY);
        highTopY = Mathf.Max(highTopY, platformY);

        minForward -= startExtension;
        maxForward += endExtension;

        float centerRightOffset = (minRight + maxRight) * 0.5f;
        float halfWidth = Mathf.Max(0.35f, (maxRight - minRight) * 0.5f + widthPadding * 0.5f);

        Vector3 baseFlat = new Vector3(reference.x, 0f, reference.z) + right * centerRightOffset;
        Vector3 lowTop = baseFlat + direction * minForward;
        Vector3 highTop = baseFlat + direction * maxForward;

        lowTop.y = lowTopY + surfaceLift;
        highTop.y = highTopY + surfaceLift;

        data.lowTop = lowTop;
        data.highTop = highTop;
        data.direction = direction;
        data.right = right;
        data.halfWidth = halfWidth;
        data.thickness = Mathf.Max(0.05f, rampThickness);

        return true;
    }

    private float TryFindNearbyPlatformTop(Transform root, Vector3 highCenter, float fallbackY)
    {
        string[] platformKeywords =
        {
            "Control_Platform",
            "Control_Room_Floor",
            "ControlRoom_Floor",
            "Platform"
        };

        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        float bestY = fallbackY;
        float bestDistance = float.PositiveInfinity;

        foreach (Renderer renderer in renderers)
        {
            if (renderer == null) continue;

            bool isPlatform = platformKeywords.Any(k => renderer.name.Contains(k));
            if (!isPlatform) continue;

            Vector3 c = renderer.bounds.center;
            float verticalDiff = Mathf.Abs(renderer.bounds.max.y - fallbackY);
            float horizontalDistance = Vector2.Distance(new Vector2(c.x, c.z), new Vector2(highCenter.x, highCenter.z));

            if (horizontalDistance < 4.5f && verticalDiff < 1.2f && horizontalDistance < bestDistance)
            {
                bestDistance = horizontalDistance;
                bestY = renderer.bounds.max.y;
            }
        }

        return bestY;
    }

    private Mesh BuildRampMesh(StairRampData data)
    {
        Vector3 lowLeftTop = data.lowTop - data.right * data.halfWidth;
        Vector3 lowRightTop = data.lowTop + data.right * data.halfWidth;
        Vector3 highLeftTop = data.highTop - data.right * data.halfWidth;
        Vector3 highRightTop = data.highTop + data.right * data.halfWidth;

        Vector3 lowLeftBottom = lowLeftTop - Vector3.up * data.thickness;
        Vector3 lowRightBottom = lowRightTop - Vector3.up * data.thickness;
        Vector3 highLeftBottom = highLeftTop - Vector3.up * data.thickness;
        Vector3 highRightBottom = highRightTop - Vector3.up * data.thickness;

        Vector3[] vertices =
        {
            lowLeftTop,      // 0
            lowRightTop,     // 1
            highLeftTop,     // 2
            highRightTop,    // 3
            lowLeftBottom,   // 4
            lowRightBottom,  // 5
            highLeftBottom,  // 6
            highRightBottom  // 7
        };

        int[] triangles =
        {
            // topo inclinado
            0, 2, 1,
            1, 2, 3,

            // baixo
            4, 5, 6,
            5, 7, 6,

            // lado esquerdo
            0, 4, 2,
            2, 4, 6,

            // lado direito
            1, 3, 5,
            3, 7, 5,

            // frente baixa
            0, 1, 4,
            1, 5, 4,

            // topo alto
            2, 6, 3,
            3, 6, 7
        };

        Mesh mesh = new Mesh();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    private void DisableStepColliders(Transform root)
    {
        int count = 0;
        foreach (Collider col in root.GetComponentsInChildren<Collider>(true))
        {
            if (col == null) continue;
            if (col.name == generatedRampName) continue;

            if (col.name.Contains(stairStepNameContains))
            {
                col.enabled = false;
                count++;
            }
        }

        Debug.Log($"[{nameof(FactoryStairWalkFixer)}] Colliders dos degraus desativados: {count}.");
    }

    private void TunePlayerCharacterController(Transform root)
    {
        CharacterController controller = playerCharacterController;

        if (controller == null)
        {
            GameObject player = GameObject.Find("Player_FPS");
            if (player != null)
                controller = player.GetComponent<CharacterController>();
        }

        if (controller == null)
        {
            Debug.Log($"[{nameof(FactoryStairWalkFixer)}] Nenhum CharacterController encontrado. A rampa ainda foi criada normalmente.");
            return;
        }

        controller.stepOffset = characterStepOffset;
        controller.slopeLimit = characterSlopeLimit;
        controller.skinWidth = Mathf.Max(controller.skinWidth, 0.04f);

        Debug.Log($"[{nameof(FactoryStairWalkFixer)}] CharacterController ajustado: Step Offset {controller.stepOffset}, Slope Limit {controller.slopeLimit}.");
    }

    private void RemoveGeneratedRamp(Transform root)
    {
        List<GameObject> toDelete = new List<GameObject>();
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
        {
            if (child != null && child.name == generatedRampName)
                toDelete.Add(child.gameObject);
        }

        foreach (GameObject obj in toDelete)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
                DestroyImmediate(obj);
            else
                Destroy(obj);
#else
            Destroy(obj);
#endif
        }
    }

    private IEnumerable<Vector3> GetBoundsCorners(Bounds b)
    {
        Vector3 min = b.min;
        Vector3 max = b.max;

        yield return new Vector3(min.x, min.y, min.z);
        yield return new Vector3(min.x, min.y, max.z);
        yield return new Vector3(min.x, max.y, min.z);
        yield return new Vector3(min.x, max.y, max.z);
        yield return new Vector3(max.x, min.y, min.z);
        yield return new Vector3(max.x, min.y, max.z);
        yield return new Vector3(max.x, max.y, min.z);
        yield return new Vector3(max.x, max.y, max.z);
    }

    private Material CreateDebugMaterial()
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");

        Material mat = new Material(shader);
        mat.name = "Debug_Stair_Ramp_Transparent";
        mat.color = new Color(1f, 0.85f, 0f, 0.35f);

        // Tenta deixar transparente em URP/Standard.
        if (mat.HasProperty("_Surface")) mat.SetFloat("_Surface", 1f);
        if (mat.HasProperty("_AlphaClip")) mat.SetFloat("_AlphaClip", 0f);
        if (mat.HasProperty("_SrcBlend")) mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        if (mat.HasProperty("_DstBlend")) mat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        if (mat.HasProperty("_ZWrite")) mat.SetFloat("_ZWrite", 0f);
        mat.renderQueue = 3000;

        return mat;
    }

    private struct StairRampData
    {
        public Vector3 lowTop;
        public Vector3 highTop;
        public Vector3 direction;
        public Vector3 right;
        public float halfWidth;
        public float thickness;
    }
}
