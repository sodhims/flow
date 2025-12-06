using dfd2wasm.Models;
using dfd2wasm.Services;
using System.Text.RegularExpressions;

namespace dfd2wasm.Services
{
    public class ImportService
    {
        private int nodeIdCounter = 1;
        private int edgeIdCounter = 1;
        private Dictionary<string, int> nodeMapping = new Dictionary<string, int>();

        public class ImportResult
        {
            public List<Node> Nodes { get; set; } = new List<Node>();
            public List<Edge> Edges { get; set; } = new List<Edge>();
            public string Format { get; set; } = "";
            public bool Success { get; set; }
            public string ErrorMessage { get; set; } = "";
        }

        public ImportResult ImportFromText(string input)
        {
            var result = new ImportResult();

            if (string.IsNullOrWhiteSpace(input))
            {
                result.ErrorMessage = "Input is empty";
                return result;
            }

            // Detect format
            if (input.TrimStart().StartsWith("graph") || input.TrimStart().StartsWith("flowchart"))
            {
                result.Format = "Mermaid";
                return ImportMermaid(input);
            }
            else if (input.Contains("digraph") || input.Contains("graph {"))
            {
                result.Format = "Graphviz";
                return ImportGraphviz(input);
            }
            else
            {
                result.ErrorMessage = "Unrecognized format. Expected Mermaid (flowchart/graph) or Graphviz (digraph)";
                return result;
            }
        }

        #region Mermaid Import

        private ImportResult ImportMermaid(string input)
        {
            var result = new ImportResult { Format = "Mermaid" };
            nodeIdCounter = 1;
            edgeIdCounter = 1;
            nodeMapping.Clear();

            try
            {
                var lines = input.Split('\n', StringSplitOptions.RemoveEmptyEntries)
                    .Select(l => l.Trim())
                    .Where(l => !string.IsNullOrWhiteSpace(l))
                    .ToList();

                // Skip the first line (graph/flowchart declaration)
                lines = lines.Skip(1).ToList();

                foreach (var line in lines)
                {
                    if (line.StartsWith("%") || line.StartsWith("%%"))
                        continue; // Skip comments

                    ParseMermaidLine(line, result);
                }

                // Auto-layout nodes with edge information for hierarchical layout
                AutoLayoutNodes(result.Nodes, result.Edges);

                // Calculate edge paths
                foreach (var edge in result.Edges)
                {
                    edge.PathData = CalculateEdgePath(edge, result.Nodes);
                }

                result.Success = true;
            }
            catch (Exception ex)
            {
                result.ErrorMessage = $"Error parsing Mermaid: {ex.Message}";
                result.Success = false;
            }

            return result;
        }

        private void ParseMermaidLine(string line, ImportResult result)
        {
            // Mermaid edge patterns:
            // A --> B (arrow)
            // A[Text] --> B{Text} (arrow with inline node definitions)
            // A -->|label| B (arrow with label)
            // A -.-> B (dotted arrow)
            // A ==> B (thick arrow)

            // First, extract any node definitions from the line (inline or standalone)
            var nodePattern = @"(\w+)\s*([\[\(\{])\s*([^\]\)\}]+)\s*([\]\)\}])";
            var nodeMatches = Regex.Matches(line, nodePattern);

            foreach (Match nodeMatch in nodeMatches)
            {
                var nodeKey = nodeMatch.Groups[1].Value;
                var openBracket = nodeMatch.Groups[2].Value;
                var text = nodeMatch.Groups[3].Value.Trim();
                var closeBracket = nodeMatch.Groups[4].Value;

                // Determine shape from brackets
                var shape = "rectangle";
                if (openBracket == "(" && closeBracket == ")")
                    shape = "rounded";
                else if (openBracket == "{" && closeBracket == "}")
                    shape = "diamond";

                EnsureNode(nodeKey, result, text, shape);
            }

            // Then, check for edges
            // Enhanced pattern to handle inline node definitions: A[Text] --> B{Text}
            var edgePattern = @"(\w+)(?:\s*[\[\(\{][^\]\)\}]+[\]\)\}])?\s*(-->|---|==>|\.\.>|-\.-|==|-\.->)\s*(?:\|([^\|]+)\|)?\s*(\w+)(?:\s*[\[\(\{][^\]\)\}]+[\]\)\}])?";
            var edgeMatch = Regex.Match(line, edgePattern);

            if (edgeMatch.Success)
            {
                var fromNodeKey = edgeMatch.Groups[1].Value;
                var arrowType = edgeMatch.Groups[2].Value;
                var label = edgeMatch.Groups[3].Value.Trim();
                var toNodeKey = edgeMatch.Groups[4].Value;

                // Ensure nodes exist (in case they weren't defined with brackets)
                EnsureNode(fromNodeKey, result);
                EnsureNode(toNodeKey, result);

                // Create edge
                var edge = new Edge
                {
                    Id = edgeIdCounter++,
                    From = nodeMapping[fromNodeKey],
                    To = nodeMapping[toNodeKey],
                    Label = string.IsNullOrEmpty(label) ? null : label,
                    FromConnection = new ConnectionPoint { Side = "right", Position = 0 },
                    ToConnection = new ConnectionPoint { Side = "left", Position = 0 },
                    IsOrthogonal = false
                };

                // Set style based on arrow type
                switch (arrowType)
                {
                    case "==>":
                    case "==":
                        edge.StrokeWidth = 4;
                        break;
                    case "-.->":
                    case "-.-":
                    case "..>":
                        edge.StrokeDashArray = "5,5";
                        break;
                    default:
                        edge.StrokeWidth = 2;
                        break;
                }

                result.Edges.Add(edge);
            }
        }

