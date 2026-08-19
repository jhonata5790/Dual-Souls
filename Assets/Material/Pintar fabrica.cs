using UnityEngine;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
public class PintarFabricaPorNome : MonoBehaviour
{
    [Header("Pintura automática")]
    public bool pintarDesconhecidos = true;
    public bool mostrarObjetosDesconhecidos = true;

    [Header("Materiais")]
    public string pastaMateriais =
        "Assets/Lototo/Material/Fabrica_Auto";

    private readonly Dictionary<Categoria, Material> cache =
        new Dictionary<Categoria, Material>();


    // ============================================================
    // PALETA
    // ============================================================

    static readonly Color Concreto =
        Hex("#AEB5B9");

    static readonly Color ConcretoClaro =
        Hex("#CFD4D6");

    static readonly Color Piso =
        Hex("#687279");

    static readonly Color PisoEscuro =
        Hex("#4B5459");

    static readonly Color Metal =
        Hex("#555F65");

    static readonly Color MetalEscuro =
        Hex("#2D3438");

    static readonly Color MetalClaro =
        Hex("#9DA7AB");

    static readonly Color Aco =
        Hex("#727D82");

    static readonly Color AzulIndustrial =
        Hex("#466879");

    static readonly Color AzulHidraulico =
        Hex("#397CA2");

    static readonly Color CianoPneumatico =
        Hex("#4B9DA5");

    static readonly Color AmareloEletrico =
        Hex("#D4AA36");

    static readonly Color AmareloSeguranca =
        Hex("#E0A82E");

    static readonly Color LaranjaMaquina =
        Hex("#C87532");

    static readonly Color Vermelho =
        Hex("#B9483D");

    static readonly Color Verde =
        Hex("#4F8060");

    static readonly Color Madeira =
        Hex("#8B6D50");

    static readonly Color CaixaPapelao =
        Hex("#A78961");

    static readonly Color Borracha =
        Hex("#24292C");

    static readonly Color Branco =
        Hex("#E0E3E2");

    static readonly Color Vidro =
        new Color(0.45f, 0.75f, 0.83f, 0.28f);

    static readonly Color Tela =
        Hex("#16353D");

    static readonly Color LuzCiano =
        Hex("#55DDE5");

    static readonly Color LuzVerde =
        Hex("#63DF91");

    static readonly Color LuzVermelha =
        Hex("#FF5448");

    static readonly Color Default =
        Hex("#818A8F");


    // ============================================================
    // EXECUTAR
    // ============================================================

