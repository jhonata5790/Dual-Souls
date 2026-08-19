using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

/// <summary>
/// Corrige Z-Fighting visual em uma fábrica importada do Blender/FBX.
/// Coloque este componente no objeto raiz da fábrica e use o menu de contexto:
/// "Apply Z-Fight Fixes".
///
/// O script faz três coisas principais:
/// 1) Levanta decalques/linhas/faixas do chão para ficarem acima da superfície correta.
/// 2) Empurra janelas, placas, painéis e vidros levemente para fora da superfície onde estão colados.
/// 3) Desativa renderizadores duplicados com o mesmo nome/base, mesma posição e mesmo tamanho.
///
/// Recomendado: rode no Editor, depois dos scripts de setup/posição/sala de controle.
/// </summary>
[ExecuteAlways]
public class FactoryZFightFixer : MonoBehaviour
{
    [Header("Execução")]
    [Tooltip("Se marcado, aplica a correção automaticamente no Start. Normalmente é melhor deixar desligado e rodar pelo menu do componente.")]
    public bool fixOnStart = false;

    [Tooltip("Desativa renderizadores duplicados quando parecem ser cópias exatas do mesmo objeto.")]
    public bool disableExactDuplicateRenderers = true;

    [Tooltip("Mostra um resumo no Console depois de aplicar as correções.")]
    public bool verboseLogs = true;

    [Header("Offsets contra Z-Fighting")]
    [Tooltip("Distância para deixar decalques, linhas e marcações acima de pisos, rampas, plataformas e teto.")]
    public float horizontalSurfaceGap = 0.025f;

    [Tooltip("Distância para afastar janelas, placas, painéis e vidros de paredes/máquinas.")]
    public float verticalSurfaceGap = 0.025f;

    [Tooltip("Distância extra para objetos muito brilhantes/emissivos, como placas e status lights.")]
    public float emissiveSurfaceGap = 0.035f;

    [Header("Busca de suporte")]
    [Tooltip("Quanto a área X/Z precisa se sobrepor para considerar que um objeto está em cima de outro.")]
    [Range(0.01f, 1f)]
    public float minimumFootprintOverlap = 0.12f;

    [Tooltip("Altura máxima acima do suporte para procurar onde um decalque deveria ficar.")]
    public float maxSupportSearchHeight = 2.5f;

    [Header("Duplicados")]
    [Tooltip("Tolerância para detectar duplicados exatos. Aumente um pouco se o FBX estiver com escala estranha.")]
    public float duplicateTolerance = 0.002f;

    [Tooltip("Não desativa objetos importantes mesmo que pareçam duplicados.")]
    public bool protectInteractiveObjects = true;

    [Header("Categorias detectadas")]
    [Tooltip("Objetos com esses termos são tratados como marcações horizontais que devem ficar por cima do chão/teto/plataforma.")]
    public string[] horizontalDecalKeywords =
    {
        "Safety", "Parking_Line", "Path", "Line", "Stripe", "Marking", "Zone", "Skylight", "Floor_Mark", "Arrow"
    };

    [Tooltip("Objetos com esses termos são afastados de paredes/máquinas para evitar briga visual com superfícies verticais.")]
    public string[] verticalDetailKeywords =
    {
        "Window", "Sign", "Panel", "Handle", "Glass", "Plate", "Label", "Screen", "Monitor", "Light", "Status"
    };

    [Tooltip("Objetos com esses termos podem servir como chão, plataforma, parede, teto ou máquina para decalques encaixarem.")]
    public string[] supportKeywords =
    {
        "Floor", "Base", "Dock", "Ramp", "Platform", "Roof", "Ceiling", "Stairs", "Step", "Conveyor", "Machine", "Pallet", "Rack", "Shelf", "Wall"
    };

    private int movedHorizontal;
    private int movedVertical;
    private int disabledDuplicates;
    private int skippedProtected;

    private void Start()
    {
        if (Application.isPlaying && fixOnStart)
        {
            ApplyZFightFixes();
        }
    }

