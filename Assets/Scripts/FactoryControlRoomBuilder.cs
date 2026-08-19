using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Cria do zero uma sala de controle elevada para a fábrica e adiciona uma porta interativa.
/// Coloque este script no objeto raiz da fábrica, por exemplo: "Fabrica".
/// Depois use o menu de contexto do componente: Build/Rebuild Control Room From Scratch.
///
/// Pensado para FBX exportado do Blender para Unity, onde:
/// Blender X -> Unity -X em alguns imports
/// Blender Y -> Unity -Z
/// Blender Z -> Unity Y
/// Por isso os valores padrão já estão em coordenadas Unity, não Blender.
/// </summary>
public class FactoryControlRoomBuilder : MonoBehaviour
{
    public enum DoorSide
    {
        AutoNearStairs,
        LeftSide,
        RightSide
    }

    [Header("Player / Interação")]
    public Transform playerTransform;
    public string playerNameFallback = "Player_FPS";
    public KeyCode interactKey = KeyCode.F;
    public float interactionDistance = 3.0f;
    public string openPrompt = "Pressione F para abrir";
    public string closePrompt = "Pressione F para fechar";

    [Header("Sala de controle - coordenadas Unity")]
    public Vector3 roomCenter = new Vector3(0f, 6.0f, -16.5f);
    public Vector3 roomSize = new Vector3(8f, 3f, 5f);
    public float floorY = 4.55f;
    public float wallThickness = 0.18f;
    public float floorThickness = 0.12f;
    public float ceilingThickness = 0.12f;

    [Header("Porta")]
    public DoorSide doorSide = DoorSide.AutoNearStairs;
    public float doorWidth = 1.05f;
    public float doorHeight = 2.1f;
    public float doorThickness = 0.08f;
    [Tooltip("Posição da porta no eixo Z. 0 = centro da sala, valores positivos aproximam da frente da sala.")]
    public float doorLocalZ = 1.55f;
    public float openAngle = 95f;
    public bool invertOpeningDirection = false;
    public float animationDuration = 0.65f;

    [Header("Janela frontal")]
    public int frontWindowCount = 3;
    public Vector2 frontWindowSize = new Vector2(1.95f, 1.15f); // X largura, Y altura
    public float frontWindowCenterY = 6f;

    [Header("Construção extra")]
    public bool createPlatform = true;
    public bool createSupports = true;
    public bool createInteriorFurniture = true;
    public bool createColliders = true;
    public bool disableOldControlRoomIfFound = true;

    [Header("Materiais gerados")]
    public Material concreteMaterial;
    public Material glassMaterial;
    public Material darkMetalMaterial;
    public Material lightMetalMaterial;
    public Material yellowSafetyMaterial;
    public Material darkPlasticMaterial;
    public Material monitorScreenMaterial;

    private const string ContainerName = "Generated_Control_Room_From_Scratch";
    private Transform generatedContainer;
    private Transform doorPivot;
    private Transform doorPanel;
    private Quaternion closedRotation;
    private Quaternion openRotation;
    private bool isOpen;
    private bool isAnimating;
    private bool playerInRange;

    private void Awake()
    {
        TryFindPlayer();
        FindGeneratedReferences();
    }

    private void Start()
    {
        FindGeneratedReferences();
    }

    private void Update()
    {
        if (playerTransform == null)
            TryFindPlayer();

        if (doorPivot == null || doorPanel == null || playerTransform == null)
            return;

        float distance = Vector3.Distance(playerTransform.position, GetDoorInteractionPoint());
        playerInRange = distance <= interactionDistance;

        if (playerInRange && Input.GetKeyDown(interactKey) && !isAnimating)
        {
            ToggleDoor();
        }
    }

    private void OnGUI()
    {
        if (!playerInRange || doorPivot == null || isAnimating)
            return;

        string text = isOpen ? closePrompt : openPrompt;

        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.alignment = TextAnchor.MiddleCenter;
        style.fontSize = 24;
        style.normal.textColor = Color.white;

        Rect shadowRect = new Rect(0, Screen.height - 135, Screen.width, 40);
        GUIStyle shadowStyle = new GUIStyle(style);
        shadowStyle.normal.textColor = new Color(0f, 0f, 0f, 0.75f);
        GUI.Label(new Rect(shadowRect.x + 2, shadowRect.y + 2, shadowRect.width, shadowRect.height), text, shadowStyle);
        GUI.Label(shadowRect, text, style);
    }

