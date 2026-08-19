using UnityEngine;
using DualSouls.Player;

namespace DualSouls.Combat
{
    public class PlayerCombat3D : MonoBehaviour
    {
        [Header("Attack")]
        public Transform attackPoint;
        public float attackRadius = 1.1f;
        public LayerMask enemyLayer;
        public int baseDamage = 10;
        public float attackCooldown = 0.45f;

        [Header("Animation")]
        public Animator animator;

        [Header("Stats")]
        public PlayerStats stats;

        private float cooldownTimer;

        private void Awake()
        {
            if (animator == null)
                animator = GetComponentInChildren<Animator>();

            if (stats == null)
                stats = GetComponent<PlayerStats>();
        }

        private void Update()
        {
            if (cooldownTimer > 0)
                cooldownTimer -= Time.deltaTime;

            if (Input.GetMouseButtonDown(0) && cooldownTimer <= 0)
                Attack();
        }

        private void Attack()
        {
            cooldownTimer = attackCooldown;

            if (animator != null)
                animator.SetTrigger("Attack");

            int finalDamage = baseDamage;

            if (stats != null)
            {
                int attribute = stats.useAgilityForMelee ? stats.agility : stats.strength;
                finalDamage += attribute + stats.fightBonus;
            }

            Vector3 center = attackPoint != null ? attackPoint.position : transform.position + transform.forward;
            Collider[] hits = Physics.OverlapSphere(center, attackRadius, enemyLayer);

            foreach (Collider hit in hits)
            {
                Health health = hit.GetComponentInParent<Health>();
                if (health != null)
                    health.TakeDamage(finalDamage);
            }
        }

        private void OnDrawGizmosSelected()
        {
            Vector3 center = attackPoint != null ? attackPoint.position : transform.position + transform.forward;
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(center, attackRadius);
        }
    }
}
