using UnityEngine;

namespace DualSouls.Interaction
{
    public class PlayerInteraction3D : MonoBehaviour
    {
        public float interactionRadius = 2f;
        public LayerMask interactableLayer;
        public KeyCode interactKey = KeyCode.E;

        private Interactable3D currentInteractable;

        private void Update()
        {
            FindClosestInteractable();

            if (Input.GetKeyDown(interactKey) && currentInteractable != null)
                currentInteractable.Interact(gameObject);
        }

        private void FindClosestInteractable()
        {
            Collider[] hits = Physics.OverlapSphere(transform.position, interactionRadius, interactableLayer);

            float closestDistance = Mathf.Infinity;
            currentInteractable = null;

            foreach (Collider hit in hits)
            {
                Interactable3D interactable = hit.GetComponentInParent<Interactable3D>();
                if (interactable == null)
                    continue;

                float distance = Vector3.Distance(transform.position, interactable.transform.position);

                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    currentInteractable = interactable;
                }
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, interactionRadius);
        }
    }
}
