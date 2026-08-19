using UnityEngine;

[ExecuteAlways]
[DisallowMultipleComponent]
public class ElectricalProductionRoomBlockoutBuilder : MonoBehaviour
{
    [Header("Build")]
    public bool clearBeforeBuild = true;
    public bool addColliders = true;
    public bool markStatic = true;
    public bool createCeiling = false;

    [Header("Room Size")]
    public float roomWidth = 40f;
    public float roomDepth = 50f;
    public float wallHeight = 9f;
    public float wallThickness = 0.35f;

    [Header("Blockout Colors")]
    public Color wallColor = new Color(0.36f, 0.72f, 0.78f, 1f);
    public Color floorColor = new Color(0.18f, 0.18f, 0.18f, 1f);
    public Color ceilingColor = new Color(0.06f, 0.06f, 0.07f, 0.35f);
    public Color blueConveyorColor = new Color(0.05f, 0.22f, 0.75f, 1f);
    public Color greenConveyorColor = new Color(0.05f, 0.65f, 0.18f, 1f);
    public Color machineColor = new Color(0.45f, 0.20f, 0.75f, 1f);
    public Color inspectionColor = new Color(1f, 0.72f, 0.05f, 1f);
    public Color platformColor = new Color(0.42f, 0.42f, 0.42f, 1f);
    public Color stairColor = new Color(0.20f, 0.20f, 0.20f, 1f);
    public Color railingColor = new Color(1f, 0.78f, 0.05f, 1f);
    public Color panelColor = new Color(0.85f, 0.04f, 0.04f, 1f);
    public Color boxColor = new Color(0.55f, 0.32f, 0.14f, 1f);
    public Color teddyColor = new Color(0.50f, 0.28f, 0.12f, 1f);
    public Color markerColor = new Color(1f, 0.72f, 0.05f, 0.32f);

    Transform root;
    Transform structure;
    Transform conveyors;
    Transform machines;
    Transform platform;
    Transform stairs;
    Transform electrical;
    Transform props;
    Transform markers;
    Transform snaps;

    Material wallMat, floorMat, ceilingMat, blueMat, greenMat, machineMat, inspectionMat;
    Material platformMat, stairMat, railingMat, panelMat, boxMat, teddyMat, markerMat, blackMat;

    [ContextMenu("Build Electrical Production Room Blockout")]
    public void Build()
    {
        if (clearBeforeBuild) ClearGenerated();
        MakeMaterials();
        MakeRoots();
        BuildStructure();
        BuildWalkwayMarkers();
        BuildConveyors();
        BuildMachines();
        BuildPlatformRing();
        BuildStairs();
        BuildElectricalPanels();
        BuildBoxesAndTeddies();
        BuildSnapPoints();
        Debug.Log("[TeddyWorks] Sala de Produção Elétrica BLOCKOUT criada com sucesso.", this);
    }

    [ContextMenu("Clear Generated Blockout")]
    public void ClearGenerated()
    {
        Transform old = transform.Find("ElectricalProductionRoom_Blockout");
        if (old == null) return;
        if (Application.isPlaying) Destroy(old.gameObject);
        else DestroyImmediate(old.gameObject);
    }

    void MakeRoots()
    {
        root = Group("ElectricalProductionRoom_Blockout", transform);
        structure = Group("01_Structure", root);
        conveyors = Group("02_Conveyors", root);
        machines = Group("03_Machines", root);
        platform = Group("04_Platform_Ring", root);
        stairs = Group("05_Industrial_Stairs", root);
        electrical = Group("06_Electrical_Panels", root);
        props = Group("07_Boxes_And_Teddies", root);
        markers = Group("08_Walkway_Markers", root);
        snaps = Group("09_SnapPoints", root);
    }

    Transform Group(string name, Transform parent)
    {
        GameObject g = new GameObject(name);
        g.transform.SetParent(parent, false);
        g.transform.localPosition = Vector3.zero;
        g.transform.localRotation = Quaternion.identity;
        g.transform.localScale = Vector3.one;
        return g.transform;
    }

