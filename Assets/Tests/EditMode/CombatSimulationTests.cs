using System.Collections.Generic;
using System.Reflection;
using MemoryColoseum.Combat;
using NUnit.Framework;
using UnityEngine;

namespace MemoryColoseum.Tests
{
    public sealed class CombatSimulationTests
    {
        [Test]
        public void GroupsSplitRunsIntoMaximumThreeChains()
        {
            BlockChainBoard board = new();
            board.SetSlotsForTest("jab", "jab", "jab", "jab", "jab", "jab", "jab", "jab", "jab", "jab");

            IReadOnlyList<ChainGroup> groups = board.GetGroups();

            Assert.That(groups, Has.Count.EqualTo(4));
            Assert.That(groups[0].Length, Is.EqualTo(1));
            Assert.That(groups[1].Length, Is.EqualTo(3));
            Assert.That(groups[2].Length, Is.EqualTo(3));
            Assert.That(groups[3].Length, Is.EqualTo(3));
        }

        [Test]
        public void ConsumingGroupRemovesMatchingSlots()
        {
            BlockChainBoard board = new();
            board.SetSlotsForTest("jab", "jab", "heavy");

            Assert.That(board.TryConsumeGroup(new ChainGroup("jab", 0, 2)), Is.True);
            Assert.That(board.Slots, Has.Count.EqualTo(1));
            Assert.That(board.Slots[0].SkillId, Is.EqualTo("heavy"));
        }

        [Test]
        public void TestSlotsCanRepresentNewestBlockOnTheLeft()
        {
            BlockChainBoard board = new();
            board.SetSlotsForTest("newest", "older", "oldest");

            Assert.That(board.Slots[0].SkillId, Is.EqualTo("newest"));
            Assert.That(board.Slots[2].SkillId, Is.EqualTo("oldest"));
        }

        [Test]
        public void EnemyUsesSameBlockGroupsAutomatically()
        {
            CombatSimulation simulation = new(7);
            simulation.Initialize(MakeSkills("P"), MakeSkills("E"));
            simulation.Enemy.Board.SetSlotsForTest("E_jab", "E_jab", "E_jab");

            simulation.Tick(0.1f);

            Assert.That(simulation.Enemy.Board.Slots, Has.Count.EqualTo(0));
            Assert.That(simulation.Enemy.CurrentAction, Is.Not.Null);
            Assert.That(simulation.Enemy.CurrentAction.Action.Chain, Is.EqualTo(3));
        }

        [Test]
        public void ClashEventFiresWhenOverlappingActionHasClashEffect()
        {
            CombatSimulation simulation = new(7);
            simulation.Initialize(MakeSkills("P"), MakeSkills("E"));
            simulation.Player.Board.SetSlotsForTest("P_heavy", "P_heavy", "P_heavy");
            simulation.Enemy.Board.SetSlotsForTest("E_heavy", "E_heavy", "E_heavy");
            bool clashDetected = false;
            ClashOutcome outcome = ClashOutcome.None;
            simulation.ClashDetected += (_, args) =>
            {
                clashDetected = true;
                outcome = args.Outcome;
            };

            Assert.That(simulation.TryUsePlayerGroup(new ChainGroup("P_heavy", 0, 3)), Is.True);
            Assert.That(simulation.Enemy.TryUseGroup(new ChainGroup("E_heavy", 0, 3)), Is.True);
            simulation.Tick(0.06f);

            Assert.That(clashDetected, Is.True);
            Assert.That(outcome, Is.EqualTo(ClashOutcome.MutualCancel));
            Assert.That(simulation.Player.CurrentAction, Is.Null);
            Assert.That(simulation.Enemy.CurrentAction, Is.Null);
        }

        [Test]
        public void ClashSkillCancelsNonClashSkill()
        {
            CombatSimulation simulation = new(7);
            simulation.Initialize(MakeSkills("P"), MakeSkills("E"));
            simulation.Player.Board.SetSlotsForTest("P_heavy", "P_heavy", "P_heavy");
            simulation.Enemy.Board.SetSlotsForTest("E_jab", "E_jab", "E_jab");
            ClashOutcome outcome = ClashOutcome.None;
            simulation.ClashDetected += (_, args) => outcome = args.Outcome;

            Assert.That(simulation.TryUsePlayerGroup(new ChainGroup("P_heavy", 0, 3)), Is.True);
            Assert.That(simulation.Enemy.TryUseGroup(new ChainGroup("E_jab", 0, 3)), Is.True);
            simulation.Tick(0.06f);

            Assert.That(outcome, Is.EqualTo(ClashOutcome.PlayerWins));
            Assert.That(simulation.Player.CurrentAction, Is.Not.Null);
            Assert.That(simulation.Enemy.CurrentAction, Is.Null);
            Assert.That(simulation.Enemy.IsStunned, Is.True);
        }

