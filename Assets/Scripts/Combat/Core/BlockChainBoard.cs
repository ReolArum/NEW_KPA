using System;
using System.Collections.Generic;
using UnityEngine;

namespace MemoryColoseum.Combat
{
    public sealed class BlockChainBoard
    {
        private readonly List<BlockToken> slots = new();
        private readonly List<BlockToken> bag = new();
        private readonly List<CombatSkillDefinition> equippedSkills = new();
        private readonly System.Random random;
        private float spawnAccumulator;

        public BlockChainBoard(int seed = 0)
        {
            random = seed == 0 ? new System.Random() : new System.Random(seed);
        }

        public IReadOnlyList<BlockToken> Slots => slots;
        public int SlotCapacity { get; set; } = 10;
        public int BlocksPerSkill { get; set; } = 6;
        public float BaseSpawnInterval { get; set; } = 1f;
        public float SpawnSpeedBonus { get; set; }

        public void EquipSkills(IReadOnlyList<CombatSkillDefinition> skills)
        {
            if (skills == null)
            {
                throw new ArgumentNullException(nameof(skills));
            }

            if (skills.Count != 3)
            {
                throw new ArgumentException("Combat board requires exactly 3 equipped skills.", nameof(skills));
            }

            equippedSkills.Clear();
            equippedSkills.AddRange(skills);
            slots.Clear();
            RebuildBag();
        }

        public void TickSpawn(float deltaTime)
        {
            if (equippedSkills.Count == 0 || slots.Count >= SlotCapacity)
            {
                return;
            }

            spawnAccumulator += deltaTime;
            float interval = GetSpawnInterval();

            while (spawnAccumulator >= interval && slots.Count < SlotCapacity)
            {
                spawnAccumulator -= interval;
                SpawnOneBlock();
            }
        }

        public float GetSpawnInterval()
        {
            float clampedBonus = Mathf.Clamp(SpawnSpeedBonus, 0f, 2f);
            return BaseSpawnInterval / (1f + clampedBonus);
        }

        public IReadOnlyList<ChainGroup> GetGroups()
        {
            List<ChainGroup> groups = new();
            int runStart = 0;

            while (runStart < slots.Count)
            {
                string skillId = slots[runStart].SkillId;
                int runEnd = runStart + 1;

                while (runEnd < slots.Count && slots[runEnd].SkillId == skillId)
                {
                    runEnd++;
                }

                int remaining = runEnd - runStart;
                int cursor = runStart;
                int firstLength = remaining % 3;

                if (firstLength == 0 && remaining > 0)
                {
                    firstLength = 3;
                }

                if (firstLength > 0)
                {
                    groups.Add(new ChainGroup(skillId, cursor, firstLength));
                    cursor += firstLength;
                    remaining -= firstLength;
                }

                while (remaining > 0)
                {
                    groups.Add(new ChainGroup(skillId, cursor, Math.Min(3, remaining)));
                    cursor += 3;
                    remaining -= 3;
                }

                runStart = runEnd;
            }

            return groups;
        }

        public bool TryConsumeGroup(ChainGroup group)
        {
            if (group.StartIndex < 0 || group.Length < 1 || group.Length > 3)
            {
                return false;
            }

            if (group.EndIndex >= slots.Count)
            {
                return false;
            }

            for (int index = group.StartIndex; index <= group.EndIndex; index++)
            {
                if (slots[index].SkillId != group.SkillId)
                {
                    return false;
                }
            }

            slots.RemoveRange(group.StartIndex, group.Length);
            return true;
        }

        public void SetSlotsForTest(params string[] skillIds)
        {
            slots.Clear();
            foreach (string skillId in skillIds)
            {
                slots.Add(new BlockToken(skillId));
            }
        }

        private void SpawnOneBlock()
        {
            if (bag.Count == 0)
            {
                RebuildBag();
            }

            int index = random.Next(0, bag.Count);
            slots.Insert(0, bag[index]);
            bag.RemoveAt(index);
        }

        private void RebuildBag()
        {
            bag.Clear();

            foreach (CombatSkillDefinition skill in equippedSkills)
            {
                for (int i = 0; i < BlocksPerSkill; i++)
                {
                    bag.Add(new BlockToken(skill.SkillId));
                }
            }
        }
    }
}
