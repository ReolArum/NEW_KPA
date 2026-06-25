using System.Collections.Generic;

namespace MemoryColoseum.Combat
{
    public sealed class CombatantRuntime
    {
        private readonly Queue<CombatAction> actionQueue = new();
        private readonly Dictionary<string, CombatSkillDefinition> skillsById = new();
        private CombatGuardDefinition equippedGuard;
        private ActiveGuardState activeGuard;

        public CombatantRuntime(CombatSide side, int seed = 0)
        {
            Side = side;
            Board = new BlockChainBoard(seed);
        }

        public CombatSide Side { get; }
        public BlockChainBoard Board { get; }
        public RunningAction CurrentAction { get; private set; }
        public float MaxHp { get; private set; } = 100f;
        public float Hp { get; private set; } = 100f;
        public float GuardGauge { get; private set; }
        public float GuardGaugeToStock { get; set; } = 30f;
        public int GuardStock { get; private set; }
        public int MaxGuardStock { get; set; } = 2;
        public int QueuedActionCount => actionQueue.Count;
        public IReadOnlyList<CombatAction> QueuedActions => actionQueue.ToArray();
        public bool IsDefeated => Hp <= 0f;
        public float StunRemainingTime { get; private set; }
        public bool IsStunned => StunRemainingTime > 0f;
        public CombatGuardDefinition EquippedGuard => equippedGuard;
        public bool HasActiveGuard => activeGuard != null;
        public string ActiveGuardName => activeGuard != null ? activeGuard.Definition.DisplayName : "None";
        public float ActiveGuardRemainingTime => activeGuard != null ? activeGuard.RemainingTime : 0f;

        public void Initialize(
            float maxHp,
            IReadOnlyList<CombatSkillDefinition> equippedSkills,
            CombatGuardDefinition equippedGuardDefinition = null)
        {
            MaxHp = maxHp;
            Hp = maxHp;
            GuardGauge = 0f;
            GuardStock = 0;
            CurrentAction = null;
            StunRemainingTime = 0f;
            activeGuard = null;
            equippedGuard = equippedGuardDefinition;
            actionQueue.Clear();
            skillsById.Clear();

            foreach (CombatSkillDefinition skill in equippedSkills)
            {
                skillsById[skill.SkillId] = skill;
            }

            Board.EquipSkills(equippedSkills);
        }

        public bool TryUseGroup(ChainGroup group)
        {
            if (IsStunned)
            {
                return false;
            }

            if (!skillsById.TryGetValue(group.SkillId, out CombatSkillDefinition skill))
            {
                return false;
            }

            if (!Board.TryConsumeGroup(group))
            {
                return false;
            }

            ChainEffectDefinition effect = skill.GetEffect(group.Length);
            AddGuardGauge(effect.guardGaugeGain);
            EnqueueOrStart(new CombatAction(Side, skill, group.Length, effect));
            return true;
        }

        public bool TryUseGuard()
        {
            if (IsStunned)
            {
                return false;
            }

            if (GuardStock <= 0 || equippedGuard == null)
            {
                return false;
            }

            GuardStock--;
            activeGuard = new ActiveGuardState(equippedGuard);
            return true;
        }

        public void Tick(float deltaTime)
        {
            Board.TickSpawn(deltaTime);
            StunRemainingTime = System.Math.Max(0f, StunRemainingTime - deltaTime);
            activeGuard?.Tick(deltaTime);
            if (activeGuard != null && activeGuard.IsExpired)
            {
                activeGuard = null;
            }

            if (!IsStunned && CurrentAction == null && actionQueue.Count > 0)
            {
                CurrentAction = new RunningAction(actionQueue.Dequeue());
            }

            CurrentAction?.Tick(deltaTime);
        }

        public CombatAction CompleteCurrentAction()
        {
            CombatAction completed = CurrentAction.Action;
            CurrentAction = null;
            return completed;
        }

