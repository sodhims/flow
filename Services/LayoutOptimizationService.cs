using dfd2wasm.Models;

namespace dfd2wasm.Services
{
    /// <summary>
    /// Layout optimization using simulated annealing and fitness functions.
    /// </summary>
    public class LayoutOptimizationService
    {
        private readonly Random _random = new();

        #region Fitness Evaluation

        public class FitnessWeights
        {
            public double EdgeLength { get; set; } = 1.0;
            public double EdgeCrossings { get; set; } = 50.0;
            public double NodeOverlap { get; set; } = 100.0;
            public double EdgeNodeOverlap { get; set; } = 30.0;
            public double AspectRatio { get; set; } = 5.0;
            public double Distribution { get; set; } = 5.0;
            public double Alignment { get; set; } = -2.0;      // Bonus (negative = good)
            public double GridSnap { get; set; } = -1.0;       // Bonus
            public double Symmetry { get; set; } = -3.0;       // Bonus
        }

        public FitnessWeights Weights { get; set; } = new();

        /// <summary>
        /// Evaluate the fitness of a layout. Lower is better.
        /// </summary>
        public FitnessResult EvaluateFitness(List<Node> nodes, List<Edge> edges)
        {
            var result = new FitnessResult();

            if (nodes.Count == 0) return result;

            // Penalties (higher = worse)
            result.EdgeLengthPenalty = CalculateEdgeLengthPenalty(nodes, edges);
            result.EdgeCrossings = CountEdgeCrossings(nodes, edges);
            result.NodeOverlaps = CountNodeOverlaps(nodes);
            result.EdgeNodeOverlaps = CalculateEdgeNodeOverlaps(nodes, edges);
            result.AspectRatioPenalty = CalculateAspectRatioPenalty(nodes);
            result.DistributionPenalty = CalculateDistributionPenalty(nodes);

            // Bonuses (higher = better, applied as negative)
            result.AlignedPairs = CountAlignedPairs(nodes);
            result.GridSnappedNodes = CountGridSnappedNodes(nodes);
            result.SymmetryScore = CalculateSymmetryScore(nodes);

            // Calculate total
            result.Total =
                Weights.EdgeLength * result.EdgeLengthPenalty +
                Weights.EdgeCrossings * result.EdgeCrossings +
                Weights.NodeOverlap * result.NodeOverlaps +
                Weights.EdgeNodeOverlap * result.EdgeNodeOverlaps +
                Weights.AspectRatio * result.AspectRatioPenalty +
                Weights.Distribution * result.DistributionPenalty +
                Weights.Alignment * result.AlignedPairs +
                Weights.GridSnap * result.GridSnappedNodes +
                Weights.Symmetry * result.SymmetryScore;

            return result;
        }

        #endregion

        #region Penalty Calculations

        private double CalculateEdgeLengthPenalty(List<Node> nodes, List<Edge> edges)
        {
            double total = 0;
            foreach (var edge in edges)
            {
                var fromNode = nodes.FirstOrDefault(n => n.Id == edge.From);
                var toNode = nodes.FirstOrDefault(n => n.Id == edge.To);
                if (fromNode == null || toNode == null) continue;

                var dx = (toNode.X + toNode.Width / 2) - (fromNode.X + fromNode.Width / 2);
                var dy = (toNode.Y + toNode.Height / 2) - (fromNode.Y + fromNode.Height / 2);
                total += Math.Sqrt(dx * dx + dy * dy);
            }
            return total;
        }

        private int CountEdgeCrossings(List<Node> nodes, List<Edge> edges)
        {
            int crossings = 0;
            var edgeLines = new List<(double x1, double y1, double x2, double y2)>();

            foreach (var edge in edges)
            {
                var fromNode = nodes.FirstOrDefault(n => n.Id == edge.From);
                var toNode = nodes.FirstOrDefault(n => n.Id == edge.To);
                if (fromNode == null || toNode == null) continue;

                edgeLines.Add((
                    fromNode.X + fromNode.Width / 2,
                    fromNode.Y + fromNode.Height / 2,
                    toNode.X + toNode.Width / 2,
                    toNode.Y + toNode.Height / 2
                ));
            }

            for (int i = 0; i < edgeLines.Count; i++)
            {
                for (int j = i + 1; j < edgeLines.Count; j++)
                {
                    if (SegmentsIntersect(edgeLines[i], edgeLines[j]))
                        crossings++;
                }
            }

            return crossings;
        }