    void MakeMaterials()
    {
        wallMat = Mat("BW_Wall_Cyan", wallColor, false);
        floorMat = Mat("BW_Floor_Dark", floorColor, false);
        ceilingMat = Mat("BW_Ceiling_Transparent", ceilingColor, true);
        blueMat = Mat("BW_Conveyor_Blue", blueConveyorColor, false);
        greenMat = Mat("BW_Conveyor_Green", greenConveyorColor, false);
        machineMat = Mat("BW_Machine_Purple", machineColor, false);
        inspectionMat = Mat("BW_Inspection_Yellow", inspectionColor, false);
        platformMat = Mat("BW_Platform_Gray", platformColor, false);
        stairMat = Mat("BW_Stairs_Dark", stairColor, false);
        railingMat = Mat("BW_Railing_Yellow", railingColor, false);
        panelMat = Mat("BW_Electrical_Panel_Red", panelColor, false);
        boxMat = Mat("BW_Cardboard_Box", boxColor, false);
        teddyMat = Mat("BW_Teddy_Brown", teddyColor, false);
        markerMat = Mat("BW_Walkway_Marker", markerColor, true);
        blackMat = Mat("BW_Black_Detail", new Color(0.02f, 0.02f, 0.02f, 1f), false);
    }

    Material Mat(string name, Color color, bool transparent)
    {
        Shader s = Shader.Find("Universal Render Pipeline/Lit");
        if (s == null) s = Shader.Find("Standard");
        Material m = new Material(s);
        m.name = name;
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", color);
        if (m.HasProperty("_Color")) m.SetColor("_Color", color);
        if (transparent)
        {
            if (m.HasProperty("_Surface")) m.SetFloat("_Surface", 1f);
            if (m.HasProperty("_AlphaClip")) m.SetFloat("_AlphaClip", 0f);
            m.renderQueue = 3000;
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        }
        return m;
    }

    GameObject Cube(string name, Vector3 pos, Vector3 scale, Material mat, Transform parent)
    {
        GameObject g = GameObject.CreatePrimitive(PrimitiveType.Cube);
        g.name = name;
        g.transform.SetParent(parent, false);
        g.transform.localPosition = pos;
        g.transform.localScale = scale;
        if (mat != null) g.GetComponent<Renderer>().sharedMaterial = mat;
        if (!addColliders)
        {
            Collider c = g.GetComponent<Collider>();
            if (c != null)
            {
                if (Application.isPlaying) Destroy(c);
                else DestroyImmediate(c);
            }
        }
        if (markStatic) g.isStatic = true;
        return g;
    }

    GameObject CubeRot(string name, Vector3 pos, Vector3 scale, Vector3 euler, Material mat, Transform parent)
    {
        GameObject g = Cube(name, pos, scale, mat, parent);
        g.transform.localEulerAngles = euler;
        return g;
    }

    GameObject Sphere(string name, Vector3 pos, Vector3 scale, Material mat, Transform parent)
    {
        GameObject g = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        g.name = name;
        g.transform.SetParent(parent, false);
        g.transform.localPosition = pos;
        g.transform.localScale = scale;
        if (mat != null) g.GetComponent<Renderer>().sharedMaterial = mat;
        if (!addColliders)
        {
            Collider c = g.GetComponent<Collider>();
            if (c != null)
            {
                if (Application.isPlaying) Destroy(c);
                else DestroyImmediate(c);
            }
        }
        if (markStatic) g.isStatic = true;
        return g;
    }

    void Empty(string name, Vector3 pos, Transform parent)
    {
        GameObject g = new GameObject(name);
        g.transform.SetParent(parent, false);
        g.transform.localPosition = pos;
    }