    [ContextMenu("Build/Rebuild Control Room From Scratch")]
    public void BuildOrRebuildControlRoom()
    {
        EnsureMaterials();
        DeleteOldGeneratedRoom();

        if (disableOldControlRoomIfFound)
            DisableOldControlRoomObjects();

        GameObject container = new GameObject(ContainerName);
        container.transform.SetParent(transform, true);
        container.transform.position = Vector3.zero;
        container.transform.rotation = Quaternion.identity;
        container.transform.localScale = Vector3.one;
        generatedContainer = container.transform;

        BuildMainRoom();
        BuildDoorAndSideWall();

        if (createPlatform)
            BuildPlatformAndRails();

        if (createSupports)
            BuildSupports();

        if (createInteriorFurniture)
            BuildInteriorFurniture();

        FindGeneratedReferences();
        SetClosedStateImmediate();

#if UNITY_EDITOR
        EditorUtility.SetDirty(this);
        EditorUtility.SetDirty(gameObject);
#endif
    }

    [ContextMenu("Open Control Room Door Now")]
    public void OpenDoorNow()
    {
        FindGeneratedReferences();
        if (doorPivot == null) return;
        StopAllCoroutines();
        isAnimating = false;
        isOpen = true;
        doorPivot.localRotation = openRotation;
        SetDoorColliderEnabled(false);
    }

    [ContextMenu("Close Control Room Door Now")]
    public void CloseDoorNow()
    {
        FindGeneratedReferences();
        if (doorPivot == null) return;
        StopAllCoroutines();
        isAnimating = false;
        isOpen = false;
        doorPivot.localRotation = closedRotation;
        SetDoorColliderEnabled(true);
    }

    [ContextMenu("Toggle Control Room Door Now")]
    public void ToggleDoorNow()
    {
        FindGeneratedReferences();
        ToggleDoor();
    }

    private void BuildMainRoom()
    {
        float leftX = roomCenter.x - roomSize.x / 2f;
        float rightX = roomCenter.x + roomSize.x / 2f;
        float frontZ = roomCenter.z + roomSize.z / 2f;
        float backZ = roomCenter.z - roomSize.z / 2f;
        float bottomY = floorY;
        float topY = floorY + roomSize.y;
        float centerY = bottomY + roomSize.y / 2f;

        // Piso e teto
        CreateCube("Control_Room_Floor_Generated", new Vector3(roomCenter.x, floorY, roomCenter.z), new Vector3(roomSize.x, floorThickness, roomSize.z), concreteMaterial, true);
        CreateCube("Control_Room_Ceiling_Generated", new Vector3(roomCenter.x, topY, roomCenter.z), new Vector3(roomSize.x, ceilingThickness, roomSize.z), concreteMaterial, true);

        // Parede traseira simples
        CreateCube("Control_Room_Back_Wall_Generated", new Vector3(roomCenter.x, centerY, backZ), new Vector3(roomSize.x, roomSize.y, wallThickness), concreteMaterial, true);

        // Parede frontal com janelas: cria moldura em vez de bloco único.
        BuildFrontWallWithWindows(frontZ, bottomY, topY);
    }