        private bool SegmentsIntersect(
            (double x1, double y1, double x2, double y2) a,
            (double x1, double y1, double x2, double y2) b)
        {
            double d1 = Direction(b.x1, b.y1, b.x2, b.y2, a.x1, a.y1);
            double d2 = Direction(b.x1, b.y1, b.x2, b.y2, a.x2, a.y2);
            double d3 = Direction(a.x1, a.y1, a.x2, a.y2, b.x1, b.y1);
            double d4 = Direction(a.x1, a.y1, a.x2, a.y2, b.x2, b.y2);

            if (((d1 > 0 && d2 < 0) || (d1 < 0 && d2 > 0)) &&
                ((d3 > 0 && d4 < 0) || (d3 < 0 && d4 > 0)))
                return true;

            return false;
        }

        private double Direction(double x1, double y1, double x2, double y2, double x3, double y3)
        {
            return (x3 - x1) * (y2 - y1) - (x2 - x1) * (y3 - y1);
        }

        private int CountNodeOverlaps(List<Node> nodes)
        {
            int overlaps = 0;
            const int gap = 20;

            for (int i = 0; i < nodes.Count; i++)
            {
                for (int j = i + 1; j < nodes.Count; j++)
                {
                    var a = nodes[i];
                    var b = nodes[j];

                    if (!(a.X + a.Width + gap < b.X ||
                          b.X + b.Width + gap < a.X ||
                          a.Y + a.Height + gap < b.Y ||
                          b.Y + b.Height + gap < a.Y))
                    {
                        overlaps++;
                    }
                }
            }

            return overlaps;
        }

        private double CalculateEdgeNodeOverlaps(List<Node> nodes, List<Edge> edges)
        {
            int overlaps = 0;

            foreach (var edge in edges)
            {
                var fromNode = nodes.FirstOrDefault(n => n.Id == edge.From);
                var toNode = nodes.FirstOrDefault(n => n.Id == edge.To);
                if (fromNode == null || toNode == null) continue;

                double x1 = fromNode.X + fromNode.Width / 2;
                double y1 = fromNode.Y + fromNode.Height / 2;
                double x2 = toNode.X + toNode.Width / 2;
                double y2 = toNode.Y + toNode.Height / 2;

                foreach (var node in nodes)
                {
                    if (node.Id == edge.From || node.Id == edge.To) continue;

                    if (LineIntersectsRect(x1, y1, x2, y2, node.X, node.Y, node.Width, node.Height))
                        overlaps++;
                }
            }

            return overlaps;
        }

        private bool LineIntersectsRect(double x1, double y1, double x2, double y2,
            double rx, double ry, double rw, double rh)
        {
            // Check if line intersects any of the 4 sides of rectangle
            return LineIntersectsLine(x1, y1, x2, y2, rx, ry, rx + rw, ry) ||
                   LineIntersectsLine(x1, y1, x2, y2, rx + rw, ry, rx + rw, ry + rh) ||
                   LineIntersectsLine(x1, y1, x2, y2, rx, ry + rh, rx + rw, ry + rh) ||
                   LineIntersectsLine(x1, y1, x2, y2, rx, ry, rx, ry + rh);
        }

        private bool LineIntersectsLine(double x1, double y1, double x2, double y2,
            double x3, double y3, double x4, double y4)
        {
            return SegmentsIntersect((x1, y1, x2, y2), (x3, y3, x4, y4));
        }

        private double CalculateAspectRatioPenalty(List<Node> nodes)
        {
            if (nodes.Count == 0) return 0;

            double minX = nodes.Min(n => n.X);
            double maxX = nodes.Max(n => n.X + n.Width);
            double minY = nodes.Min(n => n.Y);
            double maxY = nodes.Max(n => n.Y + n.Height);

            double width = maxX - minX;
            double height = maxY - minY;
            if (height == 0) return 0;

            double ratio = width / height;
            double targetRatio = 1.5; // Prefer landscape

            return Math.Abs(ratio - targetRatio) * 100;
        }

        private double CalculateDistributionPenalty(List<Node> nodes)
        {
            if (nodes.Count < 2) return 0;

            // Calculate variance in distances between nodes
            var distances = new List<double>();
            for (int i = 0; i < nodes.Count; i++)
            {
                for (int j = i + 1; j < nodes.Count; j++)
                {
                    var dx = nodes[j].X - nodes[i].X;
                    var dy = nodes[j].Y - nodes[i].Y;
                    distances.Add(Math.Sqrt(dx * dx + dy * dy));
                }
            }

            double mean = distances.Average();
            double variance = distances.Sum(d => (d - mean) * (d - mean)) / distances.Count;

            return Math.Sqrt(variance);
        }

