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
        public const float ItemMinimumPurchasePercentage = 0.3f;
    }
}
