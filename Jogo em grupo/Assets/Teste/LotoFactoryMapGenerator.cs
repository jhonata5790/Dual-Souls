using UnityEngine;

public class LotoFactoryMapGenerator : MonoBehaviour
{
    [Header("GERAÇÃO")]
    public bool gerarAoIniciar = true;
    public bool apagarMapaAnterior = true;

    [Header("TAMANHO DAS SALAS")]
    public float larguraSala = 18f;
    public float comprimentoSala = 22f;
    public float alturaParede = 4f;
    public float espessuraParede = 0.35f;

    [Header("CORREDORES")]
    public float larguraCorredor = 5f;
    public float distanciaEntreSalas = 8f;

    [Header("CORES DO MAPA")]
    public Color corPiso = new Color(0.32f, 0.32f, 0.32f);
    public Color corParede = new Color(0.48f, 0.48f, 0.48f);
    public Color corMetal = new Color(0.55f, 0.55f, 0.58f);
    public Color corMetalEscuro = new Color(0.08f, 0.08f, 0.09f);
    public Color corAmareloSeguranca = new Color(1f, 0.72f, 0.08f);
    public Color corVermelhoPerigo = new Color(0.85f, 0.04f, 0.03f);
    public Color corVerdeLiberado = new Color(0.1f, 0.9f, 0.25f);
    public Color corAzulPneumatico = new Color(0.05f, 0.35f, 1f);
    public Color corLaranjaAlerta = new Color(1f, 0.4f, 0.05f);
    public Color corVidroDisplay = new Color(0.15f, 0.9f, 1f, 0.45f);
    public Color corVapor = new Color(1f, 1f, 1f, 0.35f);
    public Color corOleoQuimico = new Color(0.95f, 0.72f, 0.18f, 0.85f);

    private Transform raiz;

    private Material matPiso;
    private Material matParede;
    private Material matMetal;
    private Material matEscuro;
    private Material matAmarelo;
    private Material matVermelho;
    private Material matVerde;
    private Material matAzul;
    private Material matLaranja;
    private Material matVidro;
    private Material matVapor;
    private Material matOleo;

    private void Start()
    {
        if (gerarAoIniciar)
        {
            GerarMapa();
        }
    }

    [ContextMenu("GERAR MAPA LOTO")]
    public void GerarMapa()
    {
        if (apagarMapaAnterior)
        {
            ApagarMapaAnterior();
        }

        CriarMateriais();

        GameObject raizObj = new GameObject("MAPA_GERADO_LOTO_FACTORY");
        raiz = raizObj.transform;
        raiz.SetParent(transform);

        Vector3 sala1 = new Vector3(0, 0, 0);
        Vector3 sala2 = new Vector3(0, 0, comprimentoSala + distanciaEntreSalas);
        Vector3 sala3 = new Vector3(0, 0, (comprimentoSala + distanciaEntreSalas) * 2);
        Vector3 sala4 = new Vector3(0, 0, (comprimentoSala + distanciaEntreSalas) * 3);

        CriarSala1_Subestacao(sala1);
        CriarSala2_Prensa(sala2);
        CriarSala3_Utilidades(sala3);
        CriarSala4_Caldeiras(sala4);

        CriarCorredores();
        CriarPlayerTeste();
        CriarIluminacao();
    }

