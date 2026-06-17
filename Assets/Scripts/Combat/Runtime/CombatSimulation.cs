using System;
using System.Collections.Generic;

namespace MemoryColoseum.Combat
{
    public enum CombatEndReason
    {
        None,
        PlayerDefeated,
        EnemyDefeated,
        TimeLimit
    }

    public enum ClashOutcome
    {
        None,
        PlayerWins,
        EnemyWins,
        MutualCancel
    }

    public sealed class ClashEventArgs : EventArgs
    {
        public ClashEventArgs(CombatAction playerAction, CombatAction enemyAction, ClashOutcome outcome)
        {
            PlayerAction = playerAction;
            EnemyAction = enemyAction;
            Outcome = outcome;
        }

        public CombatAction PlayerAction { get; }
        public CombatAction EnemyAction { get; }
        public ClashOutcome Outcome { get; }
    }

    public sealed class DamageResolvedEventArgs : EventArgs
    {
        public DamageResolvedEventArgs(CombatSide targetSide, DamageResult result)
        {
            TargetSide = targetSide;
            Result = result;
        }

        public CombatSide TargetSide { get; }
        public DamageResult Result { get; }
    }

    public sealed class CombatSimulation
    {
        private readonly EnemyBlockAi enemyAi = new();

        public CombatSimulation(int seed = 0)
        {
            Player = new CombatantRuntime(CombatSide.Player, seed + 1);
            Enemy = new CombatantRuntime(CombatSide.Enemy, seed + 2);
        }

        public event Action<CombatAction> ActionResolved;
        public event EventHandler<DamageResolvedEventArgs> DamageResolved;
        public event EventHandler<ClashEventArgs> ClashDetected;
        public event Action<CombatEndReason> CombatEnded;

        public CombatantRuntime Player { get; }
        public CombatantRuntime Enemy { get; }
        public float ElapsedTime { get; private set; }
        public float TimeLimit { get; set; } = 120f;
        public CombatEndReason EndReason { get; private set; } = CombatEndReason.None;
        public bool IsEnded => EndReason != CombatEndReason.None;

        public void Initialize(
            IReadOnlyList<CombatSkillDefinition> playerSkills,
            IReadOnlyList<CombatSkillDefinition> enemySkills,
            CombatGuardDefinition playerGuard = null,
            CombatGuardDefinition enemyGuard = null,
            float playerHp = 100f,
            float enemyHp = 100f)
        {
            ElapsedTime = 0f;
            EndReason = CombatEndReason.None;
            Player.Initialize(playerHp, playerSkills, playerGuard);
            Enemy.Initialize(enemyHp, enemySkills, enemyGuard);
        }

        public bool TryUsePlayerGroup(ChainGroup group)
        {
            if (IsEnded)
            {
                return false;
            }

            return Player.TryUseGroup(group);
        }

        public bool TryUsePlayerGuard()
        {
            if (IsEnded)
            {
                return false;
            }

            return Player.TryUseGuard();
        }

        public void Tick(float deltaTime)
        {
            if (IsEnded)
            {
                return;
            }

            ElapsedTime += deltaTime;
            Player.Tick(deltaTime);
            Enemy.Tick(deltaTime);
            enemyAi.Tick(Enemy);

            DetectClash();
            ResolveActiveHits();
            ClearCompletedActions();
            CheckEndConditions();
        }

        private void DetectClash()
        {
            RunningAction playerAction = Player.CurrentAction;
            RunningAction enemyAction = Enemy.CurrentAction;

            if (playerAction == null || enemyAction == null)
            {
                return;
            }

            if (!playerAction.IsActive || !enemyAction.IsActive)
            {
                return;
            }

            if (playerAction.HitResolved || enemyAction.HitResolved)
            {
                return;
            }

            if (playerAction.ClashChecked || enemyAction.ClashChecked)
            {
                return;
            }

            bool canClash = playerAction.Action.Effect.hasClashEffect || enemyAction.Action.Effect.hasClashEffect;
            if (!canClash)
            {
                return;
            }

            playerAction.ClashChecked = true;
            enemyAction.ClashChecked = true;
            ClashOutcome outcome = ResolveClash(playerAction.Action, enemyAction.Action);
            ClashDetected?.Invoke(this, new ClashEventArgs(playerAction.Action, enemyAction.Action, outcome));
        }

        private ClashOutcome ResolveClash(CombatAction playerAction, CombatAction enemyAction)
        {
            bool playerCanClash = playerAction.Effect.hasClashEffect;
            bool enemyCanClash = enemyAction.Effect.hasClashEffect;

            if (playerCanClash && !enemyCanClash)
            {
                Enemy.CancelCurrentAction();
                return ClashOutcome.PlayerWins;
            }

            if (enemyCanClash && !playerCanClash)
            {
                Player.CancelCurrentAction();
                return ClashOutcome.EnemyWins;
            }

            if (playerAction.Chain > enemyAction.Chain)
            {
                Enemy.CancelCurrentAction();
                return ClashOutcome.PlayerWins;
            }

            if (enemyAction.Chain > playerAction.Chain)
            {
                Player.CancelCurrentAction();
                return ClashOutcome.EnemyWins;
            }

            Player.CancelCurrentAction();
            Enemy.CancelCurrentAction();
            return ClashOutcome.MutualCancel;
        }

        private void ResolveActiveHits()
        {
            ResolveActiveHit(Player, Enemy);
            ResolveActiveHit(Enemy, Player);
        }

        private void ResolveActiveHit(CombatantRuntime actor, CombatantRuntime target)
        {
            RunningAction runningAction = actor.CurrentAction;
            if (runningAction == null || runningAction.HitResolved || !runningAction.HasCompletedActivePhase)
            {
                return;
            }

            CombatAction action = runningAction.Action;
            runningAction.HitResolved = true;
            DamageResult result = target.TakeDamage(action);
            if (result.ReflectedDamage > 0f)
            {
                actor.TakeReflectedDamage(result.ReflectedDamage);
            }

            ActionResolved?.Invoke(action);
            DamageResolved?.Invoke(this, new DamageResolvedEventArgs(target.Side, result));
        }

        private void ClearCompletedActions()
        {
            ClearCompletedAction(Player);
            ClearCompletedAction(Enemy);
        }

        private static void ClearCompletedAction(CombatantRuntime combatant)
        {
            if (combatant.CurrentAction != null && combatant.CurrentAction.IsComplete)
            {
                combatant.CompleteCurrentAction();
            }
        }

        private void CheckEndConditions()
        {
            if (Enemy.IsDefeated)
            {
                EndCombat(CombatEndReason.EnemyDefeated);
                return;
            }

            if (Player.IsDefeated)
            {
                EndCombat(CombatEndReason.PlayerDefeated);
                return;
            }

            if (ElapsedTime >= TimeLimit)
            {
                EndCombat(CombatEndReason.TimeLimit);
            }
        }

        private void EndCombat(CombatEndReason reason)
        {
            EndReason = reason;
            CombatEnded?.Invoke(reason);
        }
    }
}