    private void BuildFrontWallWithWindows(float frontZ, float bottomY, float topY)
    {
        float windowW = frontWindowSize.x;
        float windowH = frontWindowSize.y;
        float windowBottom = frontWindowCenterY - windowH / 2f;
        float windowTop = frontWindowCenterY + windowH / 2f;

        float bottomH = Mathf.Max(0.1f, windowBottom - bottomY);
        float topH = Mathf.Max(0.1f, topY - windowTop);

        // Faixa inferior e superior da parede frontal.
        CreateCube("Control_Room_Front_Wall_Bottom_Generated", new Vector3(roomCenter.x, bottomY + bottomH / 2f, frontZ), new Vector3(roomSize.x, bottomH, wallThickness), concreteMaterial, true);
        CreateCube("Control_Room_Front_Wall_Top_Generated", new Vector3(roomCenter.x, windowTop + topH / 2f, frontZ), new Vector3(roomSize.x, topH, wallThickness), concreteMaterial, true);

        // Colunas/molduras verticais entre as janelas.
        float totalWindowWidth = frontWindowCount * windowW;
        float spacing = 0.28f;
        float totalOpenWidth = totalWindowWidth + spacing * (frontWindowCount - 1);
        float startX = roomCenter.x - totalOpenWidth / 2f + windowW / 2f;

        // Laterais externas
        float leftEdge = roomCenter.x - roomSize.x / 2f;
        float rightEdge = roomCenter.x + roomSize.x / 2f;
        float firstWindowLeft = startX - windowW / 2f;
        float lastWindowRight = startX + (frontWindowCount - 1) * (windowW + spacing) + windowW / 2f;
        float sideLeftW = Mathf.Max(0.1f, firstWindowLeft - leftEdge);
        float sideRightW = Mathf.Max(0.1f, rightEdge - lastWindowRight);
        float midY = (windowBottom + windowTop) / 2f;

        CreateCube("Control_Room_Front_Wall_Left_Frame_Generated", new Vector3(leftEdge + sideLeftW / 2f, midY, frontZ), new Vector3(sideLeftW, windowH, wallThickness), concreteMaterial, true);
        CreateCube("Control_Room_Front_Wall_Right_Frame_Generated", new Vector3(rightEdge - sideRightW / 2f, midY, frontZ), new Vector3(sideRightW, windowH, wallThickness), concreteMaterial, true);

        for (int i = 0; i < frontWindowCount; i++)
        {
            float x = startX + i * (windowW + spacing);
            CreateCube("Control_Room_Window_" + (i + 1) + "_Generated", new Vector3(x, frontWindowCenterY, frontZ + 0.01f), new Vector3(windowW, windowH, 0.035f), glassMaterial, false);

            // Moldura fina em volta de cada vidro.
            CreateCube("Control_Room_Window_" + (i + 1) + "_Top_Frame", new Vector3(x, windowTop + 0.04f, frontZ + 0.025f), new Vector3(windowW + 0.1f, 0.08f, 0.08f), darkMetalMaterial, false);
            CreateCube("Control_Room_Window_" + (i + 1) + "_Bottom_Frame", new Vector3(x, windowBottom - 0.04f, frontZ + 0.025f), new Vector3(windowW + 0.1f, 0.08f, 0.08f), darkMetalMaterial, false);
            CreateCube("Control_Room_Window_" + (i + 1) + "_Left_Frame", new Vector3(x - windowW / 2f - 0.04f, frontWindowCenterY, frontZ + 0.025f), new Vector3(0.08f, windowH + 0.1f, 0.08f), darkMetalMaterial, false);
            CreateCube("Control_Room_Window_" + (i + 1) + "_Right_Frame", new Vector3(x + windowW / 2f + 0.04f, frontWindowCenterY, frontZ + 0.025f), new Vector3(0.08f, windowH + 0.1f, 0.08f), darkMetalMaterial, false);
        }
    }

