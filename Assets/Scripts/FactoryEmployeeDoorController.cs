using System.Collections;
using UnityEngine;

/// <summary>
/// Door controller for the factory employee door exported from Blender/FBX.
/// Attach this component to the door object itself or to the factory root and assign Door Transform.
/// It creates a fake hinge pivot on one side of the door, then rotates the pivot to open/close.
/// </summary>
[DisallowMultipleComponent]
public class FactoryEmployeeDoorController : MonoBehaviour
{
    public enum HingeSide
    {
        Left,
        Right
    }

    [Header("References")]
    [Tooltip("Player object used to measure interaction distance. Usually Player_FPS.")]
    public Transform playerTransform;

    [Tooltip("Door object to rotate. If empty, the script tries to find an object named Employee_Door below this object.")]
    public Transform doorTransform;

    [Header("Interaction")]
    public KeyCode interactionKey = KeyCode.F;
    public float interactionDistance = 3.0f;
    public string openPrompt = "Pressione F para abrir";
    public string closePrompt = "Pressione F para fechar";

    [Header("Door Motion")]
    [Tooltip("Which side of the door becomes the hinge/pivot.")]
    public HingeSide hingeSide = HingeSide.Left;

    [Tooltip("Opening angle in degrees. If the door opens to the wrong side, use a negative value.")]
    public float openAngle = -95.0f;

    [Tooltip("Seconds used by the opening/closing animation.")]
    public float animationDuration = 0.75f;

    [Tooltip("Small offset to place the hinge slightly outside the door edge, avoiding visual clipping.")]
    public float hingeOutwardOffset = 0.02f;

    [Tooltip("If true, automatically creates/rebuilds the fake pivot on Start.")]
    public bool setupPivotOnStart = true;

    [Header("Collider")]
    [Tooltip("Adds a BoxCollider if the door has no collider.")]
    public bool ensureCollider = true;

    [Tooltip("Keeps the collider active even while the door is open.")]
    public bool keepColliderWhileOpen = true;

    [Header("Debug")]
    public bool showDebugGizmos = true;

    private Transform hingePivot;
    private Quaternion closedRotation;
    private Quaternion openedRotation;
    private bool isOpen;
    private bool isAnimating;
    private Collider[] doorColliders;
    private Renderer doorRenderer;

    private void Reset()
    {
        TryAutoAssignDoor();
    }

    private void Awake()
    {
        if (doorTransform == null)
            TryAutoAssignDoor();
    }

    private void Start()
    {
        if (doorTransform == null)
        {
            Debug.LogWarning("[FactoryEmployeeDoorController] Door Transform is missing. Assign Employee_Door in the Inspector.", this);
            enabled = false;
            return;
        }

        MakeDynamicRecursively(doorTransform.gameObject);

        if (ensureCollider)
            EnsureDoorCollider();

        if (setupPivotOnStart)
            SetupHingePivot();

        CacheDoorColliders();
    }

    private void Update()
    {
        if (doorTransform == null || playerTransform == null || hingePivot == null)
            return;

        if (!IsPlayerNear())
            return;

        if (Input.GetKeyDown(interactionKey))
            ToggleDoor();
    }

    private void OnGUI()
    {
        if (doorTransform == null || playerTransform == null || hingePivot == null)
            return;

        if (!IsPlayerNear())
            return;

        string message = isOpen ? closePrompt : openPrompt;

        GUIStyle style = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 24,
            normal = { textColor = Color.white }
        };

