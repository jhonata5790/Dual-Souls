using UnityEngine;

namespace DualSouls.Combat
{
    public class DamageDealer3D : MonoBehaviour
    {
        public int damage = 10;
        public LayerMask targetLayer;
        public bool destroyAfterHit = false;

        private void OnTriggerEnter(Collider other)
        {
            if (((1 << other.gameObject.layer) & targetLayer) == 0)
                return;

            Health health = other.GetComponentInParent<Health>();
            if (health != null)
            {
                health.TakeDamage(damage);

                if (destroyAfterHit)
                    Destroy(gameObject);
            }
        }
    }
}
