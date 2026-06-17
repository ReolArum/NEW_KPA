using UnityEngine;

namespace MemoryColoseum.Combat
{
    [CreateAssetMenu(menuName = "Memory Coloseum/Combat/Skill Asset", fileName = "CombatSkill")]
    public sealed class CombatSkillAsset : ScriptableObject
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

        public CombatSkillDefinition BuildDefinition()
        {
            return new CombatSkillDefinition(
                skillId,
                displayName,
                family,
                Clone(chain1),
                Clone(chain2),
                Clone(chain3));
        }

        private static ChainEffectDefinition Clone(ChainEffectDefinition source)
        {
            return new ChainEffectDefinition
            {
                startupDuration = source.startupDuration,
                activeDuration = source.activeDuration,
                recoveryDuration = source.recoveryDuration,
                damage = source.damage,
                guardGaugeGain = source.guardGaugeGain,
                hasClashEffect = source.hasClashEffect,
                statusEffectId = source.statusEffectId
            };
        }
    }
}