    [ContextMenu("Apply Z-Fight Fixes")]
    public void ApplyZFightFixes()
    {
        movedHorizontal = 0;
        movedVertical = 0;
        disabledDuplicates = 0;
        skippedProtected = 0;

        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        List<Renderer> activeRenderers = new List<Renderer>();

        foreach (Renderer renderer in renderers)
        {
            if (renderer == null || renderer.transform == transform) continue;
            if (!renderer.gameObject.activeInHierarchy && Application.isPlaying) continue;
            if (!renderer.enabled) continue;
            activeRenderers.Add(renderer);
        }

        if (disableExactDuplicateRenderers)
        {
            DisableDuplicateRenderers(activeRenderers);
            // Recoleta depois de desativar duplicados.
            renderers = GetComponentsInChildren<Renderer>(true);
            activeRenderers.Clear();
            foreach (Renderer renderer in renderers)
            {
                if (renderer == null || renderer.transform == transform) continue;
                if (!renderer.enabled) continue;
                if (!renderer.gameObject.activeInHierarchy && Application.isPlaying) continue;
                activeRenderers.Add(renderer);
            }
        }

        List<Renderer> supportRenderers = new List<Renderer>();
        foreach (Renderer renderer in activeRenderers)
        {
            string n = renderer.gameObject.name;
            if (IsSupport(n) && !IsHorizontalDecal(n))
            {
                supportRenderers.Add(renderer);
            }
        }

        // Primeiro corrige as coisas horizontais: faixas, linhas, skylights, marcações.
        foreach (Renderer renderer in activeRenderers)
        {
            if (renderer == null || !renderer.enabled) continue;
            string n = renderer.gameObject.name;

            if (!IsHorizontalDecal(n)) continue;
            if (IsProtectedInteractive(n)) continue;

            bool moved = LiftOntoBestSupport(renderer, supportRenderers);
            if (moved) movedHorizontal++;
        }

        // Depois corrige detalhes verticais: janelas, placas, painéis, telas, vidro etc.
        foreach (Renderer renderer in activeRenderers)
        {
            if (renderer == null || !renderer.enabled) continue;
            string n = renderer.gameObject.name;

            if (!IsVerticalDetail(n)) continue;
            if (IsProtectedInteractive(n)) continue;

            bool moved = PushAwayFromSurface(renderer);
            if (moved) movedVertical++;
        }

        if (verboseLogs)
        {
            Debug.Log(
                $"[FactoryZFightFixer] Correção concluída em '{name}'. " +
                $"Horizontais ajustados: {movedHorizontal}. " +
                $"Verticais ajustados: {movedVertical}. " +
                $"Duplicados desativados: {disabledDuplicates}. " +
                $"Protegidos ignorados: {skippedProtected}."
            );
        }
    }

    [ContextMenu("Disable Exact Duplicate Renderers Only")]
    public void DisableExactDuplicateRenderersOnly()
    {
        disabledDuplicates = 0;
        skippedProtected = 0;
        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        DisableDuplicateRenderers(new List<Renderer>(renderers));

        if (verboseLogs)
        {
            Debug.Log($"[FactoryZFightFixer] Duplicados desativados: {disabledDuplicates}. Protegidos ignorados: {skippedProtected}.");
        }
    }

    [ContextMenu("Nudge Selected-Like Flat Details")]
    public void NudgeAllFlatDetails()
    {
        movedVertical = 0;
        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        foreach (Renderer renderer in renderers)
        {
            if (renderer == null || !renderer.enabled) continue;
            if (IsProtectedInteractive(renderer.gameObject.name)) continue;

            Bounds b = renderer.bounds;
            Vector3 s = b.size;
            float min = Mathf.Min(s.x, Mathf.Min(s.y, s.z));

            // Move apenas objetos bem finos.
            if (min <= 0.15f)
            {
                if (PushAwayFromSurface(renderer)) movedVertical++;
            }
        }

        if (verboseLogs)
        {
            Debug.Log($"[FactoryZFightFixer] Flat details ajustados: {movedVertical}.");
        }
    }

