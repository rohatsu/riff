// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2019 rohatsu software studios limited / www.rohatsu.com
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RIFF.Core
{
    public class RFGraphMap
    {
        public List<RFGraphMapEdge> Edges { get; set; }
        public Dictionary<string, RFGraphMapNode> KeyNodes { get; set; }
        public List<RFGraphMapNode> Nodes { get; set; }
        public int NodeCounter = 1;

        public RFGraphMap()
        {
            Nodes = new List<RFGraphMapNode>();
            KeyNodes = new Dictionary<string, RFGraphMapNode>();
            Edges = new List<RFGraphMapEdge>();
        }

        public Dictionary<string, SortedSet<string>> CalculateDependencies()
        {
            // for each node list nodes it depends on (i.e. reverse traverse)
            var processors = Nodes.ToDictionary(n => n.ID, n => RFGraphDefinition.GetFullName(n.GraphName, n.Label));
            var dependencies = new Dictionary<string, SortedSet<string>>();
            foreach (var node in Nodes)
            {
                var visited = new SortedSet<int>();
                var toVisit = new SortedSet<int>(Edges.Where(e => e.DestinationNode == node.ID).Select(e => e.SourceNode));
                while (toVisit.Any())
                {
                    var next = toVisit.First();
                    toVisit.Remove(next);
                    if (!visited.Contains(next))
                    {
                        visited.Add(next);
                        foreach (var source in Edges.Where(e => e.DestinationNode == next).Select(e => e.SourceNode))
                        {
                            if (!visited.Contains(source))
                            {
                                toVisit.Add(source);
                            }
                        }
                    }
                }
                dependencies.Add(processors[node.ID], new SortedSet<string>(visited.Where(v => processors.ContainsKey(v)).Select(v => processors[v])));
            }
            return dependencies;
        }

        // topologically sort the graph to work out calculation order
        public Dictionary<string, int> CalculateWeights()
        {
            var remainingNodes = new SortedSet<int>(Nodes.Select(n => n.ID).Union(KeyNodes.Select(k => k.Value.ID)));
            var markedNodes = new SortedSet<int>();
            var processors = Nodes.ToDictionary(n => n.ID, n => RFGraphDefinition.GetFullName(n.GraphName, n.Label));
            var visitOrder = new List<int>();

            while (remainingNodes.Any())
            {
                var remainingNode = remainingNodes.First();
                Visit(remainingNode, markedNodes, remainingNodes, visitOrder);
            }

            var weights = new Dictionary<string, int>();
            int idx = 1;
            visitOrder.Reverse();
            foreach (var node in visitOrder)
            {
                if (processors.ContainsKey(node))
                {
                    weights.Add(processors[node], idx++);
                }
            }
            return weights;
        }

        public void dump()
        {
            foreach (var node in Nodes)
            {
                Console.WriteLine("N#{0}: {1} - {2}", node.ID, node.NodeType, node.Label);
            }
            foreach (var node in KeyNodes.Values)
            {
                Console.WriteLine("K#{0}: {1} - {2}", node.ID, node.NodeType, node.Label);
            }
            foreach (var edge in Edges)
            {
                Console.WriteLine("E: {0} <-> {1}", edge.SourceNode, edge.DestinationNode);
            }
        }

        public string GetCytoscapeCrossGraphs()
        {
            var sb = new StringBuilder();
            int cgeCounter = 1;
            foreach (var node in KeyNodes.Values.GroupBy(k => k.RawKey))
            {
                if (node.Count() > 1)
                {
                    // key is in more than one graph - create cross-graph edges
                    foreach (var instance1 in node.OrderBy(n => n.ID))
                    {
                        foreach (var instance2 in node.Where(n => n.ID > instance1.ID))
                        {
                            sb.AppendLine("{ data: { id: 'cge" + cgeCounter + "', source: 'n" + instance1.ID + "', target: 'n" + instance2.ID + "' }, classes: 'edge_cross transparent' },");
                            cgeCounter++;
                        }
                    }
                }
            }
            return sb.ToString();
        }

        public Dictionary<string, string> GetCytoscapeStrings()
        {
            var allGraphs = new SortedSet<string>(Nodes.Select(n => n.GraphName)).ToList();
            var strings = new Dictionary<string, string>();
            foreach (var g in allGraphs)
            {
                strings.Add(g, GetCytoscapeString(g, allGraphs));
            }
            strings.Add("ALL", GetCytoscapeString(null, allGraphs));
            return strings;
        }

        protected static string BreakLabel(string label)
        {
            return label.Replace(".", "\\n").Replace("/", "\\n");
        }

        protected void Visit(int node, SortedSet<int> markedNodes, SortedSet<int> remainingNodes, List<int> visitOrder)
        {
            if (markedNodes.Contains(node))
            {
                throw new RFLogicException(this, "Cycle in graph");
            }
            else if (remainingNodes.Contains(node))
            {
                markedNodes.Add(node);
                foreach (var destination in Edges.Where(e => e.SourceNode == node).Select(e => e.DestinationNode))
                {
                    Visit(destination, markedNodes, remainingNodes, visitOrder);
                }
                remainingNodes.Remove(node);
                markedNodes.Remove(node);
                visitOrder.Add(node);
            }
        }

        private string GetCytoscapeString(string graph, List<string> allGraphs)
        {
            var sb = new StringBuilder();

            var nodesInGraph = Nodes.Where(n => graph == null || graph == n.GraphName);
            var nodeIDs = new SortedSet<int>(nodesInGraph.Select(n => n.ID));
            foreach (var node in nodesInGraph)
            {
                sb.AppendLine("{ data: { id: 'n" + node.ID + "', label: '" + BreakLabel(node.Label) + "', info: '" + GetNodeInfo(node) + "' }, classes: 'transparent process graph" +
                    (allGraphs.IndexOf(node.GraphName) + 1) + "' },");
            }

            // add all connected nodes
            var connectedNodes = new SortedSet<int>();
            foreach (var e in Edges.Where(e => nodeIDs.Contains(e.DestinationNode) || nodeIDs.Contains(e.SourceNode)))
            {
                connectedNodes.Add(e.SourceNode);
                connectedNodes.Add(e.DestinationNode);
            }
            nodeIDs.UnionWith(connectedNodes);

            foreach (var node in KeyNodes.Values.Where(n => nodeIDs.Contains(n.ID)))
            {
                sb.AppendLine("{ data: { id: 'n" + node.ID + "', label: '" + BreakLabel(node.Label) + "', info: '" + GetNodeInfo(node) + "' }, classes: 'transparent key' },");
            }
            foreach (var edge in Edges.Where(e => nodeIDs.Contains(e.SourceNode) || nodeIDs.Contains(e.DestinationNode)))
            {
                sb.AppendLine("{ data: { id: 'e" + edge.SourceNode + edge.DestinationNode + "', source: 'n" + edge.SourceNode + "', target: 'n" + edge.DestinationNode + "' }, classes: ' transparent edge_"
                    + edge.EdgeType.ToString().ToLower()
                    + "' },");
            }
            return sb.ToString();
        }

        private string GetNodeInfo(RFGraphMapNode node)
        {
            return string.Format("Type: {0}<br/>{1}", node.FullType, node.Description.Replace("'", "\\'"));
        }
    }
}
