using UnityEngine;

public class FactoryFirstFloorGenerator : MonoBehaviour
{
    [Header("Configuração Geral")]
    public float wallHeight = 3.2f;
    public float wallThickness = 0.25f;
    public float floorThickness = 0.12f;

    [Header("Materiais")]
    public Material floorMaterial;
    public Material wallMaterial;
    public Material machineMaterial;
    public Material storageMaterial;
    public Material safetyMaterial;
    public Material elevatorMaterial;
    public Material stairMaterial;
    public Material textMaterial;

    private Transform root;

    void Start()
    {
        GenerateFirstFloor();
    }

    [ContextMenu("Gerar Primeiro Andar")]
    public void GenerateFirstFloor()
    {
        ClearOldFactory();

        GameObject rootObj = new GameObject("FABRICA_PEQUENA_PRIMEIRO_ANDAR_SEM_PORTAS");
        root = rootObj.transform;

        CreateDefaultMaterials();

        GenerateFloorBase();
        GenerateOuterWalls();
        GenerateRooms();
        GenerateElevator();
        GenerateStairs();
        GenerateMachinesAndProps();
        GenerateLabels();

        Debug.Log("Primeiro andar sem portas gerado com sucesso.");
    }

    void ClearOldFactory()
    {
        GameObject oldA = GameObject.Find("FABRICA_PEQUENA_PRIMEIRO_ANDAR");
        GameObject oldB = GameObject.Find("FABRICA_PEQUENA_PRIMEIRO_ANDAR_SEM_PORTAS");

        if (oldA != null)
        {
            if (Application.isPlaying) Destroy(oldA);
            else DestroyImmediate(oldA);
        }

        if (oldB != null)
        {
            if (Application.isPlaying) Destroy(oldB);
            else DestroyImmediate(oldB);
        }
    }

    void CreateDefaultMaterials()
    {
        if (floorMaterial == null)
            floorMaterial = CreateMaterial("MAT_Piso_Industrial", new Color(0.45f, 0.45f, 0.42f));

        if (wallMaterial == null)
            wallMaterial = CreateMaterial("MAT_Parede_Concreto", new Color(0.18f, 0.2f, 0.22f));

        if (machineMaterial == null)
            machineMaterial = CreateMaterial("MAT_Maquinas", new Color(0.18f, 0.28f, 0.32f));

        if (storageMaterial == null)
            storageMaterial = CreateMaterial("MAT_Estoque_Caixas", new Color(0.55f, 0.34f, 0.16f));

        if (safetyMaterial == null)
            safetyMaterial = CreateMaterial("MAT_Seguranca", new Color(1f, 0.8f, 0.05f));

        if (elevatorMaterial == null)
            elevatorMaterial = CreateMaterial("MAT_Elevador", new Color(0.35f, 0.38f, 0.4f));

        if (stairMaterial == null)
            stairMaterial = CreateMaterial("MAT_Escada", new Color(0.3f, 0.3f, 0.32f));

        if (textMaterial == null)
            textMaterial = CreateMaterial("MAT_Texto", Color.white);
    }

    Material CreateMaterial(string name, Color color)
    {
        Material mat = new Material(Shader.Find("Standard"));
        mat.name = name;
        mat.color = color;
        return mat;
    }

    // =========================
    // BASE
    // =========================

    void GenerateFloorBase()
    {
        CreateCube(
            "Piso_Geral_Primeiro_Andar",
            new Vector3(0, -floorThickness / 2f, 0),
            new Vector3(44, floorThickness, 22),
            floorMaterial
        );

        CreateCube(
            "Piso_Torre_Escada_Lateral",
            new Vector3(-24, -floorThickness / 2f, -3),
            new Vector3(5, floorThickness, 12),
            floorMaterial
        );
    }

