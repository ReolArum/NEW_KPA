using System.Collections.Generic;

namespace MemoryColoseum.Combat
{
    public sealed class EnemyBlockAi
    {
        public int MaxQueuedActions { get; set; } = 2;
        public bool PreferLongChains { get; set; } = true;

        public void Tick(CombatantRuntime enemy)
        {
            if (enemy.QueuedActionCount >= MaxQueuedActions)
            {
                return;
            }

            IReadOnlyList<ChainGroup> groups = enemy.Board.GetGroups();
            if (groups.Count == 0)
            {
                return;
            }

            ChainGroup selected = groups[0];
            foreach (ChainGroup group in groups)
            {
                bool better = PreferLongChains
                    ? group.Length > selected.Length
                    : group.StartIndex < selected.StartIndex;

                if (better)
                {
                    selected = group;
                }
            }

            enemy.TryUseGroup(selected);
        }
    }
}