        private void EnsureNode(string key, ImportResult result, string? text = null, string? shape = null)
        {
            if (!nodeMapping.ContainsKey(key))
            {
                var nodeId = nodeIdCounter++;
                nodeMapping[key] = nodeId;

                result.Nodes.Add(new Node
                {
                    Id = nodeId,
                    X = 0, // Will be set by auto-layout
                    Y = 0,
                    Width = 120,
                    Height = 60,
                    Text = text ?? key,
                    Shape = ParseNodeShape(shape)
                });
            }
            else if (text != null)
            {
                // Update text if provided
                var node = result.Nodes.FirstOrDefault(n => n.Id == nodeMapping[key]);
                if (node != null && string.IsNullOrEmpty(node.Text))
                {
                    node.Text = text;
                    if (!string.IsNullOrEmpty(shape))
                        node.Shape = ParseNodeShape(shape);
                }
            }
        }

        #endregion

        #region Graphviz Import

        private ImportResult ImportGraphviz(string input)
        {
            var result = new ImportResult { Format = "Graphviz" };
            nodeIdCounter = 1;
            edgeIdCounter = 1;
            nodeMapping.Clear();

            try
            {
                // Remove comments
                input = Regex.Replace(input, @"//.*$", "", RegexOptions.Multiline);
                input = Regex.Replace(input, @"/\*.*?\*/", "", RegexOptions.Singleline);

                // Extract node definitions
                // node [label="text", shape=box];
                var nodePattern = @"(\w+)\s*\[([^\]]+)\]";
                var nodeMatches = Regex.Matches(input, nodePattern);

                foreach (Match match in nodeMatches)
                {
                    var nodeKey = match.Groups[1].Value;
                    var attributes = match.Groups[2].Value;

                    var label = ExtractAttribute(attributes, "label");
                    var shape = ExtractAttribute(attributes, "shape");
                    var color = ExtractAttribute(attributes, "color");
                    var fillcolor = ExtractAttribute(attributes, "fillcolor");

                    var dfdShape = ConvertGraphvizShape(shape);

                    EnsureNode(nodeKey, result, label ?? nodeKey, dfdShape);

                    // Note: Colors are ignored as Node class doesn't have color properties
                }

                // Extract edges
                // A -> B or A -- B
                var edgePattern = @"(\w+)\s*(-[->])\s*(\w+)(?:\s*\[([^\]]+)\])?";
                var edgeMatches = Regex.Matches(input, edgePattern);

                foreach (Match match in edgeMatches)
                {
                    var fromNodeKey = match.Groups[1].Value;
                    var arrowType = match.Groups[2].Value;
                    var toNodeKey = match.Groups[3].Value;
                    var attributes = match.Groups.Count > 4 ? match.Groups[4].Value : "";

                    // Ensure nodes exist
                    EnsureNode(fromNodeKey, result);
                    EnsureNode(toNodeKey, result);

                    var edge = new Edge
                    {
                        Id = edgeIdCounter++,
                        From = nodeMapping[fromNodeKey],
                        To = nodeMapping[toNodeKey],
                        FromConnection = new ConnectionPoint { Side = "right", Position = 0 },
                        ToConnection = new ConnectionPoint { Side = "left", Position = 0 },
                        IsOrthogonal = false
                    };

                    // Parse edge attributes
                    if (!string.IsNullOrEmpty(attributes))
                    {
                        var label = ExtractAttribute(attributes, "label");
                        if (!string.IsNullOrEmpty(label))
                            edge.Label = label;

                        var style = ExtractAttribute(attributes, "style");
                        if (style == "dashed" || style == "dotted")
                            edge.StrokeDashArray = "5,5";

                        var color = ExtractAttribute(attributes, "color");
                        if (!string.IsNullOrEmpty(color))
                            edge.StrokeColor = color;

                        var penwidth = ExtractAttribute(attributes, "penwidth");
                        if (!string.IsNullOrEmpty(penwidth) && int.TryParse(penwidth, out int width))
                            edge.StrokeWidth = width;
                    }

                    result.Edges.Add(edge);
                }

                // Auto-layout nodes with edge information for hierarchical layout
                AutoLayoutNodes(result.Nodes, result.Edges);

                // Calculate edge paths
                foreach (var edge in result.Edges)
                {
                    edge.PathData = CalculateEdgePath(edge, result.Nodes);
                }

                result.Success = true;
            }
            catch (Exception ex)
            {
                result.ErrorMessage = $"Error parsing Graphviz: {ex.Message}";
                result.Success = false;
            }

            return result;
        }

