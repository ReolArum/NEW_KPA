using UnityEngine;

namespace MemoryColoseum.Combat
{
    [CreateAssetMenu(menuName = "Memory Coloseum/Combat/Guard Asset", fileName = "CombatGuard")]
    public sealed class CombatGuardAsset : ScriptableObject
    {
        [SerializeField] private string guardId;
        [SerializeField] private string displayName;
        [SerializeField] private GuardStyle style;
        [SerializeField, Min(0.05f)] private float duration = 1f;
        [SerializeField, Range(0f, 1f)] private float damageReductionRatio;
        [SerializeField, Range(0f, 1f)] private float reflectDamageRatio;
        [SerializeField] private bool nullifiesFirstHit;
        [SerializeField] private bool reflectsStatusEffects;

        public string GuardId => guardId;
        public string DisplayName => displayName;
        public GuardStyle Style => style;
        public float Duration => duration;
        public float DamageReductionRatio => damageReductionRatio;
        public float ReflectDamageRatio => reflectDamageRatio;
        public bool NullifiesFirstHit => nullifiesFirstHit;
        public bool ReflectsStatusEffects => reflectsStatusEffects;

        public CombatGuardDefinition BuildDefinition()
        {
            return new CombatGuardDefinition(
                guardId,
                displayName,
                style,
                duration,
                damageReductionRatio,
                reflectDamageRatio,
                nullifiesFirstHit,
                reflectsStatusEffects);
        }
    }
}