    void BuildStructure()
    {
        Cube("Floor_40x50", new Vector3(0, 0, -0.1f), new Vector3(40, 50, 0.2f), floorMat, structure);
        if (createCeiling) Cube("Ceiling_Optional", new Vector3(0, 0, 9.2f), new Vector3(40.4f, 50.4f, 0.4f), ceilingMat, structure);

        // Front/back walls with real central openings.
        Cube("Wall_Front_Left", new Vector3(-12.5f, -25, 4.5f), new Vector3(15, wallThickness, wallHeight), wallMat, structure);
        Cube("Wall_Front_Right", new Vector3(12.5f, -25, 4.5f), new Vector3(15, wallThickness, wallHeight), wallMat, structure);
        Cube("Wall_Front_Top", new Vector3(0, -25, 7f), new Vector3(10, wallThickness, 4), wallMat, structure);

        Cube("Wall_Back_Left", new Vector3(-12.5f, 25, 4.5f), new Vector3(15, wallThickness, wallHeight), wallMat, structure);
        Cube("Wall_Back_Right", new Vector3(12.5f, 25, 4.5f), new Vector3(15, wallThickness, wallHeight), wallMat, structure);
        Cube("Wall_Back_Top", new Vector3(0, 25, 7f), new Vector3(10, wallThickness, 4), wallMat, structure);

        // Left/right walls with two openings each.
        Cube("Wall_Left_Front", new Vector3(-20, -18, 4.5f), new Vector3(wallThickness, 14, wallHeight), wallMat, structure);
        Cube("Wall_Left_Middle", new Vector3(-20, 0, 4.5f), new Vector3(wallThickness, 10, wallHeight), wallMat, structure);
        Cube("Wall_Left_Back", new Vector3(-20, 18, 4.5f), new Vector3(wallThickness, 14, wallHeight), wallMat, structure);

        Cube("Wall_Right_Front", new Vector3(20, -18, 4.5f), new Vector3(wallThickness, 14, wallHeight), wallMat, structure);
        Cube("Wall_Right_Middle", new Vector3(20, 0, 4.5f), new Vector3(wallThickness, 10, wallHeight), wallMat, structure);
        Cube("Wall_Right_Back", new Vector3(20, 18, 4.5f), new Vector3(wallThickness, 14, wallHeight), wallMat, structure);

        DoorFrameFrontBack("Door_Main_Front_Frame", new Vector3(0, -25.15f, 2.25f), 5, 4.5f);
        DoorFrameFrontBack("Door_Back_To_Dock_Frame", new Vector3(0, 25.15f, 2.5f), 5, 5);
        DoorFrameSide("Door_Right_Chemical_Frame", new Vector3(20.15f, -8, 1.5f), 3, 3);
        DoorFrameSide("Door_Right_Thermal_Frame", new Vector3(20.15f, 12, 1.5f), 3, 3);
        DoorFrameSide("Door_Left_Pneumatic_Frame", new Vector3(-20.15f, -8, 1.5f), 3, 3);
        DoorFrameSide("Door_Left_Mechanical_Frame", new Vector3(-20.15f, 12, 1.5f), 3, 3);
    }

    void DoorFrameFrontBack(string name, Vector3 c, float w, float h)
    {
        Transform p = Group(name, structure);
        Cube(name + "_LeftPost", c + new Vector3(-w / 2 - 0.18f, 0, 0), new Vector3(0.25f, 0.45f, h), blackMat, p);
        Cube(name + "_RightPost", c + new Vector3(w / 2 + 0.18f, 0, 0), new Vector3(0.25f, 0.45f, h), blackMat, p);
        Cube(name + "_Top", new Vector3(c.x, c.y, h + 0.15f), new Vector3(w + 0.6f, 0.45f, 0.25f), blackMat, p);
    }

    void DoorFrameSide(string name, Vector3 c, float wY, float h)
    {
        Transform p = Group(name, structure);
        Cube(name + "_FrontPost", c + new Vector3(0, -wY / 2 - 0.18f, 0), new Vector3(0.45f, 0.25f, h), blackMat, p);
        Cube(name + "_BackPost", c + new Vector3(0, wY / 2 + 0.18f, 0), new Vector3(0.45f, 0.25f, h), blackMat, p);
        Cube(name + "_Top", new Vector3(c.x, c.y, h + 0.15f), new Vector3(0.45f, wY + 0.6f, 0.25f), blackMat, p);
    }

