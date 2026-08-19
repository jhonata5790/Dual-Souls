using UnityEngine;

namespace DualSouls.UI
{
    public class HUDController : MonoBehaviour
    {
        [Header("Panels")]
        public GameObject inventoryPanel;
        public GameObject atlasPanel;

        [Header("Input")]
        public KeyCode inventoryKey = KeyCode.I;
        public KeyCode atlasKey = KeyCode.J;
        public KeyCode closeKey = KeyCode.Escape;

        private void Start()
        {
            CloseAllPanels();
        }

        private void Update()
        {
            if (Input.GetKeyDown(inventoryKey))
                ToggleInventory();

            if (Input.GetKeyDown(atlasKey))
                ToggleAtlas();

            if (Input.GetKeyDown(closeKey))
                CloseAllPanels();
        }

        public void ToggleInventory()
        {
            bool willOpen = inventoryPanel != null && !inventoryPanel.activeSelf;

            CloseAllPanels();

            if (inventoryPanel != null)
                inventoryPanel.SetActive(willOpen);
        }

        public void ToggleAtlas()
        {
            bool willOpen = atlasPanel != null && !atlasPanel.activeSelf;

            CloseAllPanels();

            if (atlasPanel != null)
                atlasPanel.SetActive(willOpen);
        }

        public void CloseAllPanels()
        {
            if (inventoryPanel != null)
                inventoryPanel.SetActive(false);

            if (atlasPanel != null)
                atlasPanel.SetActive(false);
        }
    }
}