    [ContextMenu("PINTAR FABRICA INTEIRA")]
    public void PintarFabrica()
    {
#if UNITY_EDITOR

        CriarPasta();

        cache.Clear();

        Renderer[] renderers =
            GetComponentsInChildren<Renderer>(true);

        int pintados = 0;
        int desconhecidos = 0;

        foreach (Renderer renderer in renderers)
        {
            if (renderer == null)
                continue;

            string nome =
                Normalizar(renderer.gameObject.name);

            string contexto =
                ConstruirContexto(renderer.transform);

            Categoria categoria =
                DescobrirCategoria(nome, contexto);

            if (categoria == Categoria.Desconhecido)
            {
                desconhecidos++;

                if (mostrarObjetosDesconhecidos)
                {
                    Debug.Log(
                        "[FABRICA - SEM REGRA] " +
                        renderer.gameObject.name,
                        renderer.gameObject
                    );
                }

                if (!pintarDesconhecidos)
                    continue;
            }

            Material material =
                ObterMaterial(categoria);

            if (material == null)
                continue;

            Undo.RecordObject(
                renderer,
                "Pintar fábrica"
            );

            int quantidadeSlots =
                Mathf.Max(
                    renderer.sharedMaterials.Length,
                    1
                );

            Material[] novos =
                new Material[quantidadeSlots];

            for (int i = 0; i < novos.Length; i++)
                novos[i] = material;

            renderer.sharedMaterials = novos;

            EditorUtility.SetDirty(renderer);

            pintados++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log(
            "<b><color=#6DFF92>FÁBRICA PINTADA!</color></b>\n" +
            "Renderers pintados: " + pintados + "\n" +
            "Sem regra específica: " + desconhecidos
        );

#endif
    }


    // ============================================================
    // IDENTIFICAÇÃO
    // ============================================================

    Categoria DescobrirCategoria(
        string nome,
        string contexto)
    {
        // ========================================================
        // ITENS QUE DEVEM TER PRIORIDADE
        // ========================================================

        if (Tem(nome, "glass", "vidro"))
            return Categoria.Vidro;


        // --------------------------------------------------------
        // TELAS
        // --------------------------------------------------------

        if (Tem(nome,
            "screen",
            "monitor",
            "display"))
        {
            return Categoria.Tela;
        }


        // --------------------------------------------------------
        // LED
        // --------------------------------------------------------

        if (Tem(nome, "led"))
        {
            return Categoria.LED;
        }


        // --------------------------------------------------------
        // BOTÕES
        // --------------------------------------------------------

        if (Tem(nome,
            "botao_emergencia",
            "emergency"))
        {
            return Categoria.BotaoEmergencia;
        }

        if (Tem(nome,
            "botao_acesso",
            "access_button"))
        {
            return Categoria.BotaoAcesso;
        }


        // ========================================================
        // SISTEMA ELÉTRICO
        // ========================================================

        if (Tem(nome,
            "placa_eletrica",
            "eletrica_raio",
            "electrical"))
        {
            return Categoria.Eletrico;
        }

        if (Tem(contexto,
            "porta_eletrica"))
        {
            if (Tem(nome,
                "handle",
                "frame",
                "lintel"))
                return Categoria.MetalEscuro;

            return Categoria.Eletrico;
        }


        // ========================================================
        // HIDRÁULICA
        // ========================================================

        if (Tem(nome,
            "bomba_hidraulica",
            "reservatorio_hidraulico",
            "corpo_cilindro_hidraulico",
            "base_cilindro",
            "skid_hidraulico",
            "tanque_hid",
            "placa_hid",
            "hidraulica"))
        {
            return Categoria.Hidraulico;
        }


        // ========================================================
        // COMPRESSORES / PNEUMÁTICA
        // ========================================================

        if (Tem(nome,
            "compressor",
            "pulmao_ar",
            "pneumat"))
        {
            if (Tem(nome,
                "skid",
                "motor",
                "frame"))
                return Categoria.MetalEscuro;

            return Categoria.Pneumatico;
        }

        if (Tem(contexto,
            "porta_compressores"))
        {
            return Categoria.Pneumatico;
        }


        // ========================================================
        // EMPILHADEIRA
        // ========================================================

        if (Tem(nome, "empilhadeira"))
        {
            if (Tem(nome,
                "fork",
                "garfo"))
                return Categoria.MetalClaro;

            if (Tem(nome,
                "mast",
                "mastro"))
                return Categoria.MetalEscuro;

            return Categoria.Empilhadeira;
        }


        // ========================================================
        // ILHAS / PRATELEIRAS GRANDES
        // ========================================================

        if (Tem(contexto,
            "ilha_direita",
            "ilha_esquerda"))
        {
            if (Tem(nome, "led"))
                return Categoria.LED;

            if (Tem(nome,
                "bay"))
                return Categoria.Prateleira;

            if (Tem(nome,
                "frontframe",
                "border",
                "body",
                "base"))
                return Categoria.EstruturaEscura;

            return Categoria.Prateleira;
        }


        // ========================================================
        // ALMOXARIFADO
        // ========================================================

        if (Tem(nome,
            "estante_almox"))
        {
            if (Tem(nome, "board"))
                return Categoria.Prateleira;

            return Categoria.EstruturaEscura;
        }

        if (Tem(nome,
            "faixa_almox"))
        {
            return Categoria.Seguranca;
        }


        // ========================================================
        // TOMBADOR DE GRÃOS
        // ========================================================

        if (Tem(contexto,
            "tombador de graos",
            "tombador_de_graos"))
        {
            if (Tem(nome,
                "bocal_descarga"))
                return Categoria.MetalEscuro;

            if (Tem(nome,
                "bollard"))
                return Categoria.Seguranca;

            if (Tem(nome,
                "coletor_po"))
                return Categoria.MetalClaro;

            if (Tem(nome,
                "rolete"))
                return Categoria.MetalEscuro;

            if (Tem(nome,
                "tambor_lubrificante"))
                return Categoria.Laranja;

            if (Tem(nome,
                "carcaca_rosca"))
                return Categoria.MetalEscuro;

            if (Tem(nome,
                "caixa_moega",
                "deck",
                "chapa"))
                return Categoria.Aco;

            if (Tem(nome,
                "coluna",
                "travessa",
                "viga",
                "reforco",
                "suporte"))
                return Categoria.EstruturaEscura;
        }


        // ========================================================
        // TRANSFORMADOR
        // ========================================================

        if (Tem(nome, "transformador"))
        {
            if (Tem(nome, "bushing"))
                return Categoria.Ceramica;

            return Categoria.MetalEscuro;
        }


        // ========================================================
        // VENTILAÇÃO
        // ========================================================

        if (Tem(nome, "vent_"))
        {
            if (Tem(nome, "slat"))
                return Categoria.Metal;

            return Categoria.MetalEscuro;
        }


        // ========================================================
        // PORTAS
        // ========================================================

        if (Tem(nome,
            "porta_",
            "porta ",
            "door"))
        {
            if (Tem(nome,
                "frame",
                "lintel",
                "handle",
                "rail",
                "trilho"))
            {
                return Categoria.MetalEscuro;
            }

            return Categoria.Porta;
        }


        // ========================================================
        // SALA DE COMANDO
        // ========================================================

        if (Tem(nome,
            "console_comando"))
        {
            if (Tem(nome, "screen"))
                return Categoria.Tela;

            return Categoria.Console;
        }

        if (Tem(nome,
            "cmd_cadeira"))
        {
            if (Tem(nome, "seat"))
                return Categoria.Estofado;

            return Categoria.MetalEscuro;
        }


        // ========================================================
        // MANUTENÇÃO
        // ========================================================

        if (Tem(nome,
            "bancada_manutencao"))
        {
            if (Tem(nome, "top"))
                return Categoria.MetalClaro;

            return Categoria.AzulIndustrial;
        }

        if (Tem(nome,
            "armario_ferramentas"))
        {
            return Categoria.Armario;
        }

        if (Tem(nome,
            "bandeja_cabos"))
        {
            return Categoria.MetalEscuro;
        }


        // ========================================================
        // LOJA
        // ========================================================

        if (Tem(nome,
            "balcao_loja"))
        {
            return Categoria.AzulIndustrial;
        }

        if (Tem(nome,
            "cadeira_loja"))
        {
            if (Tem(nome, "seat"))
                return Categoria.Estofado;

            return Categoria.MetalEscuro;
        }

        if (Tem(nome,
            "prateleira_loja"))
        {
            if (Tem(nome, "board"))
                return Categoria.Prateleira;

            return Categoria.MetalEscuro;
        }

        if (Tem(nome,
            "terminal_loja"))
        {
            if (Tem(nome, "screen"))
                return Categoria.Tela;

            return Categoria.Console;
        }


        // ========================================================
        // CAIXAS
        // ========================================================

        if (Tem(nome,
            "caixa_a",
            "caixa_b",
            "caixa_c",
            "caixa_pecas"))
        {
            return Categoria.Caixa;
        }


        // ========================================================
        // PLACAS / SINALIZAÇÃO
        // ========================================================

        if (Tem(nome,
            "placa_paineis",
            "placa_principal",
            "placa_"))
        {
            if (Tem(nome, "text"))
                return Categoria.Texto;

            if (Tem(nome,
                "eletrica",
                "raio"))
                return Categoria.Eletrico;

            if (Tem(nome, "hid"))
                return Categoria.Hidraulico;

            return Categoria.Placa;
        }


        // ========================================================
        // ESTRUTURA
        // ========================================================

        if (Tem(nome,
            "parede",
            "wall"))
        {
            return Categoria.Parede;
        }

        if (Tem(nome,
            "piso",
            "floor"))
        {
            return Categoria.Piso;
        }

        if (Tem(nome,
            "passarela",
            "patamar"))
        {
            return Categoria.PisoIndustrial;
        }

        if (Tem(nome,
            "coluna",
            "viga",
            "travessa",
            "reforco_diagonal",
            "estrutura",
            "suporte_eixo"))
        {
            return Categoria.EstruturaEscura;
        }


        // ========================================================
        // NOMES DE SALA DIRECIONAIS
        //
        // Ex:
        // CENTRO_COMANDOS_N_0
        // ALMOXARIFADO_MANUTENCAO_E_0
        // AREA_DESCANSO_S_0
        // ========================================================

        if (EhParedeDeSala(nome))
        {
            return Categoria.Parede;
        }


        // ========================================================
        // PEÇAS MECÂNICAS GENÉRICAS
        // ========================================================

        if (Tem(nome,
            "rolamento",
            "eixo",
            "shaft",
            "rail",
            "trilho",
            "mancal",
            "flange"))
        {
            return Categoria.MetalEscuro;
        }


        // ========================================================
        // ARMÁRIOS
        // ========================================================

        if (Tem(nome, "armario"))
        {
            if (Tem(nome,
                "puxador",
                "handle",
                "fechadura"))
                return Categoria.MetalEscuro;

            return Categoria.Armario;
        }


        // ========================================================
        // MÓVEIS
        // ========================================================

        if (Tem(nome,
            "cadeira",
            "seat"))
        {
            return Categoria.Estofado;
        }


        // ========================================================
        // BORRACHA
        // ========================================================

        if (Tem(nome,
            "borracha",
            "rubber",
            "pneu",
            "tire"))
        {
            return Categoria.Borracha;
        }


        // ========================================================
        // DEFAULT
        // ========================================================

        return Categoria.Desconhecido;
    }


    // ============================================================
    // NOMES DAS SALAS
    // ============================================================

    bool EhParedeDeSala(string nome)
    {
        if (
            nome.StartsWith("centro_comandos_") ||
            nome.StartsWith("almoxarifado_manutencao_") ||
            nome.StartsWith("area_descanso_")
        )
        {
            return true;
        }

        return false;
    }


    // ============================================================
    // MATERIAL
    // ============================================================

#if UNITY_EDITOR

    Material ObterMaterial(Categoria categoria)
    {
        if (cache.TryGetValue(
            categoria,
            out Material existente))
        {
            return existente;
        }

        string caminho =
            pastaMateriais +
            "/MAT_" +
            categoria +
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

            material = new Material(shader);

            material.name =
                "MAT_" + categoria;

            AssetDatabase.CreateAsset(
                material,
                caminho
            );
        }

        ConfigurarMaterial(
            material,
            categoria
        );

        EditorUtility.SetDirty(material);

        cache[categoria] = material;

        return material;
    }

#endif