    private void BuildDoorAndSideWall()
    {
        int sideSign = ResolveDoorSideSign(); // -1 esquerda, +1 direita

        float sideX = roomCenter.x + sideSign * roomSize.x / 2f;
        float frontZ = roomCenter.z + roomSize.z / 2f;
        float backZ = roomCenter.z - roomSize.z / 2f;
        float bottomY = floorY;
        float topY = floorY + roomSize.y;
        float centerY = bottomY + roomSize.y / 2f;

        float doorCenterZ = Mathf.Clamp(roomCenter.z + doorLocalZ, backZ + doorWidth * 0.75f, frontZ - doorWidth * 0.75f);
        float doorBottom = bottomY;
        float doorTop = bottomY + doorHeight;

        // Parede lateral oposta, inteira.
        float oppositeX = roomCenter.x - sideSign * roomSize.x / 2f;
        CreateCube("Control_Room_Opposite_Side_Wall_Generated", new Vector3(oppositeX, centerY, roomCenter.z), new Vector3(wallThickness, roomSize.y, roomSize.z), concreteMaterial, true);

        // Parede lateral da porta, em partes.
        float frontSegmentLength = Mathf.Max(0.05f, frontZ - (doorCenterZ + doorWidth / 2f));
        float backSegmentLength = Mathf.Max(0.05f, (doorCenterZ - doorWidth / 2f) - backZ);

        if (frontSegmentLength > 0.08f)
            CreateCube("Control_Room_Door_Side_Wall_Front_Generated", new Vector3(sideX, centerY, doorCenterZ + doorWidth / 2f + frontSegmentLength / 2f), new Vector3(wallThickness, roomSize.y, frontSegmentLength), concreteMaterial, true);

        if (backSegmentLength > 0.08f)
            CreateCube("Control_Room_Door_Side_Wall_Back_Generated", new Vector3(sideX, centerY, backZ + backSegmentLength / 2f), new Vector3(wallThickness, roomSize.y, backSegmentLength), concreteMaterial, true);

        float aboveDoorHeight = Mathf.Max(0.05f, topY - doorTop);
        CreateCube("Control_Room_Door_Side_Wall_Top_Generated", new Vector3(sideX, doorTop + aboveDoorHeight / 2f, doorCenterZ), new Vector3(wallThickness, aboveDoorHeight, doorWidth), concreteMaterial, true);

        // Moldura da porta.
        float frameX = sideX;
        CreateCube("Control_Room_Door_Frame_Top", new Vector3(frameX, doorTop + 0.055f, doorCenterZ), new Vector3(wallThickness + 0.08f, 0.11f, doorWidth + 0.16f), darkMetalMaterial, false);
        CreateCube("Control_Room_Door_Frame_A", new Vector3(frameX, doorBottom + doorHeight / 2f, doorCenterZ - doorWidth / 2f - 0.055f), new Vector3(wallThickness + 0.08f, doorHeight, 0.11f), darkMetalMaterial, false);
        CreateCube("Control_Room_Door_Frame_B", new Vector3(frameX, doorBottom + doorHeight / 2f, doorCenterZ + doorWidth / 2f + 0.055f), new Vector3(wallThickness + 0.08f, doorHeight, 0.11f), darkMetalMaterial, false);

        // Pivô falso no eixo lateral da porta.
        // A porta fica na parede lateral; largura da porta corre no eixo Z.
        float hingeZ = doorCenterZ - doorWidth / 2f;
        Vector3 hingePos = new Vector3(sideX, doorBottom + doorHeight / 2f, hingeZ);
        GameObject pivot = new GameObject("Control_Room_Door_Pivot_Generated");
        pivot.transform.SetParent(generatedContainer, true);
        pivot.transform.position = hingePos;
        pivot.transform.rotation = Quaternion.identity;
        pivot.transform.localScale = Vector3.one;
        doorPivot = pivot.transform;

        GameObject panel = CreateCube("Control_Room_Door_Generated", new Vector3(sideX + sideSign * 0.01f, doorBottom + doorHeight / 2f, doorCenterZ), new Vector3(doorThickness, doorHeight, doorWidth), darkMetalMaterial, true);
        panel.isStatic = false;
        panel.transform.SetParent(doorPivot, true);
        doorPanel = panel.transform;

        // Maçaneta.
        GameObject handle = CreateCube("Control_Room_Door_Handle_Generated", new Vector3(sideX + sideSign * 0.07f, doorBottom + 1.05f, doorCenterZ + doorWidth * 0.28f), new Vector3(0.08f, 0.08f, 0.16f), lightMetalMaterial, false);
        handle.isStatic = false;
        handle.transform.SetParent(doorPanel, true);

        closedRotation = doorPivot.localRotation;
        float angle = openAngle * sideSign;
        if (invertOpeningDirection)
            angle *= -1f;
        openRotation = Quaternion.Euler(0f, angle, 0f) * closedRotation;
    }

