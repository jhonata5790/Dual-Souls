using System.Collections.Generic;
using UnityEngine;

namespace DualSouls.Atlas
{
    public class AtlasManager : MonoBehaviour
    {
        public static AtlasManager Instance { get; private set; }

        public List<AtlasEntry> unlockedEntries = new List<AtlasEntry>();

        public event System.Action OnAtlasChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        public void UnlockEntry(AtlasEntry entry)
        {
            if (entry == null || unlockedEntries.Contains(entry))
                return;

            unlockedEntries.Add(entry);
            Debug.Log("Atlas da Zunna atualizado: " + entry.entryTitle);
            OnAtlasChanged?.Invoke();
        }

        public bool IsUnlocked(AtlasEntry entry)
        {
            return unlockedEntries.Contains(entry);
        }
    }
}