    private bool LiftOntoBestSupport(Renderer renderer, List<Renderer> supports)
    {
        if (renderer == null) return false;

        Bounds b = renderer.bounds;
        Renderer best = null;
        float bestTopY = float.NegativeInfinity;

        foreach (Renderer support in supports)
        {
            if (support == null || support == renderer) continue;
            if (!support.enabled) continue;

            Bounds sb = support.bounds;

            // Precisa ter alguma sobreposição X/Z.
            if (!OverlapsXZ(b, sb, minimumFootprintOverlap)) continue;

            // O suporte precisa estar abaixo ou praticamente encostado.
            float topY = sb.max.y;
            float distanceDown = b.min.y - topY;

            if (distanceDown < -0.5f) continue; // objeto está muito enfiado dentro do suporte errado.
            if (distanceDown > maxSupportSearchHeight) continue;

            if (topY > bestTopY)
            {
                bestTopY = topY;
                best = support;
            }
        }

        if (best == null) return false;

        Bounds current = renderer.bounds;
        float targetBottom = bestTopY + horizontalSurfaceGap;
        float deltaY = targetBottom - current.min.y;

        if (Mathf.Abs(deltaY) < 0.0005f) return false;

        MoveWorld(renderer.transform, Vector3.up * deltaY);
        return true;
    }

    private bool PushAwayFromSurface(Renderer renderer)
    {
        if (renderer == null) return false;

        Bounds b = renderer.bounds;
        Vector3 size = b.size;
        Vector3 center = b.center;
        Vector3 rootCenter = transform.position;

        float gap = NeedsExtraEmissiveGap(renderer.gameObject.name) ? emissiveSurfaceGap : verticalSurfaceGap;
        Vector3 move = Vector3.zero;

        // Unity: Y é altura. Para parede frontal/traseira, a espessura costuma estar em Z.
        // Para paredes laterais, a espessura costuma estar em X.
        // Para decalques horizontais, a espessura costuma estar em Y.
        bool xIsThin = size.x <= size.z && size.x <= size.y * 0.75f;
        bool zIsThin = size.z < size.x && size.z <= size.y * 0.75f;
        bool yIsThin = size.y < size.x && size.y < size.z;

        if (xIsThin)
        {
            float dir = Mathf.Sign(center.x - rootCenter.x);
            if (Mathf.Approximately(dir, 0f)) dir = 1f;
            move = Vector3.right * dir * gap;
        }
        else if (zIsThin)
        {
            float dir = Mathf.Sign(center.z - rootCenter.z);
            if (Mathf.Approximately(dir, 0f)) dir = 1f;
            move = Vector3.forward * dir * gap;
        }
        else if (yIsThin)
        {
            // Detalhe horizontal que não entrou como decalque: levanta um pouco.
            move = Vector3.up * gap;
        }
        else
        {
            // Fallback para detalhes como luzes ou placas com proporção estranha.
            Vector3 horizontal = center - rootCenter;
            horizontal.y = 0f;
            if (horizontal.sqrMagnitude < 0.0001f)
            {
                horizontal = Vector3.forward;
            }
            move = horizontal.normalized * gap;
        }

        if (move.sqrMagnitude <= 0.000001f) return false;

        MoveWorld(renderer.transform, move);
        return true;
    }

