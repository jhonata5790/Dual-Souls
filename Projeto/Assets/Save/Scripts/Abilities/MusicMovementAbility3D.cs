using UnityEngine;
using DualSouls.Player;

namespace DualSouls.Abilities
{
    public class MusicMovementAbility3D : AbilityBase
    {
        public enum MusicStyle
        {
            Calm,
            Heavy,
            Upbeat,
            Mixed
        }

        [Header("Music Style")]
        public MusicStyle selectedStyle = MusicStyle.Calm;

        [Header("Timing")]
        public float syncDuration = 1f;
        public float buffDuration = 8f;

        [Header("References")]
        public PlayerStats stats;
        public PlayerController3D controller;

        private bool isSyncing;
        private bool buffActive;
        private float syncTimer;
        private float buffTimer;

        private float originalWalkSpeed;
        private float originalRunSpeed;

        private void Awake()
        {
            abilityName = "Movimentos Artísticos";

            if (stats == null)
                stats = GetComponent<PlayerStats>();

            if (controller == null)
                controller = GetComponent<PlayerController3D>();

            if (controller != null)
            {
                originalWalkSpeed = controller.walkSpeed;
                originalRunSpeed = controller.runSpeed;
            }
        }

        protected override void Update()
        {
            base.Update();

            if (isSyncing)
            {
                syncTimer -= Time.deltaTime;

                if (syncTimer <= 0f)
                {
                    isSyncing = false;
                    ApplyStyle(GetResolvedStyle());
                }
            }

            if (buffActive)
            {
                buffTimer -= Time.deltaTime;

                if (buffTimer <= 0f)
                    EndBuff();
            }
        }

        public override void Activate()
        {
            if (stats == null || IsOnCooldown)
                return;

            isSyncing = true;
            syncTimer = syncDuration;

            Debug.Log("Sincronizando com a batida...");
        }

        private MusicStyle GetResolvedStyle()
        {
            if (selectedStyle != MusicStyle.Mixed)
                return selectedStyle;

            int roll = Random.Range(0, 3);
            return (MusicStyle)roll;
        }

        private void ApplyStyle(MusicStyle style)
        {
            stats.ClearTemporaryBonuses();

            if (controller != null)
            {
                controller.walkSpeed = originalWalkSpeed;
                controller.runSpeed = originalRunSpeed;
            }

            switch (style)
            {
                case MusicStyle.Calm:
                    stats.reflexesBonus += 5;
                    stats.perceptionBonus += 5;
                    stats.stealthBonus += 5;
                    stats.acrobaticsBonus += 5;

                    if (controller != null)
                    {
                        controller.walkSpeed = originalWalkSpeed + 0.8f;
                        controller.runSpeed = originalRunSpeed + 0.8f;
                    }

                    Debug.Log("Movimentos Artísticos: Música Calma");
                    break;

                case MusicStyle.Heavy:
                    stats.willBonus += 5;
                    stats.intimidationBonus += 5;
                    stats.AddTemporaryHealth(10);
                    Debug.Log("Movimentos Artísticos: Música Grave");
                    break;

                case MusicStyle.Upbeat:
                    stats.fightBonus += 5;
                    stats.artsBonus += 5;
                    stats.diplomacyBonus += 5;
                    stats.deceptionBonus += 5;
                    stats.useAgilityForMelee = true;

                    if (controller != null)
                    {
                        controller.walkSpeed = originalWalkSpeed + 1.2f;
                        controller.runSpeed = originalRunSpeed + 1.2f;
                    }

                    Debug.Log("Movimentos Artísticos: Música Animada");
                    break;
            }

            buffActive = true;
            buffTimer = buffDuration;
        }

        private void EndBuff()
        {
            buffActive = false;
            stats.ClearTemporaryBonuses();

            if (controller != null)
            {
                controller.walkSpeed = originalWalkSpeed;
                controller.runSpeed = originalRunSpeed;
            }

            StartCooldown();
            Debug.Log("Movimentos Artísticos acabou.");
        }
    }
}
