using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

public static class InventorySort
{
    public static void Sort(List<SItemStack> items, IReadOnlyList<int> regularSlots)
    {
        var indices = regularSlots.Where(i => i >= 0 && i < items.Count).Distinct().OrderBy(i => i).ToList();
        var stacks = new List<SItemStack>();
        foreach (int i in indices)
        {
            var item = items[i];
            if (SItemStack.IsEmpty(item)) continue;
            int max = Math.Max(1, item.itemBaseType.maxStack);
            if (max > 1)
            {
                foreach (var previous in stacks)
                {
                    if (previous.id != item.id || previous.amount >= max) continue;
                    int move = Math.Min(max - previous.amount, item.amount);
                    previous.amount += move;
                    item.amount -= move;
                    if (item.amount == 0) break;
                }
            }
            if (item.amount > 0) stacks.Add(item);
        }
        stacks = stacks.OrderBy(s => s.itemBaseType.categories)
            .ThenBy(s => s.itemBaseType.typeName, StringComparer.Create(new CultureInfo("ko-KR"), false)).ToList();
        for (int i = 0; i < indices.Count; i++) items[indices[i]] = i < stacks.Count ? stacks[i] : null;
    }
}
