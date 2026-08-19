using UnityEngine;

namespace DualSouls.Player
{
    public class PlayerStats : MonoBehaviour
    {
        [Header("Core Attributes")]
        public int strength = 1;
        public int agility = 1;
        public int presence = 1;
        public int intellect = 1;
        public int vigor = 1;

        [Header("Health")]
        public int maxHealth = 100;
        public int currentHealth = 100;
        public int temporaryHealth = 0;

        [Header("Temporary Skill Bonuses")]
        public int reflexesBonus;
        public int perceptionBonus;
        public int stealthBonus;
        public int acrobaticsBonus;
        public int willBonus;
        public int intimidationBonus;
        public int fightBonus;
        public int artsBonus;
        public int diplomacyBonus;
        public int deceptionBonus;

        public bool useAgilityForMelee;

        private void Awake()
        {
            currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        }

        public void AddTemporaryHealth(int amount)
        {
            temporaryHealth += Mathf.Max(0, amount);
        }

        public void TakeDamage(int amount)
        {
            int remainingDamage = Mathf.Max(0, amount);

            if (temporaryHealth > 0)
            {
                int absorbed = Mathf.Min(temporaryHealth, remainingDamage);
                temporaryHealth -= absorbed;
                remainingDamage -= absorbed;
            }

            currentHealth -= remainingDamage;
            currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        }

        public void Heal(int amount)
        {
            currentHealth += Mathf.Max(0, amount);
            currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        }

        public void ClearTemporaryBonuses()
        {
            reflexesBonus = 0;
            perceptionBonus = 0;
            stealthBonus = 0;
            acrobaticsBonus = 0;
            willBonus = 0;
            intimidationBonus = 0;
            fightBonus = 0;
            artsBonus = 0;
            diplomacyBonus = 0;
            deceptionBonus = 0;
            useAgilityForMelee = false;
        }
    }
}
