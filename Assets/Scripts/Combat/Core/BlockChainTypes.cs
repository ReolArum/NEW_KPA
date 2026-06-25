using System;

namespace MemoryColoseum.Combat
{
    public enum CombatSide
    {
        Player,
        Enemy
    }

    public readonly struct BlockToken : IEquatable<BlockToken>
    {
        public BlockToken(string skillId)
        {
            SkillId = skillId;
        }

        public string SkillId { get; }

        public bool Equals(BlockToken other)
        {
            return SkillId == other.SkillId;
        }

        public override bool Equals(object obj)
        {
            return obj is BlockToken other && Equals(other);
        }

        public override int GetHashCode()
        {
            return SkillId != null ? SkillId.GetHashCode() : 0;
        }

        public override string ToString()
        {
            return SkillId ?? "<empty>";
        }
    }

    public readonly struct ChainGroup
    {
        public ChainGroup(string skillId, int startIndex, int length)
        {
            SkillId = skillId;
            StartIndex = startIndex;
            Length = length;
        }

        public string SkillId { get; }
        public int StartIndex { get; }
        public int Length { get; }
        public int EndIndex => StartIndex + Length - 1;

        public override string ToString()
        {
            return $"{SkillId} x{Length} [{StartIndex}-{EndIndex}]";
        }
    }

    public readonly struct CombatAction
    {
        public CombatAction(CombatSide side, CombatSkillDefinition skill, int chain, ChainEffectDefinition effect)
        {
            Side = side;
            Skill = skill;
            Chain = chain;
            Effect = effect;
        }

        public CombatSide Side { get; }
        public CombatSkillDefinition Skill { get; }
        public int Chain { get; }
        public ChainEffectDefinition Effect { get; }
    }

    public enum ActionPhase
    {
        Startup,
        Active,
        Recovery,
        Complete
    }

    public sealed class RunningAction
    {
        public RunningAction(CombatAction action)
        {
            Action = action;
        }

        public CombatAction Action { get; }
        public float ElapsedTime { get; private set; }
        public float Duration => Action.Effect.TotalDuration;
        public float RemainingTime => Math.Max(0f, Duration - ElapsedTime);
        public float Progress => Duration <= 0f ? 1f : Math.Min(1f, ElapsedTime / Duration);
        public float HitTime => Action.Effect.HitTime;
        public float ActiveEndTime => Action.Effect.startupDuration + Action.Effect.activeDuration;
        public ActionPhase Phase
        {
            get
            {
                if (ElapsedTime < Action.Effect.startupDuration)
                {
                    return ActionPhase.Startup;
                }

                if (ElapsedTime < Action.Effect.startupDuration + Action.Effect.activeDuration)
                {
                    return ActionPhase.Active;
                }

                if (ElapsedTime < Duration)
                {
                    return ActionPhase.Recovery;
                }

                return ActionPhase.Complete;
            }
        }

        public bool IsActive => Phase == ActionPhase.Active;
        public bool HasReachedHitTiming => ElapsedTime >= HitTime;
        public bool HasCompletedActivePhase => ElapsedTime >= ActiveEndTime;
        public bool IsComplete => Phase == ActionPhase.Complete;
        public bool HitResolved { get; set; }
        public bool ClashChecked { get; set; }

        public void Tick(float deltaTime)
        {
            ElapsedTime = Math.Min(Duration, ElapsedTime + deltaTime);
        }
    }
}
