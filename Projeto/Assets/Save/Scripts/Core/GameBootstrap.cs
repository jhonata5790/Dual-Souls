using UnityEngine;

namespace DualSouls.Core
{
    public class GameBootstrap : MonoBehaviour
    {
        [Header("Optional Manager Prefabs")]
        public GameObject[] managerPrefabs;

        private void Awake()
        {
            foreach (GameObject prefab in managerPrefabs)
            {
                if (prefab != null)
                    Instantiate(prefab);
            }
        }
    }
}
