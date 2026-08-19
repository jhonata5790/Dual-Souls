
using UnityEngine;

[ExecuteAlways]
[DisallowMultipleComponent]
public class HydraulicRoomInteriorBuilder : MonoBehaviour
{
    public enum PipeAxis
    {
        X,
        Y,
        Z
    }

    [Header("Geração")]
    public bool clearBeforeBuild = true;
    public bool addColliders = true;
    public bool markStatic = true;

    [Header("Tamanho base da sala")]
    [Tooltip("Largura da sala no eixo X.")]
    public float roomSizeX = 132f;

    [Tooltip("Altura da sala no eixo Y.")]
    public float roomSizeY = 101f;

    [Tooltip("Profundidade da sala no eixo Z.")]
    public float roomSizeZ = 100f;

    [Header("Opções")]
    [Tooltip("Cria um piso de referência. Desligue se sua sala já tem piso.")]
    public bool createReferenceFloor = false;

    [Tooltip("Cria marcações de corredor no chão.")]
    public bool createWalkwayMarkers = true;

    [Tooltip("Cria snap points nas laterais da sala.")]
    public bool createSnapPoints = true;

    [Tooltip("Cria luzes simples de teste.")]
    public bool createTestLights = true;

    [Header("Cores")]
    public Color floorColor = new Color(0.18f, 0.19f, 0.20f, 1f);
    public Color metalDarkColor = new Color(0.08f, 0.085f, 0.09f, 1f);
    public Color metalLightColor = new Color(0.48f, 0.50f, 0.52f, 1f);
    public Color hydraulicBlueColor = new Color(0.05f, 0.35f, 0.90f, 1f);
    public Color fluidGreenBlueColor = new Color(0.05f, 0.75f, 0.62f, 1f);
    public Color safetyYellowColor = new Color(1.0f, 0.75f, 0.05f, 1f);
    public Color warningRedColor = new Color(0.85f, 0.04f, 0.04f, 1f);
    public Color rubberBlackColor = new Color(0.02f, 0.02f, 0.025f, 1f);
    public Color cardboardColor = new Color(0.55f, 0.34f, 0.16f, 1f);
    public Color walkwayColor = new Color(1.0f, 0.70f, 0.05f, 0.35f);

    private Transform root;
    private Transform machinesRoot;
    private Transform pipesRoot;
    private Transform tanksRoot;
    private Transform panelsRoot;
    private Transform propsRoot;
    private Transform markersRoot;
    private Transform snapRoot;
    private Transform lightsRoot;

    private Material floorMat;
    private Material metalDarkMat;
    private Material metalLightMat;
    private Material hydraulicBlueMat;
    private Material fluidMat;
    private Material safetyYellowMat;
    private Material warningRedMat;
    private Material rubberBlackMat;
    private Material cardboardMat;
    private Material walkwayMat;
    private Material glassMat;

    [ContextMenu("Build Hydraulic Room Interior")]
    public void Build()
    {
        if (clearBeforeBuild)
            ClearGenerated();

        CreateMaterials();
        CreateRoots();

        if (createReferenceFloor)
            CreateReferenceFloor();

        if (createWalkwayMarkers)
            CreateWalkwayMarkers();

        CreateMainHydraulicMachines();
        CreateReservoirTanks();
        CreatePumpStations();
        CreatePipeNetwork();
        CreateControlPanels();
        CreatePropsAndHazards();

        if (createSnapPoints)
            CreateSnapPoints();

        if (createTestLights)
            CreateTestLights();

        Debug.Log("[TeddyWorks] Interior da Sala Hidráulica criado com sucesso.", this);
    }

    [ContextMenu("Clear Hydraulic Room Interior")]
    public void ClearGenerated()
    {
        Transform old = transform.Find("HydraulicRoom_Interior_Generated");

        if (old != null)
        {
            if (Application.isPlaying)
                Destroy(old.gameObject);
            else
                DestroyImmediate(old.gameObject);
        }
    }

