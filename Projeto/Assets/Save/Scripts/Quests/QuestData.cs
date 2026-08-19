using UnityEngine;

namespace DualSouls.Quests
{
    [CreateAssetMenu(menuName = "Dual Souls/Quests/Quest Data")]
    public class QuestData : ScriptableObject
    {
        public string questId;
        public string questTitle;

        [TextArea(3, 8)]
        public string description;

        public QuestStep[] steps;
    }

    [System.Serializable]
    public class QuestStep
    {
        public string stepId;
        public string objectiveText;
        public bool completed;
    }
}
