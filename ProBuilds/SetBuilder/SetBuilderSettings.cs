using ProBuilds.BuildPath;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProBuilds.SetBuilder
{
    public static class SetBuilderSettings
    {
        /// <summary>
        /// The minimum percentage of matches read that must include a champion in
        /// a particular role for that item set to be included.
        /// </summary>
        public const double FilterMatchMinPercentage = 0.002;

        /// <summary>
        /// Specifies the minimum number of matches required to create a reasonable
        /// item set. The lower of MinMatchPercentage and AbsoluteMinCount will be
        /// used, in case there aren't enough matches available to meet this value.
        /// </summary>
        public const long FilterMatchMinCount = 50;

        /// <summary>
        /// Whether or not smiteless jungle should be excluded from resulting sets.
        /// We assume this combination is created from roaming champions, misidentified
        /// as junglers.
        /// </summary>
        public const bool FilterExcludeNoSmiteJungle = true;

        /// <summary>
        /// Minimum percentage of times an item must be built to be included in a block.
        /// </summary>
        public static Dictionary<GameStage, float> ItemMinimumPurchasePercentage = new Dictionary<GameStage, float>()
        {
            { GameStage.Start, 0.4f },
            { GameStage.Early, 0.3f },
            { GameStage.Mid, 0.25f },
            { GameStage.Late, 0.1f }
        };

        /// <summary>
        /// Upgrades from a base item that should always be included.
        /// </summary>
        public const int BuildPathAlwaysIncludeCount = 1;

        /// <summary>
        /// Minimum percentage of times an item must be upgraded to in order to include.
        /// </summary>
        /// <remarks>This is above the "always include at least one" requirement.</remarks>
        public const float BuildPathItemMinimumPurchasePercentage = 0.25f;

        /// <summary>
        /// Whether or not an item was purchased, on average, during the start of the game.
        /// </summary>
        public static bool IsStartPurchase(ItemPurchaseTrackerData tracker)
        {
            return
                tracker.AveragePurchaseTimeSeconds <= 90.0 &&
                tracker.AverageKills < 1.0f &&
                tracker.AverageTowerKills < 1.0;
        }

        /// <summary>
        /// Whether or not an item was purchased, on average, during the early stage of the game.
        /// </summary>
        public static bool IsEarlyPurchase(ItemPurchaseTrackerData tracker)
        {
            return
                !IsStartPurchase(tracker) &&
                tracker.AverageTowerKills < 1.0f;
        }

        /// <summary>
        /// Whether or not an item was purchased, on average, during the mid stage of the game.
        /// </summary>
        public static bool IsMidPurchase(ItemPurchaseTrackerData tracker)
        {
            return
                !IsStartPurchase(tracker) &&
                !IsEarlyPurchase(tracker) &&
                tracker.AverageInnerTowerKills < 2.5f && // Getting too close to 3 puts a lot of items into the mid-game bucket
                tracker.AverageBaseTowerKills < 1.0f;
        }

        /// <summary>
        /// Whether or not an item was purchased, on average, during the late stage of the game.
        /// </summary>
        public static bool IsLatePurchase(ItemPurchaseTrackerData tracker)
        {
            return
                !IsStartPurchase(tracker) &&
                !IsEarlyPurchase(tracker) &&
                !IsMidPurchase(tracker);
        }

        /// <summary>
        /// Gets the game stage a purchase took place in.
        /// </summary>
        public static GameStage GetGameStage(ItemPurchaseTrackerData tracker)
        {
            return
                IsStartPurchase(tracker) ? GameStage.Start :
                IsEarlyPurchase(tracker) ? GameStage.Early :
                IsMidPurchase(tracker) ? GameStage.Mid :
                GameStage.Late;
        }
    }
}
