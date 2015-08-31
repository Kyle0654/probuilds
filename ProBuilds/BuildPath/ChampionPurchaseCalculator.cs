using ProBuilds.SetBuilder;
using RiotSharp.StaticDataEndpoint;
using System.Collections.Generic;
using System.Linq;

namespace ProBuilds.BuildPath
{
    public enum GameStage
    {
        Start = 0,
        Early = 1,
        Mid = 2,
        Late = 3
    }

    /// <summary>
    /// Calculates which items should be purchased.
    /// </summary>
    public class ChampionPurchaseCalculator
    {
        /// <summary>
        /// Class to track whether or not an item should be included.
        /// </summary>
        private class IncludeDetermination
        {
            public ItemPurchaseStats Stats;
            public GameStage GameStage;
            public bool Include;

            public override string ToString()
            {
                return string.Format("({0},{1}) {2} [{3}]", Stats.ItemId, Stats.Number, GameStage.ToString(), Include.ToString());
            }
        }

        public PurchaseSetKey Key;

        public int ChampionId { get { return Key.ChampionId; } }
        public long MatchCount;

        /// <summary>
        /// All purchases that should be included in the final item set.
        /// </summary>
        public Dictionary<GameStage, List<ItemPurchaseStats>> Purchases;

        public ChampionPurchaseCalculator(PurchaseSet set)
        {
            Key = set.Key;

            MatchCount = set.MatchCount;

            // Create stats
            var stats = PurchaseStats.Create(set.AllItemPurchases, set.MatchCount);

            // Compute initial determinations based on game stage and settings percentages
            var statsList = stats.SelectMany(kvp => kvp.Value.Select(nkvp => nkvp.Value));
            var purchaseDeterminations = statsList.Select(purchaseStats => new IncludeDetermination()
            {
                Stats = purchaseStats,
                GameStage = SetBuilderSettings.GetGameStage(purchaseStats),
                Include = false
            }).ToDictionary(
                determination => new ItemPurchaseTrackerData.ItemPurchaseKey(determination.Stats)
            );

            // Filter initial include list by percentage
            purchaseDeterminations.Values
                .Where(determination => determination.Stats.Percentage >= SetBuilderSettings.ItemMinimumPurchasePercentage[determination.GameStage])
                .ToList()
                .ForEach(determination => determination.Include = true);

            // Compute build path includes
            DetermineBuildPathIncludes(purchaseDeterminations);

            // Generate stage-based include list
            Purchases = purchaseDeterminations.Values
                .Where(determination => determination.Include)
                .GroupBy(determination => determination.GameStage)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(d => d.Stats).OrderBy(s => s.AveragePurchaseTimeSeconds).ToList()
                );

            // If early items contain items that build into a mid-game item, migrate the mid-game item to early game
            if (Purchases.ContainsKey(GameStage.Early) && Purchases.ContainsKey(GameStage.Mid))
            {
                // Check if a full build path exists in the early stage already
                bool hasFullEarlyPath = Purchases[GameStage.Early].Any(p =>
                    p.FinalBuildItemPercentage.Any(finalItem => Purchases[GameStage.Early].Any(i => finalItem.Key.ItemId == i.ItemId)));

                if (!hasFullEarlyPath)
                {
                    // Find the first early game item that builds into something in mid-game
                    var earlyItem = Purchases[GameStage.Early].FirstOrDefault(p =>
                        p.FinalBuildItemPercentage.Any(finalItem => Purchases[GameStage.Mid].Any(i => finalItem.Key.ItemId == i.ItemId)));

                    if (earlyItem != null)
                    {
                        // Find the mid-game item to move
                        var midItemIndex = Purchases[GameStage.Mid].FindIndex(i => earlyItem.FinalBuildItemPercentage.Any(kvp => kvp.Key.ItemId == i.ItemId));
                        var midItem = Purchases[GameStage.Mid][midItemIndex];

                        // Move the mid-game item
                        Purchases[GameStage.Early].Add(midItem);
                        Purchases[GameStage.Mid].RemoveAt(midItemIndex);
                    }
                }
            }

