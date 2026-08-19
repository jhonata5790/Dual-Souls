using UnityEngine;
using UnityEngine.Events;

namespace DualSouls.Combat
{
    public class Health : MonoBehaviour
    {
        public int maxHealth = 30;
        public int currentHealth = 30;
        public bool destroyOnDeath = false;

        public UnityEvent onDamaged;
        public UnityEvent onDeath;

        private bool isDead;

        private void Awake()
        {
            currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        }

        public void TakeDamage(int damage)
        {
            if (isDead)
                return;

            currentHealth -= Mathf.Max(0, damage);
            onDamaged?.Invoke();

            if (currentHealth <= 0)
                Die();
        }

        public void Heal(int amount)
        {
            if (isDead)
                return;

            currentHealth += Mathf.Max(0, amount);
            currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        }

        private void Die()
        {
            isDead = true;
            currentHealth = 0;
            onDeath?.Invoke();

            if (destroyOnDeath)
                Destroy(gameObject);
        }
    }
}
