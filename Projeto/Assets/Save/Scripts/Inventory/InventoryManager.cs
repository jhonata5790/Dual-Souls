using System;
using System.Collections.Generic;
using UnityEngine;

namespace DualSouls.Inventory
{
    public class InventoryManager : MonoBehaviour
    {
        public static InventoryManager Instance { get; private set; }

        [Serializable]
        public class InventorySlot
        {
            public ItemData item;
            public int quantity;

            public InventorySlot(ItemData item, int quantity)
            {
                this.item = item;
                this.quantity = quantity;
            }
        }

        [Header("Inventory")]
        public List<InventorySlot> items = new List<InventorySlot>();

        public event Action OnInventoryChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        public void AddItem(ItemData item, int amount = 1)
        {
            if (item == null || amount <= 0)
                return;

            if (item.canStack)
            {
                InventorySlot existingSlot = items.Find(slot => slot.item == item);

                if (existingSlot != null)
                {
                    existingSlot.quantity += amount;
                    existingSlot.quantity = Mathf.Clamp(existingSlot.quantity, 1, item.maxStack);

                    OnInventoryChanged?.Invoke();
                    Debug.Log("Item atualizado: " + item.itemName + " x" + existingSlot.quantity);
                    return;
                }
            }

            items.Add(new InventorySlot(item, amount));

            OnInventoryChanged?.Invoke();
            Debug.Log("Item adicionado: " + item.itemName + " x" + amount);
        }

        public bool RemoveItem(ItemData item, int amount = 1)
        {
            if (item == null || amount <= 0)
                return false;

            InventorySlot slot = items.Find(s => s.item == item);

            if (slot == null)
                return false;

            if (slot.quantity < amount)
                return false;

            slot.quantity -= amount;

            if (slot.quantity <= 0)
                items.Remove(slot);

            OnInventoryChanged?.Invoke();
            return true;
        }

        public bool HasItem(ItemData item, int amount = 1)
        {
            InventorySlot slot = items.Find(s => s.item == item);

            if (slot == null)
                return false;

            return slot.quantity >= amount;
        }

        public List<InventorySlot> GetItems()
        {
            return items;
        }
    }
}