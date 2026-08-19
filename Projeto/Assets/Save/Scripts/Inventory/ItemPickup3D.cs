using UnityEngine;
using DualSouls.Interaction;

namespace DualSouls.Inventory
{
    public class ItemPickup3D : Interactable3D
    {
        [Header("Item")]
        public ItemData item;
        public int quantity = 1;

        [Header("Pickup")]
        public bool destroyAfterPickup = true;

        public override void Interact(GameObject interactor)
        {
            if (InventoryManager.Instance == null)
            {
                Debug.LogWarning("Não existe InventoryManager na cena.");
                return;
            }

            InventoryManager.Instance.AddItem(item, quantity);

            if (destroyAfterPickup)
                Destroy(gameObject);
        }
    }
}