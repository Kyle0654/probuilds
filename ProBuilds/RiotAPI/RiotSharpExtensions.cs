using RiotSharp;
using RiotSharp.StaticDataEndpoint;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProBuilds
{
    /// <summary>
    /// Extensions for RiotSharp
    /// </summary>
    static class RiotSharpExtensions
    {
        public static bool IsRetryable(this RiotSharpException ex)
        {
            return (ex.Message.StartsWith("429") || ex.Message.StartsWith("5"));
        }

        /// <summary>
        /// Gets a champion by champion id.
        /// </summary>
        public static ChampionStatic GetChampionById(this ChampionListStatic list, int id)
        {
            string key;
            if (!list.Keys.TryGetValue(id, out key))
                return null;

            ChampionStatic champion;
            if (!list.Champions.TryGetValue(key, out champion))
                return null;

            return champion;
        }

        /// <summary>
        /// Whether or not this item builds into the specified item.
        /// </summary>
        public static bool BuildsInto(this ItemStatic component, ItemStatic item)
        {
            if (component.Into == null)
                return false;

            // NOTE: While using item.from would be faster if it were ints, it contains strings,
            //       so we'd have to parse them every time. The item trees aren't deep enough that
            //       traversing into is much slower.
            return component.Into.Any(i =>
            {
                if (item.Id == i)
                    return true;

                ItemStatic componentItem;
                if (!StaticDataStore.Items.Items.TryGetValue(i, out componentItem))
                {
                    return false;
                }

                return componentItem.BuildsInto(item);
            });
        }

        /// <summary>
        /// Gets the final build paths of this item.
        /// </summary>
        /// <param name="component"></param>
        public static IEnumerable<ItemStatic> FinalBuilds(this ItemStatic component)
        {
            if (component.Into == null)
                return Enumerable.Empty<ItemStatic>();

            var builds = component.Into.Select(id => StaticDataStore.Items.Items[id]);

            return builds.Where(item => item.Into == null || item.Into.Count == 0).Concat(
                builds.SelectMany(comp => comp.FinalBuilds())
            );
        }

        /// <summary>
        /// Gets all recipe components that build into this item.
        /// </summary>
        public static IEnumerable<ItemStatic> AllComponents(this ItemStatic item)
        {
            if (item.From == null)
                return Enumerable.Empty<ItemStatic>();

            var baseItems = item.From.Select(strid => { int o; return int.TryParse(strid, out o) ? o : -1; })
                .Where(i => i != -1)
                .Select(i => StaticDataStore.Items.Items[i]);

            return baseItems.Concat(baseItems.SelectMany(i => i.AllComponents()));
        }
    }
}