    void GenerateOuterWalls()
    {
        CreateWall("Parede_Norte", new Vector3(0, wallHeight / 2f, 11), new Vector3(44, wallHeight, wallThickness));
        CreateWall("Parede_Sul", new Vector3(0, wallHeight / 2f, -11), new Vector3(44, wallHeight, wallThickness));
        CreateWall("Parede_Oeste", new Vector3(-22, wallHeight / 2f, 0), new Vector3(wallThickness, wallHeight, 22));
        CreateWall("Parede_Leste", new Vector3(22, wallHeight / 2f, 0), new Vector3(wallThickness, wallHeight, 22));

        CreateWall("Torre_Escada_Parede_Norte", new Vector3(-24, wallHeight / 2f, 3), new Vector3(5, wallHeight, wallThickness));
        CreateWall("Torre_Escada_Parede_Sul", new Vector3(-24, wallHeight / 2f, -9), new Vector3(5, wallHeight, wallThickness));
        CreateWall("Torre_Escada_Parede_Oeste", new Vector3(-26.5f, wallHeight / 2f, -3), new Vector3(wallThickness, wallHeight, 12));
        CreateWall("Torre_Escada_Parede_Leste", new Vector3(-21.5f, wallHeight / 2f, -3), new Vector3(wallThickness, wallHeight, 12));
    }

    // =========================
    // DIVISÕES INTERNAS
    // =========================

    void GenerateRooms()
    {
        CreateWall("Divisoria_Estoque_MP", new Vector3(-13, wallHeight / 2f, 0), new Vector3(wallThickness, wallHeight, 22));
        CreateWall("Divisoria_Estoque_Componentes", new Vector3(-4, wallHeight / 2f, 0), new Vector3(wallThickness, wallHeight, 22));
        CreateWall("Divisoria_Montagem", new Vector3(8, wallHeight / 2f, 0), new Vector3(wallThickness, wallHeight, 22));
        CreateWall("Divisoria_Area_Elevador", new Vector3(16, wallHeight / 2f, 0), new Vector3(wallThickness, wallHeight, 22));

        CreateWall("Divisoria_Controle_Qualidade", new Vector3(-8.5f, wallHeight / 2f, -3), new Vector3(17, wallHeight, wallThickness));
        CreateWall("Divisoria_Paletizacao", new Vector3(8, wallHeight / 2f, -3), new Vector3(24, wallHeight, wallThickness));

        CreateWall("Parede_Elevador_Sul", new Vector3(19, wallHeight / 2f, 3), new Vector3(6, wallHeight, wallThickness));
        CreateWall("Parede_Elevador_Oeste", new Vector3(16, wallHeight / 2f, 7), new Vector3(wallThickness, wallHeight, 8));

        CreateWall("Parede_Escada_Sul", new Vector3(19, wallHeight / 2f, -2), new Vector3(6, wallHeight, wallThickness));
    }

    // =========================
    // ELEVADOR
    // =========================

    void GenerateElevator()
    {
        CreateCube(
            "Elevador_Cabine",
            new Vector3(18.8f, 1.5f, 7),
            new Vector3(3.2f, 3f, 3.2f),
            elevatorMaterial
        );

        CreateCube(
            "Painel_Elevador",
            new Vector3(16.25f, 1.2f, 5.4f),
            new Vector3(0.1f, 1f, 0.4f),
            safetyMaterial
        );
    }

    // =========================
    // ESCADAS
    // =========================

    void GenerateStairs()
    {
        Vector3 start = new Vector3(19, 0.1f, -7.5f);

        for (int i = 0; i < 8; i++)
        {
            CreateCube(
                "Degrau_Escada_Direita_" + i,
                new Vector3(start.x, 0.1f + i * 0.12f, start.z + i * 0.45f),
                new Vector3(4f, 0.22f, 0.42f),
                stairMaterial
            );
        }

        CreateCube(
            "Corrimao_Escada_Direita_A",
            new Vector3(16.9f, 1.2f, -5.8f),
            new Vector3(0.12f, 1.2f, 4f),
            safetyMaterial
        );

        CreateCube(
            "Corrimao_Escada_Direita_B",
            new Vector3(21.1f, 1.2f, -5.8f),
            new Vector3(0.12f, 1.2f, 4f),
            safetyMaterial
        );

        for (int i = 0; i < 9; i++)
        {
            CreateCube(
                "Degrau_Escada_Lateral_" + i,
                new Vector3(-24, 0.1f + i * 0.12f, -7.5f + i * 0.55f),
                new Vector3(3.5f, 0.22f, 0.5f),
                stairMaterial
            );
        }

        CreateCube(
            "Corrimao_Escada_Lateral_A",
            new Vector3(-25.9f, 1.2f, -5.2f),
            new Vector3(0.12f, 1.2f, 5f),
            safetyMaterial
        );

        CreateCube(
            "Corrimao_Escada_Lateral_B",
            new Vector3(-22.1f, 1.2f, -5.2f),
            new Vector3(0.12f, 1.2f, 5f),
            safetyMaterial
        );
    }