        private int CountAlignedPairs(List<Node> nodes)
        {
            int aligned = 0;
            const double tolerance = 5;

            for (int i = 0; i < nodes.Count; i++)
            {
                for (int j = i + 1; j < nodes.Count; j++)
                {
                    // Check horizontal alignment (same Y center)
                    if (Math.Abs((nodes[i].Y + nodes[i].Height / 2) -
                                 (nodes[j].Y + nodes[j].Height / 2)) < tolerance)
                        aligned++;

                    // Check vertical alignment (same X center)
                    if (Math.Abs((nodes[i].X + nodes[i].Width / 2) -
                                 (nodes[j].X + nodes[j].Width / 2)) < tolerance)
                        aligned++;
                }
            }

            return aligned;
        }

        private int CountGridSnappedNodes(List<Node> nodes)
        {
            const int gridSize = 20;
            int snapped = 0;

            foreach (var node in nodes)
            {
                if (node.X % gridSize == 0 && node.Y % gridSize == 0)
                    snapped++;
            }

            return snapped;
        }

        private double CalculateSymmetryScore(List<Node> nodes)
        {
            if (nodes.Count < 2) return 0;

            // Calculate center of mass
            double centerX = nodes.Average(n => n.X + n.Width / 2);
            double centerY = nodes.Average(n => n.Y + n.Height / 2);

            int symmetricPairs = 0;
            const double tolerance = 30;

            for (int i = 0; i < nodes.Count; i++)
            {
                double dx = (nodes[i].X + nodes[i].Width / 2) - centerX;
                double dy = (nodes[i].Y + nodes[i].Height / 2) - centerY;

                // Look for a node at the mirrored position
                for (int j = i + 1; j < nodes.Count; j++)
                {
                    double mirrorX = centerX - dx;
                    double mirrorY = centerY - dy;

                    double actualX = nodes[j].X + nodes[j].Width / 2;
                    double actualY = nodes[j].Y + nodes[j].Height / 2;

                    // Check vertical symmetry (mirror across vertical axis)
                    if (Math.Abs(actualX - mirrorX) < tolerance &&
                        Math.Abs(actualY - (centerY + dy)) < tolerance)
                    {
                        symmetricPairs++;
                    }
                }
            }

            return symmetricPairs;
        }

        #endregion

        #region Simulated Annealing

        public class AnnealingOptions
        {
            public double InitialTemperature { get; set; } = 1000;
            public double CoolingRate { get; set; } = 0.995;
            public double MinTemperature { get; set; } = 0.1;
            public int MaxIterations { get; set; } = 5000;
            public int MaxNoImprovement { get; set; } = 500;
            public int GridSize { get; set; } = 20;
        }

        public async Task<(List<Node> Nodes, double Improvement)> OptimizeWithSimulatedAnnealing(
            List<Node> nodes,
            List<Edge> edges,
            AnnealingOptions? options = null,
            Action<int, double>? progressCallback = null,
            CancellationToken cancellationToken = default)
        {
            options ??= new AnnealingOptions();

            // Create working copy
            var workingNodes = nodes.Select(n => new Node
            {
                Id = n.Id,
                X = n.X,
                Y = n.Y,
                Width = n.Width,
                Height = n.Height,
                Text = n.Text,
                Shape = n.Shape,
                StrokeColor = n.StrokeColor
            }).ToList();

            var bestNodes = workingNodes.Select(n => new Node
            {
                Id = n.Id,
                X = n.X,
                Y = n.Y,
                Width = n.Width,
                Height = n.Height,
                Text = n.Text,
                Shape = n.Shape,
                StrokeColor = n.StrokeColor
            }).ToList();

            double initialFitness = EvaluateFitness(workingNodes, edges).Total;
            double currentFitness = initialFitness;
            double bestFitness = currentFitness;

            double temperature = options.InitialTemperature;
            int noImprovementCount = 0;
            int iteration = 0;

            while (temperature > options.MinTemperature &&
                   iteration < options.MaxIterations &&
                   noImprovementCount < options.MaxNoImprovement)
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Pick a random node
                int nodeIndex = _random.Next(workingNodes.Count);
                var node = workingNodes[nodeIndex];

                // Save old position
                double oldX = node.X;
                double oldY = node.Y;

                // Generate new position (scaled by temperature)
                double moveScale = temperature / options.InitialTemperature * 100;
                node.X += (_random.NextDouble() * 2 - 1) * moveScale;
                node.Y += (_random.NextDouble() * 2 - 1) * moveScale;

                // Keep in bounds
                node.X = Math.Max(0, Math.Min(2000, node.X));
                node.Y = Math.Max(0, Math.Min(2000, node.Y));

                // Evaluate new fitness
                double newFitness = EvaluateFitness(workingNodes, edges).Total;
                double delta = newFitness - currentFitness;

                // Metropolis acceptance criterion
                if (delta < 0 || _random.NextDouble() < Math.Exp(-delta / temperature))
                {
                    currentFitness = newFitness;

                    if (currentFitness < bestFitness)
                    {
                        bestFitness = currentFitness;
                        // Save best solution
                        for (int i = 0; i < workingNodes.Count; i++)
                        {
                            bestNodes[i].X = workingNodes[i].X;
                            bestNodes[i].Y = workingNodes[i].Y;
                        }
                        noImprovementCount = 0;
                    }
                    else
                    {
                        noImprovementCount++;
                    }
                }
                else
                {
                    // Revert
                    node.X = oldX;
                    node.Y = oldY;
                    noImprovementCount++;
                }

                // Cool down
                temperature *= options.CoolingRate;
                iteration++;

                // Progress callback
                if (progressCallback != null && iteration % 100 == 0)
                {
                    progressCallback(iteration, bestFitness);
                    await Task.Yield(); // Allow UI updates
                }
            }