    void BuildWalkwayMarkers()
    {
        Cube("Marker_Left_Side_Clear", new Vector3(-15.8f, 0, 0.015f), new Vector3(0.08f, 42, 0.03f), markerMat, markers);
        Cube("Marker_Right_Side_Clear", new Vector3(15.8f, 0, 0.015f), new Vector3(0.08f, 42, 0.03f), markerMat, markers);
        Cube("Marker_Central_Left_Clear", new Vector3(-3.2f, -2, 0.015f), new Vector3(0.08f, 30, 0.03f), markerMat, markers);
        Cube("Marker_Central_Right_Clear", new Vector3(3.2f, -2, 0.015f), new Vector3(0.08f, 30, 0.03f), markerMat, markers);
    }

    void BuildConveyors()
    {
        Conveyor("Main_Central_Blue_Conveyor", new Vector3(0, -2, 0.45f), new Vector3(2.4f, 28, 0.35f), Vector3.zero, blueMat);
        float[] ys = { -10, -2, 6 };
        for (int i = 0; i < ys.Length; i++)
        {
            Conveyor("Aux_Blue_Conveyor_L" + (i + 1), new Vector3(-3.9f, ys[i], 0.45f), new Vector3(5.2f, 1.15f, 0.30f), Vector3.zero, blueMat);
            Conveyor("Aux_Blue_Conveyor_R" + (i + 1), new Vector3(3.9f, ys[i], 0.45f), new Vector3(5.2f, 1.15f, 0.30f), Vector3.zero, blueMat);
        }

        Transform insp = Group("Inspection_Splitter_Station", conveyors);
        Cube("Inspection_Base", new Vector3(0, 12.5f, 0.75f), new Vector3(4.8f, 3.0f, 0.9f), inspectionMat, insp);
        Cube("Inspection_Reader_Top", new Vector3(0, 12.5f, 1.85f), new Vector3(3.8f, 1.1f, 1.0f), inspectionMat, insp);
        Cube("Inspection_Blue_Screen", new Vector3(0, 11.85f, 1.9f), new Vector3(1.2f, 0.08f, 0.7f), blueMat, insp);
        CubeRot("Splitter_Arm_Left", new Vector3(-1.2f, 13.55f, 1.25f), new Vector3(2.2f, 0.18f, 0.18f), new Vector3(0, 0, -25), blackMat, insp);
        CubeRot("Splitter_Arm_Right", new Vector3(1.2f, 13.55f, 1.25f), new Vector3(2.2f, 0.18f, 0.18f), new Vector3(0, 0, 25), blackMat, insp);

        Conveyor("Reject_Green_Conveyor_Left", new Vector3(-6, 16, 0.45f), new Vector3(10, 2.2f, 0.35f), new Vector3(0, 0, 30), greenMat);
        Conveyor("Approved_Green_Conveyor_Right", new Vector3(6, 16, 0.45f), new Vector3(10, 2.2f, 0.35f), new Vector3(0, 0, -30), greenMat);

        float[] teddyY = { -14, -9, -4, 1, 6, 10 };
        for (int i = 0; i < teddyY.Length; i++) Teddy("Teddy_In_Line_" + (i + 1), new Vector3(0, teddyY[i], 0.95f), conveyors);
    }

    void Conveyor(string name, Vector3 pos, Vector3 size, Vector3 euler, Material beltMat)
    {
        Transform p = Group(name, conveyors);
        CubeRot(name + "_Base", pos + new Vector3(0, 0, -0.12f), new Vector3(size.x + 0.35f, size.y + 0.35f, 0.2f), euler, blackMat, p);
        CubeRot(name + "_Belt", pos, size, euler, beltMat, p);
    }

    void BuildMachines()
    {
        Machine("Sewing_Machine_L1", new Vector3(-7.5f, -10, 1), true);
        Machine("Sewing_Machine_L2", new Vector3(-7.5f, -2, 1), true);
        Machine("Sewing_Machine_L3", new Vector3(-7.5f, 6, 1), true);
        Machine("Sewing_Machine_R1", new Vector3(7.5f, -10, 1), false);
        Machine("Sewing_Machine_R2", new Vector3(7.5f, -2, 1), false);
        Machine("Sewing_Machine_R3", new Vector3(7.5f, 6, 1), false);
    }

