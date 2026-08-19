#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public static class AldeiaMedievalBlockoutV01
{
    private static Transform root;

    private static Material matGrass;
    private static Material matDirt;
    private static Material matStone;
    private static Material matWood;
    private static Material matDarkWood;
    private static Material matWall;
    private static Material matRoofRed;
    private static Material matRoofBlue;
    private static Material matWater;
    private static Material matLeaf;
    private static Material matWarmLight;
    private static Material matCrop;
    private static Material matPumpkin;

    [MenuItem("Tools/Aldeia Medieval/Gerar Blockout V01")]
    public static void GerarAldeia()
    {
        GameObject old = GameObject.Find("AldeiaMedieval_Blockout_V01");
        if (old != null)
        {
            Object.DestroyImmediate(old);
        }

        GameObject rootObj = new GameObject("AldeiaMedieval_Blockout_V01");
        root = rootObj.transform;

        CriarMateriais();
        CriarTerreno();
        CriarRuas();
        CriarPracaCentral();
        CriarConstrucoes();
        CriarRioEPonte();
        CriarCampos();
        CriarArvores();
        CriarLuzECamera();

        Selection.activeGameObject = rootObj;
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());

        Debug.Log("Aldeia Medieval Blockout V01 criada com sucesso!");
    }

    private static void CriarMateriais()
    {
        matGrass = Mat("Grama", new Color(0.25f, 0.48f, 0.20f));
        matDirt = Mat("Terra_Estrada", new Color(0.48f, 0.34f, 0.20f));
        matStone = Mat("Pedra", new Color(0.45f, 0.45f, 0.43f));
        matWood = Mat("Madeira", new Color(0.45f, 0.26f, 0.13f));
        matDarkWood = Mat("Madeira_Escura", new Color(0.22f, 0.12f, 0.06f));
        matWall = Mat("Parede_Clara", new Color(0.72f, 0.61f, 0.45f));
        matRoofRed = Mat("Telhado_Vermelho", new Color(0.55f, 0.15f, 0.08f));
        matRoofBlue = Mat("Telhado_Azul_Escuro", new Color(0.12f, 0.20f, 0.30f));
        matWater = Mat("Agua", new Color(0.10f, 0.35f, 0.65f, 0.8f));
        matLeaf = Mat("Folhagem", new Color(0.12f, 0.38f, 0.14f));
        matWarmLight = Mat("Luz_Quente", new Color(1.0f, 0.55f, 0.15f));
        matCrop = Mat("Plantacao", new Color(0.28f, 0.55f, 0.14f));
        matPumpkin = Mat("Aboboras", new Color(0.95f, 0.42f, 0.08f));
    }

    private static Material Mat(string name, Color color)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");

        if (shader == null)
            shader = Shader.Find("Standard");

        if (shader == null)
            shader = Shader.Find("Diffuse");

        Material mat = new Material(shader);
        mat.name = name;

        if (mat.HasProperty("_BaseColor"))
            mat.SetColor("_BaseColor", color);
        else
            mat.color = color;

        return mat;
    }

    private static GameObject Prim(PrimitiveType type, string name, Vector3 pos, Vector3 scale, Material mat, Transform parent = null, float yRot = 0f)
    {
        GameObject go = GameObject.CreatePrimitive(type);
        go.name = name;

        go.transform.SetParent(parent == null ? root : parent, false);
        go.transform.localPosition = pos;
        go.transform.localRotation = Quaternion.Euler(0f, yRot, 0f);
        go.transform.localScale = scale;

        Renderer r = go.GetComponent<Renderer>();
        if (r != null)
            r.sharedMaterial = mat;

        return go;
    }

    private static void CriarTerreno()
    {
        Prim(PrimitiveType.Cube, "Terreno_Grama", new Vector3(0, -0.1f, 0), new Vector3(100, 0.2f, 100), matGrass);
    }

    private static void CriarRuas()
    {
        Transform ruas = new GameObject("Ruas_De_Terra").transform;
        ruas.SetParent(root, false);

        Prim(PrimitiveType.Cube, "Estrada_Entrada", new Vector3(-18, 0.03f, -32), new Vector3(8, 0.08f, 35), matDirt, ruas, -25);
        Prim(PrimitiveType.Cube, "Estrada_Central", new Vector3(0, 0.04f, -10), new Vector3(9, 0.08f, 30), matDirt, ruas, 0);
        Prim(PrimitiveType.Cube, "Estrada_Capela", new Vector3(5, 0.04f, 18), new Vector3(6, 0.08f, 25), matDirt, ruas, 22);
        Prim(PrimitiveType.Cube, "Estrada_Fazenda", new Vector3(25, 0.04f, 7), new Vector3(6, 0.08f, 35), matDirt, ruas, 72);
        Prim(PrimitiveType.Cube, "Estrada_Ferreiro", new Vector3(-24, 0.04f, 5), new Vector3(6, 0.08f, 30), matDirt, ruas, -62);
        Prim(PrimitiveType.Cube, "Estrada_Rio", new Vector3(20, 0.04f, -22), new Vector3(6, 0.08f, 28), matDirt, ruas, -38);
    }

    private static void CriarPracaCentral()
    {
        Transform praca = new GameObject("Praca_Central").transform;
        praca.SetParent(root, false);

        Prim(PrimitiveType.Cylinder, "Base_Circular_Praca", new Vector3(0, 0.08f, 0), new Vector3(8, 0.08f, 8), matStone, praca);
        Prim(PrimitiveType.Cylinder, "Poco_De_Pedra", new Vector3(0, 0.55f, 0), new Vector3(1.2f, 0.6f, 1.2f), matStone, praca);

        Prim(PrimitiveType.Cube, "Pilar_Poco_Esquerdo", new Vector3(-0.65f, 1.3f, 0), new Vector3(0.18f, 1.4f, 0.18f), matWood, praca);
        Prim(PrimitiveType.Cube, "Pilar_Poco_Direito", new Vector3(0.65f, 1.3f, 0), new Vector3(0.18f, 1.4f, 0.18f), matWood, praca);
        Prim(PrimitiveType.Cube, "Teto_Poco", new Vector3(0, 2.1f, 0), new Vector3(2.0f, 0.25f, 1.4f), matRoofRed, praca);

        CriarPoste("Poste_Praca_01", new Vector3(5, 0, 5), praca);
        CriarPoste("Poste_Praca_02", new Vector3(-5, 0, 5), praca);
        CriarPoste("Poste_Praca_03", new Vector3(5, 0, -5), praca);
        CriarPoste("Poste_Praca_04", new Vector3(-5, 0, -5), praca);
    }

    private static void CriarConstrucoes()
    {
        Transform casas = new GameObject("Construcoes").transform;
        casas.SetParent(root, false);

        CriarCasa("Taverna_Grande", new Vector3(-8, 0, 8), new Vector3(9, 5, 7), 15, matRoofBlue, casas);
        CriarCasa("Casa_01", new Vector3(10, 0, 8), new Vector3(5, 3.5f, 5), -20, matRoofRed, casas);
        CriarCasa("Casa_02", new Vector3(-13, 0, -7), new Vector3(5, 3.5f, 5), 35, matRoofRed, casas);
        CriarCasa("Casa_03", new Vector3(9, 0, -11), new Vector3(5, 3.3f, 4.5f), -35, matRoofBlue, casas);
        CriarCasa("Casa_04", new Vector3(21, 0, -4), new Vector3(6, 3.8f, 5), -60, matRoofRed, casas);
        CriarCasa("Casa_05", new Vector3(-24, 0, -8), new Vector3(5, 3.5f, 5), 65, matRoofRed, casas);
        CriarCasa("Casa_06", new Vector3(3, 0, -22), new Vector3(5, 3.4f, 5), 10, matRoofBlue, casas);
        CriarCasa("Casa_07", new Vector3(27, 0, -18), new Vector3(5, 3.4f, 5), -30, matRoofRed, casas);

        CriarCasa("Capela_Pequena", new Vector3(5, 0, 26), new Vector3(6, 5, 8), 0, matRoofBlue, casas);
        Prim(PrimitiveType.Cube, "Torre_Capela", new Vector3(5, 7.2f, 23), new Vector3(2.5f, 4.5f, 2.5f), matWall, casas);
        Prim(PrimitiveType.Cube, "Telhado_Torre_Capela", new Vector3(5, 10.0f, 23), new Vector3(3.2f, 1.2f, 3.2f), matRoofBlue, casas);

        CriarFerreiro(new Vector3(-28, 0, 10), casas);
        CriarEstabulo(new Vector3(-32, 0, -18), casas);
    }

    private static void CriarCasa(string name, Vector3 pos, Vector3 size, float rotY, Material roofMat, Transform parent)
    {
        GameObject casa = new GameObject(name);
        casa.transform.SetParent(parent, false);
        casa.transform.localPosition = pos;
        casa.transform.localRotation = Quaternion.Euler(0, rotY, 0);

        float w = size.x;
        float h = size.y;
        float d = size.z;
        float roofH = Mathf.Max(1.3f, h * 0.45f);

        Prim(PrimitiveType.Cube, "Corpo", new Vector3(0, h / 2f, 0), new Vector3(w, h, d), matWall, casa.transform);
        CriarTelhado("Telhado", casa.transform, new Vector3(0, h, 0), w + 1.2f, d + 1.2f, roofH, roofMat);

        Prim(PrimitiveType.Cube, "Porta", new Vector3(0, 1.05f, -d / 2f - 0.05f), new Vector3(1.1f, 2.1f, 0.12f), matDarkWood, casa.transform);
        Prim(PrimitiveType.Cube, "Janela_Esquerda", new Vector3(-w * 0.28f, h * 0.58f, -d / 2f - 0.06f), new Vector3(0.8f, 0.8f, 0.12f), matWarmLight, casa.transform);
        Prim(PrimitiveType.Cube, "Janela_Direita", new Vector3(w * 0.28f, h * 0.58f, -d / 2f - 0.06f), new Vector3(0.8f, 0.8f, 0.12f), matWarmLight, casa.transform);

        Prim(PrimitiveType.Cube, "Viga_Frontal", new Vector3(0, h - 0.2f, -d / 2f - 0.08f), new Vector3(w + 0.2f, 0.25f, 0.15f), matWood, casa.transform);
        Prim(PrimitiveType.Cube, "Chamine", new Vector3(w * 0.25f, h + roofH * 0.55f, 0.4f), new Vector3(0.7f, 1.7f, 0.7f), matStone, casa.transform);
    }

    private static void CriarTelhado(string name, Transform parent, Vector3 localPos, float width, float depth, float height, Material mat)
    {
        GameObject roof = new GameObject(name);
        roof.transform.SetParent(parent, false);
        roof.transform.localPosition = localPos;

        MeshFilter mf = roof.AddComponent<MeshFilter>();
        MeshRenderer mr = roof.AddComponent<MeshRenderer>();
        mr.sharedMaterial = mat;

        float w = width / 2f;
        float d = depth / 2f;

        Vector3[] v =
        {
            new Vector3(-w, 0, -d),
            new Vector3(w, 0, -d),
            new Vector3(w, 0, d),
            new Vector3(-w, 0, d),
            new Vector3(0, height, -d),
            new Vector3(0, height, d)
        };

        int[] t =
        {
            0, 3, 4,
            3, 5, 4,

            1, 4, 2,
            2, 4, 5,

            0, 4, 1,
            3, 2, 5
        };

        Mesh mesh = new Mesh();
        mesh.vertices = v;
        mesh.triangles = t;
        mesh.RecalculateNormals();

        mf.sharedMesh = mesh;
        roof.AddComponent<MeshCollider>();
    }

    private static void CriarFerreiro(Vector3 pos, Transform parent)
    {
        GameObject ferreiro = new GameObject("Ferreiro");
        ferreiro.transform.SetParent(parent, false);
        ferreiro.transform.localPosition = pos;
        ferreiro.transform.localRotation = Quaternion.Euler(0, -25, 0);

        Prim(PrimitiveType.Cube, "Base_Ferreiro", new Vector3(0, 1.6f, 0), new Vector3(7, 3.2f, 5), matWall, ferreiro.transform);
        CriarTelhado("Telhado_Ferreiro", ferreiro.transform, new Vector3(0, 3.2f, 0), 8, 6, 2, matRoofRed);

        Prim(PrimitiveType.Cube, "Forja", new Vector3(-2.2f, 0.8f, -2.9f), new Vector3(1.6f, 1.2f, 1.2f), matStone, ferreiro.transform);
        Prim(PrimitiveType.Cube, "Brilho_Forja", new Vector3(-2.2f, 1.25f, -3.55f), new Vector3(1.1f, 0.5f, 0.1f), matWarmLight, ferreiro.transform);
        Prim(PrimitiveType.Cube, "Chamine_Ferreiro", new Vector3(-2.2f, 4.8f, -1.2f), new Vector3(0.9f, 3.2f, 0.9f), matStone, ferreiro.transform);
    }

    private static void CriarEstabulo(Vector3 pos, Transform parent)
    {
        GameObject estabulo = new GameObject("Estabulo");
        estabulo.transform.SetParent(parent, false);
        estabulo.transform.localPosition = pos;
        estabulo.transform.localRotation = Quaternion.Euler(0, 20, 0);

        Prim(PrimitiveType.Cube, "Base_Estabulo", new Vector3(0, 1.7f, 0), new Vector3(8, 3.4f, 5), matWood, estabulo.transform);
        CriarTelhado("Telhado_Estabulo", estabulo.transform, new Vector3(0, 3.4f, 0), 9, 6, 2.2f, matRoofRed);

        Prim(PrimitiveType.Cube, "Entrada_Estabulo", new Vector3(0, 1.2f, -2.6f), new Vector3(3.0f, 2.4f, 0.2f), matDarkWood, estabulo.transform);
    }

    private static void CriarRioEPonte()
    {
        Transform rio = new GameObject("Rio_E_Ponte").transform;
        rio.SetParent(root, false);

        Prim(PrimitiveType.Cube, "Rio_Principal", new Vector3(31, 0.02f, -20), new Vector3(9, 0.08f, 48), matWater, rio, -28);
        Prim(PrimitiveType.Cube, "Ponte_Madeira", new Vector3(25, 0.6f, -25), new Vector3(8, 0.4f, 4), matWood, rio, -28);

        for (int i = -3; i <= 3; i++)
        {
            Prim(PrimitiveType.Cube, "Tabua_Ponte_" + i, new Vector3(25 + i * 0.9f, 0.9f, -25), new Vector3(0.15f, 0.2f, 4.5f), matDarkWood, rio, -28);
        }

        Prim(PrimitiveType.Cube, "Corrimao_Ponte_A", new Vector3(23.1f, 1.5f, -23.5f), new Vector3(8, 0.2f, 0.2f), matDarkWood, rio, -28);
        Prim(PrimitiveType.Cube, "Corrimao_Ponte_B", new Vector3(26.9f, 1.5f, -26.5f), new Vector3(8, 0.2f, 0.2f), matDarkWood, rio, -28);
    }

    private static void CriarCampos()
    {
        Transform campos = new GameObject("Campos_E_Hortas").transform;
        campos.SetParent(root, false);

        Prim(PrimitiveType.Cube, "Campo_01_Base", new Vector3(29, 0.06f, 20), new Vector3(14, 0.08f, 10), matDirt, campos);
        Prim(PrimitiveType.Cube, "Campo_02_Base", new Vector3(18, 0.06f, 25), new Vector3(10, 0.08f, 8), matDirt, campos);

        for (int i = 0; i < 6; i++)
        {
            Prim(PrimitiveType.Cube, "Fileira_Campo_01_" + i, new Vector3(23.5f + i * 2.1f, 0.25f, 20), new Vector3(0.7f, 0.35f, 9), matCrop, campos);
        }

        for (int i = 0; i < 5; i++)
        {
            Prim(PrimitiveType.Sphere, "Abobora_" + i, new Vector3(16 + i * 1.5f, 0.35f, 27), new Vector3(0.8f, 0.5f, 0.8f), matPumpkin, campos);
        }

        CriarCercaRetangular("Cerca_Campo_01", new Vector3(29, 0, 20), new Vector2(16, 12), campos);
        CriarCercaRetangular("Cerca_Campo_02", new Vector3(18, 0, 25), new Vector2(12, 10), campos);
    }

    private static void CriarCercaRetangular(string name, Vector3 center, Vector2 size, Transform parent)
    {
        GameObject fence = new GameObject(name);
        fence.transform.SetParent(parent, false);
        fence.transform.localPosition = center;

        float w = size.x;
        float d = size.y;

        Prim(PrimitiveType.Cube, "Cerca_Frente", new Vector3(0, 0.65f, -d / 2f), new Vector3(w, 0.25f, 0.18f), matWood, fence.transform);
        Prim(PrimitiveType.Cube, "Cerca_Tras", new Vector3(0, 0.65f, d / 2f), new Vector3(w, 0.25f, 0.18f), matWood, fence.transform);
        Prim(PrimitiveType.Cube, "Cerca_Esquerda", new Vector3(-w / 2f, 0.65f, 0), new Vector3(0.18f, 0.25f, d), matWood, fence.transform);
        Prim(PrimitiveType.Cube, "Cerca_Direita", new Vector3(w / 2f, 0.65f, 0), new Vector3(0.18f, 0.25f, d), matWood, fence.transform);

        Prim(PrimitiveType.Cube, "Poste_01", new Vector3(-w / 2f, 0.7f, -d / 2f), new Vector3(0.35f, 1.4f, 0.35f), matDarkWood, fence.transform);
        Prim(PrimitiveType.Cube, "Poste_02", new Vector3(w / 2f, 0.7f, -d / 2f), new Vector3(0.35f, 1.4f, 0.35f), matDarkWood, fence.transform);
        Prim(PrimitiveType.Cube, "Poste_03", new Vector3(-w / 2f, 0.7f, d / 2f), new Vector3(0.35f, 1.4f, 0.35f), matDarkWood, fence.transform);
        Prim(PrimitiveType.Cube, "Poste_04", new Vector3(w / 2f, 0.7f, d / 2f), new Vector3(0.35f, 1.4f, 0.35f), matDarkWood, fence.transform);
    }

    private static void CriarPoste(string name, Vector3 pos, Transform parent)
    {
        GameObject poste = new GameObject(name);
        poste.transform.SetParent(parent, false);
        poste.transform.localPosition = pos;

        Prim(PrimitiveType.Cylinder, "Madeira", new Vector3(0, 1.4f, 0), new Vector3(0.15f, 1.4f, 0.15f), matDarkWood, poste.transform);
        Prim(PrimitiveType.Cube, "Lanterna", new Vector3(0, 2.7f, 0), new Vector3(0.45f, 0.45f, 0.45f), matWarmLight, poste.transform);
    }

    private static void CriarArvores()
    {
        Transform arvores = new GameObject("Arvores_E_Floresta").transform;
        arvores.SetParent(root, false);

        Random.InitState(5790);

        for (int i = 0; i < 75; i++)
        {
            float x = Random.Range(-47f, 47f);
            float z = Random.Range(-47f, 47f);

            float distCentro = Vector2.Distance(new Vector2(x, z), Vector2.zero);

            if (distCentro < 24f && Random.value < 0.75f)
                continue;

            CriarArvore("Arvore_" + i, new Vector3(x, 0, z), arvores);
        }
    }

    private static void CriarArvore(string name, Vector3 pos, Transform parent)
    {
        GameObject tree = new GameObject(name);
        tree.transform.SetParent(parent, false);
        tree.transform.localPosition = pos;

        float h = Random.Range(2.8f, 5.5f);
        float crown = Random.Range(1.8f, 3.2f);

        Prim(PrimitiveType.Cylinder, "Tronco", new Vector3(0, h / 2f, 0), new Vector3(0.35f, h / 2f, 0.35f), matWood, tree.transform);
        Prim(PrimitiveType.Sphere, "Copa", new Vector3(0, h + 0.8f, 0), new Vector3(crown, crown, crown), matLeaf, tree.transform);
    }

    private static void CriarLuzECamera()
    {
        Light existingLight = Object.FindFirstObjectByType<Light>();

        if (existingLight == null)
        {
            GameObject lightObj = new GameObject("Sol_Directional_Light");
            lightObj.transform.SetParent(root, false);
            lightObj.transform.rotation = Quaternion.Euler(45, -35, 0);

            Light light = lightObj.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.2f;
        }

        Camera cam = Camera.main;

        if (cam == null)
        {
            GameObject camObj = new GameObject("Camera_Blockout");
            camObj.tag = "MainCamera";
            cam = camObj.AddComponent<Camera>();
        }

        cam.transform.position = new Vector3(38, 38, -45);
        cam.transform.rotation = Quaternion.Euler(55, -38, 0);
        cam.orthographic = true;
        cam.orthographicSize = 36;
        cam.nearClipPlane = 0.1f;
        cam.farClipPlane = 500f;
    }
}
#endif