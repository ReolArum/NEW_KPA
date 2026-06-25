using System;
using UnityEngine;

namespace MemoryColoseum.Combat
{
    public enum SkillFamily
    {
        Jab,
        Heavy,
        Focus,
        Guard,
        Special
    }

    [Serializable]
    public sealed class ChainEffectDefinition
    {
        [Min(0f)] public float startupDuration = 0.36f;
        [Min(0.01f)] public float activeDuration = 0.12f;
        [Min(0f)] public float recoveryDuration = 0.12f;
        [Range(0f, 1f)] public float hitTiming = 0.35f;
        [Min(0f)] public float damage = 5f;
        [Min(0f)] public float guardGaugeGain = 0f;
        public bool hasClashEffect;
        public string statusEffectId;

        public float TotalDuration => startupDuration + activeDuration + recoveryDuration;
        public float HitTime => startupDuration + activeDuration * hitTiming;
    }

    [Serializable]
    public sealed class CombatSkillDefinition
    {
        [SerializeField] private string skillId;
        [SerializeField] private string displayName;
        [SerializeField] private SkillFamily family;
        [SerializeField] private ChainEffectDefinition chain1 = new();
        [SerializeField] private ChainEffectDefinition chain2 = new();
        [SerializeField] private ChainEffectDefinition chain3 = new();

        public string SkillId => skillId;
        public string DisplayName => displayName;
        public SkillFamily Family => family;

        public CombatSkillDefinition(
            string skillId,
            string displayName,
            SkillFamily family,
            ChainEffectDefinition chain1,
            ChainEffectDefinition chain2,
            ChainEffectDefinition chain3)
        {
            this.skillId = skillId;
            this.displayName = displayName;
            this.family = family;
            this.chain1 = chain1;
            this.chain2 = chain2;
            this.chain3 = chain3;
        }

        public ChainEffectDefinition GetEffect(int chain)
        {
            return chain switch
            {
                1 => chain1,
                2 => chain2,
                3 => chain3,
                _ => throw new ArgumentOutOfRangeException(nameof(chain), chain, "Chain must be 1, 2, or 3.")
            };
        }
    }
}
