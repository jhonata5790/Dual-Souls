
using UnityEngine;

[DisallowMultipleComponent]
public class FactoryExtinguisherMarker : MonoBehaviour
{
    [Header("Interação futura")]
    public string interactionPrompt = "Pegar extintor";
    public bool canBePicked = true;
    public bool isPicked = false;

    [Header("Carga")]
    public float maxFuel = 100f;
    public float currentFuel = 100f;

    [Header("Pontos")]
    public Transform holdPoint;
    public Transform nozzlePoint;

    [Header("Visual")]
    public GameObject visualRoot;

    void Reset()
    {
        holdPoint = transform.Find("Hold_Point");
        nozzlePoint = transform.Find("Nozzle_Point");

        Transform visual = transform.Find("Extinguisher_Visual_Generated");
        if (visual != null)
            visualRoot = visual.gameObject;
    }

    public void SetPicked(bool picked)
    {
        isPicked = picked;

        if (visualRoot != null)
            visualRoot.SetActive(!picked);
    }

    public bool HasFuel()
    {
        return currentFuel > 0f;
    }

    public void Refill()
    {
        currentFuel = maxFuel;
    }
}
