// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2017 rohatsu software studios limited / www.rohatsu.com
using System.Collections.Generic;

namespace RIFF.Core
{
    public enum RFGraphMapEdgeType
    {
        NotSet = 0,
        Input = 1,
        Output = 2,
        State = 3
    }

    public enum RFGraphMapNodeType
    {
        NotSet = 0,
        Processor = 1,
        Key = 2
    }

    public static class RFGraphMapper
    {
        public static string KeyLabel(RFCatalogKey key)
        {
            return key.FriendlyString();
        }

        public static RFGraphMap MapGraphs(IEnumerable<RFGraphDefinition> graphs, bool forPresentation = true)
        {
            var map = new RFGraphMap();
            foreach (var graph in graphs)
            {
                foreach (var process in graph.Processes)
                {
                    var name = RFGraphDefinition.GetFullName(graph.GraphName, process.Value.Name);
                    var processNode = new RFGraphMapNode(map)
                    {
                        Label = process.Value.Name,
                        GraphName = graph.GraphName,
                        NodeType = RFGraphMapNodeType.Processor,
                        FullType = process.Value.Processor().GetType().Name,
                        Description = process.Value.Description
                    };
                    map.Nodes.Add(processNode);
                    foreach (var ioMapping in process.Value.IOMappings)
                    {
                        if (ioMapping.PropertyName == "Status")
                        {
                            continue;
                        }

                        var ioBehaviour = RFReflectionHelpers.GetIOBehaviour(ioMapping.Property);
                        var dateBehaviour = ioMapping.DateBehaviour; //RFReflectionHelpers.GetDateBehaviour(ioMapping.Property);
                        if (!forPresentation)
                        {
                            // for graph sort we are only interested in exact date inputs and
                            // outputs, which should result in acycylical graph
                            if (ioBehaviour == RFIOBehaviour.State)
                            {
                                continue;
                            }
                            if (dateBehaviour != RFDateBehaviour.Exact && dateBehaviour != RFDateBehaviour.Range && dateBehaviour != RFDateBehaviour.Latest)
                            {
                                continue;
                            }
                        }
                        if (ioMapping.Key != null)
                        {
                            var rawKeyString = KeyLabel(ioMapping.Key.CreateForInstance(null));
                            var keyLabel = forPresentation ? graph.GraphName + rawKeyString : rawKeyString;
                            if (!map.KeyNodes.ContainsKey(keyLabel))
                            {
                                map.KeyNodes.Add(keyLabel, new RFGraphMapNode(map)
                                {
                                    Label = rawKeyString,
                                    RawKey = rawKeyString,
                                    NodeType = RFGraphMapNodeType.Key,
                                    GraphName = graph.GraphName,
                                    FullType = ioMapping.Key.CreateForInstance(null).GetType().Name,
                                    Description = ""
                                });
                            }
                            var keyNode = map.KeyNodes[keyLabel];
                            if (ioBehaviour == RFIOBehaviour.Input || ioBehaviour == RFIOBehaviour.State)
                            {
                                map.Edges.Add(new RFGraphMapEdge
                                {
                                    SourceNode = keyNode.ID,
                                    DestinationNode = processNode.ID,
                                    Label = ioMapping.PropertyName,
                                    EdgeType = ioBehaviour == RFIOBehaviour.Input ? RFGraphMapEdgeType.Input : RFGraphMapEdgeType.State
                                });
                                keyNode.Description += string.Format("{0} to {1} ({2})<br/>", ioBehaviour, processNode.Label, ioMapping.PropertyName);
                            }
                            if (ioBehaviour == RFIOBehaviour.Output)
                            {
                                map.Edges.Add(new RFGraphMapEdge
                                {
                                    SourceNode = processNode.ID,
                                    DestinationNode = keyNode.ID,
                                    Label = ioMapping.PropertyName,
                                    EdgeType = RFGraphMapEdgeType.Output
                                });
                                keyNode.Description += string.Format("{0} of {1} ({2})<br/>", ioBehaviour, processNode.Label, ioMapping.PropertyName);
                            }
                        }
                        else
                        {
                            // unbound item? check for other domain properties?
                        }
                    }
                }
            }
            return map;
        }
    }

    public class RFGraphMapEdge
    {
        public int DestinationNode { get; set; }

        public RFGraphMapEdgeType EdgeType { get; set; }

        public string Label { get; set; }

        public int SourceNode { get; set; }
    }

    public class RFGraphMapNode
    {
        public string Description { get; set; }
        public string FullType { get; set; }
        public string GraphName { get; set; }

        public int ID { get; set; }

        public string Label { get; set; }

        public RFGraphMapNodeType NodeType { get; set; }

        public string RawKey { get; set; }

        public RFGraphMapNode(RFGraphMap map)
        {
            ID = map.NodeCounter++;
        }
    }
}