    void ConfigurarMaterial(
        Material material,
        Categoria categoria)
    {
        Color cor =
            CorDaCategoria(categoria);

        bool emissivo =
            categoria == Categoria.LED ||
            categoria == Categoria.Tela ||
            categoria == Categoria.BotaoAcesso ||
            categoria == Categoria.BotaoEmergencia;

        bool transparente =
            categoria == Categoria.Vidro;


        // --------------------------------------------------------
        // COR
        // --------------------------------------------------------

        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", cor);

        if (material.HasProperty("_Color"))
            material.SetColor("_Color", cor);


        // --------------------------------------------------------
        // METAL / ROUGHNESS
        // --------------------------------------------------------

        float metalico = 0.15f;
        float smoothness = 0.3f;

        switch (categoria)
        {
            case Categoria.Metal:
            case Categoria.MetalClaro:
            case Categoria.MetalEscuro:
            case Categoria.Aco:
            case Categoria.EstruturaEscura:
                metalico = 0.75f;
                smoothness = 0.32f;
                break;

            case Categoria.Piso:
            case Categoria.PisoIndustrial:
            case Categoria.Parede:
                metalico = 0f;
                smoothness = 0.18f;
                break;

            case Categoria.Vidro:
                metalico = 0f;
                smoothness = 0.88f;
                break;

            case Categoria.Borracha:
                metalico = 0f;
                smoothness = 0.08f;
                break;
        }

        if (material.HasProperty("_Metallic"))
            material.SetFloat("_Metallic", metalico);

        if (material.HasProperty("_Smoothness"))
            material.SetFloat("_Smoothness", smoothness);


        // --------------------------------------------------------
        // EMISSÃO
        // --------------------------------------------------------

        if (emissivo)
        {
            material.EnableKeyword("_EMISSION");

            Color emissao = cor * 3.8f;

            if (material.HasProperty("_EmissionColor"))
                material.SetColor(
                    "_EmissionColor",
                    emissao
                );
        }
        else
        {
            material.DisableKeyword("_EMISSION");
        }


        // --------------------------------------------------------
        // VIDRO
        // --------------------------------------------------------

        if (transparente)
        {
            ConfigurarTransparencia(material);
        }
        else
        {
            ConfigurarOpaco(material);
        }
    }