        [Test]
        public void HigherChainWinsWhenBothActionsCanClash()
        {
            CombatSimulation simulation = new(7);
            simulation.Initialize(MakeSkills("P"), MakeSkills("E"));
            simulation.Player.Board.SetSlotsForTest("P_heavy", "P_heavy", "P_heavy");
            simulation.Enemy.Board.SetSlotsForTest("E_heavy", "E_heavy");
            ClashOutcome outcome = ClashOutcome.None;
            simulation.ClashDetected += (_, args) => outcome = args.Outcome;

            Assert.That(simulation.TryUsePlayerGroup(new ChainGroup("P_heavy", 0, 3)), Is.True);
            Assert.That(simulation.Enemy.TryUseGroup(new ChainGroup("E_heavy", 0, 2)), Is.True);
            simulation.Tick(0.06f);

            Assert.That(outcome, Is.EqualTo(ClashOutcome.PlayerWins));
            Assert.That(simulation.Player.CurrentAction, Is.Not.Null);
            Assert.That(simulation.Enemy.CurrentAction, Is.Null);
            Assert.That(simulation.Enemy.IsStunned, Is.True);
        }

        [Test]
        public void DamageResolvesAtHitTimingBeforeActivePhaseEnds()
        {
            CombatSimulation simulation = new(7);
            simulation.Initialize(MakeSkills("P"), MakeSkills("E"));
            simulation.Player.Board.SetSlotsForTest("P_jab");
            bool damageResolved = false;
            simulation.DamageResolved += (_, _) => damageResolved = true;

            Assert.That(simulation.TryUsePlayerGroup(new ChainGroup("P_jab", 0, 1)), Is.True);
            simulation.Tick(0.09f);

            Assert.That(damageResolved, Is.True);
            Assert.That(simulation.Enemy.Hp, Is.EqualTo(99f));
            Assert.That(simulation.Player.CurrentAction.IsActive, Is.True);
        }

        [Test]
        public void DamageWaitsUntilHitTiming()
        {
            CombatSimulation simulation = new(7);
            simulation.Initialize(MakeSkills("P"), MakeSkills("E"));
            simulation.Player.Board.SetSlotsForTest("P_jab");

            Assert.That(simulation.TryUsePlayerGroup(new ChainGroup("P_jab", 0, 1)), Is.True);
            simulation.Tick(0.07f);

            Assert.That(simulation.Enemy.Hp, Is.EqualTo(100f));
            Assert.That(simulation.Player.CurrentAction.HitResolved, Is.False);
        }

        [Test]
        public void StunnedCombatantCannotStartActionsButBoardKeepsSpawning()
        {
            CombatantRuntime player = new(CombatSide.Player, 1);
            player.Initialize(100f, MakeSkills("P"));
            player.Board.BaseSpawnInterval = 0.1f;
            player.ApplyStun(0.3f);
            player.Board.SetSlotsForTest("P_jab", "P_jab", "P_jab");

            Assert.That(player.TryUseGroup(new ChainGroup("P_jab", 0, 3)), Is.False);
            player.Tick(0.11f);

            Assert.That(player.CurrentAction, Is.Null);
            Assert.That(player.Board.Slots, Has.Count.GreaterThan(3));
            Assert.That(player.IsStunned, Is.True);
        }

        [Test]
        public void QueuedActionsResumeAfterStunEnds()
        {
            CombatantRuntime player = new(CombatSide.Player, 1);
            player.Initialize(100f, MakeSkills("P"));
            player.Board.SetSlotsForTest("P_jab", "P_jab", "P_jab", "P_focus");

            Assert.That(player.TryUseGroup(new ChainGroup("P_jab", 0, 3)), Is.True);
            Assert.That(player.TryUseGroup(new ChainGroup("P_focus", 0, 1)), Is.True);
            player.CancelCurrentAction();
            player.ApplyStun(0.2f);
            player.Tick(0.1f);

            Assert.That(player.CurrentAction, Is.Null);
            Assert.That(player.QueuedActionCount, Is.EqualTo(1));

            player.Tick(0.11f);

            Assert.That(player.IsStunned, Is.False);
            Assert.That(player.CurrentAction, Is.Not.Null);
            Assert.That(player.CurrentAction.Action.Skill.SkillId, Is.EqualTo("P_focus"));
        }

        [Test]
        public void LoadoutRejectsDuplicateEquipsAcrossSlots()
        {
            CombatLoadoutAsset loadout = ScriptableObject.CreateInstance<CombatLoadoutAsset>();
            CombatSkillAsset skillA = ScriptableObject.CreateInstance<CombatSkillAsset>();
            CombatSkillAsset skillB = ScriptableObject.CreateInstance<CombatSkillAsset>();

            SetPrivateField(loadout, "ownedSkills", new List<CombatSkillAsset> { skillA, skillB });
            SetPrivateField(loadout, "equippedSkills", new[] { skillA, skillB, (CombatSkillAsset)null });

            bool didEquip = loadout.TryEquipSkill(2, skillA, out string reason);

            Assert.That(didEquip, Is.False);
            Assert.That(reason, Does.Contain("already equipped"));
        }

