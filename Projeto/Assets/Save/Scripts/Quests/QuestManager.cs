using System.Collections.Generic;
using UnityEngine;

namespace DualSouls.Quests
{
    public class QuestManager : MonoBehaviour
    {
        public static QuestManager Instance { get; private set; }

        public List<QuestData> activeQuests = new List<QuestData>();
        public List<QuestData> completedQuests = new List<QuestData>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        public void StartQuest(QuestData quest)
        {
            if (quest == null)
                return;

            if (activeQuests.Contains(quest) || completedQuests.Contains(quest))
                return;

            ResetQuestSteps(quest);
            activeQuests.Add(quest);

            Debug.Log("Missão iniciada: " + quest.questTitle);
        }

        public void CompleteStep(QuestData quest, string stepId)
        {
            if (quest == null || quest.steps == null)
                return;

            foreach (QuestStep step in quest.steps)
            {
                if (step.stepId == stepId)
                {
                    step.completed = true;
                    Debug.Log("Objetivo concluído: " + step.objectiveText);
                    break;
                }
            }

            if (AreAllStepsCompleted(quest))
                CompleteQuest(quest);
        }

        public bool AreAllStepsCompleted(QuestData quest)
        {
            if (quest == null || quest.steps == null || quest.steps.Length == 0)
                return false;

            foreach (QuestStep step in quest.steps)
            {
                if (!step.completed)
                    return false;
            }

            return true;
        }

        private void CompleteQuest(QuestData quest)
        {
            if (!activeQuests.Contains(quest))
                return;

            activeQuests.Remove(quest);
            completedQuests.Add(quest);

            Debug.Log("Missão concluída: " + quest.questTitle);
        }

        private void ResetQuestSteps(QuestData quest)
        {
            if (quest.steps == null)
                return;

            foreach (QuestStep step in quest.steps)
                step.completed = false;
        }
    }
}