        public void CancelCurrentAction()
        {
            CurrentAction = null;
        }

        public void ApplyStun(float duration)
        {
            StunRemainingTime = System.Math.Max(StunRemainingTime, duration);
        }

        public DamageResult TakeDamage(CombatAction incomingAction)
        {
            float incomingDamage = incomingAction.Effect.damage;
            float finalDamage = incomingDamage;
            float reflectedDamage = 0f;
            string reflectedStatusEffectId = null;
            string guardName = null;

            if (activeGuard != null)
            {
                guardName = activeGuard.Definition.DisplayName;
                CombatGuardDefinition guard = activeGuard.Definition;

                if (guard.NullifiesFirstHit)
                {
                    finalDamage = 0f;
                    activeGuard.Consume();
                }
                else if (guard.DamageReductionRatio > 0f)
                {
                    finalDamage = incomingDamage * (1f - guard.DamageReductionRatio);
                }

                if (guard.ReflectDamageRatio > 0f)
                {
                    reflectedDamage = incomingDamage * guard.ReflectDamageRatio;
                }

                if (guard.ReflectsStatusEffects && !string.IsNullOrWhiteSpace(incomingAction.Effect.statusEffectId))
                {
                    reflectedStatusEffectId = incomingAction.Effect.statusEffectId;
                }

                if (activeGuard != null && activeGuard.IsConsumed)
                {
                    activeGuard = null;
                }
            }

            Hp = System.Math.Max(0f, Hp - finalDamage);
            return new DamageResult(incomingAction, incomingDamage, finalDamage, reflectedDamage, guardName, reflectedStatusEffectId);
        }

        public void TakeReflectedDamage(float damage)
        {
            Hp = System.Math.Max(0f, Hp - damage);
        }

        private void EnqueueOrStart(CombatAction action)
        {
            if (CurrentAction == null)
            {
                CurrentAction = new RunningAction(action);
                return;
            }

            actionQueue.Enqueue(action);
        }

        private void AddGuardGauge(float amount)
        {
            GuardGauge += amount;

            while (GuardGauge >= GuardGaugeToStock && GuardStock < MaxGuardStock)
            {
                GuardGauge -= GuardGaugeToStock;
                GuardStock++;
            }

            if (GuardStock >= MaxGuardStock)
            {
                GuardGauge = System.Math.Min(GuardGauge, GuardGaugeToStock - 0.001f);
            }
        }
    }

    public readonly struct DamageResult
    {
        public DamageResult(
            CombatAction sourceAction,
            float originalDamage,
            float finalDamage,
            float reflectedDamage,
            string guardName,
            string reflectedStatusEffectId)
        {
            SourceAction = sourceAction;
            OriginalDamage = originalDamage;
            FinalDamage = finalDamage;
            ReflectedDamage = reflectedDamage;
            GuardName = guardName;
            ReflectedStatusEffectId = reflectedStatusEffectId;
        }

        public CombatAction SourceAction { get; }
        public float OriginalDamage { get; }
        public float FinalDamage { get; }
        public float ReflectedDamage { get; }
        public string GuardName { get; }
        public string ReflectedStatusEffectId { get; }
        public bool WasGuarded => !string.IsNullOrEmpty(GuardName);
    }

    public sealed class ActiveGuardState
    {
        public ActiveGuardState(CombatGuardDefinition definition)
        {
            Definition = definition;
            RemainingTime = definition.Duration;
        }

        public CombatGuardDefinition Definition { get; }
        public float RemainingTime { get; private set; }
        public bool IsConsumed { get; private set; }
        public bool IsExpired => RemainingTime <= 0f;

        public void Tick(float deltaTime)
        {
            RemainingTime = System.Math.Max(0f, RemainingTime - deltaTime);
        }

        public void Consume()
        {
            IsConsumed = true;
            RemainingTime = 0f;
        }
    }
}