        private string? ExtractAttribute(string attributes, string attrName)
        {
            var pattern = $@"{attrName}\s*=\s*""([^""]+)""";
            var match = Regex.Match(attributes, pattern);
            if (match.Success)
                return match.Groups[1].Value;

            // Try without quotes
            pattern = $@"{attrName}\s*=\s*(\w+)";
            match = Regex.Match(attributes, pattern);
            if (match.Success)
                return match.Groups[1].Value;

            return null;
        }

        private string ConvertGraphvizShape(string? graphvizShape)
        {
            if (string.IsNullOrEmpty(graphvizShape))
                return "rectangle";

            return graphvizShape.ToLower() switch
            {
                "box" => "rectangle",
                "rectangle" => "rectangle",
                "ellipse" => "ellipse",
                "circle" => "circle",
                "diamond" => "diamond",
                "parallelogram" => "parallelogram",
                "trapezium" => "trapezoid",
                "cylinder" => "cylinder",
                _ => "rectangle"
            };
        }

        #endregion

        #region Auto Layout - Hierarchical (Sugiyama-style)

        private void AutoLayoutNodes(List<Node> nodes, List<Edge> edges)
        {
            if (nodes.Count == 0) return;

            const int horizontalSpacing = 250;
            const int verticalSpacing = 200;
            const int startX = 100;
            const int startY = 100;

            // Build adjacency information from edges
            var adjacency = BuildAdjacencyLists(nodes, edges);

            // Step 1: Assign nodes to layers (hierarchical levels)
            var layers = AssignNodesToLayers(nodes, adjacency);

            // Safety check: ensure all nodes are in some layer
            var layeredNodeIds = new HashSet<int>(layers.SelectMany(l => l));
            foreach (var node in nodes)
            {
                if (!layeredNodeIds.Contains(node.Id))
                {
                    // Add orphaned nodes to layer 0
                    layers[0].Add(node.Id);
                }
            }

            // Step 2: Minimize edge crossings by ordering nodes within layers
            MinimizeCrossings(layers, adjacency);

            // Step 3: Position nodes based on layer assignment
            PositionNodes(layers, horizontalSpacing, verticalSpacing, startX, startY, nodes);
        }

