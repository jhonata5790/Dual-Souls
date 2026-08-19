using UnityEngine;
using TMPro;
using UnityEngine.UI;
using DualSouls.Atlas;

namespace DualSouls.UI
{
    public class AtlasUI : MonoBehaviour
    {
        [Header("List")]
        public Transform contentParent;
        public GameObject entryButtonPrefab;

        [Header("Details")]
        public TMP_Text titleText;
        public TMP_Text categoryText;
        public TMP_Text regionText;
        public TMP_Text dangerText;
        public TMP_Text notesText;
        public Image illustrationImage;

        private void OnEnable()
        {
            Refresh();

            if (AtlasManager.Instance != null)
                AtlasManager.Instance.OnAtlasChanged += Refresh;
        }

        private void OnDisable()
        {
            if (AtlasManager.Instance != null)
                AtlasManager.Instance.OnAtlasChanged -= Refresh;
        }

        public void Refresh()
        {
            if (contentParent == null || entryButtonPrefab == null)
                return;

            foreach (Transform child in contentParent)
                Destroy(child.gameObject);

            if (AtlasManager.Instance == null)
                return;

            foreach (AtlasEntry entry in AtlasManager.Instance.unlockedEntries)
            {
                GameObject buttonObject = Instantiate(entryButtonPrefab, contentParent);

                TMP_Text buttonText = buttonObject.GetComponentInChildren<TMP_Text>();
                if (buttonText != null)
                    buttonText.text = entry.entryTitle;

                Button button = buttonObject.GetComponent<Button>();
                if (button != null)
                {
                    AtlasEntry capturedEntry = entry;
                    button.onClick.AddListener(() => ShowEntry(capturedEntry));
                }
            }

            if (AtlasManager.Instance.unlockedEntries.Count > 0)
                ShowEntry(AtlasManager.Instance.unlockedEntries[0]);
            else
                ClearDetails();
        }

        public void ShowEntry(AtlasEntry entry)
        {
            if (entry == null)
                return;

            if (titleText != null)
                titleText.text = entry.entryTitle;

            if (categoryText != null)
                categoryText.text = "Categoria: " + entry.category;

            if (regionText != null)
                regionText.text = "Região: " + entry.region;

            if (dangerText != null)
                dangerText.text = "Perigo: " + entry.dangerLevel;

            if (notesText != null)
                notesText.text = entry.zunnaNotes;

            if (illustrationImage != null)
            {
                illustrationImage.sprite = entry.illustration;
                illustrationImage.enabled = entry.illustration != null;
            }
        }

        private void ClearDetails()
        {
            if (titleText != null)
                titleText.text = "Atlas da Zunna";

            if (categoryText != null)
                categoryText.text = "Nenhuma página desbloqueada.";

            if (regionText != null)
                regionText.text = "";

            if (dangerText != null)
                dangerText.text = "";

            if (notesText != null)
                notesText.text = "Explore Altheris para desbloquear anotações.";

            if (illustrationImage != null)
                illustrationImage.enabled = false;
        }
    }
}