    void CreateRoots()
    {
        root = NewGroup("HydraulicRoom_Interior_Generated", transform);
        machinesRoot = NewGroup("01_Machines_Hydraulic", root);
        tanksRoot = NewGroup("02_Tanks_Reservoirs", root);
        pipesRoot = NewGroup("03_Pipes_And_Valves", root);
        panelsRoot = NewGroup("04_Control_Panels", root);
        propsRoot = NewGroup("05_Props_And_Hazards", root);
        markersRoot = NewGroup("06_Walkway_Markers", root);
        snapRoot = NewGroup("07_SnapPoints", root);
        lightsRoot = NewGroup("08_Test_Lights", root);
    }

    Transform NewGroup(string name, Transform parent)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = Vector3.zero;
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale = Vector3.one;
        return go.transform;
    }

    void CreateMaterials()
    {
        floorMat = MakeMaterial("HR_Floor_Dark", floorColor, false);
        metalDarkMat = MakeMaterial("HR_Metal_Dark", metalDarkColor, false);
        metalLightMat = MakeMaterial("HR_Metal_Light", metalLightColor, false);
        hydraulicBlueMat = MakeMaterial("HR_Hydraulic_Blue", hydraulicBlueColor, false);
        fluidMat = MakeMaterial("HR_Hydraulic_Fluid_GreenBlue", fluidGreenBlueColor, false);
        safetyYellowMat = MakeMaterial("HR_Safety_Yellow", safetyYellowColor, false);
        warningRedMat = MakeMaterial("HR_Warning_Red", warningRedColor, false);
        rubberBlackMat = MakeMaterial("HR_Rubber_Black", rubberBlackColor, false);
        cardboardMat = MakeMaterial("HR_Cardboard", cardboardColor, false);
        walkwayMat = MakeMaterial("HR_Walkway_Marker_Transparent", walkwayColor, true);
        glassMat = MakeMaterial("HR_Glass_Gauge", new Color(0.55f, 0.80f, 1f, 0.45f), true);
    }

    Material MakeMaterial(string matName, Color color, bool transparent)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
            shader = Shader.Find("Standard");

        Material mat = new Material(shader);
        mat.name = matName;

        if (mat.HasProperty("_BaseColor"))
            mat.SetColor("_BaseColor", color);

        if (mat.HasProperty("_Color"))
            mat.SetColor("_Color", color);

        if (transparent)
        {
            if (mat.HasProperty("_Surface"))
                mat.SetFloat("_Surface", 1f);

            mat.renderQueue = 3000;
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        }

        return mat;
    }

    GameObject Cube(string name, Vector3 position, Vector3 size, Material material, Transform parent)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = position;
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale = size;

        ApplyMaterialAndCollider(go, material);
        return go;
    }

    GameObject RotatedCube(string name, Vector3 position, Vector3 size, Vector3 euler, Material material, Transform parent)
    {
        GameObject go = Cube(name, position, size, material, parent);
        go.transform.localEulerAngles = euler;
        return go;
    }

    GameObject Cylinder(string name, Vector3 position, float diameter, float length, PipeAxis axis, Material material, Transform parent)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = position;
        go.transform.localRotation = Quaternion.identity;

        // Cylinder padrão da Unity: altura 2 no eixo Y e diâmetro 1 no X/Z.
        go.transform.localScale = new Vector3(diameter, length * 0.5f, diameter);

        if (axis == PipeAxis.X)
            go.transform.localEulerAngles = new Vector3(0f, 0f, 90f);
        else if (axis == PipeAxis.Z)
            go.transform.localEulerAngles = new Vector3(90f, 0f, 0f);

        ApplyMaterialAndCollider(go, material);
        return go;
    }

    GameObject Sphere(string name, Vector3 position, Vector3 scale, Material material, Transform parent)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = position;
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale = scale;

        ApplyMaterialAndCollider(go, material);
        return go;
    }

    void ApplyMaterialAndCollider(GameObject go, Material material)
    {
        Renderer r = go.GetComponent<Renderer>();
        if (r != null && material != null)
            r.sharedMaterial = material;

        Collider col = go.GetComponent<Collider>();
        if (!addColliders && col != null)
        {
            if (Application.isPlaying)
                Destroy(col);
            else
                DestroyImmediate(col);
        }

        if (markStatic)
            go.isStatic = true;
    }

    void CreateReferenceFloor()
    {
        Cube(
            "Reference_Floor_HydraulicRoom_132x100",
            new Vector3(0f, -0.05f, 0f),
            new Vector3(roomSizeX, 0.1f, roomSizeZ),
            floorMat,
            root
        );
    }

    void CreateWalkwayMarkers()
    {
        float halfX = roomSizeX * 0.5f;
        float halfZ = roomSizeZ * 0.5f;

        // Corredores principais: centro e laterais.
        Cube("Walkway_Central_Line_Left", new Vector3(-5f, 0.03f, 0f), new Vector3(0.25f, 0.04f, roomSizeZ - 16f), walkwayMat, markersRoot);
        Cube("Walkway_Central_Line_Right", new Vector3(5f, 0.03f, 0f), new Vector3(0.25f, 0.04f, roomSizeZ - 16f), walkwayMat, markersRoot);

        Cube("Walkway_Left_Service_Line", new Vector3(-halfX + 12f, 0.03f, 0f), new Vector3(0.25f, 0.04f, roomSizeZ - 14f), walkwayMat, markersRoot);
        Cube("Walkway_Right_Service_Line", new Vector3(halfX - 12f, 0.03f, 0f), new Vector3(0.25f, 0.04f, roomSizeZ - 14f), walkwayMat, markersRoot);

        Cube("Walkway_Back_Service_Line", new Vector3(0f, 0.03f, halfZ - 10f), new Vector3(roomSizeX - 18f, 0.04f, 0.25f), walkwayMat, markersRoot);
        Cube("Walkway_Front_Service_Line", new Vector3(0f, 0.03f, -halfZ + 10f), new Vector3(roomSizeX - 18f, 0.04f, 0.25f), walkwayMat, markersRoot);
    }

    void CreateMainHydraulicMachines()
    {
        CreateHydraulicPress(new Vector3(0f, 0f, -4f));
        CreateHydraulicLiftTable(new Vector3(0f, 0f, -28f));
        CreatePipeTestBench(new Vector3(0f, 0f, 22f));
    }

    void CreateHydraulicPress(Vector3 center)
    {
        Transform g = NewGroup("Main_Hydraulic_Press_Block", machinesRoot);

        Cube("Press_Foundation", center + new Vector3(0f, 0.25f, 0f), new Vector3(18f, 0.5f, 12f), metalDarkMat, g);
        Cube("Press_Work_Table", center + new Vector3(0f, 1.0f, 0f), new Vector3(13f, 0.7f, 8f), metalLightMat, g);

        float x = 6.5f;
        float z = 4.2f;

        Cube("Press_Column_FL", center + new Vector3(-x, 4.5f, -z), new Vector3(0.6f, 8f, 0.6f), metalDarkMat, g);
        Cube("Press_Column_FR", center + new Vector3(x, 4.5f, -z), new Vector3(0.6f, 8f, 0.6f), metalDarkMat, g);
        Cube("Press_Column_BL", center + new Vector3(-x, 4.5f, z), new Vector3(0.6f, 8f, 0.6f), metalDarkMat, g);
        Cube("Press_Column_BR", center + new Vector3(x, 4.5f, z), new Vector3(0.6f, 8f, 0.6f), metalDarkMat, g);

        Cube("Press_Top_Beam", center + new Vector3(0f, 8.8f, 0f), new Vector3(15f, 1.0f, 9f), metalDarkMat, g);
        Cube("Press_Sliding_Plate", center + new Vector3(0f, 4.5f, 0f), new Vector3(11f, 0.6f, 7f), safetyYellowMat, g);

        Cylinder("Press_Main_Vertical_Piston", center + new Vector3(0f, 6.6f, 0f), 1.2f, 4.5f, PipeAxis.Y, hydraulicBlueMat, g);
        Cylinder("Press_Left_Piston", center + new Vector3(-3.5f, 6.0f, 0f), 0.55f, 3.6f, PipeAxis.Y, hydraulicBlueMat, g);
        Cylinder("Press_Right_Piston", center + new Vector3(3.5f, 6.0f, 0f), 0.55f, 3.6f, PipeAxis.Y, hydraulicBlueMat, g);

        Cube("Press_Danger_Stripe_Front", center + new Vector3(0f, 1.45f, -6.15f), new Vector3(15f, 0.15f, 0.15f), warningRedMat, g);
        Cube("Press_Danger_Stripe_Back", center + new Vector3(0f, 1.45f, 6.15f), new Vector3(15f, 0.15f, 0.15f), warningRedMat, g);
    }

    void CreateHydraulicLiftTable(Vector3 center)
    {
        Transform g = NewGroup("Hydraulic_Lift_Table_Block", machinesRoot);

        Cube("Lift_Base", center + new Vector3(0f, 0.25f, 0f), new Vector3(16f, 0.5f, 10f), metalDarkMat, g);
        Cube("Lift_Table_Platform", center + new Vector3(0f, 2.4f, 0f), new Vector3(13f, 0.45f, 8f), metalLightMat, g);

        // Tesouras simplificadas com blocos inclinados.
        RotatedCube("Lift_Scissor_Left_A", center + new Vector3(-3.5f, 1.35f, 0f), new Vector3(0.35f, 4.5f, 0.35f), new Vector3(0f, 0f, 35f), hydraulicBlueMat, g);
        RotatedCube("Lift_Scissor_Left_B", center + new Vector3(-3.5f, 1.35f, 0f), new Vector3(0.35f, 4.5f, 0.35f), new Vector3(0f, 0f, -35f), hydraulicBlueMat, g);
        RotatedCube("Lift_Scissor_Right_A", center + new Vector3(3.5f, 1.35f, 0f), new Vector3(0.35f, 4.5f, 0.35f), new Vector3(0f, 0f, 35f), hydraulicBlueMat, g);
        RotatedCube("Lift_Scissor_Right_B", center + new Vector3(3.5f, 1.35f, 0f), new Vector3(0.35f, 4.5f, 0.35f), new Vector3(0f, 0f, -35f), hydraulicBlueMat, g);

        Cylinder("Lift_Main_Cylinder_Left", center + new Vector3(-5.8f, 1.4f, -2.8f), 0.55f, 4.0f, PipeAxis.X, hydraulicBlueMat, g);
        Cylinder("Lift_Main_Cylinder_Right", center + new Vector3(5.8f, 1.4f, 2.8f), 0.55f, 4.0f, PipeAxis.X, hydraulicBlueMat, g);
    }

    void CreatePipeTestBench(Vector3 center)
    {
        Transform g = NewGroup("Hydraulic_Test_Bench_Block", machinesRoot);

        Cube("TestBench_Base", center + new Vector3(0f, 0.45f, 0f), new Vector3(18f, 0.9f, 7f), metalDarkMat, g);
        Cube("TestBench_BackPanel", center + new Vector3(0f, 2.8f, 3.2f), new Vector3(18f, 4.0f, 0.35f), metalLightMat, g);
        Cube("TestBench_Worktop", center + new Vector3(0f, 1.35f, 0f), new Vector3(17f, 0.35f, 6f), metalLightMat, g);

        for (int i = 0; i < 5; i++)
        {
            float x = -6f + i * 3f;
            Cylinder("TestBench_Gauge_" + i, center + new Vector3(x, 3.35f, 2.95f), 0.7f, 0.12f, PipeAxis.Z, glassMat, g);
            Cylinder("TestBench_Valve_" + i, center + new Vector3(x, 2.55f, 2.75f), 0.35f, 0.25f, PipeAxis.Z, warningRedMat, g);
        }

        Cylinder("TestBench_Horizontal_Pipe", center + new Vector3(0f, 2.35f, 2.65f), 0.35f, 15f, PipeAxis.X, hydraulicBlueMat, g);
    }

    void CreateReservoirTanks()
    {
        float backZ = roomSizeZ * 0.5f - 13f;

        CreateReservoirTank("Reservoir_Tank_Left", new Vector3(-38f, 0f, backZ));
        CreateReservoirTank("Reservoir_Tank_Center", new Vector3(0f, 0f, backZ));
        CreateReservoirTank("Reservoir_Tank_Right", new Vector3(38f, 0f, backZ));

        // Bacia de contenção visual.
        Cube("Reservoir_Containment_Basin", new Vector3(0f, 0.12f, backZ), new Vector3(96f, 0.24f, 16f), rubberBlackMat, tanksRoot);
    }

    void CreateReservoirTank(string name, Vector3 center)
    {
        Transform g = NewGroup(name, tanksRoot);

        Cylinder(name + "_Tank_Body", center + new Vector3(0f, 5.5f, 0f), 7.5f, 11f, PipeAxis.Y, fluidMat, g);
        Cylinder(name + "_Tank_Top_Cap", center + new Vector3(0f, 11.1f, 0f), 7.8f, 0.35f, PipeAxis.Y, metalDarkMat, g);
        Cylinder(name + "_Tank_Bottom_Cap", center + new Vector3(0f, 0.35f, 0f), 7.8f, 0.35f, PipeAxis.Y, metalDarkMat, g);

        Cube(name + "_Level_Window", center + new Vector3(0f, 5.5f, -3.9f), new Vector3(1.1f, 7.5f, 0.12f), glassMat, g);
        Cylinder(name + "_Top_Pipe_Output", center + new Vector3(0f, 12.2f, 0f), 0.7f, 4f, PipeAxis.Y, hydraulicBlueMat, g);
    }

    void CreatePumpStations()
    {
        float leftX = -roomSizeX * 0.5f + 18f;
        float rightX = roomSizeX * 0.5f - 18f;

        float[] zPositions = { -30f, -12f, 8f };

        for (int i = 0; i < zPositions.Length; i++)
        {
            CreatePumpStation("Pump_Station_Left_" + (i + 1), new Vector3(leftX, 0f, zPositions[i]), true);
            CreatePumpStation("Pump_Station_Right_" + (i + 1), new Vector3(rightX, 0f, zPositions[i]), false);
        }
    }

    void CreatePumpStation(string name, Vector3 center, bool leftSide)
    {
        Transform g = NewGroup(name, machinesRoot);

        Cube(name + "_Base", center + new Vector3(0f, 0.35f, 0f), new Vector3(9f, 0.7f, 6f), metalDarkMat, g);
        Cube(name + "_Motor_Block", center + new Vector3(0f, 1.35f, -1.2f), new Vector3(4.5f, 1.8f, 2.5f), hydraulicBlueMat, g);
        Cylinder(name + "_Pump_Cylinder", center + new Vector3(0f, 1.55f, 1.6f), 2.2f, 3.8f, PipeAxis.X, metalLightMat, g);

        float dir = leftSide ? 1f : -1f;

        Cylinder(name + "_Output_Pipe_To_Center", center + new Vector3(dir * 5.0f, 2.0f, 1.6f), 0.45f, 8f, PipeAxis.X, hydraulicBlueMat, g);
        Sphere(name + "_Valve_Red", center + new Vector3(dir * 2.7f, 2.15f, 1.6f), new Vector3(0.7f, 0.7f, 0.7f), warningRedMat, g);
        Cube(name + "_Small_Control_Box", center + new Vector3(-dir * 3.8f, 1.35f, 2.7f), new Vector3(1.3f, 1.6f, 0.35f), metalDarkMat, g);
        Cube(name + "_Status_Blue", center + new Vector3(-dir * 3.8f, 1.7f, 2.48f), new Vector3(0.45f, 0.45f, 0.08f), hydraulicBlueMat, g);
    }

    void CreatePipeNetwork()
    {
        float backZ = roomSizeZ * 0.5f - 13f;

        // Linha principal no teto.
        Cylinder("Ceiling_Main_Hydraulic_Pipe_Z", new Vector3(0f, 13f, 0f), 0.85f, roomSizeZ - 22f, PipeAxis.Z, hydraulicBlueMat, pipesRoot);
        Cylinder("Ceiling_Cross_Pipe_X_Back", new Vector3(0f, 13f, backZ), 0.85f, roomSizeX - 26f, PipeAxis.X, hydraulicBlueMat, pipesRoot);
        Cylinder("Ceiling_Cross_Pipe_X_Middle", new Vector3(0f, 11f, -4f), 0.65f, roomSizeX - 36f, PipeAxis.X, hydraulicBlueMat, pipesRoot);
        Cylinder("Ceiling_Cross_Pipe_X_Front", new Vector3(0f, 10f, -28f), 0.65f, roomSizeX - 42f, PipeAxis.X, hydraulicBlueMat, pipesRoot);

        // Descidas para as máquinas principais.
        Cylinder("Drop_Pipe_To_Press", new Vector3(0f, 7f, -4f), 0.55f, 12f, PipeAxis.Y, hydraulicBlueMat, pipesRoot);
        Cylinder("Drop_Pipe_To_Lift", new Vector3(0f, 6f, -28f), 0.50f, 10f, PipeAxis.Y, hydraulicBlueMat, pipesRoot);
        Cylinder("Drop_Pipe_To_TestBench", new Vector3(0f, 6f, 22f), 0.50f, 10f, PipeAxis.Y, hydraulicBlueMat, pipesRoot);

        // Válvulas visuais na rede.
        Sphere("Ceiling_Valve_Red_Back", new Vector3(-16f, 13f, backZ), new Vector3(1.2f, 1.2f, 1.2f), warningRedMat, pipesRoot);
        Sphere("Ceiling_Valve_Red_Mid", new Vector3(16f, 11f, -4f), new Vector3(1.1f, 1.1f, 1.1f), warningRedMat, pipesRoot);
        Sphere("Ceiling_Valve_Yellow_Front", new Vector3(0f, 10f, -28f), new Vector3(1.1f, 1.1f, 1.1f), safetyYellowMat, pipesRoot);
    }

    void CreateControlPanels()
    {
        float rightWallX = roomSizeX * 0.5f - 4f;
        float leftWallX = -roomSizeX * 0.5f + 4f;

        CreatePanel("Main_Hydraulic_Control_Panel", new Vector3(rightWallX, 2.2f, -36f), true);
        CreatePanel("Pressure_Valve_Board_Right", new Vector3(rightWallX, 2.2f, 6f), true);
        CreatePanel("Pressure_Valve_Board_Left", new Vector3(leftWallX, 2.2f, 6f), false);

        // Console perto da prensa.
        Transform g = NewGroup("Operator_Control_Console", panelsRoot);
        Cube("Console_Base", new Vector3(12f, 0.6f, -8f), new Vector3(4.2f, 1.2f, 2.4f), metalDarkMat, g);
        RotatedCube("Console_Top_Angled", new Vector3(12f, 1.45f, -8.35f), new Vector3(4.2f, 0.25f, 1.5f), new Vector3(-18f, 0f, 0f), metalLightMat, g);
        Cube("Console_Blue_Screen", new Vector3(12f, 1.62f, -8.95f), new Vector3(1.6f, 0.08f, 0.65f), hydraulicBlueMat, g);
        Sphere("Console_Red_Button", new Vector3(10.7f, 1.72f, -8.7f), new Vector3(0.35f, 0.35f, 0.35f), warningRedMat, g);
        Sphere("Console_Yellow_Button", new Vector3(11.4f, 1.72f, -8.7f), new Vector3(0.35f, 0.35f, 0.35f), safetyYellowMat, g);
    }

    void CreatePanel(string name, Vector3 center, bool rightWall)
    {
        Transform g = NewGroup(name, panelsRoot);

        Cube(name + "_Body", center, new Vector3(0.45f, 3.8f, 5.0f), metalDarkMat, g);
        Cube(name + "_Door_Panel", center + new Vector3(rightWall ? -0.25f : 0.25f, 0f, 0f), new Vector3(0.12f, 3.4f, 4.4f), metalLightMat, g);

        Cube(name + "_Screen_Blue", center + new Vector3(rightWall ? -0.33f : 0.33f, 0.9f, 0.9f), new Vector3(0.08f, 1.1f, 0.65f), hydraulicBlueMat, g);
        Sphere(name + "_Light_Red", center + new Vector3(rightWall ? -0.38f : 0.38f, -1.1f, 1.3f), new Vector3(0.35f, 0.35f, 0.35f), warningRedMat, g);
        Sphere(name + "_Light_Yellow", center + new Vector3(rightWall ? -0.38f : 0.38f, -0.4f, 1.3f), new Vector3(0.35f, 0.35f, 0.35f), safetyYellowMat, g);
        Sphere(name + "_Light_Blue", center + new Vector3(rightWall ? -0.38f : 0.38f, 0.3f, 1.3f), new Vector3(0.35f, 0.35f, 0.35f), hydraulicBlueMat, g);
    }

    void CreatePropsAndHazards()
    {
        // Tambor/galões de fluido hidráulico.
        CreateFluidDrumGroup(new Vector3(-48f, 0f, -40f), "Fluid_Drums_Left");
        CreateFluidDrumGroup(new Vector3(48f, 0f, -40f), "Fluid_Drums_Right");

        // Caixas de manutenção.
        Cube("Maintenance_Crate_01", new Vector3(-42f, 0.65f, 30f), new Vector3(6f, 1.3f, 4f), cardboardMat, propsRoot);
        Cube("Maintenance_Crate_02", new Vector3(42f, 0.65f, 30f), new Vector3(6f, 1.3f, 4f), cardboardMat, propsRoot);

        // Vazamento como trigger visual.
        GameObject spill = Cube("Hydraulic_Fluid_Leak_Trigger", new Vector3(-18f, 0.035f, 18f), new Vector3(10f, 0.06f, 6f), fluidMat, propsRoot);
        Collider col = spill.GetComponent<Collider>();
        if (col != null)
            col.isTrigger = true;

        // Barreiras de segurança próximas ao vazamento.
        Cube("Leak_Barrier_01", new Vector3(-24f, 0.6f, 15f), new Vector3(0.4f, 1.2f, 5f), safetyYellowMat, propsRoot);
        Cube("Leak_Barrier_02", new Vector3(-12f, 0.6f, 15f), new Vector3(0.4f, 1.2f, 5f), safetyYellowMat, propsRoot);
        Cube("Leak_Barrier_03", new Vector3(-18f, 0.6f, 21.5f), new Vector3(9f, 1.2f, 0.4f), safetyYellowMat, propsRoot);

        // Dreno/canaleta central.
        Cube("Central_Drain_Channel", new Vector3(0f, 0.04f, 0f), new Vector3(1.2f, 0.08f, roomSizeZ - 24f), rubberBlackMat, propsRoot);
    }

    void CreateFluidDrumGroup(Vector3 start, string groupName)
    {
        Transform g = NewGroup(groupName, propsRoot);

        for (int i = 0; i < 4; i++)
        {
            float x = start.x + (i % 2) * 2.4f;
            float z = start.z + (i / 2) * 2.4f;

            Cylinder(groupName + "_Drum_" + (i + 1), new Vector3(x, 1.1f, z), 1.6f, 2.2f, PipeAxis.Y, fluidMat, g);
            Cube(groupName + "_Label_" + (i + 1), new Vector3(x, 1.35f, z - 0.82f), new Vector3(1.0f, 0.55f, 0.06f), safetyYellowMat, g);
        }
    }

    void CreateSnapPoints()
    {
        float halfX = roomSizeX * 0.5f;
        float halfZ = roomSizeZ * 0.5f;

        Empty("Snap_Front", new Vector3(0f, 1.5f, -halfZ), snapRoot);
        Empty("Snap_Back", new Vector3(0f, 1.5f, halfZ), snapRoot);
        Empty("Snap_Left", new Vector3(-halfX, 1.5f, 0f), snapRoot);
        Empty("Snap_Right", new Vector3(halfX, 1.5f, 0f), snapRoot);

        Empty("Interaction_Main_Hydraulic_Control_Panel", new Vector3(roomSizeX * 0.5f - 6f, 1.5f, -36f), snapRoot);
        Empty("Hazard_Hydraulic_Leak_Area", new Vector3(-18f, 0.5f, 18f), snapRoot);
        Empty("Objective_Main_Press", new Vector3(0f, 1.5f, -4f), snapRoot);
    }

    void Empty(string name, Vector3 position, Transform parent)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = position;
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale = Vector3.one;
    }

    void CreateTestLights()
    {
        CreatePointLight("HydraulicRoom_Test_Light_Center", new Vector3(0f, 14f, 0f), new Color(0.55f, 0.75f, 1f, 1f), 1.6f, 55f);
        CreatePointLight("HydraulicRoom_Test_Light_Back", new Vector3(0f, 14f, 32f), new Color(0.45f, 0.85f, 0.75f, 1f), 1.3f, 45f);
        CreatePointLight("HydraulicRoom_Test_Light_Front", new Vector3(0f, 14f, -34f), new Color(0.55f, 0.75f, 1f, 1f), 1.2f, 45f);
    }

    void CreatePointLight(string name, Vector3 position, Color color, float intensity, float range)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(lightsRoot, false);
        go.transform.localPosition = position;

        Light light = go.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = color;
        light.intensity = intensity;
        light.range = range;
        light.shadows = LightShadows.Soft;
    }
}