            // Final snap to grid
            foreach (var node in bestNodes)
            {
                node.X = Math.Round(node.X / options.GridSize) * options.GridSize;
                node.Y = Math.Round(node.Y / options.GridSize) * options.GridSize;
            }

            double improvement = initialFitness > 0
                ? (initialFitness - bestFitness) / initialFitness * 100
                : 0;

            return (bestNodes, improvement);
        }

        #endregion

        #region Quick Operations

        public void RemoveOverlaps(List<Node> nodes)
        {
            const int gap = 20;
            const int maxIterations = 100;

            for (int iter = 0; iter < maxIterations; iter++)
            {
                bool anyOverlap = false;

                for (int i = 0; i < nodes.Count; i++)
                {
                    for (int j = i + 1; j < nodes.Count; j++)
                    {
                        var a = nodes[i];
                        var b = nodes[j];

                        // Check overlap
                        if (a.X + a.Width + gap >= b.X &&
                            b.X + b.Width + gap >= a.X &&
                            a.Y + a.Height + gap >= b.Y &&
                            b.Y + b.Height + gap >= a.Y)
                        {
                            anyOverlap = true;

                            // Calculate push direction
                            double dx = (b.X + b.Width / 2) - (a.X + a.Width / 2);
                            double dy = (b.Y + b.Height / 2) - (a.Y + a.Height / 2);
                            double dist = Math.Sqrt(dx * dx + dy * dy);
                            if (dist == 0) { dx = 1; dist = 1; }

                            // Push apart
                            double pushX = dx / dist * gap;
                            double pushY = dy / dist * gap;

                            b.X += pushX;
                            b.Y += pushY;
                        }
                    }
                }

                if (!anyOverlap) break;
            }
        }

        public void CompactLayout(List<Node> nodes, double factor = 0.8)
        {
            if (nodes.Count == 0) return;

            // Find center of mass
            double centerX = nodes.Average(n => n.X + n.Width / 2);
            double centerY = nodes.Average(n => n.Y + n.Height / 2);

            // Move each node toward center
            foreach (var node in nodes)
            {
                double nodeCenterX = node.X + node.Width / 2;
                double nodeCenterY = node.Y + node.Height / 2;

                double dx = centerX - nodeCenterX;
                double dy = centerY - nodeCenterY;

                node.X += dx * (1 - factor);
                node.Y += dy * (1 - factor);
            }
        }

        public void SnapToGrid(List<Node> nodes, int gridSize = 20)
        {
            foreach (var node in nodes)
            {
                node.X = Math.Round(node.X / gridSize) * gridSize;
                node.Y = Math.Round(node.Y / gridSize) * gridSize;
            }
        }

        #endregion
    }

    #region Result Classes

    public class FitnessResult
    {
        public double Total { get; set; }
        public double EdgeLengthPenalty { get; set; }
        public int EdgeCrossings { get; set; }
        public int NodeOverlaps { get; set; }
        public double EdgeNodeOverlaps { get; set; }
        public double AspectRatioPenalty { get; set; }
        public double DistributionPenalty { get; set; }
        public int AlignedPairs { get; set; }
        public int GridSnappedNodes { get; set; }
        public double SymmetryScore { get; set; }

        public override string ToString()
        {
            return $@"Total Fitness: {Total:F1} (lower = better)

Components:
• Edge Length: {EdgeLengthPenalty:F1}
• Edge Crossings: {EdgeCrossings} crossings
• Node Overlaps: {NodeOverlaps} overlaps
• Edge-Node Overlap: {EdgeNodeOverlaps:F1}
• Aspect Ratio: {AspectRatioPenalty:F1}
• Distribution: {DistributionPenalty:F1}

Bonuses:
• Alignment: {AlignedPairs} aligned pairs
• Grid Snap: {GridSnappedNodes} snapped nodes
• Symmetry: {SymmetryScore:F1}";
        }
    }

    #endregion
}