    // =========================
    // OBJETOS DE PRODUÇÃO
    // =========================

    void GenerateMachinesAndProps()
    {
        GenerateRawMaterialStorage();
        GenerateComponentStorage();
        GenerateFinalAssembly();
        GenerateQualityControl();
        GeneratePackagingArea();
        GenerateSafetyLines();
    }

    void GenerateRawMaterialStorage()
    {
        for (int i = 0; i < 5; i++)
        {
            CreateRack(
                "Rack_MP_" + i,
                new Vector3(-18.5f, 0.8f, 7 - i * 2.5f),
                new Vector3(2.4f, 1.6f, 0.7f)
            );
        }

        CreateCube("Empilhadeira_MP", new Vector3(-15.5f, 0.45f, -6.5f), new Vector3(1.2f, 0.9f, 2f), machineMaterial);
        CreateCube("Palete_MP_01", new Vector3(-19, 0.25f, -6.5f), new Vector3(2f, 0.5f, 1.5f), storageMaterial);
        CreateCube("Palete_MP_02", new Vector3(-16.5f, 0.25f, -8.3f), new Vector3(2f, 0.5f, 1.5f), storageMaterial);
    }

    void GenerateComponentStorage()
    {
        for (int i = 0; i < 4; i++)
        {
            CreateRack(
                "Rack_Componentes_" + i,
                new Vector3(-8.5f + i * 1.8f, 0.8f, 7.5f),
                new Vector3(1.5f, 1.6f, 0.7f)
            );
        }

        CreateCube("Bancada_Componentes", new Vector3(-8, 0.45f, 1.5f), new Vector3(4f, 0.9f, 1.2f), machineMaterial);
        CreateCube("Caixas_Componentes_A", new Vector3(-5.7f, 0.4f, -0.5f), new Vector3(1.5f, 0.8f, 1.5f), storageMaterial);
    }

    void GenerateFinalAssembly()
    {
        CreateCube("Esteira_Montagem_Final", new Vector3(2, 0.35f, 3.5f), new Vector3(7f, 0.7f, 1.1f), machineMaterial);
        CreateCube("Maquina_Montagem_A", new Vector3(2, 0.9f, 7), new Vector3(2.2f, 1.8f, 1.8f), machineMaterial);
        CreateCube("Maquina_Montagem_B", new Vector3(6, 0.9f, 7), new Vector3(2.2f, 1.8f, 1.8f), machineMaterial);
        CreateCube("Bancada_Montagem", new Vector3(4, 0.45f, 0.5f), new Vector3(4.5f, 0.9f, 1.3f), machineMaterial);
    }

    void GenerateQualityControl()
    {
        CreateCube("Mesa_Controle_Qualidade", new Vector3(-9.5f, 0.45f, -7), new Vector3(4f, 0.9f, 1.6f), machineMaterial);
        CreateCube("Scanner_Qualidade", new Vector3(-7.2f, 0.9f, -7), new Vector3(1.2f, 1.8f, 1.2f), machineMaterial);
        CreateCube("Computador_Qualidade", new Vector3(-10.8f, 1.05f, -6.5f), new Vector3(0.8f, 0.5f, 0.1f), elevatorMaterial);
        CreateCube("Caixa_Amostras_Qualidade", new Vector3(-12, 0.35f, -9), new Vector3(1.5f, 0.7f, 1.2f), storageMaterial);
    }

