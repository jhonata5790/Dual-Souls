using UnityEngine;
using UnityEngine.AI;
using DualSouls.Combat;

namespace DualSouls.Enemies
{
    [RequireComponent(typeof(NavMeshAgent))]
    public class EnemyAI3D : MonoBehaviour
    {
        public enum EnemyState
        {
            Idle,
            Patrol,
            Chase,
            Attack
        }

        [Header("Target")]
        public Transform player;

        [Header("Ranges")]
        public float detectionRange = 8f;
        public float attackRange = 1.6f;

        [Header("Combat")]
        public int attackDamage = 10;
        public float attackCooldown = 1.2f;

        [Header("Patrol")]
        public Transform[] patrolPoints;
        public float pointReachDistance = 0.5f;

        [Header("Animation")]
        public Animator animator;

        private NavMeshAgent agent;
        private EnemyState state;
        private int patrolIndex;
        private float attackTimer;

        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>();

            if (animator == null)
                animator = GetComponentInChildren<Animator>();
        }

        private void Update()
        {
            FindPlayerIfNeeded();
            UpdateTimers();
            DecideState();
            RunState();
            UpdateAnimator();
        }

        private void FindPlayerIfNeeded()
        {
            if (player != null)
                return;

            GameObject found = GameObject.FindGameObjectWithTag("Player");
            if (found != null)
                player = found.transform;
        }

        private void UpdateTimers()
        {
            if (attackTimer > 0)
                attackTimer -= Time.deltaTime;
        }

        private void DecideState()
        {
            if (player == null)
            {
                state = patrolPoints.Length > 0 ? EnemyState.Patrol : EnemyState.Idle;
                return;
            }

            float distance = Vector3.Distance(transform.position, player.position);

            if (distance <= attackRange)
                state = EnemyState.Attack;
            else if (distance <= detectionRange)
                state = EnemyState.Chase;
            else
                state = patrolPoints.Length > 0 ? EnemyState.Patrol : EnemyState.Idle;
        }

        private void RunState()
        {
            switch (state)
            {
                case EnemyState.Idle:
                    agent.isStopped = true;
                    break;

                case EnemyState.Patrol:
                    Patrol();
                    break;

                case EnemyState.Chase:
                    Chase();
                    break;

                case EnemyState.Attack:
                    Attack();
                    break;
            }
        }

        private void Patrol()
        {
            if (patrolPoints.Length == 0)
            {
                agent.isStopped = true;
                return;
            }

            agent.isStopped = false;
            agent.SetDestination(patrolPoints[patrolIndex].position);

            if (!agent.pathPending && agent.remainingDistance <= pointReachDistance)
                patrolIndex = (patrolIndex + 1) % patrolPoints.Length;
        }

        private void Chase()
        {
            if (player == null)
                return;

            agent.isStopped = false;
            agent.SetDestination(player.position);
        }

        private void Attack()
        {
            if (player == null)
                return;

            agent.isStopped = true;

            Vector3 lookDirection = player.position - transform.position;
            lookDirection.y = 0f;

            if (lookDirection.sqrMagnitude > 0.01f)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lookDirection), 12f * Time.deltaTime);

            if (attackTimer > 0)
                return;

            attackTimer = attackCooldown;

            if (animator != null)
                animator.SetTrigger("Attack");

            Health health = player.GetComponent<Health>();
            if (health != null)
                health.TakeDamage(attackDamage);
        }

        private void UpdateAnimator()
        {
            if (animator == null || agent == null)
                return;

            animator.SetFloat("Speed", agent.velocity.magnitude);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, detectionRange);

            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, attackRange);
        }
    }
}
