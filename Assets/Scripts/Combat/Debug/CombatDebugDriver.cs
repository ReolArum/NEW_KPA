using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MemoryColoseum.Combat
{
    public sealed class CombatDebugDriver : MonoBehaviour
    {
        [SerializeField] private CombatLoadoutAsset playerLoadout;
        [SerializeField] private CombatLoadoutAsset enemyLoadout;
        [SerializeField] private CombatHudView hudView;

        private CombatSimulation simulation;
        private List<CombatSkillDefinition> playerSkills;
        private List<CombatSkillDefinition> enemySkills;
        private readonly List<string> eventLog = new();
        private string statusText;
        private string loadoutStatus;
        private int selectedLoadoutSlot;

        private void Awake()
        {
            EnsureLoadouts();
            EnsureHud();
            BindHud();
            RestartCombat();
        }

        private void Update()
        {
            simulation.Tick(Time.deltaTime);
            HandleKeyboard();
            RefreshHud();
        }

        private void RestartCombat()
        {
            simulation = new CombatSimulation(42);
            playerSkills = playerLoadout.BuildEquippedDefinitions();
            enemySkills = enemyLoadout.BuildEquippedDefinitions();
            eventLog.Clear();
            statusText = "Arena live. Click a block to consume its chain. Space uses equipped guard.";
            loadoutStatus = "Loadout ready.";
            simulation.Initialize(
                playerSkills,
                enemySkills,
                playerLoadout.BuildEquippedGuardDefinition(),
                enemyLoadout.BuildEquippedGuardDefinition());
            simulation.ClashDetected += (_, args) =>
            {
                AppendLog($"Clash {args.Outcome}: {args.PlayerAction.Skill.DisplayName} x{args.PlayerAction.Chain} vs {args.EnemyAction.Skill.DisplayName} x{args.EnemyAction.Chain}");
                hudView?.ShowClashFeedback(args.Outcome);
            };
            simulation.ActionResolved += action =>
            {
                AppendLog($"{action.Side} resolved {action.Skill.DisplayName} x{action.Chain}");
            };
            simulation.DamageResolved += (_, args) =>
            {
                DamageResult result = args.Result;
                string guard = result.WasGuarded ? $" guarded by {result.GuardName}," : string.Empty;
                string reflected = result.ReflectedDamage > 0f ? $", reflected {result.ReflectedDamage:0.#}" : string.Empty;
                string status = !string.IsNullOrEmpty(result.ReflectedStatusEffectId) ? $", reflected status {result.ReflectedStatusEffectId}" : string.Empty;
                AppendLog($"{args.TargetSide}{guard} took {result.FinalDamage:0.#}/{result.OriginalDamage:0.#} damage{reflected}{status}");
                hudView?.ShowDamageFeedback(args.TargetSide, result);
            };
            simulation.CombatEnded += reason =>
            {
                statusText = $"Combat ended: {reason}. Press R to restart.";
                AppendLog(statusText);
            };
            RefreshHud();
        }

        private void BindHud()
        {
            if (hudView == null)
            {
                return;
            }

            hudView.Bind(
                SelectLoadoutSlot,
                TryEquipOwnedSkill,
                TryEquipOwnedGuard,
                TryUsePlayerGroup,
                TryUsePlayerGuard,
                RestartCombat);
        }

        private void HandleKeyboard()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            if (keyboard.digit1Key.wasPressedThisFrame)
            {
                TryUsePlayerGroup(1);
            }
            else if (keyboard.digit2Key.wasPressedThisFrame)
            {
                TryUsePlayerGroup(2);
            }
            else if (keyboard.digit3Key.wasPressedThisFrame)
            {
                TryUsePlayerGroup(3);
            }
            else if (keyboard.spaceKey.wasPressedThisFrame)
            {
                TryUsePlayerGuard();
            }
            else if (keyboard.lKey.wasPressedThisFrame)
            {
                TogglePlayerLoadout();
            }
            else if (keyboard.rKey.wasPressedThisFrame)
            {
                RestartCombat();
            }
        }

        private void TogglePlayerLoadout()
        {
            if (hudView == null)
            {
                return;
            }

            hudView.ToggleLoadoutVisible();
            statusText = hudView.IsLoadoutVisible ? "Player loadout opened." : "Player loadout hidden. Press L to open it.";
            RefreshHud();
        }

        private void SelectLoadoutSlot(int slotIndex)
        {
            selectedLoadoutSlot = slotIndex;
            loadoutStatus = $"Slot {slotIndex + 1} selected.";
            RefreshHud();
        }

        private void TryEquipOwnedSkill(int ownedSkillIndex)
        {
            if (ownedSkillIndex < 0 || ownedSkillIndex >= playerLoadout.OwnedSkills.Count)
            {
                return;
            }

            CombatSkillAsset skill = playerLoadout.OwnedSkills[ownedSkillIndex];
            if (playerLoadout.TryEquipSkill(selectedLoadoutSlot, skill, out string reason))
            {
                loadoutStatus = reason;
                RestartCombat();
                return;
            }

            loadoutStatus = reason;
            RefreshHud();
        }

        private void TryEquipOwnedGuard(int ownedGuardIndex)
        {
            if (ownedGuardIndex < 0 || ownedGuardIndex >= playerLoadout.OwnedGuards.Count)
            {
                return;
            }

            CombatGuardAsset guard = playerLoadout.OwnedGuards[ownedGuardIndex];
            if (playerLoadout.TryEquipGuard(guard, out string reason))
            {
                loadoutStatus = reason;
                RestartCombat();
                return;
            }

            loadoutStatus = reason;
            RefreshHud();
        }

        private void TryUsePlayerGroup(int chain)
        {
            foreach (ChainGroup group in simulation.Player.Board.GetGroups())
            {
                if (group.Length == chain && simulation.TryUsePlayerGroup(group))
                {
                    statusText = $"Player used {group}";
                    AppendLog(statusText);
                    RefreshHud();
                    return;
                }
            }

            statusText = simulation.Player.IsStunned
                ? $"Player stunned for {simulation.Player.StunRemainingTime:0.00}s."
                : $"No x{chain} chain available.";
            RefreshHud();
        }

        private void TryUsePlayerGroup(ChainGroup group)
        {
            if (simulation.TryUsePlayerGroup(group))
            {
                statusText = $"Player used {group}";
                AppendLog(statusText);
            }

            RefreshHud();
        }

        private void TryUsePlayerGuard()
        {
            if (simulation.TryUsePlayerGuard())
            {
                statusText = $"{playerLoadout.EquippedGuard.DisplayName} activated.";
                AppendLog(statusText);
            }
            else
            {
                statusText = simulation.Player.IsStunned
                    ? $"Player stunned for {simulation.Player.StunRemainingTime:0.00}s."
                    : "No guard stock available.";
            }

            RefreshHud();
        }

        private void EnsureLoadouts()
        {
            if (playerLoadout == null)
            {
                playerLoadout = Resources.Load<CombatLoadoutAsset>("Combat/Loadouts/DebugPlayerLoadout");
            }

            if (enemyLoadout == null)
            {
                enemyLoadout = Resources.Load<CombatLoadoutAsset>("Combat/Loadouts/DebugEnemyLoadout");
            }

            if (playerLoadout == null || enemyLoadout == null)
            {
                throw new UnityException("Default combat loadout assets could not be loaded from Resources/Combat/Loadouts.");
            }
        }

        private void EnsureHud()
        {
            if (hudView == null)
            {
                hudView = FindAnyObjectByType<CombatHudView>();
            }
        }

        private void RefreshHud()
        {
            if (hudView == null)
            {
                return;
            }

            hudView.Render(simulation, playerLoadout, eventLog, selectedLoadoutSlot, statusText, loadoutStatus);
        }

        private void AppendLog(string message)
        {
            eventLog.Insert(0, $"[{simulation.ElapsedTime:0.0}] {message}");
            if (eventLog.Count > 12)
            {
                eventLog.RemoveAt(eventLog.Count - 1);
            }
        }
    }
}