    void Machine(string name, Vector3 p, bool left)
    {
        Transform g = Group(name, machines);
        float dir = left ? 1 : -1;
        Cube("Table", p + new Vector3(0, 0, -0.35f), new Vector3(3.0f, 1.8f, 0.35f), blackMat, g);
        Cube("Body", p + new Vector3(0, 0, 0.35f), new Vector3(2.1f, 1.2f, 1.1f), machineMat, g);
        Cube("Arm_To_Conveyor", p + new Vector3(dir * 1.15f, 0, 0.95f), new Vector3(1.3f, 0.25f, 0.25f), machineMat, g);
        Cube("Needle", p + new Vector3(dir * 1.8f, 0, 0.45f), new Vector3(0.12f, 0.12f, 0.85f), blackMat, g);
        Cube("Blue_Control", p + new Vector3(-dir * 1.1f, -0.72f, 0.45f), new Vector3(0.6f, 0.08f, 0.45f), blueMat, g);
    }

    void BuildPlatformRing()
    {
        Cube("Platform_Front", new Vector3(0, -22.2f, 3.2f), new Vector3(34, 1.8f, 0.25f), platformMat, platform);
        Cube("Platform_Back", new Vector3(0, 22.2f, 3.2f), new Vector3(34, 1.8f, 0.25f), platformMat, platform);
        Cube("Platform_Left", new Vector3(-17.2f, 0, 3.2f), new Vector3(1.8f, 42, 0.25f), platformMat, platform);
        Cube("Platform_Right", new Vector3(17.2f, 0, 3.2f), new Vector3(1.8f, 42, 0.25f), platformMat, platform);

        float railZ = 3.95f;
        Cube("Rail_Front_Inner", new Vector3(0, -21.25f, railZ), new Vector3(34, 0.12f, 0.18f), railingMat, platform);
        Cube("Rail_Back_Inner", new Vector3(0, 21.25f, railZ), new Vector3(34, 0.12f, 0.18f), railingMat, platform);
        Cube("Rail_Left_Inner", new Vector3(-16.25f, 0, railZ), new Vector3(0.12f, 42, 0.18f), railingMat, platform);
        Cube("Rail_Right_Inner", new Vector3(16.25f, 0, railZ), new Vector3(0.12f, 42, 0.18f), railingMat, platform);

        float[] supportY = { -20, -12, -4, 4, 12, 20 };
        foreach (float y in supportY)
        {
            Cube("Support_Left", new Vector3(-17.2f, y, 1.6f), new Vector3(0.25f, 0.25f, 3.2f), blackMat, platform);
            Cube("Support_Right", new Vector3(17.2f, y, 1.6f), new Vector3(0.25f, 0.25f, 3.2f), blackMat, platform);
        }
    }

    void BuildStairs()
    {
        Transform g = Group("Stair_Right_Big_Industrial", stairs);
        int steps = 16;
        float x = 17.2f;
        float startY = -21.8f;
        float totalLength = 8.0f;
        float totalHeight = 3.2f;
        float stepDepth = totalLength / steps;
        float stepHeight = totalHeight / steps;

        for (int i = 0; i < steps; i++)
        {
            float y = startY + i * stepDepth;
            float z = (i + 0.5f) * stepHeight;
            Cube("Step_" + (i + 1).ToString("00"), new Vector3(x, y, z), new Vector3(2.0f, stepDepth * 0.95f, stepHeight), stairMat, g);
        }

        Cube("Top_Landing", new Vector3(x, -13.4f, 3.2f), new Vector3(2.2f, 1.6f, 0.25f), platformMat, g);
        Cube("Left_Rail", new Vector3(x - 1.15f, -17.6f, 2.1f), new Vector3(0.12f, 8.5f, 0.18f), railingMat, g);
        Cube("Right_Rail", new Vector3(x + 1.15f, -17.6f, 2.1f), new Vector3(0.12f, 8.5f, 0.18f), railingMat, g);
    }

    void BuildElectricalPanels()
    {
        Panel("Main_Electrical_Panel", new Vector3(-19.55f, -20, 1.7f));
        Panel("Panel_Left_02", new Vector3(-19.55f, 0, 1.5f));
        Panel("Panel_Left_03", new Vector3(-19.55f, 16, 1.5f));
        Panel("Panel_Right_01", new Vector3(19.55f, 0, 1.5f));
        Panel("Panel_Right_02", new Vector3(19.55f, 16, 1.5f));
        Empty("Interaction_Main_Electrical_Panel", new Vector3(-19.9f, -20, 1.5f), electrical);
    }

