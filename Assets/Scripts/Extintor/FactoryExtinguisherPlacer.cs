
using UnityEngine;

[DisallowMultipleComponent]
public class FactoryExtinguisherPlacer : MonoBehaviour
{
    [Header("Pai dos extintores")]
    public Transform extinguishersParent;

    [Header("Construção")]
    public bool createOnStart = false;
    public bool clearOldGenerated = true;

    [Header("Posições locais")]
    public Vector3[] localPositions =
    {
        new Vector3(-11.5f, 0f, -13.5f),
        new Vector3(11.5f, 0f, -4.0f),
        new Vector3(-8.5f, 0f, 10.5f),
        new Vector3(4.5f, 0f, 15.0f)
    };

    [Header("Rotações locais")]
    public Vector3[] localEulerRotations =
    {
        new Vector3(0f, 0f, 0f),
        new Vector3(0f, 180f, 0f),
        new Vector3(0f, 90f, 0f),
        new Vector3(0f, -90f, 0f)
    };

    [Header("Debug")]
    public bool showDebugLogs = false;

    private const string GENERATED_PARENT_NAME = "Extinguishers_Generated";

    void Start()
    {
        if (createOnStart)
            CreateExtinguishers();
    }

    [ContextMenu("Create Extinguishers")]
    public void CreateExtinguishers()
    {
        EnsureParent();

        if (clearOldGenerated)
            ClearGenerated();

        for (int i = 0; i < localPositions.Length; i++)
        {
            GameObject obj = new GameObject("Extinguisher_" + (i + 1).ToString("00"));
            obj.transform.SetParent(extinguishersParent, false);
            obj.transform.localPosition = localPositions[i];

            Vector3 rot = Vector3.zero;
            if (localEulerRotations != null && localEulerRotations.Length > i)
                rot = localEulerRotations[i];

            obj.transform.localRotation = Quaternion.Euler(rot);
            obj.transform.localScale = Vector3.one;

            FactoryExtinguisherBuilder builder = obj.AddComponent<FactoryExtinguisherBuilder>();
            builder.BuildExtinguisher();
        }

        if (showDebugLogs)
            Debug.Log("[FactoryExtinguisherPlacer] Extintores criados: " + localPositions.Length, this);
    }

    void EnsureParent()
    {
        if (extinguishersParent != null)
            return;

        Transform existing = transform.Find(GENERATED_PARENT_NAME);

        if (existing != null)
        {
            extinguishersParent = existing;
            return;
        }

        GameObject parent = new GameObject(GENERATED_PARENT_NAME);
        parent.transform.SetParent(transform, false);
        parent.transform.localPosition = Vector3.zero;
        parent.transform.localRotation = Quaternion.identity;
        parent.transform.localScale = Vector3.one;

        extinguishersParent = parent.transform;
    }

    [ContextMenu("Clear Generated Extinguishers")]
    public void ClearGenerated()
    {
        EnsureParent();

        for (int i = extinguishersParent.childCount - 1; i >= 0; i--)
        {
            Transform child = extinguishersParent.GetChild(i);

            if (Application.isPlaying)
                Destroy(child.gameObject);
            else
                DestroyImmediate(child.gameObject);
        }
    }
}
