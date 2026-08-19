using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DualSouls.Inventory
{
    public class InventorySlotUI : MonoBehaviour
    {
        [Header("UI")]
        public TMP_Text itemNameText;
        public TMP_Text quantityText;
        public Image iconImage;
        public Button button;

        private ItemData currentItem;
        private InventoryUI inventoryUI;

        private void Awake()
        {
            if (button == null)
                button = GetComponent<Button>();
        }

        public void Setup(ItemData item, int quantity, InventoryUI ui)
        {
            currentItem = item;
            inventoryUI = ui;

            if (itemNameText != null)
                itemNameText.text = item.itemName;

            if (quantityText != null)
                quantityText.text = "x" + quantity;

            if (iconImage != null)
            {
                iconImage.sprite = item.icon;
                iconImage.enabled = item.icon != null;
            }

            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(SelectItem);
            }
        }

        private void SelectItem()
        {
            if (inventoryUI != null)
                inventoryUI.ShowItemDetails(currentItem);
        }
    }
}