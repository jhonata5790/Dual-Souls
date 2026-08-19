using UnityEngine;

namespace DualSouls.Atlas
{
    public class AtlasTrigger3D : MonoBehaviour
    {
        public AtlasEntry entry;
        public bool unlockOnce = true;

        private bool unlocked;

        private void OnTriggerEnter(Collider other)
        {
            if (unlockOnce && unlocked)
                return;

            if (!other.CompareTag("Player"))
                return;

            if (AtlasManager.Instance != null)
            {
                AtlasManager.Instance.UnlockEntry(entry);
                unlocked = true;
            }
        }
    }
}
