using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProBuilds.BuildPath
{
    public static class SkillOrderCalculator
    {
        private class SkillTreeNode
        {
            public int Id;
            public int Count = 0;
            public Dictionary<int, SkillTreeNode> Nodes = new Dictionary<int, SkillTreeNode>();
            public SkillTreeNode(int id) { Id = id; }
        }

        public static int[] CalculateSkillOrder(Dictionary<string, int> skillOrderCounts)
        {
            if (skillOrderCounts.Count == 0)
                return new int[] { };

            SkillTreeNode root = new SkillTreeNode(-1);
            foreach (var key in skillOrderCounts.Keys)
            {
                if (string.IsNullOrEmpty(key))
                    continue;

                int[] skillOrder = key
                    .Split(PurchaseSet.SkillSeparator)
                    .Select(k => { int v; if (!int.TryParse(k, out v)) return -1; return v; })
                    .ToArray();

                if (skillOrder.Any(v => v == -1))
                    continue;

                SkillTreeNode node = root;
                for (int i = 0; i < skillOrder.Length; ++i)
			    {
                    int v = skillOrder[i];
                    if (!node.Nodes.ContainsKey(v))
                    {
                        node.Nodes.Add(v, new SkillTreeNode(v));
                    }

                    node = node.Nodes[v];
                    node.Count += skillOrderCounts[key];
			    }
            }

            // Get most common path
            if (root.Nodes.Count == 0)
                return new int[] {};

            List<int> order = new List<int>();
            SkillTreeNode n = root;
            while (n != null)
            {
                // Pick next
                n = n.Nodes.OrderBy(kvp => kvp.Value.Count).Reverse().Select(kvp => kvp.Value).FirstOrDefault();
                if (n != null)
                    order.Add(n.Id);
            }

            return order.ToArray();
        }
    }
}
