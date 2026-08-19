using UnityEngine;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
public class PintarSalaPorNome : MonoBehaviour
{
    [Header("Configuração")]
    public bool pintarObjetosDesconhecidos = true;
    public bool mostrarLog = true;

    [Header("Pasta dos materiais")]
    public string pastaMateriais = "Assets/Lototo/Material/SalaDescanso_Auto";

    private Dictionary<string, Material> materiais =
        new Dictionary<string, Material>();

    // =========================================================
    // CORES
    // =========================================================

    static readonly Color COR_PAREDE =
        Hex("#D8DDE2");

    static readonly Color COR_TETO =
        Hex("#ECEFF1");

    static readonly Color COR_PISO =
        Hex("#9CA8B2");

    static readonly Color COR_PORTA =
        Hex("#89949C");

    static readonly Color COR_METAL =
        Hex("#414A50");

    static readonly Color COR_METAL_CLARO =
        Hex("#A9B1B6");

    static readonly Color COR_ARMARIO =
        Hex("#C7CED1");

    static readonly Color COR_ETIQUETA =
        Hex("#E8DFBC");

    static readonly Color COR_SOFA =
        Hex("#738274");

    static readonly Color COR_ALMOFADA =
        Hex("#DDD3C3");

    static readonly Color COR_ALMOFADA_VERDE =
        Hex("#73866C");

    static readonly Color COR_PUFF =
        Hex("#CBBEAA");

    static readonly Color COR_TAPETE =
        Hex("#BEB4A4");

    static readonly Color COR_MESA =
        Hex("#D6C5A9");

    static readonly Color COR_MADEIRA =
        Hex("#96775D");

    static readonly Color COR_PLANTA =
        Hex("#557252");

    static readonly Color COR_CAUle =
        Hex("#665441");

    static readonly Color COR_VASO =
        Hex("#D3D0C7");

    static readonly Color COR_CERAMICA =
        Hex("#ECE8DF");

    static readonly Color COR_PRETO =
        Hex("#202528");

    static readonly Color COR_ELETRO =
        Hex("#30373B");

    static readonly Color COR_QUADRO =
        Hex("#3A3C3D");

    static readonly Color COR_POSTER =
        Hex("#A84F4C");

    static readonly Color COR_LIXEIRA =
        Hex("#A8AFB1");

    static readonly Color COR_LUZ =
        new Color(1.0f, 0.70f, 0.32f, 1f);

    static readonly Color COR_DEFAULT =
        Hex("#BABFC2");


    // =========================================================
    // BOTÃO PRINCIPAL
    // =========================================================

    [ContextMenu("PINTAR SALA AGORA")]
    public void PintarSala()
    {
#if UNITY_EDITOR

        CriarPastaSeNecessario();

        materiais.Clear();

        Renderer[] renderers =
            GetComponentsInChildren<Renderer>(true);

        int pintados = 0;
        int desconhecidos = 0;

        foreach (Renderer renderer in renderers)
        {
            if (renderer == null)
                continue;

            GameObject obj = renderer.gameObject;

            string nome = Normalizar(obj.name);

            Categoria categoria = DescobrirCategoria(nome);

            if (categoria == Categoria.Desconhecido)
            {
                desconhecidos++;

                if (!pintarObjetosDesconhecidos)
                {
                    if (mostrarLog)
                        Debug.Log(
                            "[Sala] Sem categoria: "
                            + obj.name,
                            obj
                        );

                    continue;
                }
            }

            Material material =
                ObterMaterial(categoria);

            if (material == null)
                continue;

            Undo.RecordObject(
                renderer,
                "Pintar sala de descanso"
            );

            Material[] slots =
                new Material[
                    Mathf.Max(
                        1,
                        renderer.sharedMaterials.Length
                    )
                ];

            for (int i = 0; i < slots.Length; i++)
                slots[i] = material;

            renderer.sharedMaterials = slots;

            EditorUtility.SetDirty(renderer);

            pintados++;
        }

        Debug.Log(
            $"<color=#7CFF9B><b>SALA PINTADA!</b></color>\n" +
            $"Objetos pintados: {pintados}\n" +
            $"Objetos sem categoria específica: {desconhecidos}"
        );

        AssetDatabase.SaveAssets();

#else

        Debug.LogWarning(
            "A pintura automática foi feita para ser usada dentro do Editor."
        );

#endif
    }


    // =========================================================
    // DETECÇÃO PELO NOME
    // =========================================================