        private (Dictionary<int, List<int>> outgoing, Dictionary<int, List<int>> incoming) BuildAdjacencyLists(List<Node> nodes, List<Edge> edges)
        {
            var outgoing = new Dictionary<int, List<int>>();
            var incoming = new Dictionary<int, List<int>>();

            // Initialize all nodes
            foreach (var node in nodes)
            {
                outgoing[node.Id] = new List<int>();
                incoming[node.Id] = new List<int>();
            }

            // Build adjacency lists from edges
            foreach (var edge in edges)
            {
                if (outgoing.ContainsKey(edge.From) && incoming.ContainsKey(edge.To))
                {
                    outgoing[edge.From].Add(edge.To);
                    incoming[edge.To].Add(edge.From);
                }
            }

            return (outgoing, incoming);
        }

        private List<List<int>> AssignNodesToLayers(List<Node> nodes, (Dictionary<int, List<int>> outgoing, Dictionary<int, List<int>> incoming) adjacency)
        {
            var layers = new List<List<int>>();
            var nodeToLayer = new Dictionary<int, int>();
            var outgoing = adjacency.outgoing;
            var incoming = adjacency.incoming;

            // Find root nodes (nodes with no incoming edges)
            var rootNodes = nodes.Where(n => incoming[n.Id].Count == 0).ToList();

            // If no roots found (cycle or all connected), pick nodes with minimum incoming
            if (rootNodes.Count == 0)
            {
                var minIncoming = incoming.Values.Min(list => list.Count);
                rootNodes = nodes.Where(n => incoming[n.Id].Count == minIncoming).ToList();
            }

            // Initialize all nodes to layer 0
            foreach (var node in nodes)
            {
                nodeToLayer[node.Id] = 0;
            }

            // Use topological ordering with cycle detection
            // Detect back-edges (edges that would create cycles)
            var backEdges = new HashSet<(int from, int to)>();
            var visited = new HashSet<int>();
            var recursionStack = new HashSet<int>();

            // DFS to detect back-edges
            void DetectBackEdges(int nodeId)
            {
                visited.Add(nodeId);
                recursionStack.Add(nodeId);

                foreach (var childId in outgoing[nodeId])
                {
                    if (!visited.Contains(childId))
                    {
                        DetectBackEdges(childId);
                    }
                    else if (recursionStack.Contains(childId))
                    {
                        // This is a back-edge (creates a cycle)
                        backEdges.Add((nodeId, childId));
                    }
                }

                recursionStack.Remove(nodeId);
            }

            // Run DFS from all roots to detect cycles
            foreach (var root in rootNodes)
            {
                if (!visited.Contains(root.Id))
                {
                    DetectBackEdges(root.Id);
                }
            }

            // Calculate longest path to each node, ignoring back-edges
            bool changed = true;
            int maxIterations = nodes.Count * 2; // Limit iterations
            int iteration = 0;

            while (changed && iteration < maxIterations)
            {
                changed = false;
                iteration++;

                foreach (var node in nodes)
                {
                    foreach (var childId in outgoing[node.Id])
                    {
                        // Skip back-edges when calculating layers
                        if (backEdges.Contains((node.Id, childId)))
                            continue;

                        int newLayer = nodeToLayer[node.Id] + 1;
                        if (newLayer > nodeToLayer[childId])
                        {
                            nodeToLayer[childId] = newLayer;
                            changed = true;
                        }
                    }
                }
            }

            // Build layers from nodeToLayer mapping
            int maxLayer = nodeToLayer.Values.Count > 0 ? nodeToLayer.Values.Max() : 0;

            // Cap maximum layer to prevent nodes going too far down
            maxLayer = Math.Min(maxLayer, nodes.Count - 1);

            for (int i = 0; i <= maxLayer; i++)
            {
                layers.Add(new List<int>());
            }

            foreach (var kvp in nodeToLayer)
            {
                int layer = Math.Min(kvp.Value, maxLayer); // Cap layer
                layers[layer].Add(kvp.Key);
            }

            // Ensure we have at least one layer
            if (layers.Count == 0)
            {
                layers.Add(new List<int>());
                foreach (var node in nodes)
                {
                    layers[0].Add(node.Id);
                }
            }

            return layers;
        }