    void ConfigurarTransparencia(Material mat)
    {
        // URP
        if (mat.HasProperty("_Surface"))
        {
            mat.SetFloat("_Surface", 1);
            mat.SetFloat("_Blend", 0);
            mat.SetFloat("_ZWrite", 0);

            mat.SetFloat(
                "_SrcBlend",
                (float)UnityEngine.Rendering.BlendMode.SrcAlpha
            );

            mat.SetFloat(
                "_DstBlend",
                (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha
            );

            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");

            mat.renderQueue = 3000;
        }

        // Standard fallback
        if (mat.HasProperty("_Mode"))
        {
            mat.SetFloat("_Mode", 3);

            mat.SetInt(
                "_SrcBlend",
                (int)UnityEngine.Rendering.BlendMode.SrcAlpha
            );

            mat.SetInt(
                "_DstBlend",
                (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha
            );

            mat.SetInt("_ZWrite", 0);

            mat.DisableKeyword("_ALPHATEST_ON");
            mat.EnableKeyword("_ALPHABLEND_ON");

            mat.renderQueue = 3000;
        }
    }


    void ConfigurarOpaco(Material mat)
    {
        if (mat.HasProperty("_Surface"))
        {
            mat.SetFloat("_Surface", 0);
            mat.SetFloat("_ZWrite", 1);

            mat.DisableKeyword(
                "_SURFACE_TYPE_TRANSPARENT"
            );

            mat.renderQueue = -1;
        }

        if (mat.HasProperty("_Mode"))
        {
            mat.SetFloat("_Mode", 0);

            mat.SetInt(
                "_SrcBlend",
                (int)UnityEngine.Rendering.BlendMode.One
            );

            mat.SetInt(
                "_DstBlend",
                (int)UnityEngine.Rendering.BlendMode.Zero
            );

            mat.SetInt("_ZWrite", 1);

            mat.DisableKeyword("_ALPHABLEND_ON");
            mat.DisableKeyword("_ALPHATEST_ON");

            mat.renderQueue = -1;
        }
    }


    // ============================================================
    // CORES
    // ============================================================

    Color CorDaCategoria(Categoria categoria)
    {
        switch (categoria)
        {
            case Categoria.Parede:
                return ConcretoClaro;

            case Categoria.Piso:
                return Piso;

            case Categoria.PisoIndustrial:
                return PisoEscuro;

            case Categoria.Metal:
                return Metal;

            case Categoria.MetalEscuro:
            case Categoria.EstruturaEscura:
                return MetalEscuro;

            case Categoria.MetalClaro:
                return MetalClaro;

            case Categoria.Aco:
                return Aco;

            case Categoria.AzulIndustrial:
            case Categoria.Armario:
            case Categoria.Console:
                return AzulIndustrial;

            case Categoria.Hidraulico:
                return AzulHidraulico;

            case Categoria.Pneumatico:
                return CianoPneumatico;

            case Categoria.Eletrico:
                return AmareloEletrico;

            case Categoria.Seguranca:
                return AmareloSeguranca;

            case Categoria.Empilhadeira:
            case Categoria.Laranja:
                return LaranjaMaquina;

            case Categoria.BotaoEmergencia:
                return LuzVermelha;

            case Categoria.BotaoAcesso:
                return LuzVerde;

            case Categoria.Vidro:
                return Vidro;

            case Categoria.Tela:
                return Tela;

            case Categoria.LED:
                return LuzCiano;

            case Categoria.Porta:
                return AzulIndustrial;

            case Categoria.Prateleira:
                return Aco;

            case Categoria.Estofado:
                return Verde;

            case Categoria.Caixa:
                return CaixaPapelao;

            case Categoria.Madeira:
                return Madeira;

            case Categoria.Borracha:
                return Borracha;

            case Categoria.Ceramica:
            case Categoria.Texto:
                return Branco;

            case Categoria.Placa:
                return MetalClaro;

            default:
                return Default;
        }
    }


    // ============================================================
    // CONTEXTO DA HIERARQUIA
    // ============================================================

    string ConstruirContexto(Transform transform)
    {
        StringBuilder sb = new StringBuilder();

        Transform atual = transform;

        while (atual != null)
        {
            sb.Append(
                Normalizar(atual.name)
            );

            sb.Append("/");

            if (atual == this.transform)
                break;

            atual = atual.parent;
        }

        return sb.ToString();
    }


    // ============================================================
    // UTILITÁRIOS
    // ============================================================

    bool Tem(
        string texto,
        params string[] palavras)
    {
        foreach (string palavra in palavras)
        {
            if (texto.Contains(
                Normalizar(palavra)))
            {
                return true;
            }
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

        StringBuilder resultado =
            new StringBuilder();

        foreach (char c in texto)
        {
            UnicodeCategory categoria =
                CharUnicodeInfo
                .GetUnicodeCategory(c);

            if (categoria !=
                UnicodeCategory.NonSpacingMark)
            {
                resultado.Append(c);
            }
        }

        return resultado
            .ToString()
            .Normalize(
                NormalizationForm.FormC
            );
    }


#if UNITY_EDITOR

    void CriarPasta()
    {
        string[] partes =
            pastaMateriais.Split('/');

        string atual = partes[0];

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


    // ============================================================
    // CATEGORIAS
    // ============================================================

    enum Categoria
    {
        Parede,
        Piso,
        PisoIndustrial,

        Metal,
        MetalClaro,
        MetalEscuro,
        Aco,
        EstruturaEscura,

        AzulIndustrial,

        Armario,
        Console,

        Hidraulico,
        Pneumatico,
        Eletrico,

        Seguranca,

        Empilhadeira,
        Laranja,

        Porta,
        Vidro,

        Tela,
        LED,

        BotaoEmergencia,
        BotaoAcesso,

        Prateleira,

        Estofado,
        Caixa,
        Madeira,
        Borracha,
        Ceramica,

        Placa,
        Texto,

        Desconhecido
    }
}