    Categoria DescobrirCategoria(string nome)
    {
        // -----------------------------------------------------
        // LUZ
        // -----------------------------------------------------

        if (Tem(nome,
            "lampada",
            "bulb",
            "luz acesa"))
            return Categoria.Luz;

        // -----------------------------------------------------
        // ALMOFADAS
        // -----------------------------------------------------

        if (Tem(nome,
            "almofada verde"))
            return Categoria.AlmofadaVerde;

        if (Tem(nome,
            "almofada",
            "cushion"))
            return Categoria.Almofada;

        // -----------------------------------------------------
        // PLANTAS
        // -----------------------------------------------------

        if (Tem(nome,
            "folha",
            "folhagem",
            "planta",
            "leaf",
            "folhas"))
            return Categoria.Planta;

        if (Tem(nome,
            "caule",
            "galho",
            "tronco",
            "stem"))
            return Categoria.Caule;

        if (Tem(nome,
            "vaso",
            "plant pot"))
            return Categoria.Vaso;

        // -----------------------------------------------------
        // METAIS PEQUENOS
        // IMPORTANTE: vem antes de armário
        // -----------------------------------------------------

        if (Tem(nome,
            "puxador",
            "fechadura",
            "arruela",
            "parafuso",
            "porca",
            "dobradica",
            "respiro",
            "alca",
            "alça",
            "suporte",
            "ferragem",
            "metal",
            "gancho"))
            return Categoria.Metal;

        // -----------------------------------------------------
        // ARMÁRIO
        // -----------------------------------------------------

        if (Tem(nome,
            "etiqueta",
            "label"))
            return Categoria.Etiqueta;

        if (Tem(nome,
            "armario",
            "locker",
            "gabinete"))
            return Categoria.Armario;

        // -----------------------------------------------------
        // MÓVEIS
        // -----------------------------------------------------

        if (Tem(nome,
            "sofa",
            "sofa",
            "couch"))
            return Categoria.Sofa;

        if (Tem(nome,
            "puff",
            "pufe"))
            return Categoria.Puff;

        if (Tem(nome,
            "tapete",
            "rug",
            "carpet"))
            return Categoria.Tapete;

        if (Tem(nome,
            "mesa",
            "table"))
            return Categoria.Mesa;

        if (Tem(nome,
            "madeira",
            "wood"))
            return Categoria.Madeira;

        // -----------------------------------------------------
        // COZINHA / CAFÉ
        // -----------------------------------------------------

        if (Tem(nome,
            "caneca",
            "xicara",
            "copo",
            "prato",
            "ceramica"))
            return Categoria.Ceramica;

        if (Tem(nome,
            "cafeteira",
            "maquina de cafe",
            "microondas",
            "torradeira",
            "eletrodomestico"))
            return Categoria.Eletro;

        if (Tem(nome,
            "lixeira",
            "trash",
            "garbage"))
            return Categoria.Lixeira;

        // -----------------------------------------------------
        // ELETRÔNICOS
        // -----------------------------------------------------

        if (Tem(nome,
            "tv",
            "televisao",
            "televisao",
            "screen",
            "tela"))
            return Categoria.Preto;

        if (Tem(nome,
            "controle",
            "remote",
            "cabo",
            "fio"))
            return Categoria.Preto;

        // -----------------------------------------------------
        // DECORAÇÃO
        // -----------------------------------------------------

        if (Tem(nome,
            "poster",
            "imagem",
            "foto",
            "picture"))
            return Categoria.Poster;

        if (Tem(nome,
            "quadro",
            "moldura",
            "frame"))
            return Categoria.Quadro;

        // -----------------------------------------------------
        // LUMINÁRIAS
        // -----------------------------------------------------

        if (Tem(nome,
            "luminaria",
            "pendente",
            "abajur",
            "lamp",
            "lustre"))
            return Categoria.Preto;

        // -----------------------------------------------------
        // ARQUITETURA
        // -----------------------------------------------------

        if (Tem(nome,
            "porta",
            "door"))
            return Categoria.Porta;

        if (Tem(nome,
            "parede",
            "wall"))
            return Categoria.Parede;

        if (Tem(nome,
            "teto",
            "ceiling"))
            return Categoria.Teto;

        if (Tem(nome,
            "piso",
            "chao",
            "floor"))
            return Categoria.Piso;

        // -----------------------------------------------------
        // BANCADA
        // -----------------------------------------------------

        if (Tem(nome,
            "bancada",
            "counter"))
            return Categoria.Armario;

        // -----------------------------------------------------
        // DEFAULT
        // -----------------------------------------------------

        return Categoria.Desconhecido;
    }


    // =========================================================
    // MATERIAIS
    // =========================================================

#if UNITY_EDITOR

    Material ObterMaterial(Categoria categoria)
    {
        string chave = categoria.ToString();

        if (materiais.TryGetValue(
            chave,
            out Material existente))
            return existente;

        Color cor = ObterCor(categoria);

        string caminho =
            pastaMateriais +
            "/MAT_" +
            chave +
            ".mat";

        Material material =
            AssetDatabase.LoadAssetAtPath<Material>(
                caminho
            );

        if (material == null)
        {
            Shader shader =
                Shader.Find(
                    "Universal Render Pipeline/Lit"
                );

            if (shader == null)
                shader = Shader.Find("Standard");

            material =
                new Material(shader);

            material.name =
                "MAT_" + chave;

            ConfigurarCor(
                material,
                cor,
                categoria == Categoria.Luz
            );

            AssetDatabase.CreateAsset(
                material,
                caminho
            );
        }
        else
        {
            ConfigurarCor(
                material,
                cor,
                categoria == Categoria.Luz
            );

            EditorUtility.SetDirty(material);
        }

        materiais[chave] = material;

        return material;
    }

#endif


