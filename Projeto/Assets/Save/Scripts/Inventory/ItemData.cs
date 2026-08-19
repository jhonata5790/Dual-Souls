using UnityEngine;

namespace DualSouls.Inventory
{
    public enum ItemType
    {
        Consumable,
        Material,
        KeyItem,
        Equipment,
        QuestItem
    }

    [CreateAssetMenu(menuName = "Dual Souls/Inventory/Item Data")]
    public class ItemData : ScriptableObject
    {
        [Header("Basic Info")]
        public string itemName;

        [TextArea(3, 8)]
        public string description;

        public Sprite icon;
        public ItemType itemType;

        [Header("Stack")]
        public bool canStack = true;
        public int maxStack = 99;
    }
}