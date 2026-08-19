using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Builds and controls an interactive door for the elevated factory control room.
/// Attach this script to the factory root object, usually "Fabrica".
/// It can rebuild the old solid Control_Room cube into wall pieces, leaving a real doorway.
/// </summary>
public class FactoryControlRoomDoorController : MonoBehaviour
{
    [Header("References")]
    public Transform playerTransform;
    public Transform factoryRoot;
    public Transform controlRoomTransform;
    public Transform stairsHint;

    [Header("Door Placement")]
    public bool rebuildSolidControlRoom = true;
    public bool createAtSideClosestToStairs = true;
    public float wallThickness = 0.12f;
    public float doorWidth = 1.15f;
    public float doorHeight = 2.2f;
    public float doorThickness = 0.08f;
    public float doorBottomOffset = 0.04f;
    public float doorZOffset = 0f;

    [Header("Interaction")]
    public KeyCode interactionKey = KeyCode.F;
    public float interactionDistance = 2.6f;
    public string openMessage = "Pressione F para abrir";
    public string closeMessage = "Pressione F para fechar";

    [Header("Animation")]
    public float openAngle = 95f;
    public bool invertOpeningDirection = false;
    public float animationDuration = 0.65f;
    public AnimationCurve animationCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Generated Names")]
    public string generatedRootName = "Control_Room_Door_System";
    public string rebuiltShellName = "Control_Room_Rebuilt_Shell";
    public string pivotName = "Control_Room_Door_Pivot";
    public string doorName = "Control_Room_Interactive_Door";

    private Transform generatedRoot;
    private Transform doorPivot;
    private Transform doorPanel;
    private BoxCollider doorCollider;
    private Bounds roomBounds;
    private bool isOpen;
    private bool isAnimating;
    private int sideSign = 1;
    private float closedYAngle;
    private float openYAngle;

    private Material concreteMaterial;
    private Material darkMetalMaterial;
    private Material lightMetalMaterial;
    private Material glassMaterial;
    private Material blackGapMaterial;

    private void Awake()
    {
        AutoFindReferences();
        BuildOrRebuild();
    }

    private void Reset()
    {
        factoryRoot = transform;
        AutoFindReferences();
    }

    private void Update()
    {
        if (!Application.isPlaying) return;
        if (playerTransform == null || doorPivot == null) return;

        if (Vector3.Distance(playerTransform.position, GetInteractionPoint()) <= interactionDistance)
        {
            if (Input.GetKeyDown(interactionKey))
            {
                ToggleDoor();
            }
        }
    }

