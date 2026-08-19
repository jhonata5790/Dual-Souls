using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DualSouls.Inventory
{
    public class InventoryUI : MonoBehaviour
    {
        [Header("Main")]
        public GameObject inventoryPanel;
        public KeyCode inventoryKey = KeyCode.I;

        [Header("List")]
        public Transform contentParent;
        public GameObject slotPrefab;

        [Header("Details")]
        public TMP_Text selectedItemNameText;
        public TMP_Text selectedItemTypeText;
        public TMP_Text selectedItemDescriptionText;
        public Image selectedItemIcon;

        private bool isOpen;

        private void Start()
        {
            if (inventoryPanel != null)
                inventoryPanel.SetActive(false);

            ClearDetails();
        }

        private void OnEnable()
        {
            if (InventoryManager.Instance != null)
                InventoryManager.Instance.OnInventoryChanged += Refresh;
        }

        private void OnDisable()
        {
            if (InventoryManager.Instance != null)
                InventoryManager.Instance.OnInventoryChanged -= Refresh;
        }

        private void Update()
        {
            if (Input.GetKeyDown(inventoryKey))
                ToggleInventory();
        }

        public void ToggleInventory()
        {
            if (isOpen)
                CloseInventory();
            else
                OpenInventory();
        }

        public void OpenInventory()
        {
            isOpen = true;

            if (inventoryPanel != null)
                inventoryPanel.SetActive(true);

            Refresh();
        }

        public void CloseInventory()
        {
            isOpen = false;

            if (inventoryPanel != null)
                inventoryPanel.SetActive(false);
        }

        public void Refresh()
        {
            if (contentParent == null)
            {
                Debug.LogWarning("InventoryUI: Content Parent não foi configurado.");
                return;
            }

            if (slotPrefab == null)
            {
                Debug.LogWarning("InventoryUI: Slot Prefab não foi configurado.");
                return;
            }

            foreach (Transform child in contentParent)
                Destroy(child.gameObject);

            if (InventoryManager.Instance == null)
            {
                Debug.LogWarning("InventoryUI: Não existe InventoryManager na cena.");
                return;
            }

            var items = InventoryManager.Instance.GetItems();

            Debug.Log("InventoryUI atualizando. Itens encontrados: " + items.Count);

            foreach (InventoryManager.InventorySlot slot in items)
            {
                GameObject slotObject = Instantiate(slotPrefab, contentParent);
                slotObject.SetActive(true);

                InventorySlotUI slotUI = slotObject.GetComponent<InventorySlotUI>();

                if (slotUI != null)
                {
                    slotUI.Setup(slot.item, slot.quantity, this);
                }
                else
                {
                    TMP_Text text = slotObject.GetComponentInChildren<TMP_Text>();

                    if (text != null)
                        text.text = slot.item.itemName + " x" + slot.quantity;
                    else
                        Debug.LogWarning("InventoryUI: O Slot Prefab não tem InventorySlotUI nem TMP_Text.");
                }
            }
        }

        public void ShowItemDetails(ItemData item)
        {
            if (item == null)
                return;

            if (selectedItemNameText != null)
                selectedItemNameText.text = item.itemName;

            if (selectedItemTypeText != null)
                selectedItemTypeText.text = item.itemType.ToString();

            if (selectedItemDescriptionText != null)
                selectedItemDescriptionText.text = item.description;

            if (selectedItemIcon != null)
            {
                selectedItemIcon.sprite = item.icon;
                selectedItemIcon.enabled = item.icon != null;
            }
        }

        private void ClearDetails()
        {
            if (selectedItemNameText != null)
                selectedItemNameText.text = "Nenhum item selecionado";

            if (selectedItemTypeText != null)
                selectedItemTypeText.text = "Tipo:";

            if (selectedItemDescriptionText != null)
                selectedItemDescriptionText.text = "Clique em um item para ver a descrição.";

            if (selectedItemIcon != null)
                selectedItemIcon.enabled = false;
        }
    }
}