
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
public class FactoryEmergencyManager : MonoBehaviour
{
    [Header("Focos de fumaça/fogo")]
    public List<FactoryFireHazard> hazards = new List<FactoryFireHazard>();

    [Header("Começo")]
    public bool startEmergencyOnPlay = false;
    public float startDelay = 1f;

    [Tooltip("Se ligado, um foco começa depois do outro.")]
    public bool startSequentially = true;

    public float delayBetweenHazards = 1.5f;

    [Header("Eventos")]
    public UnityEvent onEmergencyStarted;
    public UnityEvent onAllFiresExtinguished;

    [Header("Debug")]
    public bool showDebugLogs = false;

    private bool emergencyStarted;
    private bool allSolvedNotified;
    private float timer;
    private int nextHazardIndex;

    void Awake()
    {
        if (hazards.Count == 0)
            FindHazards();
    }

    void Start()
    {
        if (startEmergencyOnPlay)
            Invoke(nameof(StartEmergency), startDelay);
    }

    void Update()
    {
        if (emergencyStarted && startSequentially && nextHazardIndex < hazards.Count)
        {
            timer += Time.deltaTime;

            if (timer >= delayBetweenHazards)
            {
                timer = 0f;
                StartNextHazard();
            }
        }

        if (emergencyStarted && !allSolvedNotified && AreAllHazardsSolved())
        {
            allSolvedNotified = true;
            onAllFiresExtinguished?.Invoke();

            if (showDebugLogs)
                Debug.Log("[FactoryEmergencyManager] Todos os focos foram resolvidos.", this);
        }
    }

    [ContextMenu("Find Hazards")]
    public void FindHazards()
    {
        hazards.Clear();
        hazards.AddRange(FindObjectsOfType<FactoryFireHazard>(true));

        if (showDebugLogs)
            Debug.Log("[FactoryEmergencyManager] Focos encontrados: " + hazards.Count, this);
    }

    [ContextMenu("Start Emergency")]
    public void StartEmergency()
    {
        if (hazards.Count == 0)
            FindHazards();

        emergencyStarted = true;
        allSolvedNotified = false;
        timer = 0f;
        nextHazardIndex = 0;

        onEmergencyStarted?.Invoke();

        if (startSequentially)
            StartNextHazard();
        else
        {
            foreach (FactoryFireHazard hazard in hazards)
            {
                if (hazard != null)
                    hazard.StartSmoke();
            }
        }

        if (showDebugLogs)
            Debug.Log("[FactoryEmergencyManager] Emergência iniciada.", this);
    }

    void StartNextHazard()
    {
        if (nextHazardIndex >= hazards.Count)
            return;

        FactoryFireHazard hazard = hazards[nextHazardIndex];
        nextHazardIndex++;

        if (hazard != null)
            hazard.StartSmoke();
    }

    [ContextMenu("Force All Fires")]
    public void ForceAllFires()
    {
        if (hazards.Count == 0)
            FindHazards();

        foreach (FactoryFireHazard hazard in hazards)
        {
            if (hazard != null)
                hazard.StartFire();
        }

        emergencyStarted = true;
        allSolvedNotified = false;
    }

    [ContextMenu("Reset Emergency")]
    public void ResetEmergency()
    {
        foreach (FactoryFireHazard hazard in hazards)
        {
            if (hazard != null)
                hazard.ResetHazard();
        }

        emergencyStarted = false;
        allSolvedNotified = false;
        timer = 0f;
        nextHazardIndex = 0;
    }

    public bool AreAllHazardsSolved()
    {
        if (hazards.Count == 0)
            return true;

        foreach (FactoryFireHazard hazard in hazards)
        {
            if (hazard == null)
                continue;

            if (!hazard.IsSolved())
                return false;
        }

        return true;
    }
}
