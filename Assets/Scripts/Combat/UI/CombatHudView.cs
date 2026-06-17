using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MemoryColoseum.Combat
{
    public sealed class CombatHudView : MonoBehaviour
    {
        [Header("Combatants")]
        [SerializeField] private Text playerHpText;
        [SerializeField] private Slider playerHpSlider;
        [SerializeField] private Text playerGuardText;
        [SerializeField] private Slider playerGuardSlider;
        [SerializeField] private Text playerActionText;
        [SerializeField] private Slider playerCastSlider;
        [SerializeField] private Text playerQueuedText;
        [SerializeField] private Text playerQueueListText;
        [SerializeField] private Text playerSlotsText;
        [SerializeField] private Text enemyHpText;
        [SerializeField] private Slider enemyHpSlider;
        [SerializeField] private Text enemyGuardText;
        [SerializeField] private Slider enemyGuardSlider;
        [SerializeField] private Text enemyActionText;
        [SerializeField] private Slider enemyCastSlider;
        [SerializeField] private Text enemyQueuedText;
        [SerializeField] private Text enemyQueueListText;
        [SerializeField] private Text enemySlotsText;

        [Header("Status")]
        [SerializeField] private Text timeText;
        [SerializeField] private Text statusText;
        [SerializeField] private Text logText;

        [Header("Combat Feedback")]
        [SerializeField] private Text playerFeedbackText;
        [SerializeField] private Text enemyFeedbackText;
        [SerializeField] private float feedbackDuration = 0.9f;
        [SerializeField] private Color damageFeedbackColor = new(1f, 0.32f, 0.28f, 1f);
        [SerializeField] private Color guardFeedbackColor = new(0.35f, 0.8f, 1f, 1f);
        [SerializeField] private Color clashFeedbackColor = new(1f, 0.86f, 0.25f, 1f);

        [Header("Loadout")]
        [SerializeField] private GameObject loadoutPanelRoot;
        [SerializeField] private Button loadoutShortcutButton;
        [SerializeField] private Text loadoutShortcutLabel;
        [SerializeField] private Text loadoutStatusText;
        [SerializeField] private Button[] loadoutSlotButtons = new Button[3];
        [SerializeField] private Text[] loadoutSlotLabels = new Text[3];
        [SerializeField] private Image[] loadoutSlotImages = new Image[3];
        [SerializeField] private Button[] ownedSkillButtons = Array.Empty<Button>();
        [SerializeField] private Text[] ownedSkillLabels = Array.Empty<Text>();
        [SerializeField] private Text equippedGuardLabel;
        [SerializeField] private Button[] ownedGuardButtons = Array.Empty<Button>();
        [SerializeField] private Text[] ownedGuardLabels = Array.Empty<Text>();
        [SerializeField] private Image[] ownedGuardImages = Array.Empty<Image>();
        [SerializeField] private Color selectedSlotColor = new(0.18f, 0.7f, 1f, 1f);
        [SerializeField] private Color normalSlotColor = new(0.18f, 0.2f, 0.25f, 1f);

        [Header("Board")]
        [SerializeField] private Button[] blockButtons = new Button[10];
        [SerializeField] private Text[] blockLabels = new Text[10];
        [SerializeField] private Text[] blockChainLabels = new Text[10];
        [SerializeField] private Image[] blockImages = new Image[10];
        [SerializeField] private Color filledBlockColor = new(0.21f, 0.66f, 0.8f, 1f);
        [SerializeField] private Color emptyBlockColor = new(0.16f, 0.17f, 0.2f, 0.7f);

        [Header("Commands")]
        [SerializeField] private Button guardButton;
        [SerializeField] private Button restartButton;

        private Action<int> slotSelected;
        private Action<int> ownedSkillSelected;
        private Action<int> ownedGuardSelected;
        private Action<ChainGroup> blockGroupSelected;
        private Action guardRequested;
        private Action restartRequested;
        private CombatLoadoutAsset currentPlayerLoadout;
        private string normalStatusText;
        private string hoverStatusText;
        private bool isLoadoutVisible = true;
        private float playerFeedbackTimer;
        private float enemyFeedbackTimer;

        public bool IsLoadoutVisible => isLoadoutVisible;

        private void Awake()
        {
            EnsureLoadoutPanelRoot();
            SetLoadoutVisible(isLoadoutVisible);
            HideFeedback(playerFeedbackText);
            HideFeedback(enemyFeedbackText);
        }

        private void Update()
        {
            TickFeedback(playerFeedbackText, ref playerFeedbackTimer);
            TickFeedback(enemyFeedbackText, ref enemyFeedbackTimer);
        }

        public void Bind(
            Action<int> onSlotSelected,
            Action<int> onOwnedSkillSelected,
            Action<int> onOwnedGuardSelected,
            Action<ChainGroup> onBlockGroupSelected,
            Action onGuardRequested,
            Action onRestartRequested)
        {
            slotSelected = onSlotSelected;
            ownedSkillSelected = onOwnedSkillSelected;
            ownedGuardSelected = onOwnedGuardSelected;
            blockGroupSelected = onBlockGroupSelected;
            guardRequested = onGuardRequested;
            restartRequested = onRestartRequested;

            BindIndexedButtons(loadoutSlotButtons, slotSelected);
            BindIndexedButtons(ownedSkillButtons, ownedSkillSelected);
            BindIndexedButtons(ownedGuardButtons, ownedGuardSelected);

            if (loadoutShortcutButton != null)
            {
                loadoutShortcutButton.onClick.RemoveAllListeners();
                loadoutShortcutButton.onClick.AddListener(ToggleLoadoutVisible);
            }

            if (guardButton != null)
            {
                guardButton.onClick.RemoveAllListeners();
                guardButton.onClick.AddListener(() => guardRequested?.Invoke());
            }

            if (restartButton != null)
            {
                restartButton.onClick.RemoveAllListeners();
                restartButton.onClick.AddListener(() => restartRequested?.Invoke());
            }
        }

        public void Render(
            CombatSimulation simulation,
            CombatLoadoutAsset playerLoadout,
            IReadOnlyList<string> eventLog,
            int selectedLoadoutSlot,
            string currentStatus,
            string currentLoadoutStatus)
        {
            if (simulation == null || playerLoadout == null)
            {
                return;
            }

            currentPlayerLoadout = playerLoadout;
            normalStatusText = currentStatus;

            RenderCombatant(simulation.Player, playerHpText, playerHpSlider, playerGuardText, playerGuardSlider, playerActionText, playerCastSlider, playerQueuedText, playerQueueListText, playerSlotsText);
            RenderCombatant(simulation.Enemy, enemyHpText, enemyHpSlider, enemyGuardText, enemyGuardSlider, enemyActionText, enemyCastSlider, enemyQueuedText, enemyQueueListText, enemySlotsText);
            SetText(timeText, $"Time {simulation.ElapsedTime:0.0}s / {simulation.TimeLimit:0}s");
            SetText(statusText, hoverStatusText ?? currentStatus);
            SetText(loadoutStatusText, currentLoadoutStatus);
            RenderLoadout(playerLoadout, selectedLoadoutSlot);
            RenderBoard(simulation.Player.Board);
            RenderLog(eventLog);
        }

        public void ToggleLoadoutVisible()
        {
            SetLoadoutVisible(!isLoadoutVisible);
        }

        public void SetLoadoutVisible(bool isVisible)
        {
            isLoadoutVisible = isVisible;
            EnsureLoadoutPanelRoot();
            if (loadoutPanelRoot != null)
            {
                loadoutPanelRoot.SetActive(isLoadoutVisible);
            }

            SetText(loadoutShortcutLabel, isLoadoutVisible ? "LOAD" : "OPEN");
        }

        public void ShowDamageFeedback(CombatSide targetSide, DamageResult result)
        {
            Text targetText = targetSide == CombatSide.Player ? playerFeedbackText : enemyFeedbackText;
            ref float timer = ref GetFeedbackTimer(targetSide);
            Color color = result.WasGuarded ? guardFeedbackColor : damageFeedbackColor;

            string message = result.FinalDamage <= 0f
                ? "NO DAMAGE"
                : $"-{result.FinalDamage:0.#}";

            if (result.ReflectedDamage > 0f)
            {
                message += $"\nREFLECT {result.ReflectedDamage:0.#}";
            }

            if (!string.IsNullOrEmpty(result.ReflectedStatusEffectId))
            {
                message += "\nSTATUS REFLECT";
            }

            ShowFeedback(targetText, message, color, ref timer);
        }

        public void ShowClashFeedback(ClashOutcome outcome)
        {
            switch (outcome)
            {
                case ClashOutcome.PlayerWins:
                    ShowFeedback(enemyFeedbackText, "CLASH WIN", clashFeedbackColor, ref enemyFeedbackTimer);
                    ShowFeedback(playerFeedbackText, "CLASH", clashFeedbackColor, ref playerFeedbackTimer);
                    break;
                case ClashOutcome.EnemyWins:
                    ShowFeedback(playerFeedbackText, "CLASH LOSE", clashFeedbackColor, ref playerFeedbackTimer);
                    ShowFeedback(enemyFeedbackText, "CLASH", clashFeedbackColor, ref enemyFeedbackTimer);
                    break;
                case ClashOutcome.MutualCancel:
                    ShowFeedback(playerFeedbackText, "CLASH\nCANCEL", clashFeedbackColor, ref playerFeedbackTimer);
                    ShowFeedback(enemyFeedbackText, "CLASH\nCANCEL", clashFeedbackColor, ref enemyFeedbackTimer);
                    break;
            }
        }

        private ref float GetFeedbackTimer(CombatSide side)
        {
            if (side == CombatSide.Player)
            {
                return ref playerFeedbackTimer;
            }

            return ref enemyFeedbackTimer;
        }

        private void ShowFeedback(Text target, string message, Color color, ref float timer)
        {
            if (target == null)
            {
                return;
            }

            target.text = message;
            target.color = color;
            target.gameObject.SetActive(true);
            timer = feedbackDuration;
        }

        private void TickFeedback(Text target, ref float timer)
        {
            if (target == null || timer <= 0f)
            {
                return;
            }

            timer = Mathf.Max(0f, timer - Time.deltaTime);
            Color color = target.color;
            color.a = feedbackDuration <= 0f ? 0f : Mathf.Clamp01(timer / feedbackDuration);
            target.color = color;

            if (timer <= 0f)
            {
                HideFeedback(target);
            }
        }

        private static void HideFeedback(Text target)
        {
            if (target != null)
            {
                target.gameObject.SetActive(false);
            }
        }

        private void EnsureLoadoutPanelRoot()
        {
            if (loadoutPanelRoot == null && loadoutStatusText != null)
            {
                loadoutPanelRoot = loadoutStatusText.transform.parent.gameObject;
            }
        }

        private void RenderCombatant(
            CombatantRuntime runtime,
            Text hpText,
            Slider hpSlider,
            Text guardText,
            Slider guardSlider,
            Text actionText,
            Slider castSlider,
            Text queuedText,
            Text queueListText,
            Text slotsText)
        {
            SetText(hpText, $"HP {runtime.Hp:0}/{runtime.MaxHp:0}");
            SetSlider(hpSlider, runtime.MaxHp <= 0f ? 0f : runtime.Hp / runtime.MaxHp);
            SetText(guardText, $"Guard {runtime.GuardStock}/{runtime.MaxGuardStock}  Gauge {runtime.GuardGauge:0.0}/{runtime.GuardGaugeToStock:0}");
            SetSlider(guardSlider, runtime.GuardGaugeToStock <= 0f ? 0f : runtime.GuardGauge / runtime.GuardGaugeToStock);
            string activeGuard = runtime.HasActiveGuard ? $"  Guard: {runtime.ActiveGuardName} ({runtime.ActiveGuardRemainingTime:0.00}s)" : string.Empty;
            SetText(actionText, $"Action: {DescribeRunningAction(runtime.CurrentAction)}{activeGuard}");
            SetSlider(castSlider, runtime.CurrentAction != null ? runtime.CurrentAction.Progress : 0f);
            SetText(queuedText, $"Queued: {runtime.QueuedActionCount}");
            SetText(queueListText, DescribeQueue(runtime.QueuedActions));
            SetText(slotsText, $"Slots: {DescribeSlots(runtime.Board)}");
        }

        private void RenderLoadout(CombatLoadoutAsset loadout, int selectedSlot)
        {
            for (int i = 0; i < loadoutSlotButtons.Length; i++)
            {
                CombatSkillAsset equipped = i < 3 ? loadout.GetEquippedSkill(i) : null;
                SetText(Get(loadoutSlotLabels, i), $"Slot {i + 1}\n{(equipped != null ? equipped.DisplayName : "Empty")}");
                Image image = Get(loadoutSlotImages, i);
                if (image != null)
                {
                    image.color = i == selectedSlot ? selectedSlotColor : normalSlotColor;
                }
            }

            for (int i = 0; i < ownedSkillButtons.Length; i++)
            {
                bool hasSkill = i < loadout.OwnedSkills.Count;
                SetActive(ownedSkillButtons[i], hasSkill);
                SetText(Get(ownedSkillLabels, i), hasSkill ? loadout.OwnedSkills[i].DisplayName : string.Empty);
            }

            SetText(equippedGuardLabel, loadout.EquippedGuard != null ? $"Guard: {loadout.EquippedGuard.DisplayName}" : "Guard: Empty");
            for (int i = 0; i < ownedGuardButtons.Length; i++)
            {
                bool hasGuard = i < loadout.OwnedGuards.Count;
                CombatGuardAsset guard = hasGuard ? loadout.OwnedGuards[i] : null;
                SetActive(ownedGuardButtons[i], hasGuard);
                SetText(Get(ownedGuardLabels, i), guard != null ? guard.DisplayName : string.Empty);

                Image image = Get(ownedGuardImages, i);
                if (image != null && guard != null)
                {
                    image.color = guard == loadout.EquippedGuard ? selectedSlotColor : normalSlotColor;
                }
            }
        }

        private void RenderBoard(BlockChainBoard board)
        {
            IReadOnlyList<BlockToken> slots = board.Slots;
            IReadOnlyList<ChainGroup> groups = board.GetGroups();
            int capacity = Math.Min(blockButtons.Length, board.SlotCapacity);
            int startColumn = capacity - slots.Count;

            for (int i = 0; i < blockButtons.Length; i++)
            {
                Button button = blockButtons[i];
                int logicalIndex = i - startColumn;
                bool hasBlock = i < capacity && logicalIndex >= 0 && logicalIndex < slots.Count;
                ChainGroup? group = hasBlock ? FindGroupAtIndex(groups, logicalIndex) : null;

                if (button != null)
                {
                    button.onClick.RemoveAllListeners();
                    button.interactable = group.HasValue;
                    if (group.HasValue)
                    {
                        ChainGroup selectedGroup = group.Value;
                        button.onClick.AddListener(() => blockGroupSelected?.Invoke(selectedGroup));
                    }

                    BindBlockHover(button, group);
                }

                SetText(Get(blockLabels, i), hasBlock ? ShortNameFor(slots[logicalIndex].SkillId) : string.Empty);
                SetText(Get(blockChainLabels, i), group.HasValue && group.Value.StartIndex == logicalIndex ? $"x{group.Value.Length}" : string.Empty);

                Image image = Get(blockImages, i);
                if (image != null)
                {
                    image.color = hasBlock ? filledBlockColor : emptyBlockColor;
                }
            }
        }

        private void RenderLog(IReadOnlyList<string> eventLog)
        {
            StringBuilder builder = new();
            foreach (string line in eventLog)
            {
                builder.AppendLine(line);
            }

            SetText(logText, builder.ToString());
        }

        private void BindBlockHover(Button button, ChainGroup? group)
        {
            EventTrigger trigger = button.GetComponent<EventTrigger>();
            if (trigger == null)
            {
                trigger = button.gameObject.AddComponent<EventTrigger>();
            }

            trigger.triggers.Clear();
            if (!group.HasValue)
            {
                return;
            }

            ChainGroup selectedGroup = group.Value;
            EventTrigger.Entry enter = new()
            {
                eventID = EventTriggerType.PointerEnter
            };
            enter.callback.AddListener(_ => ShowBlockInfo(selectedGroup));
            trigger.triggers.Add(enter);

            EventTrigger.Entry exit = new()
            {
                eventID = EventTriggerType.PointerExit
            };
            exit.callback.AddListener(_ => HideBlockInfo());
            trigger.triggers.Add(exit);
        }

        private void ShowBlockInfo(ChainGroup group)
        {
            hoverStatusText = BuildBlockInfo(group);
            SetText(statusText, hoverStatusText);
        }

        private void HideBlockInfo()
        {
            hoverStatusText = null;
            SetText(statusText, normalStatusText);
        }

        private string BuildBlockInfo(ChainGroup group)
        {
            CombatSkillDefinition skill = FindPlayerSkill(group.SkillId);
            if (skill == null)
            {
                return $"{group.SkillId} x{group.Length}";
            }

            ChainEffectDefinition effect = skill.GetEffect(group.Length);
            string clash = effect.hasClashEffect ? " / Clash" : string.Empty;
            return $"{skill.DisplayName} x{group.Length} [{skill.Family}]  Damage {effect.damage:0.#} / Startup {effect.startupDuration:0.##}s / Active {effect.activeDuration:0.##}s / Recovery {effect.recoveryDuration:0.##}s / Guard +{effect.guardGaugeGain:0.#}{clash}";
        }

        private CombatSkillDefinition FindPlayerSkill(string skillId)
        {
            if (currentPlayerLoadout == null)
            {
                return null;
            }

            foreach (CombatSkillAsset skill in currentPlayerLoadout.EquippedSkills)
            {
                if (skill != null && skill.SkillId == skillId)
                {
                    return skill.BuildDefinition();
                }
            }

            return null;
        }

        private static void BindIndexedButtons(Button[] buttons, Action<int> callback)
        {
            for (int i = 0; i < buttons.Length; i++)
            {
                Button button = buttons[i];
                if (button == null)
                {
                    continue;
                }

                int index = i;
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => callback?.Invoke(index));
            }
        }

        private static ChainGroup? FindGroupAtIndex(IReadOnlyList<ChainGroup> groups, int slotIndex)
        {
            foreach (ChainGroup group in groups)
            {
                if (slotIndex >= group.StartIndex && slotIndex <= group.EndIndex)
                {
                    return group;
                }
            }

            return null;
        }

        private static string DescribeRunningAction(RunningAction action)
        {
            return action == null
                ? "Idle"
                : $"{action.Action.Skill.DisplayName} x{action.Action.Chain} [{action.Phase}] ({action.RemainingTime:0.00}s)";
        }

        private static string DescribeQueue(IReadOnlyList<CombatAction> queuedActions)
        {
            if (queuedActions == null || queuedActions.Count == 0)
            {
                return "Queue: Empty";
            }

            StringBuilder builder = new("Queue: ");
            int count = Math.Min(queuedActions.Count, 3);
            for (int i = 0; i < count; i++)
            {
                if (i > 0)
                {
                    builder.Append(" > ");
                }

                CombatAction action = queuedActions[i];
                builder.Append(action.Skill.DisplayName);
                builder.Append(" x");
                builder.Append(action.Chain);
            }

            if (queuedActions.Count > count)
            {
                builder.Append($" +{queuedActions.Count - count}");
            }

            return builder.ToString();
        }

        private static string DescribeSlots(BlockChainBoard board)
        {
            StringBuilder builder = new();
            IReadOnlyList<BlockToken> slots = board.Slots;

            for (int i = 0; i < slots.Count; i++)
            {
                if (i > 0)
                {
                    builder.Append(' ');
                }

                builder.Append(ShortNameFor(slots[i].SkillId));
            }

            return builder.Length == 0 ? "Empty" : builder.ToString();
        }

        private static string ShortNameFor(string skillId)
        {
            if (skillId.Contains("jab"))
            {
                return "JAB";
            }

            if (skillId.Contains("heavy"))
            {
                return "HVY";
            }

            if (skillId.Contains("focus"))
            {
                return "FOC";
            }

            if (skillId.Contains("counter"))
            {
                return "CTR";
            }

            if (skillId.Contains("guard"))
            {
                return "BRK";
            }

            return skillId.ToUpperInvariant();
        }

        private static void SetText(Text target, string value)
        {
            if (target != null)
            {
                target.text = value;
            }
        }

        private static void SetSlider(Slider target, float value)
        {
            if (target != null)
            {
                target.value = Mathf.Clamp01(value);
            }
        }

        private static void SetActive(Component target, bool isActive)
        {
            if (target != null)
            {
                target.gameObject.SetActive(isActive);
            }
        }

        private static T Get<T>(T[] items, int index) where T : class
        {
            return index >= 0 && index < items.Length ? items[index] : null;
        }
    }
}