    private void ApagarMapaAnterior()
    {
        Transform antigo = transform.Find("MAPA_GERADO_LOTO_FACTORY");

        if (antigo == null)
        {
            return;
        }

#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            DestroyImmediate(antigo.gameObject);
        }
        else
        {
            Destroy(antigo.gameObject);
        }
#else
        Destroy(antigo.gameObject);
#endif
    }

    private void CriarMateriais()
    {
        matPiso = CriarMaterial("Piso Concreto", corPiso);
        matParede = CriarMaterial("Parede Concreto", corParede);
        matMetal = CriarMaterial("Metal Industrial", corMetal);
        matEscuro = CriarMaterial("Metal Escuro", corMetalEscuro);
        matAmarelo = CriarMaterial("Amarelo Segurança", corAmareloSeguranca);
        matVermelho = CriarMaterial("Vermelho Perigo", corVermelhoPerigo);
        matVerde = CriarMaterial("Verde Liberado", corVerdeLiberado);
        matAzul = CriarMaterial("Azul Pneumático", corAzulPneumatico);
        matLaranja = CriarMaterial("Laranja Alerta", corLaranjaAlerta);
        matVidro = CriarMaterial("Vidro Display", corVidroDisplay);
        matVapor = CriarMaterial("Vapor", corVapor);
        matOleo = CriarMaterial("Óleo Químico", corOleoQuimico);
    }

    private Material CriarMaterial(string nome, Color cor)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");

        if (shader == null)
        {
            shader = Shader.Find("Universal Render Pipeline/Unlit");
        }

        if (shader == null)
        {
            shader = Shader.Find("Sprites/Default");
        }

        if (shader == null)
        {
            Debug.LogWarning("Nenhum shader compatível encontrado. O material pode aparecer rosa.");
            return null;
        }

        Material material = new Material(shader);
        material.name = nome;

        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", cor);
        }

        if (material.HasProperty("_Color"))
        {
            material.SetColor("_Color", cor);
        }

        if (cor.a < 1f)
        {
            ConfigurarTransparencia(material);
        }

        return material;
    }

    private void ConfigurarTransparencia(Material material)
    {
        if (material == null)
        {
            return;
        }

        if (material.HasProperty("_Surface"))
        {
            material.SetFloat("_Surface", 1f);
        }

        if (material.HasProperty("_Blend"))
        {
            material.SetFloat("_Blend", 0f);
        }

        if (material.HasProperty("_AlphaClip"))
        {
            material.SetFloat("_AlphaClip", 0f);
        }

        material.renderQueue = 3000;

        material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        material.SetInt("_ZWrite", 0);

        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.EnableKeyword("_ALPHABLEND_ON");
    }

    private void CriarSalaBase(Vector3 centro, string nome, Material pisoOverride = null)
    {
        GameObject setorObj = new GameObject(nome);
        setorObj.transform.SetParent(raiz);
        Transform setor = setorObj.transform;

        Material piso = pisoOverride != null ? pisoOverride : matPiso;

        CriarCubo("Piso", centro + new Vector3(0, -0.05f, 0), new Vector3(larguraSala, 0.1f, comprimentoSala), piso, setor);

        CriarCubo("Parede Esquerda", centro + new Vector3(-larguraSala / 2f, alturaParede / 2f, 0), new Vector3(espessuraParede, alturaParede, comprimentoSala), matParede, setor);
        CriarCubo("Parede Direita", centro + new Vector3(larguraSala / 2f, alturaParede / 2f, 0), new Vector3(espessuraParede, alturaParede, comprimentoSala), matParede, setor);
        CriarCubo("Parede Fundo", centro + new Vector3(0, alturaParede / 2f, -comprimentoSala / 2f), new Vector3(larguraSala, alturaParede, espessuraParede), matParede, setor);

        CriarCubo("Parede Frente Esquerda", centro + new Vector3(-5.75f, alturaParede / 2f, comprimentoSala / 2f), new Vector3(6.5f, alturaParede, espessuraParede), matParede, setor);
        CriarCubo("Parede Frente Direita", centro + new Vector3(5.75f, alturaParede / 2f, comprimentoSala / 2f), new Vector3(6.5f, alturaParede, espessuraParede), matParede, setor);

        CriarCubo("Porta Saida", centro + new Vector3(0, 1.55f, comprimentoSala / 2f + 0.08f), new Vector3(3.2f, 3.1f, 0.2f), matEscuro, setor);
        CriarCubo("Luz Status Porta", centro + new Vector3(0, 3.35f, comprimentoSala / 2f + 0.22f), new Vector3(1f, 0.25f, 0.2f), matAmarelo, setor);

        CriarTexto(nome, centro + new Vector3(0, 0.06f, -comprimentoSala / 2f + 1.2f), Quaternion.Euler(90, 0, 0), 0.48f, setor);
    }

    private void CriarSala1_Subestacao(Vector3 centro)
    {
        CriarSalaBase(centro, "SALA 1 - SUBESTAÇÃO ELÉTRICA");
        Transform setor = raiz.Find("SALA 1 - SUBESTAÇÃO ELÉTRICA");

        CriarCubo("Painel Elétrico de Controle 01", centro + new Vector3(0, 1.65f, -3.5f), new Vector3(4.3f, 3.3f, 0.65f), matMetal, setor);
        CriarCubo("Porta do Painel", centro + new Vector3(0, 1.65f, -3.12f), new Vector3(4.4f, 3.15f, 0.12f), matEscuro, setor);

        CriarCilindro("LED L1 Vermelho", centro + new Vector3(-1.2f, 2.85f, -3f), new Vector3(0.18f, 0.18f, 0.08f), Quaternion.Euler(90, 0, 0), matVermelho, setor);
        CriarCilindro("LED L2 Vermelho", centro + new Vector3(0f, 2.85f, -3f), new Vector3(0.18f, 0.18f, 0.08f), Quaternion.Euler(90, 0, 0), matVermelho, setor);
        CriarCilindro("LED L3 Vermelho", centro + new Vector3(1.2f, 2.85f, -3f), new Vector3(0.18f, 0.18f, 0.08f), Quaternion.Euler(90, 0, 0), matVermelho, setor);

        for (int i = 0; i < 6; i++)
        {
            float x = -1.5f + i * 0.6f;

            CriarCubo("Mini Disjuntor DIN " + (i + 1), centro + new Vector3(x, 1.65f, -2.82f), new Vector3(0.35f, 0.9f, 0.18f), matParede, setor);
            CriarCubo("Manípulo DIN " + (i + 1), centro + new Vector3(x, 1.45f, -2.62f), new Vector3(0.25f, 0.12f, 0.12f), matVermelho, setor);
        }

        CriarCubo("Disjuntor Caixa Moldada 250A", centro + new Vector3(0, 0.78f, -2.82f), new Vector3(1.5f, 0.9f, 0.2f), matParede, setor);
        CriarCubo("Manopla Disjuntor de Potência", centro + new Vector3(0, 0.78f, -2.58f), new Vector3(0.24f, 0.75f, 0.16f), matVermelho, setor);

        CriarCubo("Canaleta de Cabos no Chão", centro + new Vector3(0, 0.04f, 1.3f), new Vector3(1.1f, 0.1f, 9f), matEscuro, setor);

        CriarCilindro("Tomada Industrial Steck", centro + new Vector3(4.8f, 1.1f, -2.5f), new Vector3(0.45f, 0.45f, 0.35f), Quaternion.Euler(90, 0, 0), matAzul, setor);
        CriarCilindro("Plugue Industrial Desconectável", centro + new Vector3(4.8f, 1.1f, -1.55f), new Vector3(0.32f, 0.32f, 0.7f), Quaternion.Euler(90, 0, 0), matVermelho, setor);
        CriarCubo("Cabo do Plugue", centro + new Vector3(4.8f, 1.1f, -0.55f), new Vector3(0.12f, 0.12f, 1.5f), matEscuro, setor);

        CriarCubo("Maleta Técnica Tagout", centro + new Vector3(-5.6f, 0.25f, 3.5f), new Vector3(1.8f, 0.5f, 1.1f), matVermelho, setor);
        CriarTexto("DBDD\nDBDM\nBSP\nBB000408\nCBUnique\nET", centro + new Vector3(-5.6f, 0.65f, 3.5f), Quaternion.Euler(90, 0, 0), 0.19f, setor);

        CriarCubo("Voltímetro", centro + new Vector3(5.6f, 0.18f, 3.4f), new Vector3(0.9f, 0.35f, 0.65f), matVidro, setor);
        CriarTexto("0.0 V", centro + new Vector3(5.6f, 0.42f, 3.4f), Quaternion.Euler(90, 0, 0), 0.22f, setor);
    }

    private void CriarSala2_Prensa(Vector3 centro)
    {
        CriarSalaBase(centro, "SALA 2 - USINAGEM E PRENSA PNEUMÁTICA");
        Transform setor = raiz.Find("SALA 2 - USINAGEM E PRENSA PNEUMÁTICA");

        CriarFaixasSeguranca(centro, setor);

        CriarCubo("Base da Prensa", centro + new Vector3(0, 0.45f, -1.5f), new Vector3(5.5f, 0.9f, 3f), matMetal, setor);
        CriarCubo("Coluna Esquerda da Prensa", centro + new Vector3(-2.3f, 2f, -1.5f), new Vector3(0.45f, 3.2f, 0.45f), matEscuro, setor);
        CriarCubo("Coluna Direita da Prensa", centro + new Vector3(2.3f, 2f, -1.5f), new Vector3(0.45f, 3.2f, 0.45f), matEscuro, setor);
        CriarCubo("Travessa Superior da Prensa", centro + new Vector3(0, 3.5f, -1.5f), new Vector3(5.2f, 0.7f, 1.2f), matMetal, setor);
        CriarCubo("Martelo Suspenso da Prensa", centro + new Vector3(0, 2.35f, -1.5f), new Vector3(4.2f, 0.7f, 1.7f), matEscuro, setor);

        CriarCubo("Calço Técnico de Segurança", centro + new Vector3(-4.7f, 0.55f, -1.5f), new Vector3(0.8f, 1.1f, 0.8f), matAmarelo, setor);
        CriarCubo("Local de Encaixe do Calço", centro + new Vector3(0, 1.25f, -1.5f), new Vector3(0.65f, 1.5f, 0.65f), matAmarelo, setor);

        CriarCilindro("Tubulação Azul de Ar Comprimido", centro + new Vector3(4.2f, 2.3f, -1.5f), new Vector3(0.18f, 0.18f, 7f), Quaternion.Euler(90, 0, 0), matAzul, setor);
        CriarCilindro("Válvula de Esfera 1 Polegada", centro + new Vector3(4.2f, 1.5f, 1.7f), new Vector3(0.42f, 0.42f, 0.35f), Quaternion.Euler(90, 0, 0), matMetal, setor);
        CriarCubo("Alavanca Vermelha da Válvula", centro + new Vector3(4.2f, 1.95f, 1.7f), new Vector3(0.18f, 1.1f, 0.15f), matVermelho, setor);

        CriarManometro("Manômetro Pneumático", centro + new Vector3(3.9f, 2.25f, 2.4f), setor, "6 BAR");

        CriarCubo("Mangueira Rompida", centro + new Vector3(0, 3.1f, 0.2f), new Vector3(0.18f, 0.18f, 2f), matEscuro, setor);
        CriarCubo("Jato de Ar Comprimido", centro + new Vector3(0, 3.1f, 1.4f), new Vector3(1.1f, 0.08f, 0.08f), matVidro, setor);

        CriarCilindro("Válvula de Dreno Amarela", centro + new Vector3(2.8f, 0.8f, 2.5f), new Vector3(0.3f, 0.3f, 0.25f), Quaternion.Euler(90, 0, 0), matAmarelo, setor);

        CriarCubo("Maleta Tagout Sala 2", centro + new Vector3(-5.6f, 0.25f, 4.2f), new Vector3(1.8f, 0.5f, 1.1f), matVermelho, setor);
        CriarTexto("BVEPP\nBVEPLP7\nCHUnique-Inox\nET", centro + new Vector3(-5.6f, 0.65f, 4.2f), Quaternion.Euler(90, 0, 0), 0.2f, setor);
    }

    private void CriarSala3_Utilidades(Vector3 centro)
    {
        CriarSalaBase(centro, "SALA 3 - UTILIDADES HIDRÁULICA E QUÍMICA");
        Transform setor = raiz.Find("SALA 3 - UTILIDADES HIDRÁULICA E QUÍMICA");

        CriarCubo("Piso Gradeado Metálico", centro + new Vector3(0, 0.03f, 0), new Vector3(larguraSala - 1f, 0.05f, comprimentoSala - 1f), matEscuro, setor);

        for (int i = 0; i < 5; i++)
        {
            CriarCilindro("Tubulação Pesada Paralela " + (i + 1), centro + new Vector3(-5f + i * 2.5f, 2.5f, -2f), new Vector3(0.22f, 0.22f, 16f), Quaternion.Euler(90, 0, 0), matMetal, setor);
        }

        CriarCilindro("Bomba Hidráulica de Recalque 02", centro + new Vector3(0, 1.1f, -1f), new Vector3(1.2f, 1.2f, 2.2f), Quaternion.Euler(0, 0, 90), matMetal, setor);
        CriarCubo("Base da Bomba", centro + new Vector3(0, 0.35f, -1f), new Vector3(4.2f, 0.7f, 2.6f), matEscuro, setor);
        CriarCubo("Flange Trincado", centro + new Vector3(2.4f, 1.1f, -1f), new Vector3(0.22f, 1.2f, 1.2f), matVermelho, setor);

        CriarCubo("Bacia de Contenção", centro + new Vector3(0, 0.12f, 1.4f), new Vector3(5.2f, 0.25f, 3.1f), matMetal, setor);
        CriarCubo("Vazamento de Óleo Químico", centro + new Vector3(1.2f, 0.28f, 1.4f), new Vector3(1.8f, 0.06f, 1.2f), matOleo, setor);
        CriarCilindro("Gotejamento de Óleo", centro + new Vector3(1.2f, 0.95f, 0.55f), new Vector3(0.08f, 0.08f, 0.7f), Quaternion.identity, matOleo, setor);

        CriarValvulaVolante("Registro A - Sucção 4 Polegadas - BVR130", centro + new Vector3(-4.7f, 1.4f, -4.2f), 1.25f, setor);
        CriarValvulaVolante("Registro B - Retorno 2 Polegadas - BVR65", centro + new Vector3(4.7f, 1.2f, -4.2f), 0.8f, setor);

        CriarManometro("Manômetro Hidráulico", centro + new Vector3(0, 2.4f, -4.2f), setor, "120 PSI");

        CriarCubo("NPC Mecânico Marcos Corpo", centro + new Vector3(-5.2f, 0.9f, 2.7f), new Vector3(0.75f, 1.8f, 0.55f), matAzul, setor);
        CriarCilindro("NPC Mecânico Marcos Cabeça", centro + new Vector3(-5.2f, 2.05f, 2.7f), new Vector3(0.35f, 0.35f, 0.35f), Quaternion.identity, matMetal, setor);
        CriarTexto("MARCOS\nCADEADO AZUL", centro + new Vector3(-5.2f, 2.65f, 2.7f), Quaternion.identity, 0.23f, setor);

        CriarCubo("Maleta Tagout Sala 3", centro + new Vector3(5.4f, 0.25f, 4.3f), new Vector3(1.8f, 0.5f, 1.1f), matVermelho, setor);
        CriarTexto("BVR130\nBVR65\nBB000408\nCHUnique-Inox\nET", centro + new Vector3(5.4f, 0.65f, 4.3f), Quaternion.Euler(90, 0, 0), 0.18f, setor);
    }

    private void CriarSala4_Caldeiras(Vector3 centro)
    {
        CriarSalaBase(centro, "SALA 4 - CALDEIRAS E VAPOR", matEscuro);
        Transform setor = raiz.Find("SALA 4 - CALDEIRAS E VAPOR");

        CriarCilindro("Tanque Térmico Esquerdo", centro + new Vector3(-5.5f, 1.8f, -1.5f), new Vector3(1.3f, 1.3f, 4.5f), Quaternion.Euler(0, 0, 90), matMetal, setor);
        CriarCilindro("Tanque Térmico Direito", centro + new Vector3(5.5f, 1.8f, -1.5f), new Vector3(1.3f, 1.3f, 4.5f), Quaternion.Euler(0, 0, 90), matMetal, setor);

        CriarCilindro("Tubulação de Vapor 8 Polegadas", centro + new Vector3(0, 2.4f, -1.5f), new Vector3(0.45f, 0.45f, 13f), Quaternion.Euler(90, 0, 0), matMetal, setor);
        CriarCubo("Flange Rompido com Vapor", centro + new Vector3(0, 2.4f, 0.5f), new Vector3(1.1f, 1.1f, 0.25f), matVermelho, setor);

        CriarVapor(centro, setor);

        CriarValvulaVolante("Registro Volante Grande 8 Polegadas - BVR265", centro + new Vector3(-3.7f, 2.4f, -4.8f), 1.6f, setor);

        CriarCilindro("Válvula de Purga de Condensado", centro + new Vector3(2.8f, 0.85f, -0.5f), new Vector3(0.35f, 0.35f, 0.35f), Quaternion.Euler(90, 0, 0), matMetal, setor);
        CriarCubo("Alavanca da Purga", centro + new Vector3(2.8f, 1.25f, -0.5f), new Vector3(0.2f, 0.9f, 0.15f), matVermelho, setor);
        CriarCubo("Ralo de Drenagem Industrial", centro + new Vector3(2.8f, 0.04f, 1.2f), new Vector3(1.2f, 0.07f, 1.2f), matEscuro, setor);

        CriarManometro("Medidor de Pressão Vapor", centro + new Vector3(-1.3f, 3.3f, -1.5f), setor, "8 BAR");
        CriarManometro("Termômetro Tubulação", centro + new Vector3(1.3f, 3.3f, -1.5f), setor, "185 C");

        CriarCubo("Armário de EPIs Térmicos", centro + new Vector3(-6.4f, 1.4f, 4.1f), new Vector3(1.5f, 2.8f, 1f), matAmarelo, setor);
        CriarCubo("Luvas Térmicas", centro + new Vector3(-6.4f, 2.1f, 4.7f), new Vector3(0.9f, 0.35f, 0.15f), matLaranja, setor);
        CriarCubo("Avental Aluminizado", centro + new Vector3(-6.4f, 1.2f, 4.7f), new Vector3(0.9f, 1.1f, 0.15f), matMetal, setor);
        CriarTexto("EPIs\nTÉRMICOS", centro + new Vector3(-6.4f, 3.1f, 4.7f), Quaternion.identity, 0.2f, setor);

        CriarCubo("Alarme Vermelho Esquerdo", centro + new Vector3(-7.8f, 3.2f, -5f), new Vector3(0.35f, 0.35f, 0.35f), matVermelho, setor);
        CriarCubo("Alarme Vermelho Direito", centro + new Vector3(7.8f, 3.2f, -5f), new Vector3(0.35f, 0.35f, 0.35f), matVermelho, setor);

        CriarCubo("Maleta Tagout Sala 4", centro + new Vector3(5.6f, 0.25f, 4.3f), new Vector3(1.8f, 0.5f, 1.1f), matVermelho, setor);
        CriarTexto("BVR265\nBB000408\nCHUnique-Inox\nET", centro + new Vector3(5.6f, 0.65f, 4.3f), Quaternion.Euler(90, 0, 0), 0.19f, setor);
    }

    private void CriarCorredores()
    {
        for (int i = 0; i < 3; i++)
        {
            float z = comprimentoSala / 2f + i * (comprimentoSala + distanciaEntreSalas) + distanciaEntreSalas / 2f;

            CriarCubo("Corredor Entre Salas " + (i + 1), new Vector3(0, -0.04f, z), new Vector3(larguraCorredor, 0.1f, distanciaEntreSalas), matPiso, raiz);
            CriarCubo("Parede Corredor Esquerda " + (i + 1), new Vector3(-larguraCorredor / 2f, alturaParede / 2f, z), new Vector3(espessuraParede, alturaParede, distanciaEntreSalas), matParede, raiz);
            CriarCubo("Parede Corredor Direita " + (i + 1), new Vector3(larguraCorredor / 2f, alturaParede / 2f, z), new Vector3(espessuraParede, alturaParede, distanciaEntreSalas), matParede, raiz);
        }
    }

    private void CriarFaixasSeguranca(Vector3 centro, Transform parent)
    {
        for (int i = 0; i < 7; i++)
        {
            float x = -6f + i * 2f;
            CriarCubo("Faixa Amarela Segurança " + i, centro + new Vector3(x, 0.035f, 4.7f), new Vector3(1.2f, 0.05f, 0.2f), matAmarelo, parent);
        }
    }

    private void CriarVapor(Vector3 centro, Transform parent)
    {
        Vector3[] posicoes =
        {
            new Vector3(-1.4f, 2.1f, 0.9f),
            new Vector3(-0.7f, 2.8f, 1.1f),
            new Vector3(0.1f, 2.5f, 1.5f),
            new Vector3(0.8f, 3.1f, 1.9f),
            new Vector3(1.5f, 2.2f, 2.3f),
            new Vector3(-1.1f, 3.2f, 2.4f),
            new Vector3(0.3f, 1.9f, 2.8f),
            new Vector3(1.2f, 2.8f, 3.1f)
        };

        for (int i = 0; i < posicoes.Length; i++)
        {
            CriarCilindro("Nuvem de Vapor " + (i + 1), centro + posicoes[i], new Vector3(0.7f, 0.7f, 0.7f), Quaternion.identity, matVapor, parent);
        }
    }

    private void CriarValvulaVolante(string nome, Vector3 pos, float raio, Transform parent)
    {
        CriarCilindro(nome + " Aro", pos, new Vector3(raio, raio, 0.08f), Quaternion.Euler(90, 0, 0), matVermelho, parent);
        CriarCilindro(nome + " Centro", pos, new Vector3(0.22f, 0.22f, 0.18f), Quaternion.Euler(90, 0, 0), matMetal, parent);

        CriarCubo(nome + " Raio Horizontal", pos, new Vector3(raio * 2f, 0.08f, 0.08f), matVermelho, parent);
        CriarCubo(nome + " Raio Vertical", pos, new Vector3(0.08f, raio * 2f, 0.08f), matVermelho, parent);
    }

    private void CriarManometro(string nome, Vector3 pos, Transform parent, string texto)
    {
        CriarCilindro(nome + " Corpo", pos, new Vector3(0.55f, 0.55f, 0.12f), Quaternion.Euler(90, 0, 0), matMetal, parent);
        CriarCilindro(nome + " Tela", pos + new Vector3(0, 0, -0.08f), new Vector3(0.45f, 0.45f, 0.04f), Quaternion.Euler(90, 0, 0), matVidro, parent);
        CriarCubo(nome + " Ponteiro", pos + new Vector3(0.12f, 0.05f, -0.15f), new Vector3(0.35f, 0.04f, 0.04f), matVermelho, parent);
        CriarTexto(texto, pos + new Vector3(0, -0.75f, 0), Quaternion.identity, 0.22f, parent);
    }

    private GameObject CriarCubo(string nome, Vector3 pos, Vector3 escala, Material mat, Transform parent)
    {
        GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        obj.name = nome;
        obj.transform.position = pos;
        obj.transform.localScale = escala;
        obj.transform.SetParent(parent);

        AplicarMaterial(obj, mat);

        return obj;
    }

    private GameObject CriarCilindro(string nome, Vector3 pos, Vector3 escala, Quaternion rotacao, Material mat, Transform parent)
    {
        GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        obj.name = nome;
        obj.transform.position = pos;
        obj.transform.rotation = rotacao;
        obj.transform.localScale = escala;
        obj.transform.SetParent(parent);

        AplicarMaterial(obj, mat);

        return obj;
    }

    private void AplicarMaterial(GameObject obj, Material mat)
    {
        Renderer renderer = obj.GetComponent<Renderer>();

        if (renderer != null && mat != null)
        {
            renderer.material = mat;
        }
    }

    private void CriarTexto(string texto, Vector3 pos, Quaternion rotacao, float tamanho, Transform parent)
    {
        GameObject obj = new GameObject("Texto - " + texto);
        obj.transform.position = pos;
        obj.transform.rotation = rotacao;
        obj.transform.SetParent(parent);

        TextMesh textMesh = obj.AddComponent<TextMesh>();
        textMesh.text = texto;
        textMesh.fontSize = 48;
        textMesh.characterSize = tamanho;
        textMesh.anchor = TextAnchor.MiddleCenter;
        textMesh.alignment = TextAlignment.Center;
        textMesh.color = Color.white;
    }

    private void CriarPlayerTeste()
    {
        GameObject player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        player.name = "Player_Teste_Mapa";
        player.transform.position = new Vector3(0, 1.05f, -comprimentoSala / 2f + 3f);
        player.transform.SetParent(raiz);

        AplicarMaterial(player, matVerde);

        GameObject cameraObj = new GameObject("Camera_Teste_Mapa");
        cameraObj.transform.SetParent(player.transform);
        cameraObj.transform.localPosition = new Vector3(0, 0.75f, 0);
        cameraObj.transform.localRotation = Quaternion.identity;

        Camera cam = cameraObj.AddComponent<Camera>();
        cam.fieldOfView = 70f;
        cam.nearClipPlane = 0.05f;

        if (Camera.main == null)
        {
            cameraObj.tag = "MainCamera";
        }
    }

    private void CriarIluminacao()
    {
        GameObject luzDirecional = new GameObject("Luz Direcional Industrial");
        luzDirecional.transform.SetParent(raiz);
        luzDirecional.transform.position = new Vector3(0, 12f, 45f);
        luzDirecional.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

        Light luz = luzDirecional.AddComponent<Light>();
        luz.type = LightType.Directional;
        luz.intensity = 1.15f;

        CriarLuzPontual("Luz Sala 1", new Vector3(0, 5.5f, 0));
        CriarLuzPontual("Luz Sala 2", new Vector3(0, 5.5f, comprimentoSala + distanciaEntreSalas));
        CriarLuzPontual("Luz Sala 3", new Vector3(0, 5.5f, (comprimentoSala + distanciaEntreSalas) * 2));
        CriarLuzPontual("Luz Sala 4", new Vector3(0, 5.5f, (comprimentoSala + distanciaEntreSalas) * 3));
    }

    private void CriarLuzPontual(string nome, Vector3 pos)
    {
        GameObject obj = new GameObject(nome);
        obj.transform.SetParent(raiz);
        obj.transform.position = pos;

        Light luz = obj.AddComponent<Light>();
        luz.type = LightType.Point;
        luz.intensity = 2.2f;
        luz.range = 18f;
    }
}