    void ConfigurarCor(
        Material mat,
        Color cor,
        bool emissivo)
    {
        if (mat.HasProperty("_BaseColor"))
            mat.SetColor("_BaseColor", cor);

        if (mat.HasProperty("_Color"))
            mat.SetColor("_Color", cor);

        mat.SetFloat("_Smoothness", 0.35f);

        if (emissivo)
        {
            mat.EnableKeyword("_EMISSION");

            Color emission =
                cor * 3.5f;

            if (mat.HasProperty("_EmissionColor"))
                mat.SetColor(
                    "_EmissionColor",
                    emission
                );
        }
    }


    Color ObterCor(Categoria categoria)
    {
        switch (categoria)
        {
            case Categoria.Parede:
                return COR_PAREDE;

            case Categoria.Teto:
                return COR_TETO;

            case Categoria.Piso:
                return COR_PISO;

            case Categoria.Porta:
                return COR_PORTA;

            case Categoria.Metal:
                return COR_METAL;

            case Categoria.MetalClaro:
                return COR_METAL_CLARO;

            case Categoria.Armario:
                return COR_ARMARIO;

            case Categoria.Etiqueta:
                return COR_ETIQUETA;

            case Categoria.Sofa:
                return COR_SOFA;

            case Categoria.Almofada:
                return COR_ALMOFADA;

            case Categoria.AlmofadaVerde:
                return COR_ALMOFADA_VERDE;

            case Categoria.Puff:
                return COR_PUFF;

            case Categoria.Tapete:
                return COR_TAPETE;

            case Categoria.Mesa:
                return COR_MESA;

            case Categoria.Madeira:
                return COR_MADEIRA;

            case Categoria.Planta:
                return COR_PLANTA;

            case Categoria.Caule:
                return COR_CAUle;

            case Categoria.Vaso:
                return COR_VASO;

            case Categoria.Ceramica:
                return COR_CERAMICA;

            case Categoria.Preto:
                return COR_PRETO;

            case Categoria.Eletro:
                return COR_ELETRO;

            case Categoria.Quadro:
                return COR_QUADRO;

            case Categoria.Poster:
                return COR_POSTER;

            case Categoria.Lixeira:
                return COR_LIXEIRA;

            case Categoria.Luz:
                return COR_LUZ;

            default:
                return COR_DEFAULT;
        }
    }


    // =========================================================
    // UTILITÁRIOS
    // =========================================================

    bool Tem(
        string texto,
        params string[] palavras)
    {
        foreach (string palavra in palavras)
        {
            if (texto.Contains(
                Normalizar(palavra)))
                return true;
        }

        return false;
    }


    static string Normalizar(string texto)
    {
        if (string.IsNullOrEmpty(texto))
            return "";

        texto =
            texto
            .ToLowerInvariant()
            .Normalize(
                NormalizationForm.FormD
            );

        StringBuilder sb =
            new StringBuilder();

        foreach (char c in texto)
        {
            UnicodeCategory categoria =
                CharUnicodeInfo.GetUnicodeCategory(c);

            if (categoria !=
                UnicodeCategory.NonSpacingMark)
            {
                sb.Append(c);
            }
        }

        return sb
            .ToString()
            .Normalize(
                NormalizationForm.FormC
            );
    }


#if UNITY_EDITOR

    void CriarPastaSeNecessario()
    {
        string[] partes =
            pastaMateriais.Split('/');

        string atual =
            partes[0];

        for (int i = 1;
             i < partes.Length;
             i++)
        {
            string proximo =
                atual + "/" + partes[i];

            if (!AssetDatabase.IsValidFolder(
                proximo))
            {
                AssetDatabase.CreateFolder(
                    atual,
                    partes[i]
                );
            }

            atual = proximo;
        }
    }

#endif


    static Color Hex(string hex)
    {
        ColorUtility.TryParseHtmlString(
            hex,
            out Color cor
        );

        return cor;
    }


    enum Categoria
    {
        Parede,
        Teto,
        Piso,
        Porta,

        Metal,
        MetalClaro,

        Armario,
        Etiqueta,

        Sofa,
        Almofada,
        AlmofadaVerde,
        Puff,
        Tapete,
        Mesa,
        Madeira,

        Planta,
        Caule,
        Vaso,

        Ceramica,

        Preto,
        Eletro,

        Quadro,
        Poster,

        Lixeira,

        Luz,

        Desconhecido
    }
}