    private void DisableDuplicateRenderers(List<Renderer> renderers)
    {
        Dictionary<string, Renderer> seen = new Dictionary<string, Renderer>();

        foreach (Renderer renderer in renderers)
        {
            if (renderer == null) continue;
            if (!renderer.enabled) continue;

            string objectName = renderer.gameObject.name;
            if (IsProtectedInteractive(objectName))
            {
                skippedProtected++;
                continue;
            }

            Bounds b = renderer.bounds;
            string normalizedName = NormalizeDuplicateName(objectName);
            string matName = renderer.sharedMaterial != null ? NormalizeDuplicateName(renderer.sharedMaterial.name) : "NoMaterial";

            string key =
                normalizedName + "|" + matName + "|" +
                RoundVector(b.center, duplicateTolerance) + "|" +
                RoundVector(b.size, duplicateTolerance);

            if (seen.ContainsKey(key))
            {
                renderer.enabled = false;

                Collider c = renderer.GetComponent<Collider>();
                if (c != null) c.enabled = false;

                disabledDuplicates++;
            }
            else
            {
                seen.Add(key, renderer);
            }
        }
    }

    private bool IsHorizontalDecal(string objectName)
    {
        return ContainsAny(objectName, horizontalDecalKeywords);
    }

    private bool IsVerticalDetail(string objectName)
    {
        return ContainsAny(objectName, verticalDetailKeywords);
    }

    private bool IsSupport(string objectName)
    {
        return ContainsAny(objectName, supportKeywords);
    }

    private bool NeedsExtraEmissiveGap(string objectName)
    {
        string n = objectName.ToLowerInvariant();
        return n.Contains("light") || n.Contains("sign") || n.Contains("status") || n.Contains("emissive");
    }

    private bool IsProtectedInteractive(string objectName)
    {
        if (!protectInteractiveObjects) return false;

        string n = objectName.ToLowerInvariant();

        // Não mexe em objetos que provavelmente têm scripts de interação/pivô.
        if (n.Contains("loading_gate")) return true;
        if (n.Contains("employee_door")) return true;
        if (n.Contains("control_room_door")) return true;
        if (n.Contains("door_pivot")) return true;
        if (n.Contains("gate_pivot")) return true;
        if (n.Contains("player")) return true;
        if (n.Contains("camera")) return true;

        return false;
    }

    private bool ContainsAny(string objectName, string[] keywords)
    {
        if (string.IsNullOrEmpty(objectName) || keywords == null) return false;

        string n = objectName.ToLowerInvariant();
        for (int i = 0; i < keywords.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(keywords[i])) continue;
            if (n.Contains(keywords[i].ToLowerInvariant())) return true;
        }

        return false;
    }

    private bool OverlapsXZ(Bounds a, Bounds b, float minimumOverlap01)
    {
        float overlapX = Mathf.Min(a.max.x, b.max.x) - Mathf.Max(a.min.x, b.min.x);
        float overlapZ = Mathf.Min(a.max.z, b.max.z) - Mathf.Max(a.min.z, b.min.z);

        if (overlapX <= 0f || overlapZ <= 0f) return false;

        float areaOverlap = overlapX * overlapZ;
        float areaA = Mathf.Max(0.0001f, a.size.x * a.size.z);
        float ratio = areaOverlap / areaA;

        return ratio >= minimumOverlap01;
    }

    private void MoveWorld(Transform t, Vector3 deltaWorld)
    {
        if (t == null) return;

        // Se for objeto importado do FBX marcado como Static, ainda dá para ajustar no Editor,
        // mas é melhor remover Static caso o objeto precise ser animado depois.
        // Aqui não desmarcamos tudo para não destruir otimização do cenário.
        t.position += deltaWorld;
    }

    private string NormalizeDuplicateName(string raw)
    {
        if (string.IsNullOrEmpty(raw)) return "";

        string s = raw;
        s = Regex.Replace(s, @"\s*\(\d+\)$", "");
        s = Regex.Replace(s, @"_Copy\d*$", "", RegexOptions.IgnoreCase);
        s = Regex.Replace(s, @"\.\d{3}$", "");
        return s.Trim().ToLowerInvariant();
    }

    private string RoundVector(Vector3 v, float tolerance)
    {
        float t = Mathf.Max(0.00001f, tolerance);
        int x = Mathf.RoundToInt(v.x / t);
        int y = Mathf.RoundToInt(v.y / t);
        int z = Mathf.RoundToInt(v.z / t);
        return x + "," + y + "," + z;
    }
}