    private void BuildPlatformAndRails()
    {
        int sideSign = ResolveDoorSideSign();
        float sideX = roomCenter.x + sideSign * roomSize.x / 2f;
        float frontZ = roomCenter.z + roomSize.z / 2f;
        float backZ = roomCenter.z - roomSize.z / 2f;
        float doorCenterZ = Mathf.Clamp(roomCenter.z + doorLocalZ, backZ + doorWidth * 0.75f, frontZ - doorWidth * 0.75f);
        float platformY = floorY - 0.08f;
        float platformCenterX = sideX + sideSign * 1.15f;

        CreateCube("Control_Room_Platform_Generated", new Vector3(platformCenterX, platformY, doorCenterZ), new Vector3(2.4f, 0.16f, 2.7f), lightMetalMaterial, true);

        // Corrimãos simples. Deixa uma abertura na frente da porta.
        float railY = floorY + 0.55f;
        float railHeight = 1.1f;
        CreateCube("Control_Room_Platform_Rail_Outer", new Vector3(platformCenterX + sideSign * 1.15f, railY, doorCenterZ), new Vector3(0.08f, railHeight, 2.7f), yellowSafetyMaterial, true);
        CreateCube("Control_Room_Platform_Rail_Back", new Vector3(platformCenterX, railY, doorCenterZ - 1.35f), new Vector3(2.4f, railHeight, 0.08f), yellowSafetyMaterial, true);
        CreateCube("Control_Room_Platform_Rail_Front_Short", new Vector3(platformCenterX + sideSign * 0.45f, railY, doorCenterZ + 1.35f), new Vector3(1.2f, railHeight, 0.08f), yellowSafetyMaterial, true);
    }

    private void BuildSupports()
    {
        float leftX = roomCenter.x - roomSize.x / 2f + 0.35f;
        float rightX = roomCenter.x + roomSize.x / 2f - 0.35f;
        float frontZ = roomCenter.z + roomSize.z / 2f - 0.35f;
        float backZ = roomCenter.z - roomSize.z / 2f + 0.35f;
        float supportH = floorY;
        float centerY = supportH / 2f;

        Vector3 size = new Vector3(0.18f, supportH, 0.18f);
        CreateCube("Control_Room_Support_1", new Vector3(leftX, centerY, frontZ), size, darkMetalMaterial, true);
        CreateCube("Control_Room_Support_2", new Vector3(rightX, centerY, frontZ), size, darkMetalMaterial, true);
        CreateCube("Control_Room_Support_3", new Vector3(leftX, centerY, backZ), size, darkMetalMaterial, true);
        CreateCube("Control_Room_Support_4", new Vector3(rightX, centerY, backZ), size, darkMetalMaterial, true);

        CreateCube("Control_Room_Beam_Front", new Vector3(roomCenter.x, floorY - 0.12f, frontZ), new Vector3(roomSize.x, 0.16f, 0.16f), darkMetalMaterial, true);
        CreateCube("Control_Room_Beam_Back", new Vector3(roomCenter.x, floorY - 0.12f, backZ), new Vector3(roomSize.x, 0.16f, 0.16f), darkMetalMaterial, true);
        CreateCube("Control_Room_Beam_Left", new Vector3(leftX, floorY - 0.12f, roomCenter.z), new Vector3(0.16f, 0.16f, roomSize.z), darkMetalMaterial, true);
        CreateCube("Control_Room_Beam_Right", new Vector3(rightX, floorY - 0.12f, roomCenter.z), new Vector3(0.16f, 0.16f, roomSize.z), darkMetalMaterial, true);
    }

