using dfd2wasm.Models;
namespace dfd2wasm.Services
{
    using dfd2wasm.Services;

    public class GeometryService
    {
        public const int GridSize = 20;
        public const int OrthoSpacing = 140;
        public const int ConnectionPointSpacing = 15;
        public const int ColumnHeightLimit = 10000;

        public double SnapToGrid(double value, bool enabled)
        {
            return enabled ? Math.Round(value / GridSize) * GridSize : value;
        }

        public (double X, double Y) GetConnectionPointCoordinates(Node node, string side, int position)
        {
            var offset = position * ConnectionPointSpacing;

            return side switch
            {
                "top" => (node.X + node.Width / 2 + offset, node.Y),
                "bottom" => (node.X + node.Width / 2 + offset, node.Y + node.Height),
                "left" => (node.X, node.Y + node.Height / 2 + offset),
                "right" => (node.X + node.Width, node.Y + node.Height / 2 + offset),
                _ => (node.X + node.Width / 2, node.Y + node.Height / 2)
            };
        }

        public ConnectionPoint FindClosestConnectionPoint(Node node, double clickX, double clickY)
        {
            var sides = new[] { "top", "bottom", "left", "right" };
            var positions = new[] { -2, -1, 0, 1, 2 };

            ConnectionPoint closest = null;
            double minDistance = double.MaxValue;

            foreach (var side in sides)
            {
                foreach (var pos in positions)
                {
                    var (cx, cy) = GetConnectionPointCoordinates(node, side, pos);
                    var distance = Math.Sqrt(Math.Pow(clickX - cx, 2) + Math.Pow(clickY - cy, 2));

                    if (distance < minDistance)
                    {
                        minDistance = distance;
                        closest = new ConnectionPoint { Side = side, Position = pos };
                    }
                }
            }

            return closest ?? new ConnectionPoint { Side = "right", Position = 0 };
        }
        public (ConnectionPoint from, ConnectionPoint to) GetOptimalConnectionPoints(Node fromNode, Node toNode)
        {
            // Calculate centers
            var fromCenterX = fromNode.X + fromNode.Width / 2;
            var fromCenterY = fromNode.Y + fromNode.Height / 2;
            var toCenterX = toNode.X + toNode.Width / 2;
            var toCenterY = toNode.Y + toNode.Height / 2;

            // Calculate angle between nodes
            var dx = toCenterX - fromCenterX;
            var dy = toCenterY - fromCenterY;
            var angle = Math.Atan2(dy, dx) * 180 / Math.PI;

            // Determine best sides based on relative position
            string fromSide, toSide;

            if (Math.Abs(dx) > Math.Abs(dy))
            {
                // Horizontal connection is dominant
                if (dx > 0)
                {
                    // Target is to the right
                    fromSide = "right";
                    toSide = "left";
                }
                else
                {
                    // Target is to the left
                    fromSide = "left";
                    toSide = "right";
                }
            }
            else
            {
                // Vertical connection is dominant
                if (dy > 0)
                {
                    // Target is below
                    fromSide = "bottom";
                    toSide = "top";
                }
                else
                {
                    // Target is above
                    fromSide = "top";
                    toSide = "bottom";
                }
            }

            return (
                new ConnectionPoint { Side = fromSide, Position = 0 },
                new ConnectionPoint { Side = toSide, Position = 0 }
            );
        }

        public (double X, double Y) CalculateOrthoPlacement(
            List<Node> nodes,
            double clickX,
            double clickY,
            bool snapEnabled)
        {
            const double epsilon = 5.0;

            // First node - center on click
            if (nodes.Count == 0)
            {
                double x = clickX - 60;
                double y = clickY - 30;
                return (SnapToGrid(x, snapEnabled), SnapToGrid(y, snapEnabled));
            }

            var lastNode = nodes[^1];
            var lastNodeBottom = lastNode.Y + lastNode.Height;

            // Check if click X is within the last node's column (with margin)
            var margin = 40.0; // pixels of margin on each side
            var isInSameColumn = clickX >= (lastNode.X - margin) &&
                                clickX <= (lastNode.X + lastNode.Width + margin);

            // Check if click Y is below the last node
            var isBelow = clickY > (lastNodeBottom + epsilon);

            double targetX, targetY;

            if (isInSameColumn && isBelow)
            {
                // Click is below and horizontally aligned - stack vertically
                targetX = lastNode.X;
                targetY = lastNodeBottom + OrthoSpacing;

                // No height limit for now - just keep stacking
                // (You can scroll if needed)
            }
            else if (isInSameColumn && !isBelow)
            {
                // Click is above or at same height in same column
                if (clickY < lastNode.Y - epsilon)
                {
                    // Place above
                    targetX = lastNode.X;
                    targetY = lastNode.Y - lastNode.Height - OrthoSpacing;
                    if (targetY < 0) targetY = 0;
                }
                else
                {
                    // Same height - place to the right
                    targetX = lastNode.X + lastNode.Width + OrthoSpacing;
                    targetY = lastNode.Y;
                }
            }
            else
            {
                // Click is in a different column
                var topY = nodes.Min(n => n.Y);

                if (clickX > lastNode.X + lastNode.Width)
                {
                    // New column to the right
                    targetX = lastNode.X + lastNode.Width + OrthoSpacing;
                    targetY = topY;
                }
                else
                {
                    // New column to the left
                    targetX = lastNode.X - lastNode.Width - OrthoSpacing;
                    targetY = topY;
                    if (targetX < 0) targetX = 0;
                }
            }

            // Apply snapping
            targetX = SnapToGrid(targetX, snapEnabled);
            targetY = SnapToGrid(targetY, snapEnabled);

            // Simple collision check
            int nudgeAttempts = 0;
            while (nudgeAttempts < 10 && nodes.Any(n =>
                Math.Abs(n.X - targetX) < epsilon &&
                Math.Abs(n.Y - targetY) < epsilon))
            {
                targetY = SnapToGrid(targetY + GridSize, snapEnabled);
                nudgeAttempts++;
            }

            // Clamp to canvas bounds
            targetX = Math.Max(0, Math.Min(1800, targetX));
            targetY = Math.Max(0, Math.Min(1800, targetY));

            return (targetX, targetY);
        }

        // Helper: Find the topmost Y position across all columns
        private double FindTopOfNewColumn(List<IGrouping<double, Node>> columns)
        {
            if (columns.Count == 0) return 0;

            var topY = columns
                .SelectMany(col => col)
                .Min(n => n.Y);

            return topY;
        }

        // Helper: Check for collisions and nudge position if needed
        private (double X, double Y) AvoidCollisions(
            double x, double y, double width, double height,
            List<Node> nodes, bool snapEnabled)
        {
            const int maxAttempts = 20;
            int attempts = 0;

            while (attempts < maxAttempts)
            {
                bool collision = nodes.Any(n =>
                    !(x + width < n.X ||
                      x > n.X + n.Width ||
                      y + height < n.Y ||
                      y > n.Y + n.Height));

                if (!collision) return (x, y);

                // Nudge down by grid size
                y = SnapToGrid(y + GridSize, snapEnabled);
                attempts++;
            }

            // If still colliding after max attempts, nudge right
            x = SnapToGrid(x + OrthoSpacing, snapEnabled);
            y = FindTopOfNewColumn(nodes.GroupBy(n => SnapToGrid(n.X, snapEnabled)).ToList());

            return (x, y);
        }
    }
}
