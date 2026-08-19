using UnityEngine;

namespace DualSouls.Abilities
{
    public abstract class AbilityBase : MonoBehaviour
    {
        public string abilityName;

        [TextArea(2, 6)]
        public string description;

        public float cooldown = 1f;
        protected float cooldownTimer;

        public bool IsOnCooldown => cooldownTimer > 0f;

        protected virtual void Update()
        {
            if (cooldownTimer > 0f)
                cooldownTimer -= Time.deltaTime;
        }

        public abstract void Activate();

        protected void StartCooldown()
        {
            cooldownTimer = cooldown;
        }
    }
}