    private void BuildInteriorFurniture()
    {
        float deskY = floorY + 0.75f;
        float frontZ = roomCenter.z + roomSize.z / 2f;

        // Mesa de controle olhando para a fábrica/janelas.
        CreateCube("Control_Room_Desk_Generated", new Vector3(roomCenter.x, deskY, frontZ - 1.3f), new Vector3(2.4f, 0.12f, 0.75f), lightMetalMaterial, true);
        CreateCube("Control_Room_Desk_Leg_1", new Vector3(roomCenter.x - 1.0f, floorY + 0.35f, frontZ - 1.0f), new Vector3(0.08f, 0.7f, 0.08f), darkMetalMaterial, true);
        CreateCube("Control_Room_Desk_Leg_2", new Vector3(roomCenter.x + 1.0f, floorY + 0.35f, frontZ - 1.0f), new Vector3(0.08f, 0.7f, 0.08f), darkMetalMaterial, true);
        CreateCube("Control_Room_Desk_Leg_3", new Vector3(roomCenter.x - 1.0f, floorY + 0.35f, frontZ - 1.6f), new Vector3(0.08f, 0.7f, 0.08f), darkMetalMaterial, true);
        CreateCube("Control_Room_Desk_Leg_4", new Vector3(roomCenter.x + 1.0f, floorY + 0.35f, frontZ - 1.6f), new Vector3(0.08f, 0.7f, 0.08f), darkMetalMaterial, true);

        CreateCube("Control_Room_Monitor_1_Generated", new Vector3(roomCenter.x - 0.45f, floorY + 1.1f, frontZ - 1.0f), new Vector3(0.55f, 0.35f, 0.06f), monitorScreenMaterial, false);
        CreateCube("Control_Room_Monitor_2_Generated", new Vector3(roomCenter.x + 0.25f, floorY + 1.1f, frontZ - 1.0f), new Vector3(0.55f, 0.35f, 0.06f), monitorScreenMaterial, false);

        // Cadeira simples.
        CreateCube("Control_Room_Chair_Seat", new Vector3(roomCenter.x + 0.6f, floorY + 0.45f, frontZ - 2.05f), new Vector3(0.6f, 0.12f, 0.6f), darkPlasticMaterial, true);
        CreateCube("Control_Room_Chair_Back", new Vector3(roomCenter.x + 0.6f, floorY + 0.9f, frontZ - 2.33f), new Vector3(0.6f, 0.8f, 0.1f), darkPlasticMaterial, true);
        CreateCube("Control_Room_Chair_Leg_1", new Vector3(roomCenter.x + 0.35f, floorY + 0.22f, frontZ - 1.82f), new Vector3(0.06f, 0.44f, 0.06f), darkMetalMaterial, true);
        CreateCube("Control_Room_Chair_Leg_2", new Vector3(roomCenter.x + 0.85f, floorY + 0.22f, frontZ - 1.82f), new Vector3(0.06f, 0.44f, 0.06f), darkMetalMaterial, true);
        CreateCube("Control_Room_Chair_Leg_3", new Vector3(roomCenter.x + 0.35f, floorY + 0.22f, frontZ - 2.28f), new Vector3(0.06f, 0.44f, 0.06f), darkMetalMaterial, true);
        CreateCube("Control_Room_Chair_Leg_4", new Vector3(roomCenter.x + 0.85f, floorY + 0.22f, frontZ - 2.28f), new Vector3(0.06f, 0.44f, 0.06f), darkMetalMaterial, true);

        // Luz interna simples.
        GameObject lightObj = new GameObject("Control_Room_Interior_Light_Generated");
        lightObj.transform.SetParent(generatedContainer, true);
        lightObj.transform.position = new Vector3(roomCenter.x, floorY + roomSize.y - 0.35f, roomCenter.z);
        Light l = lightObj.AddComponent<Light>();
        l.type = LightType.Point;
        l.color = new Color(0.86f, 0.93f, 1f);
        l.intensity = 1.4f;
        l.range = 6f;
    }

    private GameObject CreateCube(string objectName, Vector3 position, Vector3 scale, Material material, bool collider)
    {
        GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        obj.name = objectName;
        obj.transform.SetParent(generatedContainer, true);
        obj.transform.position = position;
        obj.transform.rotation = Quaternion.identity;
        obj.transform.localScale = scale;
        obj.isStatic = objectName.Contains("Door") ? false : true;

        Renderer r = obj.GetComponent<Renderer>();
        if (r != null && material != null)
            r.sharedMaterial = material;

        Collider c = obj.GetComponent<Collider>();
        if (!createColliders || !collider)
        {
            if (c != null)
            {
#if UNITY_EDITOR
                if (!Application.isPlaying) DestroyImmediate(c);
                else Destroy(c);
#else
                Destroy(c);
#endif
            }
        }

        return obj;
    }

