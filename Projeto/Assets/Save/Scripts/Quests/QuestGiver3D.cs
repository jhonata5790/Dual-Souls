using UnityEngine;
using DualSouls.Interaction;

namespace DualSouls.Quests
{
    public class QuestGiver3D : Interactable3D
    {
        public QuestData quest;

        public override void Interact(GameObject interactor)
        {
            if (QuestManager.Instance != null)
                QuestManager.Instance.StartQuest(quest);
        }
    }
}
