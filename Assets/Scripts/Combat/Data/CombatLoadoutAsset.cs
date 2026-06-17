using System;
using System.Collections.Generic;
using UnityEngine;

namespace MemoryColoseum.Combat
{
    [CreateAssetMenu(menuName = "Memory Coloseum/Combat/Loadout Asset", fileName = "CombatLoadout")]
    public sealed class CombatLoadoutAsset : ScriptableObject
    {
        [SerializeField] private List<CombatSkillAsset> ownedSkills = new();
        [SerializeField] private CombatSkillAsset[] equippedSkills = new CombatSkillAsset[3];
        [SerializeField] private List<CombatGuardAsset> ownedGuards = new();
        [SerializeField] private CombatGuardAsset equippedGuard;

        public IReadOnlyList<CombatSkillAsset> OwnedSkills => ownedSkills;
        public IReadOnlyList<CombatSkillAsset> EquippedSkills => equippedSkills;
        public IReadOnlyList<CombatGuardAsset> OwnedGuards => ownedGuards;
        public CombatGuardAsset EquippedGuard => equippedGuard;

        public CombatSkillAsset GetEquippedSkill(int slotIndex)
        {
            ValidateSlot(slotIndex);
            return equippedSkills[slotIndex];
        }

        public bool TryEquipSkill(int slotIndex, CombatSkillAsset skill, out string reason)
        {
            ValidateSlot(slotIndex);

            if (skill == null)
            {
                reason = "Cannot equip an empty skill.";
                return false;
            }

            if (!ownedSkills.Contains(skill))
            {
                reason = $"{skill.DisplayName} is not owned.";
                return false;
            }

            for (int i = 0; i < equippedSkills.Length; i++)
            {
                if (i != slotIndex && equippedSkills[i] == skill)
                {
                    reason = $"{skill.DisplayName} is already equipped.";
                    return false;
                }
            }

            equippedSkills[slotIndex] = skill;
            reason = $"{skill.DisplayName} equipped to slot {slotIndex + 1}.";
            return true;
        }

        public bool TryEquipGuard(CombatGuardAsset guard, out string reason)
        {
            if (guard == null)
            {
                reason = "Cannot equip an empty guard.";
                return false;
            }

            if (!ownedGuards.Contains(guard))
            {
                reason = $"{guard.DisplayName} is not owned.";
                return false;
            }

            equippedGuard = guard;
            reason = $"{guard.DisplayName} equipped.";
            return true;
        }

        public List<CombatSkillDefinition> BuildEquippedDefinitions()
        {
            List<CombatSkillDefinition> definitions = new(equippedSkills.Length);
            for (int i = 0; i < equippedSkills.Length; i++)
            {
                CombatSkillAsset skill = equippedSkills[i];
                if (skill == null)
                {
                    throw new InvalidOperationException($"{name} has an empty loadout slot at index {i}.");
                }

                definitions.Add(skill.BuildDefinition());
            }

            return definitions;
        }

        public CombatGuardDefinition BuildEquippedGuardDefinition()
        {
            if (equippedGuard == null)
            {
                throw new InvalidOperationException($"{name} has no equipped guard.");
            }

            return equippedGuard.BuildDefinition();
        }

        private void OnValidate()
        {
            if (equippedSkills == null || equippedSkills.Length != 3)
            {
                Array.Resize(ref equippedSkills, 3);
            }

            for (int i = ownedSkills.Count - 1; i >= 0; i--)
            {
                if (ownedSkills[i] == null)
                {
                    ownedSkills.RemoveAt(i);
                }
            }

            for (int i = 0; i < equippedSkills.Length; i++)
            {
                CombatSkillAsset skill = equippedSkills[i];
                if (skill != null && !ownedSkills.Contains(skill))
                {
                    ownedSkills.Add(skill);
                }
            }

            for (int i = ownedGuards.Count - 1; i >= 0; i--)
            {
                if (ownedGuards[i] == null)
                {
                    ownedGuards.RemoveAt(i);
                }
            }

            if (equippedGuard != null && !ownedGuards.Contains(equippedGuard))
            {
                ownedGuards.Add(equippedGuard);
            }

            if (equippedGuard == null && ownedGuards.Count > 0)
            {
                equippedGuard = ownedGuards[0];
            }
        }

        private static void ValidateSlot(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= 3)
            {
                throw new ArgumentOutOfRangeException(nameof(slotIndex), slotIndex, "Loadout slot must be 0, 1, or 2.");
            }
        }
    }
}