    private int ResolveDoorSideSign()
    {
        if (doorSide == DoorSide.LeftSide)
            return -1;
        if (doorSide == DoorSide.RightSide)
            return 1;

        Transform stairs = FindFirstTransformByNameContains("Control_Stairs");
        if (stairs != null)
        {
            Bounds b;
            if (TryGetBounds(stairs.gameObject, out b))
                return b.center.x >= roomCenter.x ? 1 : -1;

            return stairs.position.x >= roomCenter.x ? 1 : -1;
        }

        // Pela sua cena, normalmente a escada fica no lado direito visual da sala.
        return 1;
    }

    private Vector3 GetDoorInteractionPoint()
    {
        if (doorPanel != null)
            return doorPanel.position;
        if (doorPivot != null)
            return doorPivot.position;
        return roomCenter;
    }

    private void ToggleDoor()
    {
        if (doorPivot == null)
            FindGeneratedReferences();
        if (doorPivot == null)
            return;

        StopAllCoroutines();
        StartCoroutine(AnimateDoor(isOpen ? closedRotation : openRotation, !isOpen));
    }

    private IEnumerator AnimateDoor(Quaternion targetRotation, bool opening)
    {
        isAnimating = true;
        if (!opening)
            SetDoorColliderEnabled(true);

        Quaternion startRotation = doorPivot.localRotation;
        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime / Mathf.Max(0.01f, animationDuration);
            float smoothT = Mathf.SmoothStep(0f, 1f, t);
            doorPivot.localRotation = Quaternion.Slerp(startRotation, targetRotation, smoothT);
            yield return null;
        }

        doorPivot.localRotation = targetRotation;
        isOpen = opening;

        if (isOpen)
            SetDoorColliderEnabled(false);

        isAnimating = false;
    }

    private void SetClosedStateImmediate()
    {
        if (doorPivot == null)
            return;

        doorPivot.localRotation = closedRotation;
        isOpen = false;
        isAnimating = false;
        SetDoorColliderEnabled(true);
    }

    private void SetDoorColliderEnabled(bool enabled)
    {
        if (doorPanel == null)
            return;

        Collider[] colliders = doorPanel.GetComponentsInChildren<Collider>(true);
        foreach (Collider c in colliders)
            c.enabled = enabled;
    }

    private void FindGeneratedReferences()
    {
        Transform container = FindDirectOrDeepChild(transform, ContainerName);
        if (container != null)
            generatedContainer = container;

        if (doorPivot == null)
        {
            Transform p = FindDirectOrDeepChild(transform, "Control_Room_Door_Pivot_Generated");
            if (p != null)
                doorPivot = p;
        }

        if (doorPanel == null)
        {
            Transform d = FindDirectOrDeepChild(transform, "Control_Room_Door_Generated");
            if (d != null)
                doorPanel = d;
        }

        if (doorPivot != null)
        {
            closedRotation = Quaternion.identity;
            int sideSign = ResolveDoorSideSign();
            float angle = openAngle * sideSign;
            if (invertOpeningDirection)
                angle *= -1f;
            openRotation = Quaternion.Euler(0f, angle, 0f);
        }
    }

    private void DeleteOldGeneratedRoom()
    {
        List<GameObject> toDelete = new List<GameObject>();
        Transform[] all = GetComponentsInChildren<Transform>(true);
        foreach (Transform t in all)
        {
            if (t != transform && t.name == ContainerName)
                toDelete.Add(t.gameObject);
        }

        foreach (GameObject obj in toDelete)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying) DestroyImmediate(obj);
            else Destroy(obj);
#else
            Destroy(obj);
