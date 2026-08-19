using UnityEngine;

namespace DualSouls.Interaction
{
    public abstract class Interactable3D : MonoBehaviour
    {
        public string interactionPrompt = "Interagir";

        public abstract void Interact(GameObject interactor);
    }
}