    void GeneratePackagingArea()
    {
        CreateCube("Esteira_Embalagem", new Vector3(12, 0.35f, -7), new Vector3(7f, 0.7f, 1.1f), machineMaterial);
        CreateCube("Maquina_Embalagem", new Vector3(9.5f, 0.9f, -7), new Vector3(2f, 1.8f, 1.8f), machineMaterial);

        for (int i = 0; i < 4; i++)
        {
            CreateCube(
                "Palete_Produto_Final_" + i,
                new Vector3(16.5f + i * 1.2f, 0.35f, -9),
                new Vector3(1f, 0.7f, 1.2f),
                storageMaterial
            );
        }

        CreateCube("Empilhadeira_Expedicao", new Vector3(18, 0.45f, -5), new Vector3(1.2f, 0.9f, 2f), machineMaterial);
    }

    void GenerateSafetyLines()
    {
        CreateCube("Faixa_Seguranca_Corredor_Central", new Vector3(0, 0.02f, -1.5f), new Vector3(42f, 0.03f, 0.12f), safetyMaterial);
        CreateCube("Faixa_Seguranca_Estoque", new Vector3(-13, 0.02f, 0), new Vector3(0.12f, 0.03f, 20f), safetyMaterial);
        CreateCube("Faixa_Seguranca_Montagem", new Vector3(8, 0.02f, 0), new Vector3(0.12f, 0.03f, 20f), safetyMaterial);
        CreateCube("Faixa_Seguranca_Saida", new Vector3(20.5f, 0.02f, -7), new Vector3(3f, 0.03f, 0.12f), safetyMaterial);
    }

    void CreateRack(string name, Vector3 position, Vector3 scale)
    {
        CreateCube(name + "_Estrutura", position, scale, machineMaterial);

        CreateCube(
            name + "_Caixas_01",
            position + new Vector3(0, 0.65f, 0),
            new Vector3(scale.x * 0.9f, 0.35f, scale.z * 0.8f),
            storageMaterial
        );

        CreateCube(
            name + "_Caixas_02",
            position + new Vector3(0, -0.05f, 0),
            new Vector3(scale.x * 0.9f, 0.35f, scale.z * 0.8f),
            storageMaterial
        );
    }

    // =========================
    // TEXTOS
    // =========================

    void GenerateLabels()
    {
        CreateLabel("07\nESTOQUE DE\nMATÉRIA-PRIMA", new Vector3(-18, 0.06f, 1), 1.2f);
        CreateLabel("08\nESTOQUE DE\nCOMPONENTES", new Vector3(-8.5f, 0.06f, 5), 1.1f);
        CreateLabel("09\nMONTAGEM\nFINAL", new Vector3(3, 0.06f, 4.5f), 1.2f);
        CreateLabel("10\nCONTROLE DE\nQUALIDADE", new Vector3(-9.5f, 0.06f, -5), 1.1f);
        CreateLabel("11\nPALETIZAÇÃO E\nEMBALAGEM", new Vector3(12, 0.06f, -5), 1.1f);
        CreateLabel("ELEVADOR", new Vector3(18.8f, 0.06f, 9.5f), 0.9f);
        CreateLabel("ESCADA", new Vector3(19, 0.06f, -4.5f), 0.9f);
        CreateLabel("SAÍDA DE\nPRODUTOS", new Vector3(20.5f, 0.06f, -9.5f), 0.9f);
    }

    void CreateLabel(string text, Vector3 position, float size)
    {
        GameObject labelObj = new GameObject("Label_" + text.Replace("\n", "_"));
        labelObj.transform.SetParent(root);
        labelObj.transform.position = position;
        labelObj.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

        TextMesh textMesh = labelObj.AddComponent<TextMesh>();
        textMesh.text = text;
        textMesh.fontSize = 48;
        textMesh.characterSize = size * 0.1f;
        textMesh.anchor = TextAnchor.MiddleCenter;
        textMesh.alignment = TextAlignment.Center;

        MeshRenderer renderer = labelObj.GetComponent<MeshRenderer>();
        renderer.material = textMaterial;
    }

    // =========================
    // HELPERS
    // =========================

    void CreateWall(string name, Vector3 position, Vector3 scale)
    {
        CreateCube(name, position, scale, wallMaterial);
    }

    GameObject CreateCube(string name, Vector3 position, Vector3 scale, Material material)
    {
        GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        obj.name = name;
        obj.transform.SetParent(root);
        obj.transform.position = position;
        obj.transform.localScale = scale;

        Renderer renderer = obj.GetComponent<Renderer>();
        renderer.material = material;

        return obj;
    }
}