    private void OnGUI()
    {
        if (!Application.isPlaying) return;
        if (playerTransform == null || doorPivot == null) return;

        if (Vector3.Distance(playerTransform.position, GetInteractionPoint()) <= interactionDistance && !isAnimating)
        {
            GUIStyle style = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 24,
                fontStyle = FontStyle.Bold
            };

            string message = isOpen ? closeMessage : openMessage;
            GUI.Label(new Rect(0, Screen.height - 120, Screen.width, 40), message, style);
        }
    }

    [ContextMenu("Build/Rebuild Control Room Door")]
    public void BuildOrRebuild()
    {
        AutoFindReferences();

        if (controlRoomTransform == null)
        {
            Debug.LogWarning("FactoryControlRoomDoorController: Control_Room was not found. Assign it manually or keep the original object name.");
            return;
        }

        Renderer roomRenderer = controlRoomTransform.GetComponentInChildren<Renderer>();
        if (roomRenderer == null)
        {
            Debug.LogWarning("FactoryControlRoomDoorController: Control_Room has no renderer. Cannot read its size.");
            return;
        }

        roomBounds = roomRenderer.bounds;
        CreateMaterialsFromExisting(roomRenderer);
        DestroyGeneratedRootImmediateOrRuntime();

        GameObject rootObject = new GameObject(generatedRootName);
        generatedRoot = rootObject.transform;
        generatedRoot.SetParent(factoryRoot != null ? factoryRoot : transform, true);
        generatedRoot.position = Vector3.zero;
        generatedRoot.rotation = Quaternion.identity;

        sideSign = CalculateDoorSideSign();

        if (rebuildSolidControlRoom)
        {
            DisableOldControlRoomBlock();
            BuildControlRoomShellWithDoorway();
        }
        else
        {
            CreateFakeDoorOpeningBackdrop();
        }

        BuildDoorFrameAndDoor();
        SetStaticRecursive(generatedRoot.gameObject, false);

        isOpen = false;
        isAnimating = false;

        Debug.Log("FactoryControlRoomDoorController: control room door created/rebuilt.");
    }

    [ContextMenu("Open Door Now")]
    public void OpenDoorNow()
    {
        if (doorPivot == null) BuildOrRebuild();
        SetDoorStateImmediate(true);
    }

    [ContextMenu("Close Door Now")]
    public void CloseDoorNow()
    {
        if (doorPivot == null) BuildOrRebuild();
        SetDoorStateImmediate(false);
    }

    [ContextMenu("Toggle Door Now")]
    public void ToggleDoorNow()
    {
        if (doorPivot == null) BuildOrRebuild();
        ToggleDoor();
    }

    public void ToggleDoor()
    {
        if (isAnimating || doorPivot == null) return;

        if (Application.isPlaying)
        {
            StartCoroutine(AnimateDoor(!isOpen));
        }
        else
        {
            SetDoorStateImmediate(!isOpen);
        }
    }

    private IEnumerator AnimateDoor(bool targetOpen)
    {
        isAnimating = true;

        float startAngle = doorPivot.localEulerAngles.y;
        float targetAngle = targetOpen ? openYAngle : closedYAngle;
        startAngle = NormalizeAngle(startAngle);
        targetAngle = NormalizeAngle(targetAngle);

        if (doorCollider != null)
        {
            doorCollider.enabled = true;
        }

        float elapsed = 0f;
        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / Mathf.Max(0.01f, animationDuration));
            float eased = animationCurve != null ? animationCurve.Evaluate(t) : t;
            float angle = Mathf.LerpAngle(startAngle, targetAngle, eased);
            doorPivot.localRotation = Quaternion.Euler(0f, angle, 0f);
            yield return null;
        }

        doorPivot.localRotation = Quaternion.Euler(0f, targetAngle, 0f);
        isOpen = targetOpen;
        isAnimating = false;
    }

    private void SetDoorStateImmediate(bool open)
    {
        if (doorPivot == null) return;
        doorPivot.localRotation = Quaternion.Euler(0f, open ? openYAngle : closedYAngle, 0f);
        isOpen = open;
    }

    private void AutoFindReferences()
    {
        if (factoryRoot == null)
        {
            factoryRoot = transform;
        }

        if (controlRoomTransform == null)
        {
            Transform foundRoom = FindTransformByExactOrContains(factoryRoot, "Control_Room");
            if (foundRoom != null && !foundRoom.name.Contains("Floor") && !foundRoom.name.Contains("Window") && !foundRoom.name.Contains("Door"))
            {
                controlRoomTransform = foundRoom;
            }
            else
            {
                controlRoomTransform = FindBestControlRoomTransform();
            }
        }

        if (stairsHint == null)
        {
            stairsHint = FindTransformByExactOrContains(factoryRoot, "Control_Stairs");
        }

        if (playerTransform == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) playerTransform = player.transform;

            if (playerTransform == null)
            {
                GameObject playerByName = GameObject.Find("Player_FPS");
                if (playerByName != null) playerTransform = playerByName.transform;
            }
        }
    }

    private Transform FindBestControlRoomTransform()
    {
        Transform[] all = factoryRoot != null ? factoryRoot.GetComponentsInChildren<Transform>(true) : FindObjectsOfType<Transform>(true);
        return all.FirstOrDefault(t => t.name == "Control_Room")
            ?? all.FirstOrDefault(t => t.name.Contains("Control_Room") && !t.name.Contains("Floor") && !t.name.Contains("Window") && !t.name.Contains("Door"));
    }

    private Transform FindTransformByExactOrContains(Transform root, string query)
    {
        Transform[] all = root != null ? root.GetComponentsInChildren<Transform>(true) : FindObjectsOfType<Transform>(true);
        return all.FirstOrDefault(t => t.name == query) ?? all.FirstOrDefault(t => t.name.Contains(query));
    }

    private void CreateMaterialsFromExisting(Renderer roomRenderer)
    {
        concreteMaterial = roomRenderer.sharedMaterial != null ? roomRenderer.sharedMaterial : MakeMaterial("Factory_ControlRoom_Concrete", new Color(0.72f, 0.76f, 0.75f));
        darkMetalMaterial = FindMaterialByName("Dark") ?? FindMaterialByName("Metal") ?? MakeMaterial("Factory_Dark_Metal_Runtime", new Color(0.02f, 0.025f, 0.03f));
        lightMetalMaterial = FindMaterialByName("Light") ?? MakeMaterial("Factory_Light_Metal_Runtime", new Color(0.62f, 0.66f, 0.68f));
        glassMaterial = FindMaterialByName("Glass") ?? FindMaterialByName("Vidro") ?? MakeTransparentMaterial("Factory_Glass_Runtime", new Color(0.35f, 0.78f, 1f, 0.35f));
        blackGapMaterial = MakeMaterial("Factory_Doorway_Dark_Backdrop", new Color(0.01f, 0.012f, 0.015f));
    }

    private Material FindMaterialByName(string namePart)
    {
        Renderer[] renderers = factoryRoot != null ? factoryRoot.GetComponentsInChildren<Renderer>(true) : FindObjectsOfType<Renderer>(true);
        foreach (Renderer r in renderers)
        {
            foreach (Material m in r.sharedMaterials)
            {
                if (m != null && m.name.ToLower().Contains(namePart.ToLower())) return m;
            }
        }
        return null;
    }

    private Material MakeMaterial(string materialName, Color color)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        Material mat = new Material(shader) { name = materialName };
        mat.color = color;
        return mat;
    }

    private Material MakeTransparentMaterial(string materialName, Color color)
    {
        Material mat = MakeMaterial(materialName, color);
        mat.color = color;
        return mat;
    }

    private void DestroyGeneratedRootImmediateOrRuntime()
    {
        Transform old = null;
        Transform searchRoot = factoryRoot != null ? factoryRoot : transform;
        Transform[] all = searchRoot.GetComponentsInChildren<Transform>(true);
        foreach (Transform t in all)
        {
            if (t.name == generatedRootName)
            {
                old = t;
                break;
            }
        }

        if (old == null) return;

        if (Application.isPlaying)
        {
            Destroy(old.gameObject);
        }
        else
        {
            DestroyImmediate(old.gameObject);
        }
    }

    private int CalculateDoorSideSign()
    {
        if (!createAtSideClosestToStairs || stairsHint == null)
        {
            return 1;
        }

        Renderer stairsRenderer = stairsHint.GetComponentInChildren<Renderer>();
        Vector3 stairsPosition = stairsRenderer != null ? stairsRenderer.bounds.center : stairsHint.position;
        return stairsPosition.x >= roomBounds.center.x ? 1 : -1;
    }

    private void DisableOldControlRoomBlock()
    {
        Renderer[] renderers = controlRoomTransform.GetComponentsInChildren<Renderer>(true);
        Collider[] colliders = controlRoomTransform.GetComponentsInChildren<Collider>(true);

        foreach (Renderer r in renderers)
        {
            if (r.transform == controlRoomTransform || r.name == "Control_Room")
            {
                r.enabled = false;
            }
        }

        foreach (Collider c in colliders)
        {
            if (c.transform == controlRoomTransform || c.name == "Control_Room")
            {
                c.enabled = false;
            }
        }
    }

    private void BuildControlRoomShellWithDoorway()
    {
        GameObject shell = new GameObject(rebuiltShellName);
        shell.transform.SetParent(generatedRoot, true);

        float minX = roomBounds.min.x;
        float maxX = roomBounds.max.x;
        float minY = roomBounds.min.y;
        float maxY = roomBounds.max.y;
        float minZ = roomBounds.min.z;
        float maxZ = roomBounds.max.z;

        float width = roomBounds.size.x;
        float height = roomBounds.size.y;
        float depth = roomBounds.size.z;
        float t = Mathf.Max(0.04f, wallThickness);

        float sideX = sideSign > 0 ? maxX - t * 0.5f : minX + t * 0.5f;
        float oppositeSideX = sideSign > 0 ? minX + t * 0.5f : maxX - t * 0.5f;

        float doorCenterZ = GetDoorCenterZ();
        float doorBottomY = minY + doorBottomOffset;
        float doorTopY = Mathf.Min(doorBottomY + doorHeight, maxY - 0.25f);
        float actualDoorHeight = doorTopY - doorBottomY;
        float halfDoorWidth = doorWidth * 0.5f;

        // Opposite side wall.
        CreateCube("Control_Room_Wall_Opposite", new Vector3(oppositeSideX, roomBounds.center.y, roomBounds.center.z), new Vector3(t, height, depth), concreteMaterial, shell.transform, true);

        // Door side wall split into pieces.
        float beforeMinZ = minZ;
        float beforeMaxZ = doorCenterZ - halfDoorWidth;
        float afterMinZ = doorCenterZ + halfDoorWidth;
        float afterMaxZ = maxZ;

        if (beforeMaxZ - beforeMinZ > 0.05f)
        {
            CreateCube("Control_Room_DoorSide_Wall_A", new Vector3(sideX, roomBounds.center.y, (beforeMinZ + beforeMaxZ) * 0.5f), new Vector3(t, height, beforeMaxZ - beforeMinZ), concreteMaterial, shell.transform, true);
        }

        if (afterMaxZ - afterMinZ > 0.05f)
        {
            CreateCube("Control_Room_DoorSide_Wall_B", new Vector3(sideX, roomBounds.center.y, (afterMinZ + afterMaxZ) * 0.5f), new Vector3(t, height, afterMaxZ - afterMinZ), concreteMaterial, shell.transform, true);
        }

        if (maxY - doorTopY > 0.05f)
        {
            CreateCube("Control_Room_DoorSide_Wall_Top", new Vector3(sideX, (doorTopY + maxY) * 0.5f, doorCenterZ), new Vector3(t, maxY - doorTopY, doorWidth), concreteMaterial, shell.transform, true);
        }

        // Depth walls. One of these probably has the glass windows.
        BuildDepthWallsWithPossibleWindowOpenings(shell.transform, minX, maxX, minY, maxY, minZ, maxZ, t, width, height);

        // Ceiling.
        CreateCube("Control_Room_Ceiling", new Vector3(roomBounds.center.x, maxY - t * 0.5f, roomBounds.center.z), new Vector3(width, t, depth), concreteMaterial, shell.transform, true);

        // Dark opening backdrop to make the doorway read clearly from outside.
        CreateFakeDoorOpeningBackdrop(shell.transform);
    }

    private void BuildDepthWallsWithPossibleWindowOpenings(Transform shellParent, float minX, float maxX, float minY, float maxY, float minZ, float maxZ, float t, float width, float height)
    {
        List<Renderer> windows = GetControlWindowRenderers();

        if (windows.Count == 0)
        {
            CreateCube("Control_Room_Back_Wall_A", new Vector3(roomBounds.center.x, roomBounds.center.y, minZ + t * 0.5f), new Vector3(width, height, t), concreteMaterial, shellParent, true);
            CreateCube("Control_Room_Back_Wall_B", new Vector3(roomBounds.center.x, roomBounds.center.y, maxZ - t * 0.5f), new Vector3(width, height, t), concreteMaterial, shellParent, true);
            return;
        }

        float averageWindowZ = windows.Average(w => w.bounds.center.z);
        bool windowsOnMinZ = Mathf.Abs(averageWindowZ - minZ) < Mathf.Abs(averageWindowZ - maxZ);
        float windowWallZ = windowsOnMinZ ? minZ + t * 0.5f : maxZ - t * 0.5f;
        float solidWallZ = windowsOnMinZ ? maxZ - t * 0.5f : minZ + t * 0.5f;

        CreateCube("Control_Room_Solid_Depth_Wall", new Vector3(roomBounds.center.x, roomBounds.center.y, solidWallZ), new Vector3(width, height, t), concreteMaterial, shellParent, true);

        float windowMinY = windows.Min(w => w.bounds.min.y) - 0.05f;
        float windowMaxY = windows.Max(w => w.bounds.max.y) + 0.05f;
        windowMinY = Mathf.Clamp(windowMinY, minY + 0.2f, maxY - 0.4f);
        windowMaxY = Mathf.Clamp(windowMaxY, windowMinY + 0.4f, maxY - 0.1f);

        // Lower and upper bands.
        if (windowMinY - minY > 0.05f)
        {
            CreateCube("Control_Room_WindowWall_Lower_Band", new Vector3(roomBounds.center.x, (minY + windowMinY) * 0.5f, windowWallZ), new Vector3(width, windowMinY - minY, t), concreteMaterial, shellParent, true);
        }

        if (maxY - windowMaxY > 0.05f)
        {
            CreateCube("Control_Room_WindowWall_Upper_Band", new Vector3(roomBounds.center.x, (windowMaxY + maxY) * 0.5f, windowWallZ), new Vector3(width, maxY - windowMaxY, t), concreteMaterial, shellParent, true);
        }

        // Vertical posts between windows.
        List<Vector2> openings = windows
            .Select(w => new Vector2(Mathf.Clamp(w.bounds.min.x - 0.06f, minX, maxX), Mathf.Clamp(w.bounds.max.x + 0.06f, minX, maxX)))
            .OrderBy(v => v.x)
            .ToList();

        float cursor = minX;
        int postIndex = 1;
        foreach (Vector2 opening in openings)
        {
            if (opening.x - cursor > 0.05f)
            {
                CreateCube("Control_Room_WindowWall_Post_" + postIndex, new Vector3((cursor + opening.x) * 0.5f, (windowMinY + windowMaxY) * 0.5f, windowWallZ), new Vector3(opening.x - cursor, windowMaxY - windowMinY, t), concreteMaterial, shellParent, true);
                postIndex++;
            }
            cursor = Mathf.Max(cursor, opening.y);
        }

        if (maxX - cursor > 0.05f)
        {
            CreateCube("Control_Room_WindowWall_Post_" + postIndex, new Vector3((cursor + maxX) * 0.5f, (windowMinY + windowMaxY) * 0.5f, windowWallZ), new Vector3(maxX - cursor, windowMaxY - windowMinY, t), concreteMaterial, shellParent, true);
        }
    }

    private List<Renderer> GetControlWindowRenderers()
    {
        Transform searchRoot = factoryRoot != null ? factoryRoot : transform;
        Renderer[] renderers = searchRoot.GetComponentsInChildren<Renderer>(true);
        return renderers.Where(r => r.name.Contains("Control_Window") || r.transform.name.Contains("Control_Window")).ToList();
    }

    private void CreateFakeDoorOpeningBackdrop()
    {
        CreateFakeDoorOpeningBackdrop(generatedRoot);
    }

    private void CreateFakeDoorOpeningBackdrop(Transform parent)
    {
        float sideX = sideSign > 0 ? roomBounds.max.x + 0.012f : roomBounds.min.x - 0.012f;
        float doorCenterZ = GetDoorCenterZ();
        float doorCenterY = roomBounds.min.y + doorBottomOffset + doorHeight * 0.5f;
        CreateCube("Control_Room_Doorway_Dark_Opening", new Vector3(sideX, doorCenterY, doorCenterZ), new Vector3(0.025f, doorHeight, doorWidth), blackGapMaterial, parent, false);
    }

    private void BuildDoorFrameAndDoor()
    {
        float sideX = sideSign > 0 ? roomBounds.max.x + doorThickness * 0.55f : roomBounds.min.x - doorThickness * 0.55f;
        float doorCenterZ = GetDoorCenterZ();
        float doorBottomY = roomBounds.min.y + doorBottomOffset;
        float doorCenterY = doorBottomY + doorHeight * 0.5f;
        float hingeZ = doorCenterZ - doorWidth * 0.5f;
        float frameX = sideX;

        // Frame around the door.
        Transform frameParent = generatedRoot;
        CreateCube("Control_Room_Door_Frame_Left", new Vector3(frameX, doorCenterY, doorCenterZ - doorWidth * 0.5f - 0.035f), new Vector3(doorThickness * 1.5f, doorHeight + 0.12f, 0.07f), darkMetalMaterial, frameParent, true);
        CreateCube("Control_Room_Door_Frame_Right", new Vector3(frameX, doorCenterY, doorCenterZ + doorWidth * 0.5f + 0.035f), new Vector3(doorThickness * 1.5f, doorHeight + 0.12f, 0.07f), darkMetalMaterial, frameParent, true);
        CreateCube("Control_Room_Door_Frame_Top", new Vector3(frameX, doorBottomY + doorHeight + 0.035f, doorCenterZ), new Vector3(doorThickness * 1.5f, 0.07f, doorWidth + 0.16f), darkMetalMaterial, frameParent, true);

        GameObject pivotObject = new GameObject(pivotName);
        doorPivot = pivotObject.transform;
        doorPivot.SetParent(generatedRoot, true);
        doorPivot.position = new Vector3(sideX, doorCenterY, hingeZ);
        doorPivot.rotation = Quaternion.identity;

        GameObject doorObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
        doorObject.name = doorName;
        doorPanel = doorObject.transform;
        doorPanel.SetParent(doorPivot, false);
        doorPanel.localPosition = new Vector3(0f, 0f, doorWidth * 0.5f);
        doorPanel.localRotation = Quaternion.identity;
        doorPanel.localScale = new Vector3(doorThickness, doorHeight, doorWidth);
        doorObject.GetComponent<Renderer>().sharedMaterial = darkMetalMaterial;

        doorCollider = doorObject.GetComponent<BoxCollider>();
        SetStaticRecursive(doorObject, false);

        // Door handle.
        GameObject handleObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
        handleObject.name = "Control_Room_Door_Handle";
        Transform handle = handleObject.transform;
        handle.SetParent(doorPanel, false);
        handle.localPosition = new Vector3(sideSign > 0 ? -0.65f : 0.65f, 0f, doorWidth * 0.33f);
        handle.localScale = new Vector3(0.08f, 0.08f, 0.16f);
        handleObject.GetComponent<Renderer>().sharedMaterial = lightMetalMaterial;
        SetStaticRecursive(handleObject, false);

        closedYAngle = 0f;
        float direction = sideSign > 0 ? -1f : 1f;
        if (invertOpeningDirection) direction *= -1f;
        openYAngle = openAngle * direction;
    }

    private float GetDoorCenterZ()
    {
        float targetZ = roomBounds.center.z;

        if (stairsHint != null)
        {
            Renderer r = stairsHint.GetComponentInChildren<Renderer>();
            targetZ = r != null ? r.bounds.center.z : stairsHint.position.z;
        }

        targetZ += doorZOffset;
        float margin = doorWidth * 0.5f + 0.18f;
        return Mathf.Clamp(targetZ, roomBounds.min.z + margin, roomBounds.max.z - margin);
    }

    private Vector3 GetInteractionPoint()
    {
        if (doorPivot == null) return transform.position;
        return doorPivot.position + new Vector3(0f, -doorHeight * 0.25f, doorWidth * 0.45f);
    }

    private GameObject CreateCube(string objectName, Vector3 position, Vector3 scale, Material material, Transform parent, bool addCollider)
    {
        GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        obj.name = objectName;
        obj.transform.position = position;
        obj.transform.rotation = Quaternion.identity;
        obj.transform.localScale = scale;
        if (material != null)
        {
            Renderer renderer = obj.GetComponent<Renderer>();
            renderer.sharedMaterial = material;
        }

        Collider collider = obj.GetComponent<Collider>();
        if (!addCollider && collider != null)
        {
            if (Application.isPlaying) Destroy(collider);
            else DestroyImmediate(collider);
        }

        obj.transform.SetParent(parent, true);
        return obj;
    }

    private void SetStaticRecursive(GameObject obj, bool value)
    {
        if (obj == null) return;
        obj.isStatic = value;
        foreach (Transform child in obj.transform)
        {
            SetStaticRecursive(child.gameObject, value);
        }
    }

    private float NormalizeAngle(float angle)
    {
        angle %= 360f;
        if (angle > 180f) angle -= 360f;
        if (angle < -180f) angle += 360f;
        return angle;
    }
}