        Rect rect = new Rect(0, Screen.height - 115, Screen.width, 40);
        GUI.Label(rect, message, style);
    }

    private bool IsPlayerNear()
    {
        return Vector3.Distance(playerTransform.position, GetDoorCenterWorld()) <= interactionDistance;
    }

    private Vector3 GetDoorCenterWorld()
    {
        if (doorRenderer != null)
            return doorRenderer.bounds.center;

        return doorTransform.position;
    }

    private void TryAutoAssignDoor()
    {
        if (name.Contains("Employee_Door"))
        {
            doorTransform = transform;
            return;
        }

        Transform found = FindDeepChild(transform, "Employee_Door");
        if (found != null)
            doorTransform = found;
    }

    private static Transform FindDeepChild(Transform parent, string targetName)
    {
        foreach (Transform child in parent)
        {
            if (child.name == targetName || child.name.Contains(targetName))
                return child;

            Transform result = FindDeepChild(child, targetName);
            if (result != null)
                return result;
        }

        return null;
    }

    [ContextMenu("Setup/Rebuild Door Pivot")]
    public void SetupHingePivot()
    {
        if (doorTransform == null)
        {
            TryAutoAssignDoor();
            if (doorTransform == null)
            {
                Debug.LogWarning("[FactoryEmployeeDoorController] Could not find Employee_Door.", this);
                return;
            }
        }

        MakeDynamicRecursively(doorTransform.gameObject);

        doorRenderer = doorTransform.GetComponentInChildren<Renderer>();
        if (doorRenderer == null)
        {
            Debug.LogWarning("[FactoryEmployeeDoorController] Door has no Renderer. Cannot calculate hinge side.", this);
            return;
        }

        Transform oldPivot = doorTransform.parent != null ? doorTransform.parent.Find(doorTransform.name + "_RuntimeHingePivot") : null;
        if (oldPivot != null)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
                DestroyImmediate(oldPivot.gameObject);
            else
                Destroy(oldPivot.gameObject);
#else
            Destroy(oldPivot.gameObject);
#endif
        }

        Transform originalParent = doorTransform.parent;
        Bounds bounds = doorRenderer.bounds;

        Vector3 hingePosition = bounds.center;
        hingePosition.x = hingeSide == HingeSide.Left ? bounds.min.x : bounds.max.x;
        hingePosition.y = bounds.center.y;
        hingePosition.z = bounds.center.z;

        // Tiny outward offset on Z. For the front employee door this usually helps avoid clipping.
        hingePosition.z += hingeOutwardOffset;

        GameObject pivotObject = new GameObject(doorTransform.name + "_RuntimeHingePivot");
        hingePivot = pivotObject.transform;
        hingePivot.SetParent(originalParent, true);
        hingePivot.position = hingePosition;
        hingePivot.rotation = Quaternion.identity;
        hingePivot.localScale = Vector3.one;

        doorTransform.SetParent(hingePivot, true);

        closedRotation = hingePivot.localRotation;
        openedRotation = closedRotation * Quaternion.Euler(0f, openAngle, 0f);

        CacheDoorColliders();

        Debug.Log("[FactoryEmployeeDoorController] Door hinge pivot created. If it opens wrong, change Hinge Side or invert Open Angle.", this);
    }

    [ContextMenu("Toggle Door Now")]
    public void ToggleDoor()
    {
        if (isAnimating || hingePivot == null)
            return;

        StartCoroutine(AnimateDoor(!isOpen));
    }

    [ContextMenu("Open Door Now")]
    public void OpenDoorNow()
    {
        if (hingePivot == null)
            SetupHingePivot();

        if (hingePivot == null)
            return;

        StopAllCoroutines();
        isAnimating = false;
        isOpen = true;
        hingePivot.localRotation = openedRotation;
        SetDoorColliders(keepColliderWhileOpen);
    }

    [ContextMenu("Close Door Now")]
    public void CloseDoorNow()
    {
        if (hingePivot == null)
            SetupHingePivot();

        if (hingePivot == null)
            return;

        StopAllCoroutines();
        isAnimating = false;
        isOpen = false;
        hingePivot.localRotation = closedRotation;
        SetDoorColliders(true);
    }

    private IEnumerator AnimateDoor(bool open)
    {
        isAnimating = true;

        if (!open)
            SetDoorColliders(true);

        Quaternion start = hingePivot.localRotation;
        Quaternion target = open ? openedRotation : closedRotation;

        float time = 0f;
        float duration = Mathf.Max(0.01f, animationDuration);

        while (time < duration)
        {
            time += Time.deltaTime;
            float t = Mathf.Clamp01(time / duration);
            t = t * t * (3f - 2f * t); // SmoothStep
            hingePivot.localRotation = Quaternion.Slerp(start, target, t);
            yield return null;
        }

        hingePivot.localRotation = target;
        isOpen = open;

        if (open)
            SetDoorColliders(keepColliderWhileOpen);

        isAnimating = false;
    }

    private void EnsureDoorCollider()
    {
        if (doorTransform == null)
            return;

        Collider existing = doorTransform.GetComponent<Collider>();
        if (existing != null)
            return;

        MeshRenderer renderer = doorTransform.GetComponentInChildren<MeshRenderer>();
        BoxCollider box = doorTransform.gameObject.AddComponent<BoxCollider>();

        if (renderer != null)
        {
            // Approximate collider from local mesh bounds when possible.
            MeshFilter meshFilter = doorTransform.GetComponent<MeshFilter>();
            if (meshFilter != null && meshFilter.sharedMesh != null)
            {
                Bounds meshBounds = meshFilter.sharedMesh.bounds;
                box.center = meshBounds.center;
                box.size = meshBounds.size;
            }
        }
    }

    private void CacheDoorColliders()
    {
        if (doorTransform != null)
            doorColliders = doorTransform.GetComponentsInChildren<Collider>(true);
    }

    private void SetDoorColliders(bool active)
    {
        if (doorColliders == null)
            return;

        foreach (Collider c in doorColliders)
        {
            if (c != null)
                c.enabled = active;
        }
    }

    private static void MakeDynamicRecursively(GameObject obj)
    {
        obj.isStatic = false;
        foreach (Transform child in obj.transform)
            MakeDynamicRecursively(child.gameObject);
    }

    private void OnDrawGizmosSelected()
    {
        if (!showDebugGizmos)
            return;

        Transform targetDoor = doorTransform != null ? doorTransform : transform;
        Renderer r = targetDoor.GetComponentInChildren<Renderer>();

        if (r != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(r.bounds.center, r.bounds.size);

            Vector3 hingePos = r.bounds.center;
            hingePos.x = hingeSide == HingeSide.Left ? r.bounds.min.x : r.bounds.max.x;
            hingePos.z += hingeOutwardOffset;

            Gizmos.color = Color.green;
            Gizmos.DrawSphere(hingePos, 0.12f);
            Gizmos.DrawLine(hingePos + Vector3.down * 1.2f, hingePos + Vector3.up * 1.2f);
        }
    }
}