        private void MinimizeCrossings(List<List<int>> layers, (Dictionary<int, List<int>> outgoing, Dictionary<int, List<int>> incoming) adjacency)
        {
            // Use barycenter heuristic to minimize crossings
            // Iterate multiple times for better results
            const int iterations = 4;

            for (int iter = 0; iter < iterations; iter++)
            {
                // Forward pass: order based on parents
                for (int i = 1; i < layers.Count; i++)
                {
                    OrderLayerByBarycenter(layers[i], layers[i - 1], adjacency.incoming, isForward: true);
                }

                // Backward pass: order based on children
                for (int i = layers.Count - 2; i >= 0; i--)
                {
                    OrderLayerByBarycenter(layers[i], layers[i + 1], adjacency.outgoing, isForward: false);
                }
            }
        }

        private void OrderLayerByBarycenter(List<int> currentLayer, List<int> referenceLayer, Dictionary<int, List<int>> connections, bool isForward)
        {
            if (currentLayer.Count <= 1) return;

            // Calculate barycenter (average position) for each node
            var barycenters = new Dictionary<int, double>();

            foreach (var nodeId in currentLayer)
            {
                var connectedNodes = connections[nodeId];
                if (connectedNodes.Count == 0)
                {
                    // No connections, keep current relative position
                    barycenters[nodeId] = currentLayer.IndexOf(nodeId);
                    continue;
                }

                double sum = 0;
                int count = 0;
                foreach (var connectedId in connectedNodes)
                {
                    int pos = referenceLayer.IndexOf(connectedId);
                    if (pos >= 0)
                    {
                        sum += pos;
                        count++;
                    }
                }

                barycenters[nodeId] = count > 0 ? sum / count : currentLayer.IndexOf(nodeId);
            }

            // Sort layer by barycenter values (stable sort to maintain order for ties)
            currentLayer.Sort((a, b) =>
            {
                int cmp = barycenters[a].CompareTo(barycenters[b]);
                return cmp != 0 ? cmp : a.CompareTo(b); // Use ID as tiebreaker for stability
            });
        }

        private void PositionNodes(List<List<int>> layers, int horizontalSpacing, int verticalSpacing, int startX, int startY, List<Node> nodes)
        {
            for (int layerIndex = 0; layerIndex < layers.Count; layerIndex++)
            {
                var layer = layers[layerIndex];
                var layerY = startY + (layerIndex * verticalSpacing);

                // Center the layer horizontally
                var layerWidth = (layer.Count - 1) * horizontalSpacing;
                var layerStartX = startX + 400 - (layerWidth / 2); // Center with offset to keep visible

                for (int posIndex = 0; posIndex < layer.Count; posIndex++)
                {
                    var nodeId = layer[posIndex];
                    var node = nodes.FirstOrDefault(n => n.Id == nodeId);

                    if (node != null)
                    {
                        node.X = layerStartX + (posIndex * horizontalSpacing);
                        node.Y = layerY;
                    }
                }
            }
        }

        #endregion

        #region Edge Path Calculation

        private string CalculateEdgePath(Edge edge, List<Node> nodes)
        {
            var fromNode = nodes.FirstOrDefault(n => n.Id == edge.From);
            var toNode = nodes.FirstOrDefault(n => n.Id == edge.To);

            if (fromNode == null || toNode == null)
                return "";

            // Simple straight line path
            var fromX = fromNode.X + fromNode.Width;
            var fromY = fromNode.Y + fromNode.Height / 2;
            var toX = toNode.X;
            var toY = toNode.Y + toNode.Height / 2;

            return $"M {fromX} {fromY} L {toX} {toY}";
        }

        #endregion

        #region Helper Methods

        private NodeShape ParseNodeShape(string? shape)
        {
            if (string.IsNullOrEmpty(shape))
                return NodeShape.Rectangle;

            return shape.ToLower() switch
            {
                "rectangle" => NodeShape.Rectangle,
                "rounded" => NodeShape.Rectangle,  // Use Rectangle if your enum doesn't have Rounded
                "ellipse" => NodeShape.Ellipse,
                "circle" => NodeShape.Ellipse,     // Use Ellipse if your enum doesn't have Circle
                "diamond" => NodeShape.Diamond,
                "parallelogram" => NodeShape.Parallelogram,
                "trapezoid" => NodeShape.Rectangle,
                "cylinder" => NodeShape.Cylinder,
                "document" => NodeShape.Rectangle,
                _ => NodeShape.Rectangle
            };
        }

        #endregion
    }
}
