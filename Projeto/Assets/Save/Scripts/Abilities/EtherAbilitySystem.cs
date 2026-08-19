using UnityEngine;

namespace DualSouls.Abilities
{
    public class EtherAbilitySystem : MonoBehaviour
    {
        public AbilityBase[] abilities;
        public KeyCode[] abilityKeys;

        private void Awake()
        {
            abilities = GetComponents<AbilityBase>();
        }

        private void Update()
        {
            for (int i = 0; i < abilities.Length; i++)
            {
                if (i >= abilityKeys.Length)
                    continue;

                if (Input.GetKeyDown(abilityKeys[i]) && !abilities[i].IsOnCooldown)
                    abilities[i].Activate();
            }
        }
    }
}