            // If start items add up to less than 475, try to absorb first early item
            if (Purchases.ContainsKey(GameStage.Start) && Purchases.ContainsKey(GameStage.Early))
            {
                int startCost = Purchases[GameStage.Start].Sum(d => StaticDataStore.Items.Items[d.ItemId].Gold.TotalPrice);
                if (startCost < 475)
                {
                    ItemPurchaseStats d = Purchases[GameStage.Early].FirstOrDefault();
                    if (StaticDataStore.Items.Items[d.ItemId].Gold.TotalPrice + startCost <= 475)
                    {
                        // Move item
                        Purchases[GameStage.Start].Add(d);
                        Purchases[GameStage.Early].RemoveAt(0);
                    }
                }
            }

            // Move consumables to end of each section
            foreach (GameStage stage in Purchases.Keys)
            {
                // Remove all consumables, sort by cost, then add to end of list
                var purchaseList = Purchases[stage];
                var consumables = purchaseList.Where(d => StaticDataStore.Items.Items[d.ItemId].Consumed).ToList();
                purchaseList.RemoveAll(d => consumables.Contains(d));
                consumables.Sort((a, b) =>
                    {
                        int cmp = StaticDataStore.Items.Items[a.ItemId].Gold.TotalPrice.CompareTo(
                            StaticDataStore.Items.Items[b.ItemId].Gold.TotalPrice);

                        if (cmp != 0)
                            return cmp;

                        return a.ItemId.CompareTo(b.ItemId);
                    }
                );
                purchaseList.AddRange(consumables);
            }

            // Remove components in mid/late game
            var removeComponentsStages = new[] { GameStage.Late, GameStage.Mid };
            IEnumerable<ItemStatic> componentItems = Enumerable.Empty<ItemStatic>();
            foreach (GameStage stage in removeComponentsStages)
            {
                if (!Purchases.ContainsKey(stage))
                    continue;

                var allComponents = Purchases[stage]
                    .Select(p => StaticDataStore.Items.Items[p.ItemId])
                    .Where(i => i.Into == null || i.Into.Count == 0)
                    .SelectMany(i => i.AllComponents())
                    .Where(i => i.Tags == null || (!i.Tags.Contains("Boots") && !i.Tags.Contains("Trinket")))
                    .Distinct();

                componentItems = componentItems.Concat(allComponents).Distinct().ToList();

                // Remove any component items
                Purchases[stage].RemoveAll(i => componentItems.Any(c => c.Id == i.ItemId));
            }
        }

        /// <summary>
        /// Marks build-path items as included based on SetBuilderSettings.
        /// </summary>
        private void DetermineBuildPathIncludes(Dictionary<ItemPurchaseTrackerData.ItemPurchaseKey, IncludeDetermination> determinations)
        {
            // Include build-path items
            Queue<ItemPurchaseTrackerData.ItemPurchaseKey> toProcess = new Queue<ItemPurchaseTrackerData.ItemPurchaseKey>(
                determinations
                    .Where(determination => determination.Value.Include)
                    .Select(determination => determination.Key)
            );

            while (toProcess.Count > 0)
            {
                var key = toProcess.Dequeue();

                // Find the determination
                IncludeDetermination determination;
                if (!determinations.TryGetValue(key, out determination))
                    return;

                // Any determination we're processing should be included
                determination.Include = true;

                // Include as many build path items as the settings say we should always include
                if (SetBuilderSettings.BuildPathAlwaysIncludeCount != 0)
                {
                    determination.Stats.BuiltInto
                        .OrderBy(kvp => kvp.Value).Reverse() // Order by count in descending order
                        .Take(SetBuilderSettings.BuildPathAlwaysIncludeCount)
                        .Where(kvp => !toProcess.Contains(kvp.Key))
                        .Where(kvp => !determinations[kvp.Key].Include) // Don't re-process anything we've processed already
                        .ToList()
                        .ForEach(item =>
                        {
                            toProcess.Enqueue(item.Key);
                        });
                }

                // Include any build path items that haven't already been included, and that are above the minimum include percentage
                determination.Stats.BuiltIntoPercentage
                    .Where(kvp => !toProcess.Contains(kvp.Key))
                    .Where(kvp => !determinations[kvp.Key].Include) // Don't re-process anything we've processed already
                    .Where(kvp => kvp.Value >= SetBuilderSettings.BuildPathItemMinimumPurchasePercentage)
                    .ToList()
                    .ForEach(item =>
                    {
                        toProcess.Enqueue(item.Key);
                    });
            }
        }
    }
}