#endif
        }

        generatedContainer = null;
        doorPivot = null;
        doorPanel = null;
    }

    private void DisableOldControlRoomObjects()
    {
        Transform[] all = GetComponentsInChildren<Transform>(true);
        foreach (Transform t in all)
        {
            if (t == transform)
                continue;

            if (t.name == "Control_Room" || t.name == "Control_Room_Floor")
            {
                t.gameObject.SetActive(false);
            }
        }
    }

    private void TryFindPlayer()
    {
        if (playerTransform != null)
            return;

        GameObject byName = GameObject.Find(playerNameFallback);
        if (byName != null)
        {
            playerTransform = byName.transform;
            return;
        }

        GameObject byTag = GameObject.FindGameObjectWithTag("Player");
        if (byTag != null)
            playerTransform = byTag.transform;
    }

    private Transform FindFirstTransformByNameContains(string text)
    {
        Transform[] all = GetComponentsInChildren<Transform>(true);
        foreach (Transform t in all)
        {
            if (t.name.Contains(text))
                return t;
        }
        return null;
    }

    private Transform FindDirectOrDeepChild(Transform root, string exactName)
    {
        Transform[] all = root.GetComponentsInChildren<Transform>(true);
        foreach (Transform t in all)
        {
            if (t.name == exactName)
                return t;
        }
        return null;
    }

    private bool TryGetBounds(GameObject obj, out Bounds bounds)
    {
        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>(true);
        if (renderers == null || renderers.Length == 0)
        {
            bounds = new Bounds(obj.transform.position, Vector3.zero);
            return false;
        }

        bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);
        return true;
    }

    private void EnsureMaterials()
    {
        if (concreteMaterial == null)
            concreteMaterial = CreateMaterial("Generated_ControlRoom_Concrete", new Color(0.66f, 0.70f, 0.70f), 0.55f, 0f);
        if (glassMaterial == null)
            glassMaterial = CreateGlassMaterial("Generated_ControlRoom_Glass", new Color(0.45f, 0.82f, 1f, 0.38f));
        if (darkMetalMaterial == null)
            darkMetalMaterial = CreateMaterial("Generated_ControlRoom_DarkMetal", new Color(0.05f, 0.07f, 0.08f), 0.35f, 0.25f);
        if (lightMetalMaterial == null)
            lightMetalMaterial = CreateMaterial("Generated_ControlRoom_LightMetal", new Color(0.70f, 0.74f, 0.74f), 0.25f, 0.25f);
        if (yellowSafetyMaterial == null)
            yellowSafetyMaterial = CreateMaterial("Generated_ControlRoom_SafetyYellow", new Color(1f, 0.75f, 0.02f), 0.5f, 0f);
        if (darkPlasticMaterial == null)
            darkPlasticMaterial = CreateMaterial("Generated_ControlRoom_DarkPlastic", new Color(0.015f, 0.017f, 0.02f), 0.65f, 0f);
        if (monitorScreenMaterial == null)
            monitorScreenMaterial = CreateEmissionMaterial("Generated_ControlRoom_MonitorScreen", new Color(0.05f, 0.45f, 0.75f), 1.4f);
    }

    private Material CreateMaterial(string matName, Color color, float smoothness, float metallic)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");

        Material mat = new Material(shader);
        mat.name = matName;
        mat.color = color;

        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
        if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);
        if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", metallic);

        return mat;
    }

    private Material CreateGlassMaterial(string matName, Color color)
    {
        Material mat = CreateMaterial(matName, color, 0.75f, 0f);

        // Configuração simples de transparência. Em URP isso costuma funcionar no Scene/Game View.
        if (mat.HasProperty("_Surface")) mat.SetFloat("_Surface", 1f);
        if (mat.HasProperty("_AlphaClip")) mat.SetFloat("_AlphaClip", 0f);
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
        if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);

        mat.renderQueue = 3000;
        mat.SetOverrideTag("RenderType", "Transparent");
        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        mat.EnableKeyword("_ALPHAPREMULTIPLY_ON");
        return mat;
    }

    private Material CreateEmissionMaterial(string matName, Color color, float intensity)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");

        Material mat = new Material(shader);
        mat.name = matName;
        Color emission = color * intensity;

        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
        if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
        if (mat.HasProperty("_EmissionColor")) mat.SetColor("_EmissionColor", emission);

        mat.EnableKeyword("_EMISSION");
        return mat;
    }
}
