using System;
using UnityEngine;

namespace MemoryColoseum.Combat
{
    public enum GuardStyle
    {
        Guard,
        DamageReduction,
        Counter
    }

    [Serializable]
    public sealed class CombatGuardDefinition
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

        public CombatGuardDefinition(
            string guardId,
            string displayName,
            GuardStyle style,
            float duration,
            float damageReductionRatio,
            float reflectDamageRatio,
            bool nullifiesFirstHit,
            bool reflectsStatusEffects)
        {
            this.guardId = guardId;
            this.displayName = displayName;
            this.style = style;
            this.duration = duration;
            this.damageReductionRatio = damageReductionRatio;
            this.reflectDamageRatio = reflectDamageRatio;
            this.nullifiesFirstHit = nullifiesFirstHit;
            this.reflectsStatusEffects = reflectsStatusEffects;
        }
    }
}