        [Test]
        public void BarrierGuardNullifiesOneHitAndEnds()
        {
            CombatantRuntime player = MakeRuntimeWithGuard(MakeGuard("barrier", GuardStyle.Guard, 1.5f, 0f, 0f, true, false));
            SetPrivateField(player, "<GuardStock>k__BackingField", 1);

            Assert.That(player.TryUseGuard(), Is.True);
            DamageResult result = player.TakeDamage(MakeAction(10f));

            Assert.That(result.FinalDamage, Is.EqualTo(0f));
            Assert.That(player.Hp, Is.EqualTo(100f));
            Assert.That(player.HasActiveGuard, Is.False);
        }

        [Test]
        public void DamageReductionGuardReducesDamageWhileActive()
        {
            CombatantRuntime player = MakeRuntimeWithGuard(MakeGuard("reduction", GuardStyle.DamageReduction, 3f, 0.4f, 0f, false, false));
            SetPrivateField(player, "<GuardStock>k__BackingField", 1);

            Assert.That(player.TryUseGuard(), Is.True);
            DamageResult result = player.TakeDamage(MakeAction(10f));

            Assert.That(result.FinalDamage, Is.EqualTo(6f));
            Assert.That(player.Hp, Is.EqualTo(94f));
            Assert.That(player.HasActiveGuard, Is.True);
        }

        [Test]
        public void CounterGuardNullifiesAndReflectsDamageAndStatus()
        {
            CombatantRuntime player = MakeRuntimeWithGuard(MakeGuard("counter", GuardStyle.Counter, 1f, 0f, 0.3f, true, true));
            SetPrivateField(player, "<GuardStock>k__BackingField", 1);

            Assert.That(player.TryUseGuard(), Is.True);
            DamageResult result = player.TakeDamage(MakeAction(10f, "burn"));

            Assert.That(result.FinalDamage, Is.EqualTo(0f));
            Assert.That(result.ReflectedDamage, Is.EqualTo(3f));
            Assert.That(result.ReflectedStatusEffectId, Is.EqualTo("burn"));
            Assert.That(player.HasActiveGuard, Is.False);
        }

        private static List<CombatSkillDefinition> MakeSkills(string prefix)
        {
            return new List<CombatSkillDefinition>
            {
                MakeSkill($"{prefix}_jab", false),
                MakeSkill($"{prefix}_heavy", true),
                MakeSkill($"{prefix}_focus", false)
            };
        }

        private static CombatSkillDefinition MakeSkill(string id, bool clash)
        {
            return new CombatSkillDefinition(
                id,
                id,
                SkillFamily.Jab,
                new ChainEffectDefinition { startupDuration = 0.05f, activeDuration = 0.1f, recoveryDuration = 0.05f, damage = 1f, guardGaugeGain = 1f, hasClashEffect = clash },
                new ChainEffectDefinition { startupDuration = 0.05f, activeDuration = 0.1f, recoveryDuration = 0.05f, damage = 2f, guardGaugeGain = 3f, hasClashEffect = clash },
                new ChainEffectDefinition { startupDuration = 0.05f, activeDuration = 0.1f, recoveryDuration = 0.05f, damage = 3f, guardGaugeGain = 6f, hasClashEffect = clash });
        }

        private static CombatantRuntime MakeRuntimeWithGuard(CombatGuardDefinition guard)
        {
            CombatantRuntime runtime = new(CombatSide.Player, 1);
            runtime.Initialize(100f, MakeSkills("P"), guard);
            return runtime;
        }

        private static CombatGuardDefinition MakeGuard(
            string id,
            GuardStyle style,
            float duration,
            float damageReductionRatio,
            float reflectDamageRatio,
            bool nullifiesFirstHit,
            bool reflectsStatusEffects)
        {
            return new CombatGuardDefinition(
                id,
                id,
                style,
                duration,
                damageReductionRatio,
                reflectDamageRatio,
                nullifiesFirstHit,
                reflectsStatusEffects);
        }

        private static CombatAction MakeAction(float damage, string statusEffectId = null)
        {
            ChainEffectDefinition effect = new()
            {
                startupDuration = 0.02f,
                activeDuration = 0.04f,
                recoveryDuration = 0.04f,
                damage = damage,
                statusEffectId = statusEffectId
            };
            CombatSkillDefinition skill = new("incoming", "Incoming", SkillFamily.Jab, effect, effect, effect);
            return new CombatAction(CombatSide.Enemy, skill, 1, effect);
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            field.SetValue(target, value);
        }
    }
}
