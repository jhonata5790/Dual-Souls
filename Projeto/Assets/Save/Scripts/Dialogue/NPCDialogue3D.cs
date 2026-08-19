using UnityEngine;
using DualSouls.Interaction;

namespace DualSouls.Dialogue
{
    public class NPCDialogue3D : Interactable3D
    {
        public DialogueData dialogue;

        public override void Interact(GameObject interactor)
        {
            if (DialogueSystem.Instance != null)
                DialogueSystem.Instance.StartDialogue(dialogue);
        }
    }
}