    void Panel(string name, Vector3 p)
    {
        Transform g = Group(name, electrical);
        Cube("Body", p, new Vector3(0.25f, 1.2f, 2.4f), panelMat, g);
        Cube("Handle", p + new Vector3(0, -0.45f, 0.2f), new Vector3(0.08f, 0.08f, 0.5f), blackMat, g);
        Cube("Blue_Light", p + new Vector3(0, 0.25f, 0.8f), new Vector3(0.08f, 0.18f, 0.18f), blueMat, g);
    }

    void BuildBoxesAndTeddies()
    {
        OpenBox("Reject_Box_Left", new Vector3(-12, 20, 0.6f));
        OpenBox("Approved_Box_Right", new Vector3(12, 20, 0.6f));
        Teddy("Reject_Teddy_01", new Vector3(-12.3f, 20, 1.35f), props);
        Teddy("Reject_Teddy_02", new Vector3(-11.5f, 19.5f, 1.35f), props);
        Teddy("Approved_Teddy_01", new Vector3(12.3f, 20, 1.35f), props);
        Teddy("Approved_Teddy_02", new Vector3(11.5f, 19.5f, 1.35f), props);
        Cube("Fabric_And_Parts_Left", new Vector3(-13, -20, 0.55f), new Vector3(4, 2, 1), boxMat, props);
        Cube("Fabric_And_Parts_Right", new Vector3(13, -20, 0.55f), new Vector3(4, 2, 1), boxMat, props);
    }

    void OpenBox(string name, Vector3 c)
    {
        Transform g = Group(name, props);
        Vector3 s = new Vector3(3.4f, 3.2f, 1.2f);
        float w = 0.18f;
        Cube("Bottom", c + new Vector3(0, 0, -s.z / 2), new Vector3(s.x, s.y, w), boxMat, g);
        Cube("LeftWall", c + new Vector3(-s.x / 2, 0, 0), new Vector3(w, s.y, s.z), boxMat, g);
        Cube("RightWall", c + new Vector3(s.x / 2, 0, 0), new Vector3(w, s.y, s.z), boxMat, g);
        Cube("FrontWall", c + new Vector3(0, -s.y / 2, 0), new Vector3(s.x, w, s.z), boxMat, g);
        Cube("BackWall", c + new Vector3(0, s.y / 2, 0), new Vector3(s.x, w, s.z), boxMat, g);
    }

    void Teddy(string name, Vector3 c, Transform parent)
    {
        Transform g = Group(name, parent);
        Sphere("Body", c, new Vector3(0.45f, 0.35f, 0.55f), teddyMat, g);
        Sphere("Head", c + new Vector3(0, 0, 0.55f), new Vector3(0.35f, 0.30f, 0.35f), teddyMat, g);
        Sphere("Ear_L", c + new Vector3(-0.22f, 0, 0.82f), new Vector3(0.16f, 0.12f, 0.16f), teddyMat, g);
        Sphere("Ear_R", c + new Vector3(0.22f, 0, 0.82f), new Vector3(0.16f, 0.12f, 0.16f), teddyMat, g);
    }

    void BuildSnapPoints()
    {
        Empty("Snap_Front", new Vector3(0, -25, 1.5f), snaps);
        Empty("Snap_Back", new Vector3(0, 25, 1.5f), snaps);
        Empty("Snap_Left", new Vector3(-20, 0, 1.5f), snaps);
        Empty("Snap_Right", new Vector3(20, 0, 1.5f), snaps);
        Empty("Snap_Right_Chemical", new Vector3(20, -8, 1.5f), snaps);
        Empty("Snap_Right_Thermal", new Vector3(20, 12, 1.5f), snaps);
        Empty("Snap_Left_Pneumatic", new Vector3(-20, -8, 1.5f), snaps);
        Empty("Snap_Left_Mechanical", new Vector3(-20, 12, 1.5f), snaps);
        Empty("Snap_Platform_Stair_Top", new Vector3(17.2f, -13.4f, 3.2f), snaps);
    }
}
